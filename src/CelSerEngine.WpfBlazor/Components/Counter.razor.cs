using CelSerEngine.Core.Comparators;
using CelSerEngine.Core.Models;
using CelSerEngine.Core.Native;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Diagnostics;
using System.Text.Json;

namespace CelSerEngine.WpfBlazor.Components;

public partial class Counter : ComponentBase, IAsyncDisposable
{
    [Inject]
    public IJSRuntime JS { get; set; } = default!;

    [Inject]
    public INativeApi NativeApi { get; set; } = default!;

    [Inject]
    private MainWindow? MainWindow { get; set; }

    private IJSObjectReference? _module;

    private DotNetObjectReference<Counter>? _dotNetHelper;

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotNetHelper = DotNetObjectReference.Create(this);
            _module = await JS.InvokeAsync<IJSObjectReference>("import", "/js/main-window.js");
            await _module.InvokeVoidAsync("ready", _dotNetHelper);
        }
    }

    private int counter = 0;

    private IList<IMemorySegment> _memorySegments;

    private async Task OpenSelectProcess()
    {
        var process = Process.GetProcessesByName("chrome").First();
        var selectedProcess = new ProcessAdapter(process);
        var pHandle = selectedProcess.GetProcessHandle(NativeApi);
        var virtualMemoryRegions = NativeApi.GatherVirtualMemoryRegions(pHandle);
        var scanConstraint = new ScanConstraint(ScanCompareType.ExactValue, ScanDataType.Integer, "1");
        var comparer = ComparerFactory.CreateVectorComparer(scanConstraint);
        _memorySegments = comparer.GetMatchingMemorySegments(virtualMemoryRegions, null);
        selectedProcess.Dispose();
        await _module.InvokeVoidAsync("applyData", JsonSerializer.Serialize(_memorySegments.Take(8).Select(x => new { Address = x.Address.ToString("X"), x.Value })), _memorySegments.Count);
        //MainWindow.OpenProcessSelector();
    }

    private async Task NextScan()
    {
        var process = Process.GetProcessesByName("chrome").First();
        var selectedProcess = new ProcessAdapter(process);
        var pHandle = selectedProcess.GetProcessHandle(NativeApi);
        var virtualMemoryRegions = NativeApi.GatherVirtualMemoryRegions(pHandle);
        var scanConstraint = new ScanConstraint(ScanCompareType.ExactValue, ScanDataType.Integer, "1");
        var comparer = ComparerFactory.CreateVectorComparer(scanConstraint);
        NativeApi.UpdateAddresses(pHandle, _memorySegments);
        var passedMemorySegments = new List<IMemorySegment>();

        for (var i = 0; i < _memorySegments.Count; i++)
        {
            if (ValueComparer.MeetsTheScanConstraint(_memorySegments[i].Value, scanConstraint.UserInput, scanConstraint))
                passedMemorySegments.Add(_memorySegments[i]);
        }

        selectedProcess.Dispose();
        await _module.InvokeVoidAsync("applyData", JsonSerializer.Serialize(passedMemorySegments.Select(x => new { Address = x.Address.ToString("X"), x.Value })));
        //MainWindow.OpenProcessSelector();
    }

    [JSInvokable]
    public Task<string> GetItemsAsync(int startIndex, int amount)
    {
        var visibleItems = _memorySegments.Skip(startIndex).Take(amount);

        return Task.FromResult(JsonSerializer.Serialize(visibleItems.Select(x => new { Address = x.Address.ToString("X"), x.Value })));
    }

    private HashSet<string> _selectedItems = [];

    [JSInvokable]
    public Task SelectTillItemAsync(string selectTillItem)
    {
        var isSlecting = false;
        var lastAddress = selectTillItem;
        var firstAddress = _selectedItems.LastOrDefault();

        if (firstAddress == null)
        {
            _selectedItems.Add(lastAddress);

            return Task.CompletedTask;
        }

        foreach (var item in _memorySegments)
        {
            var address = item.Address.ToString("X");
            if (address == firstAddress || address == lastAddress)
            {
                isSlecting = !isSlecting;

                if (!isSlecting)
                {
                    _selectedItems.Add(address);
                    break;
                }
            }

            if (isSlecting)
            {
                _selectedItems.Add(address);
            }
        }

        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task<bool> IsItemSelectedAsync(string item)
    {
        return Task.FromResult(_selectedItems.Contains(item));
    }

    [JSInvokable]
    public Task ClearSelectedItems()
    {
        _selectedItems.Clear();

        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task AddSelectedItemAsync(string item)
    {
        _selectedItems.Add(item);

        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task RemoveSelectedItemAsync(string item)
    {
        _selectedItems.Remove(item);

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _dotNetHelper?.Dispose();

        if (_module != null)
        {
            await _module.DisposeAsync();
        }
    }
}
