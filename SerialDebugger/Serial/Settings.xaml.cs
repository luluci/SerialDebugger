﻿using System;
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

namespace SerialDebugger.Serial
{
    /// <summary>
    /// Settings1.xaml の相互作用ロジック
    /// </summary>
    public partial class Settings : UserControl
    {
        public SettingsViewModel vm;

        public Settings()
        {
            InitializeComponent();

            // ViewModel
            vm = new SettingsViewModel();
            DataContext = vm;

            //
        }
    }
}
