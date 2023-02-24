// C# 連携オブジェクト
// C#内でWebView2初期化後にJavaScript初期設定を実施。
// chrome.webview.hostObjects.*で直接参照すればいいが長くなるので。
var Comm;
var CommAsync;

var MatchProgress;
var MatchFailed;
var MatchSuccess;


const Comm_Loaded = () => {
	Comm = chrome.webview.hostObjects.sync.Comm;
    CommAsync = chrome.webview.hostObjects.Comm;

    MatchProgress = Comm.Rx.MatchProgress;
    MatchFailed = Comm.Rx.MatchFailed;
    MatchSuccess = Comm.Rx.MatchSuccess;
}

// ↑変更しないこと↑

// C#側でExecuteScriptAsync()から実行するスクリプト記載補足
// 関数コール等の1命令だけならそのまま記載でいい。
/*
  ExecuteScriptAsync("Comm_Loaded()")
 */
// 複数文を記載して実行したい場合は
// IIFE (即時実行関数式)
// で記載する。そうしないと実行されない。
/*
var script = @"(() => {
    try {
        CommDebug();
        //throw new Error('error');
        //Comm.Debug();
        //return true;
    }
    catch (e) {
        // ↓
        Comm.Error(e.message);
        return e.message;
    }
    return true;
})();";
ExecuteScriptAsync(script);
*/

const CommDebug = () => {
    let result;
    //Comm.Tx[0] += 1;
    //result = Comm.Tx[0];
    Comm.Tx[0][0][1] += 0xFF;
    result = Comm.Tx[0][0][1];
    Comm.Tx.Fix(0,0);
    //Comm?.Debug();
    //throw new Error('error');
    //let Commm;
    //Commm.Debug();
    /*
    try {
        throw new Error('error');
        Comm.Debug();
    }
    catch (e) {
        Comm.Error(e.message);
    }
    */

    return result;
}

// ↑ C#-WebView2 連携の補足 ↑


const Comm_Open = () => {
}

