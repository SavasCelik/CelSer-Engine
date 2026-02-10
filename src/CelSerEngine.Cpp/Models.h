#pragma once
#include <cstdint>

struct StaticData
{
    bool HasValue;
    int32_t ModuleIndex;
    int64_t Offset;
};

struct PointerData
{
    int64_t Address;
    StaticData StaticData;
};

struct PointerList
{
    int32_t MaxSize;
    int32_t ExpectedSize;
    int32_t Pos;
    PointerData* List;

    //Linked list
    int64_t PointerValue;
    PointerList* Previous;
    //PointerList* Next;
};