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
        Closing += async (s, args) =>
        {
            // if this DisposeAsync fails, make sure IAsyncDisposable isn't used in the components that are loaded in the BlazorWebViewWindow.xaml.cs
            // In case a nested component is using IAsyncDisposable make sure to dispose the nested component in the parent component
            await blazorWebView.DisposeAsync();
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
