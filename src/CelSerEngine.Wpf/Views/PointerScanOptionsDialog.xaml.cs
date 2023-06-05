using System.Windows;

namespace CelSerEngine.Wpf.Views;

/// <summary>
/// Interaction logic for PointerScanOptionsDialog.xaml
/// </summary>
public partial class PointerScanOptionsDialog : Window
{
    public PointerScanOptionsDialog()
    {
        InitializeComponent();
    }

    private void OkBtn_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
