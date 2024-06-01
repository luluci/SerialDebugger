using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using SerialDebugger.Serial;

namespace SerialDebugger.Script
{
    // C# <-> WebView2 インターフェース
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class WebView2Interface
    {
        public CommIf Comm { get; set; }
        public SettingsIf Settings { get; set; }
        public UtilityIf Utility { get; set; }
        public IoIf IO { get; set; }

        //
        MainWindowViewModel ToolRef { get; set; }
        View WebView2Ref { get; set; }
        Serial.Protocol ProtocolRef { get; set; }

        public WebView2Interface()
        {

            //
            Comm = new CommIf();
            Settings = new SettingsIf();
            Utility = new UtilityIf();
            IO = new IoIf();

            ToolRef = null;
            WebView2Ref = null;
            ProtocolRef = null;
        }

        internal void Init(MainWindowViewModel window, View view)
        {
            //
            ToolRef = window;
            WebView2Ref = view;
            //
            Comm.Init(window);
        }

        internal void UpdateProtocol(Protocol protocol)
        {
            ProtocolRef = protocol;
            Comm.Init(protocol);
        }


        public void ShowView()
        {
            if (!WebView2Ref.IsVisible)
            {
                WebView2Ref.Top = ToolRef.Window.Top + 10;
                WebView2Ref.Show();
            }
        }

    }
}
