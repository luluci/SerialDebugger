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
            vm = new MainWindowViewModel(this);
            this.DataContext = vm;

            /*
            //
            {
                var f = new Comm.TxFrame("Frame_A", 2);
                f.Add(new Comm.TxField("field1_1", 8, 0x01));
                f.Add(new Comm.TxField(
                    new Comm.TxField.InnerField[] {
                        new Comm.TxField.InnerField("field1_2_status", 5),
                        new Comm.TxField.InnerField("field1_2_error", 3),
                    },
                    0x55, Comm.TxField.SelectModeType.Edit));
                f.Add(new Comm.TxField("field2", 4, 0x02, Comm.TxField.SelectModeType.Edit));
                f.Add(new Comm.TxField("field3", 7, 0x04, Comm.TxField.SelectModeType.Unit, new Comm.TxField.Selecter("hPa", 10, 1100, 850, 0, "F1")));
                f.Add(new Comm.TxField("field4", 9, 0x0F));
                f.Add(new Comm.TxField(
                    "field5", 1, 0xFF,
                    Comm.TxField.SelectModeType.Dict,
                    new Comm.TxField.Selecter( new (UInt64,string)[]
                        {
                            (0, "OFF"),
                            (1, "ON"),
                            (2, "Err"),
                            (0xFF, "Err"),
                        })
                    ));
                f.Add(new Comm.TxField("field6", 11, 0xAA));
                f.Add(new Comm.TxField("Checksum", 8, 1, 6, type:Comm.TxField.SelectModeType.Checksum));
                f.Build();
                vm.TxFrames.Add(f);
            }
            {
                var f = new Comm.TxFrame("Frame_B", 0);
                f.Add(new Comm.TxField("field1", 1, 0));
                f.Add(new Comm.TxField("field2", 2, 0));
                f.Add(new Comm.TxField("field3", 3, 0));
                f.Add(new Comm.TxField("field4", 4, 0));
                f.Add(new Comm.TxField("field5", 5, 0));
                f.Add(new Comm.TxField("field6", 6, 0));
                f.Build();
                vm.TxFrames.Add(f);
            }
            {
                var f = new Comm.TxFrame("Frame_C", 4);
                f.Add(new Comm.TxField("field1", 1, 0));
                f.Add(new Comm.TxField("field2", 2, 0));
                f.Add(new Comm.TxField("field3", 3, 0));
                f.Add(new Comm.TxField("field4", 4, 0));
                f.Add(new Comm.TxField("field5", 5, 0));
                f.Add(new Comm.TxField("field6", 6, 0));
                f.Build();
                vm.TxFrames.Add(f);
            }
            var i = Comm.Settings.ok();
            var setting = new Comm.SettingsImpl();
            // GUI構築する
            Comm.TxGui.Make(BaseSerialTx, vm.TxFrames);
            */
        }
        
    }
}
