using CelSerEngine.Models;
using CelSerEngine.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CelSerEngine.ViewModels;

public partial class SelectProcessViewModel : ObservableRecipient
{
    [ObservableProperty]
    private IList<ProcessAdapter> _processes;
    [ObservableProperty]
    private ProcessAdapter? _selectedProcess;
    [ObservableProperty]
    private string _searchProcessText;

    private readonly IList<ProcessAdapter> _allProcesses;

    public SelectProcessViewModel()
    {
        _searchProcessText = "";
        _allProcesses = Process.GetProcesses()
            .OrderBy(p => p.ProcessName)
            .Select(p => new ProcessAdapter(p))
            .Where(pa => pa.MainModule != null)
            .ToList();
        _processes = _allProcesses;
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
            return SelectedProcess.GetProcessHandle();

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
