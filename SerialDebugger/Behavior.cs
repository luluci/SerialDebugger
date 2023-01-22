using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using Microsoft.Xaml.Behaviors;

namespace SerialDebugger
{
    public interface IClosing
    {
        /// <summary>
        /// Executes when window is closing
        /// </summary>
        /// <returns>Whether the windows should be closed by the caller</returns>
        bool OnClosing();
    }

    public class WindowClosingBehavior : Behavior<Window>
    {
        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.Closing += Window_Closing;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.Closing -= Window_Closing;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            var window = sender as Window;

            //ViewModelがインターフェイスを実装していたらメソッドを実行する
            if (window.DataContext is IClosing)
                e.Cancel = (window.DataContext as IClosing).OnClosing();
        }
    }
}
