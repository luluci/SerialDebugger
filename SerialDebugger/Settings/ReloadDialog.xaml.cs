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

namespace SerialDebugger.Settings
{
    /// <summary>
    /// ReloadDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class ReloadDialog : Window
    {
        bool exitTool = false;
        public bool IsAccepted;

        public ReloadDialog()
        {
            IsAccepted = false;

            InitializeComponent();
        }

        public new void ShowDialog()
        {
            IsAccepted = false;
            ((Window)this).ShowDialog();
        }

        public void Dispose()
        {
            exitTool = true;
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!exitTool)
            {
                this.Hide();
                e.Cancel = true;
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            IsAccepted = true;
            this.Hide();
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            IsAccepted = false;
            this.Hide();
        }
    }
}
