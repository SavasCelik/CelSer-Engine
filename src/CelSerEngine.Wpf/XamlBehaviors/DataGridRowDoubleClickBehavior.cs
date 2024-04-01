using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using CelSerEngine.Wpf.Extensions;
using Microsoft.Xaml.Behaviors;

namespace CelSerEngine.Wpf.XamlBehaviors;
public class DataGridRowDoubleClickBehavior : TriggerAction<DataGrid>
{
    // Command DependencyProperty
    public ICommand Command
    {
        get { return (ICommand)GetValue(CommandProperty); }
        set { SetValue(CommandProperty, value); }
    }

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand),
            typeof(DataGridRowDoubleClickBehavior), new PropertyMetadata(null));

    // CommandParameter DependencyProperty
    public object CommandParameter
    {
        get { return GetValue(CommandParameterProperty); }
        set { SetValue(CommandParameterProperty, value); }
    }

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(nameof(CommandParameter), typeof(object),
            typeof(DataGridRowDoubleClickBehavior), new PropertyMetadata(null));

    // ShouldCloseDialog
    public bool ShouldCloseDialog
    {
        get { return (bool)GetValue(ShouldCloseDialogProperty); }
        set { SetValue(ShouldCloseDialogProperty, value); }
    }

    public static readonly DependencyProperty ShouldCloseDialogProperty =
        DependencyProperty.Register(nameof(ShouldCloseDialog), typeof(bool),
            typeof(DataGridRowDoubleClickBehavior), new PropertyMetadata(false));

    protected override void Invoke(object parameter)
    {
        if (parameter is not MouseButtonEventArgs args || args.ChangedButton != MouseButton.Left || AssociatedObject == null || Command == null)
            return;

        // Find the element that was clicked
        var clickedElement = args.OriginalSource as DependencyObject;
        var clickedRow = clickedElement?.GetVisualParent<DataGridRow>();

        if (clickedRow == null)
            return;

        object commandParam = CommandParameter ?? clickedRow.DataContext;

        if (Command.CanExecute(commandParam))
        {
            Command.Execute(commandParam);

            if (ShouldCloseDialog)
            {
                var wind = Window.GetWindow(AssociatedObject);
                wind.DialogResult = true;
                wind.Close();
            }
        }
    }
}