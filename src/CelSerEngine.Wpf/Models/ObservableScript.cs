using CelSerEngine.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelSerEngine.Wpf.Models;

[ObservableObject]
public partial class ObservableScript : BaseScript
{
    [ObservableProperty]
    private bool _isActivated;

}
