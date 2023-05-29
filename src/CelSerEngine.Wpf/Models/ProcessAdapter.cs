using CelSerEngine.Core.Native;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace CelSerEngine.Wpf.Models;

public class ProcessAdapter
{
    public Process Process { get; private set; }
    public string DisplayString { get; private set; }
    public BitmapSource? IconSource { get; private set; }
    public ProcessModule? MainModule { get; private set; }
    private IntPtr _processHandle;

    public ProcessAdapter(Process process)
    {
        Process = process;
        TryGetMainModule();
        DisplayString = MainModule != null ? $"0x{Process.Id:X8} - {MainModule.ModuleName}" : "MainModule not found!";
        GetIconImageSource();
    }

    private void TryGetMainModule()
    {
        try
        {
            if (Process.HasExited)
                return;

            MainModule = Process.MainModule;
        }
        catch (Win32Exception) { }
    }

    private void GetIconImageSource()
    {
        if (MainModule != null && !string.IsNullOrEmpty(MainModule.FileName))
        {
            var processIcon = Icon.ExtractAssociatedIcon(MainModule.FileName)!;
            IconSource = Imaging.CreateBitmapSourceFromHIcon(
                processIcon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }
    }

    public IntPtr GetProcessHandle()
    {
        if (_processHandle == IntPtr.Zero)
            _processHandle = NativeApi.OpenProcess(Process.Id);

        return _processHandle;
    }
}
