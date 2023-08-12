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

    class AutoTxGui
    {
        // GUI Resource
        public static SolidColorBrush ColorFrameNameBg = new SolidColorBrush(Color.FromArgb(0xFF, 11, 40, 75));

        // 関数のネストが深くなるので参照を取っておく
        static private Settings.SettingInfo setting;
        public static Grid Make(Settings.SettingInfo setting)
        {
            AutoTxGui.setting = setting;
            // GUI作成
            var jobs = setting.Comm.AutoTx;

            // ベースGrid作成
            var grid = MakeBase(jobs.Count);

            int row = 0;
            foreach (var job in jobs)
            {
                MakeJob($"AutoTxJobs[{row}]", row, grid, job);
                row++;
            }

            return grid;
        }

        private static Grid MakeBase(int row_size)
        {
            var grid = new Grid();
            // 2列
            var col1 = new ColumnDefinition();
            var col2 = new ColumnDefinition();
            col1.Width = new GridLength(0, GridUnitType.Auto);
            col2.Width = new GridLength(100, GridUnitType.Star);
            grid.ColumnDefinitions.Add(col1);
            grid.ColumnDefinitions.Add(col2);
            // N行
            for (int i = 0; i < row_size; i++)
            {
                var row = new RowDefinition();
                row.Height = new GridLength(0, GridUnitType.Auto);
                grid.RowDefinitions.Add(row);
            }
            // Margin
            grid.Margin = new Thickness(10, 10, 10, 5);
            //grid.HorizontalAlignment = HorizontalAlignment.Left;
            
            return grid;
        }

        private static void MakeJob(string path, int row, Grid parent, AutoTxJob job)
        {
            // Job Name/checkbox
            var name = MakeJobName(job, path);
            Grid.SetRow(name, row);
            Grid.SetColumn(name, 0);

            // Job Detail
            var border = MakeActionsBase();
            var sp = new WrapPanel();
            sp.Orientation = Orientation.Horizontal;
            sp.Margin = new System.Windows.Thickness(5, 5, 5, 5);

            border.Child = sp;
            Grid.SetRow(border, row);
            Grid.SetColumn(border, 1);

            var act_margin = new System.Windows.Thickness(10, 0, 0, 5);

            int i = 0;
            bool is_first = true;
            foreach (var action in job.Actions)
            {
                // 矢印テキスト挿入
                if (!is_first)
                {
                    var sep = MakeActionSeparator();
                    sp.Children.Add(sep);
                }
                // Action GUI挿入
                var act = MakeAction(job, action, path + $".Actions[{i}]", is_first, job.IsEditable);
                //
                sp.Children.Add(act);

                is_first = false;
                i++;
            }

            //
            parent.Children.Add(name);
            parent.Children.Add(border);
        }

        public static Border MakeJobName(AutoTxJob job, string path)
        {
            var tb = new TextBlock();
            string name;
            if (Setting.Data.Comm.DisplayId)
            {
                name = $"[{job.Id}] {job.Alias}";
            }
            else
            {
                name = job.Alias;
            }
            tb.Text = name;
            tb.TextWrapping = TextWrapping.Wrap;

            var cb = new CheckBox();
            cb.Content = tb;
            var bind = new Binding(path + ".IsActive.Value");
            cb.SetBinding(CheckBox.IsCheckedProperty, bind);
            cb.FontWeight = FontWeights.Bold;
            cb.Foreground = Brushes.White;
            //cb.Padding = new Thickness(5, 10, 5, 10);
            // Resetボタン
            var btn = new Button();
            btn.Content = "Reset";
            btn.Margin = new Thickness(5, 10, 5, 0);
            btn.Width = 70;
            bind = new Binding(path + ".OnClickReset");
            btn.SetBinding(Button.CommandProperty, bind);
            // 
            var sp = new StackPanel();
            sp.Orientation = Orientation.Vertical;
            sp.Children.Add(cb);
            sp.Children.Add(btn);
            //
            var border = new Border();
            border.BorderThickness = new Thickness(1);
            border.BorderBrush = Brushes.DarkSlateGray;
            border.CornerRadius = new CornerRadius(9, 0, 0, 9);
            //border.Background = ColorFrameNameBg;
            //border.BorderBrush = ColorFrameNameBg;
            border.Child = sp;
            border.Padding = new Thickness(10,10,10,10);
            border.Margin = new Thickness(5, 5, 0, 5);
            border.MaxWidth = 200;

            var bind_bg = new Binding(path + ".IsActive.Value");
            bind_bg.Converter = ActiveJobBGColorConverter;
            border.SetBinding(Border.BackgroundProperty, bind_bg);


            return border;
        }

        private static Border MakeActionsBase()
        {
            var border = new Border();
            border.BorderThickness = new Thickness(1);
            border.BorderBrush = Brushes.DarkSlateGray;
            border.CornerRadius = new CornerRadius(0, 9, 9, 0);
            //border.Background = ColorFrameNameBg;
            //border.BorderBrush = ColorFrameNameBg;
            //border.Padding = new Thickness(10, 10, 10, 10);
            border.Margin = new Thickness(0, 5, 5, 5);
            border.Padding = new Thickness(0);

            return border;
        }

        private static UIElement MakeAction(AutoTxJob job, AutoTxAction action, string path, bool is_first, bool editable)
        {
            switch (action.Type)
            {
                case AutoTxActionType.Jump:
                    if (editable)
                    {
                        return MakeActionEditJump(job, action, path, is_first);
                    }
                    else
                    {
                        return MakeActionDisp(action, path, is_first);
                    }
                case AutoTxActionType.Wait:
                    if (editable)
                    {
                        return MakeActionEditWait(job, action, path, is_first);
                    }
                    else
                    {
                        return MakeActionDisp(action, path, is_first);
                    }
                case AutoTxActionType.Send:
                case AutoTxActionType.Recv:
                case AutoTxActionType.AnyRecv:
                case AutoTxActionType.Script:
                case AutoTxActionType.ActivateAutoTx:
                case AutoTxActionType.ActivateRx:
                case AutoTxActionType.Log:
                    return MakeActionDisp(action, path, is_first);
                    
                default:
                    throw new Exception("undefined Action.");
            }
        }

        private static AutoTxGuiActiveJobBGColorConverter ActiveJobBGColorConverter = new AutoTxGuiActiveJobBGColorConverter();
        private static AutoTxGuiActiveActionBGColorConverter ActiveActionBGColorConverter = new AutoTxGuiActiveActionBGColorConverter();
        private static Thickness ActionBaseBorderThickness = new Thickness(1);
        private static Thickness ActionBasePadding = new Thickness(5);
        private static Thickness ActionBaseEditPadding = new Thickness(5, 3, 5, 3);
        private static Thickness ActionBaseMarginFirst = new Thickness(5, 5, 0, 5);
        private static Thickness ActionBaseMarginNext = new Thickness(10, 5, 0, 5);
        private static Border MakeActionBase(string path, bool is_first, bool is_edit = false)
        {
            var border = new Border();
            border.BorderThickness = ActionBaseBorderThickness;
            border.BorderBrush = Brushes.DarkSlateBlue;
            //border.CornerRadius = new CornerRadius(0, 9, 9, 0);
            //border.Background = ColorFrameNameBg;
            //border.BorderBrush = ColorFrameNameBg;
            if (is_first)
            {
                border.Margin = ActionBaseMarginFirst;
            }
            else
            {
                border.Margin = ActionBaseMarginNext;
            }
            if (is_edit)
            {
                border.Padding = ActionBaseEditPadding;
            }
            else
            {
                border.Padding = ActionBasePadding;
            }

            var bind = new Binding(path + ".IsActive.Value");
            bind.Converter = ActiveActionBGColorConverter;
            border.SetBinding(Border.BackgroundProperty, bind);

            return border;
        }

        private static Thickness ActionSeparatorMargin = new Thickness(10, 10, 0, 10);
        private static TextBlock MakeActionSeparator()
        {
            var arrow = new TextBlock();
            arrow.Text = "➡";
            arrow.Margin = ActionSeparatorMargin;

            return arrow;
        }

        private static UIElement MakeActionDisp(AutoTxAction action, string path, bool is_first)
        {
            var border = MakeActionBase(path, is_first);
            var tb = new TextBlock();


            string name;
            if (Setting.Data.Comm.DisplayId)
            {
                name = $"[{action.Id}] {action.Alias}";
            }
            else
            {
                name = action.Alias;
            }
            tb.Text = name;
            
            border.Child = tb;
            return border;
        }


        private static Thickness ActionEditNameMargin = new Thickness(0, 2, 0, 0);
        private static Thickness ActionEditUnitMargin = new Thickness(5, 2, 0, 0);
        private static Thickness ActionEditJumpMarginSelectActionId = new Thickness(5, 0, 0, 0);
        private static UIElement MakeActionEditJump(AutoTxJob job, AutoTxAction action, string path, bool is_first)
        {
            var border = MakeActionBase(path, is_first, true);
            var item = new StackPanel();
            item.Orientation = Orientation.Horizontal;

            // Jumpラベル
            var tb = new TextBlock();
            string name;
            if (Setting.Data.Comm.DisplayId)
            {
                name = $"[{action.Id}] {action.Alias}";
            }
            else
            {
                name = action.Alias;
            }
            tb.Text = name;
            tb.Margin = ActionEditNameMargin;
            item.Children.Add(tb);

            // Jump先
            if (action.AutoTxJobIndex.Value == -1)
            {
                // 自Job内ジャンプ
                var select_action_id = new ComboBox();
                select_action_id.Margin = ActionEditJumpMarginSelectActionId;
                var bind = new Binding($"AutoTxJobs[{job.Id}].ActionIdList");
                select_action_id.SetBinding(ComboBox.ItemsSourceProperty, bind);
                bind = new Binding($"AutoTxJobs[{job.Id}].Actions[{action.Id}].JumpTo.Value");
                select_action_id.SetBinding(ComboBox.SelectedIndexProperty, bind);
                item.Children.Add(select_action_id);
            }
            else
            {
                // 他Job内ジャンプ
                // Job選択ComboBox
                var select_job_id = new ComboBox();
                select_job_id.IsSynchronizedWithCurrentItem = true;
                select_job_id.Margin = ActionEditJumpMarginSelectActionId;
                var bind = new Binding($"AutoTxJobs");
                select_job_id.SetBinding(ComboBox.ItemsSourceProperty, bind);
                select_job_id.DisplayMemberPath = "Name";
                bind = new Binding($"AutoTxJobs[{job.Id}].Actions[{action.Id}].AutoTxJobIndex.Value");
                select_job_id.SetBinding(ComboBox.SelectedIndexProperty, bind);
                item.Children.Add(select_job_id);
                // Action選択ComboBox
                var select_action_id = new ComboBox();
                select_action_id.IsSynchronizedWithCurrentItem = true;
                select_action_id.Margin = ActionEditJumpMarginSelectActionId;
                bind = new Binding($"AutoTxJobs/ActionIdList");
                select_action_id.SetBinding(ComboBox.ItemsSourceProperty, bind);
                bind = new Binding($"AutoTxJobs[{job.Id}].Actions[{action.Id}].JumpTo.Value");
                select_action_id.SetBinding(ComboBox.SelectedIndexProperty, bind);
                item.Children.Add(select_action_id);
            }

            border.Child = item;
            return border;
        }

        private static AutoTxGuiWaitEditConverter AutoTxActionWaitConverter = new AutoTxGuiWaitEditConverter();
        private static Thickness ActionEditWaitMargin = new Thickness(5, 0, 0, 0);
        private static UIElement MakeActionEditWait(AutoTxJob job, AutoTxAction action, string path, bool is_first)
        {
            var border = MakeActionBase(path, is_first, true);
            var item = new StackPanel();
            item.Orientation = Orientation.Horizontal;

            // Waitラベル
            var tb = new TextBlock();
            string name;
            if (Setting.Data.Comm.DisplayId)
            {
                name = $"[{action.Id}] {action.Alias}";
            }
            else
            {
                name = action.Alias;
            }
            tb.Text = name;
            tb.Margin = ActionEditNameMargin;
            item.Children.Add(tb);

            // Waitテキストボックス
            var wait_edit = new TextBox();
            wait_edit.Width = 50;
            wait_edit.Margin = ActionEditWaitMargin;
            var bind = new Binding(path + ".WaitTime.Value");
            bind.Converter = AutoTxActionWaitConverter;
            wait_edit.SetBinding(TextBox.TextProperty, bind);
            item.Children.Add(wait_edit);
            // 単位表示
            var unit = new TextBlock();
            unit.Text = "ms";
            unit.Margin = ActionEditUnitMargin;
            item.Children.Add(unit);

            border.Child = item;
            return border;
        }
    }
}
