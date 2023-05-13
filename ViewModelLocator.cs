using CelSerEngine.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CelSerEngine;

public class ViewModelLocator
{
    public MainViewModel MainViewModel => App.Current.Services.GetRequiredService<MainViewModel>();
    public SelectProcessViewModel SelectProcessViewModel => App.Current.Services.GetRequiredService<SelectProcessViewModel>();
    public TrackedScanItemsViewModel TrackedScanItemsViewModel => App.Current.Services.GetRequiredService<TrackedScanItemsViewModel>();
    public ScanResultsViewModel ScanResultsViewModel => App.Current.Services.GetRequiredService<ScanResultsViewModel>();
    public PointerScanOptionsViewModel PointerScanOptionsViewModel => App.Current.Services.GetRequiredService<PointerScanOptionsViewModel>();
    public PointerScanResultsViewModel PointerScanResultsViewModel => App.Current.Services.GetRequiredService<PointerScanResultsViewModel>();
}
