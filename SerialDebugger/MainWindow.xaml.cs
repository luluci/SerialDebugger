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

            // vmを参照するので明示的に持っておく.
            vm = new MainWindowViewModel();
            this.DataContext = vm;

            //
            {
                var f = new Serial.TxFrame("Frame_A", 2);
                f.Add(new Serial.TxField("field1", 8, 0));
                f.Add(new Serial.TxField("field2", 4, 0));
                f.Add(new Serial.TxField("field3", 7, 0));
                f.Add(new Serial.TxField("field4", 9, 0));
                f.Add(new Serial.TxField("field5", 1, 0));
                f.Add(new Serial.TxField("field6", 11, 0));
                f.Build();
                vm.TxFrames.Add(f);
            }
            {
                var f = new Serial.TxFrame("Frame_B", 3);
                f.Add(new Serial.TxField("field1", 1, 0));
                f.Add(new Serial.TxField("field2", 2, 0));
                f.Add(new Serial.TxField("field3", 3, 0));
                f.Add(new Serial.TxField("field4", 4, 0));
                f.Add(new Serial.TxField("field5", 5, 0));
                f.Add(new Serial.TxField("field6", 6, 0));
                f.Build();
                vm.TxFrames.Add(f);
            }
            {
                var f = new Serial.TxFrame("Frame_C", 4);
                f.Add(new Serial.TxField("field1", 1, 0));
                f.Add(new Serial.TxField("field2", 2, 0));
                f.Add(new Serial.TxField("field3", 3, 0));
                f.Add(new Serial.TxField("field4", 4, 0));
                f.Add(new Serial.TxField("field5", 5, 0));
                f.Add(new Serial.TxField("field6", 6, 0));
                f.Build();
                vm.TxFrames.Add(f);
            }
            // GUI構築する
            Serial.TxGui.Make(BaseSerialTx, vm.TxFrames);
        }
    }
}
