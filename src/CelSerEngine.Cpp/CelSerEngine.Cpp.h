// The following ifdef block is the standard way of creating macros which make exporting
// from a DLL simpler. All files within this DLL are compiled with the CELSERENGINECPP_EXPORTS
// symbol defined on the command line. This symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see
// CELSERENGINECPP_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.
#ifdef CELSERENGINECPP_EXPORTS
#define CELSERENGINECPP_API __declspec(dllexport)
#else
#define CELSERENGINECPP_API __declspec(dllimport)
#endif

#include <string>
#include <algorithm>
#include "Models.h"

struct ModuleInfo
{
    const char* Name;    // C-style string for interop
    int64_t BaseAddress;
    uint32_t Size;       // uint
    int32_t ModuleIndex; // int

    // Helper functions (not part of the memory layout)
    std::string ShortName() const
    {
        if (Name == nullptr) return "";

        std::string nameStr(Name);
        size_t index = nameStr.find_last_of("\\");
        if (index != std::string::npos)
            nameStr = nameStr.substr(index + 1);

        return nameStr;
    }

    bool IsSystemModule() const
    {
        if (Name == nullptr) return false;

        std::string lowerName(Name);
        std::transform(lowerName.begin(), lowerName.end(), lowerName.begin(), ::tolower);
        return lowerName.find("windows\\") != std::string::npos;
    }
};

extern "C" {
	CELSERENGINECPP_API int32_t StartPointerScan(int64_t* pDictkeys, PointerList* pLists, int32_t count, int64_t valueToFind, int32_t maxLevel, int32_t structSize);
}

