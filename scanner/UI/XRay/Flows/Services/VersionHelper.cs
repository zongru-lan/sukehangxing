using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Control;

namespace UI.XRay.Flows.Services
{
    /// <summary>
    /// 获得算法子系统版本
    /// </summary>
    public class VersionHelper
    {
        public static VersionHelper Service { get; private set; }

        static VersionHelper()
        {
            Service = new VersionHelper();
        }
        public VersionHelper()
        {

        }

        /// <summary>
        /// 获取版本号
        /// </summary>
        /// <returns></returns>
        public string GetVersionStr(SubSystemComponents sub)
        {
            switch (sub)
            {
                case SubSystemComponents.Software:
                    string softVersion;
                    if (!ScannerConfig.Read(ConfigPath.SystemSoftwareVersion, out softVersion))
                    {
                        // 如果注册表中没有版本号，则计算版本号
                        softVersion = "1.5.8";
                    }

                    return softVersion;
                case SubSystemComponents.Algorithm:
                    string algoVersion;
                    if (!ScannerConfig.Read(ConfigPath.SystemAlgorithmVersion, out algoVersion))
                    {
                        // 如果注册表中没有版本号，则计算版本号
                        algoVersion = "1.4.1";
                    }

                    return algoVersion;
                case SubSystemComponents.ControlSys:
                    CtrlSysVersion firmware = new CtrlSysVersion(4, 1);
                    CtrlSysVersion protocol = new CtrlSysVersion(0, 0);
                    if (ControlService.ServicePart.IsOpened)
                    {
                        ControlService.ServicePart.GetSystemDesc(ref firmware, ref protocol);
                        return firmware.ToString();
                    }
                    else
                    {
                        return "UnKnown";
                    }
                default:
                    break;
            }
            return string.Empty;
        }
    }

    public enum SubSystemComponents
    {
        Software = 0,
        Algorithm = 1,
        ControlSys = 2
    }
}
