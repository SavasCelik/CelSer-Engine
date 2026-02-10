using System.Threading;
using System.Threading.Channels;
using System.Xml.Linq;

namespace CelSerEngine.Core.Scanners;

public class PointerScanWorker
{
    public int PointersFound { get; set; }

    private readonly DefaultPointerScanner _pointerScanner;
    private readonly IResultStorage _resultStorage;
    private readonly CancellationToken _cancellationToken;
    private readonly Channel<PathQueueElement> _channel;
    private readonly PendingCounter _pendingCounter;
    private IntPtr[] _tempResults;
    private UIntPtr[] _valueList;
    private int _pathsEvaluated = 0;
    private List<ResultPointer> _results = new List<ResultPointer>();
    private int _maxLevel;
    private int _structSize;
    private readonly bool _preventLoops;
    private readonly bool _onlyOneStaticInPath;
    private readonly bool _limitToMaxOffsetsPerNode;
    private readonly int _maxOffsetsPerNode;

    public PointerScanWorker(PointerScanner2 pointerScanner, IResultStorage resultStorage, Channel<PathQueueElement> channel, PendingCounter pendingCounter, CancellationToken cancellationToken)
    {
        _pointerScanner = (DefaultPointerScanner)pointerScanner;
        _resultStorage = resultStorage;
        _cancellationToken = cancellationToken;
        _channel = channel;
        _pendingCounter = pendingCounter;
        _structSize = pointerScanner.PointerScanOptions.MaxOffset;
        _maxLevel = pointerScanner.PointerScanOptions.MaxLevel;
        _tempResults = new IntPtr[_maxLevel];
        _valueList = new UIntPtr[_maxLevel];
        _preventLoops = pointerScanner.PointerScanOptions.PreventLoops;
        _onlyOneStaticInPath = pointerScanner.PointerScanOptions.OnlyOneStaticInPath;
        _limitToMaxOffsetsPerNode = pointerScanner.PointerScanOptions.LimitToMaxOffsetsPerNode;
        _maxOffsetsPerNode = pointerScanner.PointerScanOptions.MaxOffsetsPerNode;
    }

