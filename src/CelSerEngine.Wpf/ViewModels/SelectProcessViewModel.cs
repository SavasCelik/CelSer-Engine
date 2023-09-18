using CelSerEngine.Core.Native;
using CelSerEngine.Wpf.Models;
using CelSerEngine.Wpf.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CelSerEngine.Wpf.ViewModels;

public partial class SelectProcessViewModel : ObservableRecipient
{
    [ObservableProperty]
    private IList<ProcessAdapter> _processes;
    [ObservableProperty]
    private ProcessAdapter? _selectedProcess;
    [ObservableProperty]
    private string _searchProcessText;

    private IList<ProcessAdapter> _allProcesses;
    private readonly INativeApi _nativeApi;

    public SelectProcessViewModel(INativeApi nativeApi)
    {
        _searchProcessText = "";
        _allProcesses = new List<ProcessAdapter>();
        _processes = _allProcesses;
        _nativeApi = nativeApi;
    }

    partial void OnSearchProcessTextChanged(string value)
    {
        if (value == "")
        {
            Processes = _allProcesses;
        }
        else
        {
            Processes = _allProcesses.Where(p => p.Process.ProcessName.ToLower().Contains(value.ToLower())).ToList();
        }
    }

    public bool ShowSelectProcessDialog()
    {
        SearchProcessText = "";
        _allProcesses = Process.GetProcesses()
            .OrderBy(p => p.ProcessName)
            .Select(p => new ProcessAdapter(p))
            .Where(pa => pa.MainModule != null)
            .ToList();
        Processes = _allProcesses;

        var selectProcessWidnwow = new SelectProcess
        {
            Owner = App.Current.MainWindow
        };

        return selectProcessWidnwow.ShowDialog() ?? false;
    }

    public IntPtr GetSelectedProcessHandle()
    {
        if (SelectedProcess != null)
            return SelectedProcess.GetProcessHandle(_nativeApi);

        return IntPtr.Zero;
    }

    [Conditional("DEBUG")]

    public void AttachToDebugGame()
    {
        var process = Process.GetProcessesByName("SmallGame").First();
        SelectedProcess = new ProcessAdapter(process);
        var pHandle = GetSelectedProcessHandle();

        if (pHandle != IntPtr.Zero)
            Debug.WriteLine("Attached To DebugGame");
    }

}
