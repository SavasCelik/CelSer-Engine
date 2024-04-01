using System.Windows;
using System.Windows.Media;

namespace CelSerEngine.Wpf.Extensions;

public static class WpfExtension
{
    // https://stackoverflow.com/questions/11187382/get-listview-visible-items
    public static T? GetVisualChild<T>(this Visual referenceVisual) where T : Visual
    {
        Visual? child = null;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(referenceVisual); i++)
        {
            child = VisualTreeHelper.GetChild(referenceVisual, i) as Visual;
            if (child != null && child is T)
                break;
            else if (child != null)
            {
                child = GetVisualChild<T>(child);
                if (child != null && child is T)
                    break;
            }
        }

        return child as T;
    }

    public static T? GetVisualParent<T>(this DependencyObject referenceVisual) where T : DependencyObject
    {
        DependencyObject? parent = referenceVisual;

        while ((parent != null) && !(parent is T))
        {
            parent = VisualTreeHelper.GetParent(parent);
        }

        return parent as T;
    }
}
