namespace CelSerEngine.Core.Models;

public class ModuleInfo
{
    public required string Name { get; set; }
    public IntPtr BaseAddress { get; set; }
    public uint Size { get; set; }
    public int ModuleIndex { get; set; }

    private bool? _isSystemModule;
    public bool IsSystemModule 
    {
        get
        {
            if (_isSystemModule == null)
                _isSystemModule = Name.ToLower().Contains("windows\\");

            return _isSystemModule.Value;
        }
    }
}
