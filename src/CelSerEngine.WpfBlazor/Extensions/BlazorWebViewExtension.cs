using Microsoft.AspNetCore.Components.WebView.Wpf;
using System.Diagnostics;

namespace CelSerEngine.WpfBlazor.Extensions;

public static class BlazorWebViewExtension
{
    public static void ConfigureWebView(this BlazorWebView blazorWebView)
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

    public static void CloseWebView(this BlazorWebView blazorWebView)
    {
        blazorWebView.RootComponents.Clear();
        blazorWebView.WebView.Stop();
        // this is a workaround for a bug in WebView2 that causes a crash when disposing the control
#pragma warning disable CA2012
        _ = blazorWebView.DisposeAsync();
#pragma warning restore CA2012
        if (blazorWebView.WebView != null!)
        {
            blazorWebView.WebView.Dispose();
        }
    }
}
