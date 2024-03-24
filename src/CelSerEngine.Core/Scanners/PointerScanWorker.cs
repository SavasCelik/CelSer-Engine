namespace CelSerEngine.Core.Scanners;

internal class PointerScanWorker
{
    private readonly PointerScanner2 _pointerScanner;
    private IntPtr[] _tempResults;
    private UIntPtr[] _valueList;
    private int _pathsEvaluated = 0;
    private int _pointersFound = 0;
    private List<ResultPointer> _results = new List<ResultPointer>();

    public PointerScanWorker(PointerScanner2 pointerScanner)
    {
        _pointerScanner = pointerScanner;
        _tempResults = new IntPtr[PointerScanner2.MaxLevel];
        _valueList = new UIntPtr[PointerScanner2.MaxLevel];
    }

    public void Start()
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
                Array.Copy(_pointerScanner.PathQueue[i].TempResults, _tempResults, PointerScanner2.MaxLevel);

                if (PointerScanner2.NoLoop)
                {
                    Array.Copy(_pointerScanner.PathQueue[i].ValueList, _valueList, PointerScanner2.MaxLevel);
                }
            }

            ReverseScan(valueToFind, startLevel);

            if (_pointerScanner.PathQueueLength == 0)
            {
                break;
            }
        }

        var resa = _results.OrderBy(x => x.TempResults[0]).ThenBy(x => x.TempResults[1]).ToArray();
        var hoh = "";
    }

    private void ReverseScan(IntPtr valueToFind, int level)
    {
        if (level >= PointerScanner2.MaxLevel)
            return;

        var differentOffsetsInThisNode = 0;
        var startValue = IntPtr.Zero;
        var stopValue = IntPtr.Zero;
        var exactOffset = false;

        if (!exactOffset)
        {
            startValue = valueToFind - PointerScanner2.StructSize;
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
            //fix this even first run result with plist = null
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
                _pathsEvaluated++;

                if (plist.List[j].StaticData == null) //this removes a lot of other possible paths. Perhaps a feature to remove this check ?
                {
                    if (!dontGoDeeper)
                    {
                        //check if we should go deeper into these results (not if max level has been reached)

                        if ((level + 1) < PointerScanner2.MaxLevel)
                        {
                            var addedToQueue = false;

                            //if (not terminated) and(not outofdiskspace ^) then //if there is not enough diskspace left wait till it's terminated, or diskspace is freed

                            if (
                                ((level + 3 < PointerScanner2.MaxLevel) &&
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

                                    Array.Copy(_tempResults, _pointerScanner.PathQueue[_pointerScanner.PathQueueLength].TempResults, PointerScanner2.MaxLevel);

                                    if (PointerScanner2.NoLoop)
                                    {
                                        Array.Copy(_valueList, _pointerScanner.PathQueue[_pointerScanner.PathQueueLength].ValueList, PointerScanner2.MaxLevel);
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
        _pointersFound++;
        _results.Add(new ResultPointer { Level = level, ModuleIndex = moduleIndex, Offset = offset, TempResults = _tempResults.ToArray() });
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
