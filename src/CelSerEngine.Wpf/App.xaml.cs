﻿using CelSerEngine.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace CelSerEngine.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public sealed partial class App : Application
{
    public App()
    {
        Services = ConfigureServices();
        InitializeComponent();
    }

    /// <summary>
    /// Gets the current <see cref="App"/> instance in use
    /// </summary>
    public new static App Current => (App)Application.Current;

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
    /// </summary>
    public IServiceProvider Services { get; }

    /// <summary>
    /// Configures the services for the application.
    /// </summary>
    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<SelectProcessViewModel>();
        services.AddSingleton<TrackedScanItemsViewModel>();
        services.AddSingleton<ScanResultsViewModel>();
        services.AddSingleton<PointerScanOptionsViewModel>();
        services.AddSingleton<PointerScanResultsViewModel>();

        return services.BuildServiceProvider();
    }
}
