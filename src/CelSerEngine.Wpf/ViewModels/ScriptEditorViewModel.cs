using CelSerEngine.Core.Database;
using CelSerEngine.Core.Models;
using CelSerEngine.Core.Scripting;
using CelSerEngine.Core.Scripting.Template;
using CelSerEngine.Wpf.Services;
using CelSerEngine.Wpf.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace CelSerEngine.Wpf.ViewModels;

/// <summary>
/// ViewModel for script editing, responsible for handling user interactions and 
/// coordinating updates to the underlying model.
/// </summary>
public partial class ScriptEditorViewModel : ObservableObject
{

    [ObservableProperty]
    private string _scriptLogic;

    private readonly CelSerEngineDbContext _celSerEngineDbContext;
    private readonly IScriptService _scriptService;
    private ScriptEditorWindow? _scriptEditor;

    /// <summary>
    /// Gets or sets the currently selected script.
    /// </summary>
    public IScript? SelectedScript { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptEditorViewModel"/> class.
    /// </summary>
    /// <param name="celSerEngineDbContext">The database context.</param>
    /// <param name="scriptService">The script service.</param>
    public ScriptEditorViewModel(CelSerEngineDbContext celSerEngineDbContext, IScriptService scriptService)
    {
        _celSerEngineDbContext = celSerEngineDbContext;
        _scriptService = scriptService;
        _scriptLogic = "";
    }

    /// <summary>
    /// Saves the current script asynchronously.
    /// </summary>
    [RelayCommand]
    private async Task SaveScriptAsync()
    {
        if (SelectedScript == null)
        {
            MessageBox.Show("There is no script selected", "Script Editor");
            return;
        }

        SelectedScript.Logic = _scriptEditor!.GetText();
        await _scriptService.UpdateScriptAsync(SelectedScript);
        await _celSerEngineDbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Validates the current script.
    /// </summary>
    [RelayCommand]
    private void ValidateScript()
    {
        if (SelectedScript == null)
        {
            MessageBox.Show("There is no script selected", "Script Editor");
            return;
        }

        var logicBefore = SelectedScript.Logic;
        SelectedScript.Logic = _scriptEditor!.GetText();
        try
        {
            Cursor.Current = Cursors.WaitCursor;
            _scriptService.ValidateScript(SelectedScript);
            MessageBox.Show("Validation successful!", "Script Editor");
            Cursor.Current = Cursors.Default;
        }
        catch (ScriptValidationException ex)
        {
            MessageBox.Show(ex.Message, "Script Editor");
        }
        finally
        {
            SelectedScript.Logic = logicBefore;
        }
    }

    /// <summary>
    /// Pastes a basic script template into the editor.
    /// </summary>
    [RelayCommand]
    private void PasteBasicTemplate()
    {
        _scriptEditor!.SetText(ScriptTemplates.BasicTemplate);
    }

    /// <summary>
    /// Opens the script editor for a given script.
    /// </summary>
    /// <param name="selectedScript">The script to edit.</param>
    public void OpenScriptEditor(IScript selectedScript)
    {
        SelectedScript = selectedScript;

        if (_scriptEditor == null || !_scriptEditor.IsVisible)
            _scriptEditor = new ScriptEditorWindow();

        _scriptEditor.SetText(SelectedScript.Logic);
        _scriptEditor.Show();
    }
}
