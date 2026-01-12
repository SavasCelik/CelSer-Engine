using System.Media;
using System.Windows;

namespace CelSerEngine.WpfReact;

public static class WpfWindowExtensions
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
            owner.Focus();
        };

        window.Show();
    }
}
