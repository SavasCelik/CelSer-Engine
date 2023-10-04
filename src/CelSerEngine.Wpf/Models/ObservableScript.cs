using CelSerEngine.Core.Models;
using CelSerEngine.Core.Scripting.Template;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CelSerEngine.Wpf.Models;

public partial class ObservableScript : ObservableObject, IScript
{
    public int Id { get; set; }
    public string Logic { get; set; }
    [ObservableProperty]
    private bool _isActivated;
    [ObservableProperty]
    private string _name;
    public ILoopingScript? LoopingScript { get; set; }

    public ObservableScript(int id, string name, string logic)
    {
        Id = id;
        _name = name;
        Logic = logic;
    }

}
