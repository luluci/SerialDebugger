using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace SerialDebugger.Comm
{
    using Setting = SerialDebugger.Settings.Settings;
    using SettingGui = SerialDebugger.Settings.Gui;

    class Gui
    {
        // BackupBuffer操作ボタン幅
        public static int[] Button3Width = { 15, 35, 15 };
        // Input列に表示する単位(h)表示幅
        public static int InputColUnitWidth = 15;

        // GUI Resource
        public static SolidColorBrush ColorFrameNameBg = new SolidColorBrush(Color.FromArgb(0xFF, 11, 40, 75));

        // Converter
        public static GuiValueColConverter ColConverter = new GuiValueColConverter();
        public static GuiTxBufferColConverter TxBufConverter = new GuiTxBufferColConverter();
        public static GuiHexEditConverter HexEditConverter = new GuiHexEditConverter();
        public static GuiDecEditConverter DecEditConverter = new GuiDecEditConverter();
        public static GuiTxSendFixNameConverter TxSendFixNameConverter = new GuiTxSendFixNameConverter();
        public static GuiTxSendFixBGColorConverter TxSendFixBGColorConverter = new GuiTxSendFixBGColorConverter();
        public static GuiBitColBgConverter[] BitColBgConverter = new GuiBitColBgConverter[]
        {
            // 2byte
            new GuiBitColBgConverter(0x0000000000000001),
            new GuiBitColBgConverter(0x0000000000000002),
            new GuiBitColBgConverter(0x0000000000000004),
            new GuiBitColBgConverter(0x0000000000000008),
            new GuiBitColBgConverter(0x0000000000000010),
            new GuiBitColBgConverter(0x0000000000000020),
            new GuiBitColBgConverter(0x0000000000000040),
            new GuiBitColBgConverter(0x0000000000000080),
            new GuiBitColBgConverter(0x0000000000000100),
            new GuiBitColBgConverter(0x0000000000000200),
            new GuiBitColBgConverter(0x0000000000000400),
            new GuiBitColBgConverter(0x0000000000000800),
            new GuiBitColBgConverter(0x0000000000001000),
            new GuiBitColBgConverter(0x0000000000002000),
            new GuiBitColBgConverter(0x0000000000004000),
            new GuiBitColBgConverter(0x0000000000008000),
            // 4byte
            new GuiBitColBgConverter(0x0000000000010000),
            new GuiBitColBgConverter(0x0000000000020000),
            new GuiBitColBgConverter(0x0000000000040000),
            new GuiBitColBgConverter(0x0000000000080000),
            new GuiBitColBgConverter(0x0000000000100000),
            new GuiBitColBgConverter(0x0000000000200000),
            new GuiBitColBgConverter(0x0000000000400000),
            new GuiBitColBgConverter(0x0000000000800000),
            new GuiBitColBgConverter(0x0000000001000000),
            new GuiBitColBgConverter(0x0000000002000000),
            new GuiBitColBgConverter(0x0000000004000000),
            new GuiBitColBgConverter(0x0000000008000000),
            new GuiBitColBgConverter(0x0000000010000000),
            new GuiBitColBgConverter(0x0000000020000000),
            new GuiBitColBgConverter(0x0000000040000000),
            new GuiBitColBgConverter(0x0000000080000000),
            // 6byte
            new GuiBitColBgConverter(0x0000000100000000),
            new GuiBitColBgConverter(0x0000000200000000),
            new GuiBitColBgConverter(0x0000000400000000),
            new GuiBitColBgConverter(0x0000000800000000),
            new GuiBitColBgConverter(0x0000001000000000),
            new GuiBitColBgConverter(0x0000002000000000),
            new GuiBitColBgConverter(0x0000004000000000),
            new GuiBitColBgConverter(0x0000008000000000),
            new GuiBitColBgConverter(0x0000010000000000),
            new GuiBitColBgConverter(0x0000020000000000),
            new GuiBitColBgConverter(0x0000040000000000),
            new GuiBitColBgConverter(0x0000080000000000),
            new GuiBitColBgConverter(0x0000100000000000),
            new GuiBitColBgConverter(0x0000200000000000),
            new GuiBitColBgConverter(0x0000400000000000),
            new GuiBitColBgConverter(0x0000800000000000),
            // 8byte
            new GuiBitColBgConverter(0x0001000000000000),
            new GuiBitColBgConverter(0x0002000000000000),
            new GuiBitColBgConverter(0x0004000000000000),
            new GuiBitColBgConverter(0x0008000000000000),
            new GuiBitColBgConverter(0x0010000000000000),
            new GuiBitColBgConverter(0x0020000000000000),
            new GuiBitColBgConverter(0x0040000000000000),
            new GuiBitColBgConverter(0x0080000000000000),
            new GuiBitColBgConverter(0x0100000000000000),
            new GuiBitColBgConverter(0x0200000000000000),
            new GuiBitColBgConverter(0x0400000000000000),
            new GuiBitColBgConverter(0x0800000000000000),
            new GuiBitColBgConverter(0x1000000000000000),
            new GuiBitColBgConverter(0x2000000000000000),
            new GuiBitColBgConverter(0x4000000000000000),
            //new GuiBitColBgConverter(0x8000000000000000),
        };

        static public Settings.SettingInfo setting;

        public static void Init(Settings.SettingInfo setting)
        {
            Gui.setting = setting;
        }


        /// <summary>
        /// キャプション的な部分の部品
        /// </summary>
        /// <param name="tgt"></param>
        /// <returns></returns>
        public static UIElement MakeTextBlockStyle1(string text, int row, int col, int rowspan = -1, int colspan = -1)
        {
            //
            var tb = new TextBlock();
            tb.Text = text;
            tb.Background = SystemColors.ControlBrush;
            tb.TextAlignment = TextAlignment.Center;
            //
            var border = MakeBorder1();
            border.Child = tb;
            Grid.SetRow(border, row);
            Grid.SetColumn(border, col);
            if (rowspan != -1)
            {
                Grid.SetRowSpan(border, rowspan);
            }
            if (colspan != -1)
            {
                Grid.SetColumnSpan(border, colspan);
            }

            return border;
        }

        /// <summary>
        /// キャプション的な部分の部品
        /// </summary>
        /// <param name="tgt"></param>
        /// <returns></returns>
        public static UIElement MakeTextBlockStyleDisable(string text, int row, int col, int rowspan = -1, int colspan = -1)
        {
            //
            var tb = new TextBlock();
            tb.Text = text;
            tb.Background = SystemColors.ControlDarkBrush;
            tb.TextAlignment = TextAlignment.Center;
            //
            var border = MakeBorder1();
            border.Child = tb;
            Grid.SetRow(border, row);
            Grid.SetColumn(border, col);
            if (rowspan != -1)
            {
                Grid.SetRowSpan(border, rowspan);
            }
            if (colspan != -1)
            {
                Grid.SetColumnSpan(border, colspan);
            }

            return border;
        }

        /// <summary>
        /// 強調キャプション的な部分の部品
        /// Frame名称
        /// </summary>
        /// <param name="tgt"></param>
        /// <returns></returns>
        public static UIElement MakeTextBlockStyle2(string text, int row, int col, int rowspan = -1, int colspan = -1)
        {
            //
            var tb = new TextBlock();
            tb.Text = text;
            //tb.Background = SystemColors.ControlLightLightBrush;
            tb.FontSize += 2;
            tb.FontWeight = FontWeights.Bold;
            tb.Foreground = Brushes.White;
            tb.Padding = new Thickness(5, 2, 2, 2);
            //
            var border = MakeBorder1();
            border.CornerRadius = new CornerRadius(9, 9, 0, 0);
            border.Background = ColorFrameNameBg;
            border.BorderBrush = ColorFrameNameBg;
            border.Child = tb;
            Grid.SetRow(border, row);
            Grid.SetColumn(border, col);
            if (rowspan != -1)
            {
                Grid.SetRowSpan(border, rowspan);
            }
            if (colspan != -1)
            {
                Grid.SetColumnSpan(border, colspan);
            }

            return border;
        }

        /// <summary>
        /// Binding設定付きテキストブロック
        /// UInt64型binding
        /// </summary>
        /// <param name="tgt"></param>
        /// <returns></returns>
        public static UIElement MakeTextBlockBindStyle1(Field field, string path, int row, int col, int rowspan = -1, int colspan = -1)
        {
            // binding作成
            var bind = new Binding(path);
            bind.Converter = ColConverter;
            bind.ConverterParameter = field;
            //
            var tb = new TextBlock();
            tb.SetBinding(TextBlock.TextProperty, bind);
            tb.Background = SystemColors.ControlLightLightBrush;
            tb.TextAlignment = TextAlignment.Center;
            /*
            Matrix matrix = (tb.RenderTransform as MatrixTransform).Matrix;
            matrix.Rotate(-90);
            tb.RenderTransform = new MatrixTransform(matrix);
            tb.Height = 20;
            //tb.Width = 200;
            var tfg = new TransformGroup();
            tfg.Children.Add(new RotateTransform(-90));
            tb.RenderTransformOrigin = new Point(0,-1);
            tb.RenderTransform = tfg;
            */
            //
            var border = MakeBorder1();
            border.Child = tb;
            Grid.SetRow(border, row);
            Grid.SetColumn(border, col);
            if (rowspan != -1)
            {
                Grid.SetRowSpan(border, rowspan);
            }
            if (colspan != -1)
            {
                Grid.SetColumnSpan(border, colspan);
            }

            return border;
        }

        /// <summary>
        /// Binding設定付きテキストブロック
        /// byte型binding
        /// </summary>
        /// <param name="tgt"></param>
        /// <returns></returns>
        public static UIElement MakeTextBlockBindStyle2(string path, int row, int col, int rowspan = -1, int colspan = -1)
        {
            // binding作成
            var bind = new Binding(path);
            bind.Converter = TxBufConverter;
            //
            var tb = new TextBlock();
            tb.SetBinding(TextBlock.TextProperty, bind);
            tb.Background = SystemColors.ControlLightLightBrush;
            tb.TextAlignment = TextAlignment.Center;
            /*
            Matrix matrix = (tb.RenderTransform as MatrixTransform).Matrix;
            matrix.Rotate(-90);
            tb.RenderTransform = new MatrixTransform(matrix);
            tb.Height = 20;
            //tb.Width = 200;
            var tfg = new TransformGroup();
            tfg.Children.Add(new RotateTransform(-90));
            tb.RenderTransformOrigin = new Point(0,-1);
            tb.RenderTransform = tfg;
            */
            //
            var border = MakeBorder1();
            border.Child = tb;
            Grid.SetRow(border, row);
            Grid.SetColumn(border, col);
            if (rowspan != -1)
            {
                Grid.SetRowSpan(border, rowspan);
            }
            if (colspan != -1)
            {
                Grid.SetColumnSpan(border, colspan);
            }

            return border;
        }

        /// <summary>
        /// Binding設定付きBit表示テキストブロック
        /// 該当ビットが立っていたら背景色を変える
        /// </summary>
        /// <param name="tgt"></param>
        /// <returns></returns>
        public static UIElement MakeTextBlockBindStyle2(Field field, string name, string path, int bit, int row, int col, int rowspan = -1, int colspan = -1)
        {
            // binding作成
            var bind = new Binding(path);
            bind.Converter = BitColBgConverter[bit];
            //
            var tb = new TextBlock();
            tb.Text = name;
            tb.TextAlignment = TextAlignment.Center;
            tb.SetBinding(TextBlock.BackgroundProperty, bind);
            //
            var border = MakeBorder1();
            border.Child = tb;
            Grid.SetRow(border, row);
            Grid.SetColumn(border, col);
            if (rowspan != -1)
            {
                Grid.SetRowSpan(border, rowspan);
            }
            if (colspan != -1)
            {
                Grid.SetColumnSpan(border, colspan);
            }

            return border;
        }

        public static UIElement ApplyGridStyle(Grid tgt)
        {
            tgt.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x0, 0x0));
            //
            var border = MakeBorder1();
            border.Child = tgt;
            return border;
        }

        public static Border MakeBorder1()
        {
            var border = new Border();
            border.BorderThickness = new Thickness(1);
            border.BorderBrush = Brushes.DarkSlateGray;
            return border;
        }

        /// <summary>
        /// Input GUI作成
        /// </summary>
        /// <param name="path"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="rowspan"></param>
        /// <param name="colspan"></param>
        /// <returns></returns>
        public static UIElement MakeInputGui(Field field, string path, int row, int col, int rowspan = -1, int colspan = -1)
        {
            switch (field.InputType)
            {
                case Field.InputModeType.Dict:
                case Field.InputModeType.Unit:
                case Field.InputModeType.Time:
                case Field.InputModeType.Script:
                    return MakeInputGuiSelecter(field, field, path, row, col, rowspan, colspan);
                case Field.InputModeType.Edit:
                    return MakeInputGuiEdit(field, field, path, row, col, rowspan, colspan);
                case Field.InputModeType.Checksum:
                    return MakeInputGuiEdit(field, field, path, row, col, rowspan, colspan);
                case Field.InputModeType.Fix:
                default:
                    return MakeTextBlockStyle1("<FIX>", row, col, rowspan, colspan);
            }
        }

        /// <summary>
        /// Binding設定付きTextBox
        /// </summary>
        /// <param name="tgt"></param>
        /// <returns></returns>
        public static UIElement MakeInputGuiEdit(Field field, Object param, string path, int row, int col, int rowspan = -1, int colspan = -1)
        {
            var tb = MakeInputGuiTextBox(param, path, field.InputBase, row, col, rowspan, colspan);
            //
            var border = MakeBorder1();
            border.Child = tb;
            Grid.SetRow(border, row);
            Grid.SetColumn(border, col);
            if (rowspan != -1)
            {
                Grid.SetRowSpan(border, rowspan);
            }
            if (colspan != -1)
            {
                Grid.SetColumnSpan(border, colspan);
            }

            var bind_bgcolor = new Binding(path + ".ChangeState.Value");
            bind_bgcolor.Converter = TxSendFixBGColorConverter;
            border.SetBinding(Border.BorderBrushProperty, bind_bgcolor);

            return border;
        }

        /// <summary>
        /// Binding設定付きTextBox
        /// </summary>
        /// <param name="tgt"></param>
        /// <returns></returns>
        public static UIElement MakeInputGuiTextBox(Object param, string path, int input_base, int row, int col, int rowspan = -1, int colspan = -1)
        {
            // ベース作成
            var sp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Top
            };

            // binding作成
            var bind = new Binding(path + ".Value.Value");
            switch (input_base)
            {
                case 10:
                    bind.Converter = DecEditConverter;
                    break;

                default:
                    bind.Converter = HexEditConverter;
                    break;
            }
            bind.ConverterParameter = param;
            //
            var tb = new TextBox();
            tb.SetBinding(TextBox.TextProperty, bind);
            tb.Background = SystemColors.ControlLightLightBrush;
            tb.TextAlignment = TextAlignment.Center;
            // 表示幅
            switch (input_base)
            {
                case 10:
                    tb.Width = Gui.setting.Gui.ColWidth[(int)SettingGui.Col.FieldInput];
                    break;

                default:
                    tb.Width = Gui.setting.Gui.ColWidth[(int)SettingGui.Col.FieldInput] - InputColUnitWidth;
                    break;
            }
            //
            sp.Children.Add(tb);

            // 単位表示
            switch (input_base)
            {
                case 10:
                    // なし
                    break;

                default:
                    // h表示
                    var text = new TextBlock
                    {
                        Text = "h"
                    };
                    //
                    sp.Children.Add(text);
                    break;
            }

            return sp;
        }

        /// <summary>
        /// Binding設定付き選択コンボボックスGUI作成
        /// </summary>
        /// <param name="tgt"></param>
        /// <returns></returns>
        public static UIElement MakeInputGuiSelecter(Field field, Object param, string path, int row, int col, int rowspan = -1, int colspan = -1)
        {
            UIElement gui_ptr;
            // 2行(2bit)以上の領域があれば直接編集GUI追加
            if (field.BitSize > 1)
            {
                // ベース作成
                var sp = new StackPanel();
                // TextBox作成
                var tb = MakeInputGuiTextBox(param, path, field.InputBase, row, col, rowspan, colspan);
                //
                sp.Children.Add(tb);

                // ComboBox作成
                var cb = MakeInputGuiComboBox(param, path, row, col, rowspan, colspan);
                sp.Children.Add(cb);

                gui_ptr = sp;
            }
            else
            {
                // ComboBox作成
                var cb = MakeInputGuiComboBox(param, path, row, col, rowspan, colspan);

                gui_ptr = cb;
            }
            //
            var border = MakeBorder1();
            border.Child = gui_ptr;
            Grid.SetRow(border, row);
            Grid.SetColumn(border, col);
            if (rowspan != -1)
            {
                Grid.SetRowSpan(border, rowspan);
            }
            if (colspan != -1)
            {
                Grid.SetColumnSpan(border, colspan);
            }

            var bind_bgcolor = new Binding(path + ".ChangeState.Value");
            bind_bgcolor.Converter = TxSendFixBGColorConverter;
            border.SetBinding(Border.BorderBrushProperty, bind_bgcolor);

            return border;
        }

        /// <summary>
        /// Binding設定付きComboBox
        /// </summary>
        /// <param name="tgt"></param>
        /// <returns></returns>
        public static UIElement MakeInputGuiComboBox(Object field, string path, int row, int col, int rowspan = -1, int colspan = -1)
        {
            // binding作成
            var bind_itemsrc = new Binding(path + ".Selects");
            var bind_selectidx = new Binding(path + ".SelectIndexSelects.Value");
            //
            var cb = new ComboBox();
            cb.SetBinding(ComboBox.ItemsSourceProperty, bind_itemsrc);
            cb.SetBinding(ComboBox.SelectedIndexProperty, bind_selectidx);
            cb.DisplayMemberPath = "Disp";
            cb.SelectedValuePath = "Value";
            //cb.Background = SystemColors.ControlLightLightBrush;

            return cb;
        }

    }
}
