using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CelSerEngine.WpfReact;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        reactWebView.ReactWebViewInitialized += ReactWebView_ReactWebViewInitialized;
    }

    private void ReactWebView_ReactWebViewInitialized(object? sender, EventArgs e)
    {
        reactWebView.WebView.Source = new Uri("http://localhost:49356/");
    }
}