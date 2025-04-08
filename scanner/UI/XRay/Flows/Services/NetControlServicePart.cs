using System;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using UI.Common.Tracers;
using UI.XRay.Business.Algo;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Common.Utilities;
using UI.XRay.Control;
using UI.XRay.ControlWorkflows;
using XRayGeneratorIndex = UI.XRay.Control.XRayGeneratorIndex;
namespace UI.XRay.Flows.Services
{
    public class NetControlServicePart : IControlServicePart
    {
        private ControlSysWorkflow _controlSys;

        private ControlSysDescription _description;

        private IPEndPoint _computerEndPoint;

        private IPEndPoint _cardEndPoint;

        private int _xrayGenCount;

        private int _waitForTimeout = 100;//设置控制板网络连接等待超时

        private ushort _controlBoardCmdInterval = 9;

        /// <summary>
        /// 是否自动倒带，即是否开启节能光障功能
        /// </summary>
        private bool _rollbackWhenRestart;

        /// <summary>
        /// 电机回退距离
        /// </summary>
        private float _rollbackDistance;

        private double _xrayGen1MaRatio = 1.0;
        private double _xrayGen2MaRatio = 1.0;

        private double _xrayGen1KvRatio = 1.0;
        private double _xrayGen2KvRatio = 1.0;

        XRayGeneratorSettings xrayGenSetting;
        XRayGeneratorSettings xrayGenSetting2;

        public XRayGeneratorIndex AllXRayGen
        {
            get
            {
                if (_xrayGenCount == 2)
                {
                    return XRayGeneratorIndex.AllGenerators;
                }
                else
                {
                    return XRayGeneratorIndex.XRayGenerator1;
                }
            }
        }

        private int _conveyorChangeSpeedRatio;//速度转频率，9400

        public NetControlServicePart()
        {
            if (!ScannerConfig.Read(ConfigPath.XRayGenCount, out _xrayGenCount))
            {
                Tracer.TraceWarning("Could not get X-Ray generator count from config, use 1 as default.");
                _xrayGenCount = 1;
            }
        }

        private float conveyorSpeed;

