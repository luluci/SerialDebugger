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

namespace SerialDebugger.Utility
{
    /// <summary>
    /// Loading.xaml の相互作用ロジック
    /// </summary>
    public partial class LoadingDot : UserControl
    {
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(LoadingDot), new PropertyMetadata(string.Empty));

        public double DotSize
        {
            get { return (double)GetValue(DotSizeProperty); }
            set { SetValue(DotSizeProperty, value); }
        }
        public static readonly DependencyProperty DotSizeProperty =
            DependencyProperty.Register(nameof(DotSize), typeof(double), typeof(LoadingDot), new PropertyMetadata(10.0));

        public bool IsFrozen
        {
            get { return (bool)GetValue(IsFrozenProperty); }
            set { SetValue(IsFrozenProperty, value); }
        }
        public static readonly DependencyProperty IsFrozenProperty =
            DependencyProperty.Register(nameof(IsFrozen), typeof(bool), typeof(LoadingDot), new PropertyMetadata(true, OnIsFrozenChanged));

        private System.Windows.Media.Animation.Storyboard storyboard;

        public LoadingDot()
        {
            InitializeComponent();
            storyboard = this.Resources["LoadingAnimation"] as System.Windows.Media.Animation.Storyboard;
        }

        private static void OnIsFrozenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var data = d as LoadingDot;
            if (!(data is null))
            {
                if (data.IsFrozen)
                {
                    data.storyboard.Stop();
                }
                else
                {
                    data.storyboard.Begin();
                }
            }
        }
    }
}
