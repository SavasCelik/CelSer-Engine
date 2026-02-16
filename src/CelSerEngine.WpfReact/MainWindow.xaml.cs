using CelSerEngine.Core.Native;
using CelSerEngine.Shared.Services.MemoryScan;
using CelSerEngine.WpfReact.Loggers;
using CelSerEngine.WpfReact.Trackers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
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
        serviceCollection.AddSingleton<TrackedItemNotifier>();
        serviceCollection.AddSingleton<IMemoryScanService, MemoryScanService>();
        serviceCollection.AddSingleton<INativeApi, NativeApi>();
        serviceCollection.AddSingleton(this);

        var logManager = new LogTracker();
        serviceCollection.AddSingleton(logManager);
        serviceCollection.AddLogging(x => x.ClearProviders().AddProvider(new DefaultLoggerProvider(logManager)).SetMinimumLevel(LogLevel.Debug));

        Services = serviceCollection.BuildServiceProvider();
        Resources.Add("services", Services);
        InitializeComponent();
        reactWebView.ReactWebViewInitialized += ReactWebView_ReactWebViewInitialized;
    }

    private async void ReactWebView_ReactWebViewInitialized(object? sender, EventArgs e)
    {
#if DEBUG
        // Add React Developer Tools extension
        var installedExtensions = await reactWebView.WebView.CoreWebView2.Profile.GetBrowserExtensionsAsync();

        if (!installedExtensions.Any(ext => ext.Name == "React Developer Tools"))
        {
            // could also download the extension from the store. like so: https://github.com/MicrosoftEdge/WebView2Feedback/issues/3694#issuecomment-1993649183
            var extensionPath = Path.Combine(AppContext.BaseDirectory, "BrowserExtensions", "ReactDeveloperTools", "7.0.1_0");
            await reactWebView.WebView.CoreWebView2.Profile.AddBrowserExtensionAsync(extensionPath);
        }
#endif

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

    public void OpenPointerScanner(IntPtr searchedAddress)
    {
        var pointerScannerWindow = new PointerScannerWindow(Services, searchedAddress);

        // Ensure the pointer scanner window closes when the main window closes.
        // By not setting Owner, we avoid forcing the window to stay above the main window,
        // which preserves normal focus and Z-order behavior.
        void MainWindowClosed(object? sender, EventArgs e)
        {
            pointerScannerWindow.Close();
            Closed -= MainWindowClosed;
        }

        Closed += MainWindowClosed;

        pointerScannerWindow.Show();
    }
}
