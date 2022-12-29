using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace SerialDebugger.Comm
{
    /// <summary>
    /// TxFrameからGUIを動的に生成する
    /// </summary>
    class TxGui
    {
        // Frame 列幅 (Byte, Bit, Value, Name, Select, Buffer)
        public static int[] FrameColWidth = { 25, 25, 40, 80, 80, 80 };
        public static int[] Button3Width = { 15, 35, 15 };

        // Converter
        private static TxGuiValueColConverter ColConverter = new TxGuiValueColConverter();
        private static TxGuiEditConverter EditConverter = new TxGuiEditConverter();
        private static TxGuiBitColBgConverter[] BitColBgConverter = new TxGuiBitColBgConverter[]
        {
            new TxGuiBitColBgConverter(0x0001),
            new TxGuiBitColBgConverter(0x0002),
            new TxGuiBitColBgConverter(0x0004),
            new TxGuiBitColBgConverter(0x0008),
            new TxGuiBitColBgConverter(0x0010),
            new TxGuiBitColBgConverter(0x0020),
            new TxGuiBitColBgConverter(0x0040),
            new TxGuiBitColBgConverter(0x0080),
            new TxGuiBitColBgConverter(0x0100),
            new TxGuiBitColBgConverter(0x0200),
            new TxGuiBitColBgConverter(0x0400),
            new TxGuiBitColBgConverter(0x0800),
            new TxGuiBitColBgConverter(0x1000),
            new TxGuiBitColBgConverter(0x2000),
            new TxGuiBitColBgConverter(0x4000),
            new TxGuiBitColBgConverter(0x8000),
        };

        public static void Make(UIElement parent, ICollection<TxFrame> frames)
        {
            int frame_no = 0;
            double margin_l = 0;
            foreach (var frame in frames)
            {
                var (grid1, grid2, grid3) = MakeBase((IAddChild)parent, margin_l);
                var w = MakeHeader(grid2, frame);
                w = MakeBody(grid3, frame, frame_no);

                //margin_l += (grid1.Width + 50);
                margin_l += (w + 50);
                frame_no++;
            }
        }

        /// <summary>
        /// parent上にTxFrameGUIのベースになるGrid(2x1)(header/body)を作成する
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        private static (Grid, Grid, Grid) MakeBase(IAddChild parent, double margin_l)
        {
            // ベースGrid作成
            Grid grid = new Grid();
            // 2x1
            var col = new ColumnDefinition();
            grid.ColumnDefinitions.Add(col);
            var row_header = new RowDefinition();
            var row_body = new RowDefinition();
            row_header.Height = GridLength.Auto;
            grid.RowDefinitions.Add(row_header);
            grid.RowDefinitions.Add(row_body);
            // Margin
            grid.Margin = new Thickness(margin_l, 10, 0, 10);
            grid.HorizontalAlignment = HorizontalAlignment.Left;

            // ベースGrid内初期化
            // headerGrid作成
            var grid_header = new Grid();
            {
                Grid.SetRow(grid_header, 0);
                Grid.SetColumn(grid_header, 0);
                grid.Children.Add(grid_header);
            }
            // bodyGrid作成
            var grid_body = new Grid();
            {
                var grid_outer_body = new Grid();
                Grid.SetRow(grid_outer_body, 1);
                Grid.SetColumn(grid_outer_body, 0);
                grid.Children.Add(grid_outer_body);
                //
                var scrl_body = new ScrollViewer();
                grid_outer_body.Children.Add(scrl_body);
                //
                scrl_body.Content = grid_body;
            }

            // ベースGrid登録
            parent.AddChild(grid);

            return (grid, grid_header, grid_body);
        }

        private static int MakeHeader(Grid grid, TxFrame frame)
        {
            int width = 0;
            {
                // 2x(5+buff_size)
                var col_byte = new ColumnDefinition();
                var col_bit = new ColumnDefinition();
                var col_value = new ColumnDefinition();
                var col_name = new ColumnDefinition();
                var col_select = new ColumnDefinition();
                col_byte.Width = new GridLength(FrameColWidth[0]);
                col_bit.Width = new GridLength(FrameColWidth[1]);
                col_value.Width = new GridLength(FrameColWidth[2]);
                col_name.Width = new GridLength(FrameColWidth[3]);
                col_select.Width = new GridLength(FrameColWidth[4]);
                grid.ColumnDefinitions.Add(col_byte);
                grid.ColumnDefinitions.Add(col_bit);
                grid.ColumnDefinitions.Add(col_value);
                grid.ColumnDefinitions.Add(col_name);
                grid.ColumnDefinitions.Add(col_select);
                for (int i = 0; i < frame.BufferLength; i++)
                {
                    var col = new ColumnDefinition();
                    col.Width = new GridLength(FrameColWidth[5]);
                    grid.ColumnDefinitions.Add(col);
                }
                var row_1 = new RowDefinition();
                var row_2 = new RowDefinition();
                row_1.Height = GridLength.Auto;
                row_2.Height = GridLength.Auto;
                grid.RowDefinitions.Add(row_1);
                grid.RowDefinitions.Add(row_2);
                // Width作成
                for (int i=0; i<5; i++)
                {
                    width += FrameColWidth[i];
                }
                for (int i = 0; i < frame.BufferLength; i++)
                {
                    width += FrameColWidth[5];
                }
            }
            // Frame名称作成
            grid.Children.Add(MakeTextBlockStyle2(frame.Name, 0, 0, -1, 5));
            // column作成: byte
            grid.Children.Add(MakeTextBlockStyle1("Byte", 1, 0));
            // column作成: bit
            grid.Children.Add(MakeTextBlockStyle1("Bit", 1, 1));
            // column作成: value
            grid.Children.Add(MakeTextBlockStyle1("Value", 1, 2));
            // column作成: name
            grid.Children.Add(MakeTextBlockStyle1("Name", 1, 3));
            // column作成: select
            grid.Children.Add(MakeTextBlockStyle1("Select", 1, 4));
            // Buffer列作成
            {
                // 送信ボタン作成
                // Buffer[0]は各fieldと連動
                grid.Children.Add(MakeButtonStyle1("Send", 0, 5));
                grid.Children.Add(MakeTextBlockStyle1(frame.Buffer[0].Name, 1, 5));
                // Buffer[1～]は[0]の保存/展開により値を決定
                for (int i = 1; i < frame.BufferLength; i++)
                {
                    // 送信ボタン作成
                    //grid.Children.Add(MakeButtonStyle1("Send", 0, 5+i));
                    grid.Children.Add(MakeButtonLoadStore("Send", 0, 5 + i));
                    // 表示ラベル
                    grid.Children.Add(MakeTextBlockStyle1(frame.Buffer[i].Name, 1, 5 + i));
                }
            }

            return width;
        }

        private static int MakeBody(Grid grid, TxFrame frame, int frame_no)
        {
            int bitlength = frame.Length * 8;
            int width = 0;
            {
                // 2x(5+buff_size)
                // Columns
                var col_byte = new ColumnDefinition();
                var col_bit = new ColumnDefinition();
                var col_value = new ColumnDefinition();
                var col_name = new ColumnDefinition();
                var col_select = new ColumnDefinition();
                col_byte.Width = new GridLength(FrameColWidth[0]);
                col_bit.Width = new GridLength(FrameColWidth[1]);
                col_value.Width = new GridLength(FrameColWidth[2]);
                col_name.Width = new GridLength(FrameColWidth[3]);
                col_select.Width = new GridLength(FrameColWidth[4]);
                grid.ColumnDefinitions.Add(col_byte);
                grid.ColumnDefinitions.Add(col_bit);
                grid.ColumnDefinitions.Add(col_value);
                grid.ColumnDefinitions.Add(col_name);
                grid.ColumnDefinitions.Add(col_select);
                for (int i = 0; i < frame.BufferLength; i++)
                {
                    var col = new ColumnDefinition();
                    col.Width = new GridLength(FrameColWidth[5]);
                    grid.ColumnDefinitions.Add(col);
                }
                // Rows
                for (int bit=0; bit<bitlength; bit++)
                {
                    var row = new RowDefinition();
                    row.Height = GridLength.Auto;
                    grid.RowDefinitions.Add(row);
                }
                // Width作成
                for (int i = 0; i < 5; i++)
                {
                    width += FrameColWidth[i];
                }
                for (int i = 0; i < frame.BufferLength; i++)
                {
                    width += FrameColWidth[5];
                }
            }
            {
                // 通信フレーム作成
                int bit_rest = 0;
                int bit_pos = 0;
                int byte_pos = 0;
                int field_pos = 0;
                bool is_byte = false;
                for (int bit = 0; bit < bitlength; bit++)
                {
                    // Byte列作成
                    is_byte = ((bit % 8) == 0);
                    if (bit_pos == 0)
                    {
                        grid.Children.Add(MakeTextBlockStyle1($"{byte_pos}", bit, 0, 8));
                    }
                    // field情報作成
                    if (bit_rest == 0)
                    {
                        if (field_pos < frame.Fields.Count)
                        {
                            var field = frame.Fields[field_pos];
                            // Bit列作成
                            if (is_byte && (field.BitSize == 8))
                            {
                                // バイト境界に配置、かつ、バイト単位データのとき
                                // バイト単位でまとめて表示
                                grid.Children.Add(MakeTextBlockStyle1($"-", bit, 1, field.BitSize));
                            }
                            else
                            {
                                // その他は各ビット情報を出力
                                for (int i=0; i<field.BitSize; i++)
                                {
                                    grid.Children.Add(MakeTextBlockBindStyle2(field, $"{bit + i}", $"TxFrames[{frame_no}].Fields[{field_pos}].Value.Value", i, bit+i, 1));
                                }
                            }
                            // Value列作成
                            grid.Children.Add(MakeTextBlockBindStyle1($"TxFrames[{frame_no}].Fields[{field_pos}].Value.Value", bit, 2, field.BitSize));
                            // Name列作成
                            grid.Children.Add(MakeTextBlockStyle3(field.Name, bit, 3, field.BitSize));
                            // Select列作成
                            grid.Children.Add(MakeSelectGui(field, $"TxFrames[{frame_no}].Fields[{field_pos}]", bit, 4, field.BitSize));

                            // 次周回設定処理
                            field_pos++;
                            bit_rest = field.BitSize;
                        }
                        else
                        {
                            // このパスはTxFieldの指定がきっかりバイト単位でないときに入る
                            // 端数ビットを埋める
                            // 残りビット数
                            bit_rest = bitlength - bit;
                            // Bit列作成
                            for (int i = 0; i < bit_rest; i++)
                            {
                                grid.Children.Add(MakeTextBlockStyle1($"{bit + i}", bit+i, 1));
                            }
                            // Value列作成
                            grid.Children.Add(MakeTextBlockStyle1("-", bit, 2, bit_rest));
                            // Name列作成
                            grid.Children.Add(MakeTextBlockStyle1("-", bit, 3, bit_rest));
                            // Select列作成
                            grid.Children.Add(MakeTextBlockStyle1("-", bit, 4, bit_rest));
                        }
                    }
                    //
                    bit_rest--;
                    //
                    bit_pos++;
                    if (bit_pos >= 8)
                    {
                        bit_pos = 0;
                        byte_pos++;
                    }
                }

            }

            return width;
        }

        /// <summary>
        /// キャプション的な部分の部品
        /// </summary>
        /// <param name="tgt"></param>
        /// <returns></returns>
        private static UIElement MakeButtonStyle1(string text, int row, int col, int rowspan = -1, int colspan = -1)
        {
            /*
            var btn = new Button();
            btn.Content = text;
            //
            var border = MakeBorder1();
            border.Child = btn;
            border.CornerRadius = new CornerRadius(4);
            border.Margin = new Thickness(2, 1, 2, 1);
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
            */
            var btn = new Button();
            btn.Content = text;
            btn.Margin = new Thickness(10, 2, 10, 2);
            Grid.SetRow(btn, row);
            Grid.SetColumn(btn, col);
            if (rowspan != -1)
            {
                Grid.SetRowSpan(btn, rowspan);
            }
            if (colspan != -1)
            {
                Grid.SetColumnSpan(btn, colspan);
            }

            return btn;
        }

        /// <summary>
        /// キャプション的な部分の部品
        /// 3連ボタン
        /// </summary>
        /// <param name="tgt"></param>
        /// <returns></returns>
        private static UIElement MakeButtonLoadStore(string text, int row, int col, int rowspan = -1, int colspan = -1)
        {
            var btn_store = new Button();
            btn_store.Content = "←";
            btn_store.Margin = new Thickness(2, 1, 2, 1);
            btn_store.Width = Button3Width[0];

            var btn = new Button();
            btn.Content = text;
            btn.Margin = new Thickness(2, 1, 2, 1);
            btn.Width = Button3Width[1];

            var btn_save = new Button();
            btn_save.Content = "↓";
            btn_save.Margin = new Thickness(2, 1, 1, 1);
            btn_save.Width = Button3Width[2];

            var sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;
            Grid.SetRow(sp, row);
            Grid.SetColumn(sp, col);
            if (rowspan != -1)
            {
                Grid.SetRowSpan(sp, rowspan);
            }
            if (colspan != -1)
            {
                Grid.SetColumnSpan(sp, colspan);
            }
            sp.Children.Add(btn_store);
            sp.Children.Add(btn);
            sp.Children.Add(btn_save);

            return sp;
        }

        /// <summary>
        /// キャプション的な部分の部品
        /// </summary>
        /// <param name="tgt"></param>
        /// <returns></returns>
        private static UIElement MakeTextBlockStyle1(string text, int row, int col, int rowspan = -1, int colspan = -1)
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
        /// 強調キャプション的な部分の部品
        /// Frame名称
        /// </summary>
        /// <param name="tgt"></param>
        /// <returns></returns>
        private static UIElement MakeTextBlockStyle2(string text, int row, int col, int rowspan = -1, int colspan = -1)
        {
            //
            var tb = new TextBlock();
            tb.Text = text;
            tb.Background = SystemColors.ControlLightLightBrush;
            tb.FontSize += 2;
            tb.Padding = new Thickness(5, 2, 2, 2);
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
        /// Field名称
        /// </summary>
        /// <param name="tgt"></param>
        /// <returns></returns>
        private static UIElement MakeTextBlockStyle3(string text, int row, int col, int rowspan = -1, int colspan = -1)
        {
            //
            var tb = new TextBlock();
            tb.Text = text;
            tb.Background = SystemColors.ControlLightLightBrush;
            tb.FontSize += 3;
            tb.TextWrapping = TextWrapping.Wrap;
            tb.Padding = new Thickness(5, 2, 2, 2);
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
        /// </summary>
        /// <param name="tgt"></param>
        /// <returns></returns>
        private static UIElement MakeTextBlockBindStyle1(string path, int row, int col, int rowspan = -1, int colspan = -1)
        {
            // binding作成
            var bind = new Binding(path);
            bind.Converter = ColConverter;
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
        private static UIElement MakeTextBlockBindStyle2(TxField field, string name, string path, int bit, int row, int col, int rowspan = -1, int colspan = -1)
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

        private static UIElement ApplyGridStyle(Grid tgt)
        {
            tgt.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x0, 0x0));
            //
            var border = MakeBorder1();
            border.Child = tgt;
            return border;
        }

        private static Border MakeBorder1()
        {
            var border = new Border();
            border.BorderThickness = new Thickness(1);
            border.BorderBrush = Brushes.DarkSlateGray;
            return border;
        }

        /// <summary>
        /// Select GUI作成
        /// </summary>
        /// <param name="path"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="rowspan"></param>
        /// <param name="colspan"></param>
        /// <returns></returns>
        private static UIElement MakeSelectGui(TxField field, string path, int row, int col, int rowspan = -1, int colspan = -1)
        {
            switch (field.SelectType)
            {
                case TxField.SelectModeType.Edit:
                    return MakeSelectGuiTextBox(field, path, row, col, rowspan, colspan);
                case TxField.SelectModeType.Fix:
                default:
                    return MakeTextBlockStyle1("<FIX>", row, col, rowspan, colspan);
            }
        }

        /// <summary>
        /// Binding設定付きTextBox
        /// </summary>
        /// <param name="tgt"></param>
        /// <returns></returns>
        private static UIElement MakeSelectGuiTextBox(TxField field, string path, int row, int col, int rowspan = -1, int colspan = -1)
        {
            // binding作成
            var bind = new Binding(path + ".Value.Value");
            bind.Converter = EditConverter;
            bind.ConverterParameter = field;
            //
            var tb = new TextBox();
            tb.SetBinding(TextBox.TextProperty, bind);
            tb.Background = SystemColors.ControlLightLightBrush;
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

    }
}
