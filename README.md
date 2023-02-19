# SerialDebugger

## Folder構成

---

    SerialDebugger/
        Settings/
            <User定義設定ファイル>.json
            <User定義スクリプトファイル>.js
        Script/
            index.html
            Comm.js
            Settings.js
        SerialDebugger.exe
        WebView2Loader.dll

---

### Settings

jsonファイルに通信フレームを定義する。  
jsonファイル内で指定するjsファイルもここに配置する。

### Script

#### index.html

WebView2連携ではindex.html上でJavaScriptを実行する。  
html自体も別ウインドウに表示できるため、JavaScriptからDOM操作して独自表示をすることも可能。

#### Comm.js

シリアル通信実行時に使用するC# interfaceや初期化スクリプトを記載している。
必要に応じて共通処理を記載することも可能。

#### Settings.js

設定ファイル読み込み時に使用するC# interfaceや初期化スクリプトを記載している。
必要に応じて共通処理を記載することも可能。

### WebView2Loader.dll

WebView2連携ライブラリがこのファイルを実行時にロードするが、exeファイルに結合してしまうと見つけられなくなるためdllファイルとして独立した状態で配置している。

## Setting File Format 概要

jsonフォーマットで送信設定、受信解析設定、自動送信設定、その他ツール動作設定を定義する。

---

| Setting | Format | Description |
----|----|---- 
| name | string | 設定名称。ツール上で設定ファイルを選択するときの表示に使われる。
| log | object | ログファイルに関する設定。
| output | object | ツールからの出力に関する設定。
| gui | object | GUIに関する設定。
| serial | object | COM通信設定初期値。ツール上からも変更可能。
| comm | object | 送信設定、受信解析設定、自動送信設定。
| script | array | ロードするJavaScriptを指定可能。

---

    {
        "name", "Setting Name",
        "log": {
            ...
        },
        "output": {
            ...
        },
        "gui": {
            ...
        },
        "serial": {
            ...
        },
        "script": {
            ...
        },
        "comm": {
            ...
        }
    }


### name

| Setting | Format | Description |
----|----|---- 
| name | string | 設定名称。ツール上で設定ファイルを選択するときの表示に使われる。


### serial

| Setting | Format | Description |
----|----|---- 
| baudrate | number | (省略時:9600)
| data_bits | number | (省略時:8)
| parity | string | "even", "odd", "mark", "space", "none" (省略時:none)
| stop_bits | number | 1, 1.5, 2, 0 (省略時:0)
| rts_cts | bool | RTS有効無効設定
| datxon_xoffa_bits | bool | XOn/XOff有効無効設定
| dtr_str | bool | DTR有効無効設定
| tx_timeout | number |(msec/LSB) 送信タイムアウト設定。 (省略時:500)
| rx_timeout | number |(msec/LSB) 受信タイムアウト設定。最後にデータ受信があった時間から受信タイムアウト設定時間が経過したら受信確定して、次の受信解析に移行する。 (省略時:500)
| polling_cycle | number |(msec/LSB) 受信解析および自動送信処理を周期的に実行している。その周期時間設定。

---

	"serial": {
		"baudrate": 9600,
		"data_bits": 8,
		"parity": "None",
		"stop_bits": 0,
		"rts_cts": false,
		"xon_xoff": false,
		"dtr_str": false,
		"tx_timeout": 500,
		"rx_timeout": 500,
		"polling_cycle": 100
	}

### script

| Setting | Format | Description |
----|----|---- 
| (array) | string | Settingsディレクトリをカレントディレクトリとして、指定したファイルを&lt;script&gt;タグでindex.htmlに挿入する。<br>複数指定可能。他設定ファイルでファイルに重複があったときは最初に出現した一度のみ挿入する。

---

	"script": [
		"<JavaScriptファイル>.js"
	]


### comm

| Setting | Format | Description |
----|----|---- 
| display_id | bool | 以下comm設定にそれぞれIDが振られている。IDをGUI上に表示するか選択する。script内では主にこのIDで各データを指定して操作するため主目的はその設定補助。 (省略時:false)
| tx | object | 送信フレーム定義
| rx | object | 受信フレーム定義
| auto_tx | object | 自動操作手順定義

---

	"comm": {
		"display_id": false
		"tx": {
			...
		},
		"rx": {
			...
		},
		"auto_tx": {
			...
		}
	}

#### tx

