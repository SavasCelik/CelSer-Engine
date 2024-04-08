using Microsoft.AspNetCore.Components.WebView;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
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

namespace CelSerEngine.WpfBlazor;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddWpfBlazorWebView();
#if DEBUG
        serviceCollection.AddBlazorWebViewDeveloperTools();
#endif
        Resources.Add("services", serviceCollection.BuildServiceProvider());

        InitializeComponent();
        blazorWebView.BlazorWebViewInitialized += BlazorWebView_BlazorWebViewInitialized;
    }

    private void BlazorWebView_BlazorWebViewInitialized(object? sender, BlazorWebViewInitializedEventArgs e)
    {
        blazorWebView.WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        blazorWebView.WebView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
        blazorWebView.WebView.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
        blazorWebView.WebView.CoreWebView2.Settings.IsPinchZoomEnabled = false;
        blazorWebView.WebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
        blazorWebView.WebView.CoreWebView2.Settings.IsSwipeNavigationEnabled = false;
        blazorWebView.WebView.CoreWebView2.Settings.IsStatusBarEnabled = false;

        if (Debugger.IsAttached)
        {
            blazorWebView.WebView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = true;
            blazorWebView.WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
        }

        blazorWebView.Visibility = Visibility.Visible;
        blazorWebView.Focus();
        blazorWebView.WebView.Focus();
    }
}