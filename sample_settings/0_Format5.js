
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

