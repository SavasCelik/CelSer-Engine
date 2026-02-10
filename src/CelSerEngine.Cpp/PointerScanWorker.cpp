#include "pch.h"
#include "PointerScanWorker.h"

void PointerScanWorker::StartScan()
{
	/*auto lockingIt = L"locking";
	auto unlockingIt = L"unlocking";*/

	while (!Terminated)
	{
		int64_t valueToFind;
		int32_t startLevel;
		bool gotValue = false;

		{
			//OutputDebugString(lockingIt);
			std::lock_guard<std::recursive_mutex> lock(pointerScanner_.pathqueueCS);
			if (pointerScanner_.PathQueueLength > 0)
			{
				gotValue = true;
				pointerScanner_.PathQueueLength--;
				auto i = pointerScanner_.PathQueueLength;
				auto& element = pointerScanner_.PathQueue[i];
				valueToFind = element->ValueToFind;
				startLevel = element->StartLevel;

				tempResults_ = element->TempResults;

				if (pointerScanner_.NoLoop)
				{
					valueList_ = element->ValueList;
				}
			}
			//OutputDebugString(unlockingIt);
			isDone = false;
			//pointerScanner_.notifyWorkerStarted();
		}

		if (gotValue)
		{
			ReverseScan(valueToFind, startLevel);
		}

		isDone = true;
		//pointerScanner_.notifyWorkerFinished();
	}
}
#include <iostream>
#include <sstream>

//************************************************************ Stack variant
void PointerScanWorker::ReverseScanStackVariant(uint64_t valueToFindRoot, int32_t levelRoot)
{
	if (levelRoot >= maxLevel_)
		return;

	struct Frame
	{
		uint64_t value;
		int32_t level;
	};

	std::vector<Frame> stack;
	stack.reserve(1024); // reserve enough space to avoid reallocs
	stack.push_back({ valueToFindRoot, levelRoot });

	while (!stack.empty())
	{
		auto frame = stack.back();
		stack.pop_back();

		if (frame.level >= maxLevel_)
			continue;

		auto level = frame.level;
		auto valueToFind = frame.value;
		auto differentOffsetsInThisNode = 0;
		int64_t startValue = 0;
		int64_t stopValue = 0;
		bool exactOffset = false;

		if (!exactOffset)
		{
			startValue = valueToFind - structSize_;
			stopValue = valueToFind;

			if (startValue > stopValue)
			{
				startValue = 0;
			}
		}

		// --- NoLoop protection ---
		if (pointerScanner_.NoLoop)
		{
			bool skip = false;
			for (int32_t i = 0; i < frame.level; ++i)
			{
				if (valueList_[i] == valueToFind)
				{
					skip = true;
					break;
				}
			}
			if (skip)
				continue;

			valueList_[frame.level] = valueToFind;
		}

		bool dontGoDeeper = false;
		//PointerList* plist = plist = pointerScanner_.FindPointerValue(startValue, stopValue);
		int32_t currentIndex = pointerScanner_.BinarySearchClosestLowerKey(stopValue, startValue);
		PointerList* plist = pointerScanner_.GetPointerValueByIndex(currentIndex);

		while (stopValue >= startValue)
		{
			if (plist == nullptr)
			{
				//_pathsEvaluated++;
				break;
			}

			tempResults_[level] = valueToFind - stopValue;

			for (size_t j = 0; j < plist->Pos; j++)
			{
				//_pathsEvaluated++;
				if (!plist->List[j].StaticData.HasValue)
				{
					if (!dontGoDeeper)
					{
						if ((level + 1) < maxLevel_)
						{
							bool addedToQueue = false;

							if ((
								(level + 3 < maxLevel_) &&
								(
									((pointerScanner_.PathQueueLength < pointerScanner_.MaxQueueSize - (pointerScanner_.MaxQueueSize / 3))) ||
									((level <= 2) && (pointerScanner_.PathQueueLength < pointerScanner_.MaxQueueSize - (pointerScanner_.MaxQueueSize / 8))) ||
									((level <= 1) && (pointerScanner_.PathQueueLength < pointerScanner_.MaxQueueSize - (pointerScanner_.MaxQueueSize / 16))) ||
									((level == 0) && (pointerScanner_.PathQueueLength < pointerScanner_.MaxQueueSize - 1))
									)
								)
								||
								(pointerScanner_.PathQueueLength == 0))
							{
								auto locked = pointerScanner_.pathqueueCS.try_lock();

								if (!locked && (level <= 2))
									locked = pointerScanner_.pathqueueCS.try_lock();

								if (!locked && (level <= 1))
								{
									//std::this_thread::sleep_for(std::chrono::milliseconds(0));
									std::this_thread::yield();
									//Sleep(0);
									locked = pointerScanner_.pathqueueCS.try_lock();
									if (!locked)
									{
										//std::this_thread::sleep_for(std::chrono::milliseconds(0));
										std::this_thread::yield();
										//Sleep(0);
										locked = pointerScanner_.pathqueueCS.try_lock();
									}
								}

								if (!locked && (level == 0))
								{
									pointerScanner_.pathqueueCS.lock();
									locked = true;
								}

								if (locked)
								{
									if (pointerScanner_.PathQueueLength < pointerScanner_.MaxQueueSize - 1)
									{
										pointerScanner_.PathQueue[pointerScanner_.PathQueueLength]->TempResults = tempResults_;

										if (pointerScanner_.NoLoop)
										{
											pointerScanner_.PathQueue[pointerScanner_.PathQueueLength]->ValueList = valueList_;
										}

										pointerScanner_.PathQueue[pointerScanner_.PathQueueLength]->StartLevel = level + 1;
										pointerScanner_.PathQueue[pointerScanner_.PathQueueLength]->ValueToFind = plist->List[j].Address;
										pointerScanner_.PathQueueLength++;
										addedToQueue = true;
									}

									pointerScanner_.pathqueueCS.unlock();
								}
							}

							if (!addedToQueue)
							{
								//ReverseScan(plist->List[j].Address, level + 1);
								stack.push_back(Frame{ static_cast<uint64_t>(plist->List[j].Address), level + 1 });
							}
						}
					}
				}
				else
				{
					Count++;
				}
			}

			if (true) //check if the current iteration is less than maxOffsetsPerNode 
			{
				if (level > 0)
				{
					differentOffsetsInThisNode++;
				}

				if (differentOffsetsInThisNode >= 3)
				{
					break; //the max node has been reached 
				}
			}

			//get previous
			plist = pointerScanner_.GetPointerValueByIndex(--currentIndex);

			if (plist != nullptr)
			{
				stopValue = plist->PointerValue;
			}
			else
			{
				break; //nothing else to be found 
			}
		}
	}
}

