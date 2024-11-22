

const Job_UpdateField_1 = (frm, fld, buf) => {
    Comm.Tx[frm][fld][buf]++;
}


const Job_UpdateField_2 = () => {
    Comm.Tx[0][0][0]++;
    Comm.Tx[1][0][0]++;
    Comm.Tx[2][0][0]++;
    Comm.Tx[3][0][0]++;
    Comm.Tx[4][0][0]++;
    Comm.Tx[5][0][0]++;
}

