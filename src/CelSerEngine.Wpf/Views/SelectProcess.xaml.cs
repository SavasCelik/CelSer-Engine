using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;

namespace CelSerEngine.Wpf.Views;

/// <summary>
/// Interaction logic for SelectProcess.xaml
/// </summary>
public partial class SelectProcess : Window
{
    private readonly ImageBrush _searchProcessWatermark;
    public SelectProcess()
    {
        InitializeComponent();
        _searchProcessWatermark = (ImageBrush)searchProcessTxtBox.Background;
    }

    /// <summary>
    /// [View Exclusive Method] This method is only used for displaing the "Search Process" watermark
    /// </summary>
    void OnSearchProcessTextChanged(object sender, TextChangedEventArgs e)
    {
        if (searchProcessTxtBox.Text == "")
        {
            // Use the brush to paint the TextBox's background.
            searchProcessTxtBox.Background = _searchProcessWatermark;
        }
        else
        {
            searchProcessTxtBox.Background = null;
        }
    }
}