void PointerScanWorker::ReverseScan(uint64_t valueToFind, int32_t level)
{
	if (level >= maxLevel_)
		return;

	auto differentOffsetsInThisNode = 0;
	int64_t startValue = 0;
	int64_t stopValue = 0;
	bool exactOffset = false;

	if (!exactOffset)
	{
		startValue = valueToFind - structSize_;
		stopValue = valueToFind;

		if (startValue > stopValue)
		{
			startValue = 0;
		}
	}

	if (pointerScanner_.NoLoop)
	{
		for (size_t i = 0; i < level; i++)
		{
			if (valueList_[i] == valueToFind)
			{
				return;
			}
		}

		valueList_[level] = valueToFind;
	}

	bool dontGoDeeper = false;
	//PointerList* plist = plist = pointerScanner_.FindPointerValue(startValue, stopValue);
	int32_t currentIndex = pointerScanner_.BinarySearchClosestLowerKey(stopValue, startValue);
	PointerList* plist = pointerScanner_.GetPointerValueByIndex(currentIndex);

	while (stopValue >= startValue)
	{
		if (plist == nullptr)
		{
			//_pathsEvaluated++;
			return;
		}

		tempResults_[level] = valueToFind - stopValue;

		for (size_t j = 0; j < plist->Pos; j++)
		{
			//_pathsEvaluated++;
			if (!plist->List[j].StaticData.HasValue)
			{
				if (!dontGoDeeper)
				{
					if ((level + 1) < maxLevel_)
					{
						bool addedToQueue = false;

						if ((
							(level + 3 < maxLevel_) &&
							(
								((pointerScanner_.PathQueueLength < pointerScanner_.MaxQueueSize - (pointerScanner_.MaxQueueSize / 3))) ||
								((level <= 2) && (pointerScanner_.PathQueueLength < pointerScanner_.MaxQueueSize - (pointerScanner_.MaxQueueSize / 8))) ||
								((level <= 1) && (pointerScanner_.PathQueueLength < pointerScanner_.MaxQueueSize - (pointerScanner_.MaxQueueSize / 16))) ||
								((level == 0) && (pointerScanner_.PathQueueLength < pointerScanner_.MaxQueueSize - 1))
								)
							)
							||
							(pointerScanner_.PathQueueLength == 0))
						{
							auto locked = pointerScanner_.pathqueueCS.try_lock();

							if (!locked && (level <= 2))
								locked = pointerScanner_.pathqueueCS.try_lock();

							if (!locked && (level <= 1))
							{
								//std::this_thread::sleep_for(std::chrono::milliseconds(0));
								Sleep(0);
								locked = pointerScanner_.pathqueueCS.try_lock();
								if (!locked)
								{
									//std::this_thread::sleep_for(std::chrono::milliseconds(0));
									Sleep(0);
									locked = pointerScanner_.pathqueueCS.try_lock();
								}
							}

							if (!locked && (level == 0))
							{
								pointerScanner_.pathqueueCS.lock();
								locked = true;
							}

							if (locked)
							{
								if (pointerScanner_.PathQueueLength < pointerScanner_.MaxQueueSize - 1)
								{
									pointerScanner_.PathQueue[pointerScanner_.PathQueueLength]->TempResults = tempResults_;

									if (pointerScanner_.NoLoop)
									{
										pointerScanner_.PathQueue[pointerScanner_.PathQueueLength]->ValueList = valueList_;
									}

									pointerScanner_.PathQueue[pointerScanner_.PathQueueLength]->StartLevel = level + 1;
									pointerScanner_.PathQueue[pointerScanner_.PathQueueLength]->ValueToFind = plist->List[j].Address;
									pointerScanner_.PathQueueLength++;
									addedToQueue = true;
								}

								pointerScanner_.pathqueueCS.unlock();
							}
						}

						if (!addedToQueue) 
						{
							ReverseScan(plist->List[j].Address, level + 1);
						}
					}
				}
			}
			else
			{
				Count++;
			}
		}

		if (true) //check if the current iteration is less than maxOffsetsPerNode 
		{
			if (level > 0)
			{
				differentOffsetsInThisNode++;
			}

			if (differentOffsetsInThisNode >= 3)
			{
				return; //the max node has been reached 
			}
		}

		//get previous
		plist = pointerScanner_.GetPointerValueByIndex(--currentIndex);

		if (plist != nullptr)
		{
			stopValue = plist->PointerValue;
		}
		else
		{
			return; //nothing else to be found 
		}
	}
}
