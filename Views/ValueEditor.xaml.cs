using System.Windows;

namespace CelSerEngine.Views;

public partial class ValueEditor : Window
{
    public string Value { get; private set; } = string.Empty;

    public ValueEditor(string propertyName = "Value")
    {
        InitializeComponent();
        Title = $"Change {propertyName}";
        lblTxtBox.Content = $"Set new {propertyName}:";
    }

    public void SetValueTextBox(string value)
    {
        valueTxtBox.Text = value;
    }

    public void SetFocusTextBox()
    {
        valueTxtBox.SelectionStart = valueTxtBox.Text.Length;
        valueTxtBox.Focus();
    }

    private void OkBtn_Click(object sender, RoutedEventArgs e)
    {
        Value = valueTxtBox.Text;
        DialogResult = true;
        Close();
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e)
    {
        Value = string.Empty;
        DialogResult = false;
        Close();
    }
}
