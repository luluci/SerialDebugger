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
    using Logger = Log.Log;

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindowViewModel vm;

        public MainWindow()
        {
            InitializeComponent();

            // Logger初期化
            // 例外処理で使用するため最初に初期化しておく
            // グローバルインスタンスの管理はViewModelで行う
            Logger.Init(this);

            // vmを参照するので明示的に持っておく.
            vm = new MainWindowViewModel(this);
            this.DataContext = vm;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Scripter設定
                // 通信定義GUI作成に使用するので先に初期化すること
                await Script.Interpreter.Init();
                // 通信定義GUI作成
                await vm.InitAsync();
            }
            catch (Exception ex)
            {
                Logger.AddException(ex, "初期化時エラー:");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var window = sender as Window;

            //ViewModelがインターフェイスを実装していたらメソッドを実行する
            if (window.DataContext is IClosing)
                e.Cancel = (window.DataContext as IClosing).OnClosing();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // Window破棄の前に明示的にDisposeする
            // GCに任せるとデストラクタが走らない
            (vm as IDisposable)?.Dispose();
        }
    }
}
