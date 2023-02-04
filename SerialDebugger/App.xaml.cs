using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SerialDebugger
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Reactive.Bindings.ReactivePropertyScheduler.SetDefault(new Reactive.Bindings.Schedulers.ReactivePropertyWpfScheduler(Dispatcher));
            //
            EventManager.RegisterClassHandler(typeof(TextBox), TextBox.KeyDownEvent, new KeyEventHandler(UIElement_MoveNext));
        }

        public App()
        {
            var font = new System.Windows.Media.FontFamily("Meiryo UI");

            var style = new Style(typeof(Window));
            style.Setters.Add(new Setter(Window.FontFamilyProperty, font));

            FrameworkElement.StyleProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata(style));
        }


        void UIElement_MoveNext(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                MoveToNextUIElement(e);
            }
        }
        void MoveToNextUIElement(KeyEventArgs e)
        {
            var elementWithFocus = Keyboard.FocusedElement as UIElement;
            if (!(elementWithFocus is null))
            {
                var focusDirectionNext = FocusNavigationDirection.Next;
                var focusDirectionPrev = FocusNavigationDirection.Previous;
                var requestNext = new TraversalRequest(focusDirectionNext);
                var requestPrev = new TraversalRequest(focusDirectionPrev);
                elementWithFocus.MoveFocus(requestNext);

                (Keyboard.FocusedElement as UIElement)?.MoveFocus(requestPrev);
            }
        }
    }
}
