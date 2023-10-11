using CelSerEngine.Core.Scripting;
using CelSerEngine.Wpf.AvalonEdit;
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
using System.Windows.Media;
using System.Windows.Resources;
using System.Xml;

namespace CelSerEngine.Wpf.Views;
/// <summary>
/// Interaction logic for ScriptEditorWindow.xaml
/// Represents a window for editing scripts with auto-completion and syntax highlighting features.
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

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptEditorWindow"/> class.
    /// </summary>
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

    /// <summary>
    /// Loads syntax highlighting rules for the text editor.
    /// </summary>
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

    /// <summary>
    /// Gets the text currently present in the text editor.
    /// </summary>
    /// <returns>The script text.</returns>
    public string GetText()
    {
        return textEditor.Text;
    }

    /// <summary>
    /// Sets the text in the text editor and updates code foldings.
    /// </summary>
    /// <param name="text">The script text.</param>
    public void SetText(string text)
    {
        textEditor.Text = text;
        _braceFoldingStrategy.UpdateFoldings(_foldingManager, textEditor.Document);
    }

    /// <summary>
    /// Handles the TextEntered event of the text editor to provide auto-completion suggestions.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments.</param>
    private void textEditor_TextArea_TextEntered(object sender, TextCompositionEventArgs e)
    {
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

    /// <summary>
    /// Creates and displays the auto-completion window with the provided completion data.
    /// </summary>
    /// <param name="editorCompletions">The collection of auto-completion suggestions.</param>
    private void CreateCompletionWindow(IEnumerable<EditorCompletionData> editorCompletions)
    {
        _completionWindow = new CompletionWindow(textEditor.TextArea)
        {
            Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#1F1F1F")!,
            Foreground = Brushes.White
        };
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

    /// <summary>
    /// Extracts and returns variable names defined within the text editor's content.
    /// </summary>
    /// <returns>An enumerable collection of completion data for in-text defined variables.</returns>
    private IEnumerable<EditorCompletionData> GetInTextDefinedVariables()
    {
        // Extract variables from textEditor's text:
        var regex = VariableNameRegex();

        foreach (Match match in regex.Matches(textEditor.Text))
        {
            yield return new EditorCompletionData(match.Groups[1].Value, "Variable specified in this document.");
        }
    }

    /// <summary>
    /// Extracts and returns method names defined within the text editor's content.
    /// </summary>
    /// <returns>An enumerable collection of completion data for in-text defined methods.</returns>
    private IEnumerable<EditorCompletionData> GetInTextDefinedMethods()
    {
        // Extract methods from textEditor's text:
        var regex = MethodNameRegex();

        foreach (Match match in regex.Matches(textEditor.Text))
        {
            yield return new EditorCompletionData(match.Groups[1].Value, "Method specified in this document.");
        }
    }

    /// <summary>
    /// Provides a collection of pre-defined variables available for auto-completion.
    /// </summary>
    /// <returns>An enumerable collection of completion data for pre-defined variables.</returns>
    private static IEnumerable<EditorCompletionData> GetPreDefinedVariables()
    {
        const string memoryManagerName = nameof(MemoryManager);
        yield return new EditorCompletionData(char.ToLowerInvariant(memoryManagerName[0]) + memoryManagerName[1..],
            "This class provides functionality for reading and writing to a process's memory.");
    }

    /// <summary>
    /// Provides a collection of pre-defined methods available for auto-completion.
    /// </summary>
    /// <returns>An enumerable collection of completion data for pre-defined methods.</returns>
    private static IEnumerable<EditorCompletionData> GetPreDefinedMethods()
    {
        yield return new EditorCompletionData(nameof(MemoryManager.ReadMemoryAt) + "<T>",
            "(int memoryAddress)\nReads the memory at the given address and returns it as defined type T.");
        yield return new EditorCompletionData(nameof(MemoryManager.WriteMemoryAt),
            "(int memoryAddress, T newValue)\nWrites the specified value to the memory at the given address.");
    }

    /// <summary>
    /// Handles the TextEntering event of the text editor. This is responsible for inserting the selected auto-completion suggestion.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event arguments.</param>
    private void textEditor_TextArea_TextEntering(object sender, TextCompositionEventArgs e)
    {
        if (e.Text.Length > 0 && _completionWindow != null)
        {
            if (e.Text == " ")
            {
                _completionWindow.Close();
                return;
            }

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