        /// <summary>
        /// 读取并加载配置信息
        /// </summary>
        private void LoadSettings()
        {
            float entryPESensorToXRayGenDistance;
            if (!ScannerConfig.Read(ConfigPath.MachineEntryPESensorToXRayGenDistance, out entryPESensorToXRayGenDistance))
            {
                entryPESensorToXRayGenDistance = 0.24f;
            }

            float exitPESensorToXRayGenDistance;
            if (!ScannerConfig.Read(ConfigPath.MachineExitPESensorToXRayGenDistance, out exitPESensorToXRayGenDistance))
            {
                exitPESensorToXRayGenDistance = 0.24f;
            }

            float conveyorLength;
            if (!ScannerConfig.Read(ConfigPath.MachineConveyorLength, out conveyorLength))
            {
                conveyorLength = 1.6f;
            }

            int xrayGenCount;
            if (!ScannerConfig.Read(ConfigPath.XRayGenCount, out xrayGenCount))
            {
                xrayGenCount = 1;
            }

            XRayGeneratorType xrayGenType;
            if (!ScannerConfig.Read(ConfigPath.XRayGenType, out xrayGenType))
            {
                xrayGenType = XRayGeneratorType.XRayGen_KWD;
            }

            if (!ScannerConfig.Read(ConfigPath.XRayGenWaitTimeout, out _waitForTimeout))
            {
                _waitForTimeout = 110;
            }
            if (!ScannerConfig.Read(ConfigPath.ControlSysBoardCmdInterval, out _controlBoardCmdInterval))
            {
                _controlBoardCmdInterval = 9;
            }
            
            if (!ScannerConfig.Read(ConfigPath.XRayGenMARatio,out _xrayGen1MaRatio))
            {
                _xrayGen1MaRatio = 1.0;
            }
            if (!ScannerConfig.Read(ConfigPath.XRayGenMARatio2, out _xrayGen2MaRatio))
            {
                _xrayGen2MaRatio = 1.0;
            }
            if (!ScannerConfig.Read(ConfigPath.XRayGenKVRatio,out _xrayGen1KvRatio))
            {
                _xrayGen1KvRatio = 1.0;
            }
            if (!ScannerConfig.Read(ConfigPath.XRayGenKVRatio2, out _xrayGen2KvRatio))
            {
                _xrayGen2KvRatio = 1.0;
            }

            if (_xrayGen1MaRatio < 0.5 || _xrayGen1MaRatio > 2) _xrayGen1MaRatio = 1.0;
            if (_xrayGen2MaRatio < 0.5 || _xrayGen2MaRatio > 2) _xrayGen2MaRatio = 1.0;
            if (_xrayGen1KvRatio < 0.5 || _xrayGen1KvRatio > 2) _xrayGen1KvRatio = 1.0;
            if (_xrayGen2KvRatio < 0.5 || _xrayGen2KvRatio > 2) _xrayGen2KvRatio = 1.0;

            float kv;
            if (!ScannerConfig.Read(ConfigPath.XRayGenKV, out kv))
            {
                kv = 160f;
            }

            float ma;
            if (!ScannerConfig.Read(ConfigPath.XRayGenMA, out ma))
            {
                ma = 0.5f;
            }

            float kv2;
            if (!ScannerConfig.Read(ConfigPath.XRayGenKV2, out kv2))
            {
                kv2 = 160f;
            }

            float ma2;
            if (!ScannerConfig.Read(ConfigPath.XRayGenMA2, out ma2))
            {
                ma2 = 0.5f;
            }

            xrayGenSetting = new XRayGeneratorSettings(xrayGenType, kv, ma, kv2, ma2);
            xrayGenSetting2 = new XRayGeneratorSettings(xrayGenType, kv2, ma2, kv2, ma2);

            // float conveyorSpeed;
            if (!ScannerConfig.Read(ConfigPath.MachineConveyorSpeed, out conveyorSpeed))
            {
                conveyorSpeed = 0.2f;
            }
            //是否是双向扫描
            bool _bidirectionScan = true;
            if (!ScannerConfig.Read(ConfigPath.MachineBiDirectionScan, out _bidirectionScan))
            {
                _bidirectionScan = true;
            }
            BidirectionalScan = _bidirectionScan;

            bool _conveyorDefaultDirection = true;
            if (!ScannerConfig.Read(ConfigPath.MachineDirectionExchangeConveyor, out _conveyorDefaultDirection))
            {
                _conveyorDefaultDirection = true;
            }

            if (!ScannerConfig.Read(ConfigPath.MachineConveyorSpeedChangeRatio, out _conveyorChangeSpeedRatio))
            {
                _conveyorChangeSpeedRatio = 9400;
                ScannerConfig.Write(ConfigPath.MachineConveyorSpeedChangeRatio, _conveyorChangeSpeedRatio);
            }

            float onPosBeforeXRayGen;
            if (!ScannerConfig.Read(ConfigPath.MachineXRayOnPosBeforeXRayGen, out onPosBeforeXRayGen))
            {
                onPosBeforeXRayGen = 0.3f;
            }

            float offPosAfterXRayGen;
            if (!ScannerConfig.Read(ConfigPath.MachineXRayOffPosAfterXRayGen, out offPosAfterXRayGen))
            {
                offPosAfterXRayGen = 0.3f;
            }

            //是否启用节能光障
            bool enableConveyorTriggerSensor;
            if (!ScannerConfig.Read(ConfigPath.EnableConveyorTriggerSensor, out enableConveyorTriggerSensor))
            {
                enableConveyorTriggerSensor = false;
            }

            int entryConveyorTriggerSensorIndex;
            if (!ScannerConfig.Read(ConfigPath.EntryConveyorTriggerSensorIndex, out entryConveyorTriggerSensorIndex))
            {
                entryConveyorTriggerSensorIndex = 2;
            }

            //出口节能光障被触发后电机延时停止时间
            double conveyorTriggerSensorMotorStopDelayTimeSpan;
            if (!ScannerConfig.Read(ConfigPath.ConveyorTriggerSensorMotorStopDelayTimeSpan, out conveyorTriggerSensorMotorStopDelayTimeSpan))
            {
                conveyorTriggerSensorMotorStopDelayTimeSpan = 20.0;
            }

            bool pcControlXRayGen;

            if (!ScannerConfig.Read(ConfigPath.PcControlXRayGen, out pcControlXRayGen))
            {
                pcControlXRayGen = false;
            }

            if (!ScannerConfig.Read(ConfigPath.MachineInterruptModeRollbackDistance, out _rollbackDistance))
            {
                _rollbackDistance = 0.35f;
            }

            string xrayGen1ComName = null;
            string xrayGen2ComName = null;

            if (pcControlXRayGen)
            {
                if (!ScannerConfig.Read(ConfigPath.XRayGen1ComName, out xrayGen1ComName))
                {
                    xrayGen1ComName = "Com1";
                }

                if (xrayGenCount == 2)
                {
                    if (!ScannerConfig.Read(ConfigPath.XRayGen2ComName, out xrayGen2ComName))
                    {
                        xrayGen2ComName = "Com2";
                    }
                }
            }

            bool exchangeEntrySensor = false;
            if (!ScannerConfig.Read(ConfigPath.MachineDirectionExchangePESensor, out exchangeEntrySensor))
            {
                exchangeEntrySensor = false;
            }


            _description = new ControlSysDescription(entryPESensorToXRayGenDistance, exitPESensorToXRayGenDistance, conveyorLength,
                xrayGenCount, conveyorSpeed, enableConveyorTriggerSensor, entryConveyorTriggerSensorIndex,
                conveyorTriggerSensorMotorStopDelayTimeSpan, xrayGenSetting, xrayGen1ComName, xrayGen2ComName, _rollbackDistance, 
                _conveyorDefaultDirection, exchangeEntrySensor);

            _description.XRayOnPosBeforeXRayGen = onPosBeforeXRayGen;
            _description.XRayOffPosAfterXRayGen = offPosAfterXRayGen;


            if (!ScannerConfig.Read(ConfigPath.MachineAutoRewind, out _rollbackWhenRestart))
            {
                _rollbackWhenRestart = false;
            }

            string computerIpString;
            IPAddress computerIp = null;
            if (!(ScannerConfig.Read(ConfigPath.ComputerIp, out computerIpString) && IPAddress.TryParse(computerIpString, out computerIp)))
            {
                Tracer.TraceError("Could not load Computer IP address from config.");

                var all = NetworkUtil.GetAllIpV4Addresses();
                if (all != null && all.Count > 0)
                {
                    computerIp = all.First();
                    Tracer.TraceWarning("Use following computer IP to connect with control system: " + computerIp.ToString());
                }

                if (computerIp == null)
                {
                    Tracer.TraceError("No computer Ip could be used to connect to control system. Use IPAddress.Any now.");
                    computerIp = IPAddress.Any;
                }
            }

            IPAddress ctrlSysIp = null;
            string ctrlSysIpString;
            if (!(ScannerConfig.Read(ConfigPath.ControlSysBoardIp, out ctrlSysIpString) && IPAddress.TryParse(ctrlSysIpString, out ctrlSysIp)))
            {
                ctrlSysIp = IPAddress.Parse("192.168.1.150");
            }

            ushort port;
            if (!ScannerConfig.Read(ConfigPath.ControlSysBoardPort, out port))
            {
                port = 10050;
            }

            // 计算机和控制板的网络地址
            _computerEndPoint = new IPEndPoint(computerIp, port);
            _cardEndPoint = new IPEndPoint(ctrlSysIp, port);
        }

