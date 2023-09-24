using CelSerEngine.Core.Database;
using CelSerEngine.Wpf.Models;
using CelSerEngine.Wpf.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelSerEngine.Wpf.ViewModels;
public partial class ScriptOverviewViewModel : ObservableObject
{
    [ObservableProperty]
    private IList<ObservableScript> _scripts;

    private readonly SelectProcessViewModel _selectProcessViewModel;
    private readonly CelSerEngineDbContext _celSerEngineDbContext;

    public ScriptOverviewViewModel(SelectProcessViewModel selectProcessViewModel, CelSerEngineDbContext celSerEngineDbContext)
    {
        _selectProcessViewModel = selectProcessViewModel;
        _celSerEngineDbContext = celSerEngineDbContext;
        _scripts = new List<ObservableScript>();
    }

    [RelayCommand]
    public void OpenScriptEditor(ObservableScript script)
    {
        var scriptEditor = new ScriptEditorWindow();
        scriptEditor.SetText(script.Logic);
        scriptEditor.Show();
    }

    public void OpenScriptOverview()
    {
        Scripts = _celSerEngineDbContext.Scripts.Select(x => new ObservableScript
        {
            Id = x.Id,
            Name = x.Name,
            Logic = x.Logic
        }).ToList();
        var scriptOverviewWindow = new ScriptOverviewWindow();
        scriptOverviewWindow.Show();
    }
}
