using CelSerEngine.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CelSerEngine.ViewModels
{
    public partial class SelectProcessViewModel : ObservableRecipient
    {
        private readonly IList<ProcessAdapter> _allProcesses;
        [ObservableProperty]
        private IList<ProcessAdapter> processes;
        [ObservableProperty]
        private ProcessAdapter? selectedProcess;
        [ObservableProperty]
        private string searchProcessText;

        public SelectProcessViewModel()
        {
            searchProcessText = "";
            _allProcesses = Process.GetProcesses()
                .OrderBy(p => p.ProcessName)
                .Select(p => new ProcessAdapter(p))
                .Where(pa => pa.MainModule != null)
                .ToList();
            processes = _allProcesses;
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
    }
}
