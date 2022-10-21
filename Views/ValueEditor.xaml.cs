using CommunityToolkit.Mvvm.ComponentModel;
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

namespace CelSerEngine.Views
{
    public partial class ValueEditor : Window
    {
        public string Value { get; private set; } = string.Empty;

        public ValueEditor()
        {
            InitializeComponent();
        }

        public void SetValueTextBox(string value)
        {
            valueTxtBox.Text = value;
        }

        public void SetFocusTextBox()
        {
            valueTxtBox.SelectionStart = valueTxtBox.Text.Length;
            valueTxtBox.Focus();
        }

        private void OkBtn_Click(object sender, RoutedEventArgs e)
        {
            Value = valueTxtBox.Text;
            DialogResult = true;
            Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Value = string.Empty;
            DialogResult = false;
            Close();
        }
    }
}
