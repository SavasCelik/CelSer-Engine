using Microsoft.AspNetCore.Components;
using System.Diagnostics;
using System.Windows.Input;

namespace CelSerEngine.WpfBlazor.Components;

public partial class SelectProcess : ComponentBase
{
    private IList<ProcessAdapter> _processes;

    protected override Task OnInitializedAsync()
    {
        _processes = Process.GetProcesses()
            .OrderBy(p => p.ProcessName)
            .Select(p => new ProcessAdapter(p))
            .Where(pa => pa.MainModule != null)
            .ToList();

        return base.OnInitializedAsync();
    }
    HashSet<ProcessAdapter> selectedItems = new HashSet<ProcessAdapter>();
    ProcessAdapter? lastClickedItem = null;

    protected void RowDblClicked(ProcessAdapter item)
    {
        if (!Keyboard.IsKeyDown(Key.LeftCtrl))
        {
            selectedItems.Clear();
        }

        if (lastClickedItem == null || (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift)))
        {
            lastClickedItem = item;
            selectedItems.Add(item);
        }

        int startIndex = _processes.IndexOf(lastClickedItem);
        int endIndex = _processes.IndexOf(item);

        if (startIndex < endIndex)
        {
            for (int i = startIndex; i <= endIndex; i++)
            {
                selectedItems.Add(_processes[i]);
            }
        }
        else
        {
            for (int i = startIndex; i >= endIndex; i--)
            {
                selectedItems.Add(_processes[i]);
            }
        }
    }

    string GetRowStyle(ProcessAdapter item)
    {
        return selectedItems.Contains(item) ? "table-primary" : "";
    }
}
