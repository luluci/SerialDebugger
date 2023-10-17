using Microsoft.Xaml.Behaviors;
using Reactive.Bindings.Interactivity;
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
        public static Grid Make()
        {
            // ベースGrid作成
            Grid grid = new Grid();

            // 先頭要素を選択
            var frames = Gui.setting.Comm.Tx;

            int frame_no = 0;
            double margin_l = 0;
            foreach (var frame in frames)
            {
                // frameごとにGUI作成
                var (grid1, grid2, grid3) = MakeBase(grid, margin_l);
                var w = MakeHeader(Gui.setting, grid2, frame, frame_no);
                w = MakeBody(Gui.setting, grid3, frame, frame_no);

                //margin_l += (grid1.Width + 50);
                margin_l += (w + 30);
                frame_no++;
            }

            return grid;
        }

        /// <summary>
        /// parent上にTxFrameGUIのベースになるGrid(2x1)(header/body)を作成する
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        private static (Grid, Grid, Grid) MakeBase(Grid parent, double margin_l)
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
            parent.Children.Add(grid);

            return (grid, grid_header, grid_body);
        }

        private static int MakeHeader(Settings.SettingInfo setting, Grid grid, TxFrame frame, int frame_no)
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
                col_byte.Width = new GridLength(setting.Gui.ColWidth[(int)SettingGui.Col.ByteIndex]);
                col_bit.Width = new GridLength(setting.Gui.ColWidth[(int)SettingGui.Col.BitIndex]);
                col_value.Width = new GridLength(setting.Gui.ColWidth[(int)SettingGui.Col.FieldValue]);
                col_name.Width = new GridLength(setting.Gui.ColWidth[(int)SettingGui.Col.FieldName]);
                col_input.Width = new GridLength(setting.Gui.ColWidth[(int)SettingGui.Col.FieldInput]);
                col_txdata.Width = new GridLength(setting.Gui.ColWidth[(int)SettingGui.Col.TxBytes]);
                grid.ColumnDefinitions.Add(col_byte);
                grid.ColumnDefinitions.Add(col_bit);
                grid.ColumnDefinitions.Add(col_value);
                grid.ColumnDefinitions.Add(col_name);
                grid.ColumnDefinitions.Add(col_input);
                grid.ColumnDefinitions.Add(col_txdata);
                // BackupBufferを持つ場合はスペースを少し開けてGUI作成
                if (frame.BufferSize > 1)
                {
                    var col_space = new ColumnDefinition();
                    col_space.Width = new GridLength(setting.Gui.ColWidth[(int)SettingGui.Col.Spacer]);
                    grid.ColumnDefinitions.Add(col_space);
                    for (int i = 1; i < frame.BufferSize; i++)
                    {
                        var col = new ColumnDefinition();
                        col.Width = new GridLength(setting.Gui.ColWidth[(int)SettingGui.Col.TxBuffer]);
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
                    width += setting.Gui.ColWidth[i];
                }
                // BackupBufferを持つ場合はスペースを少し開けてGUI作成
                if (frame.BufferSize > 1)
                {
                    width += setting.Gui.ColWidth[(int)SettingGui.Col.Spacer];
                    for (int i = 1; i < frame.BufferSize; i++)
                    {
                        width += setting.Gui.ColWidth[(int)SettingGui.Col.TxBuffer];
                    }
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
            grid.Children.Add(Gui.MakeTextBlockStyle2(name, 0, 0, -1, 5));
            // column作成: byte
            grid.Children.Add(Gui.MakeTextBlockStyle1("Byte", 1, setting.Gui.ColOrder[(int)SettingGui.Col.ByteIndex]));
            // column作成: bit
            grid.Children.Add(Gui.MakeTextBlockStyle1("Bit", 1, setting.Gui.ColOrder[(int)SettingGui.Col.BitIndex]));
            // column作成: value
            grid.Children.Add(Gui.MakeTextBlockStyle1("Value", 1, setting.Gui.ColOrder[(int)SettingGui.Col.FieldValue]));
            // column作成: name
            grid.Children.Add(Gui.MakeTextBlockStyle1("Name", 1, setting.Gui.ColOrder[(int)SettingGui.Col.FieldName]));
            // column作成: input
            grid.Children.Add(Gui.MakeTextBlockStyle1("Input", 1, setting.Gui.ColOrder[(int)SettingGui.Col.FieldInput]));
            // Buffer列作成
            {
                // 送信ボタン作成
                // Bufferは各fieldと連動
                grid.Children.Add(MakeTxSendFixButton($"TxFrames[{frame_no}].Buffers[0]", "OnClickTxDataSend", 0, setting.Gui.ColOrder[(int)SettingGui.Col.TxBytes]));
                grid.Children.Add(Gui.MakeTextBlockStyle1("TxData", 1, setting.Gui.ColOrder[(int)SettingGui.Col.TxBytes]));
            }
            // BackupBufferを持つ場合はスペースを少し開けてGUI作成
            // BackupBuffer列作成
            // BackupBufferはBufferの保存/展開により値を決定
            for (int i = 1; i < frame.BufferSize; i++)
            {
                // 送信ボタン作成
                grid.Children.Add(MakeButtonLoadStore(frame.Buffers[i], $"TxFrames[{frame_no}].Buffers[{i}]", "OnClickTxDataSend", 0, setting.Gui.ColOrder[(int)SettingGui.Col.TxBuffer] + i-1));
                // 表示ラベル
                if (Setting.Data.Comm.DisplayId)
                {
                    name = $"[{frame.Buffers[i].Id}] {frame.Buffers[i].Name}";
                }
                else
                {
                    name = frame.Buffers[i].Name;
                }
                grid.Children.Add(Gui.MakeTextBlockStyle1(name, 1, setting.Gui.ColOrder[(int)SettingGui.Col.TxBuffer] + i-1));
            }

            return width;
        }

        private static int MakeBody(Settings.SettingInfo setting, Grid grid, TxFrame frame, int frame_no)
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
                col_byte.Width = new GridLength(setting.Gui.ColWidth[(int)SettingGui.Col.ByteIndex]);
                col_bit.Width = new GridLength(setting.Gui.ColWidth[(int)SettingGui.Col.BitIndex]);
                col_value.Width = new GridLength(setting.Gui.ColWidth[(int)SettingGui.Col.FieldValue]);
                col_name.Width = new GridLength(setting.Gui.ColWidth[(int)SettingGui.Col.FieldName]);
                col_input.Width = new GridLength(setting.Gui.ColWidth[(int)SettingGui.Col.FieldInput]);
                col_txdata.Width = new GridLength(setting.Gui.ColWidth[(int)SettingGui.Col.TxBytes]);
                grid.ColumnDefinitions.Add(col_byte);
                grid.ColumnDefinitions.Add(col_bit);
                grid.ColumnDefinitions.Add(col_value);
                grid.ColumnDefinitions.Add(col_name);
                grid.ColumnDefinitions.Add(col_input);
                grid.ColumnDefinitions.Add(col_txdata);
                // BackupBufferを持つ場合はスペースを少し開けてGUI作成
                if (frame.BufferSize > 1)
                {
                    var col_space = new ColumnDefinition();
                    col_space.Width = new GridLength(setting.Gui.ColWidth[(int)SettingGui.Col.Spacer]);
                    grid.ColumnDefinitions.Add(col_space);
                    for (int i = 1; i < frame.BufferSize; i++)
                    {
                        var col = new ColumnDefinition();
                        col.Width = new GridLength(setting.Gui.ColWidth[(int)SettingGui.Col.TxBuffer]);
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
                    width += setting.Gui.ColWidth[i];
                }
                // BackupBufferを持つ場合はスペースを少し開けてGUI作成
                if (frame.BufferSize > 1)
                {
                    width += setting.Gui.ColWidth[(int)SettingGui.Col.Spacer];
                    for (int i = 1; i < frame.BufferSize; i++)
                    {
                        width += setting.Gui.ColWidth[(int)SettingGui.Col.TxBuffer];
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
                        grid.Children.Add(Gui.MakeTextBlockStyle1($"{byte_pos}", bit, setting.Gui.ColOrder[(int)SettingGui.Col.ByteIndex], 8));
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
                                grid.Children.Add(Gui.MakeTextBlockStyle1($"-", bit, setting.Gui.ColOrder[(int)SettingGui.Col.BitIndex], field.BitSize));
                            }
                            else
                            {
                                // その他は各ビット情報を出力
                                for (int i=0; i<field.BitSize; i++)
                                {
                                    grid.Children.Add(Gui.MakeTextBlockBindBitData(field, $"{bit + i}", $"TxFrames[{frame_no}].Buffers[0].FieldValues[{field_pos}].Value.Value", i, bit+i, setting.Gui.ColOrder[(int)SettingGui.Col.BitIndex]));
                                }
                            }
                            // Value列作成
                            grid.Children.Add(Gui.MakeTextBlockBindStyle1(field, $"TxFrames[{frame_no}].Buffers[0].FieldValues[{field_pos}].Value.Value", bit, setting.Gui.ColOrder[(int)SettingGui.Col.FieldValue], field.BitSize));
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
                                grid.Children.Add(MakeNameGui(field, $"TxFrames[{frame_no}].Fields[{field_pos}]", name, bit+inner_idx, setting.Gui.ColOrder[(int)SettingGui.Col.FieldName], inner.BitSize));
                                inner_idx += inner.BitSize;
                            }
                            //grid.Children.Add(MakeTextBlockStyle3(field.Name, bit, 3, field.BitSize));
                            // Input列作成
                            grid.Children.Add(Gui.MakeInputGui(field, $"TxFrames[{frame_no}].Fields[{field_pos}]", $"TxFrames[{frame_no}].Buffers[0].FieldValues[{field_pos}]", bit, setting.Gui.ColOrder[(int)SettingGui.Col.FieldInput], field.BitSize));
                            // BackupBuffer列作成
                            for (int i = 1; i < frame.BufferSize; i++)
                            {
                                var fb = frame.Buffers[i];
                                var fv = fb.FieldValues[field_pos];
                                grid.Children.Add(MakeBackupBufferGui(field, fv, $"TxFrames[{frame_no}].Fields[{field_pos}]", $"TxFrames[{frame_no}].Buffers[{i}].FieldValues[{field_pos}]", bit, setting.Gui.ColOrder[(int)SettingGui.Col.TxBuffer] + i-1, field.BitSize));
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
                                grid.Children.Add(Gui.MakeTextBlockStyle1($"{bit + i}", bit+i, setting.Gui.ColOrder[(int)SettingGui.Col.BitIndex]));
                            }
                            // Value列作成
                            grid.Children.Add(Gui.MakeTextBlockStyle1("-", bit, setting.Gui.ColOrder[(int)SettingGui.Col.FieldValue], bit_rest));
                            // Name列作成
                            grid.Children.Add(Gui.MakeTextBlockStyle1("-", bit, setting.Gui.ColOrder[(int)SettingGui.Col.FieldName], bit_rest));
                            // Input列作成
                            grid.Children.Add(Gui.MakeTextBlockStyle1("-", bit, setting.Gui.ColOrder[(int)SettingGui.Col.FieldInput], bit_rest));
                            // BackupBuffer列作成
                            for (int i = 1; i < frame.BufferSize; i++)
                            {
                                grid.Children.Add(Gui.MakeTextBlockStyle1("-", bit, setting.Gui.ColOrder[(int)SettingGui.Col.TxBuffer] + i-1, bit_rest));
                            }
                        }
                    }
                    // 送信バイトシーケンス
                    if (bit_pos == 0)
                    {
                        grid.Children.Add(Gui.MakeTextBlockBindByteData($"TxFrames[{frame_no}].Buffers[0].Buffer[{byte_pos}]", bit, setting.Gui.ColOrder[(int)SettingGui.Col.TxBytes], 8));
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
        private static Button MakeTxSendFixButton(string buff_path, string cmnd_path, int row, int col, int rowspan = -1, int colspan = -1)
        {
            var btn = new Button();
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

            // Binding
            // Command
            var bind_cmnd = new Binding(cmnd_path);
            var bind_param = new Binding(buff_path);
            btn.SetBinding(Button.CommandProperty, bind_cmnd);
            btn.SetBinding(Button.CommandParameterProperty, bind_param);
            // Text
            var bind_text = new Binding(buff_path + ".ChangeState.Value");
            bind_text.Converter = Gui.TxSendFixNameConverter;
            btn.SetBinding(Button.ContentProperty, bind_text);
            // border
            var bind_bgcolor = new Binding(buff_path + ".ChangeState.Value");
            bind_bgcolor.Converter = Gui.TxSendFixBGColorConverter;
            btn.SetBinding(Button.BorderBrushProperty, bind_bgcolor);

            return btn;
        }

        /// <summary>
        /// キャプション的な部分の部品
        /// 3連ボタン
        /// </summary>
        /// <param name="tgt"></param>
        /// <returns></returns>
        private static UIElement MakeButtonLoadStore(TxFieldBuffer buffer, string path, string cmnd_path, int row, int col, int rowspan = -1, int colspan = -1)
        {

            var bind_store = new Binding(path + ".OnClickStore");
            var btn_store = new Button();
            btn_store.Content = "←";
            btn_store.Margin = new Thickness(2, 1, 2, 1);
            btn_store.Width = Gui.Button3Width[0];
            btn_store.SetBinding(Button.CommandProperty, bind_store);

            var btn = MakeTxSendFixButton(path, cmnd_path, 0, col);
            btn.Margin = new Thickness(2, 1, 2, 1);
            btn.Padding = new Thickness(5, 0, 5, 0);
            // border
            var bind_bgcolor = new Binding(path + ".ChangeState.Value");
            bind_bgcolor.Converter = Gui.TxSendFixBGColorConverter;
            btn.SetBinding(Button.BorderBrushProperty, bind_bgcolor);

            var bind_save = new Binding(path + ".OnClickSave");
            var btn_save = new Button();
            btn_save.Content = "↓";
            btn_save.Margin = new Thickness(2, 1, 1, 1);
            btn_save.Width = Gui.Button3Width[2];
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
            //Interaction.GetTriggers(tb).Add(
            //new Microsoft.Xaml.Behaviors.EventTrigger("MouseDown")
            //{
            //    Actions =
            //    {
            //        new EventToReactiveCommand
            //        {
            //            Command = field.OnMouseDown,
            //        },
            //    },
            //});
            tb.MouseDown += (s,e) => field.OnMouseDown.Execute(e);
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
        /// BackupBuffer GUI作成
        /// </summary>
        /// <param name="value_path"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="rowspan"></param>
        /// <param name="colspan"></param>
        /// <returns></returns>
        private static UIElement MakeBackupBufferGui(Field field, FieldValue value, string field_path, string value_path, int row, int col, int rowspan = -1, int colspan = -1)
        {
            switch (value.FieldRef.InputType)
            {
                case Field.InputModeType.Dict:
                case Field.InputModeType.Unit:
                case Field.InputModeType.Time:
                case Field.InputModeType.Script:
                    return Gui.MakeInputGuiSelecter(value.FieldRef, field, field_path, value_path, row, col, rowspan, colspan);
                case Field.InputModeType.Char:
                    return Gui.MakeInputGuiEditChar(field, field, value_path, row, col, rowspan, colspan);
                case Field.InputModeType.Edit:
                    return Gui.MakeInputGuiEdit(field, field, value_path, row, col, rowspan, colspan);
                case Field.InputModeType.Checksum:
                    return Gui.MakeInputGuiEdit(field, field, value_path, row, col, rowspan, colspan);
                case Field.InputModeType.Fix:
                default:
                    return Gui.MakeTextBlockStyle1("<FIX>", row, col, rowspan, colspan);
            }

            /*
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
            */

        }

    }
}
