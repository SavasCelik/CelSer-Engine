using ICSharpCode.AvalonEdit;
using System.Windows;
using System;

namespace CelSerEngine.Wpf.XamlBehaviors;
public static class AvalonEditBehavior
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.RegisterAttached(
            "Text",
            typeof(string),
            typeof(AvalonEditBehavior),
            new FrameworkPropertyMetadata(default(string), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, PropertyChangedCallback)
        );

    public static string GetText(DependencyObject dependencyObject)
    {
        return (string)dependencyObject.GetValue(TextProperty);
    }

    public static void SetText(DependencyObject dependencyObject, string value)
    {
        dependencyObject.SetValue(TextProperty, value);
    }

    private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is TextEditor textEditor)
        {
            if (e.OldValue == null && e.NewValue != null)
            {
                textEditor.TextChanged += TextEditor_TextChanged;
            }
            else if (e.OldValue != null && e.NewValue == null)
            {
                textEditor.TextChanged -= TextEditor_TextChanged;
            }

            var caretOffset = textEditor.CaretOffset;
            textEditor.Document.Text = GetText(dependencyObject);

            if (textEditor.Document.Text.Length >= caretOffset)
                textEditor.CaretOffset = caretOffset;
        }
    }

    private static void TextEditor_TextChanged(object sender, EventArgs e)
    {
        if (sender is TextEditor textEditor)
        {
            SetText(textEditor, textEditor.Document.Text);
        }
    }
}