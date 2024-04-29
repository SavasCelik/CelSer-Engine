using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CelSerEngine.WpfBlazor.Extensions;
public static class WpfWindowExtension
{
    public static void ShowModal(this Window window, Window owner)
    {
        owner.IsEnabled = false;
        void onActivate(object? sender, EventArgs e)
        {
            SystemSounds.Beep.Play();
            window.Activate();
            window.Focus();
        }
        owner.Activated += onActivate;
        window.Owner = owner;
        window.Closing += (s, args) =>
        {
            owner.IsEnabled = true;
            owner.Activated -= onActivate;
        };

        window.Show();
    }
}
