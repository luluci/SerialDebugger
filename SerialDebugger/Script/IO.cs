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

        public FileIf GetFile()
        {
            var id = FileStack.Count;
            var file = new FileIf(id);
            FileStack.Add(file);
            return file;
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

        public FileIf(int id)
        {
            Id = id;
            IsOpen = true;
        }

        public void Dispose()
        {
            if (IsOpen)
            {
                IsOpen = false;
            }
        }

        public void Write(string str)
        {
            Logger.Add(str);
        }
    }
}
