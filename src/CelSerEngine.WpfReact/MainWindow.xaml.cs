using System.Windows;

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
        reactWebView.WebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
    }
}