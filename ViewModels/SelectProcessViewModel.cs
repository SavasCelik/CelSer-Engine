using CelSerEngine.Models;
using CelSerEngine.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CelSerEngine.ViewModels
{
    public partial class SelectProcessViewModel : ObservableRecipient
    {
        private readonly Regex _alphabetRegex;
        private readonly IList<ProcessAdapter> _allProcesses;
        [ObservableProperty]
        private IList<ProcessAdapter> processes;
        [ObservableProperty]
        private ProcessAdapter? selectedProcess;

        public SelectProcessViewModel()
        {
            _allProcesses = Process.GetProcesses()
                .OrderBy(p => p.ProcessName)
                .Select(p => new ProcessAdapter(p))
                .Where(pa => pa.MainModule != null)
                .ToList();
            processes = _allProcesses;
            _alphabetRegex = new Regex("[A-Za-z]");
        }

        [RelayCommand]
        public void KeyPressed(KeyEventArgs keyEventArgs)
        {
            var pressedKey = keyEventArgs.Key.ToString().ToLower();

            if (pressedKey.Length == 1 && _alphabetRegex.IsMatch(keyEventArgs.Key.ToString()))
            {
                Processes = _allProcesses.Where(x => x.Process.ProcessName.ToLower().StartsWith(pressedKey)).ToList();
            }
        }

        [RelayCommand]
        public void DoubleClickOnProcess(SelectProcess window)
        {
            window.Close();
        }
    }
}
