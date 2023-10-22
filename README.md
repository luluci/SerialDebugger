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

| Setting | Format | Description |
----|----|---- 
| name | string | 設定名称。ツール上で設定ファイルを選択するときの表示に使われる。
| log | object | ログファイルに関する設定。
| output | object | ツールからの出力に関する設定。
| gui | object | GUIに関する設定。
| serial | object | COM通信設定初期値。ツール上からも変更可能。
| comm | object | 送信設定、受信解析設定、自動送信設定。
| script | array | ロードするJavaScriptを指定可能。

```json
{
	"name": "Setting Name",
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
```

---

### name

| Setting | Format | Description |
----|----|---- 
| name | string | 設定名称。ツール上で設定ファイルを選択するときの表示に使われる。


---

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

```json
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
```

---

### script

| Setting | Format | Description |
----|----|---- 
| (array) | string | Settingsディレクトリをカレントディレクトリとして、指定したファイルを&lt;script&gt;タグでindex.htmlに挿入する。<br>複数指定可能。他設定ファイルでファイルに重複があったときは最初に出現した一度のみ挿入する。

```json
"script": [
	"<JavaScriptファイル>.js"
]
```

---

### comm

| Setting | Format | Description |
----|----|---- 
| display_id | bool | 以下comm設定にそれぞれIDが振られている。IDをGUI上に表示するか選択する。script内では主にこのIDで各データを指定して操作するため主目的はその設定補助。 (省略時:false)
| tx | object | 送信フレーム定義
| rx | object | 受信フレーム定義
| auto_tx | object | 自動操作手順定義

```json
"comm": {
	"display_id": false,
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
```

---

### tx

| Setting | Format | Description |
----|----|---- 
| invert_bit | bool | データ送信時にビット反転して送出するかどうかを設定。(省略時:false)
| frames | array | 送信フレーム定義を配列で入力する。先頭から順に0始まりでIDを割り振る。
| frames.name | string | 送信フレーム名称定義。重複不可。
| frames.as_ascii | bool | データ送信時にバイトをHEX文字列に変換するかどうかを設定。little-endianでバッファに格納する。(例: 0xAB -> "AB" -> 0x42,0x41)(省略時:false)
| frames.log_visualize | bool | false:受信値をHEXテキストで表示<br>true:field設定で定義した表示設定を元にログを作成<br>(省略時:false)
| frames.groups | array | fieldsをグループ分けを指定。
| frames.fields | array | 送信フレームを構成するフィールドを配列で指定。フィールド設定詳細は後述。
| frames.backup_buffer_size | number | GUI操作を直接反映する送信バッファとは別に、設定値をバックアップしておくバッファを作成可能。0でバックアップバッファ無し。1以上でバックアップバッファを作成する。backup_buffersの定義数と多いほうをバックアップバッファ数とする。(省略時:0)
| frames.backup_buffers | array | バックアップバッファ初期値付き設定。詳細は後述。

```json
"tx": {
	"invert_bit": false,
	"frames": [
		{
			"name": "frame_name",
			"as_ascii": false,
			"log_visualize": false,
			"fields": [ ... ],
			"backup_buffer_size": 1,
			"backup_buffers": [ ... ]
		},
		{
			...
		}
	]
}
```

---

### groups

| Setting | Format | Description |
----|----|---- 
| name | string | group名称を指定。
| bgcolor | string | group表示背景色を指定。C#ライブラリ"ColorConverter.ConvertFromString"で変換できる形式を指定すること。
| begin | number | group開始field IDを指定。
| end | number | group終了field IDを指定。endは含む。
| id_begin | number | group内にバイト単位でのIDを生成する。


```json
{
	"name": "group1",
	"begin": 2,
	"end": 6,
	"id_begin": 1
}
```


---

### fields

