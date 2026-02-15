using CelSerEngine.Core.Native;
using CelSerEngine.WpfReact.Loggers;
using CelSerEngine.WpfReact.Trackers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace CelSerEngine.WpfReact
{
    /// <summary>
    /// Interaction logic for SelectProcessWindow.xaml
    /// </summary>
    public partial class SelectProcessWindow : Window
    {
        public SelectProcessWindow(MainWindow mainWindow)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddWpfReactWebView();
            serviceCollection.AddSingleton(mainWindow);
            var nativeApi = mainWindow.Services.GetRequiredService<INativeApi>();
            serviceCollection.AddSingleton(nativeApi);
            var processSelectionTracker = mainWindow.Services.GetRequiredService<ProcessSelectionTracker>();
            serviceCollection.AddSingleton(processSelectionTracker);
            var logManager = mainWindow.Services.GetRequiredService<LogTracker>();
            serviceCollection.AddLogging(x => x.ClearProviders().AddProvider(new DefaultLoggerProvider(logManager)).SetMinimumLevel(LogLevel.Debug));

            var services = serviceCollection.BuildServiceProvider();
            Resources.Add("services", services);
            InitializeComponent();
            reactWebView.ReactWebViewInitialized += ReactWebView_ReactWebViewInitialized;
            Unloaded += (s, args) =>
            {
                reactWebView.Dispose();
            };
        }

        private void ReactWebView_ReactWebViewInitialized(object? sender, EventArgs e)
        {
            const string selectProcessRoute = "#select-process";
            reactWebView.ConfigureWebView(selectProcessRoute);
        }
    }
}
