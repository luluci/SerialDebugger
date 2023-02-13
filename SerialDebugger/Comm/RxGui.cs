using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace SerialDebugger.Comm
{
    using Setting = SerialDebugger.Settings.Settings;
    using SettingGui = SerialDebugger.Settings.Gui;

    class RxGui
    {
        static int[] ColOrder = new int[]
        {
            0,      // Byteインデックス表示列
            1,       // Bitインデックス表示列
            -1,     // Field設定値表示列
            2,      // Field名表示列
            -1,     // Field値入力列
            -1,        // 送信データシーケンス表示列
            -1,         // スペース列
            3,       // 送信データバッファ
        };

        public static Grid Make()
        {
            // ベースGrid作成
            Grid grid = new Grid();

            // 先頭要素を選択
            var frames = Gui.setting.Comm.Rx;

            int frame_no = 0;
            double margin_l = 0;
            foreach (var frame in frames)
            {
                var (grid1, grid2, grid3) = MakeBase((IAddChild)grid, margin_l);
                var w = MakeHeader(Gui.setting, grid2, frame, frame_no);
                w = MakeBody(Gui.setting, grid3, frame, frame_no);

                //margin_l += (grid1.Width + 50);
                margin_l += (w + 50);
                frame_no++;
            }

            return grid;
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

        private static int MakeHeader(Settings.SettingInfo setting, Grid grid, RxFrame frame, int frame_no)
        {
            int width = 0;
            {
                // 列作成
                // Field定義はbyte/bit/nameを表示
                var col_byte = new ColumnDefinition();
                var col_bit = new ColumnDefinition();
                var col_name = new ColumnDefinition();
                col_byte.Width = new GridLength(setting.Gui.ColWidth[(int)SettingGui.Col.ByteIndex]);
                col_bit.Width = new GridLength(setting.Gui.ColWidth[(int)SettingGui.Col.BitIndex]);
                col_name.Width = new GridLength(setting.Gui.ColWidth[(int)SettingGui.Col.FieldName]);
                grid.ColumnDefinitions.Add(col_byte);
                grid.ColumnDefinitions.Add(col_bit);
                grid.ColumnDefinitions.Add(col_name);
                // 各Patternの列
                foreach (var pattern in frame.Patterns)
                {
                    var col_space = new ColumnDefinition();
                    grid.ColumnDefinitions.Add(col_space);
                    col_space.Width = new GridLength(setting.Gui.ColWidth[(int)SettingGui.Col.Spacer]);
                    // bit/input/valueを表示
                    var col_ptn_bit = new ColumnDefinition();
                    var col_ptn_disp = new ColumnDefinition();
                    var col_ptn_byte = new ColumnDefinition();
                    grid.ColumnDefinitions.Add(col_ptn_bit);
                    grid.ColumnDefinitions.Add(col_ptn_disp);
                    grid.ColumnDefinitions.Add(col_ptn_byte);
                    col_ptn_bit.Width = new GridLength(setting.Gui.ColWidth[(int)SettingGui.Col.BitIndex]);
                    col_ptn_disp.Width = new GridLength(setting.Gui.ColWidth[(int)SettingGui.Col.FieldInput]);
                    col_ptn_byte.Width = new GridLength(setting.Gui.ColWidth[(int)SettingGui.Col.TxBytes]);
                }
                
                // 行作成
                var row_1 = new RowDefinition();
                var row_2 = new RowDefinition();
                row_1.Height = GridLength.Auto;
                row_2.Height = GridLength.Auto;
                grid.RowDefinitions.Add(row_1);
                grid.RowDefinitions.Add(row_2);

                // Width作成
                width += setting.Gui.ColWidth[(int)SettingGui.Col.ByteIndex];
                width += setting.Gui.ColWidth[(int)SettingGui.Col.BitIndex];
                width += setting.Gui.ColWidth[(int)SettingGui.Col.FieldName];
                foreach (var pattern in frame.Patterns)
                {
                    width += setting.Gui.ColWidth[(int)SettingGui.Col.Spacer];
                    width += setting.Gui.ColWidth[(int)SettingGui.Col.BitIndex];
                    width += setting.Gui.ColWidth[(int)SettingGui.Col.FieldInput];
                    width += setting.Gui.ColWidth[(int)SettingGui.Col.TxBytes];
                }
            }

            // Frame名称作成
            string name;
            if (Setting.Data.Comm.DisplayId)
            {
                name = $"[{frame.Id}] {frame.Name}";
            }
            else
            {
                name = frame.Name;
            }
            grid.Children.Add(Gui.MakeTextBlockStyle2(name, 0, 0, -1, 3));
            // column作成: byte
            grid.Children.Add(Gui.MakeTextBlockStyle1("Byte", 1, ColOrder[(int)SettingGui.Col.ByteIndex]));
            // column作成: bit
            grid.Children.Add(Gui.MakeTextBlockStyle1("Bit", 1, ColOrder[(int)SettingGui.Col.BitIndex]));
            // column作成: name
            grid.Children.Add(Gui.MakeTextBlockStyle1("Name", 1, ColOrder[(int)SettingGui.Col.FieldName]));
            // Pattern列作成
            int col_offset = 0;
            for (int i = 0; i < frame.Patterns.Count; i++)
            {
                var pattern = frame.Patterns[i];
                col_offset = ColOrder[(int)SettingGui.Col.TxBuffer] + (4 * i);
                // 有効無効切り替えチェックボックス/表示ラベル
                grid.Children.Add(MakeCheckboxEnable(pattern, $"RxFrames[{frame_no}].Patterns[{i}]", 0, col_offset+1, -1, 3));
                // column作成: bit
                grid.Children.Add(Gui.MakeTextBlockStyle1("Bit", 1, col_offset + 1));
                // column作成: input
                grid.Children.Add(Gui.MakeTextBlockStyle1("Pattern", 1, col_offset + 2));
                // column作成: value
                grid.Children.Add(Gui.MakeTextBlockStyle1("Match", 1, col_offset + 3));
            }
            
            return width;
        }

        private static int MakeBody(Settings.SettingInfo setting, Grid grid, RxFrame frame, int frame_no)
        {
            int bitlength = frame.DispMaxLength * 8;
            int width = 0;
            {
                // 列作成
                // Field定義はbyte/bit/nameを表示
                var col_byte = new ColumnDefinition();
                var col_bit = new ColumnDefinition();
                var col_name = new ColumnDefinition();
                col_byte.Width = new GridLength(setting.Gui.ColWidth[(int)SettingGui.Col.ByteIndex]);
                col_bit.Width = new GridLength(setting.Gui.ColWidth[(int)SettingGui.Col.BitIndex]);
                col_name.Width = new GridLength(setting.Gui.ColWidth[(int)SettingGui.Col.FieldName]);
                grid.ColumnDefinitions.Add(col_byte);
                grid.ColumnDefinitions.Add(col_bit);
                grid.ColumnDefinitions.Add(col_name);
                // 各Patternの列
                foreach (var pattern in frame.Patterns)
                {
                    var col_space = new ColumnDefinition();
                    grid.ColumnDefinitions.Add(col_space);
                    col_space.Width = new GridLength(setting.Gui.ColWidth[(int)SettingGui.Col.Spacer]);
                    // bit/valueを表示
                    var col_ptn_bit = new ColumnDefinition();
                    var col_ptn_disp = new ColumnDefinition();
                    var col_ptn_byte = new ColumnDefinition();
                    grid.ColumnDefinitions.Add(col_ptn_bit);
                    grid.ColumnDefinitions.Add(col_ptn_disp);
                    grid.ColumnDefinitions.Add(col_ptn_byte);
                    col_ptn_bit.Width = new GridLength(setting.Gui.ColWidth[(int)SettingGui.Col.BitIndex]);
                    col_ptn_disp.Width = new GridLength(setting.Gui.ColWidth[(int)SettingGui.Col.FieldInput]);
                    col_ptn_byte.Width = new GridLength(setting.Gui.ColWidth[(int)SettingGui.Col.TxBytes]);
                }
                // Rows
                for (int bit = 0; bit < bitlength; bit++)
                {
                    var row = new RowDefinition();
                    row.Height = GridLength.Auto;
                    grid.RowDefinitions.Add(row);
                }
                // Width作成
                width += setting.Gui.ColWidth[(int)SettingGui.Col.ByteIndex];
                width += setting.Gui.ColWidth[(int)SettingGui.Col.BitIndex];
                width += setting.Gui.ColWidth[(int)SettingGui.Col.FieldName];
                foreach (var pattern in frame.Patterns)
                {
                    width += setting.Gui.ColWidth[(int)SettingGui.Col.Spacer];
                    width += setting.Gui.ColWidth[(int)SettingGui.Col.BitIndex];
                    width += setting.Gui.ColWidth[(int)SettingGui.Col.FieldInput];
                    width += setting.Gui.ColWidth[(int)SettingGui.Col.TxBytes];
                }
            }
            // RxFrame GUI表示
            MakeBodyFrame(setting, grid, frame, frame_no);
            MakeBodyPattern(setting, grid, frame, frame_no);
            
            return width;
        }

        private static void MakeBodyFrame(Settings.SettingInfo setting, Grid grid, RxFrame frame, int frame_no)
        {
            int bitlength = frame.Length * 8;
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
                    grid.Children.Add(Gui.MakeTextBlockStyle1($"{byte_pos}", bit, ColOrder[(int)SettingGui.Col.ByteIndex], 8));
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
                            grid.Children.Add(Gui.MakeTextBlockStyle1($"-", bit, ColOrder[(int)SettingGui.Col.BitIndex], field.BitSize));
                        }
                        else
                        {
                            // その他は各ビット情報を出力
                            for (int i = 0; i < field.BitSize; i++)
                            {
                                grid.Children.Add(Gui.MakeTextBlockBindBitData(field, $"{bit + i}", $"RxFrames[{frame_no}].Fields[{field_pos}].InitValue", i, bit + i, ColOrder[(int)SettingGui.Col.BitIndex]));
                            }
                        }
                        // Name列作成
                        int inner_idx = 0;
                        foreach (var inner in field.InnerFields)
                        {
                            string name;
                            if (Setting.Data.Comm.DisplayId)
                            {
                                name = $"[{field.Id}] {inner.Name}";
                            }
                            else
                            {
                                name = inner.Name;
                            }
                            grid.Children.Add(MakeNameGui(field, $"RxFrames[{frame_no}].Fields[{field_pos}]", name, bit + inner_idx, ColOrder[(int)SettingGui.Col.FieldName], inner.BitSize));
                            inner_idx += inner.BitSize;
                        }

                        // 次周回設定処理
                        field_pos++;
                        bit_rest = field.BitSize;
                    }
                    else
                    {
                        // このパスはFieldの指定がきっかりバイト単位でないときに入る
                        // 端数ビットを埋める
                        // 残りビット数
                        bit_rest = bitlength - bit;
                        // Bit列作成
                        for (int i = 0; i < bit_rest; i++)
                        {
                            grid.Children.Add(Gui.MakeTextBlockStyle1($"{bit + i}", bit + i, ColOrder[(int)SettingGui.Col.BitIndex]));
                        }
                        // Name列作成
                        grid.Children.Add(Gui.MakeTextBlockStyle1("-", bit, ColOrder[(int)SettingGui.Col.FieldName], bit_rest));
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

        private static void MakeBodyPattern(Settings.SettingInfo setting, Grid grid, RxFrame frame, int frame_no)
        {
            int bitlength = frame.DispMaxLength * 8;
            int col_offset = 0;

            // Pattern列作成
            for (int ptn_idx = 0; ptn_idx < frame.Patterns.Count; ptn_idx++)
            {
                var pattern = frame.Patterns[ptn_idx];
                col_offset = ColOrder[(int)SettingGui.Col.TxBuffer] + (4 * ptn_idx);
                int col_bit = col_offset + 1;
                int col_disp = col_offset + 2;
                int col_byte = col_offset + 3;

                // Pattern表示
                int use_bit_no = 0;
                int use_bit_pos = 0;
                int bit_no = 0;
                int bit_pos = 0;
                int col_width = 1;
                int bit_size = 0;
                // バイト境界フラグ
                bool is_byte_align = false;
                // バイト表示可否フラグ
                bool is_byte_disp = false;
                for (int match_idx = 0; match_idx < pattern.Matches.Count; match_idx++)
                {
                    //
                    var match = pattern.Matches[match_idx];
                    //
                    if (match.FieldRef is null)
                    {
                        bit_size = 0;
                    }
                    else
                    {
                        bit_size = match.FieldRef.BitSize;
                    }
                    is_byte_align = (bit_no % 8) == 0;
                    is_byte_disp = is_byte_align && ((bit_size % 8) == 0);
                    // Bit列作成
                    switch (match.Type)
                    {
                        case RxMatchType.Any:
                            if (is_byte_disp)
                            {
                                grid.Children.Add(Gui.MakeTextBlockStyle1($"-", bit_pos, col_bit, match.FieldRef.BitSize));
                            }
                            else
                            {
                                for (int bit = 0; bit < match.FieldRef.BitSize; bit++)
                                {
                                    grid.Children.Add(Gui.MakeTextBlockStyleDisable($"{bit_no + bit}", bit_pos + bit, col_bit));
                                }
                            }
                            use_bit_no = match.FieldRef.BitSize;
                            use_bit_pos = match.FieldRef.BitSize;
                            col_width = 1;
                            break;

                        case RxMatchType.Value:
                            for (int bit = 0; bit < match.FieldRef.BitSize; bit++)
                            {
                                grid.Children.Add(Gui.MakeTextBlockBindBitData(match.FieldRef, $"{bit_no + bit}", $"RxFrames[{frame_no}].Patterns[{ptn_idx}].Matches[{match_idx}].Value", bit, bit_pos + bit, col_bit));
                            }
                            use_bit_no = match.FieldRef.BitSize;
                            use_bit_pos = match.FieldRef.BitSize;
                            col_width = 1;
                            break;

                        default:
                        case RxMatchType.Timeout:
                        case RxMatchType.Script:
                        case RxMatchType.ActivateAutoTx:
                        case RxMatchType.ActivateRx:
                            // バイト単位でまとめて表示
                            grid.Children.Add(Gui.MakeTextBlockStyle1($"-", bit_pos, col_bit));
                            use_bit_no = 0;
                            use_bit_pos = 1;
                            col_width = 2;
                            break;
                    }
                    // Pattern値表示
                    grid.Children.Add(MakeTextBlockRecv(match, $"RxFrames[{frame_no}].Patterns[{ptn_idx}].Matches[{match_idx}]", bit_pos, col_disp, use_bit_pos, col_width));
                    //
                    bit_no += use_bit_no;
                    bit_pos += use_bit_pos;
                }
                // bit残りの表示ケア
                int bit_rest = bit_no % 8;
                if (bit_rest > 0)
                {
                    bit_rest = 8 - bit_rest;
                    // bit表示
                    for (int bit = 0; bit < bit_rest; bit++)
                    {
                        grid.Children.Add(Gui.MakeTextBlockStyleDisable($"{bit_no + bit}", bit_pos + bit, col_bit));
                    }
                    // Pattern値表示
                    grid.Children.Add(Gui.MakeTextBlockStyle1("<any>", bit_pos, col_disp, bit_rest));
                }
                // Analyze/Match値表示
                bit_pos = 0;
                for (int idx = 0; idx < pattern.Analyzer.Rules.Count; idx++)
                {
                    var rule = pattern.Analyzer.Rules[idx];
                    switch (rule.Type)
                    {
                        case RxAnalyzeRuleType.Any:
                            grid.Children.Add(Gui.MakeTextBlockStyle1("any", bit_pos, col_byte, 8));
                            bit_pos += 8;
                            break;

                        case RxAnalyzeRuleType.Value:
                            string disp = $"0x{rule.Value:X2}";
                            grid.Children.Add(Gui.MakeTextBlockStyle1(disp, bit_pos, col_byte, 8));
                            bit_pos += 8;
                            break;

                        case RxAnalyzeRuleType.Script:
                        case RxAnalyzeRuleType.Timeout:
                        default:
                            bit_pos++;
                            break;
                    }
                }
            }
        }


        private static UIElement MakeCheckboxEnable(RxPattern buffer, string path, int row, int col, int rowspan = -1, int colspan = -1)
        {
            var cb = new CheckBox();
            cb.Margin = new Thickness(5, 4, 5, 2);
            Grid.SetRow(cb, row);
            Grid.SetColumn(cb, col);

            // Frame名称作成
            string name;
            if (Setting.Data.Comm.DisplayId)
            {
                name = $"[{buffer.Id}] {buffer.Name}";
            }
            else
            {
                name = buffer.Name;
            }
            cb.Content = name;

            // Binding
            var bind = new Binding(path + ".IsActive.Value");
            cb.SetBinding(CheckBox.IsCheckedProperty, bind);
            //var bind_text = new Binding(path + ".Name");
            //cb.SetBinding(CheckBox.ContentProperty, bind_text);

            //
            var border = Gui.MakeBorder1();
            border.CornerRadius = new CornerRadius(9, 9, 0, 0);
            //border.Background = ColorFrameNameBg;
            //border.BorderBrush = ColorFrameNameBg;
            border.Child = cb;
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
        private static UIElement MakeNameGui(Field field, string path, string text, int row, int col, int rowspan = -1, int colspan = -1)
        {
            //
            var tb = new TextBlock();
            tb.Text = text;
            tb.Background = SystemColors.ControlLightLightBrush;
            //tb.FontSize += 1;
            tb.TextWrapping = TextWrapping.Wrap;
            tb.Padding = new Thickness(5, 2, 2, 2);
            //
            var border = Gui.MakeBorder1();
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
        public static UIElement MakeTextBlockRecv(RxMatch match, string path, int row, int col, int rowspan = -1, int colspan = -1)
        {
            //
            var tb = new TextBlock();
            tb.Background = SystemColors.ControlLightLightBrush;
            //tb.FontSize += 1;
            tb.TextWrapping = TextWrapping.Wrap;
            tb.Padding = new Thickness(5, 2, 2, 2);
            // binding
            var bind = new Binding(path + ".Disp.Value");
            tb.SetBinding(TextBlock.TextProperty, bind);
            //
            var border = Gui.MakeBorder1();
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
