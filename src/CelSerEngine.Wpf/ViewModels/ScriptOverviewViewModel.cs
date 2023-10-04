using CelSerEngine.Core.Database;
using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using CelSerEngine.Core.Scripting;
using CelSerEngine.Wpf.Models;
using CelSerEngine.Wpf.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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
    private ScriptOverviewWindow? _scriptOverviewWindow;

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
    private void OpenScriptEditor(IScript script)
    {
        _scriptEditorViewModel.OpenScriptEditor(script);
    }

    [RelayCommand]
    private void ShowRenamingDialog(IScript script)
    {
        var valueEditor = new ValueEditor("Name")
        {
            Owner = _scriptOverviewWindow
        };
        valueEditor.SetValueTextBox(script.Name);
        valueEditor.SetFocusTextBox();
        var dialogResult = valueEditor.ShowDialog();

        if (dialogResult ?? false)
        {
            script.Name = valueEditor.Value;
            Script dbScript = _celSerEngineDbContext.Scripts.Single(x => x.Id == script.Id);
            dbScript.Name = script.Name;
        }
    }

    public void OpenScriptOverview()
    {
        Scripts = _celSerEngineDbContext.Scripts.AsNoTracking().Select(x => new ObservableScript(x.Id, x.Name, x.Logic)).ToList();
        _scriptOverviewWindow?.Close();
        _scriptOverviewWindow = new ScriptOverviewWindow();
        _scriptOverviewWindow.Show();
    }

    private void RunActiveScripts(object? sender, EventArgs args)
    {
        ObservableScript[] activeScripts = Scripts.Where(x => x.IsActivated).ToArray();
        var memoryManager = new MemoryManager(_selectProcessViewModel.GetSelectedProcessHandle(), _nativeApi);

        foreach (var script in activeScripts)
        {
            script.LoopingScript ??= _scriptCompiler.CompileScript(script);
            script.LoopingScript.OnLoop(memoryManager);
        }
    }
}
