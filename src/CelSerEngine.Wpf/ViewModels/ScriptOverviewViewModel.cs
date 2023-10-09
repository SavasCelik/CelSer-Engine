using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using CelSerEngine.Core.Scripting;
using CelSerEngine.Wpf.Models;
using CelSerEngine.Wpf.Services;
using CelSerEngine.Wpf.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private ObservableCollection<ObservableScript> _scripts;

    private readonly SelectProcessViewModel _selectProcessViewModel;
    private readonly ScriptEditorViewModel _scriptEditorViewModel;
    private readonly IScriptService _scriptService;
    private readonly INativeApi _nativeApi;
    private ScriptOverviewWindow? _scriptOverviewWindow;
    private MemoryManager? _memoryManager;
    private readonly DispatcherTimer _timer;

    public ScriptOverviewViewModel(SelectProcessViewModel selectProcessViewModel,
        ScriptEditorViewModel scriptEditorViewModel,
        IScriptService scriptService,
        INativeApi nativeApi)
    {
        _selectProcessViewModel = selectProcessViewModel;
        _scriptEditorViewModel = scriptEditorViewModel;
        _scriptService = scriptService;
        _nativeApi = nativeApi;
        _scripts = new ObservableCollection<ObservableScript>();
        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(0.5)
        };
        _timer.Tick += RunActivatedScripts;
        _timer.Tick += StopDeactivatedScripts;
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

        if (dialogResult == true)
        {
            script.Name = valueEditor.Value;
            await _scriptService.UpdateScriptAsync(script);
        }
    }

    [RelayCommand]
    private async Task DuplicateScriptAsync(IScript selectedScript)
    {
        var targetProcessName = GetTargetProcessName();

        if (targetProcessName == null)
            return;

        var duplicatedScript = new Script
        {
            Id = 0,
            Name = selectedScript.Name,
            Logic = selectedScript.Logic
        };
        await _scriptService.InsertScriptAsync(duplicatedScript, targetProcessName);
        Scripts.Add(new ObservableScript(duplicatedScript.Id, duplicatedScript.Name, duplicatedScript.Logic));
    }

    [RelayCommand]
    private async Task DeleteScriptAsync(ObservableScript selectedScript)
    {
        var confirmDeletion = MessageBox.Show($"\"{selectedScript.Name}\" will be deleted permanently.", "Deleting Script",
            MessageBoxButton.OKCancel, MessageBoxImage.Warning);
        if (confirmDeletion == MessageBoxResult.OK)
        {
            Scripts.Remove(selectedScript);
            await _scriptService.DeleteScriptAsync(selectedScript);
        }
    }

    [RelayCommand]
    private async Task ExportScriptAsync(IScript script)
    {
        var saveDialog = new Microsoft.Win32.SaveFileDialog {
            DefaultExt = ".json", // Default file extension
            Filter = "JSON|*.json" // Filter files by extension
        };
        var result = saveDialog.ShowDialog();

        if (result == true)
            await _scriptService.ExportScriptAsync(script, saveDialog.FileName);
    }

    [RelayCommand]
    private async Task ImportScriptAsync()
    {
        var targetProcessName = GetTargetProcessName();

        if (targetProcessName == null)
            return;

        var openDialog = new Microsoft.Win32.OpenFileDialog {
            DefaultExt = ".json", // Default file extension
            Filter = "JSON|*.json" // Filter files by extension
        };
        var result = openDialog.ShowDialog();

        if (result == true)
        {
            IScript importedScript = await _scriptService.ImportScriptAsync(openDialog.FileName, targetProcessName);
            Scripts.Add(new ObservableScript(importedScript.Id, importedScript.Name, importedScript.Logic));
        }
    }

    [RelayCommand]
    private async Task CreateNewScriptAsync()
    {
        var targetProcessName = GetTargetProcessName();

        if (targetProcessName == null)
            return;

        var newScript = new Script();
        await _scriptService.InsertScriptAsync(newScript, targetProcessName);
        Scripts.Add(new ObservableScript(newScript.Id, newScript.Name, newScript.Logic));
    }

    public async Task OpenScriptOverviewAsync()
    {
        var targetProcessName = GetTargetProcessName();

        if (targetProcessName == null)
            return;

        Scripts.Clear();

        IList<Script> dbScripts = await _scriptService.GetScriptsByTargetProcessNameAsync(targetProcessName);
        var observableScripts = dbScripts.Select(x => new ObservableScript(x.Id, x.Name, x.Logic));
        Scripts = new ObservableCollection<ObservableScript>(observableScripts);

        if (_scriptOverviewWindow == null)
        {
            _scriptOverviewWindow = new ScriptOverviewWindow();
            _memoryManager = new MemoryManager(_nativeApi.OpenProcess(targetProcessName), _nativeApi);
            _scriptOverviewWindow.Show();
            _timer.Start();
            _scriptOverviewWindow.Closed += delegate
            {
                _scriptOverviewWindow = null;
                _timer.Stop();
                _memoryManager = null;
            };
        }

        _scriptOverviewWindow.Focus();
    }

    private void RunActivatedScripts(object? sender, EventArgs args)
    {
        if (_memoryManager == null)
            return;

        var activatedScripts = Scripts.Where(x => x.IsActivated).ToArray();

        foreach (var script in activatedScripts)
        {
            try
            {
                _scriptService.RunScript(script, _memoryManager!);
            }
            catch (ScriptValidationException ex)
            {
                MessageBox.Show(ex.Message, "Script Overview");
                script.IsActivated = false;
            }
        }
    }

    private void StopDeactivatedScripts(object? sender, EventArgs args)
    {
        if (_memoryManager == null)
            return;

        var deactivatedScripts = Scripts.Where(x => !x.IsActivated && x.ScriptState != ScriptState.Stopped).ToArray();

        foreach (var script in deactivatedScripts)
        {
            _scriptService.StopScript(script, _memoryManager!);
        }
    }

    private string? GetTargetProcessName()
    {
        ProcessAdapter? selectedProcess = _selectProcessViewModel.SelectedProcess;

        if (selectedProcess == null)
            MessageBox.Show("Please select a process before create a script.", "No Process selected", MessageBoxButton.OK, MessageBoxImage.Warning);

        return _selectProcessViewModel.SelectedProcess?.Process.ProcessName;
    }
}
