﻿namespace CelSerEngine.Core.Models;

public class ProcessMemory : IProcessMemory
{
    public IntPtr BaseAddress { get; set; }
    public int BaseOffset { get; set; }
    public IntPtr Address => BaseAddress + BaseOffset;
    public string Value { get; set; }
    public ScanDataType ScanDataType { get; set; } = ScanDataType.Integer;

    public ProcessMemory(IntPtr baseAddress, int baseOffset, string value, ScanDataType scanDataType)
    {
        BaseAddress = baseAddress;
        BaseOffset = baseOffset;
        Value = value;
        ScanDataType = scanDataType;
    }
}