        #region IControlServicePart接口实现

        public event EventHandler DeviceOpened;

        public event EventHandler DeviceClosed;

        public event EventHandler<bool> ConnectionStateUpdated;

        public event EventHandler<Control.ConveyorDirectionChangedEventArgs> ConveyorDirectionChanged;

        public event EventHandler<Control.XRayGenAliveChangedEventArgs> XRayGenAliveChanged;

        public event EventHandler<Control.PESensorStateChangedEventArgs> PESensorStateChanged;

        public event EventHandler<ControlWorkflows.PreviewXRayClosingEventArgs> PreviewXRayClosing;

        public event EventHandler<ControlWorkflows.XRayStateChangedEventArgs> XRayStateChanged;

        public event EventHandler<ControlWorkflows.WorkModeChangedEventArgs> WorkModeChanged;

        public event EventHandler<Control.SwitchStateChangedEventArgs> SwitchStateChanged;

        public event EventHandler<ScannerWorkingStates> ScannerWorkingStatesUpdated;

        public event EventHandler<bool> SystemShutDown;

        public event EventHandler EnterInterruptMode;

        public event EventHandler EnterMaintenanceMode;

        public ControlWorkflows.ScannerWorkMode WorkMode
        {
            get
            {
                if (IsOpened)
                {
                    return _controlSys.WorkMode;
                }

                return ScannerWorkMode.Regular;
            }
        }

