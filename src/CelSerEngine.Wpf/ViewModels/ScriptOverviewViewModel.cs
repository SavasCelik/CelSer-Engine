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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace CelSerEngine.Wpf.ViewModels;
public partial class ScriptOverviewViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<IScript> _scripts;

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
        _scripts = new ObservableCollection<IScript>();
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
    private async Task ShowRenamingDialogAsync(IScript script)
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
            Script dbScript = await _celSerEngineDbContext.Scripts.SingleAsync(x => x.Id == script.Id);
            dbScript.Name = script.Name;
            await _celSerEngineDbContext.SaveChangesAsync();
        }
    }

    [RelayCommand]
    private async Task DuplicateScriptAsync(IScript script)
    {
        Script dbScript = await _celSerEngineDbContext.Scripts.SingleAsync(x => x.Id == script.Id);
        var duplicatedScript = new Script
        {
            Id = 0,
            Name = dbScript.Name,
            Logic = dbScript.Logic,
            TargetProcessId = dbScript.TargetProcessId
        };
        await _celSerEngineDbContext.Scripts.AddAsync(duplicatedScript);
        await _celSerEngineDbContext.SaveChangesAsync();
        Scripts.Add(new ObservableScript(duplicatedScript.Id, duplicatedScript.Name, duplicatedScript.Logic));
    }

    [RelayCommand]
    private async Task DeleteScriptAsync(IScript script)
    {
        var confirmDeletion = MessageBox.Show($"\"{script.Name}\" will be deleted permanently.", "Deleting Script",
            MessageBoxButton.OKCancel, MessageBoxImage.Warning);
        if (confirmDeletion == MessageBoxResult.OK)
        {
            Scripts.Remove(script);
            var dbScript = await _celSerEngineDbContext.Scripts.SingleAsync(x => x.Id == script.Id);
            _celSerEngineDbContext.Scripts.Remove(dbScript);
            await _celSerEngineDbContext.SaveChangesAsync();
        }
    }

    [RelayCommand]
    private async Task CreateNewScriptAsync()
    {
        ProcessAdapter? selectedProcess = _selectProcessViewModel.SelectedProcess;

        if (selectedProcess == null)
        {
            MessageBox.Show("Please select a process before create a script.", "No Process selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        TargetProcess? targetProcess = await _celSerEngineDbContext.TargetProcesses.SingleOrDefaultAsync(x => x.Name == selectedProcess.Process.ProcessName);
        var newScript = new Script();

        if (targetProcess == null)
        {
            targetProcess = new TargetProcess
            {
                Name = selectedProcess.Process.ProcessName
            };
            newScript.TargetProcess = targetProcess;
        }
        else
        {
            newScript.TargetProcessId = targetProcess.Id;
        }

        await _celSerEngineDbContext.Scripts.AddAsync(newScript);
        await _celSerEngineDbContext.SaveChangesAsync();
        Scripts.Add(new ObservableScript(newScript.Id, newScript.Name, newScript.Logic));
    }

    public async Task OpenScriptOverviewAsync()
    {
        var targetProcessName = _selectProcessViewModel.SelectedProcess?.Process.ProcessName;
        Scripts.Clear();

        if (targetProcessName != null)
        {
            IList<ObservableScript> dbScripts = await _celSerEngineDbContext.Scripts
                .AsNoTracking()
                .Include(x => x.TargetProcess)
                .Where(x => x.TargetProcess != null && x.TargetProcess.Name == targetProcessName)
                .Select(x => new ObservableScript(x.Id, x.Name, x.Logic))
                .ToListAsync();
            Scripts = new ObservableCollection<IScript>(dbScripts);
        }

        if (_scriptOverviewWindow == null)
        {
            _scriptOverviewWindow = new ScriptOverviewWindow();
            _scriptOverviewWindow.Show();
            _scriptOverviewWindow.Closed += delegate
            {
                _scriptOverviewWindow = null;
            };
        }

        _scriptOverviewWindow.Focus();
    }

    private void RunActiveScripts(object? sender, EventArgs args)
    {
        var activeScripts = Scripts.OfType<ObservableScript>().Where(x => x.IsActivated).ToArray();
        var memoryManager = new MemoryManager(_selectProcessViewModel.GetSelectedProcessHandle(), _nativeApi);

        foreach (var script in activeScripts)
        {
            script.LoopingScript ??= _scriptCompiler.CompileScript(script);
            script.LoopingScript.OnLoop(memoryManager);
        }
    }
}
