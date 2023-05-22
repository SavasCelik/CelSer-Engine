using CelSerEngine.Core.Models;
using CelSerEngine.Models.ObservableModels;

namespace CelSerEngine.Models;

public partial class ValueAddress : ObservableProcessMemory
{
    public dynamic? PrevoiusValue { get; set; }

    public ValueAddress(ulong baseAddress, int baseOffset, dynamic value, ScanDataType scanDataType) : base(baseAddress, baseOffset, 0, scanDataType)
    {
        AddressDisplayString = Address.ToString("X");
    }
}