using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.TRSNetwork;
using UI.XRay.Flows.TRSNetwork.Models;
using XRayNetEntities.Models;
using XRayNetEntities.Tools;
using UI.XRay.Flows.HttpServices;
namespace UI.XRay.Flows.Controllers
{
    /// <summary>
    /// 图像文件存储路径Helper
    /// </summary>
    public static class ImageFileStorePathHelper
    {
        /// <summary>
        /// 图像文件存储根路径
        /// </summary>
        private static string _storeRoot;

        public static string SysPath = @"D:\SecurityScanner\NetService\Config\sys.xml";/*ConfigurationManager.AppSettings["AppPath"].ToString()*/

        static ImageFileStorePathHelper()
        {
            try
            {
                if (!ScannerConfig.Read(ConfigPath.SystemImageStorePath, out _storeRoot))
                {
                    _storeRoot = "D:\\SecurityScanner\\Images";
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        /// <summary>
        /// 根据当前时间，生成一个唯一的文件名称
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="machineNumber"></param>
        /// <param name="scanningDateTime"></param>
        /// <returns></returns>
        public static string GenerateFileName(string machineNumber, string accountId, DateTime scanningDateTime)
        {
            StringBuilder fileNameBuilder = new StringBuilder(250);  

            if (!string.IsNullOrEmpty(Global.Instance.Sys.CounterNo))
            {
                fileNameBuilder.Append(Global.Instance.Sys.CounterNo);
            }
            else if (!string.IsNullOrEmpty(Global.Instance.Sys.ChannelID))
            {
                fileNameBuilder.Append(Global.Instance.Sys.ChannelID);
            }

            fileNameBuilder.Append("_");
            if (!string.IsNullOrWhiteSpace(machineNumber))
            {
                fileNameBuilder.Append(machineNumber);
            }
            fileNameBuilder.Append("_");

            if (!string.IsNullOrWhiteSpace(accountId))
            {
                fileNameBuilder.Append(accountId);
            }
            string secret;
            fileNameBuilder.Append(scanningDateTime.ToString("_yyMMddHHmmss_"));
            secret = Guid.NewGuid().ToString("N");
            fileNameBuilder.Append(secret);
            fileNameBuilder.Append(".xray");
            HttpAiJudgeServices.deviceInfo.PackageID = secret;
            return fileNameBuilder.ToString();
        }
        public static T Load<T>(string path) where T : class
        {
            if (!File.Exists(path))
                throw new FileNotFoundException(path);
            using (FileStream sw = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                return XmlUtil.Deserialize<T>(sw);
            }
        }
        /// <summary>
        /// 生成图像文件的存储路径
        /// </summary>
        /// <param name="scanTime"></param>
        /// <returns></returns>
        public static string GenerateFilePath(DateTime scanTime)
        {
            return Path.Combine(_storeRoot, scanTime.ToString("yyyy"), scanTime.ToString("MM"), scanTime.ToString("dd"));
        }
    }
}
