using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Control;
using UI.XRay.Parts.Keyboard;

namespace UI.XRay.Flows.Services
{
    /// <summary>
    /// 违禁品报警服务，单实例
    /// </summary>
    public class ContrabandAlarmService
    {
        public static ContrabandAlarmService Service { get; private set; }

        static ContrabandAlarmService()
        {
            Service = new ContrabandAlarmService();
        }

        /// <summary>
        /// 当用户取消Auto功能时，不输出报警信号
        /// </summary>
        public bool IsAlarmEnabled { get; set; }

        private bool _hdiAudibleAlarm;

        private bool _hdiLightAlarm;

        private bool _deiAudibleAlarm;

        private bool _deiLightAlarm;

        private bool _eiAudibleAlarm;

        private bool _eiLightAlarm;

        private int _alarmLight ;

        protected ContrabandAlarmService()
        {
            LoadSettings();
            IsAlarmEnabled = true;
            ScannerConfig.ConfigChanged += ScannerConfigOnConfigChanged;
        }

        private void ScannerConfigOnConfigChanged(object sender, EventArgs eventArgs)
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            ScannerConfig.Read(ConfigPath.IntellisenseHdiAudibleAlarm, out _hdiAudibleAlarm);
            ScannerConfig.Read(ConfigPath.IntellisenseHdiLightAlarm, out _hdiLightAlarm);
            ScannerConfig.Read(ConfigPath.IntellisenseDeiAudibleAlarm, out _deiAudibleAlarm);
            ScannerConfig.Read(ConfigPath.IntellisenseDeiLightAlarm, out _deiLightAlarm);
            ScannerConfig.Read(ConfigPath.IntellisenseEiAudibleAlarm, out _eiAudibleAlarm);
            ScannerConfig.Read(ConfigPath.IntellisenseEiLightAlarm, out _eiLightAlarm);

            if(!ScannerConfig.Read(ConfigPath.IntellisenseAlarmLight, out _alarmLight))
            {
                _alarmLight = 1;
            }
        }

        /// <summary>
        /// 高密度报警输出
        /// </summary>
        public void HdiAlert()
        {
            if (IsAlarmEnabled)
            {
                if (_hdiAudibleAlarm)
                {
                    ScannerKeyboardPart.Keyboard.StartBeep(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(70), 3);
                    ControlService.ServicePart.SetContrabandAlarm(true);
                }

                if (_hdiLightAlarm)
                {
                    LightAlertAsync();
                    //XRayLampControlService.Service.Flare(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(100), 3);
                }
            }
        }

        /// <summary>
        /// 异步光报警输出：闪烁X射线指示灯
        /// </summary>
        public void LightAlertAsync()
        {
            Task.Run(() =>
            {
                try
                {
                    if ((_alarmLight & (int)(IndicatorLightIndex.XRayIndicator)) == (int)IndicatorLightIndex.XRayIndicator)
                    {
                        ControlService.ServicePart.FlickerXRayLamp(true);
                    }
                    else if ((_alarmLight & (int)(IndicatorLightIndex.YellowIndicator)) == (int)IndicatorLightIndex.YellowIndicator)
                    {
                        YellowLampControlService.Service.Flare(TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(500), 3);
                    }
                }
                catch (Exception)
                {
                }
            });
        }

        /// <summary>
        /// 毒品报警输出
        /// </summary>
        public void DeiAlert()
        {
            if (IsAlarmEnabled)
            {
                if (_deiAudibleAlarm)
                {
                    ScannerKeyboardPart.Keyboard.StartBeep(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(70), 3);
                    ControlService.ServicePart.SetContrabandAlarm(true);
                }

                if (_deiLightAlarm)
                {
                    LightAlertAsync();
                    //ControlService.ServicePart.FlickerXRayLamp(true);
                    //XRayLampControlService.Service.Flare(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(100), 3);
                }
            }
        }

        /// <summary>
        /// 爆炸物报警输出
        /// </summary>
        public void EiAlert()
        {
            if (IsAlarmEnabled)
            {
                if (_eiAudibleAlarm)
                {
                    ScannerKeyboardPart.Keyboard.StartBeep(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(70), 3);
                    ControlService.ServicePart.SetContrabandAlarm(true);
                }

                if (_eiLightAlarm)
                {
                    LightAlertAsync();
                }
            }
        }
    }
}
