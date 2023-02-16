
const Setting_Selecter_Script_2 = (count) => {
	for (let i = 0; i < count; i++) {
		const unit = " (0.2/LSB)";
		const base = 22.2;
		const lsb = 0.2;
		let key = i;
		if ((i % 2) == 1) {
			key <<= 4;
		}
		let value = (base + lsb * i).toFixed(1) + unit;
		Settings.Field.AddSelecter(key, value);
	}
}

