using Microsoft.AspNetCore.Components.WebView.Wpf;
using System.Windows;
using CelSerEngine.WpfBlazor.Extensions;

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
        blazorWebView.BlazorWebViewInitialized += (s, args) =>
        {
            blazorWebView.ConfigureWebView();
        };
        Unloaded += (s, args) =>
        {
            blazorWebView.CloseWebView();
        };

        var component = new RootComponent
        {
            ComponentType = componentType,
            Selector = "#app",
            Parameters = parameters
        };
        blazorWebView.RootComponents.Add(component);
    }
}
