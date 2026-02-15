using CelSerEngine.Core.Native;
using CelSerEngine.WpfReact.Loggers;
using CelSerEngine.WpfReact.Trackers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace CelSerEngine.WpfReact;
/// <summary>
/// Interaction logic for PointerScannerWindow.xaml
/// </summary>
public partial class PointerScannerWindow : Window
{
    private readonly IntPtr _searchedAddress;

    public PointerScannerWindow(IServiceProvider mainServiceProvider, IntPtr searchedAddress)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddWpfReactWebView();
        var nativeApi = mainServiceProvider.GetRequiredService<INativeApi>();
        serviceCollection.AddSingleton(nativeApi);
        var processSelectionTracker = mainServiceProvider.GetRequiredService<ProcessSelectionTracker>();
        serviceCollection.AddSingleton(processSelectionTracker);
        var trackedItemNotifier = mainServiceProvider.GetRequiredService<TrackedItemNotifier>();
        serviceCollection.AddSingleton(trackedItemNotifier);
        var logManager = mainServiceProvider.GetRequiredService<LogTracker>();
        serviceCollection.AddLogging(x => x.ClearProviders().AddProvider(new DefaultLoggerProvider(logManager)).SetMinimumLevel(LogLevel.Debug));

        serviceCollection.AddSingleton(this);

        var services = serviceCollection.BuildServiceProvider();
        Resources.Add("services", services);
        InitializeComponent();
        reactWebView.ReactWebViewInitialized += ReactWebView_ReactWebViewInitialized;
        Unloaded += (s, args) =>
        {
            reactWebView.Dispose();
        };
        _searchedAddress = searchedAddress;
    }

    private void ReactWebView_ReactWebViewInitialized(object? sender, EventArgs e)
    {
        const string pointerScannerRoute = "#pointer-scanner";
        var searchParam = $"?searchedAddress={_searchedAddress:X8}";
        reactWebView.ConfigureWebView(pointerScannerRoute + searchParam);
    }
}
