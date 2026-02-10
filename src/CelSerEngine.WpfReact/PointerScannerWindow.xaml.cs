using CelSerEngine.Core.Native;
using Microsoft.Extensions.DependencyInjection;
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
        var mainWindow = mainServiceProvider.GetRequiredService<MainWindow>();
        serviceCollection.AddSingleton(mainWindow);

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
