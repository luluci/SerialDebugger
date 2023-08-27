
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
		// グラフcanvas情報初期化
		this.graph_width = 0;
		this.graph_height = 0;
		this.graph_left = 0;
		this.graph_top = 0;
		// 軸表示情報初期化
		// Y軸表示領域
		const v_axis_node = {
			is_enable: false,
			x: 0,
			y: 0,
			width: 0,
			height: 0,
			min: 0,
			max: 0,
			div_count: 1,	// 分割数
			div_value: 1,	// 値 / 1メモリ
			div_pixel: 0,	// pixel / 1メモリ
			is_dict: false,
			dict_div_tbl: {},	// dict要素を表示しているとき、dict要素値とグラフ上メモリ位置との対応付けテーブル
			dict_caption: [],
		};
		this.v_axis = [];
		this.v_axis.push(structuredClone(v_axis_node));	//[0]: left
		this.v_axis.push(structuredClone(v_axis_node));	//[1]: right
		// X軸表示領域
		const h_axis_node = {
			is_enable: false,
			x: 0,
			y: 0,
			width: 0,
			height: 0,
			min: 0,
			max: 0,
			div_count: 1,	// 分割数
			div_value: 1,	// 値 / 1メモリ
			div_pixel: 0,	// pixel / 1メモリ
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

	add_v_axis_left(idx, width, min, max, div_value) {
		this.add_v_axis_calc(0, idx, width, min, max, div_value);

		let field = Comm.Rx[0].Fields[2].GetSelecter();
		field.forEach(element => {
			this.v_axis[0].caption.push({
				Key: element.Key,
				Value: element.Disp,
			});
		});
	}

	add_v_axis_right(idx, width, min, max, div_value) {
		this.add_v_axis_calc(1, idx, width, min, max, div_value);
	}

	add_v_axis_dict(lr, width, div_value, frame_idx, field_idx) {
		this.v_axis[lr].is_enable = true;
		this.v_axis[lr].width = width;
		this.v_axis[lr].div_value = div_value;

		let count = 0;
		this.v_axis[lr].is_dict = true;

		let field = Comm.Rx[frame_idx].Fields[field_idx].GetSelecter();
		field.forEach(element => {
			// 表示内容テーブル登録
			this.v_axis[lr].dict_caption.push({
				Key: element.Key,
				Value: element.Disp,
			});
			//
			this.v_axis[lr].dict_div_tbl[element.Key] = count;

			count++;
		});

		this.v_axis[lr].min = 0;
		this.v_axis[lr].max = count - 1;
		this.v_axis[lr].div_count = count;
	}

	fix_v_axis(lr) {
		// init_graph()からコールする
		// height確定後の計算を実施
		this.v_axis[lr].div_pixel = this.v_axis[lr].height / this.v_axis[lr].div_count;
		// 描画基準点を計算
		if (lr == 0) {
			this.v_axis[0].x = 0;
			this.v_axis[0].y = 0;
		} else {
			this.v_axis[1].x = this.v_axis[0].width + this.graph_width;
			this.v_axis[1].y = 0;
		}
	}

	add_h_axis(idx, height, min, max) {
		this.h_axis.height = height;
	}

	// canvas初期化
	init_graph() {
		// グラフ要素サイズ計算
		this.init_graph_calc_size();
		// canvas要素調整
		this.init_graph_canvas_fix();
		// canvas初期化
		this.init_canvas_elem();
		// 軸描画
		this.draw_v_axis_right();
	}
	//
	init_graph_calc_size() {
		// グラフcanvas情報計算
		// グラフcanvasサイズ、座標を計算
		this.graph_width = this.base_width - this.v_axis[0].width - this.v_axis[1].width;
		this.graph_height = this.base_height - this.h_axis.height;
		this.graph_left = this.v_axis[0].width;
		this.graph_top = 0;
		// 軸表示領域計算
		// ここまでで add_v_axis/add_h_axis は完了していること
		this.v_axis[0].height = this.graph_height;
		this.v_axis[1].height = this.graph_height;
		this.h_axis.width = this.graph_width;
		// 
		this.fix_v_axis(0);
		this.fix_v_axis(1);
	}
	init_graph_canvas_fix() {
		// canvas要素リサイズ
		// 背景canvasサイズ＝全体サイズ
		this.canvas_bk.width = this.base_width;
		this.canvas_bk.height = this.base_height;
		// グラフcanvasリサイズ
		this.canvas_graph.width = this.graph_width;
		this.canvas_graph.height = this.graph_height;
		this.canvas_graph.style.left = this.graph_left + "px";
		this.canvas_graph.style.top = this.graph_top + "px";
	}

	init_canvas_elem() {
		// canvas要素の初期化
		// canvas設定
		// https://developer.mozilla.org/ja/docs/Web/API/Canvas_API/Tutorial/Optimizing_canvas
		// 透過をやめる
		this.ctx = this.canvas_graph.getContext('2d', { alpha: false });
		//this.ctx = this.elem.getContext('2d');
		this.ctx_bk = this.canvas_bk.getContext('2d', { alpha: false });

		// canvas背景色初期化
		this.ctx.fillStyle = "rgb(220,220,220)";
		this.ctx.fillRect(0, 0, this.graph_width, this.graph_height);
		this.ctx_bk.fillStyle = "rgb(255,255,255)";
		this.ctx_bk.fillRect(0, 0, this.base_width, this.base_height);
	}

	draw_v_axis_right() {
		// 縦軸(右)描画
		// 座標系は左上が(0,0)
		let x = this.v_axis[1].x;
		let y = this.v_axis[1].y;
		let y2 = y + this.v_axis[1].height;
		// 縦線描画
		this.ctx_bk.strokeStyle = 'rgb(0, 200, 0)';
		this.ctx_bk.moveTo(x, y);
		this.ctx_bk.lineTo(x, y2);
		this.ctx_bk.stroke();
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
	//graph_drawer.add_v_axis_left(0, 50, 0, 10, 1);
	//graph_drawer.add_v_axis_right(1, 100, 0, 10, 1);
	graph_drawer.add_v_axis_dict(1, 100, 1, 0, 2);
	graph_drawer.add_h_axis(0, 50, 0, 10);
	// グラフ表示初期化：軸描画、グラフ領域初期化
	graph_drawer.init_graph();
	//
	ofs = IO.GetFileAutoName("./log_js/", "log");
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