| Setting | Format | Description |
----|----|---- 
| name | string | field名称を指定。name+bit_sizeかmulti_nameのどちらかを必ず指定する。multi_name指定時はname,bit_sizeは不使用。
| bit_size | number | fieldビットサイズを指定。multi_name指定時はname,bit_sizeは不使用。(Max:32)
| multi_name | array | field内に複数名称を設定する。GUI表示に影響し、field自体は1つのfieldとして扱う。fieldのビットサイズはmulti_nameで指定されたものの合計を使う。ビットサイズ合計は最大32bit。
| value | number | field初期値
| base | number | 10 or 16, (省略時:16)<br>GUI表示上の基数設定。ログ表示にも影響する。
| min | number | (不使用)
| max | number | (不使用)
| type | string | field入力方式指定。type指定に対応した unit/dict/time/script/checksum/char/string のいずれかを指定する。詳細は後述。
| endian | string | "little" or "big", 省略時は"little"<br>"big"指定時、bit_sizeはバイト単位(8の倍数)とすること。
| drag_drop | object | Output.drag_dropと共通。指定したfield専用のドラッグ＆ドロップ定義が可能。

```json
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
	],
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
```

---

### fields.Fix

初期値で固定。

| Setting | Format | Description |
----|----|---- 
| type | string | "Fix" or 省略

```json
{
	"name": "field_name", "bit_size": 8, "value": 8
}
```
or
```json
{
	"name": "field_name", "bit_size": 8, "value": 8,
	"type": "Edit"
}
```

---

### fields.Edit

テキストボックスでの設定値変更可能。

| Setting | Format | Description |
----|----|---- 
| type | string | "Edit"

```json
{
	"name": "field_name", "bit_size": 8, "value": 8,
	"type": "Edit"
}
```

---

### fields.Unit

unit設定から生成する入力値をコンボボックスから指定。  
bit_sizeが2以上のときは直接編集するエディットボックスも表示する。

| Setting | Format | Description |
----|----|---- 
| type | string | "Unit"
| unit | object | 
| unit.unit | string | unitから生成する数値の接尾辞として付与する。
| unit.lsb | number | "lsb > 0" のときは "disp_max > disp_min" とすること。<br>"lsb < 0" のときは "disp_max < disp_min" とすること。<br>"lsb = 0" は設定不可。
| unit.disp_max | number | コンボボックスに表示するリスト表示の最大値
| unit.disp_min | number | コンボボックスに表示するリスト表示の最小値
| unit.value_min | number | コンボボックスの選択肢の一番上の設定値がこの値になる
| unit.format | string | C#のToString()に渡すフォーマット指定文字列

disp_strをコンボボックス上の表示とする。valueが対応する設定値となる。

```cs
if (lsb > 0)
{
	double value = value_min;
	double disp = disp_min;
	for (; disp <= disp_max && value <= (field最大値); disp += lsb, value++) {
		disp_str = disp.ToString(format) + unit;
	}
}
else if (lsb < 0)
{
	double value = value_min;
	double disp = disp_max;
	for (; disp >= disp_min && value <= (field最大値); disp += lsb, value++) {
		disp_str = disp.ToString(format) + unit;
	}
}
else
{
	// 0はエラー
}
```

```json
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
```

↓

```
{
	50: "50.0 Hz",
	51: "50.1 Hz",
	52: "50.2 Hz",
		...
	254: "70.4 Hz",
	255: "70.5 Hz"
}
```

---

### fields.Dict

dict設定した値をコンボボックスから指定。  
bit_sizeが2以上のときは直接編集するエディットボックスも表示する。

| Setting | Format | Description |
----|----|---- 
| type | string | "Dict"
| dict | array | 値と表示のペアを配列で設定する。
| dict.value | number | 
| dict.disp | string | 

```json
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
```

---

### fields.Time

time設定から生成する入力値をコンボボックスから指定。  
bit_sizeが2以上のときは直接編集するエディットボックスも表示する。

| Setting | Format | Description |
----|----|---- 
| type | string | "Time"
| time | object | 
| time.elapse | number | 0.0より大きな値を指定すること。
| time.begin | string | "HH:mm"のフォーマットで文字列で指定する。
| time.end | string | "HH:mm"のフォーマットで文字列で指定する。
| time.value_min | number | 

```json
{
	"name": "field_name",
	"bit_size": 4,
	"value": 0,
	"type": "Time",
	"time": {
		"elapse": 10,
		"begin": "10:00",
		"end": "12:00",
		"value_min": 10
	}
}
```

