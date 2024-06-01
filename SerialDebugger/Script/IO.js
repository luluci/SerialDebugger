// IO

// C# 連携オブジェクト
// C#内でWebView2初期化後にJavaScript初期設定を実施。
// chrome.webview.hostObjects.*で直接参照すればいいが長くなるので。
var IO;
var IOAsync;

const IO_Loaded = () => {
	IO = SerialDebugger.IO;
    IOAsync = SerialDebuggerAsync.IO;
}


// ↑変更しないこと↑
