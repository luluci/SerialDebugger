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
    using Setting = SerialDebugger.Settings.Settings;
    using SettingGui = SerialDebugger.Settings.Gui;

    /// <summary>
    /// TxFrameからGUIを動的に生成する
    /// </summary>
    class TxGui
    {
        // Frame 列幅 (Byte, Bit, Value, Name, Select, Buffer, space, BackupBuffer)
        public static int[] Button3Width = { 15, 35, 15 };
        // Input列に表示する単位(h)表示幅
        public static int InputColUnitWidth = 15;

        // GUI Resource
        public static SolidColorBrush ColorFrameNameBg = new SolidColorBrush(Color.FromArgb(0xFF, 11, 40, 75));

        // Converter
        private static TxGuiValueColConverter ColConverter = new TxGuiValueColConverter();
        private static TxGuiTxBufferColConverter TxBufConverter = new TxGuiTxBufferColConverter();
        private static TxGuiEditConverter EditConverter = new TxGuiEditConverter();
        private static TxGuiBitColBgConverter[] BitColBgConverter = new TxGuiBitColBgConverter[]
        {
            // 2byte
            new TxGuiBitColBgConverter(0x0000000000000001),
            new TxGuiBitColBgConverter(0x0000000000000002),
            new TxGuiBitColBgConverter(0x0000000000000004),
            new TxGuiBitColBgConverter(0x0000000000000008),
            new TxGuiBitColBgConverter(0x0000000000000010),
            new TxGuiBitColBgConverter(0x0000000000000020),
            new TxGuiBitColBgConverter(0x0000000000000040),
            new TxGuiBitColBgConverter(0x0000000000000080),
            new TxGuiBitColBgConverter(0x0000000000000100),
            new TxGuiBitColBgConverter(0x0000000000000200),
            new TxGuiBitColBgConverter(0x0000000000000400),
            new TxGuiBitColBgConverter(0x0000000000000800),
            new TxGuiBitColBgConverter(0x0000000000001000),
            new TxGuiBitColBgConverter(0x0000000000002000),
            new TxGuiBitColBgConverter(0x0000000000004000),
            new TxGuiBitColBgConverter(0x0000000000008000),
            // 4byte
            new TxGuiBitColBgConverter(0x0000000000010000),
            new TxGuiBitColBgConverter(0x0000000000020000),
            new TxGuiBitColBgConverter(0x0000000000040000),
            new TxGuiBitColBgConverter(0x0000000000080000),
            new TxGuiBitColBgConverter(0x0000000000100000),
            new TxGuiBitColBgConverter(0x0000000000200000),
            new TxGuiBitColBgConverter(0x0000000000400000),
            new TxGuiBitColBgConverter(0x0000000000800000),
            new TxGuiBitColBgConverter(0x0000000001000000),
            new TxGuiBitColBgConverter(0x0000000002000000),
            new TxGuiBitColBgConverter(0x0000000004000000),
            new TxGuiBitColBgConverter(0x0000000008000000),
            new TxGuiBitColBgConverter(0x0000000010000000),
            new TxGuiBitColBgConverter(0x0000000020000000),
            new TxGuiBitColBgConverter(0x0000000040000000),
            new TxGuiBitColBgConverter(0x0000000080000000),
            // 6byte
            new TxGuiBitColBgConverter(0x0000000100000000),
            new TxGuiBitColBgConverter(0x0000000200000000),
            new TxGuiBitColBgConverter(0x0000000400000000),
            new TxGuiBitColBgConverter(0x0000000800000000),
            new TxGuiBitColBgConverter(0x0000001000000000),
            new TxGuiBitColBgConverter(0x0000002000000000),
            new TxGuiBitColBgConverter(0x0000004000000000),
            new TxGuiBitColBgConverter(0x0000008000000000),
            new TxGuiBitColBgConverter(0x0000010000000000),
            new TxGuiBitColBgConverter(0x0000020000000000),
            new TxGuiBitColBgConverter(0x0000040000000000),
            new TxGuiBitColBgConverter(0x0000080000000000),
            new TxGuiBitColBgConverter(0x0000100000000000),
            new TxGuiBitColBgConverter(0x0000200000000000),
            new TxGuiBitColBgConverter(0x0000400000000000),
            new TxGuiBitColBgConverter(0x0000800000000000),
            // 8byte
            new TxGuiBitColBgConverter(0x0001000000000000),
            new TxGuiBitColBgConverter(0x0002000000000000),
            new TxGuiBitColBgConverter(0x0004000000000000),
            new TxGuiBitColBgConverter(0x0008000000000000),
            new TxGuiBitColBgConverter(0x0010000000000000),
            new TxGuiBitColBgConverter(0x0020000000000000),
            new TxGuiBitColBgConverter(0x0040000000000000),
            new TxGuiBitColBgConverter(0x0080000000000000),
            new TxGuiBitColBgConverter(0x0100000000000000),
            new TxGuiBitColBgConverter(0x0200000000000000),
            new TxGuiBitColBgConverter(0x0400000000000000),
            new TxGuiBitColBgConverter(0x0800000000000000),
            new TxGuiBitColBgConverter(0x1000000000000000),
            new TxGuiBitColBgConverter(0x2000000000000000),
            new TxGuiBitColBgConverter(0x4000000000000000),
            new TxGuiBitColBgConverter(0x8000000000000000),
        };

        public static void Make(UIElement parent, ICollection<TxFrame> frames)
        {
            int frame_no = 0;
            double margin_l = 0;
            foreach (var frame in frames)
            {
                var (grid1, grid2, grid3) = MakeBase((IAddChild)parent, margin_l);
                var w = MakeHeader(grid2, frame, frame_no);
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

        private static int MakeHeader(Grid grid, TxFrame frame, int frame_no)
        {
            int width = 0;
            {
                // 2x(5+buff_size)
                var col_byte = new ColumnDefinition();
                var col_bit = new ColumnDefinition();
                var col_value = new ColumnDefinition();
                var col_name = new ColumnDefinition();
                var col_input = new ColumnDefinition();
                var col_txdata = new ColumnDefinition();
                col_byte.Width = new GridLength(Setting.Data.Gui.ColWidth[(int)SettingGui.Col.ByteIndex]);
                col_bit.Width = new GridLength(Setting.Data.Gui.ColWidth[(int)SettingGui.Col.BitIndex]);
                col_value.Width = new GridLength(Setting.Data.Gui.ColWidth[(int)SettingGui.Col.FieldValue]);
                col_name.Width = new GridLength(Setting.Data.Gui.ColWidth[(int)SettingGui.Col.FieldName]);
                col_input.Width = new GridLength(Setting.Data.Gui.ColWidth[(int)SettingGui.Col.FieldInput]);
                col_txdata.Width = new GridLength(Setting.Data.Gui.ColWidth[(int)SettingGui.Col.TxBytes]);
                grid.ColumnDefinitions.Add(col_byte);
                grid.ColumnDefinitions.Add(col_bit);
                grid.ColumnDefinitions.Add(col_value);
                grid.ColumnDefinitions.Add(col_name);
                grid.ColumnDefinitions.Add(col_input);
                grid.ColumnDefinitions.Add(col_txdata);
                // BackupBufferを持つ場合はスペースを少し開けてGUI作成
                if (frame.BackupBufferLength > 0)
                {
                    var col_space = new ColumnDefinition();
                    col_space.Width = new GridLength(Setting.Data.Gui.ColWidth[(int)SettingGui.Col.Spacer]);
                    grid.ColumnDefinitions.Add(col_space);
                    for (int i = 0; i < frame.BackupBufferLength; i++)
                    {
                        var col = new ColumnDefinition();
                        col.Width = new GridLength(Setting.Data.Gui.ColWidth[(int)SettingGui.Col.TxBuffer]);
                        grid.ColumnDefinitions.Add(col);
                    }
                }
                var row_1 = new RowDefinition();
                var row_2 = new RowDefinition();
                row_1.Height = GridLength.Auto;
                row_2.Height = GridLength.Auto;
                grid.RowDefinitions.Add(row_1);
                grid.RowDefinitions.Add(row_2);
                // Width作成
                for (int i = 0; i < (int)SettingGui.Col.Spacer; i++)
                {
                    width += Setting.Data.Gui.ColWidth[i];
                }
                // BackupBufferを持つ場合はスペースを少し開けてGUI作成
                if (frame.BackupBufferLength > 0)
                {
                    width += Setting.Data.Gui.ColWidth[(int)SettingGui.Col.Spacer];
                    for (int i = 0; i < frame.BackupBufferLength; i++)
                    {
                        width += Setting.Data.Gui.ColWidth[(int)SettingGui.Col.TxBuffer];
                    }
                }
            }
            // Frame名称作成
            grid.Children.Add(MakeTextBlockStyle2(frame.Name, 0, 0, -1, 5));
            // column作成: byte
            grid.Children.Add(MakeTextBlockStyle1("Byte", 1, Setting.Data.Gui.ColOrder[(int)SettingGui.Col.ByteIndex]));
            // column作成: bit
            grid.Children.Add(MakeTextBlockStyle1("Bit", 1, Setting.Data.Gui.ColOrder[(int)SettingGui.Col.BitIndex]));
            // column作成: value
            grid.Children.Add(MakeTextBlockStyle1("Value", 1, Setting.Data.Gui.ColOrder[(int)SettingGui.Col.FieldValue]));
            // column作成: name
            grid.Children.Add(MakeTextBlockStyle1("Name", 1, Setting.Data.Gui.ColOrder[(int)SettingGui.Col.FieldName]));
            // column作成: input
            grid.Children.Add(MakeTextBlockStyle1("Select", 1, Setting.Data.Gui.ColOrder[(int)SettingGui.Col.FieldInput]));
            // Buffer列作成
            {
                // 送信ボタン作成
                // Bufferは各fieldと連動
                grid.Children.Add(MakeButtonStyle1($"TxFrames[{frame_no}].TxBuffer", "Send", 0, Setting.Data.Gui.ColOrder[(int)SettingGui.Col.TxBytes]));
                grid.Children.Add(MakeTextBlockStyle1("TxData", 1, Setting.Data.Gui.ColOrder[(int)SettingGui.Col.TxBytes]));
            }
            // BackupBufferを持つ場合はスペースを少し開けてGUI作成
            if (frame.BackupBufferLength > 0)
            {
                // BackupBuffer列作成
                // BackupBufferはBufferの保存/展開により値を決定
                for (int i = 0; i < frame.BackupBufferLength; i++)
                {
                    // 送信ボタン作成
                    grid.Children.Add(MakeButtonLoadStore(frame.BackupBuffer[i], $"TxFrames[{frame_no}].BackupBuffer[{i}]", "Send", 0, Setting.Data.Gui.ColOrder[(int)SettingGui.Col.TxBuffer] + i));
                    // 表示ラベル
                    grid.Children.Add(MakeTextBlockStyle1(frame.BackupBuffer[i].Name, 1, Setting.Data.Gui.ColOrder[(int)SettingGui.Col.TxBuffer] + i));
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
                var col_input = new ColumnDefinition();
                var col_txdata = new ColumnDefinition();
                col_byte.Width = new GridLength(Setting.Data.Gui.ColWidth[(int)SettingGui.Col.ByteIndex]);
                col_bit.Width = new GridLength(Setting.Data.Gui.ColWidth[(int)SettingGui.Col.BitIndex]);
                col_value.Width = new GridLength(Setting.Data.Gui.ColWidth[(int)SettingGui.Col.FieldValue]);
                col_name.Width = new GridLength(Setting.Data.Gui.ColWidth[(int)SettingGui.Col.FieldName]);
                col_input.Width = new GridLength(Setting.Data.Gui.ColWidth[(int)SettingGui.Col.FieldInput]);
                col_txdata.Width = new GridLength(Setting.Data.Gui.ColWidth[(int)SettingGui.Col.TxBytes]);
                grid.ColumnDefinitions.Add(col_byte);
                grid.ColumnDefinitions.Add(col_bit);
                grid.ColumnDefinitions.Add(col_value);
                grid.ColumnDefinitions.Add(col_name);
                grid.ColumnDefinitions.Add(col_input);
                grid.ColumnDefinitions.Add(col_txdata);
                // BackupBufferを持つ場合はスペースを少し開けてGUI作成
                if (frame.BackupBufferLength > 0)
                {
                    var col_space = new ColumnDefinition();
                    col_space.Width = new GridLength(Setting.Data.Gui.ColWidth[(int)SettingGui.Col.Spacer]);
                    grid.ColumnDefinitions.Add(col_space);
                    for (int i = 0; i < frame.BackupBufferLength; i++)
                    {
                        var col = new ColumnDefinition();
                        col.Width = new GridLength(Setting.Data.Gui.ColWidth[(int)SettingGui.Col.TxBuffer]);
                        grid.ColumnDefinitions.Add(col);
                    }
                }
                // Rows
                for (int bit=0; bit<bitlength; bit++)
                {
                    var row = new RowDefinition();
                    row.Height = GridLength.Auto;
                    grid.RowDefinitions.Add(row);
                }
                // Width作成
                for (int i = 0; i < (int)SettingGui.Col.Spacer; i++)
                {
                    width += Setting.Data.Gui.ColWidth[i];
                }
                // BackupBufferを持つ場合はスペースを少し開けてGUI作成
                if (frame.BackupBufferLength > 0)
                {
                    width += Setting.Data.Gui.ColWidth[(int)SettingGui.Col.Spacer];
                    for (int i = 0; i < frame.BackupBufferLength; i++)
                    {
                        width += Setting.Data.Gui.ColWidth[(int)SettingGui.Col.TxBuffer];
                    }
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
                        grid.Children.Add(MakeTextBlockStyle1($"{byte_pos}", bit, Setting.Data.Gui.ColOrder[(int)SettingGui.Col.ByteIndex], 8));
                    }
                    // field情報作成
                    if (bit_rest == 0)
                    {
                        if (field_pos < frame.Fields.Count)
                        {
                            var field = frame.Fields[field_pos];
                            // Bit列作成
                            if (is_byte && (field.BitSize % 8) == 0 && (field.InnerFields.Count == 1))
                            {
                                // バイト境界に配置、かつ、バイト単位データのとき、かつ、フィールド内で名前分割しないとき、
                                // バイト単位でまとめて表示
                                grid.Children.Add(MakeTextBlockStyle1($"-", bit, Setting.Data.Gui.ColOrder[(int)SettingGui.Col.BitIndex], field.BitSize));
                            }
                            else
                            {
                                // その他は各ビット情報を出力
                                for (int i=0; i<field.BitSize; i++)
                                {
                                    grid.Children.Add(MakeTextBlockBindStyle2(field, $"{bit + i}", $"TxFrames[{frame_no}].Fields[{field_pos}].Value.Value", i, bit+i, Setting.Data.Gui.ColOrder[(int)SettingGui.Col.BitIndex]));
                                }
                            }
                            // Value列作成
                            grid.Children.Add(MakeTextBlockBindStyle1($"TxFrames[{frame_no}].Fields[{field_pos}].Value.Value", bit, Setting.Data.Gui.ColOrder[(int)SettingGui.Col.FieldValue], field.BitSize));
                            // Name列作成
                            int inner_idx = 0;
                            foreach (var inner in field.InnerFields)
                            {
                                grid.Children.Add(MakeTextBlockStyle3(inner.Name, bit+inner_idx, Setting.Data.Gui.ColOrder[(int)SettingGui.Col.FieldName], inner.BitSize));
                                inner_idx += inner.BitSize;
                            }
                            //grid.Children.Add(MakeTextBlockStyle3(field.Name, bit, 3, field.BitSize));
                            // Input列作成
                            grid.Children.Add(MakeSelectGui(field, $"TxFrames[{frame_no}].Fields[{field_pos}]", bit, Setting.Data.Gui.ColOrder[(int)SettingGui.Col.FieldInput], field.BitSize));
                            // BackupBuffer列作成
                            for (int i = 0; i < frame.BackupBufferLength; i++)
                            {
                                grid.Children.Add(MakeBackupBufferGui(field, frame.BackupBuffer[i], $"TxFrames[{frame_no}].BackupBuffer[{i}].Disp[{field_pos}]", bit, Setting.Data.Gui.ColOrder[(int)SettingGui.Col.TxBuffer] + i, field.BitSize));
                            }

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
                                grid.Children.Add(MakeTextBlockStyle1($"{bit + i}", bit+i, Setting.Data.Gui.ColOrder[(int)SettingGui.Col.BitIndex]));
                            }
                            // Value列作成
                            grid.Children.Add(MakeTextBlockStyle1("-", bit, Setting.Data.Gui.ColOrder[(int)SettingGui.Col.FieldValue], bit_rest));
                            // Name列作成
                            grid.Children.Add(MakeTextBlockStyle1("-", bit, Setting.Data.Gui.ColOrder[(int)SettingGui.Col.FieldName], bit_rest));
                            // Input列作成
                            grid.Children.Add(MakeTextBlockStyle1("-", bit, Setting.Data.Gui.ColOrder[(int)SettingGui.Col.FieldInput], bit_rest));
                            // BackupBuffer列作成
                            for (int i = 0; i < frame.BackupBufferLength; i++)
                            {
                                grid.Children.Add(MakeTextBlockStyle1("-", bit, Setting.Data.Gui.ColOrder[(int)SettingGui.Col.TxBuffer] + i, bit_rest));
                            }
                        }
                    }
                    // 送信バイトシーケンス
                    if (bit_pos == 0)
                    {
                        grid.Children.Add(MakeTextBlockBindStyle2($"TxFrames[{frame_no}].TxBuffer[{byte_pos}]", bit, Setting.Data.Gui.ColOrder[(int)SettingGui.Col.TxBytes], 8));
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
        private static UIElement MakeButtonStyle1(string buff_path, string text, int row, int col, int rowspan = -1, int colspan = -1)
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
            btn.Margin = new Thickness(5, 2, 5, 2);
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

            // binding
            var bind_cmnd = new Binding("OnClickTxDataSend");
            var bind_param = new Binding(buff_path);
            btn.SetBinding(Button.CommandProperty, bind_cmnd);
            btn.SetBinding(Button.CommandParameterProperty, bind_param);

            return btn;
        }

        /// <summary>
        /// キャプション的な部分の部品
        /// 3連ボタン
        /// </summary>
        /// <param name="tgt"></param>
        /// <returns></returns>
        private static UIElement MakeButtonLoadStore(TxBackupBuffer buffer, string path, string text, int row, int col, int rowspan = -1, int colspan = -1)
        {

            var bind_store = new Binding(path + ".OnClickStore");
            var btn_store = new Button();
            btn_store.Content = "←";
            btn_store.Margin = new Thickness(2, 1, 2, 1);
            btn_store.Width = Button3Width[0];
            btn_store.SetBinding(Button.CommandProperty, bind_store);

            var btn = new Button();
            btn.Content = text;
            btn.Margin = new Thickness(2, 1, 2, 1);
            btn.Width = Button3Width[1];

            var bind_save = new Binding(path + ".OnClickSave");
            var btn_save = new Button();
            btn_save.Content = "↓";
            btn_save.Margin = new Thickness(2, 1, 1, 1);
            btn_save.Width = Button3Width[2];
            btn_save.SetBinding(Button.CommandProperty, bind_save);

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
        /// UInt64型binding
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
        /// Binding設定付きテキストブロック
        /// byte型binding
        /// </summary>
        /// <param name="tgt"></param>
        /// <returns></returns>
        private static UIElement MakeTextBlockBindStyle2(string path, int row, int col, int rowspan = -1, int colspan = -1)
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
                case TxField.SelectModeType.Dict:
                case TxField.SelectModeType.Unit:
                    return MakeSelectGuiSelecter(field, path, row, col, rowspan, colspan);
                case TxField.SelectModeType.Edit:
                    return MakeSelectGuiEdit(field, path, row, col, rowspan, colspan);
                case TxField.SelectModeType.Checksum:
                    return MakeSelectGuiEdit(field, path, row, col, rowspan, colspan);
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
        private static UIElement MakeSelectGuiEdit(TxField field, string path, int row, int col, int rowspan = -1, int colspan = -1)
        {
            var tb = MakeSelectGuiTextBox(field, path, row, col, rowspan, colspan);
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
        /// Binding設定付きTextBox
        /// </summary>
        /// <param name="tgt"></param>
        /// <returns></returns>
        private static UIElement MakeSelectGuiTextBox(TxField field, string path, int row, int col, int rowspan = -1, int colspan = -1)
        {
            // ベース作成
            var sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;
            sp.VerticalAlignment = VerticalAlignment.Top;

            // binding作成
            var bind = new Binding(path + ".Value.Value");
            bind.Converter = EditConverter;
            bind.ConverterParameter = field;
            //
            var tb = new TextBox();
            tb.SetBinding(TextBox.TextProperty, bind);
            tb.Background = SystemColors.ControlLightLightBrush;
            tb.TextAlignment = TextAlignment.Center;
            tb.Width = Setting.Data.Gui.ColWidth[(int)SettingGui.Col.FieldInput] - InputColUnitWidth;
            //
            sp.Children.Add(tb);

            // h表示
            var text = new TextBlock();
            text.Text = "h";
            //
            sp.Children.Add(text);

            return sp;
        }

        /// <summary>
        /// Binding設定付き選択コンボボックスGUI作成
        /// </summary>
        /// <param name="tgt"></param>
        /// <returns></returns>
        private static UIElement MakeSelectGuiSelecter(TxField field, string path, int row, int col, int rowspan = -1, int colspan = -1)
        {
            UIElement gui_ptr;
            // 2行(2bit)以上の領域があれば直接編集GUI追加
            if (field.BitSize > 1)
            {
                // ベース作成
                var sp = new StackPanel();
                // TextBox作成
                var tb = MakeSelectGuiTextBox(field, path, row, col, rowspan, colspan);
                //
                sp.Children.Add(tb);

                // ComboBox作成
                var cb = MakeSelectGuiComboBox(field, path, row, col, rowspan, colspan);
                sp.Children.Add(cb);

                gui_ptr = sp;
            }
            else
            {
                // ComboBox作成
                var cb = MakeSelectGuiComboBox(field, path, row, col, rowspan, colspan);

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

            return border;
        }

        /// <summary>
        /// Binding設定付きComboBox
        /// </summary>
        /// <param name="tgt"></param>
        /// <returns></returns>
        private static UIElement MakeSelectGuiComboBox(TxField field, string path, int row, int col, int rowspan = -1, int colspan = -1)
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

        /// <summary>
        /// Select GUI作成
        /// </summary>
        /// <param name="path"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="rowspan"></param>
        /// <param name="colspan"></param>
        /// <returns></returns>
        private static UIElement MakeBackupBufferGui(TxField field, TxBackupBuffer buffer, string path, int row, int col, int rowspan = -1, int colspan = -1)
        {
            // binding作成
            var bind = new Binding(path);
            //
            var tb = new TextBlock();
            tb.SetBinding(TextBlock.TextProperty, bind);
            tb.Background = SystemColors.ControlBrush;
            tb.TextAlignment = TextAlignment.Center;
            tb.TextWrapping = TextWrapping.Wrap;
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
