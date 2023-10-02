using CelSerEngine.Core.Database;
using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using CelSerEngine.Core.Scripting;
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
using System.Windows.Threading;

namespace CelSerEngine.Wpf.ViewModels;
public partial class ScriptOverviewViewModel : ObservableObject
{
    [ObservableProperty]
    private IList<ObservableScript> _scripts;

    private readonly SelectProcessViewModel _selectProcessViewModel;
    private readonly CelSerEngineDbContext _celSerEngineDbContext;
    private readonly ScriptEditorViewModel _scriptEditorViewModel;
    private readonly INativeApi _nativeApi;
    private readonly DispatcherTimer _timer;
    private readonly ScriptCompiler _scriptCompiler;

    public ScriptOverviewViewModel(SelectProcessViewModel selectProcessViewModel,
        CelSerEngineDbContext celSerEngineDbContext,
        ScriptEditorViewModel scriptEditorViewModel,
        INativeApi nativeApi)
    {
        _selectProcessViewModel = selectProcessViewModel;
        _celSerEngineDbContext = celSerEngineDbContext;
        _scriptEditorViewModel = scriptEditorViewModel;
        _nativeApi = nativeApi;
        _scripts = new List<ObservableScript>();
        _scriptCompiler = new ScriptCompiler();
        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(0.5)
        };
        _timer.Tick += RunActiveScripts;
        _timer.Start();
    }



    [RelayCommand]
    public void OpenScriptEditor(ObservableScript script)
    {
        _scriptEditorViewModel.OpenScriptEditor(script);
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

    private void RunActiveScripts(object? sender, EventArgs args)
    {
        var activeScripts = Scripts.Where(x => x.IsActivated).ToArray();
        var memoryManager = new MemoryManager(_selectProcessViewModel.GetSelectedProcessHandle(), _nativeApi);

        foreach (var script in activeScripts)
        {
            var loopingScript = _scriptCompiler.CompileScript(script);
            loopingScript.OnLoop(memoryManager);
        }
    }
}
