using CelSerEngine.Core.Models;
using CelSerEngine.Core.Scripting.Template;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CelSerEngine.Wpf.Models;

[ObservableObject]
public partial class ObservableScript : BaseScript
{
    [ObservableProperty]
    private bool _isActivated;
    public ILoopingScript? LoopingScript { get; set; }
}
