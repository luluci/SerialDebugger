using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SerialDebugger.Script
{
    using Logger = Log.Log;

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class IoIf
    {
        public List<FileIf> FileStack;

        public IoIf()
        {
            FileStack = new List<FileIf>();
        }

        public void Reset()
        {
            // 
            Dispose();
        }

        public FileIf GetFile(string path, string enc = "utf8")
        {
            var id = FileStack.Count;
            if (path.Length == 0)
            {
                path = Logger.GetLogFilePath();
            }
            var file = new FileIf(id, path, enc);
            FileStack.Add(file);
            return file;
        }

        public FileIf GetFileAutoName(string dir, string file, string ext = ".txt", string enc = "utf8")
        {
            var path = Logger.MakeAutoNamePath(dir, file, ext);
            return GetFile(path, enc);
        }


        public void Dispose()
        {
            foreach (var elem in FileStack)
            {
                elem.Dispose();
            }
            FileStack.Clear();
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class FileIf
    {
        public int Id;
        public bool IsOpen;
        public System.IO.StreamWriter Writer;

        private static Encoding encoding_utf8_bom = Encoding.UTF8;
        private static Encoding encoding_utf8 = new System.Text.UTF8Encoding(false);
        private static Encoding encoding_sjis = System.Text.Encoding.GetEncoding("shift_jis");

        public FileIf(int id, string path, string enc)
        {
            Id = id;
            if (!Object.ReferenceEquals(path, string.Empty))
            {
                IsOpen = true;
                Writer = MakeWriteStream(path, enc);
                if (Writer is null)
                {
                    IsOpen = false;
                }
            }
            else
            {
                IsOpen = false;
                Writer = null;
            }
        }

        public void Write(string str)
        {
            //Logger.Add(str);
            if (IsOpen)
            {
                Writer.WriteLine(str);
            }
        }

        static public System.IO.StreamWriter MakeWriteStream(string path, string enc)
        {
            try
            {
                // ファイルパスからディレクトリを分離
                // ディレクトリが存在しなかったら作成する。
                var dir = System.IO.Path.GetDirectoryName(path);
                if (!System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }
                // Encoding指定
                var encoding = encoding_utf8;
                switch (enc)
                {
                    case "sjis":
                    case "SJIS":
                    case "ShiftJIS":
                    case "Shift_JIS":
                        encoding = encoding_sjis;
                        break;

                    default:
                        // UTF-8
                        break;
                }
                //
                return new System.IO.StreamWriter(path, false, encoding);
            }
            catch (Exception ex)
            {
                Logger.Add(ex.Message);
                return null;
            }
        }

        public void Dispose()
        {
            if (IsOpen)
            {
                IsOpen = false;
                Writer.Close();
                Writer = null;
            }
        }

    }
}
