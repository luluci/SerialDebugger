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
                // frameごとにgroup定義があるのでここで列表示情報を作成する
                var (col_info, col_info_sort) = MakeColInfo(Gui.setting, frame, frame_no);
                // frameごとにGUI作成
                var (grid1, grid2, grid3) = MakeBase(grid, margin_l);
                var w = MakeHeader(Gui.setting, col_info, col_info_sort, grid2, frame, frame_no);
                w = MakeBody(Gui.setting, col_info, col_info_sort, grid3, frame, frame_no);

                //margin_l += (grid1.Width + 50);
                margin_l += (w + 30);
                frame_no++;
            }

            return grid;
        }

        class ColumnInfo
        {
            public bool IsEnable { get; set; }
            public SettingGui.Col Id { get; set; }
            public int Order { get; set; }
            public int Width { get; set; }
            public int ColLen { get; set; }
        }

        private static (ColumnInfo[], ColumnInfo[]) MakeColInfo(Settings.SettingInfo setting, TxFrame frame, int frame_no)
        {
            var col_array = new ColumnInfo[(int)SettingGui.Col.Size];
            var col_array_sort = new ColumnInfo[(int)SettingGui.Col.Size];
            // 設定ファイルの内容をそのまま列情報に展開
            for (int i = 0; i < (int)SettingGui.Col.Size; i++)
            {
                var info = new ColumnInfo{
                    IsEnable = false,
                    Id = (SettingGui.Col)Enum.ToObject(typeof(SettingGui.Col), i),
                    Order = setting.Gui.ColOrder[i],
                    Width = setting.Gui.ColWidth[i],
                    ColLen = 1,
                };
                col_array[i] = info;
                col_array_sort[i] = info;
            }
            // ColLenをチェック
            // Group
            // 今後のGroup定義の内容によって可変にするかも？
            // 現状はGroup名と個別インデックスの2つ
            col_array[(int)SettingGui.Col.Group].ColLen = 2;
            // TxFieldBufferはデフォルトで1つ、backup領域を任意数の合計
            // デフォルト分は個別に表示するのでbackup領域分だけセット
            // backup領域がゼロのときはスペーサーとbackup領域は表示しない
            int bkbuff_size = frame.BufferSize - 1;
            col_array[(int)SettingGui.Col.TxBuffer].ColLen = bkbuff_size;
            if (bkbuff_size == 0)
            {
                col_array[(int)SettingGui.Col.Spacer].Order = -1;
                col_array[(int)SettingGui.Col.TxBuffer].Order = -1;
            }

            // Orderでソートして順序を作ってGUI作成に使える形でOrderを振りなおす
            Array.Sort(col_array_sort, (a,b) => a.Order - b.Order);
            int new_order = 0;
            for (int i = 0; i < (int)SettingGui.Col.Size; i++)
            {
                if (col_array_sort[i].Order < 0)
                {
                    // -1は非表示とする
                    col_array_sort[i].IsEnable = false;
                    col_array_sort[i].Width = 0;
                    col_array_sort[i].ColLen = 0;
                }
                else
                {
                    // 0以上なら表示列としてOrderを順番に降りなおす
                    col_array_sort[i].IsEnable = true;
                    col_array_sort[i].Order = new_order;
                    // Idそのまま
                    // Widthそのまま
                    // ColLenそのまま

                    new_order += col_array_sort[i].ColLen;
                }
            }

            return (col_array, col_array_sort);
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

        private static int MakeHeader(Settings.SettingInfo setting, ColumnInfo[] col_info, ColumnInfo[] col_info_sort, Grid grid, TxFrame frame, int frame_no)
        {
            int width = 0;
            // Gridに列を作成
            for (int col_idx = 0; col_idx < col_info_sort.Length; col_idx++)
            {
                var info = col_info_sort[col_idx];
                if (info.IsEnable)
                {
                    for (int col_idx2 = 0; col_idx2 < info.ColLen; col_idx2++)
                    {
                        var col = new ColumnDefinition();
                        col.Width = new GridLength(info.Width);
                        grid.ColumnDefinitions.Add(col);
                        width += info.Width;
                    }
                }
            }
            {
                var row_1 = new RowDefinition();
                var row_2 = new RowDefinition();
                row_1.Height = GridLength.Auto;
                row_2.Height = GridLength.Auto;
                grid.RowDefinitions.Add(row_1);
                grid.RowDefinitions.Add(row_2);
            }
            // 列にヘッダGUI作成
            {
                ColumnInfo info;
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
                info = col_info[(int)SettingGui.Col.ByteIndex];
                if (info.IsEnable)
                {
                    grid.Children.Add(Gui.MakeTextBlockStyle1("Byte", 1, info.Order, -1, info.ColLen));
                }
                // column作成: bit
                info = col_info[(int)SettingGui.Col.BitIndex];
                if (info.IsEnable)
                {
                    grid.Children.Add(Gui.MakeTextBlockStyle1("Bit", 1, info.Order, -1, info.ColLen));
                }
                // column作成: group
                info = col_info[(int)SettingGui.Col.Group];
                if (info.IsEnable)
                {
                    grid.Children.Add(Gui.MakeTextBlockStyle1("Grp", 1, info.Order, -1, info.ColLen));
                }
                // column作成: value
                info = col_info[(int)SettingGui.Col.FieldValue];
                if (info.IsEnable)
                {
                    grid.Children.Add(Gui.MakeTextBlockStyle1("Value", 1, info.Order, -1, info.ColLen));
                }
                // column作成: name
                info = col_info[(int)SettingGui.Col.FieldName];
                if (info.IsEnable)
                {
                    grid.Children.Add(Gui.MakeTextBlockStyle1("Name", 1, info.Order, -1, info.ColLen));
                }
                // column作成: input
                info = col_info[(int)SettingGui.Col.FieldInput];
                if (info.IsEnable)
                {
                    grid.Children.Add(Gui.MakeTextBlockStyle1("Input", 1, info.Order, -1, info.ColLen));
                }
                // Buffer列作成
                info = col_info[(int)SettingGui.Col.TxBytes];
                if (info.IsEnable)
                {
                    // 送信ボタン作成
                    // Bufferは各fieldと連動
                    grid.Children.Add(MakeTxSendFixButton($"TxFrames[{frame_no}].Buffers[0]", "OnClickTxDataSend", 0, info.Order));
                    grid.Children.Add(Gui.MakeTextBlockStyle1("TxData", 1, info.Order));
                }
                // BackupBufferを持つ場合はスペースを少し開けてGUI作成
                // BackupBuffer列作成
                // BackupBufferはBufferの保存/展開により値を決定
                info = col_info[(int)SettingGui.Col.TxBuffer];
                if (info.IsEnable)
                {
                    for (int i = 1; i < frame.BufferSize; i++)
                    {
                        // 送信ボタン作成
                        grid.Children.Add(MakeButtonLoadStore(frame.Buffers[i], $"TxFrames[{frame_no}].Buffers[{i}]", "OnClickTxDataSend", 0, info.Order + i - 1));
                        // 表示ラベル
                        if (Setting.Data.Comm.DisplayId)
                        {
                            name = $"[{frame.Buffers[i].Id}] {frame.Buffers[i].Name}";
                        }
                        else
                        {
                            name = frame.Buffers[i].Name;
                        }
                        grid.Children.Add(Gui.MakeTextBlockStyle1(name, 1, info.Order + i - 1));
                    }
                }
            }

            return width;
        }

        private static int MakeBody(Settings.SettingInfo setting, ColumnInfo[] col_info, ColumnInfo[] col_info_sort, Grid grid, TxFrame frame, int frame_no)
        {
            int bitlength = frame.Length * 8;
            int width = 0;
            // Gridに列を作成
            for (int col_idx = 0; col_idx < col_info_sort.Length; col_idx++)
            {
                var info = col_info_sort[col_idx];
                if (info.IsEnable)
                {
                    for (int col_idx2 = 0; col_idx2 < info.ColLen; col_idx2++)
                    {
                        var col = new ColumnDefinition();
                        col.Width = new GridLength(info.Width);
                        grid.ColumnDefinitions.Add(col);
                        width += info.Width;
                    }
                }
            }
            // Gridにrowを作成
            for (int bit = 0; bit < bitlength; bit++)
            {
                var row = new RowDefinition();
                row.Height = GridLength.Auto;
                grid.RowDefinitions.Add(row);
            }
            {
                // 通信フレーム作成
                int bit_rest = 0;
                int bit_pos = 0;
                int byte_pos = 0;
                int field_pos = 0;
                bool is_byte = false;
                // bit単位でfieldサイズを指定可能でるため、1bitずつループして表示を作成する
                for (int bit = 0; bit < bitlength; bit++)
                {
                    ColumnInfo info;
                    is_byte = ((bit % 8) == 0);
                    // column作成: byte
                    // 8bit区切り＝1バイト区切りの表示項目作成
                    if (bit_pos == 0)
                    {
                        // バイト数表示
                        info = col_info[(int)SettingGui.Col.ByteIndex];
                        if (info.IsEnable)
                        {
                            grid.Children.Add(Gui.MakeTextBlockStyle1($"{byte_pos}", bit, info.Order, 8));
                        }
                        // 送信バイトシーケンス
                        info = col_info[(int)SettingGui.Col.TxBytes];
                        if (info.IsEnable)
                        {
                            grid.Children.Add(Gui.MakeTextBlockBindByteData($"TxFrames[{frame_no}].Buffers[0].Buffer[{byte_pos}]", bit, info.Order, 8));
                        }
                    }
                    // field情報作成
                    if (bit_rest == 0)
                    {
                        if (field_pos < frame.Fields.Count)
                        {
                            // 表示未作成のfieldがあるなら表示作成
                            var field = frame.Fields[field_pos];
                            // Bit列作成
                            info = col_info[(int)SettingGui.Col.BitIndex];
                            if (info.IsEnable)
                            {
                                if (is_byte && (field.BitSize % 8) == 0 && (field.InnerFields.Count == 1))
                                {
                                    // バイト境界に配置、かつ、バイト単位データのとき、かつ、フィールド内で名前分割しないとき、
                                    // バイト単位でまとめて表示
                                    grid.Children.Add(Gui.MakeTextBlockStyle1($"-", bit, info.Order, field.BitSize));
                                }
                                else
                                {
                                    // その他は各ビット情報を出力
                                    for (int i = 0; i < field.BitSize; i++)
                                    {
                                        grid.Children.Add(Gui.MakeTextBlockBindBitData(field, $"{bit + i}", $"TxFrames[{frame_no}].Buffers[0].FieldValues[{field_pos}].Value.Value", i, bit + i, info.Order));
                                    }
                                }
                            }
                            // Value列作成
                            info = col_info[(int)SettingGui.Col.FieldValue];
                            if (info.IsEnable)
                            {
                                grid.Children.Add(Gui.MakeTextBlockBindStyle1(field, $"TxFrames[{frame_no}].Buffers[0].FieldValues[{field_pos}].Value.Value", bit, info.Order, field.BitSize));
                            }
                            // Name列作成
                            int inner_idx = 0;
                            info = col_info[(int)SettingGui.Col.FieldName];
                            if (info.IsEnable)
                            {
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
                                    grid.Children.Add(MakeNameGui(field, $"TxFrames[{frame_no}].Fields[{field_pos}]", name, bit + inner_idx, info.Order, inner.BitSize));
                                    inner_idx += inner.BitSize;
                                }
                            }
                            //grid.Children.Add(MakeTextBlockStyle3(field.Name, bit, 3, field.BitSize));
                            var fb = frame.Buffers[0];
                            var fv = fb.FieldValues[field_pos];
                            // Input列作成
                            info = col_info[(int)SettingGui.Col.FieldInput];
                            if (info.IsEnable)
                            {
                                grid.Children.Add(Gui.MakeInputGui(field, fv, $"TxFrames[{frame_no}]", $"TxFrames[{frame_no}].Fields[{field_pos}]", $"TxFrames[{frame_no}].Buffers[0]", $"TxFrames[{frame_no}].Buffers[0].FieldValues[{field_pos}]", SettingGui.Col.FieldInput, bit, info.Order, field.BitSize));
                            }
                            // BackupBuffer列作成
                            info = col_info[(int)SettingGui.Col.TxBuffer];
                            if (info.IsEnable)
                            {
                                for (int i = 1; i < frame.BufferSize; i++)
                                {
                                    fb = frame.Buffers[i];
                                    fv = fb.FieldValues[field_pos];
                                    grid.Children.Add(MakeBackupBufferGui(field, fv, $"TxFrames[{frame_no}]", $"TxFrames[{frame_no}].Fields[{field_pos}]", $"TxFrames[{frame_no}].Buffers[{i}]", $"TxFrames[{frame_no}].Buffers[{i}].FieldValues[{field_pos}]", SettingGui.Col.TxBuffer, bit, info.Order + i - 1, field.BitSize));
                                }
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
                            info = col_info[(int)SettingGui.Col.BitIndex];
                            if (info.IsEnable)
                            {
                                for (int i = 0; i < bit_rest; i++)
                                {
                                    grid.Children.Add(Gui.MakeTextBlockStyle1($"{bit + i}", bit + i, info.Order));
                                }
                            }
                            // Value列作成
                            info = col_info[(int)SettingGui.Col.FieldValue];
                            if (info.IsEnable)
                            {
                                grid.Children.Add(Gui.MakeTextBlockStyle1("-", bit, info.Order, bit_rest));
                            }
                            // Name列作成
                            info = col_info[(int)SettingGui.Col.FieldName];
                            if (info.IsEnable)
                            {
                                grid.Children.Add(Gui.MakeTextBlockStyle1("-", bit, info.Order, bit_rest));
                            }
                            // Input列作成
                            info = col_info[(int)SettingGui.Col.FieldInput];
                            if (info.IsEnable)
                            {
                                grid.Children.Add(Gui.MakeTextBlockStyle1("-", bit, info.Order, bit_rest));
                            }
                            // BackupBuffer列作成
                            info = col_info[(int)SettingGui.Col.TxBuffer];
                            if (info.IsEnable)
                            {
                                for (int i = 1; i < frame.BufferSize; i++)
                                {
                                    grid.Children.Add(Gui.MakeTextBlockStyle1("-", bit, info.Order + i - 1, bit_rest));
                                }
                            }
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
        private static UIElement MakeBackupBufferGui(Field field, FieldValue value, string frame_path, string field_path, string field_value_path, string value_path, SettingGui.Col col_id, int row, int col, int rowspan = -1, int colspan = -1)
        {
            switch (value.FieldRef.InputType)
            {
                case Field.InputModeType.Dict:
                case Field.InputModeType.Unit:
                case Field.InputModeType.Time:
                case Field.InputModeType.Script:
                    return Gui.MakeInputGuiSelecter(value.FieldRef, field, field_path, value_path, row, col, rowspan, colspan);
                case Field.InputModeType.Char:
                    return Gui.MakeInputGuiEditChar(field, value, field, frame_path, field_path, field_value_path, value_path, col_id, row, col, rowspan, colspan);
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
