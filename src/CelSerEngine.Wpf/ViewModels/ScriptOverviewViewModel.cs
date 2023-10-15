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

/// <summary>
/// ViewModel for the script overview, responsible for managing the collection of scripts,
/// handling user interactions, and coordinating updates to the underlying model.
/// </summary>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptOverviewViewModel"/> class.
    /// </summary>
    /// <param name="selectProcessViewModel">ViewModel for process selection.</param>
    /// <param name="scriptEditorViewModel">ViewModel for the script editor.</param>
    /// <param name="scriptService">Service for script-related operations.</param>
    /// <param name="nativeApi">API for native operations.</param>
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

    /// <summary>
    /// Opens the script editor for the provided script.
    /// </summary>
    /// <param name="script">The script to be edited.</param>
    [RelayCommand]
    private void OpenScriptEditor(IScript script)
    {
        _scriptEditorViewModel.OpenScriptEditor(script);
    }

    /// <summary>
    /// Opens a renaming dialog for the provided script and updates the script name if changed.
    /// </summary>
    /// <param name="script">The script to be renamed.</param>
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

    /// <summary>
    /// Duplicates the provided script.
    /// </summary>
    /// <param name="script">The script to be duplicated.</param>
    [RelayCommand]
    private async Task DuplicateScriptAsync(IScript script)
    {
        IScript duplicatedScript = await _scriptService.DuplicateScriptAsync(script);
        Scripts.Add(new ObservableScript(duplicatedScript.Id, duplicatedScript.Name, duplicatedScript.Logic));
    }

    /// <summary>
    /// Deletes the selected script after confirmation.
    /// </summary>
    /// <param name="script">The script to be deleted.</param>
    [RelayCommand]
    private async Task DeleteScriptAsync(ObservableScript script)
    {
        var confirmDeletion = MessageBox.Show($"\"{script.Name}\" will be deleted permanently.", "Deleting Script",
            MessageBoxButton.OKCancel, MessageBoxImage.Warning);
        if (confirmDeletion == MessageBoxResult.OK)
        {
            Scripts.Remove(script);
            await _scriptService.DeleteScriptAsync(script);
        }
    }

    /// <summary>
    /// Exports the provided script to a selected file.
    /// </summary>
    /// <param name="script">The script to be exported.</param>
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

    /// <summary>
    /// Imports a script from a selected file.
    /// </summary>
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

    /// <summary>
    /// Creates a new script for a selected process.
    /// </summary>
    [RelayCommand]
    private async Task CreateNewScriptAsync()
    {
        var targetProcessName = GetTargetProcessName();

        if (targetProcessName == null)
            return;

        var newScript = new Script();
        await _scriptService.InsertScriptAsync(newScript, targetProcessName);
        var observableScript = new ObservableScript(newScript.Id, newScript.Name, newScript.Logic);
        Scripts.Add(observableScript);
        OpenScriptEditor(observableScript);
    }
    /// <summary>
    /// Opens the script overview for a selected process.
    /// </summary> 
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
                DeactivateScripts();
                StopDeactivatedScripts();
                _scriptOverviewWindow = null;
                _timer.Stop();
                _memoryManager = null;
            };
        }

        _scriptOverviewWindow.Focus();
    }

    /// <summary>
    /// Deactivates all scripts in the overview.
    /// </summary>
    private void DeactivateScripts()
    {
        foreach (ObservableScript observableScript in Scripts)
            observableScript.IsActivated = false;
    }

    /// <summary>
    /// Runs all activated scripts.
    /// </summary>
    private void RunActivatedScripts(object? sender = null, EventArgs? args = null)
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
            catch (Exception ex)
            {
                ShowMessageBoxForException(ex, script);
                script.IsActivated = false;
            }
        }
    }

    /// <summary>
    /// Stops all deactivated scripts.
    /// </summary>
    private void StopDeactivatedScripts(object? sender = null, EventArgs? args = null)
    {
        if (_memoryManager == null)
            return;

        var deactivatedScripts = Scripts.Where(x => !x.IsActivated && x.State == ScriptState.Started).ToArray();

        foreach (var script in deactivatedScripts)
        {
            try
            {
                _scriptService.StopScript(script, _memoryManager!);
            }
            catch (Exception ex)
            {
                ShowMessageBoxForException(ex, script);
                script.IsActivated = true;
            }
        }
    }

    /// <summary>
    /// Retrieves the name of the target process.
    /// </summary>
    /// <returns>Name of the selected process or null if no process is selected.</returns>
    private string? GetTargetProcessName()
    {
        ProcessAdapter? selectedProcess = _selectProcessViewModel.SelectedProcess;

        if (selectedProcess == null)
            MessageBox.Show("Please select a process.", "No Process selected", MessageBoxButton.OK, MessageBoxImage.Warning);

        return _selectProcessViewModel.SelectedProcess?.Process.ProcessName;
    }

    /// <summary>
    /// Displays a message box with relevant information about an exception that occurred during script execution.
    /// </summary>
    /// <param name="ex">The exception that occurred.</param>
    /// <param name="script">The script associated with the exception.</param>
    private static void ShowMessageBoxForException(Exception ex, IScript script)
    {
        var message = ex is ScriptValidationException
            ? $"Validation for script \"{script.Name}\" failed!\n\n{ex.Message}"
            : $"RunTimeException occurred for script \"{script.Name}\" failed!\n\n{ex.Message}\n{ex.StackTrace}";

        MessageBox.Show(message, "Script Overview");
    }
}
