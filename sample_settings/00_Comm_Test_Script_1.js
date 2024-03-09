
// AutoTx Script

const Job1_0_Format5 = () => {
    if (Comm.Tx[0][0][2] < 14) {
        Comm.Tx[0][0][2]++;
    }
    else {
        Comm.Tx[0][0][2] = 0;
    }
    Comm.Tx.Fix(0,0);
    
    //
    Comm.AutoTx.Result = true;
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


const Job_Test_Exec = () => {
	try {
		// Uint8ArrayをC#に引き渡すときはobjectになる
		//const buffer = new ArrayBuffer(32);
		//const send_data = new Uint8Array(buffer, 0, 32);
		//let send_data = new Uint8Array(16);
		//send_data[0] = 12;
		//const send_data = new Uint8Array([21, 31]);
		const send_data = [1, 2, 3];

		Comm.SendData(send_data, 3);
	}
	catch (e) {
		Comm.Error(e.message);
	}

	//
	Comm.RxMatch.Result = true;
}