    public async Task<IResultStorage> StartAsync()
    {
        try
        {
            var reader = _channel.Reader;
            while (await reader.WaitToReadAsync(_cancellationToken))
            {
                while (reader.TryRead(out var element))
                {
                    try
                    {
                        var startLevel = element.StartLevel;
                        Array.Copy(element.TempResults, _tempResults, startLevel);


                        if (_preventLoops)
                        {
                            Array.Copy(element.ValueList, _valueList, startLevel);
                        }

                        var valueToFind = element.ValueToFind;
                        element.Dispose();
                        ReverseScan(valueToFind, startLevel);
                    }
                    finally
                    {
                        if (Interlocked.Decrement(ref _pendingCounter.Value) == 0)
                        {
                            _channel.Writer.TryComplete();
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Scan was cancelled, just exit gracefully
        }

        return _resultStorage;
    }

    private void ReverseScan(IntPtr valueToFind, int level)
    {
        if (level >= _maxLevel)
            return;

        var differentOffsetsInThisNode = 0;
        var startValue = IntPtr.Zero;
        var stopValue = IntPtr.Zero;
        var exactOffset = false;

        if (!exactOffset)
        {
            startValue = valueToFind - _structSize;
            stopValue = valueToFind;

            if (startValue > stopValue)
            {
                startValue = IntPtr.Zero;
            }
        }

        if (_preventLoops)
        {
            //check if this valuetofind is already in the list
            for (var i = 0; i <= level - 1; i++)
            {
                if (_valueList[i] == (UIntPtr)valueToFind)
                {
                    return;
                }
            }

            _valueList[level] = (UIntPtr)valueToFind;
        }

        var dontGoDeeper = false;
        var pointerScanner = _pointerScanner;
        //PointerList? plist = pointerScanner.FindPointerValue(startValue, ref stopValue);
        var currentIndex = pointerScanner.BinarySearchClosestLowerKey(stopValue, startValue);
        PointerList? plist = pointerScanner.GetPointerValueByIndex(currentIndex);
        stopValue = plist?.PointerValue ?? 0;

        while (stopValue >= startValue)
        {
            if (plist == null)
            {
                _pathsEvaluated++;
                return;
            }

            _tempResults[level] = valueToFind - stopValue; //store the offset &/SCK: stopvalue = plist.PointerValue

            //go through the list of addresses that have this address(stopvalue) as their value
            var pListPos = plist.Pos - 1;
            for (var j = 0; j <= pListPos; j++)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                _pathsEvaluated++;
                ref var plistList = ref plist.List[j];

                if (plistList.StaticData == null) //this removes a lot of other possible paths. Perhaps a feature to remove this check ?
                {
                    if (!dontGoDeeper)
                    {
                        //check if we should go deeper into these results (not if max level has been reached)

                        if ((level + 1) < _maxLevel)
                        {
                            var addedToQueue = false;

                            //if (not terminated) and(not outofdiskspace ^) then //if there is not enough diskspace left wait till it's terminated, or diskspace is freed

                            if (
                                ((level + 3 < _maxLevel) &&
                                    (
                                        ((_pendingCounter.Value < PointerScanner2.MaxQueueSize - (PointerScanner2.MaxQueueSize / 3))) ||
                                        ((level <= 2) && (_pendingCounter.Value < PointerScanner2.MaxQueueSize - (PointerScanner2.MaxQueueSize / 8))) ||
                                        ((level <= 1) && (_pendingCounter.Value < PointerScanner2.MaxQueueSize - (PointerScanner2.MaxQueueSize / 16))) ||
                                        ((level == 0) && (_pendingCounter.Value < PointerScanner2.MaxQueueSize - 1))
                                    )
                                )
                                || (_pendingCounter.Value == 0)) // completely empty
                            {
                                //there's room and not a crappy work item. Add it
                                //if locked then

                                if (_pendingCounter.Value < PointerScanner2.MaxQueueSize - 1)
                                {
                                    //still room

                                    var newElement = new PathQueueElement(_maxLevel);
                                    Array.Copy(_tempResults, newElement.TempResults, _maxLevel);

                                    if (_preventLoops)
                                    {
                                        Array.Copy(_valueList, newElement.ValueList, _maxLevel);
                                    }

                                    newElement.StartLevel = level + 1;
                                    newElement.ValueToFind = plistList.Address;

                                    if (_channel.Writer.TryWrite(newElement))
                                    {
                                        Interlocked.Increment(ref _pendingCounter.Value);
                                        addedToQueue = true;
                                    }
                                    else
                                    {
                                        newElement.Dispose();
                                    }
                                }
                            }

                            if (!addedToQueue)
                            {
                                //I'll have to do it myself
                                ReverseScan(plistList.Address, level + 1);
                                ///done with this branch 
                            }
                        }
                        else
                        {
                            //end of the line
                            //if (not staticonly) then //store this results entry
                            //begin
                            //    nostatic.moduleindex:=$FFFFFFFF;
                            //    nostatic.offset:= plist.list[j].address;
                            //    StorePath(level, -1, plist.list[j].address);
                            //end;
                        }
                    } //else don't go deeper
                }
                else
                {
                    //found a static one 
#if DEBUG
                    StorePath(level, plist.List[j].StaticData.Value.ModuleIndex, plist.List[j].StaticData.Value.Offset);
#endif

                    if (_onlyOneStaticInPath)
                    {
                        dontGoDeeper = true;
                    }
                }
            }

            if (_limitToMaxOffsetsPerNode) //check if the current iteration is less than maxOffsetsPerNode 
            {
                if (level > 0)
                {
                    differentOffsetsInThisNode++;
                }

                if (differentOffsetsInThisNode >= _maxOffsetsPerNode)
                {
                    return; //the max node has been reached 
                }
            }

            plist = pointerScanner.GetPointerValueByIndex(--currentIndex);

            if (plist != null)
            {
                stopValue = plist.PointerValue;
            }
            else
            {
                return; //nothing else to be found 
            }
        }
    }

    private void StorePath(int level, int moduleIndex, nint offset)
    {
        PointersFound++;
        _resultStorage.Save(level, moduleIndex, offset, _tempResults.AsSpan(0, level + 1));

        //_results.Add(new ResultPointer { Level = level, ModuleIndex = moduleIndex, Offset = offset, TempResults = _tempResults.Take(level + 1).ToArray() });
        //_binaryWriter.Write7BitEncodedInt(moduleIndex);
        //_binaryWriter.Write7BitEncodedInt(offset.ToInt32());
        //_binaryWriter.Write7BitEncodedInt(level + 1);
        //foreach (var tempResult in _tempResults.AsSpan(0, level + 1))
        //{
        //    _binaryWriter.Write7BitEncodedInt(tempResult.ToInt32());
        //}

        //var res = new ResultPointer
        //{
        //    Level = level,
        //    ModuleIndex = moduleIndex,
        //    Offset = offset,
        //    TempResults = _tempResults.ToArray()
        //};
        //_memoryStream.Write(BitConverter.GetBytes(res.ModuleIndex));
        //_memoryStream.Write(BitConverter.GetBytes(res.Offset.ToInt64()));
        //_memoryStream.Write(BitConverter.GetBytes(res.Level));
        //var aaa = res.TempResults.SelectMany(x => BitConverter.GetBytes(x.ToInt64())).ToArray();
        //_memoryStream.Write(aaa);

        //if (_memoryStream.Position > 15 * 1024 * 1024)
        //{
        //    _fileStream.Write(myBuffer, 0, (int)_memoryStream.Position);
        //    _memoryStream.Seek(0, SeekOrigin.Begin);
        //}

    }
}
