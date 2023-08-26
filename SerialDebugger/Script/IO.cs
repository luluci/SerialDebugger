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

        public FileIf GetFile(string path)
        {
            var id = FileStack.Count;
            if (path.Length == 0)
            {
                path = Logger.GetLogFilePath();
            }
            var file = new FileIf(id, path);
            FileStack.Add(file);
            return file;
        }

        public FileIf GetFileAutoName(string dir, string file)
        {
            var path = Logger.MakeAutoNamePath(dir, file);
            return GetFile(path);
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

        public FileIf(int id, string path)
        {
            Id = id;
            if (!Object.ReferenceEquals(path, string.Empty))
            {
                IsOpen = true;
                Writer = MakeWriteStream(path);
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

        static public System.IO.StreamWriter MakeWriteStream(string path)
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
                return new System.IO.StreamWriter(path);
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
