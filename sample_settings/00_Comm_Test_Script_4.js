
class graph_drawer_t {
	constructor(graph_elem, canvas_elem) {
		// グラフエリア要素
		this.graph_area = graph_elem;
		// canvas要素
		this.elem = canvas_elem;
		// 系列情報
		this.series = [];

		// canvas要素のサイズをGrid(html/css)定義に合わせて自動調整
		// canvasのparentを取得
		const canvas_div = this.graph_area.getElementsByClassName("graph")[0];
		this.elem.setAttribute("width", canvas_div.clientWidth);
		this.elem.setAttribute("height", canvas_div.clientHeight);
		// canvas情報
		this.width = this.elem.width;
		this.height = this.elem.height;

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
const graph_drawer = new graph_drawer_t(graph_area, canvas);
// init
const Job_GraphDraw_init = () => {
	// 系列登録
	graph_drawer.add_series("data1");
	// 初期化終了
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
