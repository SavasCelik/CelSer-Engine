using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelSerEngine.Core.Scanners;
internal class CheatEnginePointerScanStrategy
{
    private ReversePointerTable[] _level0list = new ReversePointerTable[16];
    private const int MaxLevel = 15; // max nibble

    internal void AddPointer(IntPtr pointerValue, IntPtr pointerWithThisValue, bool add)
    {
        var plist = FindOrAddPointerValue(pointerValue, _level0list);

        if (!add)
        {
            plist.ExpectedSize += 1;
            return;
        }
        else
        {
            if (plist.List == null)
            {
                plist.List = new PointerData[plist.ExpectedSize];
                plist.MaxSize = plist.ExpectedSize;
            }

            if (plist.Pos >= plist.MaxSize) //the new entry will be over the maximum. Reallocate   
            {
                //quadrupple the storage
                var newList = new PointerData[plist.MaxSize * 4];
                Array.Copy(plist.List, newList, plist.List.Length);
                plist.List = newList;
                plist.MaxSize = plist.List.Length;
            }

            if (plist.List[plist.Pos] == null)
            {
                plist.List[plist.Pos] = new PointerData();
            }

            plist.List[plist.Pos].Address = pointerWithThisValue;

            if (isStatic(pointerWithThisValue, out var mi))
            {
                var staticData = new StaticData()
                {
                    ModuleIndex = mi.ModuleIndex,
                    Offset = pointerWithThisValue - mi.Mi.lpBaseOfDll
                };
                plist.List[plist.Pos].StaticData = staticData;
            }

            plist.Pos++;
        }
    }

    private PointerList FindOrAddPointerValue(IntPtr pointerValue, ReversePointerTable[] level0list)
    {
        var currentArray = level0list;
        var level = 0;
        int entryNr;

        while (level < MaxLevel)
        {
            entryNr = (int)(((UIntPtr)pointerValue >> ((MaxLevel - level) * 4)) & 0xF);

            if (currentArray[entryNr] == null)
            {
                currentArray[entryNr] = new ReversePointerTable();
            }

            if (currentArray[entryNr].ReversePointerListArray == null)
            {
                currentArray[entryNr].ReversePointerListArray = new ReversePointerTable[MaxLevel + 1];
            }

            currentArray = currentArray[entryNr].ReversePointerListArray!;
            level++;
        }

        entryNr = (int)((pointerValue >> ((MaxLevel - level) * 4)) & 0xF);
        if (currentArray[entryNr] == null)
        {
            currentArray[entryNr] = new ReversePointerTable();
        }

        PointerList? plist = currentArray[entryNr].PointerList;
        if (plist == null)
        {
            plist = new PointerList();
            plist.PointerValue = pointerValue;
            plist.ExpectedSize = 1;

            if (pointerValue % 0x10 == 0)
            {
                plist.ExpectedSize = 5;

                if (pointerValue % 0x100 == 0)
                {
                    plist.ExpectedSize = 10;

                    if (pointerValue % 0x1000 == 0)
                    {
                        plist.ExpectedSize = 20;

                        if (pointerValue % 0x10000 == 0)
                        {
                            plist.ExpectedSize = 50;
                        }
                    }
                }
            }

            currentArray[entryNr].PointerList = plist;
            currentArray[entryNr].ReversePointerListArray = new ReversePointerTable[MaxLevel + 1];
        }

        return plist;
    }
}
