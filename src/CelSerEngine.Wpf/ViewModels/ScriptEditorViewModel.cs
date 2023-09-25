using CelSerEngine.Core.Database;
using CelSerEngine.Core.Models;
using CelSerEngine.Core.Scripting.Template;
using CelSerEngine.Wpf.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ICSharpCode.AvalonEdit.Document;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelSerEngine.Wpf.ViewModels;

public partial class ScriptEditorViewModel : ObservableObject
{
    private readonly CelSerEngineDbContext _celSerEngineDbContext;
    public BaseScript? SelectedScript { get; set; }
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

    }

    [RelayCommand]
    private void PasteBasicTemplate()
    {

        _scriptEditor.SetText(ScriptTemplates.BasicTemplate);
    }

    public void OpenScriptEditor(BaseScript selectedScript)
    {
        SelectedScript = selectedScript;
        ScriptLogic = SelectedScript.Logic;

        if (_scriptEditor == null || !_scriptEditor.IsVisible)
            _scriptEditor = new ScriptEditorWindow();
        _scriptEditor.Show();
    }
}
