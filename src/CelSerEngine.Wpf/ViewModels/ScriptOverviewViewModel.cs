using CelSerEngine.Wpf.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelSerEngine.Wpf.ViewModels;
public partial class ScriptOverviewViewModel : ObservableObject
{
    [ObservableProperty]
    private IList<ObservableScript> _scripts;

    private readonly SelectProcessViewModel _selectProcessViewModel;

    public ScriptOverviewViewModel(SelectProcessViewModel selectProcessViewModel)
    {
        _selectProcessViewModel = selectProcessViewModel;
        _scripts = new List<ObservableScript>();
        _scripts.Add(new ObservableScript
        {
            Logic = "wow",
            Name = "contains wow"
        });
        _scripts.Add(new ObservableScript
        {
            Logic = "Hello World",
            Name = "contains Hello World"
        });
    }
}
