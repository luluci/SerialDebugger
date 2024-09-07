using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SerialDebugger.Utility
{
    /// <summary>
    /// Screen(a.k.a. ディスプレイ,モニタ)に関する情報を取得する
    /// </summary>
    public class Screen
    {

        static public Point GetElemPosOnWindow(Window wnd, UIElement ui)
        {
            var pt = ui.PointToScreen(new Point(0.0d, 0.0d));
            var transform = PresentationSource.FromVisual(wnd).CompositionTarget.TransformFromDevice;
            return transform.Transform(pt);
        }
    }
}
