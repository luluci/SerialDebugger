
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
const MatchProgress = 0;
const MatchFailed = 1;
const MatchSuccess = 2;
var Rx_ptn3_state = 0;

const Rx_ptn3_init = (frame_id) => {
	Rx_ptn3_state = 0;
}

const Rx_ptn3_match = (frame_id) => {
	let result = MatchProgress;
	Comm.Rx.Debug = 255;
	
	//
	switch (Rx_ptn3_state) {
		case 0:
			if (Comm.Rx.Data == 240) {
				Rx_ptn3_state = 1;
				Comm.Rx.AddLog(frame_id, "Header");
			} else {
				result = MatchFailed;
			}
			break;
		case 1:
			if (Comm.Rx.Data == 1) {
				Rx_ptn3_state = 2;
				Comm.Rx.AddLog(frame_id, "Frame_1");
			} else {
				result = MatchFailed;
			}
			break;
		case 2:
			Rx_ptn3_state = 3;
			Comm.Rx.AddLog(frame_id, "any");
			break;
		case 3:
			Rx_ptn3_state = 4;
			Comm.Rx.AddLog(frame_id, "any");
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

