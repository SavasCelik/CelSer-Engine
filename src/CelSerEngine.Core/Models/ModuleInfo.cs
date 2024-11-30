namespace CelSerEngine.Core.Models;

public class ModuleInfo
{
    public required string Name { get; set; }
    public IntPtr BaseAddress { get; set; }
    public uint Size { get; set; }
    public int ModuleIndex { get; set; }

    private string? _shortName;

    public string ShortName
    {
        get
        {
            if (_shortName != null) 
                return _shortName;

            var name = Name;
            var index = name.LastIndexOf('\\');

            if (index != -1)
                name = name.Substring(index + 1);

            _shortName = name;
            return _shortName;
        }
    }

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
