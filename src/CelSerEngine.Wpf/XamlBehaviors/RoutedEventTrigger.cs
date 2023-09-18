using Microsoft.Xaml.Behaviors;
using System;
using System.Windows;

namespace CelSerEngine.Wpf.XamlBehaviors;

/// <summary>
/// https://stackoverflow.com/questions/34271498/how-to-handle-scrollviewer-scrollchanged-event-in-mvvm#answer-34272606
/// </summary>
public class RoutedEventTrigger : EventTriggerBase<DependencyObject>
{
    public RoutedEvent? RoutedEvent { get; set; }

    protected override void OnAttached()
    {
        var associatedElement = AssociatedObject as FrameworkElement;

        if (AssociatedObject is Behavior behavior)
        {
            associatedElement = ((IAttachedObject)behavior).AssociatedObject as FrameworkElement;
        }

        if (associatedElement == null)
        {
            throw new ArgumentException("Routed Event trigger can only be associated to framework elements");
        }

        if (RoutedEvent != null)
        {
            associatedElement.AddHandler(RoutedEvent, new RoutedEventHandler(OnRoutedEvent));
        }
    }

    void OnRoutedEvent(object sender, RoutedEventArgs args)
    {
        base.OnEvent(args);
    }

    protected override string GetEventName()
    {
        return RoutedEvent!.Name;
    }
}
