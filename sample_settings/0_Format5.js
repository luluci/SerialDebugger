
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

