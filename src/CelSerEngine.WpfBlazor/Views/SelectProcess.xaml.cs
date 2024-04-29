using Microsoft.AspNetCore.Components.WebView;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CelSerEngine.WpfBlazor.Views;
/// <summary>
/// Interaction logic for SelectProcess.xaml
/// </summary>
public partial class SelectProcess : Window
{
    public SelectProcess(MainWindow mainWindow)
    {
        InitializeComponent();
        Resources.Add("services", mainWindow.Services);
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

        blazorWebView.Visibility = Visibility.Visible;
        blazorWebView.Focus();
        blazorWebView.WebView.Focus();
    }
}
