using CelSerEngine.Core.Native;
using CelSerEngine.Shared.Services.MemoryScan;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace CelSerEngine.WpfReact;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public IServiceProvider Services { get; set; }
    private SelectProcessWindow? _selectProcessWindow;

    public MainWindow()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddWpfReactWebView();
        serviceCollection.AddLogging();
        serviceCollection.AddSingleton<ProcessSelectionTracker>();
        serviceCollection.AddSingleton<IMemoryScanService, MemoryScanService>();
        serviceCollection.AddSingleton<INativeApi, NativeApi>();
        serviceCollection.AddSingleton(this);

        Services = serviceCollection.BuildServiceProvider();
        Resources.Add("services", Services);
        InitializeComponent();
        reactWebView.ReactWebViewInitialized += ReactWebView_ReactWebViewInitialized;
    }

    private void ReactWebView_ReactWebViewInitialized(object? sender, EventArgs e)
    {
        reactWebView.WebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
        reactWebView.ConfigureWebView();
    }

    public void OpenProcessSelector()
    {
        _selectProcessWindow = new SelectProcessWindow(this);
        _selectProcessWindow.ShowModal(this);
    }

    public void CloseProcessSelector()
    {
        Dispatcher.BeginInvoke(() =>
        {
            _selectProcessWindow?.Close();
            _selectProcessWindow = null;
        });
    }
}
