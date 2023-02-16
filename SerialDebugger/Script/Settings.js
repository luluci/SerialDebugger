// Settings/Field/Script用関数

// C# 連携オブジェクト
// C#内でWebView2初期化後にComm_Loaded()を呼び出して初期設定している。
// chrome.webview.hostObjects.*で直接参照すればいいが長くなるので。
var Settings;
var SettingsAsync;

const Settings_Loaded = () => {
	Settings = chrome.webview.hostObjects.sync.Settings;
    SettingsAsync = chrome.webview.hostObjects.Settings;

    return true;
}

const MakeFieldExecScript = (func, count) => {
    for (let i = 0; i < count; i++) {
        const result = func(i);
        Settings.Field.AddSelecter(result.key, result.value);
    }
}


// ↑変更しないこと↑
