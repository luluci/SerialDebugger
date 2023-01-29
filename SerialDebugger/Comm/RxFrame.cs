using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reactive.Disposables;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace SerialDebugger.Comm
{
    using Utility;


    class RxFrame : BindableBase, IDisposable
    {
        public int Id { get; }
        // 
        public string Name { get; }
        /// <summary>
        /// 送信フレーム: Field集合体
        /// </summary>
        public ReactiveCollection<Field> Fields { get; set; }
        /// <summary>
        /// 送信フレームバイト長
        /// </summary>
        public int Length { get; set; } = 0;
        public int BitLength { get; set; } = 0;

        // 表示用バイト長
        public int DispMaxLength { get; set; } = 0;

        public ReactiveCollection<RxPattern> Patterns { get; set; }

        public RxFrame(int id, string name)
        {
            // 基本情報
            Id = id;
            Name = name;

            //
            Fields = new ReactiveCollection<Field>();
            Fields.AddTo(Disposables);
            //
            Patterns = new ReactiveCollection<RxPattern>();
            Patterns.AddTo(Disposables);
        }

        public void Add(Field field)
        {
            Fields.Add(field);
        }

        /// <summary>
        /// 登録したFieldからFrame情報を構築する。
        /// Build()は一度のみ行い、内部情報をクリアしてのインスタンス再利用はしない。
        /// *特にブロックはかけていないので運用でカバー
        /// </summary>
        public void Build()
        {
            // Fieldから情報収集
            int field_no = 0;
            int bit_pos = 0;
            int byte_pos = 0;
            int disp_len = 0;
            foreach (var f in Fields)
            {
                f.Value.Value = 0;
                // Field位置セット
                f.BitPos = bit_pos;
                f.BytePos = byte_pos;
                f.ByteSize = (bit_pos + f.BitSize + 7) / 8;
                // Frame情報更新
                BitLength += f.BitSize;
                // Field位置更新
                bit_pos += f.BitSize;
                while (bit_pos >= 8)
                {
                    //
                    byte_pos++;
                    bit_pos -= 8;
                }
                //
                if ((f.BitSize % 8) == 0 && f.BitPos == 0)
                {
                    f.IsByteDisp = true;
                }
                // 表示データ数
                disp_len += f.InnerFields.Count;
                //
                field_no++;
            }
            // 定義がバイト単位でなくビットに残りがあった場合
            if (bit_pos > 0)
            {
                //
                byte_pos++;
                bit_pos -= 8;
            }
            // バイトサイズ計算
            Length = (BitLength / 8);
            if ((BitLength % 8) != 0)
            {
                Length++;
            }
            //
            DispMaxLength = Length;
        }

        public void BuildPattern()
        {
            // パターン定義解析
            for (int ptn_idx=0; ptn_idx<Patterns.Count; ptn_idx++)
            {
                var pattern = Patterns[ptn_idx];

                //
                int byte_pos = 0;
                int bit_pos = 0;
                UInt64 value = 0;
                UInt64 mask = 0;
                int field_idx = 0;
                int match_idx = 0;
                while (match_idx < pattern.Matches.Count)
                {
                    // Field/Matchインスタンス取得
                    Field field = null;
                    if (field_idx < Fields.Count)
                    {
                        field = Fields[field_idx];
                    }
                    var match = pattern.Matches[match_idx];
                    match.FieldRef = field;
                    
                    // Matchノードチェック
                    switch (match.Type)
                    {
                        case RxMatchType.Value:
                            //
                            if (field is null)
                            {
                                throw new Exception($"RxFrame({Name})/RxPattern({pattern.Name})/RxMatch[{match_idx}]: Matchルールの指定がField定義数の範囲外");
                            }
                            // Value設定値チェック
                            match.Value = match.Value & field.Mask;
                            //
                            value |= (match.Value << bit_pos);
                            mask |= (field.Mask << bit_pos);
                            bit_pos += field.BitSize;
                            break;

                        case RxMatchType.Any:
                            //
                            if (field is null)
                            {
                                throw new Exception($"RxFrame({Name})/RxPattern({pattern.Name})/RxMatch[{match_idx}]: Matchルールの指定がField定義数の範囲外");
                            }
                            //
                            bit_pos += field.BitSize;
                            break;

                        case RxMatchType.Timeout:
                            {
                                // 1frameの途中で時間待ちをできるように1バイト境界に配置を前提とする。
                                if (bit_pos > 0)
                                {
                                    throw new Exception($"RxFrame({Name})/RxPattern({pattern.Name})/RxMatch[{match_idx}]: Timeout,Scriptはバイト境界に配置してください。");
                                }
                                // Rule追加
                                var rule = new RxAnalyzeRule(match.Msec);
                                pattern.Analyzer.Rules.Add(rule);
                            }
                            break;

                        case RxMatchType.Script:
                            {
                                // 1frameの途中で時間待ちをできるように1バイト境界に配置を前提とする。
                                if (bit_pos > 0)
                                {
                                    throw new Exception($"RxFrame({Name})/RxPattern({pattern.Name})/RxMatch[{match_idx}]: Timeout,Scriptはバイト境界に配置してください。");
                                }
                                // Rule追加
                                var rule = new RxAnalyzeRule(match.Script);
                                pattern.Analyzer.Rules.Add(rule);
                            }
                            break;

                        default:
                            // 何もしない
                            break;
                    }
                    // ルールはバイト単位で区切る
                    // 1バイト以上の情報はRule化する
                    while (bit_pos >= 8)
                    {
                        // Rule作成
                        var m_val = (byte)(value & 0x00FF);
                        var m_mask = (byte)(mask & 0x00FF);
                        var rule = new RxAnalyzeRule(m_val, m_mask);
                        // Rule追加
                        pattern.Analyzer.Rules.Add(rule);
                        // 使用した分を削除
                        value >>= 8;
                        mask >>= 8;
                        bit_pos -= 8;
                        byte_pos++;
                    }
                    // インデックス更新
                    switch (match.Type)
                    {
                        case RxMatchType.Value:
                        case RxMatchType.Any:
                            // Value,Anyのとき該当するFieldを消化して次のFieldに移行
                            field_idx++;
                            match_idx++;
                            break;

                        case RxMatchType.Timeout:
                        case RxMatchType.Script:
                        default:
                            match_idx++;
                            break;
                    }
                }
                // ビット情報が残っていたら消費する
                if (bit_pos > 0)
                {
                    // Rule作成
                    var m_val = (byte)(value & 0x00FF);
                    var m_mask = (byte)(mask & 0x00FF);
                    var rule = new RxAnalyzeRule(m_val, m_mask);
                    // Rule追加
                    pattern.Analyzer.Rules.Add(rule);
                    // 使用した分を削除
                    value = 0;
                    mask = 0;
                    bit_pos = 0;
                }

                if (DispMaxLength < pattern.Analyzer.Rules.Count)
                {
                    DispMaxLength = pattern.Analyzer.Rules.Count;
                }
            }

        }

        #region IDisposable Support
        private CompositeDisposable Disposables { get; } = new CompositeDisposable();

        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージド状態を破棄します (マネージド オブジェクト)。
                    this.Disposables.Dispose();
                }

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // TODO: 大きなフィールドを null に設定します。

                disposedValue = true;
            }
        }

        // TODO: 上の Dispose(bool disposing) にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~TxFrame() {
        //   // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
        //   Dispose(false);
        // }

        // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        void IDisposable.Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(true);
            // TODO: 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
