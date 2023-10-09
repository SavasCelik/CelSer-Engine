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

public partial class ScriptEditorViewModel : ObservableObject
{

    [ObservableProperty]
    private string _scriptLogic;

    private readonly CelSerEngineDbContext _celSerEngineDbContext;
    private readonly IScriptService _scriptService;
    public IScript? SelectedScript { get; set; }
    private ScriptEditorWindow? _scriptEditor;

    public ScriptEditorViewModel(CelSerEngineDbContext celSerEngineDbContext, IScriptService scriptService)
    {
        _celSerEngineDbContext = celSerEngineDbContext;
        _scriptService = scriptService;
        _scriptLogic = "";
    }

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

    [RelayCommand]
    private void PasteBasicTemplate()
    {
        _scriptEditor!.SetText(ScriptTemplates.BasicTemplate);
    }

    public void OpenScriptEditor(IScript selectedScript)
    {
        SelectedScript = selectedScript;

        if (_scriptEditor == null || !_scriptEditor.IsVisible)
            _scriptEditor = new ScriptEditorWindow();

        _scriptEditor.SetText(SelectedScript.Logic);
        _scriptEditor.Show();
    }
}
