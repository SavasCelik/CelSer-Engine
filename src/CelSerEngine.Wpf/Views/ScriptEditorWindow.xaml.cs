using CelSerEngine.Core.Scripting;
using CelSerEngine.Wpf.AvalonEdit;
using CelSerEngine.Wpf.Models;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Resources;
using System.Xml;

namespace CelSerEngine.Wpf.Views;
/// <summary>
/// Interaction logic for ScriptEditorWindow.xaml
/// </summary>
public partial class ScriptEditorWindow : Window
{
    [GeneratedRegex("\\bvar\\s+(\\w+)\\s*=")]
    private static partial Regex VariableNameRegex();
    [GeneratedRegex("\\b(?!new)\\w+\\s+(\\w+)\\s*\\(")]
    private static partial Regex MethodNameRegex();
    private CompletionWindow? _completionWindow;
    private readonly char[] _allowedCharsBeforeCompletion = { ' ', '\n', '\t', '(' };
    private readonly FoldingManager _foldingManager;
    private readonly BraceFoldingStrategy _braceFoldingStrategy;

    public ScriptEditorWindow()
    {
        InitializeComponent();
        LoadHighlightingRules();
        textEditor.TextArea.TextEntering += textEditor_TextArea_TextEntering;
        textEditor.TextArea.TextEntered += textEditor_TextArea_TextEntered;
        textEditor.ShowLineNumbers = true;
        _foldingManager = FoldingManager.Install(textEditor.TextArea);
        _braceFoldingStrategy = new BraceFoldingStrategy();
    }

    private void LoadHighlightingRules()
    {
        StreamResourceInfo? highlightingResourceStream = Application.GetResourceStream(new Uri("/Resources/CsharpSyntaxStyle.xshd", UriKind.Relative));

        if (highlightingResourceStream == null)
        {
            Console.WriteLine("Unable to load code highlighting rules. Scripts will be affected");
            return;
        }

        using var reader = new XmlTextReader(highlightingResourceStream.Stream);
        textEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }

    public string GetText()
    {
        return textEditor.Text;
    }

    public void SetText(string text)
    {
        textEditor.Text = text;
        _braceFoldingStrategy.UpdateFoldings(_foldingManager, textEditor.Document);
    }

    private void textEditor_TextArea_TextEntered(object sender, TextCompositionEventArgs e)
    {
        if (e.Text == "}")
            _braceFoldingStrategy.UpdateFoldings(_foldingManager, textEditor.Document);

        if (_completionWindow != null && _completionWindow.CompletionList.ListBox.Items.Count <= 0)
            _completionWindow.Close();

        if (_completionWindow != null)
            return;

        IEnumerable<EditorCompletionData> definedVariables = GetInTextDefinedVariables();
        IEnumerable<EditorCompletionData> definedMethods = GetInTextDefinedMethods();
        IEnumerable<EditorCompletionData> preDefinedVariables = GetPreDefinedVariables();
        var foundDefinitions = preDefinedVariables
            .Concat(definedVariables)
            .Concat(definedMethods)
            .Where(x => x.Text.Contains(e.Text, StringComparison.InvariantCultureIgnoreCase))
            .ToArray();
        var lastWordIndex = Math.Max(textEditor.CaretOffset - 2, 0);

        if (_allowedCharsBeforeCompletion.Contains(textEditor.Text[lastWordIndex]) && foundDefinitions.Any())
        {
            CreateCompletionWindow(foundDefinitions);
            _completionWindow!.StartOffset -= 1;
        }
        else if (e.Text == ".")
        {
            var instanceNameLength = Math.Min(nameof(MemoryManager).Length, textEditor.Text.Length);
            var instanceNameStartIndex = Math.Max(textEditor.CaretOffset - 1 - instanceNameLength, 0);
            var instanceName = textEditor.Text.Substring(instanceNameStartIndex, instanceNameLength);

            if (!nameof(MemoryManager).Equals(instanceName, StringComparison.InvariantCultureIgnoreCase))
                return;

            var preDefinedMethods = GetPreDefinedMethods().ToArray();
            CreateCompletionWindow(preDefinedMethods);
        }
        
    }

    private void CreateCompletionWindow(IEnumerable<EditorCompletionData> editorCompletions)
    {
        _completionWindow = new CompletionWindow(textEditor.TextArea);
        IList<ICompletionData> data = _completionWindow.CompletionList.CompletionData;

        foreach (var completionData in editorCompletions)
        {
            data.Add(completionData);
        }

        _completionWindow.Show();
        _completionWindow.Closed += delegate
        {
            _completionWindow = null;
        };
    }

    private IEnumerable<EditorCompletionData> GetInTextDefinedVariables()
    {
        // Extract variables from textEditor's text:
        var regex = VariableNameRegex();

        foreach (Match match in regex.Matches(textEditor.Text))
        {
            yield return new EditorCompletionData(match.Groups[1].Value, "Variable specified in this document.");
        }
    }

    private IEnumerable<EditorCompletionData> GetInTextDefinedMethods()
    {
        // Extract methods from textEditor's text:
        var regex = MethodNameRegex();

        foreach (Match match in regex.Matches(textEditor.Text))
        {
            yield return new EditorCompletionData(match.Groups[1].Value, "Method specified in this document.");
        }
    }

    private static IEnumerable<EditorCompletionData> GetPreDefinedVariables()
    {
        const string memoryManagerName = nameof(MemoryManager);
        yield return new EditorCompletionData(char.ToLowerInvariant(memoryManagerName[0]) + memoryManagerName[1..],
            "This class provides functionality for reading and writing to a process's memory.");
    }

    private static IEnumerable<EditorCompletionData> GetPreDefinedMethods()
    {
        yield return new EditorCompletionData(nameof(MemoryManager.ReadMemoryAt),
            "Reads the memory at the given address and returns it.");
        yield return new EditorCompletionData(nameof(MemoryManager.WriteMemoryAt),
            "Writes the specified value to the memory at the given address.");
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
