// Utility

// C# 連携オブジェクト
// C#内でWebView2初期化後にJavaScript初期設定を実施。
// chrome.webview.hostObjects.*で直接参照すればいいが長くなるので。
var Utility;
var UtilityAsync;

const Utility_Loaded = () => {
	Utility = SerialDebugger.Utility;
    UtilityAsync = SerialDebuggerAsync.Utility;
}


// ↑変更しないこと↑
