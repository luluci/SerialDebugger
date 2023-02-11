
var Comm;
var CommAsync;

const Comm_Loaded = () => {
	Comm = chrome.webview.hostObjects.sync.Comm;
    CommAsync = chrome.webview.hostObjects.Comm;

    return true;
}

// ↑変更しないこと↑



const Comm_Open = () => {
}




const CommDebug = () => {
    Comm.Debug();
    return "finish";
}
