using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace SerialDebugger.Serial
{
    /// <summary>
    /// TxFrameからGUIを動的に生成する
    /// </summary>
    class TxGui
    {
        // Frame 列幅 (Byte, Bit, Value, Name, Select, Buffer)
        public static int[] FrameColWidth = { 25, 25, 25, 80, 80, 50 };

        public static void Make(UIElement parent, ICollection<TxFrame> frames)
        {
            double margin_l = 0;
            foreach (var frame in frames)
            {
                var (grid1, grid2, grid3) = MakeBase((IAddChild)parent, margin_l);
                var w = MakeHeader(grid2, frame);
                w = MakeBody(grid3, frame);

                //margin_l += (grid1.Width + 50);
                margin_l += (w + 50);
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
                for (int i = 0; i < frame.BufferLength; i++)
                {
                    // 送信ボタン作成
                    grid.Children.Add(MakeButtonStyle1("Send", 0, 5+i));
                    // 表示ラベル
                    grid.Children.Add(MakeTextBlockStyle1(frame.Buffer[i].Name, 1, 5 + i));
                }
            }

            return width;
        }

        private static int MakeBody(Grid grid, TxFrame frame)
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
                    if (is_byte)
                    {
                        grid.Children.Add(MakeTextBlockStyle1($"{byte_pos}", bit, 0, 8));
                    }
                    // field情報作成
                    if (bit_rest == 0)
                    {
                        if (field_pos < frame.Frame.Count)
                        {
                            var field = frame.Frame[field_pos];
                            // Bit列作成
                            if (is_byte && (field.BitSize == 8))
                            {
                                // バイト境界に配置、かつ、1バイトデータのとき
                                // 1バイト単位でまとめて表示
                                grid.Children.Add(MakeTextBlockStyle1($"-", bit, 1, field.BitSize));
                            }
                            else
                            {
                                // その他は各ビット情報を出力
                                for (int i=0; i<field.BitSize; i++)
                                {
                                    grid.Children.Add(MakeTextBlockStyle1($"{bit + i}", bit+i, 1));
                                }
                            }
                            // Value列作成
                            // Name列作成
                            {
                                var tb = new TextBlock();
                                tb.Text = field.Name;
                                Grid.SetRowSpan(tb, field.BitSize);
                                Grid.SetRow(tb, bit);
                                Grid.SetColumn(tb, 3);
                                grid.Children.Add(tb);
                            }
                            // Select列作成

                            // 次周回設定処理
                            field_pos++;
                            bit_rest = field.BitSize;
                            bit_pos += field.BitSize;
                            if (bit_pos >= 8)
                            {
                                byte_pos += (bit_pos-1) / 8;
                                bit_pos = (bit_pos-1) % 8;
                            }
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
                            // Name列作成
                            {
                                grid.Children.Add(MakeTextBlockStyle1("", bit, 3, bit_rest));
                            }
                        }
                    }
                    //
                    bit_rest--;
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
            var btn = new Button();
            btn.Content = text;
            //
            var border = MakeBorder1();
            border.Child = btn;
            border.CornerRadius = new CornerRadius(4);
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
    }
}
