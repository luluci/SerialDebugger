
class lib_Logger_Rx {

	constructor(dir_path, file_prefix, frm_id, ptn_id, mode) {
		this.dir_path = dir_path;
		this.file_prefix = file_prefix;
		this.fp = null;

		this.frm_id = frm_id;
		this.ptn_id = ptn_id;
		this.mode = mode;
	}

	init() {
		if (this.fp != null) {
			this.fp.Dispose();
			this.fp = null;
		}

		// Make Header
		let frm = Comm.Rx[this.frm_id];
		let fields = frm.Fields;
		if (fields.Count > 0) {
			// File Open
			this.fp = IO.GetFileAutoName(this.dir_path, this.file_prefix, ".csv", "sjis");
			//
			let header = "timestamp";
			for (let i=0; i<fields.Count; i++) {
				header += "," + fields[i].Name
			}
			this.fp.Write(header);
		}
	}
	log() {
		//
		let timestamp = Utility.GetTimestamp();
		let ptn = Comm.Rx[this.frm_id].Patterns[this.ptn_id];
		let log = `${timestamp} [Script][${ptn.Name}] `;
		//
		for (let i=0; i<ptn.Count; i++) {
			log += ptn[i].Disp + ",";
		}
		//
		this.fp.Write(log);
	}
	close() {
		if (this.fp != null) {
			this.fp.Dispose();
			this.fp = null;
		}
	}
}



const Job_ScriptTest_1 = () => {
	let count = 0;
	let data;
	let disp;

	// 
	count = Comm.Rx[0].Fields.Count;
	Utility.Log(`[Script] Rx[0].Fields.Count == ${count}`);
	// 
	count = Comm.Rx[0].Patterns.Count;
	Utility.Log(`[Script] Rx[0].Patterns.Count == ${count}`);
	// 
	count = Comm.Rx[0].Patterns[0].Count;
	Utility.Log(`[Script] Rx[0].Patterns[0].Count == ${count}`);
	// 
	count = Comm.Rx[0].Patterns[0].Matches.Count;
	Utility.Log(`[Script] Rx[0].Patterns[0].Matches.Count == ${count}`);
	// 
	disp = Comm.Rx[0].Patterns[0][0].Disp;
	Utility.Log(`[Script] Rx[0].Patterns[0][0].Disp == ${disp}`);
	data = Comm.Rx[0].Patterns[0][0].RawData;
	Utility.Log(`[Script] Rx[0].Patterns[0][0].RawData == ${data}`);


	//
	const logger = new lib_Logger_Rx("./log_js/", "script_test", 0, 0, 0);
	logger.init();
	logger.log();
	logger.close();
}

const Job_ScriptTest_2 = () => {
	// File Open
	let fp;
	// 1
	fp = IO.GetFileAutoName("./log_js/", "script_test", ".csv", "sjis");
	fp.Write("SJIS, .csv出力テスト");
	fp.Dispose();
	// 2
	fp = IO.GetFileAutoName("./log_js/", "script_test", ".csv");
	fp.Write("UTF8, .csv出力テスト");
	fp.Dispose();
	// 3
	fp = IO.GetFileAutoName("./log_js/", "script_test");
	fp.Write("UTF8, .txt出力テスト");
	fp.Dispose();
}
