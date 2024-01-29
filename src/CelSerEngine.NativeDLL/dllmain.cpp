// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include "CPointer.cpp"
#include <map>
#include <algorithm>
#include <vector>

extern "C"             //No name mangling
__declspec(dllexport)  //Tells the compiler to export the function
int                    //Function return type     
__cdecl                //Specifies calling convention, cdelc is default, 
                       //so this can be omitted 
    test(CPointer pointerList[], int pointerListSize, int maxLevel, int maxOffset, CPointer heapPointers[], int heapPointersSize, CPointer staticPointers[], int staticPointersSize) {

    std::sort(heapPointers, heapPointers + heapPointersSize, [](const CPointer& a, const CPointer& b) {
        return a.PointingTo < b.PointingTo;
    });
    
    // Grouping by PointingTo using a map
    std::map<DWORD, std::vector<CPointer>> heapDict;
    for (size_t i = 0; i < heapPointersSize; ++i) {
        heapDict[heapPointers[i].PointingTo].push_back(heapPointers[i]);
    }
    
    std::map<DWORD, std::vector<CPointer>> staticDict;
    for (size_t i = 0; i < staticPointersSize; ++i) {
        staticDict[staticPointers[i].Address].push_back(staticPointers[i]);
    }

    std::map<DWORD, int> counter;

    for (size_t currentLevel = 0; currentLevel < maxLevel; currentLevel++)
    {
        std::vector<CPointer> matchingPointers;

        for (size_t pointerIndex = 0; pointerIndex < pointerListSize; pointerIndex++)
        {
            CPointer pointer = pointerList[pointerIndex];

            if (currentLevel == maxLevel - 1)
            {
                continue;
            }

            for (int currentOffset = 0; currentOffset < maxOffset; currentOffset += 8) {
                DWORD newAddress = pointer.Address - currentOffset;
                auto foundPointers = heapDict.find(newAddress);

                if (foundPointers == heapDict.end() || foundPointers->second.size() == 0) {
                    continue;
                }

                DWORD newOffsets[5];
                std::copy_n(pointer.Offsets, 5, newOffsets);

                for (int i = 0; i < 5; i++)
                {
                    if (newOffsets[i] == -1) {
                        newOffsets[i] = currentOffset;
                        break;
                    }
                }
                
                for (const auto& cPointer : foundPointers->second) {
                    CPointer clonedCPointer = cPointer;
                    std::copy_n(newOffsets, 5, clonedCPointer.Offsets);
                    matchingPointers.push_back(clonedCPointer);
                }

                auto search = counter.find(newAddress);
                auto searchStaticNewAddy = staticDict.find(newAddress);
                int count = 0;

                if (search != counter.end()) {
                    count = search->second;
                }

                if (searchStaticNewAddy != staticDict.end() && count >= 3) {
                    heapDict.erase(foundPointers);
                }
                else {
                    count++;
                    counter[newAddress] = count;
                }

            }
        }
    }

    /*for (var currentLevel = 0; currentLevel < pointerScanOptions.MaxLevel; currentLevel++)
    {
        if (token.IsCancellationRequested)
        {
            break;
        }

        var pointerList = _pointerList.OrderBy(x = > x.Offsets.Last()).ToArray();
        _pointerList.Clear();

        if (useParallel)
        {
            Parallel.ForEach(pointerList, (pointer) = > ProcessPointer(pointer, pointerWithStaticPointerPaths, pointerScanOptions, currentLevel));
        }
        else
        {
            foreach(var pointer in pointerList)
            {
                ProcessPointer(pointer, pointerWithStaticPointerPaths, pointerScanOptions, currentLevel);
            }
        }
    }*/

    return -1;
}
