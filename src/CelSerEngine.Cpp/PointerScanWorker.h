#pragma once
#include <cstdint>
#include <vector>
#include "PointerScanner.h"

class PointerScanner; // Forward declaration

struct ResultPointer
{
	int32_t ModuleIndex;
	int64_t Offset;
	uintptr_t* TempResults;
	int32_t Level;
};

class PointerScanWorker
{
public:
	std::vector<ResultPointer> ScanResults;
	int32_t Count = 0;
	bool isDone = false;
	//std::atomic<bool> isDoneAt { false };
	bool Terminated = false;
private:
	PointerScanner& pointerScanner_;
	// uint32_t* tempResults_;
	// uintptr_t* valueList_;
	std::vector<uint32_t> tempResults_;
	std::vector<uint64_t> valueList_;
	int32_t maxLevel_;
	int32_t structSize_;

public:
	PointerScanWorker(PointerScanner& pointerScanner, int64_t valueToFind, int32_t maxLevel, int32_t structSize) 
		: 
		pointerScanner_(pointerScanner), 
		maxLevel_(maxLevel), 
		structSize_(structSize)
	{};
	void StartScan();
	void ReverseScan(uint64_t valueToFind, int32_t level);
	void ReverseScanStackVariant(uint64_t valueToFindRoot, int32_t levelRoot);
};