        public bool BidirectionalScan
        {
            get;
            set;
        }

        public bool IsOpened { get; private set; }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool Open()
        {
            if (_controlSys != null)
            {
                Close();
            }

            LoadSettings();

            _controlSys = new ControlSysWorkflow(_description, BidirectionalScan, _rollbackWhenRestart);

            if (_controlSys.Connect(_computerEndPoint, _cardEndPoint, (uint)_waitForTimeout))
            {
                IsOpened = true;
                _controlSys.SetWorkMode(ScannerWorkMode.Regular);

                _controlSys.ScannerWorkingStatesUpdated += ControlSysOnScannerWorkingStatesUpdated;
                _controlSys.XRayStateChanged += ControlSysOnXRayStateChanged;
                _controlSys.ConveyorDirectionChanged += ControlSysOnConveyorDirectionChanged;
                _controlSys.SwitchStateChanged += ControlSysOnSwitchStateChanged;
                _controlSys.PreviewXRayClosing += ControlSysOnPreviewXRayClosing;
                _controlSys.PESensorStateChanged += ControlSysOnPeSensorStateChanged;
                _controlSys.XRayGenAliveChanged += ControlSysOnXRayGenAliveChanged;
                _controlSys.WorkModeChanged += ControlSys_WorkModeChanged;
                _controlSys.EnterInterruptMode += ControlSysOnEnterInterruptMode;

                if (DeviceOpened != null)
                {
                    DeviceOpened(this, EventArgs.Empty);
                }
                SetXRayCmdInterval(_controlBoardCmdInterval);

                System.Threading.Thread.Sleep(200);

                ChangeXRayGenSettings(xrayGenSetting, XRayGeneratorIndex.XRayGenerator1);
                if (_xrayGenCount > 1)
                {
                    System.Threading.Thread.Sleep(200);
                    ChangeXRayGenSettings(xrayGenSetting2, XRayGeneratorIndex.XRayGenerator2);
                }
                
                //ChangeXRayGenSettings(_description.XRayGensSettings,XRayGeneratorIndex.AllGenerators); 
                return true;
            }

            return false;
        }

        void ControlSys_WorkModeChanged(object sender, WorkModeChangedEventArgs e)
        {
            if (WorkModeChanged != null)
            {
                WorkModeChanged(this, e);
            }
        }

        private void ControlSysOnScannerWorkingStatesUpdated(object sender, ScannerWorkingStates scannerWorkingStates)
        {
            if (ScannerWorkingStatesUpdated != null)
            {
                ScannerWorkingStatesUpdated(this, scannerWorkingStates);
            }
        }

        /// <summary>
        /// 射线源与控制板之间连接状态发生变化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="xRayGenAliveChangedEventArgs"></param>
        private void ControlSysOnXRayGenAliveChanged(object sender, XRayGenAliveChangedEventArgs xRayGenAliveChangedEventArgs)
        {
            if (XRayGenAliveChanged != null)
            {
                XRayGenAliveChanged(this, xRayGenAliveChangedEventArgs);
            }
        }

        private void ControlSysOnPeSensorStateChanged(object sender, PESensorStateChangedEventArgs peSensorStateChangedEventArgs)
        {
            Tracer.TraceInfo("[PESensor] StateChanged: Index: " + peSensorStateChangedEventArgs.PESensor + " Direction: " + peSensorStateChangedEventArgs.Direction + 
                " Old: " + peSensorStateChangedEventArgs.OldState + " New: " + peSensorStateChangedEventArgs.NewState);
            if (PESensorStateChanged != null)
            {
                PESensorStateChanged(this, peSensorStateChangedEventArgs);
            }
        }

        private void ControlSysOnPreviewXRayClosing(object sender, PreviewXRayClosingEventArgs previewXRayClosingEventArgs)
        {
            if (PreviewXRayClosing != null)
            {
                PreviewXRayClosing(this, previewXRayClosingEventArgs);
            }
        }

