// CelSerEngine.Cpp.cpp : Defines the exported functions for the DLL.
//

#include "pch.h"
#include "CelSerEngine.Cpp.h"
#include <unordered_map>
#include "PointerScanner.h"

extern "C" {
	CELSERENGINECPP_API int StartPointerScan(int64_t* pDictkeys, PointerList* pLists, int32_t count, int64_t valueToFind, int32_t maxLevel, int32_t structSize)
	{
		PointerScanner ps(pDictkeys, pLists, count);
		auto countPointers = ps.StartScan(valueToFind, maxLevel, structSize);

		return countPointers;
	}
}