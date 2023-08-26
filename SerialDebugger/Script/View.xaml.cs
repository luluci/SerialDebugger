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

namespace SerialDebugger.Script
{
    /// <summary>
    /// View.xaml の相互作用ロジック
    /// </summary>
    public partial class View : Window
    {
        public ViewViewModel vm;

        public View()
        {
            vm = new ViewViewModel(this);
            DataContext = vm;


            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var window = sender as View;

            // ViewModelがインターフェイスを実装していたらメソッドを実行する
            // WebView2用ウインドウはClose()するとWebView2も破棄されてしまうため、
            // アプリ動作中は閉じるボタンでClose()せずにHide()する。
            // ViewViewModelのDisposeはMainWindowViewModelで管理し、
            // Dispose()の後にウインドウをClose()する
            if (vm is IClosing)
                e.Cancel = (vm as IClosing).OnClosing();
        }
        
    }
}