        private void ControlSysOnSwitchStateChanged(object sender, SwitchStateChangedEventArgs switchStateChangedEventArgs)
        {
            if (SwitchStateChanged != null)
            {
                SwitchStateChanged(this, switchStateChangedEventArgs);
            }
        }

        private void ControlSysOnConveyorDirectionChanged(object sender, ConveyorDirectionChangedEventArgs conveyorDirectionChangedEventArgs)
        {
            if (ConveyorDirectionChanged != null)
            {
                ConveyorDirectionChanged(this, conveyorDirectionChangedEventArgs);
            }
        }

        private void ControlSysOnXRayStateChanged(object sender, XRayStateChangedEventArgs xRayStateChangedEventArgs)
        {
            if (XRayStateChanged != null)
            {
                XRayStateChanged(this, xRayStateChangedEventArgs);
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Close()
        {
            if (_controlSys != null)
            {
                //DriveConveyor(ConveyorDirection.Stop);
                //RadiateXRay(false);
                //PowerOnPESensors(false);
                if (_controlSys != null)
                {
                    _controlSys.Disconnect();
                    _controlSys.Dispose();
                }

                IsOpened = false;

                _controlSys.XRayStateChanged -= ControlSysOnXRayStateChanged;
                _controlSys.ConveyorDirectionChanged -= ControlSysOnConveyorDirectionChanged;
                _controlSys.SwitchStateChanged -= ControlSysOnSwitchStateChanged;
                _controlSys.PreviewXRayClosing -= ControlSysOnPreviewXRayClosing;
                _controlSys.PESensorStateChanged -= ControlSysOnPeSensorStateChanged;
                _controlSys.XRayGenAliveChanged -= ControlSysOnXRayGenAliveChanged;
                _controlSys.WorkModeChanged -= ControlSys_WorkModeChanged;
                _controlSys.EnterInterruptMode -= ControlSysOnEnterInterruptMode;


                if (DeviceClosed != null)
                {
                    DeviceClosed(this, EventArgs.Empty);
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void ControlSysOnEnterInterruptMode(object sender, EventArgs e)
        {
            if (EnterInterruptMode != null)
            {
                EnterInterruptMode(this, e);
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool SetWorkMode(ControlWorkflows.ScannerWorkMode mode)
        {
            if (IsOpened)
            {
                return _controlSys.SetWorkMode(mode);
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool EnableBidirectionalScan(bool enabled)
        {
            if (IsOpened)
            {
                return _controlSys.EnableBidirectionalScan(enabled);
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool DriveConveyor(Control.ConveyorDirection direction)
        {
            if (IsOpened)
            {
                Tracer.TraceInfo(direction.ToString() + " is send to controlboard");
                return _controlSys.DriveConveyor(direction);
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool RebootControlSys()
        {
            if (IsOpened)
            {
                return _controlSys.Reboot();
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool ShutdownScanner()
        {
            if (SystemShutDown != null)
            {
                SystemShutDown.Invoke(this, false);
            }

            if (IsOpened)
            {
                return _controlSys.Shutdown();
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool RadiateXRay(bool radiate)
        {
            if (IsOpened)
            {
                //return _controlSys.RadiateXRay(radiate, XRayGeneratorIndex.AllGenerators);
                if (_xrayGenCount == 1)
                {
                    return _controlSys.RadiateXRay(radiate, XRayGeneratorIndex.XRayGenerator1);
                }
                else
                {
                    return _controlSys.RadiateXRay(radiate, XRayGeneratorIndex.AllGenerators);
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool LightXRayLamp(bool on)
        {
            if (IsOpened)
            {
                return _controlSys.ToggleIndicatorLights(on, IndicatorLightIndex.XRayIndicator);
            }

            return false;
        }
        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool LightYellowLamp(bool on)
        {
            if (IsOpened)
            {
                return _controlSys.ToggleIndicatorLights(on, IndicatorLightIndex.YellowIndicator);
            }

            return false;
        }


        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool FlickerXRayLamp(bool fastMode)
        {
            if (IsOpened)
            {
                return _controlSys.FlickerXRayLamp(fastMode);
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool PowerOnPESensors(bool activate)
        {
            if (IsOpened)
            {
                return _controlSys.PowerOnPESensors(activate, PESensorIndex.PESensor1 | PESensorIndex.PESensor3);
            }

            return false;
        }
        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool PowerOnPESensors(bool activate, int index)
        {
            if (IsOpened)
            {
                return _controlSys.PowerOnPESensors(activate, (PESensorIndex)(1 << (index - 1)));
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool Beep(bool beep)
        {
            if (IsOpened)
            {
                return _controlSys.Beep(beep);
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool GetSystemDesc(out string desc)
        {
            if (IsOpened)
            {
                var fwVersion = new CtrlSysVersion();
                var protocalVertion = new CtrlSysVersion();
                if (_controlSys.QueryVersion(ref fwVersion, ref protocalVertion))
                {
                    var builder = new StringBuilder(100);
                    builder.Append("Firmware: ").Append(fwVersion.ToString());
                    builder.Append(" ");
                    builder.Append("Protocol: ").Append(protocalVertion.ToString());

                    desc = builder.ToString();
                    return true;
                }
            }

            desc = string.Empty;
            return false;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool GetSystemDesc(ref CtrlSysVersion firmware, ref CtrlSysVersion protocol)
        {
            if (IsOpened)
            {
                if (_controlSys.QueryVersion(ref firmware, ref protocol))
                {
                    return true;
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool GetXRayGenSettings(out XRayGeneratorSettings settings)
        {
            if (IsOpened)
            {
                XRayGeneratorSettings result = null;
                if (_controlSys.QueryXRayGeneratorSettings(ref result))
                {
                    settings = result;
                    return true;
                }
            }

            settings = null;
            return false;
        }

        /// <summary>
        /// 更改射线源配置，并且存入闪存中
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool ChangeXRayGenSettings(XRayGeneratorSettings settings, XRayGeneratorIndex index)
        {
            if (IsOpened)
            {
                if (index == XRayGeneratorIndex.XRayGenerator1)
                {
                    settings.MA = (float)Math.Round(settings.MA * _xrayGen1MaRatio, 3);
                    settings.KV = (float)Math.Round(settings.KV * _xrayGen1KvRatio, 3);
                }
                if (index == XRayGeneratorIndex.XRayGenerator2)
                {
                    settings.MA = (float)Math.Round(settings.MA * _xrayGen2MaRatio, 3);
                    settings.KV = (float)Math.Round(settings.KV * _xrayGen2KvRatio, 3);
                }
                Tracer.TraceInfo(string.Format("Setting Xraygen parameters: view:{0},kv:{1},ma:{2}", index, settings.MA, settings.KV));

                if (ExchangeDirectionConfig.Service.IsExchangeXrayGen)
                {
                    if (index == XRayGeneratorIndex.XRayGenerator1)
                    {
                        index = XRayGeneratorIndex.XRayGenerator2;
                    }
                    else
                    {
                        index = XRayGeneratorIndex.XRayGenerator1;
                    }
                }

                if (_controlSys.SetXRayGeneratorSettings(index, settings))
                {
                    Tracer.TraceInfo("Start to SaveAllSettingsToFlash!");//yxc
                    bool bResult=_controlSys.SaveAllSettingsToFlash(); //yxc
                    Tracer.TraceInfo("Net board Save All Settings To Flash:"+ (bResult ? "success" : "failure"));//yxc
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 更改射线源的配置，并存储到flash中
        /// </summary>
        /// <param name="kv"></param>
        /// <param name="ma"></param>
        /// <returns></returns>
        //private void ChangeXRayGenSettings(double kv, double ma, XRayGeneratorIndex index)
        //{
        //    XRayGeneratorSettings settings;
        //    if (ControlService.ServicePart.GetXRayGenSettings(out settings))
        //    {
        //        settings.KV = (float)kv;
        //        settings.MA = (float)ma;
        //        if (ControlService.ServicePart.ChangeXRayGenSettings(settings, index))
        //        {
        //        }
        //    }

        //}

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool GetWorkingStates(out ScannerWorkingStates states)
        {
            if (IsOpened)
            {
                ScannerWorkingStates result = null;
                if (_controlSys.QueryScannerWorkingStates(ref result))
                {
                    states = result;
                    return true;
                }
            }

            states = null;
            return false;
        }


        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool GetXRayGenWorkingStates(out XRayGeneratorWorkingStates states)
        {
            if (IsOpened)
            {
                XRayGeneratorWorkingStates result = null;
                if (_controlSys.QueryXRayGeneratorWorkingStates(ref result))
                {
                    if (ExchangeDirectionConfig.Service.IsExchangeXrayGen)
                    {
                        var view1ma = result.XRayGen1mA;
                        var view1kv = result.XRayGen1kV;
                        result.XRayGen1mA = result.XRayGen2mA;
                        result.XRayGen1kV = result.XRayGen2kV;
                        result.XRayGen2mA = view1ma;
                        result.XRayGen2kV = view1kv;
                    }
                    result.XRayGen1mA = (float)Math.Round(result.XRayGen1mA * (1 / _xrayGen1MaRatio), 3);
                    result.XRayGen2mA = (float)Math.Round(result.XRayGen2mA * (1 / _xrayGen2MaRatio), 3);
                    result.XRayGen1kV = (float)Math.Round(result.XRayGen1kV * (1 / _xrayGen1KvRatio), 3);
                    result.XRayGen2kV = (float)Math.Round(result.XRayGen2kV * (1 / _xrayGen2KvRatio), 3);
                    states = result;
                    return true;
                }
            }

            states = null;
            return false;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool GetTotalBagCount(ref int bagCount)
        {
            if (IsOpened)
            {
                int countFromControlSys = 0;
                if (_controlSys.QueryTotalBagCount(ref countFromControlSys))
                {
                    bagCount = countFromControlSys;
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool ReloadTotalBagCount(uint bagCount)
        {
            if (IsOpened)
            {
                return _controlSys.ReloadTotalBagCount(bagCount);
            }

            return false;
        }

        /// <summary>
        /// 设置速度
        /// </summary>
        /// <param name="speed">conveyor speed</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool SetConveyorSpeed(float speed)
        {
            if (!ScannerConfig.Read(ConfigPath.MachineConveyorSpeedChangeRatio, out _conveyorChangeSpeedRatio))
            {
                _conveyorChangeSpeedRatio = 9400;
            }
            if (IsOpened)
            {
                return _controlSys.SetConveyorSpeed((ushort)(speed * _conveyorChangeSpeedRatio));
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool SetConveyorStartTime(ushort time)
        {
            if (IsOpened)
            {
                return _controlSys.SetConveyorStartTime(time);
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool SetConveyorStopTime(ushort time)
        {
            if (IsOpened)
            {
                return _controlSys.SetConveyorStopTime(time);
            }
            return false;
        }

        /// <summary>
        /// 设置消杀电机速度
        /// </summary>
        /// <param name="speed">conveyor speed</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool SetSanitizeConveyorSpeed(float speed)
        {
            if (!ScannerConfig.Read(ConfigPath.MachineConveyorSpeedChangeRatio, out _conveyorChangeSpeedRatio))
            {
                _conveyorChangeSpeedRatio = 9400;
            }
            if (IsOpened)
            {
                return _controlSys.SetSanitizeConveyorSpeed((ushort)(speed * _conveyorChangeSpeedRatio));
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool SetSanitizeConveyorStartTime(ushort time)
        {
            if (IsOpened)
            {
                return _controlSys.SetSanitizeConveyorStartTime(time);
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool SetSanitizeConveyorStopTime(ushort time)
        {
            if (IsOpened)
            {
                return _controlSys.SetSanitizeConveyorStopTime(time);
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool SetXRayCmdInterval(ushort time)
        {
            if (IsOpened)
            {
                return _controlSys.SetXRayCommandInterval(time);
            }
            return false;
        }
        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool SetBagCount(byte mode, ushort count)
        {
            if (IsOpened)
            {
                return _controlSys.SetBagCount(mode, count);
            }
            return false;
        }
        public bool SetContrabandAlarm(bool isOn)
        {
            if (IsOpened)
            {
                return _controlSys.SetContrabandAlarm(isOn);
            }
            return false;
        }

        public bool SetWorkTiming(bool isEnable)
        {
            if (IsOpened)
            {
                return _controlSys.SetWorkTiming(isEnable);
            }
            return false;
        }
        #endregion IControlServicePart接口实现
    }
}
