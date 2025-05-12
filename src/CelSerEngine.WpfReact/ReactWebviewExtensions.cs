namespace CelSerEngine.WpfReact;

public static class ReactWebviewExtensions
{
    public static void ConfigureWebView(this ReactWebView reactWebView, string route = "")
    {
        if (reactWebView.WebView.CoreWebView2 == null)
        {
            throw new InvalidOperationException("WebView2 is not initialized.");
        }

#if DEBUG
        reactWebView.WebView.Source = new Uri("http://localhost:49356" + route);
#else
        // Using an IP address means that WebView2 doesn't wait for any DNS resolution,
        // making it substantially faster. Note that this isn't real HTTP traffic, since
        // we intercept all the requests within this origin.
        const string AppHostAddress = "0.0.0.1";
        var reactClientPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReactClient");
        reactWebView.WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(AppHostAddress, reactClientPath, Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);

        // could also be done like this
        // https://github.com/dotnet/maui/blob/9.0.60/src/BlazorWebView/src/SharedSource/WebView2WebViewManager.cs#L300
        //reactWebView.WebView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
        //reactWebView.WebView.CoreWebView2.WebResourceRequested += async (s, eventArgs) =>
        //{
        //    await CoreWebView2_WebResourceRequested(eventArgs);
        //};
        //_physicalFileProvider = new PhysicalFileProvider(reactClientPath);

        if (System.IO.Directory.Exists(reactClientPath))
        {
            reactWebView.WebView.Source = new Uri($"https://{AppHostAddress}/index.html{route}");
        }
        else
        {
            System.Windows.MessageBox.Show("The React client could not be found. Please ensure it has been built and published to the correct location.");
        }
#endif
    }
}