| Setting | Format | Description |
----|----|---- 
| invert_bit | bool | データ送信時にビット反転して送出するかどうかを設定。(省略時:false)
| frames | array | 送信フレーム定義を配列で入力する。先頭から順に0始まりでIDを割り振る。
| frames.name | string | 送信フレーム名称定義。重複不可。
| frames.as_ascii | array | データ送信時にバイトをHEX文字列に変換するかどうかを設定。little-endianでバッファに格納する。(例: 0xAB -> "AB" -> 0x42,0x41)(省略時:false)
| frames.fields | array | 送信フレームを構成するフィールドを配列で指定。フィールド設定詳細は後述。
| frames.backup_buffer_size | array | GUI操作を直接反映する送信バッファとは別に、設定値をバックアップしておくバッファを作成可能。0でバックアップバッファ無し。1以上でバックアップバッファを作成する。backup_buffersの定義数と多いほうをバックアップバッファ数とする。(省略時:0)
| frames.backup_buffers | array | バックアップバッファ初期値付き設定。詳細は後述。

---

	"tx": {
		"invert_bit": false,
		"frames": [
			{
				"name": "frame_name",
				"as_ascii", false,
				"fields": [ ... ],
				"backup_buffer_size", 1,
				"backup_buffers": [ ... ]
			},
			{
				...
			}
		]
	}

---

##### fields

| Setting | Format | Description |
----|----|---- 
| name | string | field名称を指定。name+bit_sizeかmulti_nameのどちらかを必ず指定する。multi_name指定時はname,bit_sizeは不使用。
| bit_size | number | fieldビットサイズを指定。multi_name指定時はname,bit_sizeは不使用。(Max:32)
| multi_name | array | field内に複数名称を設定する。GUI表示に影響し、field自体は1つのfieldとして扱う。fieldのビットサイズはmulti_nameで指定されたものの合計を使う。ビットサイズ合計は最大32bit。
| value | number | field初期値
| base | number | 10 or 16<br>GUI表示上の基数設定。ログ表示にも影響する。(省略時:16)
| min | number | (不使用)
| max | number | (不使用)
| type | string | field入力方式指定。type指定に対応した unit/dict/time/script/checksum/char/string のいずれかを指定する。詳細は後述。

	{
		"name": "field_name",
		"bit_size": 8,
		"multi_name": [
			{
				"name": "inner_name_1",
				"bit_size": 3,
			},
			{
				"name": "inner_name_2",
				"bit_size": 5,
			}
		]
		"value": 8,
		"min": 8,
		"max": 8,
		"base": 8,
		"type": "Fix",
		"unit": { ... },
		"dict": [ ... ],
		"time": { ... },
		"script": { ... },
		"checksum": { ... },
		"char": "C",
		"string": "String"
	}

###### field.Fix

初期値で固定。

| Setting | Format | Description |
----|----|---- 
| type | string | "Fix" or 省略

	{
		"name": "field_name", "bit_size": 8, "value": 8
	}

or

	{
		"name": "field_name", "bit_size": 8, "value": 8,
		"type": "Edit"
	}

###### field.Edit

テキストボックスでの設定値変更可能。

| Setting | Format | Description |
----|----|---- 
| type | string | "Edit"

	{
		"name": "field_name", "bit_size": 8, "value": 8,
		"type": "Edit"
	}

###### field.Unit

unit設定から生成する入力値をコンボボックスから指定。  
bit_sizeが2以上のときは直接編集するエディットボックスも表示する。

| Setting | Format | Description |
----|----|---- 
| type | string | "Unit"
| unit | string | unitから生成する数値の接尾辞として付与する。
| lsb | number | "lsb > 0" のときは "disp_max > disp_min" とすること。<br>"lsb < 0" のときは "disp_max < disp_min" とすること。<br>"lsb = 0" は設定不可。
| disp_max | number |^
| disp_min | number |^
| value_min | number |^
| format | string |^

---

disp_strをコンボボックス上の表示とする。valueが対応する設定値となる。


	double value = value_min;
	double disp = disp_min;
	for (; disp < disp_max && value < (field最大値); disp += lsb, value++) {
		disp_str = disp.ToString(format) + unit;
	}

---

	{
		"name": "field_name", "bit_size": 8, "value": 8,
		"type": "Unit",
		"unit": {
			"unit": " Hz",
			"lsb": 0.1,
			"disp_max": 100.0,
			"disp_min": 50.0,
			"value_min": 50.0,
			"format": "F1"
		}
	}