↓

```
{
	10: "10:00",
	11: "10:10",
	12: "10:20",
		...
	21: "11:50",
	22: "12:00"
}
```

---

### fields.Char

テキストボックスで文字を入力する。

| Setting | Format | Description |
----|----|---- 
| name | string | nameの指定が必須。Charでmulti_nameは指定不可。
| bit_size | number | 強制的に8bitフィールドとする。省略可
| type | string | "Char"
| char | string | 任意の一文字。文字列で入力するが、先頭の一文字以外は無視する。

```json
{
	"name": "field_name",
	"type": "Char", "char": "A"
}
```

---

### fields.String

Charのシンタックスシュガー。stringで指定した分だけCharとして展開する。

| Setting | Format | Description |
----|----|---- 
| name | string | nameの指定が必須。Charでmulti_nameは指定不可。
| bit_size | number | 強制的に8bitフィールドとする。省略可
| type | string | "String"
| string | string | 文字列

```json
{
	"name": "str",
	"type": "String", "string": "ABC"
}
```
↓
```json
{ "name": "str[0]", "type": "Char", "char": "A" },
{ "name": "str[1]", "type": "Char", "char": "B" },
{ "name": "str[2]", "type": "Char", "char": "C" }
```

---

### fields.Checksum

| Setting | Format | Description |
----|----|---- 
| type | string | "Checksum"
| checksum | object | 
| checksum.begin | number | チェックサム計算範囲。バイト単位で指定。begin以上end以下の範囲でサムを取る。<br>省略時は0
| checksum.end | number | 省略時は本fieldの手前までを計算範囲とする。checksumフィールドをまたいだ指定は不可。
| checksum.method | string | "2compl" or "1compl" or "Sum" or 省略<br>2compl: 2の補数<br>1compl: 1の補数<br>Sum or 省略時: サムのみ

```json
{
	"name": "Checksum",
	"bit_size": 8,
	"value": 0,
	"type": "Checksum",
	"checksum": {
		"begin": 1,
		"begin": 4,
		"method": "2compl"
	}
}
```

---

### fields.Script

| Setting | Format | Description |
----|----|---- 
| type | string | "Script"
| script | object | 
| script.mode | string | "Exec" or "Call"<br>Exec: Scriptに指定した文字列をスクリプトとして実行する。<br>Call: Scriptに指定した文字列を関数としてコールする。
| script.count | number | Scriptの実行回数を指定する。
| script.script | string | JavaScriptコード。

```json
{
	"name": "field1",
	"bit_size": 8,
	"value": 1,
	"type": "Script",
	"script": {
		"mode": "Exec",
		"count": 10,
		"script": "key = i * 2 + (i%2 == 0 ? 0x80 : 0x00);   value = key + ' h';"
	}
},
{
	"name": "field2",
	"bit_size": 8,
	"value": 0,
	"type": "Script",
	"script": {
		"mode": "Call",
		"count": 10,
		"script": "Setting_Selecter_Script_2"
	}
}
```
↓
#### Exec
count, scriptを下記のようなコードに展開する。
scriptで指定したコードを自動的に関数として作成する。
MakeFieldExecScriptがcount分だけその関数をコールする。
scriptで指定するコードは*key*と*value*を作成する必要がある。
```js
(() => {
	try {
		const exec_func = (i) => {
			let key; let value;
			key = i * 2 + (i%2 == 0 ? 0x80 : 0x00);   value = key + ' h';;
			return {key: key, value: value};
		}
		MakeFieldExecScript(exec_func, 10);
		Settings.Field.Result = true;
	}
	catch (e) {
		Settings.Field.Message = e.message;
		Settings.Field.Result = false;
	}
})()
```

#### Call
count, scriptを下記のようなコードに展開する。
scriptで指定した関数にcountを引数として指定するコードに展開する。
scriptで指定する関数は内部でcountを元にループすることを想定。
JavaScript(WebView2)→C#のI/Fとして*Settings.Field.AddSelecter(key, value);*を用意している。このI/Fで*key*と*value*のペアを指定することで現在作成中のfieldのコンボボックスに表示する入力値として追加できる。
```js
(() => {
	try {
		Setting_Selecter_Script_2(10);
		Settings.Field.Result = true;
	}
	catch (e) {
		Settings.Field.Message = e.message;
		Settings.Field.Result = false;
	}
})()
```

