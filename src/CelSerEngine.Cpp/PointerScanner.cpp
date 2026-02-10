#include "pch.h"
#include "PointerScanner.h"
#include "PointerScanWorker.h"
#include <optional>
#include <algorithm>

PointerScanner::PointerScanner(int64_t* pDictkeys, PointerList* pLists, int32_t count)
{
    keyArray_.reserve(count);
    pointerDict_.reserve(count);

    for (size_t i = 0; i < count; i++)
    {
        auto k = pDictkeys[i];
        auto v = &pLists[i];

        pointerDict_.try_emplace(k, v);
        keyArray_.push_back(k);
    }

    std::sort(keyArray_.begin(), keyArray_.end());
    PointerList* current = nullptr;

    for (auto key : keyArray_)
    {
        PointerList* value = pointerDict_[key];
        if (!current)
        {
            current = value;
            continue;
        }

        value->Previous = current;
        current = value;
    }

    for (const auto key : keyArray_)
    {
        plistArray_.emplace_back(pointerDict_[key]);
    }
}

int32_t PointerScanner::BinarySearchClosestLowerKey(int64_t searchedKey, int64_t minValue) const
{
    int32_t low = 0;
    int32_t high = static_cast<int32_t>(keyArray_.size()) - 1;
    int32_t closestLowerIndex = -1;

    while (low <= high)
    {
        int32_t mid = (low + high) >> 1;
        int64_t key = keyArray_[mid];

        if (key <= searchedKey)
        {
            if (key >= minValue)
            {
                closestLowerIndex = mid;
            }
            low = mid + 1; // Search higher half
        }
        else
        {
            high = mid - 1; // Search lower half
        }
    }

    return closestLowerIndex; // Index of the closest lower key
}

PointerList* PointerScanner::FindPointerValue(int64_t startValue, int64_t& stopValue) const
{
    PointerList* result = nullptr;
    std::optional<int64_t> closestLowerKey;
    auto it = pointerDict_.find(stopValue);

    if (it != pointerDict_.end())
    {
        result = it->second;
    }
    else
    {
        // Find the closest lower key
        int32_t closestLowerKeyIndex = BinarySearchClosestLowerKey(stopValue, startValue);
        if (closestLowerKeyIndex >= 0)
        {
            closestLowerKey = keyArray_[closestLowerKeyIndex];
        }

        if (closestLowerKey.has_value())
        {
            auto found = pointerDict_.find(closestLowerKey.value());

            if (found != pointerDict_.end())
            {
                result = found->second;
            }
        }
    }

    if (result != nullptr)
    {
        stopValue = result->PointerValue;
    }

    return result;
}

int32_t PointerScanner::StartScan(int64_t searchedAddress, int32_t maxLevel, int32_t structSize)
{
    maxLevel_ = maxLevel;
    searchedAddress_ = searchedAddress;
    InitializeEmptyPathQueue();
    int32_t workerCount = 6;

    for (int i = 0; i < workerCount; ++i) {
        workers.emplace_back(*this, searchedAddress, maxLevel, structSize);
    }
    threads.resize(workers.size());

    for (size_t i = 0; i < workers.size(); ++i) {
        threads[i] = std::thread(&PointerScanWorker::StartScan, &workers[i]);
    }

    /*PointerScanWorker pWorker(*this, searchedAddress, maxLevel, structSize);
    pWorker.StartScan();*/
    finishedCount = workers.size();
    {
        /*{
            std::unique_lock<std::mutex> lock(mtx);
            cv.wait(lock, [this]() 
                {  
                    bool allDone = false;
                    {
                        std::lock_guard<std::recursive_mutex> lock(pathqueueCS);
                        if (PathQueueLength == 0) {
                            allDone = true;
                            for (const auto& worker : workers) {
                                if (!worker.isDone) {
                                    allDone = false;
                                    break;
                                }
                            }
                        }
                    }

                    return allDone;
                });
        }*/

        
        bool allDone = false;
        while (!allDone) {
            std::this_thread::sleep_for(std::chrono::milliseconds(50));
            {
                std::lock_guard<std::recursive_mutex> lock(pathqueueCS);
                if (PathQueueLength == 0) {
                    allDone = true;
                    for (auto& worker : workers) {
                        if (!worker.isDone) {
                            allDone = false;
                            break;
                        }
                    }
                }
            }
        }
        

        for (auto& w : workers) {
            w.Terminated = true;
        }

        // Join threads
        for (auto& t : threads) {
            if (t.joinable()) t.join();
        }
    }

    int32_t pCounts = 0;

    for (auto& w : workers) {
        pCounts += w.Count;
    }

    return pCounts;
}

PointerList* PointerScanner::GetPointerValueByIndex(int32_t plistIndex) const
{
    if (plistIndex >= 0 && plistIndex < plistArray_.size())
    {
        return plistArray_[plistIndex];
    }

    return nullptr;
}

void PointerScanner::notifyWorkerFinished() 
{
    /*std::unique_lock<std::mutex> lock(mtx);
    finishedCount++;*/
    cv.notify_all();
}

void PointerScanner::notifyWorkerStarted()
{
    std::unique_lock<std::mutex> lock(mtx);
    finishedCount--;
}

void PointerScanner::InitializeEmptyPathQueue()
{
    for (int32_t i = 0; i < MaxQueueSize; ++i)
    {
        // Ensure PathQueue[i] exists
        if (!PathQueue[i])
        {
            PathQueue[i] = std::make_unique<PathQueueElement>(maxLevel_);
        }

        // Fill TempResults with 0xcececece
        for (int32_t j = 0; j <= maxLevel_; ++j)
        {
            PathQueue[i]->TempResults[j] = static_cast<intptr_t>(0xcececece);
        }

        // Fill ValueList if NoLoop is true
        if (NoLoop)
        {
            for (int32_t j = 0; j <= maxLevel_; ++j)
            {
                PathQueue[i]->ValueList[j] = static_cast<uintptr_t>(0xcecececececececeULL);
            }
        }
    }

    // Initialize the first element for scanning
    if (maxLevel_ > 0)
    {
        // equivalent to: if (initializer) then
        //if (!_findValueInsteadOfAddress)
        if (!false)
        {
            PathQueue[PathQueueLength]->StartLevel = 0;
            PathQueue[PathQueueLength]->ValueToFind = searchedAddress_;
            ++PathQueueLength;
        }
    }
}
