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

            // Logger設定
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

                //await Script.Interpreter.Engine.EvalTest();
                //await Script.Interpreter.Engine.EvalTest();
                //await Script.Interpreter.Engine.EvalInit("key = i * 2 + (i%2 == 0 ? 0x80 : 0x00);   value = key + ' h';");
                //var result = await Script.Interpreter.Engine.EvalExec(0);
                //result = await Script.Interpreter.Engine.EvalExec(1);
                //result = await Script.Interpreter.Engine.EvalExec(2);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"init exception: {ex.Message}");
                Logger.AddException(ex, "初期化時エラー:");
            }
        }
    }
}
