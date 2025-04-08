using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Common.Utilities
{
    public class ReadFileToBytes
    {
        public static byte[] ReadDataFromFile(string path)
        {
            if (!File.Exists(path))
                return null;
            byte[] data;
            using (FileStream stream = File.Open(path, FileMode.Open))
            {
                data = new Byte[stream.Length];
                stream.Read(data, 0, data.Length);
            }
            return data;
        }
    }
}
