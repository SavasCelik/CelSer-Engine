using CelSerEngine.Core.Scripting;
using CelSerEngine.Wpf.Models;
using ICSharpCode.AvalonEdit.CodeCompletion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CelSerEngine.Wpf.Views;
/// <summary>
/// Interaction logic for ScriptEditorWindow.xaml
/// </summary>
public partial class ScriptEditorWindow : Window
{
    [GeneratedRegex("\\bvar\\s+(\\w+)\\s*=")]
    private static partial Regex VariableNameRegex();
    [GeneratedRegex("\\b\\w+\\s+(\\w+)\\s*\\(")]
    private static partial Regex MethodNameRegex();
    private CompletionWindow? _completionWindow;
    private readonly char[] _allowedCharsBeforeCompletion = { ' ', '\n', '\t', '(' };

    public ScriptEditorWindow()
    {
        InitializeComponent();
        textEditor.TextArea.TextEntering += textEditor_TextArea_TextEntering;
        textEditor.TextArea.TextEntered += textEditor_TextArea_TextEntered;
    }

    public string GetText()
    {
        return textEditor.Text;
    }

    public void SetText(string text)
    {
        textEditor.Text = text;
    }

    private void textEditor_TextArea_TextEntered(object sender, TextCompositionEventArgs e)
    {
        IEnumerable<string> definedVariables = GetInTextDefinedVariables();
        IEnumerable<string> definedMethods = GetInTextDefinedMethods();
        IEnumerable<string> preDefinedVariables = GetPreDefinedVariables();
        var foundDefinitions = preDefinedVariables
            .Concat(definedVariables)
            .Concat(definedMethods)
            .Where(x => x.Contains(e.Text, StringComparison.InvariantCultureIgnoreCase))
            .ToArray();

        if (_completionWindow != null && _completionWindow.CompletionList.ListBox.Items.Count <= 0)
            _completionWindow.Close();

        if (_completionWindow != null)
            return;

        var lastWordIndex = Math.Max(textEditor.CaretOffset - 2, 0);

        if (_allowedCharsBeforeCompletion.Contains(textEditor.Text[lastWordIndex]) && foundDefinitions.Any())
        {
            CreateCompletionWindow(foundDefinitions);
            _completionWindow!.StartOffset -= 1;
        }
        else if (e.Text == ".")
        {
            var preDefinedMethods = GetPreDefinedMethods().ToArray();
            CreateCompletionWindow(preDefinedMethods);
        }
        
    }

    private void CreateCompletionWindow(ICollection<string> completionStrings)
    {
        _completionWindow = new CompletionWindow(textEditor.TextArea);
        IList<ICompletionData> data = _completionWindow.CompletionList.CompletionData;

        foreach (var definition in completionStrings)
        {
            data.Add(new EditorCompletionData(definition));
        }

        _completionWindow.Show();
        _completionWindow.Closed += delegate
        {
            _completionWindow = null;
        };
    }

    private IEnumerable<string> GetInTextDefinedVariables()
    {
        // Extract variables from textEditor's text:
        var regex = VariableNameRegex();

        foreach (Match match in regex.Matches(textEditor.Text))
        {
            yield return match.Groups[1].Value;
        }
    }

    private IEnumerable<string> GetInTextDefinedMethods()
    {
        // Extract methods from textEditor's text:
        var regex = MethodNameRegex();

        foreach (Match match in regex.Matches(textEditor.Text))
        {
            yield return match.Groups[1].Value;
        }
    }

    private static IEnumerable<string> GetPreDefinedVariables()
    {
        const string memoryManagerName = nameof(MemoryManager);
        yield return char.ToLowerInvariant(memoryManagerName[0]) + memoryManagerName[1..];
    }

    private static IEnumerable<string> GetPreDefinedMethods()
    {
        var memoryManagerMethods = typeof(MemoryManager).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        foreach (MethodInfo methodInfo in memoryManagerMethods)
        {
            yield return methodInfo.Name;
        }
    }

    private void textEditor_TextArea_TextEntering(object sender, TextCompositionEventArgs e)
    {
        if (e.Text.Length > 0 && _completionWindow != null)
        {
            if (!char.IsLetterOrDigit(e.Text[0]))
            {
                // Whenever a non-letter is typed while the completion window is open,
                // insert the currently selected element.
                _completionWindow.CompletionList.RequestInsertion(e);
            }
        }
        // Do not set e.Handled=true.
        // We still want to insert the character that was typed.
    }
}
