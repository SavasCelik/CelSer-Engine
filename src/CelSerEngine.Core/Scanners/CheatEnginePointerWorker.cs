namespace CelSerEngine.Core.Scanners;

public class CheatEnginePointerWorker
{
    public int PointersFound { get; set; }

    private readonly PointerScanner2 _pointerScanner;
    private readonly IResultStorage _resultStorage;
    private readonly CancellationToken _cancellationToken;
    private IntPtr[] _tempResults;
    private UIntPtr[] _valueList;
    private int _pathsEvaluated = 0;
    private List<ResultPointer> _results = new List<ResultPointer>();
    private int _maxLevel;
    private int _structSize;

    public CheatEnginePointerWorker(PointerScanner2 pointerScanner, IResultStorage resultStorage, CancellationToken cancellationToken)
    {
        _pointerScanner = pointerScanner;
        _resultStorage = resultStorage;
        _cancellationToken = cancellationToken;
        _structSize = pointerScanner.PointerScanOptions.MaxOffset;
        _maxLevel = pointerScanner.PointerScanOptions.MaxLevel;
        _tempResults = new IntPtr[_maxLevel];
        _valueList = new UIntPtr[_maxLevel];
    }

    public IList<ResultPointer> Start()
    {
        while (true)
        {
            var valueToFind = IntPtr.Zero;
            var startLevel = 0;

            if (_pointerScanner.PathQueueLength > 0)
            {
                _pointerScanner.PathQueueLength--;
                var i = _pointerScanner.PathQueueLength;
                valueToFind = _pointerScanner.PathQueue[i].ValueToFind;
                startLevel = _pointerScanner.PathQueue[i].StartLevel;
                Array.Copy(_pointerScanner.PathQueue[i].TempResults, _tempResults, _maxLevel);

                if (PointerScanner2.NoLoop)
                {
                    Array.Copy(_pointerScanner.PathQueue[i].ValueList, _valueList, _maxLevel);
                }
            }

            try
            {
                ReverseScan(valueToFind, startLevel);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (_pointerScanner.PathQueueLength == 0)
            {
                break;
            }
        }

        //var sorted = _results.OrderBy(x => x.TempResults[0]).ThenBy(x => x.TempResults[1]).ToArray();

        return _results;
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

        if (PointerScanner2.NoLoop)
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
        PointerList? plist = null;

        while (stopValue >= startValue)
        {
            if (plist == null)
            {
                plist = _pointerScanner.FindPointerValue(startValue, ref stopValue);
            }

            if (plist == null)
            {
                _pathsEvaluated++;
                return;
            }

            _tempResults[level] = valueToFind - stopValue; //store the offset &/SCK: stopvalue = plist.PointerValue

            //go through the list of addresses that have this address(stopvalue) as their value
            for (var j = 0; j <= plist.Pos - 1; j++)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                _pathsEvaluated++;

                if (plist.List[j].StaticData == null) //this removes a lot of other possible paths. Perhaps a feature to remove this check ?
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
                                        ((_pointerScanner.PathQueueLength < PointerScanner2.MaxQueueSize - (PointerScanner2.MaxQueueSize / 3))) ||
                                        ((level <= 2) && (_pointerScanner.PathQueueLength < PointerScanner2.MaxQueueSize - (PointerScanner2.MaxQueueSize / 8))) ||
                                        ((level <= 1) && (_pointerScanner.PathQueueLength < PointerScanner2.MaxQueueSize - (PointerScanner2.MaxQueueSize / 16))) ||
                                        ((level == 0) && (_pointerScanner.PathQueueLength < PointerScanner2.MaxQueueSize - 1))
                                    )
                                )
                                || (_pointerScanner.PathQueueLength == 0)) // completely empty
                            {
                                //there's room and not a crappy work item. Add it
                                //if locked then

                                if (_pointerScanner.PathQueueLength < PointerScanner2.MaxQueueSize - 1)
                                {
                                    //still room

                                    Array.Copy(_tempResults, _pointerScanner.PathQueue[_pointerScanner.PathQueueLength].TempResults, _maxLevel);

                                    if (PointerScanner2.NoLoop)
                                    {
                                        Array.Copy(_valueList, _pointerScanner.PathQueue[_pointerScanner.PathQueueLength].ValueList, _maxLevel);
                                    }

                                    _pointerScanner.PathQueue[_pointerScanner.PathQueueLength].StartLevel = level + 1;
                                    _pointerScanner.PathQueue[_pointerScanner.PathQueueLength].ValueToFind = plist.List[j].Address;
                                    _pointerScanner.PathQueueLength++;
                                    addedToQueue = true;
                                }
                            }

                            if (!addedToQueue)
                            {
                                //I'll have to do it myself
                                ReverseScan(plist.List[j].Address, level + 1);
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
                    StorePath(level, plist.List[j].StaticData.ModuleIndex, plist.List[j].StaticData.Offset);
                    //if onlyOneStaticInPath then DontGoDeeper:= true;
                }
            }

            if (PointerScanner2.LimitToMaxOffsetsPerNode) //check if the current iteration is less than maxOffsetsPerNode 
            {
                if (level > 0)
                {
                    differentOffsetsInThisNode++;
                }

                if (differentOffsetsInThisNode >= PointerScanner2.MaxOffsetsPerNode)
                {
                    return; //the max node has been reached 
                }
            }

            plist = plist.Previous;

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