namespace CelSerEngine.Core.Models;

public class ScanConstraint
{
    public string UserInput { get; set; }
    public ScanDataType ScanDataType { get; set; }
    public ScanCompareType ScanCompareType { get; set; }

    public ScanConstraint(ScanCompareType scanCompareType, ScanDataType dataType, string userInput)
    {
        ScanDataType = dataType;
        ScanCompareType = scanCompareType;
        UserInput = userInput;
    }
}
