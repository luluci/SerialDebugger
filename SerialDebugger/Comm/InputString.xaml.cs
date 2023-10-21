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

namespace SerialDebugger.Comm
{

    /// <summary>
    /// InputString.xaml の相互作用ロジック
    /// </summary>
    public partial class InputString : Window
    {
        public InputStringViewModel vm;

        public InputString()
        {
            InitializeComponent();
            //
            vm = new InputStringViewModel(this);
            DataContext = vm;
        }

        public void SetFocus()
        {
            InputTextBox.Focus();
            InputTextBox.SelectAll();
        }
        
    }
}
