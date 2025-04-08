using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace UI.XRay.Flows.Services
{
    public class ReadWriteXml
    {
        public static T ReadXmlFromFile<T>(string file, Encoding encoding = null)
        {
            if (!File.Exists(file)) return default(T);
            if (encoding == null) encoding = Encoding.Default;
            T t;
            try
            {
                using (FileStream fsRead = new FileStream(file, FileMode.Open))
                {
                    int fsLen = (int)fsRead.Length;
                    byte[] buffer = new byte[fsLen];
                    int result = fsRead.Read(buffer, 0, buffer.Length);
                    string xml = encoding.GetString(buffer);
                    //反序列化
                    using (StringReader sr = new StringReader(xml))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(T));
                        t = (T)serializer.Deserialize(sr);
                    }
                }
            }
            catch (Exception e)
            {
                return default(T);
            }
            return t;
        }

        public static void WriteXmlToFile<T>(T data, string file, Encoding encoding = null)
        {
            if (File.Exists(file)) File.Delete(file);
            if (encoding == null) encoding = Encoding.Default;
            using (StringWriter sw = new StringWriter())
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                serializer.Serialize(sw, data);
                using (FileStream fs = new FileStream(file, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    byte[] buffer = encoding.GetBytes(sw.ToString());  //Default
                    fs.Write(buffer, 0, buffer.Length);
                }
            }
        }
    }
}
