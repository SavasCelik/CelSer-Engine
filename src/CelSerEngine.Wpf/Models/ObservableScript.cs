using CelSerEngine.Core.Models;
using CelSerEngine.Core.Scripting.Template;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CelSerEngine.Wpf.Models;

/// <summary>
/// This class is an extension of the <see cref="ObservableObject"/> class, which implements the y<see cref="IScript"/> interface.
/// It represents a script while also providing observable capabilities to monitor and react to property changes.
/// </summary>
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

    /// <summary>
    /// Holds the compiled version of the script logic.
    /// </summary>
    public ILoopingScript? LoopingScript { get; set; }

    /// <summary>
    /// Represents the current state of the script logic
    /// </summary>
    public ScriptState ScriptState { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableScript"/> class with the provided identifier, name, and logic. 
    /// </summary>
    /// <param name="id">The unique identifier of the script.</param>
    /// <param name="name">The name of the script.</param>
    /// <param name="logic">The logic of the script.</param>
    public ObservableScript(int id, string name, string logic)
    {
        Id = id;
        _name = name;
        _logic = logic;
        ScriptState = ScriptState.NotValidated;
    }
}
