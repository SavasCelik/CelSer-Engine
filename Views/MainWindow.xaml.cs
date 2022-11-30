using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CelSerEngine.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void listView1_ColumnClick(object sender, MouseButtonEventArgs e)
        {
            var myListView = sender as ListView;
            var te = myListView.InputHitTest(e.GetPosition(myListView)) as TextBlock;
            var te2 = myListView.InputHitTest(e.GetPosition(myListView));
            //MessageBox.Show("Column " + e.Column.ToString() + " Clicked");
        }
    }
}
