#include "pch.h"

class CPointer {
public:
	DWORD Address;
	DWORD PointingTo;
	DWORD Offsets[5];

	CPointer() : Offsets{ -1, -1, -1, -1, -1 } {
		Address = 0;
		PointingTo = 0;
	}
};