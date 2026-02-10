#pragma once
#include "Models.h"
#include <unordered_map>
#include <memory>
#include <array>
#include <mutex>
#include "PointerScanWorker.h"

class PointerScanWorker; // Forward declaration

class PathQueueElement
{
public:
    std::vector<uint32_t> TempResults;
    std::vector<uint64_t> ValueList;
    /*uint32_t* TempResults;
    uintptr_t* ValueList;*/
    int64_t ValueToFind = 0;
    int32_t StartLevel = 0;

    explicit PathQueueElement(int32_t maxLevel)
        : TempResults(maxLevel + 1), ValueList(maxLevel + 1) {
    }
};

class PointerScanner
{
public:
	PointerScanner(int64_t* pDictkeys, PointerList* pLists, int32_t count);
	int32_t BinarySearchClosestLowerKey(int64_t searchedKey, int64_t minValue) const;
	PointerList* FindPointerValue(int64_t startValue, int64_t& stopValue) const;
    int32_t StartScan(int64_t searchedAddress, int32_t maxLevel, int32_t structSize);
    PointerList* GetPointerValueByIndex(int32_t plistIndex) const;
    void notifyWorkerFinished();
    void notifyWorkerStarted();

private:
    void InitializeEmptyPathQueue();

private:
	std::unordered_map<int64_t, PointerList*> pointerDict_;
	
    int32_t maxLevel_ = 0;
	int64_t searchedAddress_ = 0;
    std::vector<std::thread> threads;
    std::vector<PointerScanWorker> workers;

    std::mutex mtx;
    std::condition_variable cv;
    int finishedCount{ 0 };

public:
    std::vector<int64_t> keyArray_;
    std::vector<PointerList*> plistArray_;
    std::recursive_mutex pathqueueCS;
	bool NoLoop = true;
    static constexpr int32_t MaxQueueSize = 64;
	int32_t PathQueueLength = 0;
    std::array<std::unique_ptr<PathQueueElement>, MaxQueueSize> PathQueue;
};
