using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows;
using System.IO;

namespace CelSerEngine.WpfBlazor;

public class ProcessAdapter : IDisposable
{
    private SafeProcessHandle? _processHandle;

    public Process Process { get; private set; }
    public string DisplayString { get; private set; }
    public BitmapSource? IconSource { get; private set; }
    public string? IconBase64Source { get; private set; }
    public ProcessModule? MainModule { get; private set; }
    public SafeProcessHandle ProcessHandle { 
        get => _processHandle ?? throw new NullReferenceException("Process Handle was null"); 
        set => _processHandle = value; 
    }

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
            using var processIcon = Icon.ExtractAssociatedIcon(MainModule.FileName)!;
            IconSource = Imaging.CreateBitmapSourceFromHIcon(
                processIcon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            using var iconsBitmap = processIcon.ToBitmap();
            using var memoryStream = new MemoryStream();
            iconsBitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            byte[] iconBytes = memoryStream.ToArray();
            IconBase64Source = Convert.ToBase64String(iconBytes);
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (ProcessHandle.IsInvalid)
        {
            // Free the handle
            ProcessHandle.Dispose();
        }
    }
}