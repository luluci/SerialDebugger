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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SerialDebugger
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindowViewModel vm;

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                // vmを参照するので明示的に持っておく.
                vm = new MainWindowViewModel(this);
                this.DataContext = vm;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"init exception: {ex.Message}");
            }
        }

    }
}
