using CelSerEngine.Core.Native;
using CelSerEngine.Shared.Services.MemoryScan;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

#if !DEBUG
using Microsoft.Web.WebView2.Core;
using System.IO;
#endif

namespace CelSerEngine.WpfReact;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public IServiceProvider Services { get; set; }

    public MainWindow()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddWpfReactWebView();
        serviceCollection.AddLogging();
        serviceCollection.AddSingleton<ProcessSelectionTracker>();
        serviceCollection.AddSingleton<IMemoryScanService, MemoryScanService>();
        serviceCollection.AddSingleton<INativeApi, NativeApi>();

        Services = serviceCollection.BuildServiceProvider();
        Resources.Add("services", Services);
        InitializeComponent();
        reactWebView.ReactWebViewInitialized += ReactWebView_ReactWebViewInitialized;
    }

    private void ReactWebView_ReactWebViewInitialized(object? sender, EventArgs e)
    {
#if DEBUG
        reactWebView.WebView.Source = new Uri("http://localhost:49356/");
#else
        const string HostName = "CelSer-Engine";
        var reactClientPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReactClient");
        reactWebView.WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(HostName, reactClientPath, CoreWebView2HostResourceAccessKind.Allow);

        if (Directory.Exists(reactClientPath))
        {
            reactWebView.WebView.Source = new Uri($"https://{HostName}/index.html");
        }
        else
        {
            MessageBox.Show("The React client could not be found. Please ensure it has been built and published to the correct location.");
        }
#endif
        reactWebView.WebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
    }
}
