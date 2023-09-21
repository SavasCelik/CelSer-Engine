using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CelSerEngine.Wpf.Views;
/// <summary>
/// Interaction logic for ScriptEditorWindow.xaml
/// </summary>
public partial class ScriptEditorWindow : Window
{
    public ScriptEditorWindow()
    {
        InitializeComponent();
    }

    public void SetText(string text)
    {
        textEditor.Text = text;
    }
}
