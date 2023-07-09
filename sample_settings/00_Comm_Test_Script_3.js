
// AutoTx Script
// Scriptから全自動操作サンプル
var autoproc_state = 0;
const Job_AutoScript = () => {
	let stop = false;
	let rx_check = false;

	switch (autoproc_state) {
		case 0:
			// Tx送信
			Comm.Tx.Send(0,0);
			// next
			autoproc_state++;
			break;

		case 1:
			// Rxチェック
			for (let i = 0; i < Comm.RxMatch.Count; i++) {
				if (Comm.RxMatch[i].FrameId == 0 && Comm.RxMatch[i].PatternId == 0) {
					rx_check = true;
				}
			}
			if (rx_check) {
				//Utility.Log("[Script] Rx OK");
				Comm.RxMatch.Clear();
				// next
				autoproc_state++;
			}
			//Utility.Log("[Script] Rx Wait..");
			break;

		case 2:
			if (Comm.Tx[0][0][3] < 14) {
				Comm.Tx[0][0][3]++;
			}
			else {
				Comm.Tx[0][0][3] = 0;
				stop = true;
			}
			Comm.Tx.Fix(0, 0);
			// next
			autoproc_state = 0;
			Utility.Log("[Script] Change Value.");
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

