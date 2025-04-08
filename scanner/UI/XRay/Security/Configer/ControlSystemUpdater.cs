using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Control;
using UI.XRay.Flows.Services;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Configer
{
    /// <summary>
    /// 控制系统硬件更新接口
    /// </summary>
    public static class ControlSystemUpdater
    {
        /// <summary>
        /// 更新控制系统中的射线源参数
        /// </summary>
        /// <returns></returns>
        public static bool Update()
        {
            try
            {
                XRayGeneratorType xrayGenType;
                if (!ScannerConfig.Read(ConfigPath.XRayGenType, out xrayGenType))
                {
                    return false;
                }

                float kV;
                if (!ScannerConfig.Read(ConfigPath.XRayGenKV, out kV))
                {
                    return false;
                }

                float mA;
                if (!ScannerConfig.Read(ConfigPath.XRayGenMA, out mA))
                {
                    return false;
                }

                float kV2;
                if (!ScannerConfig.Read(ConfigPath.XRayGenKV2, out kV2))
                {
                    return false;
                }

                float mA2;
                if (!ScannerConfig.Read(ConfigPath.XRayGenMA2, out mA2))
                {
                    return false;
                }

                int genCount;
                if (!ScannerConfig.Read(ConfigPath.XRayGenCount, out genCount))
                {
                    genCount = 1;
                }

                if (ControlService.ServicePart.Open())
                {
                    
                    if (genCount == 1)
                    {
                        Tracer.TraceInfo("XRayGen Infomation:" + xrayGenType.ToString() + " " + kV.ToString() + " " + mA.ToString());
                        return ControlService.ServicePart.ChangeXRayGenSettings(new XRayGeneratorSettings(xrayGenType, kV, mA),XRayGeneratorIndex.XRayGenerator1);
                    }
                    else
                    {
                        Tracer.TraceInfo("XRayGen1 Infomation:" + xrayGenType.ToString() + " " + kV.ToString() + " " + mA.ToString());
                        Tracer.TraceInfo("XRayGen2 Infomation:" + xrayGenType.ToString() + " " + kV2.ToString() + " " + mA2.ToString());
                        return ControlService.ServicePart.ChangeXRayGenSettings(new XRayGeneratorSettings(xrayGenType, kV, mA), XRayGeneratorIndex.XRayGenerator1) &&
                            ControlService.ServicePart.ChangeXRayGenSettings(new XRayGeneratorSettings(xrayGenType, kV2, mA2), XRayGeneratorIndex.XRayGenerator2);
                    }
                    
                }
                else
                {
                    Tracer.TraceError("Open ControlBoard Failed When Setting XRayGen Setting.");
                }

                return false;
            }
            finally
            {
                ControlService.ServicePart.Close();
            }
        }

        /// <summary>
        /// 测试控制板连接是否正常
        /// </summary>
        /// <returns></returns>
        public static bool TestConnection(ref CtrlSysVersion firmware, ref CtrlSysVersion protocol)
        {
            try
            {
                if(ControlService.ServicePart.Open())
                {
                    ControlService.ServicePart.GetSystemDesc(ref firmware,ref protocol);
                    return true;
                }
                else
                {
                    return false;
                }
                
            }
            finally
            {
                ControlService.ServicePart.Close();
            }
        }

        public static bool SetCmdInterval(ushort interval)
        {
            try
            {
                if (ControlService.ServicePart.Open())
                {
                    var rst = ControlService.ServicePart.SetXRayCmdInterval(interval);
                    if (rst)
                    {
                        MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer","Success to set xray command interval."));
                        return true;
                    }
                    else
                    {
                        MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer","Failed to set xray command interval."));
                        return false;
                    }

                }
                else
                {
                    MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer","Failed to open control board."));
                    return false;
                }

            }
            finally
            {
                ControlService.ServicePart.Close();
            }
        }

        public static bool QueryTotalBagCount(out int count)
        {
            count = 0;
            try
            {
                if (ControlService.ServicePart.Open())
                {
                    var rst = ControlService.ServicePart.GetTotalBagCount(ref count);
                    return rst;
                }
                else
                {
                    MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer", "Failed to open control board."));
                    return false;
                }
            }
            finally { ControlService.ServicePart.Close(); }
        }

        public static bool SetBagCount(ushort count)
        {
            try
            {
                if (ControlService.ServicePart.Open())
                {
                    var rst = ControlService.ServicePart.SetBagCount(0, count);
                    if (rst)
                    {
                        MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer","Success to set xray command interval."));
                        return true;
                    }
                    else
                    {
                        MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer","Failed to set xray command interval."));
                        return false;
                    }

                }
                else
                {
                    MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer","Failed to open control board."));
                    return false;
                }

            }
            finally
            {
                ControlService.ServicePart.Close();
            }
        }

        public static bool ReloadBagCount(uint count)
        {
            try
            {
                if (ControlService.ServicePart.Open())
                {
                    var rst = ControlService.ServicePart.ReloadTotalBagCount(count);
                    return rst;
                }
                else
                {
                    MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer", "Failed to open control board."));
                    return false;
                }
            }
            finally { ControlService.ServicePart.Close(); }
        }

        public static bool SetWorkTiming(bool isEnable)
        {
            try
            {
                if (ControlService.ServicePart.Open())
                {
                    var rst = ControlService.ServicePart.SetWorkTiming(isEnable);
                    return rst;
                }
                else
                {
                    MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer", "Failed to open control board."), "", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            finally { ControlService.ServicePart.Close(); }
        }
    }
}
