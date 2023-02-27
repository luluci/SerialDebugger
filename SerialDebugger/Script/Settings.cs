using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SerialDebugger.Script
{

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class SettingsIf
    {
        public bool ScriptLoaded { get; set; } = false;

        public FieldIf Field { get; set; } = new FieldIf();

        public void Init(Comm.Field field)
        {
            Field.Field(field);
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class FieldIf
    {
        public Comm.Field FieldRef { get; set; }

        public int SelectIndex { get; set; }
        // 実行結果
        public bool Result { get; set; }
        public string Message { get; set; }

        public void AddSelecter(Int64 key, string value)
        {
            if (key <= FieldRef.Max)
            {
                // DropDown上の何番目の要素か
                var SelectsIndex = FieldRef.Selects.Count;
                // Selects登録
                FieldRef.Selects.Add(new Comm.Field.Select(key, value));
                // 初期値と一致する場合はDropDown上の表示位置とする
                if (FieldRef.InitValue == key)
                {
                    SelectIndex = SelectsIndex;
                }
                // 逆引き用辞書更新
                FieldRef.SelectsValueCheckTable.Add(key, SelectsIndex);
            }
        }

        public FieldIf Field(Comm.Field field)
        {
            FieldRef = field;
            SelectIndex = -1;
            return this;
        }
    }

}
