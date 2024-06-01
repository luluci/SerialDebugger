
const OnLoad_Test_Script_5 = () => {
	Utility.Log("run: OnLoad_Test_Script_5()");
	SerialDebugger.ShowView();

	const value = Comm.Rx[0].Patterns[0][0].RawData;
	const disp = Comm.Rx[0].Patterns[0][0].Disp;
}
