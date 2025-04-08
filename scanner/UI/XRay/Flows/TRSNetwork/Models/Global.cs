using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XRayNetEntities.Tools;
using XRayNetEntities.Models;
using System.IO;
using System.Reflection;

namespace UI.XRay.Flows.TRSNetwork.Models
{
    public class Global
    {
        private static Global instance;


        public Global()
        {

        }

        #region Properties
        public App App { get; private set; }

        public Sys Sys { get; private set; }
        public static Global Instance
        {
            get
            {
                if (instance == null)
                    instance = new Global();
                return instance;
            }
        }

        public string SysPath = @"D:\SecurityScanner\NetService\Config\sys.xml";/*ConfigurationManager.AppSettings["AppPath"].ToString()*/

        public string AppPath = @"D:\SecurityScanner\NetService\Config\app.xml";/*ConfigurationManager.AppSettings["SysPath"].ToString()*/

        #endregion

        private string curBagID;

        public string CurBagID
        {
            get { return curBagID; }
            set { curBagID = value; }
        }


        /// <summary>
        /// save config to xml files.
        /// </summary>
        /// <param name="cfgName">App or Sys</param>
        /// <param name="path"></param>
        public void Save(string cfgName)
        {
            switch (cfgName.ToLower())
            {
                case "app":
                    Save(App, AppPath);
                    break;
                case "sys":
                    Save(Sys, SysPath);
                    break;
                default:
                    break;
            }
        }

        public void Load(string cfgName)
        {
            switch (cfgName.ToLower())
            {
                case "app":
                    try
                    {
                        App = Load<App>(AppPath);
                    }
                    catch (Exception ex)
                    {
                        App = new App();
                        Save(cfgName);
                        LogUtil.Exception(ex);
                    }
                    break;
                case "sys":
                    try
                    {
                        Sys = Load<Sys>(SysPath);
                    }
                    catch (Exception ex)
                    {
                        Sys = new Sys();
                        Save(cfgName);
                        LogUtil.Exception(ex);
                    }
                    break;
                default:
                    break;
            }
        }
        public void Save<T>(T cfg, string path) where T : class
        {
            try
            {
                if (Directory.Exists(Path.GetDirectoryName(path)) == false)
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (FileStream sw = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    var data = Encoding.UTF8.GetBytes(XmlUtil.Serialize(cfg));
                    sw.Write(data, 0, data.Length);
                }
            }
            catch (Exception ex)
            {
                LogUtil.Exception(ex);
            }
        }

        public T Load<T>(string path) where T : class
        {
            if (!File.Exists(path))
                throw new FileNotFoundException(path);
            using (FileStream sw = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                return XmlUtil.Deserialize<T>(sw);
            }
        }
    }
}
