using CelSerEngine.Core.Database;
using CelSerEngine.Core.Models;
using CelSerEngine.Core.Scripting;
using CelSerEngine.Core.Scripting.Template;
using CelSerEngine.Wpf.Models;
using CelSerEngine.Wpf.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace CelSerEngine.Wpf.ViewModels;

public partial class ScriptEditorViewModel : ObservableObject
{
    private readonly CelSerEngineDbContext _celSerEngineDbContext;
    public IScript? SelectedScript { get; set; }
    private ScriptEditorWindow? _scriptEditor;

    [ObservableProperty]
    private string _scriptLogic;

    [ObservableProperty]
    public TextDocument _myDocument;

    public ScriptEditorViewModel(CelSerEngineDbContext celSerEngineDbContext)
    {
        _celSerEngineDbContext = celSerEngineDbContext;
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
        ((ObservableScript)SelectedScript).LoopingScript = null;
        if (SelectedScript.Id == 0)
        {
            await _celSerEngineDbContext.AddAsync(SelectedScript);
        }
        else
        {
            var dbScript = await _celSerEngineDbContext.Scripts.Where(x => x.Id == SelectedScript.Id).FirstAsync();
            dbScript.Logic = SelectedScript.Logic;
        }

        await _celSerEngineDbContext.SaveChangesAsync();
    }

    [RelayCommand]
    private async Task ValidateScriptAsync()
    {
        if (SelectedScript == null)
        {
            MessageBox.Show("There is no script selected", "Script Editor");
            return;
        }

        var logicBefore = SelectedScript.Logic;
        SelectedScript.Logic = _scriptEditor!.GetText();
        var scriptCompiler = new ScriptCompiler();
        try
        {
            Cursor.Current = Cursors.WaitCursor;
            scriptCompiler.CompileScript(SelectedScript);
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