↓

	{
		50: "50.0 Hz",
		51: "50.1 Hz",
		52: "50.2 Hz",
			...
		254: "70.4 Hz",
		255: "70.5 Hz"
	}

###### field.Dict

dict設定した値をコンボボックスから指定。  
bit_sizeが2以上のときは直接編集するエディットボックスも表示する。

| Setting | Format | Description |
----|----|---- 
| type | string | "Dict"
| dict | array | 値と表示のペアを配列で設定する。
| dict.value | number | 
| dict.disp | string | 

---

	{
		"name": "field_name",
		"bit_size": 4,
		"value": 0,
		"type": "Dict",
		"dict": [
			{ "value": 0, "disp": "設定0" },
			{ "value": 1, "disp": "設定1" },
			{ "value": 2, "disp": "設定2" }
		]
	}


###### field.Time

###### field.Char

###### field.String

###### field.Checksum

###### field.Script



### gui

| Setting | Format | Description |
----|----|---- 
| window | object | ウインドウサイズを指定可能。
| column_order | object | 通信フレーム表示の各列の表示順序を指定可能。
| column_width | object | 通信フレーム表示の各列の表示横幅を指定可能。

---

#### column_order, column_width

| Setting | Format | Description |
----|----|---- 
| byte_index | number | Byte番号表示列
| bit_index | number | Bit番号表示列
| field_value | number | field設定値(HEX)表示列
| field_name | number | field名称表示列
| field_input | number | field設定値表示列
| tx_bytes | number | 送信データ(1byte区切り)表示列
| spacer | number | 空白スペース列
| tx_buffer | number | 送信データバックアップ領域表示列

---

	"gui": {
		"window": {
			"width": number,
			"height": number
		},
		"column_order": {
			"byte_index": 0,
			"bit_index": 1,
			"field_value": 2,
			"field_name": 3,
			"field_input": 4,
			"tx_bytes": 5,
			"spacer": 6,
			"tx_buffer": 7
		},
		"column_width": {
			"byte_index": 25,
			"bit_index": 25,
			"field_value": 40,
			"field_name": 80,
			"field_input": 80,
			"tx_bytes": 50,
			"spacer": 10,
			"tx_buffer": 80
        }
	}

### log

| Setting | Format | Description |
----|----|---- 
| directory | string | SerialDebugger.exeがあるフォルダを起点にログファイルを格納するパスを指定する。<br>省略可。省略時はログを保存しない。
| max_size | number | GUI上ログ表示を何件まで保持するかを設定する。

---

	"log": {
		"directory": "path",
		"max_size": 100
	}

### output

| Setting | Format | Description |
----|----|---- 
| drag_drop | object | GUI上の通信フィールド名称部分をDrag&Drop可能。フィールド名称、設定値をテキストでエクスポートする。本設定で対象情報の前後にテキストを挿入可能。html形式でのDropデータ作成を想定している。

#### drag_drop

| Setting | Format | Description |
----|----|---- 
| body | object | Dropデータ全体を囲う設定。省略時は何も囲わない。
| item | object | frame名称とfield名称、field設定値のセットを囲う設定。省略時は何も囲わない。
| frame_name | object | field名称を囲う設定。省略時はframe名称をDropデータから除外する。
| field_name | object | field名称を囲う設定。省略時はfield名称とfield設定値のセットをDropデータから除外する。
| field_value | object | field設定値を囲う設定。省略時はfield名称とfield設定値のセットをDropデータから除外する。
| field_inner_name | object | inner_field名称を囲う設定。省略時はfield名称とfield設定値のセットをDropデータから除外する。
| field_inner_value | object | inner_field設定値を囲う設定。省略時はfield名称とfield設定値のセットをDropデータから除外する。
| value_format | string | "Input" or 省略<br>"Input"指定時はfieldで定義した入力指定を元にfield設定値をテキスト化する。省略時は16進数文字列となる。

---

    "output": {
        "drag_drop": {  
            "body": {
                "begin":    "string",
                "end":      "string"
            },
            "item": {
                "begin":    "string",
                "end":      "string"
            },
            "frame_name": {
                "begin":    "string",
                "end":      "string"
            },
            "field_name": {
                "begin":    "string",
                "end":      "string"
            },
            "field_value": {
                "begin":    "string",
                "end":      "string"
            },
            "field_inner_name": {
                "begin":    "string",
                "end":      "string"
                    
            },
            "field_inner_value": {
                "begin":    "string",
                "end":      "string"
                    
            },
            "value_format": "Input"
        }
    }



