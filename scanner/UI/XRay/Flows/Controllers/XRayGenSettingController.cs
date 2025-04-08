using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Control;
using UI.XRay.Flows.Services;

namespace UI.XRay.Flows.Controllers
{
    /// <summary>
    /// 射线源配置控制器，用于射线源配置视图模型中
    /// </summary>
    public class XRayGenSettingController
    {
        public XRayGenSettingController()
        {
        }

        public Task<XRayGeneratorSettings> GetXRayGenSettingsAsync()
        {
            return Task.Run(() =>
            {
                XRayGeneratorSettings settings;
                ControlService.ServicePart.GetXRayGenSettings(out settings);

                return settings;
            });
        }

        public Task<XRayGeneratorWorkingStates> GetXRayGenWorkingStatesAsync()
        {
            return Task.Run(() =>
            {
                XRayGeneratorWorkingStates states;
                ControlService.ServicePart.GetXRayGenWorkingStates(out states);

                return states;
            });
        }

        /// <summary>
        /// 更改射线源的配置，并存储到flash中
        /// </summary>
        /// <param name="kv"></param>
        /// <param name="ma"></param>
        /// <returns></returns>
        public bool ChangeXRayGenSettings(double kv, double ma, XRayGeneratorIndex index, bool isSave = false)
        {

            if (isSave)
            {
                // 存储至配置中
                SaveIntoConfig(kv, ma, index);
            }

            XRayGeneratorSettings settings;
            if (ControlService.ServicePart.GetXRayGenSettings(out settings))
            {
                settings.KV = (float)kv;
                settings.MA = (float)ma;
                if (ControlService.ServicePart.ChangeXRayGenSettings(settings, index))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 将射线源的高压和束流存储到配置数据库中
        /// </summary>
        /// <param name="kv"></param>
        /// <param name="ma"></param>
        private void SaveIntoConfig(double kv, double ma, XRayGeneratorIndex index)
        {
            if (index == XRayGeneratorIndex.XRayGenerator1)
            {
                ScannerConfig.Write(ConfigPath.XRayGenKV, kv);
                ScannerConfig.Write(ConfigPath.XRayGenMA, ma);
                ConfigHelper.XRayGen1Current = (float)ma;
                ConfigHelper.XRayGen1Voltage = (float)kv;
            }
            else if(index == XRayGeneratorIndex.XRayGenerator2)
            {
                ScannerConfig.Write(ConfigPath.XRayGenKV2, kv);
                ScannerConfig.Write(ConfigPath.XRayGenMA2, ma);
                ConfigHelper.XRayGen2Current = (float)ma;
                ConfigHelper.XRayGen2Voltage = (float)kv;
            }
        }

        public bool EmitXRay(bool on)
        {
            try
            {
                return (ControlService.ServicePart.RadiateXRay(on));
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            return false;
        }
    }
}
