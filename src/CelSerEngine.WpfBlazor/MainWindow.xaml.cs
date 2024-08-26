using CelSerEngine.Core.Native;
using CelSerEngine.Shared.Services.MemoryScan;
using CelSerEngine.WpfBlazor.Extensions;
using CelSerEngine.WpfBlazor.Loggers;
using CelSerEngine.WpfBlazor.Views;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Windows;

namespace CelSerEngine.WpfBlazor;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public IServiceProvider Services { get; set; }

    private SelectProcess? _selectProcess;

    public MainWindow()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddWpfBlazorWebView();
#if DEBUG
        serviceCollection.AddBlazorWebViewDeveloperTools();
#endif
        serviceCollection.AddSingleton(this);
        serviceCollection.AddSingleton<EngineSession>();
        serviceCollection.AddSingleton<ThemeManager>();
        
        serviceCollection.AddSingleton<IMemoryScanService, MemoryScanService>();
        serviceCollection.AddSingleton<INativeApi, NativeApi>();

        var logManager = new BlazorLogManager();
        serviceCollection.AddSingleton(logManager);
        serviceCollection.AddLogging(x => x.ClearProviders().AddProvider(new BlazorLoggerProvider(logManager)));

        Services = serviceCollection.BuildServiceProvider();
        Resources.Add("services", Services);

        InitializeComponent();

        blazorWebView.BlazorWebViewInitialized += BlazorWebView_BlazorWebViewInitialized;
        Closing += async (s, args) =>
        {
            await blazorWebView.DisposeAsync();
        };
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

        blazorWebView.Focus();
        blazorWebView.WebView.Focus();
    }

    public void OpenProcessSelector()
    {
        _selectProcess = new SelectProcess(this);
        _selectProcess.ShowModal(this);
    }

    public void CloseProcessSelector()
    {
        Dispatcher.BeginInvoke(() => 
        {
            _selectProcess?.Close();
            _selectProcess = null;
        });
    }
}