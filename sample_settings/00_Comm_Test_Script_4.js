
class graph_drawer_t {
	constructor(canvas_elem) {
		// canvas要素
		this.elem = canvas_elem;
		// canvas情報
		this.width = this.elem.width;
		this.height = this.elem.height;
		// 系列情報
		this.series = [];

		// canvas設定
		// https://developer.mozilla.org/ja/docs/Web/API/Canvas_API/Tutorial/Optimizing_canvas
		// 透過をやめる
		this.ctx = this.elem.getContext('2d', { alpha: false });
		//this.ctx = this.elem.getContext('2d');

		// canvas背景色初期化
		this.ctx.fillStyle = "rgb(220,220,220)";
		this.ctx.fillRect(0, 0, this.width, this.height);
	}

	add_series(name) {
		this.series.push({
			name: name,
			plot_x: null,
			plot_y: null,
		});
	}

	plot_init(idx, x, y) {
		//
		x = Math.floor(x);
		y = Math.floor(y);
		// グラフの始点を初期化
		let series = this.series[idx];
		series.plot_x = x;
		series.plot_y = y;
		// 必要に応じて始点に丸を書く
		
	}

	plot(idx, x, y) {
		//
		x = Math.floor(x);
		y = Math.floor(y);
		//
		let series = this.series[idx];
		//
		//this.ctx.fillStyle = 'rgb(200, 0, 0)';
		this.ctx.strokeStyle = 'rgb(200, 0, 0)';
		this.ctx.beginPath();
		Utility.Log("[Script] series[" + idx + "]:moveTo(" + series.plot_x + ", " + series.plot_y + ")");
		this.ctx.moveTo(series.plot_x, series.plot_y);
		this.ctx.lineTo(x, y);
		this.ctx.stroke();
		//
		series.plot_x = x;
		series.plot_y = y;
		// 

	}


}

// AutoTx Script
var graph_drawer;
// init
const Job_GraphDraw_init = () => {
	graph_drawer = new graph_drawer_t(canvas);

	graph_drawer.add_series("data1");
	//
	Comm.AutoTx.Result = true;
}
// Scriptから全自動操作サンプル
var proc_state = 0;
const Job_GraphDraw_proc = () => {
	let stop = false;
	//
	if (proc_state == 0) {
		graph_drawer.plot_init(0, 5, 5);
		proc_state++;
	} else {
		graph_drawer.plot(0, 15 + (proc_state * 5), 20 + (proc_state * 5));
		proc_state++;

		if (proc_state > 5) {
			stop = true;
		}
	}
	//
	Comm.AutoTx.Result = stop;
}
