namespace CelSerEngine.Core.Models;

public class ScanConstraint
{
    public string UserInput { get; set; }
    public ScanDataType ScanDataType { get; set; }
    public ScanCompareType ScanCompareType { get; set; }
    public MemoryProtections IncludedProtections { get; set; }
    public MemoryProtections ExcludedProtections { get; set; }
    public ICollection<Native.Enums.MEMORY_TYPE> AllowedMemoryTypes { get; set; }
    public IntPtr StartAddress { get; set; }
    public IntPtr StopAddress { get; set; }

    public ScanConstraint(ScanCompareType scanCompareType, ScanDataType dataType, string userInput)
    {
        ScanDataType = dataType;
        ScanCompareType = scanCompareType;
        UserInput = userInput;
        AllowedMemoryTypes = [];
    }
}
