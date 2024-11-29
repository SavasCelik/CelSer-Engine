using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.AspNetCore.Components.WebView;
using System.Diagnostics;
using System.Windows;

namespace CelSerEngine.WpfBlazor.Views;
/// <summary>
/// Interaction logic for BlazorWebViewWindow.xaml
/// </summary>
public partial class BlazorWebViewWindow : Window
{
    public BlazorWebViewWindow(MainWindow mainWindow, Type componentType, string title, int width = 800, int height = 450, IDictionary<string, object?>? parameters = null)
    {
        InitializeComponent();
        Title = title;
        Width = width;
        Height = height;
        Resources.Add("services", mainWindow.Services);
        blazorWebView.BlazorWebViewInitialized += BlazorWebView_BlazorWebViewInitialized;
        Closing += (s, args) =>
        {
            blazorWebView.RootComponents.Clear();
            blazorWebView.WebView.Stop();
            // this is a workaround for a bug in WebView2 that causes a crash when disposing the control
#pragma warning disable CA2012
            _ = blazorWebView.DisposeAsync();
#pragma warning restore CA2012
            if (blazorWebView.WebView != null!)
            {
                blazorWebView.WebView.Dispose();
            }
        };

        var component = new RootComponent
        {
            ComponentType = componentType,
            Selector = "#app",
            Parameters = parameters
        };
        blazorWebView.RootComponents.Add(component);
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