#### MakeFieldExecScript
Settings.jsにて定義している。
```js
const MakeFieldExecScript = (func, count) => {
    for (let i = 0; i < count; i++) {
        const result = func(i);
        Settings.Field.AddSelecter(result.key, result.value);
    }
}
```

---

### rx

受信解析パターンを定義する。

| Setting | Format | Description |
----|----|---- 
| invert_bit | bool | データ送信時にビット反転して送出するかどうかを設定。(省略時:false)
| enable_multi_match | bool | 受信パターンに複数同時にマッチすることを許可するかどうかを設定。(省略時:false)
| frames | array | 送信フレーム定義を配列で入力する。先頭から順に0始まりでIDを割り振る。
| frames.name | string | 受信フレーム名称定義。重複不可。
| frames.fields | array | 受信フレームを構成するフィールドを配列で指定。フィールド設定はtxと共通。
| frames.patterns | array | マッチングパターンを定義する。ここで定義したパターンにマッチしたらauto_txに通知される。

```json
"rx": {
	"invert_bit": false,
	"enable_multi_match": true,
	"frames": [
		{
			"name": "frame_name",
			"fields": [ ... ],
			"patterns": [ ... ]
		},
		{
			...
		}
	]
}
```

---

### rx.frames.patterns

| Setting | Format | Description |
----|----|---- 
| name | string | 受信パターン名称定義。重複不可。
| active | bool | パターンマッチング有効設定初期値。GUIまたはマッチアクション、auto_txアクションから変更可能。<br>(省略時:false)
| log_visualize | bool | false:受信値をHEXテキストで表示<br>true:field設定で定義した表示設定を元にログを作成<br>(省略時:false)
| matches | array | マッチングノードを配列で指定してマッチングパターンとする。fieldの並びと対応付けられる。同じ位置にあるfieldに対してマッチングするルールを設定する。<br>先頭から順に0始まりでIDを割り振る。
| matches.type | string | "Value" or "Any" or "Timeout" or "Script" or "Activate"<br>マッチングノードタイプ。省略時はマッチングノードに記述したプロパティに応じて自動識別。優先度：Script > Activate > Timeout > Value > Any<br>各ノードで使用するプロパティは後述。

```json
"patterns": [
	{
		"name": "Rx_Pattern_1",
		"active": true,
		"log_visualize": true,
		"matches": [
			{ "value": 240 },
			{ "type": "Any" },
			{
				"type": "Timeout",
				"msec": 100
			},
			{
				"type": "Script",
				"rx_begin": "Rx_ptn3_init",
				"rx_recieved": "Rx_ptn3_match_body"
			}
		]
	},
	{
		...
	}
]
```

---

### rx.frames.patterns.Value

| Setting | Format | Description |
----|----|---- 
| matches.type | string | "Value"
| matches.value | number | 対応するfieldの受信値が指定した値のときにマッチとする。<br>valueプロパティのみ指定したときはValueノードとみなす。

```json
"matches": [
	{ "value": 240 },
	{
		"type": "Value",
		"value": 240
	}
]
```

---

### rx.frames.patterns.Any

無条件マッチする。

| Setting | Format | Description |
----|----|---- 
| matches.type | string | "Any"

```json
"matches": [
	{ "type": "Any" },
	{}
]
```

---

### rx.frames.patterns.Script

| Setting | Format | Description |
----|----|---- 
| matches.type | string | "Script"
| matches.rx_begin | string | 受信解析シーケンス開始時に実行する。実行するJavaScript関数名を指定する。カッコは付けない。省略可。
| matches.rx_recieved | string | 受信データ解析時に1バイトにつき1回実行する。引数には該当Scriptノードが定義されているframesのid、patternsのidが渡される。

