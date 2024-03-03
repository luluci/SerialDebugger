
// AutoTx Script
// Scriptから全自動操作サンプル
var autoproc_state = 0;
const Job_AutoScript = () => {
	let stop = false;
	let rx_check = false;
	let result = false;

	// if (!Comm.IsSerialOpen()) {
	// 	Comm.AutoTx.Result = false;
	// 	return;
	// }

	switch (autoproc_state) {
		case 0:
			if (Comm.IsSerialOpen()) {
				autoproc_state++;
			} else {
				// COMポートリストを取得
				Comm.RefreshComPortList();
				let com_ports = Comm.GetComPortList();
				let com_port_len = com_ports.length;
				let com_port = "COM1";
				// com_port = com_ports[com_port_len-1];
				// Utility.Log(com_port);
				// debug_console.textContent = com_port;

				result = Comm.OpenSerial(com_port);
				if (result) {
					autoproc_state++;
				}
			}
			break;

		case 1:
		// 受信解析失敗する設定にする
			Comm.Tx[0][0][1] = 111;
			Comm.Tx.Fix(0, 0);
			// Tx送信
			result = Comm.Tx.Send(0,0);
			if (result) {
				// 送信成功で次へ移行
				autoproc_state++;
			}
			break;

		case 2:
			// Timeout判定チェック
			if (Comm.RxMatch.IsTimeout) {
				Utility.Log("[Script] case 1: OK");
				Comm.RxMatch.Clear();
				// next
				autoproc_state++;
			}
			break;

		case 3:
			// 受信解析失敗する設定で再送
			Comm.Tx[0][0][1] = 111;
			Comm.Tx.Fix(0, 0);
			// Tx送信
			result = Comm.Tx.Send(0, 0);
			if (result) {
				// 送信成功で次へ移行
				autoproc_state++;
			}
			break;

		case 4:
			// Any受信判定チェック
			if (Comm.RxMatch.HasAnyRecv) {
				Utility.Log("[Script] case 3: OK");
				Comm.RxMatch.Clear();
				// next
				autoproc_state++;
			}
			break;

		case 5:
			// 受信解析成功する設定で再送
			Comm.Tx[0][0][1] = 1;
			Comm.Tx.Fix(0, 0);
			// Tx送信
			result = Comm.Tx.Send(0, 0);
			if (result) {
				// 送信成功で次へ移行
				autoproc_state++;
			}
			break;

		case 6:
			// Rxチェック
			for (let i = 0; i < Comm.RxMatch.Count; i++) {
				if (Comm.RxMatch[i].FrameId == 0 && Comm.RxMatch[i].PatternId == 0) {
					rx_check = true;
				}
			}
			if (rx_check) {
				Utility.Log("[Script] case 5: OK");
				Comm.RxMatch.Clear();
				// next
				autoproc_state++;
			}
			//Utility.Log("[Script] Rx Wait..");
			break;

		case 7:
			// 受信解析成功する設定で再送
			Comm.Tx[0][0][1] = 1;
			Comm.Tx[0][0][3]++;
			Comm.Tx.Fix(0, 0);
			// Tx送信
			result = Comm.Tx.Send(0, 0);
			if (result) {
				// 送信成功で次へ移行
				autoproc_state++;
			}
			break;

		case 8:
			// Any受信判定チェック
			if (Comm.RxMatch.HasAnyRecv) {
				Utility.Log("[Script] case 7: OK");
				Comm.RxMatch.Clear();
				// next
				autoproc_state++;
			}
			break;

		case 9:
			if (Comm.Tx[0][0][3] < 14) {
				Comm.Tx[0][0][3]++;
				Utility.Log("[Script] case 8: change value.");
			}
			else {
				// テストOK
				Comm.Tx[0][0][3] = 0;
				stop = true;
				autoproc_state = 0;
				Utility.Log("[Script] case 8: OK");
				Comm.CloseSerial();
			}
			Comm.Tx.Fix(0, 0);
			break;
	}

    
    //
	Comm.AutoTx.Result = stop;
}

const Job1_0_RxMatch = () => {
	let result = false;
	
	for (let i = 0; i < Comm.RxMatch.Count; i++) {
		if (Comm.RxMatch[i].FrameId == 0 && Comm.RxMatch[i].PatternId == 0 ) {
			result = true;
		}
	}
	
	//
	Comm.RxMatch.Result = result;
}


// Rx Script
var Rx_ptn3_state = 0;

const Rx_ptn3_init = (frame_id, pattern_id) => {
	Rx_ptn3_state = 0;
}

const Rx_ptn3_match_header = (frame_id, pattern_id) => {
	let result = MatchProgress;

	if (Comm.Rx.Data == 240) {
		Comm.Rx.AddLog(frame_id, pattern_id, "Header");
		result = MatchSuccess;
		//Utility.Log("Result: MatchSuccess(" + Comm.Rx.MatchSuccess + ")");
	} else {
		result = MatchFailed;
	}

	Comm.Rx.Result = result;
}

const Rx_ptn3_match_body = (frame_id, pattern_id) => {
	let result = MatchProgress;
	Comm.Rx.Debug = 255;
	
	//
	switch (Rx_ptn3_state) {
		case 0:
			if (Comm.Rx.Data == 1) {
				Rx_ptn3_state = 1;
				Comm.Rx.AddLog(frame_id, pattern_id, "Frame_1");
			} else {
				result = MatchFailed;
			}
			break;
		case 1:
			Rx_ptn3_state = 2;
			Comm.Rx.AddLog(frame_id, pattern_id, `any(0x${toHex(Comm.Rx.Data)})`);
			break;
		case 2:
			Rx_ptn3_state = 3;
			Comm.Rx.AddLog(frame_id, pattern_id, `any(0x${toHex(Comm.Rx.Data)})`);
			result = MatchSuccess;
			break;
		default:
			result = MatchFailed;
			break;
	}
	
	Comm.Rx.Debug = Rx_ptn3_state;
	//
	Comm.Rx.Result = result;
}

