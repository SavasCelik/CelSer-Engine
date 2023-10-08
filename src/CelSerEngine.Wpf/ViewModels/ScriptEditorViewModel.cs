using CelSerEngine.Core.Database;
using CelSerEngine.Core.Models;
using CelSerEngine.Core.Scripting.Template;
using CelSerEngine.Wpf.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

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
    private async Task SaveScript()
    {
        if (SelectedScript == null)
        {
            MessageBox.Show("There is no script selected", "Fatal error!");
            return;
        }

        SelectedScript.Logic = _scriptEditor!.GetText();
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
