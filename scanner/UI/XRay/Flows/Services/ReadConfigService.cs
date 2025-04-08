using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Control;

namespace UI.XRay.Flows.Services
{
    public class ReadConfigService
    {
        public static ReadConfigService Service { get; private set; }

        static ReadConfigService()
        {
            Service = new ReadConfigService();
        }
        public ReadConfigService()
        {
            //读取配置参数
            ReadConfig();   //初始化阶段读取版本号好像有问题
            GetXrayGenConfig();
            GetCaptureSystemConfig();
            GetSystemConfig();
        }
        private void ReadConfig()
        {
            SystemStatus.Instance.AppVersion = VersionHelper.Service.GetVersionStr(SubSystemComponents.Software);
            SystemStatus.Instance.AlgoVersion = VersionHelper.Service.GetVersionStr(SubSystemComponents.Algorithm);
            SystemStatus.Instance.CtrlBoardVersion = VersionHelper.Service.GetVersionStr(SubSystemComponents.ControlSys);

            SystemStatus.Instance.DiskRemainingSpace = ImageStoreDiskHelper.TotalSizeGB - ImageStoreDiskHelper.TotalUsedSpaceGB;
        }
        void GetXrayGenConfig()
        {
            XRayGeneratorType xrayGenType;
            if (ScannerConfig.Read(ConfigPath.XRayGenType, out xrayGenType))
            {
                SystemStatus.Instance.GeneratorType = xrayGenType.ToString();
            }
            float kV;
            if (ScannerConfig.Read(ConfigPath.XRayGenKV, out kV))
            {
                SystemStatus.Instance.Generator1Voltage = kV;
            }
            float mA;
            if (ScannerConfig.Read(ConfigPath.XRayGenMA, out mA))
            {
                SystemStatus.Instance.Generator1Current = mA;
            }

            float kV2;
            if (ScannerConfig.Read(ConfigPath.XRayGenKV2, out kV2))
            {
                SystemStatus.Instance.Generator2Voltage = kV2;
            }

            float mA2;
            if (ScannerConfig.Read(ConfigPath.XRayGenMA2, out mA2))
            {
                SystemStatus.Instance.Generator2Current = mA2;
            }
        }

        void GetCaptureSystemConfig()
        {
            CaptureSysTypeEnum captureSysType;
            if (ScannerConfig.Read<CaptureSysTypeEnum>(ConfigPath.CaptureSysType, out captureSysType))
            {
                SystemStatus.Instance.CaptureType = captureSysType.ToString();
            }
            float lineIntegrationTime;
            if (ScannerConfig.Read(ConfigPath.CaptureSysLineIntegrationTime, out lineIntegrationTime))
            {
                SystemStatus.Instance.LineIntegration = lineIntegrationTime;
            }
            string _model = "";
            if (ScannerConfig.Read(ConfigPath.SystemModel, out _model))
            {
                SystemStatus.Instance.MachineModel = _model;
            }
        }

        void GetSystemConfig()
        {
            float _conveyorSpeed = 0.2f;
            if (ScannerConfig.Read(ConfigPath.MachineConveyorSpeed, out _conveyorSpeed))
            {
                SystemStatus.Instance.ConveyorSpeed = _conveyorSpeed;
            }
            string _machineNum = "";
            if (ScannerConfig.Read(ConfigPath.SystemMachineNum, out _machineNum))
            {
                SystemStatus.Instance.MachineNumber = _machineNum;
            }
        }
        /// <summary>
        /// 初始化类
        /// </summary>
        public void Init()
        {
            ReadRuConfig();
        }

        #region Ru
        public bool IsRuSaveModeEnable { get; set; }
        public string AutoStoreUpfImagePath { get; set; }

        public bool IsUseUSBCommandKeyboard { get; set; }

        private void ReadRuConfig()
        {
            bool isEnable;
            if (ScannerConfig.Read(ConfigPath.IsRuSaveModeEnable,out isEnable))
            {
                IsRuSaveModeEnable = isEnable;
            }
            else
            {
                IsRuSaveModeEnable = false;
            }
            Tracer.TraceInfo("Ru Save Mode Enable: " + IsRuSaveModeEnable.ToString());

            string _autoStoreUpfImagePath;
            if(ScannerConfig.Read(ConfigPath.AutoStoreUpfImagePath, out _autoStoreUpfImagePath))
            {
                AutoStoreUpfImagePath = _autoStoreUpfImagePath;
            }
            else
            {
                AutoStoreUpfImagePath = ConfigPath.ManualStorePath;
            }
            if (!System.IO.Directory.Exists(AutoStoreUpfImagePath))
            {
                System.IO.Directory.CreateDirectory(AutoStoreUpfImagePath);
            }

            string keyboardType = string.Empty;
            if (!ScannerConfig.Read(ConfigPath.KeyboardComName, out keyboardType))
            {
                IsUseUSBCommandKeyboard = false;
            }
            if (keyboardType == "USB Common Keyboard")
            {
                IsUseUSBCommandKeyboard = true;
            }
        }
        #endregion
    }
}
