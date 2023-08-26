
class graph_drawer_t {


	constructor(graph_elem) {
		// グラフエリア要素
		this.graph_area = graph_elem;
		// 系列情報
		this.series = [];

		// canvas要素取得
		// canvasのベースdiv
		const canvas_div = this.graph_area.getElementsByClassName("graph")[0];
		// canvas要素
		this.canvas_bk = canvas_div.getElementsByClassName('graph_canvas_1')[0];
		this.canvas_graph = canvas_div.getElementsByClassName('graph_canvas_2')[0];

		// canvas設定初期化
		// canvas全体サイズ
		// canvas要素のサイズをGrid(html/css)定義に合わせて自動調整
		const canvas_div_w = canvas_div.clientWidth;
		const canvas_div_h = canvas_div.clientHeight;
		this.base_width = canvas_div_w;
		this.base_height = canvas_div_h;
		// 背景canvasサイズ＝全体サイズ
		this.canvas_bk.width = this.base_width;
		this.canvas_bk.height = this.base_height;
		// グラフcanvas情報初期化
		this.graph_width = 0;
		this.graph_height = 0;
		this.graph_left = 0;
		this.graph_top = 0;
		// 軸表示情報初期化
		// Y軸表示領域
		const v_axis_node = {
			width: 0,
			height: 0,
			min: 0,
			max: 0,
			div: 0,	// 1メモリの高さ
			caption: [],
		};
		this.v_axis = [];
		this.v_axis.push(structuredClone(v_axis_node));	//[0]: left
		this.v_axis.push(structuredClone(v_axis_node));	//[1]: right
		// X軸表示領域
		const h_axis_node = {
			width: 0,
			height: 0,
			min: 0,
			max: 0,
			div: 0,	// 1メモリの幅
		}
		this.h_axis = structuredClone(h_axis_node);
	}

	add_series(name) {
		this.series.push({
			name: name,
			plot_x: null,
			plot_y: null,
		});
	}

	add_v_axis_left(idx, width, min, max) {
		this.v_axis[0].width = width;
		this.v_axis[0].min = min;
		this.v_axis[0].max = max;

		let field = Comm.Rx[0].Fields[2].GetSelecter();
		field.forEach(element => {
			this.v_axis[0].caption.push({
				Key: element.Key,
				Value: element.Disp,
			});
		});
	}

	add_v_axis_right(idx, width, min, max) {
		this.v_axis[1].width = width;
		this.v_axis[1].min = min;
		this.v_axis[1].max = max;
	}

	add_h_axis(idx, height, min, max) {
		this.h_axis.height = height;
	}

	// canvas初期化
	init_canvas() {
		// グラフcanvas情報計算
		this.graph_width = this.base_width - this.v_axis[0].width - this.v_axis[1].width;
		this.graph_height = this.base_height - this.h_axis.height;
		this.graph_left = this.v_axis[0].width;
		this.graph_top = 0;
		// 軸表示領域計算
		this.v_axis[0].height = this.graph_height;
		this.v_axis[1].height = this.graph_height;
		this.h_axis.width = this.graph_width;

		// canvas
		this.canvas_graph.width = this.graph_width;
		this.canvas_graph.height = this.graph_height;
		this.canvas_graph.style.left = this.graph_left + "px";
		this.canvas_graph.style.top = this.graph_top + "px";

		// canvas設定
		// https://developer.mozilla.org/ja/docs/Web/API/Canvas_API/Tutorial/Optimizing_canvas
		// 透過をやめる
		this.ctx = this.canvas_graph.getContext('2d', { alpha: false });
		//this.ctx = this.elem.getContext('2d');

		// canvas背景色初期化
		this.ctx.fillStyle = "rgb(200,200,200)";
		this.ctx.fillRect(0, 0, this.graph_width, this.graph_height);

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
const graph_drawer = new graph_drawer_t(graph_area);
var ofs;
// init
const Job_GraphDraw_init = () => {
	// 系列登録
	graph_drawer.add_series("data1");
	// 軸登録
	graph_drawer.add_v_axis_left(0, 50, 0, 10);
	graph_drawer.add_v_axis_right(1, 100, 0, 10);
	graph_drawer.add_h_axis(0, 50, 0, 10);
	//
	graph_drawer.init_canvas();
	//
	ofs = IO.GetFile();
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
		ofs.Write("<IO>: graph start");
	} else {
		graph_drawer.plot(0, 15 + (proc_state * 5), 20 + (proc_state * 5));
		proc_state++;

		if (proc_state > 5) {
			ofs.Write("<IO>: graph end");
			stop = true;
		}
	}
	//
	Comm.AutoTx.Result = stop;
}