```json
"matches": [
	{
		"type": "Script",
		"rx_begin": "Rx_ptn3_init",
		"rx_recieved": "Rx_ptn3_match"
	},
	{
		"rx_begin": "Rx_ptn3_init",
		"rx_recieved": "Rx_ptn3_match"
	},
	{
		"rx_recieved": "Rx_ptn3_match"
	}
]
```

以下のように展開して実行される。

```js
Rx_ptn3_init(0,0);
Rx_ptn3_match(0,0);
```

#### rx.frames.patterns.Script.rx_recieved

解析対象データは *Comm.Rx.Data* により取得する。  
判定結果は *Comm.Rx.Result* に設定する。

```js
const Rx_ptn3_match_body = (frame_id, pattern_id) => {
	let result = MatchProgress;

	//
	if (Comm.Rx.Data == 1) {
		Comm.Rx.AddLog(frame_id, pattern_id, "Frame_1");
		result = MatchSuccess;
	} else {
		result = MatchFailed;
	}

	Comm.Rx.Result = result;
}
```

判定結果用の定義は *Comm.js* にて定義している。
Script用インターフェース詳細は[WebView2 / JavaScript 連携詳細](#webview2--javascript-連携詳細)を参照。

---

### rx.frames.patterns.Timeout

| Setting | Format | Description |
----|----|---- 
| matches.type | string | "Timeout"
| matches.number | msec | (msec単位)

```json
"matches": [
	{
		"type": "Timeout",
		"msec": 100
	},
	{
		"msec": 100
	}
]
```

---

### rx.frames.patterns.Activate

| Setting | Format | Description |
----|----|---- 
| matches.type | string | "Activate"
| matches.auto_tx_job | string | auto_tx.jobs.nameで操作対象を指定する。存在しない名前を指定するとエラー。
| matches.rx_pattern | string | rx.patterns.nameで操作対象を指定する。存在しない名前を指定するとエラー。
| matches.state | bool | 有効無効設定値を指定する。(初期値:true)

```json
"matches": [
	{
		"type": "Activate",
		"auto_tx_job": "job_name",
		"state": false
	},
	{
		"auto_tx_job": "job_name"
	},
	{
		"rx_pattern": "rx_pattern_name"
	},
	{
		"rx_pattern": "rx_pattern_name",
		"state": true
	},
]
```


---

### auto_tx

シリアル通信設定から設定できるポーリング周期に基づき、全ジョブに対して1周期に1アクションを実行する。
アクションはGUIスレッドにて実行する。1周期毎にスレッドを解放して時間待機に入る。

| Setting | Format | Description |
----|----|---- 
| jobs | array | 自動操作ジョブ定義を配列で入力する。先頭から順に0始まりでIDを割り振る。
| jobs.name | string | 自動操作ジョブ名称。各種機能設定からの参照に使う。
| jobs.alias | string | GUI上表示名。指定したエイリアスをそのまま表示する。
| jobs.active | bool | 自動操作ジョブ有効無効初期設定。GUI上から操作可能。
| jobs.editable | bool | 変更可能な自動操作アクションをGUIから変更可能にする設定。
| jobs.actions | array | 自動操作アクション定義を配列で入力する。先頭から順に0始まりでIDを割り振る。

```json
"auto_tx": {
	"jobs": [
		{
			"name": "Job_Auto_1",
			"alias": "ジョブ1エイリアス",
			"active": true,
			"editable": true,
			"actions": [ ... ]
		}
	]
}
```

---

### auto_tx.jobs.actions

type毎に固有のプロパティを設定する。

| Setting | Format | Description |
----|----|---- 
| type | string | "Send" or "Wait" or "Recv" or "Jump" or "Script" or "Activate" or "Log"<br>各アクションで使用するプロパティは後述。
| alias | string | GUI上表示名。指定したエイリアスをそのまま表示する。
| immediate | bool | 即時実行有効無効設定。有効のとき、1つ前のアクション完了後にスレッドを解放することなくこのアクションを実行する。Waitアクションの場合はスレッドをロックしてスリープする。

```json
"actions": [
	{ "type": "Log", "log": "<Job_Auto_1 start.>" },
	{ "type": "Send", "tx_frame": "Frame_A" },
	{ "type": "Script", "rx_handler": "Job1_0_RxMatch()" },
	{ "type": "Script", "auto_tx_handler": "Job1_0_Format5()" },
	{ "type": "Wait", "wait_time": 2000 },
	{ "type": "Jump", "alias": "<Loop>", "jump_to": 1 }
]
```

---

### auto_tx.jobs.actions.Send

txで定義した送信フレームを指定して送信する。  
アクション実行時にWaitタイマをリスタートする。

| Setting | Format | Description |
----|----|---- 
| type | string | "Send"
| tx_frame | string | 送信フレーム名称
| tx_frame_buff_index | number | 送信フレームバッファIDを指定する。<br>0:メインバッファ<br>1～:バックアップバッファ<br>(省略時:0)
| tx_frame_buff_offset | number | 送信バッファのうち、何バイト目から送信するかを指定可能。(省略時:0)
| tx_frame_buff_length | number | 送信バッファから何バイト送信するかを指定可能。(省略時:バッファ全体)

```json
"actions": [
	{ "type": "Send", "tx_frame": "Frame_A" },
	{
		"type": "Send",
		"tx_frame": "Frame_A",
		"tx_frame_buff_index": 1,
		"tx_frame_buff_offset": 2,
		"tx_frame_buff_length": 4
	}
]
```

---

### auto_tx.jobs.actions.Wait

前のアクション完了から、指定した時間が経過するまで待機する。  
ポーリングで周期時間でアクションを実行するため、ポーリング時間設定値により誤差が出るため注意。  
immediateを指定した場合、スレッドをロックして時間経過を待機するため誤差が小さくなるが、GUIが応答なしになるため注意。  
時間待機完了して次のアクションに遷移する時にWaitタイマをリスタートする。

| Setting | Format | Description |
----|----|---- 
| type | string | "Wait"
| wait_time | number | (ms単位)

```json
"actions": [
	{ "type": "Wait", "wait_time": 2000 }
]
```

---

### auto_tx.jobs.actions.Recv

指定した受信パターンマッチングの成立を待機する。
パターンマッチング条件成立したときの受信データを受信した時刻でWaitタイマをスタートする。

| Setting | Format | Description |
----|----|---- 
| type | string | "Recv"
| rx_patterns | array | 受信待機対象とする受信パターン名称(rx.frames.patterns.name)を指定する。複数指定可能。空リスト指定でいずれかのパターンマッチング成立となる。

```json
"actions": [
	{ "type": "Recv", "rx_patterns": [ "Rx_Pattern_1" ] },
	{ "type": "Recv", "rx_patterns": [] }
]
```

---

### auto_tx.jobs.actions.AnyRecv

パターンマッチング成立、または、タイムアウトを含む、何かしらのデータ受信を待機する。
パターンマッチング条件成立したときの受信データを受信した時刻でWaitタイマをスタートする。

| Setting | Format | Description |
----|----|---- 
| type | string | "AnyRecv"

```json
"actions": [
	{ "type": "AnyRecv" }
]
```

---

### auto_tx.jobs.actions.Jump

指定したアクションに実行個所を移動する。これによりauto_txの繰り返し実行ができる。  
アクション実行時にWaitタイマをリスタートする。

| Setting | Format | Description |
----|----|---- 
| type | string | "Jump"
| jump_to | number | アクションIDを指定する。
| auto_tx_job | string | auto_tx.jobs.nameで操作対象ジョブを指定できる。存在しない名前を指定するとエラー。<br>省略時は自ジョブを対象とする。

```json
"actions": [
	{ "type": "Jump", "jump_to": 0 },
	{ "type": "Jump", "jump_to": 0, "auto_tx_job": "job_name" }
]
```

---

### auto_tx.jobs.actions.Script

指定したJavaScriptを実行する。auto_tx_handlerとrx_handlerのどちらかあるいは両方を指定する。  
rx_handler選択時、パターンマッチング条件成立したときの受信データを受信した時刻でWaitタイマをスタートする。

| Setting | Format | Description |
----|----|---- 
| type | string | "Script"
| auto_tx_handler | string | 周期処理時に実行する。
| rx_handler | string | 受信パターンマッチング成功時に実行する。

```json
"actions": [
	{ "type": "Script", "rx_handler": "Job1_0_RxMatch()" },
	{ "type": "Script", "auto_tx_handler": "Job1_0_Format5()" }
]
```

Script用インターフェース詳細は[WebView2 / JavaScript 連携詳細](#webview2--javascript-連携詳細)を参照。

---

### auto_tx.jobs.actions.Activate

アクション実行時にWaitタイマをリスタートする。

| Setting | Format | Description |
----|----|---- 
| type | string | "Activate"
| auto_tx_job | string | auto_tx.jobs.nameで操作対象を指定する。存在しない名前を指定するとエラー。
| rx_pattern | string | rx.patterns.nameで操作対象を指定する。存在しない名前を指定するとエラー。
| state | bool | 有効無効設定値を指定する。(初期値:true)

```json
"actions": [
	{
		"type": "Activate",
		"auto_tx_job": "job_name",
		"state": false
	},
	{
		"auto_tx_job": "job_name"
	},
	{
		"rx_pattern": "rx_pattern_name"
	},
	{
		"rx_pattern": "rx_pattern_name",
		"state": true
	},
]
```

---

### auto_tx.jobs.actions.Log

GUI上ログボックスにログを出力する。

| Setting | Format | Description |
----|----|---- 
| type | string | "Log"
| log | string | ログに出力する文字列。

```json
"actions": [
	{ "type": "Log", "log": "<Job_Auto_1 start.>" },
]
```

---

### gui

| Setting | Format | Description |
----|----|---- 
| window | object | ウインドウサイズを指定可能。
| column_order | object | 通信フレーム表示の各列の表示順序を指定可能。
| column_width | object | 通信フレーム表示の各列の表示横幅を指定可能。

---

### column_order, column_width

| Setting | Format | Description |
----|----|---- 
| byte_index | number | Byte番号表示列
| bit_index | number | Bit番号表示列
| group | number | fieldグループ定義表示列
| field_value | number | field設定値(HEX)表示列
| field_name | number | field名称表示列
| field_input | number | field設定値表示列
| tx_bytes | number | 送信データ(1byte区切り)表示列
| spacer | number | 空白スペース列
| tx_buffer | number | 送信データバックアップ領域表示列

* "-1"設定で非表示

```json
"gui": {
	"window": {
		"width": 400,
		"height": 400
	},
	"column_order": {
		"byte_index": 0,
		"bit_index": 1,
		"group": -1,
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
		"group": 25,
		"field_value": 40,
		"field_name": 80,
		"field_input": 80,
		"tx_bytes": 50,
		"spacer": 10,
		"tx_buffer": 80
	}
}
```

---

### log

| Setting | Format | Description |
----|----|---- 
| directory | string | SerialDebugger.exeがあるフォルダを起点にログファイルを格納するパスを指定する。<br>省略可。省略時はログを保存しない。
| max_size | number | GUI上ログ表示を何件まで保持するかを設定する。

	"log": {
		"directory": "path",
		"max_size": 100
	}

---

### output

| Setting | Format | Description |
----|----|---- 
| drag_drop | object | GUI上の通信フィールド名称部分をDrag&Drop可能。フィールド名称、設定値をテキストでエクスポートする。本設定で対象情報の前後にテキストを挿入可能。html形式でのDropデータ作成を想定している。

---

### drag_drop

| Setting | Format | Description |
----|----|---- 
| body | object | Dropデータ全体を囲う設定。省略時は何も囲わない。
| item | object | frame名称とfield名称、field設定値のセットを囲う設定。省略時は何も囲わない。
| frame_name | object | field名称を囲う設定。省略時はframe名称をDropデータから除外する。
| field_name | object | field名称を囲う設定。省略時はfield名称とfield設定値のセットをDropデータから除外する。
| field_value | object | field設定値を囲う設定。省略時はfield名称とfield設定値のセットをDropデータから除外する。
| inner_field_name | object | inner_field名称を囲う設定。fields.multi_nameで指定したfield_nameをすべてDropデータに載せる。省略時はfield名称とfield設定値のセットをDropデータから除外する。
| inner_field_value | object | inner_field設定値を囲う設定。fields.multi_nameで指定したfield_nameに該当するfield_valueをすべてDropデータに載せる。省略時はfield名称とfield設定値のセットをDropデータから除外する。
| value_format | string | "Input" or 省略<br>"Input"指定時はfieldで定義した入力指定を元にfield設定値をテキスト化する。省略時は16進数文字列となる。

```json
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
		"inner_field_name": {
			"begin":    "string",
			"end":      "string"
				
		},
		"inner_field_value": {
			"begin":    "string",
			"end":      "string"
				
		},
		"value_format": "Input"
	}
}
```

---

## WebView2 / JavaScript 連携詳細

C#からのWebView2連携ライブラリを使いindex.htmlを読み込む。
index.html上でJavaScriptを実行する。
SerialDebugger標準として *Script/Utility.js* , *Script/Settings.js* , *Script/Comm.js* を提供している。
これらはindex.htmlからロードしている。また、これらファイルの初期化関数をindex.html内の *csLoaded()* からコールしている。
*csLoaded()* はC#でWebView2の初期化が完了したらときにC#側からコールされる。

### Script/Comm.js

初期化処理で下記インターフェースを設定している。

```js
var Comm;
var CommAsync;
var MatchProgress;
var MatchFailed;
var MatchSuccess;

const Comm_Loaded = () => {
	Comm = chrome.webview.hostObjects.sync.Comm;
    CommAsync = chrome.webview.hostObjects.Comm;

    MatchProgress = Comm.Rx.MatchProgress;
    MatchFailed = Comm.Rx.MatchFailed;
    MatchSuccess = Comm.Rx.MatchSuccess;
}
```

#### Script/Comm.js Interface

| Interface | Format | Description |
----|----|---- 
| Comm.Tx | object | 
| Comm.AutoTx | object | 
| Comm.Rx | object | 
| Comm.RxMatch | object | 

##### Comm.Rx

| Interface | Format | Description |
----|----|---- 
| Comm.Rx.Init() | void | 
| Comm.Rx.AddLog(int frame_id, int pattern_id, string log) | void | 
| Comm.Rx[] | RxFrame | 受信解析(Rx)で定義したFrameへアクセスするI/F

##### Comm.Rx.RxFrame

| Interface | Format | Description |
----|----|---- 
| RxFrame.Fields[] | Field | FieldへアクセスするI/F

##### Comm.Rx.RxFrame.Field

| Interface | Format | Description |
----|----|---- 
| Field.GetDict() | Dict | Fieldで定義される選択リストを取得する

##### Comm.Rx.RxFrame.Field.Dict

| Interface | Format | Description |
----|----|---- 
| Dict.Count | number | 選択リスト要素数
| Dict[] | Node | 選択リスト要素

##### Comm.Rx.RxFrame.Field.Dict.Node

| Interface | Format | Description |
----|----|---- 
| Node.Value | number | 値
| Node.GetValue() | string | 表示文字列


##### Comm.RxMatch

| Interface | Format | Description |
----|----|---- 
| Comm.RxMatch.IsTimeout | bool | 受信解析タイムアウトの発生有無フラグ
| Comm.RxMatch.HasAnyRecv | bool | パターンマッチング成功、タイムアウトいずれかの何かしらのデータ受信有無フラグ
| Comm.RxMatch.Clear() | void | マッチ結果をクリアする
| Comm.RxMatch[] | MatchResult | RxMatchはMatchResultの配列になっている。indexerでアクセスしてMatchResultを取得するI/F

##### Comm.RxMatch.MatchResult

| Interface | Format | Description |
----|----|---- 
| MatchResult.FrameId | number | 
| MatchResult.PatternId | number | 
| MatchResult[] | RxMatch | 


### Script/Utility.js

C# <-> WebView2連携用のユーティリティを提供している。

- Utility

| I/F | Type | Detail | Description |
----|----|----|---- 
| Log | Function | void Log(string msg) | GUI上のログブロックにログを出力する。


