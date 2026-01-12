using CelSerEngine.Core.Native;
using Microsoft.Extensions.DependencyInjection;
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
