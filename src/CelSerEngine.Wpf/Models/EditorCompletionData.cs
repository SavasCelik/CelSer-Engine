using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System;
using System.Windows.Media;

namespace CelSerEngine.Wpf.Models;

/// Implements AvalonEdit ICompletionData interface to provide the entries in the
/// completion drop down.
public class EditorCompletionData : ICompletionData
{
    public ImageSource? Image => null;

    public string Text { get; }

    // Use this property if you want to show a fancy UIElement in the list.
    public object Content => Text;

    public object Description { get; }

    public double Priority => 1;

    public EditorCompletionData(string text, string description = "")
    {
        Text = text;
        Description = description;
    }


    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        textArea.Document.Replace(completionSegment, Text);
    }
}