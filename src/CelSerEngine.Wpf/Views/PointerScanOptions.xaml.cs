using System.Windows;

namespace CelSerEngine.Wpf.Views;

/// <summary>
/// Interaction logic for PointerScanOptions.xaml
/// </summary>
public partial class PointerScanOptions : Window
{
    public PointerScanOptions()
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
