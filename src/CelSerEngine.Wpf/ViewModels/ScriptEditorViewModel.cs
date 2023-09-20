using CelSerEngine.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using ICSharpCode.AvalonEdit.Document;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelSerEngine.Wpf.ViewModels;

public partial class ScriptEditorViewModel : ObservableObject
{
    public BaseScript? SelectedScript { get; set; }

    [ObservableProperty]
    private string _scriptLogic;

    [ObservableProperty]
    public TextDocument _myDocument;

    public ScriptEditorViewModel()
    {
        _scriptLogic = "";
    }
}
