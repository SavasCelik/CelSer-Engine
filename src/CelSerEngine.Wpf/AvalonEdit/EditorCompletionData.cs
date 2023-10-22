using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System;
using System.Windows.Media;

namespace CelSerEngine.Wpf.AvalonEdit;

/// <summary>
/// Implements AvalonEdit ICompletionData interface to provide the entries in the completion drop down.
/// </summary>
public class EditorCompletionData : ICompletionData
{
    public ImageSource? Image => null;
    public string Text { get; }
    // Use this property if you want to show a fancy UIElement in the list.
    public object Content => Text;
    public object? Description { get; }
    public double Priority => 1;

    /// <summary>
    /// Create an instance of <see cref="EditorCompletionData"/> used for Auto-Completion
    /// </summary>
    /// <param name="text">The text shown as completion</param>
    /// <param name="description">The text shown in the tooltip.</param>
    public EditorCompletionData(string text, string? description = null)
    {
        Text = text;
        Description = description;
    }

    /// <inheritdoc />
    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        textArea.Document.Replace(completionSegment, Text);
    }
}