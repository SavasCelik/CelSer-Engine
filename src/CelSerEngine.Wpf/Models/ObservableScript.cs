using CelSerEngine.Core.Models;
using CelSerEngine.Core.Scripting.Template;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CelSerEngine.Wpf.Models;

public partial class ObservableScript : ObservableObject, IScript
{
    public int Id { get; set; }

    private string _logic;

    public string Logic
    {
        get => _logic;
        set
        {
            _logic = value;
            ScriptState = ScriptState.NotValidated;
            LoopingScript = null;
        }
    }

    [ObservableProperty]
    private bool _isActivated;
    [ObservableProperty]
    private string _name;
    public ILoopingScript? LoopingScript { get; set; }
    public ScriptState ScriptState { get; set; }

    public ObservableScript(int id, string name, string logic)
    {
        Id = id;
        _name = name;
        _logic = logic;
        ScriptState = ScriptState.NotValidated;
    }

}
