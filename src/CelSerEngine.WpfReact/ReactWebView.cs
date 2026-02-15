using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using WebView2Control = Microsoft.Web.WebView2.Wpf.WebView2;

namespace CelSerEngine.WpfReact;

public class ReactWebView : Control, IDisposable
{
    /// <summary>
    /// The backing store for the <see cref="Services"/> property.
    /// </summary>
    public static readonly DependencyProperty ServicesProperty = DependencyProperty.Register(
        name: nameof(Services),
        propertyType: typeof(IServiceProvider),
        ownerType: typeof(ReactWebView));

    /// <summary>
    /// Gets or sets an <see cref="IServiceProvider"/> containing services to be used by this control and also by application code.
    /// This property must be set to a valid value for the Razor components to start.
    /// </summary>
    public IServiceProvider Services
    {
        get => (IServiceProvider)GetValue(ServicesProperty);
        set => SetValue(ServicesProperty, value);
    }

    /// <summary>
    /// The backing store for the <see cref="ReactWebViewInitialized"/> event.
    /// </summary>
    public static readonly DependencyProperty ReactWebViewInitializedProperty = DependencyProperty.Register(
        name: nameof(ReactWebViewInitialized),
        propertyType: typeof(EventHandler),
        ownerType: typeof(ReactWebView));

    /// <summary>
    /// Allows customizing the web view after it is created.
    /// </summary>
    public EventHandler ReactWebViewInitialized
    {
        get => (EventHandler)GetValue(ReactWebViewInitializedProperty);
        set => SetValue(ReactWebViewInitializedProperty, value);
    }

    private const string WebViewTemplateChildName = "WebView";
    private WebView2Control? _webview;
    private ReactWebViewManager? _webViewManager;
    public WebView2Control WebView => _webview!;


    public ReactWebView()
    {
        Template = new ControlTemplate
        {
            VisualTree = new FrameworkElementFactory(typeof(WebView2Control), WebViewTemplateChildName)
        };
    }

    /// <inheritdoc cref="FrameworkElement.OnApplyTemplate" />
    public override void OnApplyTemplate()
    {
        // Called when the control is created after its child control (the WebView2) is created from the Template property
        base.OnApplyTemplate();

        if (_webview == null)
        {
            _webview = (WebView2Control)GetTemplateChild(WebViewTemplateChildName);
            _ = InitializeWebViewCoreAsync();
        }
    }

    private async Task InitializeWebViewCoreAsync()
    {
        var userDataFolder = GetWebView2UserDataFolder();
        var _coreWebView2Environment = await CoreWebView2Environment.CreateAsync(
                userDataFolder: userDataFolder, 
                options: new CoreWebView2EnvironmentOptions() { AreBrowserExtensionsEnabled = true });
        await _webview!.EnsureCoreWebView2Async(_coreWebView2Environment);
        ApplyDefaultWebViewSettings();

        var jsonOptions = new JsonSerializerOptions // could also allow passing json options via services and only create a default one, when not provided
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };

        var reactJsRuntime = Services.GetRequiredService<ReactJsRuntime>();
        reactJsRuntime.AttachToWebViewAndJsonOptions(_webview.CoreWebView2, jsonOptions);
        _webViewManager = new ReactWebViewManager(Services, _webview, jsonOptions);
        ReactWebViewInitialized?.Invoke(this, EventArgs.Empty);
    }

    private static string? GetWebView2UserDataFolder()
    {
        if (Assembly.GetEntryAssembly() is { } mainAssembly)
        {
            // In case the application is running from a non-writable location (e.g., program files if you're not running
            // elevated), use our own convention of %LocalAppData%\YourApplicationName.WebView2.
            // We may be able to remove this if https://github.com/MicrosoftEdge/WebView2Feedback/issues/297 is fixed.
            var applicationName = mainAssembly.GetName().Name;
            var result = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                $"{applicationName}.WebView2");

            return result;
        }

        return null;
    }

    private void ApplyDefaultWebViewSettings()
    {
        _webview.CoreWebView2.Settings.AreDevToolsEnabled = false;

        // Desktop applications typically don't want the default web browser context menu
        _webview.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;

        // Desktop applications almost never want to show a URL preview when hovering over a link
        _webview.CoreWebView2.Settings.IsStatusBarEnabled = false;
    }

    public void Dispose() {
        _webViewManager?.Dispose();
        _webview?.Dispose();
    }
}
