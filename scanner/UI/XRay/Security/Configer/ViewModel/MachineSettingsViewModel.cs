using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Services;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Configer.ViewModel
{
    /// <summary>
    /// 设备相关设置视图的ViewModel
    /// </summary>
    public class MachineSettingsViewModel : ViewModelBase, IViewModel
    {
        private float _conveySpeed;
        /// <summary>
        /// 输送带速度，单位m/s
        /// </summary>
        public float ConveySpeed
        {
            get { return _conveySpeed; }
            set
            {
                if (value < _conveyorMinSpeed)
                {
                    value = _conveyorMinSpeed;
                }
                else if (value > _conveyorMaxSpeed)
                {
                    value = _conveyorMaxSpeed;
                }
                _conveySpeed = value;
                RaisePropertyChanged();
            }
        }

        private float _sanitizeConveySpeed;
        /// <summary>
        /// 消杀输送带速度，单位m/s
        /// </summary>
        public float SanitizeConveySpeed
        {
            get { return _sanitizeConveySpeed; }
            set
            {
                if (value < _conveyorMinSpeed)
                {
                    value = _conveyorMinSpeed;
                }
                else if (value > _conveyorMaxSpeed)
                {
                    value = _conveyorMaxSpeed;
                }
                _sanitizeConveySpeed = value;
                RaisePropertyChanged();
            }
        }

        private float _conveyorMaxSpeed;
        private float _conveyorMinSpeed;

        private ushort _conveyorStartTime;
        public ushort ConveyorStartTime
        {
            get { return _conveyorStartTime; }
            set { _conveyorStartTime = value; RaisePropertyChanged(); }
        }

        private ushort _conveyorStopTime;
        public ushort ConveyorStopTime
        {
            get { return _conveyorStopTime; }
            set { _conveyorStopTime = value; RaisePropertyChanged(); }
        }

        private ushort _sanitizeConveyStartTime;
        public ushort SanitizeConveyStartTime
        {
            get { return _sanitizeConveyStartTime; }
            set { _sanitizeConveyStartTime = value; RaisePropertyChanged(); }
        }

        private ushort _sanitizeConveyStopTime;
        public ushort SanitizeConveyStopTime
        {
            get { return _sanitizeConveyStopTime; }
            set { _sanitizeConveyStopTime = value; RaisePropertyChanged(); }
        }

        private int _equipmentFrequency;
        public int EquipmentFrequency
        {
            get { return _equipmentFrequency; }
            set { _equipmentFrequency = value; RaisePropertyChanged(); }
        }

        private bool _canChangeConveyorSpeed;
        public bool CanChangeConveyorSpeed
        {
            get
            {
                SetConveyorSpeed(_canChangeConveyorSpeed);
                return _canChangeConveyorSpeed;
            }
            set
            {
                SetConveyorSpeed(_canChangeConveyorSpeed);
                _canChangeConveyorSpeed = value;
                RaisePropertyChanged();
            }
        }

        private void SetConveyorSpeed(bool visible)
        {
            if (visible)
            {
                SetConveyorSpeedVisibility = Visibility.Visible;
            }
            else
            {
                SetConveyorSpeedVisibility = Visibility.Collapsed;
            }
        }

        private Visibility _setConveyorSpeedVisibility;
        public Visibility SetConveyorSpeedVisibility
        {
            get { return _setConveyorSpeedVisibility; }
            set { _setConveyorSpeedVisibility = value; RaisePropertyChanged(); }
        }

        private Visibility _setSanitizeConveyorSpeedVisibility;
        public Visibility SetSanitizeConveyorSpeedVisibility
        {
            get { return _setSanitizeConveyorSpeedVisibility; }
            set { _setSanitizeConveyorSpeedVisibility = value; RaisePropertyChanged(); }
        }

        private bool _multiFfrequency;
        public bool MultiFfrequency
        {
            get { return _multiFfrequency; }
            set 
            { 
                _multiFfrequency = value;
                if (_multiFfrequency)
                    SetSanitizeConveyorSpeedVisibility = Visibility.Visible;
                else
                    SetSanitizeConveyorSpeedVisibility = Visibility.Collapsed;
                RaisePropertyChanged(); 
            }
        }

        private float _conveyLength;

        private int _viewsCount = 1;

        /// <summary>
        /// 视角数量，用于UI绑定
        /// </summary>
        public int ViewsCount
        {
            get { return _viewsCount; }
            set
            {
                _viewsCount = value;
                if (_viewsCount == 1)
                {
                    ShowView2Settings = Visibility.Collapsed;
                }
                else
                {
                    ShowView2Settings = Visibility.Visible;
                }
                RaisePropertyChanged();
            }
        }

        private Visibility _showView2Settings = Visibility.Collapsed;

        /// <summary>
        /// 是否显示视角2的设置，如果选中双视角，则显示，否者不显示，用于UI绑定
        /// </summary>
        public Visibility ShowView2Settings
        {
            get { return _showView2Settings; }
            set
            {
                _showView2Settings = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 视角数量列表，用于UI绑定
        /// </summary>
        public List<int> ViewsCountSource { get; set; }


        /// <summary>
        /// 输送带长度，单位m
        /// </summary>
        public float ConveyLength
        {
            get { return _conveyLength; }
            set
            {
                _conveyLength = value;
                RaisePropertyChanged();
            }
        }

        private float _viewsDistance;

        private float _entryPESensorToXRayGenDistance;
        /// <summary>
        /// 入口光障到射线缝的距离，单位m
        /// </summary>
        public float EntryPESensorToXRayGenDistance
        {
            get { return _entryPESensorToXRayGenDistance; }
            set
            {
                _entryPESensorToXRayGenDistance = value;
                RaisePropertyChanged();
            }
        }

        private float _exitPESensorToXRayGenDistance;
        /// <summary>
        /// 出口光障到射线缝的距离，单位m
        /// </summary>
        public float ExitPESensorToXRayGenDistance
        {
            get { return _exitPESensorToXRayGenDistance; }
            set
            {
                _exitPESensorToXRayGenDistance = value;
                RaisePropertyChanged();
            }
        }                     

        /// <summary>
        /// 两个视角之间的距离，单位为m
        /// </summary>
        public float ViewsDistance
        {
            get { return _viewsDistance; }
            set { _viewsDistance = value;  RaisePropertyChanged();}
        }

        private float _xrayOnPosBeforeXRayGen;
        /// <summary>
        /// 射线提前发射的位置，相距于运转方向的第一个射线源的位置，米
        /// </summary>
        public float XrayOnPosBeforeXRayGen
        {
            get { return _xrayOnPosBeforeXRayGen; }
            set { _xrayOnPosBeforeXRayGen = value; RaisePropertyChanged();}
        }

        private float _xrayOffPosAfterXRayGen;
        /// <summary>
        /// 射线延迟关闭的位置，相距于运转方向的第一个射线源的位置，米
        /// </summary>
        public float XrayOffPosAfterXRayGen
        {
            get { return _xrayOffPosAfterXRayGen; }
            set { _xrayOffPosAfterXRayGen = value; RaisePropertyChanged();}
        }

        private bool _autoRewind;
        /// <summary>
        /// 是否自动倒带（扫描期间，突然停机，重新开始时自动倒带）
        /// </summary>
        public bool AutoRewind
        {
            get { return _autoRewind; }
            set { _autoRewind = value; RaisePropertyChanged(); }
        }

        private bool _directionalScan;
        public bool BidirectionalScan
        {
            get { return _directionalScan; }
            set { _directionalScan = value; RaisePropertyChanged(); }
        }

        private Visibility _controlIntervalVisibility = Visibility.Collapsed;
        public Visibility ControlIntervalVisibility
        {
            get { return _controlIntervalVisibility; }
            set { _controlIntervalVisibility = value; RaisePropertyChanged(); }
        }

        private bool _exchangePESensor;

        public bool ExchangePESensor
        {
            get { return _exchangePESensor; }
            set { _exchangePESensor = value; RaisePropertyChanged(); }
        }

        private bool _exchangeXrayGen;

        public bool ExchangeXrayGen
        {
            get { return _exchangeXrayGen; }
            set { _exchangeXrayGen = value; RaisePropertyChanged(); }
        }

        private bool _exchangeDetector;

        public bool ExchangeDetector
        {
            get { return _exchangeDetector; }
            set { _exchangeDetector = value; RaisePropertyChanged(); }
        }

        private bool _exchangeConveyor;

        public bool ExchangeConveyor
        {
            get { return _exchangeConveyor; }
            set { _exchangeConveyor = value; RaisePropertyChanged(); }
        }


        public MachineSettingsViewModel()
        {
            try
            {
                ViewsCountSource = new List<int> { 1, 2 };
                LoadSettings();
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        /// <summary>
        /// 保存设备设置到本地
        /// </summary>
        public void SaveSettings()
        {
            if (ViewsCount > 1)
            {
                ScannerConfig.Write(ConfigPath.MachineViewsDistanceForForward, ViewsDistance);
            }

            ScannerConfig.Write(ConfigPath.MachineXRayOnPosBeforeXRayGen, XrayOnPosBeforeXRayGen);
            ScannerConfig.Write(ConfigPath.MachineXRayOffPosAfterXRayGen, XrayOffPosAfterXRayGen);
            ScannerConfig.Write(ConfigPath.MachineViewsCount, ViewsCount);
            ScannerConfig.Write(ConfigPath.MachineConveyorSpeed, _conveySpeed);
            ScannerConfig.Write(ConfigPath.SanitizeConveySpeed, _sanitizeConveySpeed);
            ScannerConfig.Write(ConfigPath.MachineCanChangeConveyorSpeed, _canChangeConveyorSpeed);
            ScannerConfig.Write(ConfigPath.MultiFfrequency, _multiFfrequency);
            ScannerConfig.Write(ConfigPath.MachineConveyorStartTime, (ushort)_conveyorStartTime);
            ScannerConfig.Write(ConfigPath.MachineConveyorStopTime, (ushort)_conveyorStopTime);
            ScannerConfig.Write(ConfigPath.SanitizeConveyStartTime, (ushort)_sanitizeConveyStartTime);
            ScannerConfig.Write(ConfigPath.SanitizeConveyStopTime, (ushort)_sanitizeConveyStopTime);
            ScannerConfig.Write(ConfigPath.MachineConveyorSpeedChangeRatio, _equipmentFrequency);
            ScannerConfig.Write(ConfigPath.MachineConveyorLength, _conveyLength);
            ScannerConfig.Write(ConfigPath.MachineEntryPESensorToXRayGenDistance, _entryPESensorToXRayGenDistance);
            ScannerConfig.Write(ConfigPath.MachineExitPESensorToXRayGenDistance, _exitPESensorToXRayGenDistance);
            ScannerConfig.Write(ConfigPath.MachineAutoRewind, _autoRewind);
            ScannerConfig.Write(ConfigPath.MachineBiDirectionScan, _directionalScan);

            ScannerConfig.Write(ConfigPath.MachineDirectionExchangePESensor, _exchangePESensor);
            ScannerConfig.Write(ConfigPath.MachineDirectionExchangeDetector, _exchangeDetector);
            ScannerConfig.Write(ConfigPath.MachineDirectionExchangeConveyor, _exchangeConveyor);
            ScannerConfig.Write(ConfigPath.MachineDirectionExchangeXrayGen, _exchangeXrayGen);

            SetSpeedFrequency();
        }

        /// <summary>
        /// 从本地加载设备配置
        /// </summary>
        public void LoadSettings()
        {
            if (!ScannerConfig.Read(ConfigPath.MachineXRayOnPosBeforeXRayGen, out _xrayOnPosBeforeXRayGen))
            {
                XrayOnPosBeforeXRayGen = 0;
            }

            if (!ScannerConfig.Read(ConfigPath.MachineXRayOffPosAfterXRayGen, out _xrayOffPosAfterXRayGen))
            {
                XrayOffPosAfterXRayGen = 0;
            }

            if (!ScannerConfig.Read(ConfigPath.MachineViewsCount, out _viewsCount))
            {
                _viewsCount = 1;
            }

            if (_viewsCount > 1)
            {
                if (!ScannerConfig.Read(ConfigPath.MachineViewsDistanceForForward, out _viewsDistance))
                {
                    ViewsDistance = 0.2f;
                }

                ShowView2Settings = Visibility.Visible;
            }

            if (!ScannerConfig.Read(ConfigPath.MachineConveyorSpeed, out _conveySpeed))
            {
                ConveySpeed = 0.2f;
            }            

            if (!ScannerConfig.Read(ConfigPath.SanitizeConveySpeed, out _sanitizeConveySpeed))
            {
                SanitizeConveySpeed = 0.2f;
            }
            if (!ScannerConfig.Read(ConfigPath.MachineCanChangeConveyorSpeed, out _canChangeConveyorSpeed))
            {
                CanChangeConveyorSpeed = false;
            }
            CanChangeConveyorSpeed = _canChangeConveyorSpeed;

            if (!ScannerConfig.Read(ConfigPath.MultiFfrequency, out _multiFfrequency))
            {
                MultiFfrequency = false;
            }
            if(_multiFfrequency)
                SetSanitizeConveyorSpeedVisibility = Visibility.Visible;
            else
                SetSanitizeConveyorSpeedVisibility = Visibility.Collapsed;

            if (!ScannerConfig.Read(ConfigPath.MachineConveyorMinSpeed, out _conveyorMinSpeed))
            {
                _conveyorMinSpeed = 0.0f;
            }

            if (!ScannerConfig.Read(ConfigPath.MachineConveyorMaxSpeed, out _conveyorMaxSpeed))
            {
                _conveyorMaxSpeed = 0.5f;
            }

            if (!ScannerConfig.Read(ConfigPath.MachineConveyorStartTime, out _conveyorStartTime))
            {
                ConveyorStartTime = 3;
            }

            if (!ScannerConfig.Read(ConfigPath.MachineConveyorStopTime, out _conveyorStopTime))
            {
                ConveyorStopTime = 3;
            }

            if (!ScannerConfig.Read(ConfigPath.SanitizeConveyStartTime, out _sanitizeConveyStartTime))
            {
                SanitizeConveyStartTime = 3;
            }

            if (!ScannerConfig.Read(ConfigPath.SanitizeConveyStopTime, out _sanitizeConveyStopTime))
            {
                SanitizeConveyStopTime = 3;
            }

            if (!ScannerConfig.Read(ConfigPath.MachineConveyorSpeedChangeRatio, out _equipmentFrequency))
            {
                EquipmentFrequency = 9400;
            }

            if (!ScannerConfig.Read(ConfigPath.MachineConveyorLength, out _conveyLength))
            {
                ConveyLength = 1.5f;
            }

            if (!ScannerConfig.Read(ConfigPath.MachineEntryPESensorToXRayGenDistance, out _entryPESensorToXRayGenDistance))
            {
                EntryPESensorToXRayGenDistance = 0.3f;
            }

            if (!ScannerConfig.Read(ConfigPath.MachineExitPESensorToXRayGenDistance, out _exitPESensorToXRayGenDistance))
            {
                ExitPESensorToXRayGenDistance = 0.3f;
            }

            if (!ScannerConfig.Read(ConfigPath.MachineAutoRewind, out _autoRewind))
            {
                _autoRewind = false;
            }
            if (!ScannerConfig.Read(ConfigPath.MachineBiDirectionScan, out _directionalScan))
            {
                _directionalScan = true;
            }

            float firmware = 0f;
            if (!ScannerConfig.Read(ConfigPath.ControlFirmWare, out firmware))
            {
                firmware = 0.0f;
            }
            ControlIntervalVisibility = firmware >= 3.0 ? Visibility.Visible : Visibility.Collapsed;
            ControlIntervalVisibility = Visibility.Visible;

            if (!ScannerConfig.Read(ConfigPath.MachineDirectionExchangePESensor,out _exchangePESensor))
            {
                _exchangePESensor = false;
            }
            if (!ScannerConfig.Read(ConfigPath.MachineDirectionExchangeDetector, out _exchangeDetector))
            {
                _exchangeDetector = false;
            }
            if (!ScannerConfig.Read(ConfigPath.MachineDirectionExchangeConveyor, out _exchangeConveyor))
            {
                _exchangeConveyor = false;
            }
            if (!ScannerConfig.Read(ConfigPath.MachineDirectionExchangeXrayGen, out _exchangeXrayGen))
            {
                _exchangeXrayGen = false;
            }
        }

        private void SetSpeedFrequency()
        {
            if (_canChangeConveyorSpeed)
            {
                try
                {
                    if (ControlService.ServicePart.Open())
                    {
                        bool boolSetResult = ControlService.ServicePart.SetConveyorSpeed(_conveySpeed);
                        if (!boolSetResult)
                        {
                            Tracer.TraceInfo("Set Conveyor Speed Failed! " + "Value: " + _conveySpeed.ToString());
                        }
                        boolSetResult = ControlService.ServicePart.SetConveyorStartTime((ushort)_conveyorStartTime);
                        if (!boolSetResult)
                        {
                            Tracer.TraceInfo("Set Conveyor Start Time Failed! " + "Value: " + _conveyorStartTime.ToString());
                        }
                        boolSetResult = ControlService.ServicePart.SetConveyorStopTime((ushort)_conveyorStopTime);
                        if (!boolSetResult)
                        {
                            Tracer.TraceInfo("Set Conveyor Stop Time Failed! " + "Value: " + _conveySpeed.ToString());
                        }
                        boolSetResult = ControlService.ServicePart.SetSanitizeConveyorSpeed(_sanitizeConveySpeed);
                        if (!boolSetResult)
                        {
                            Tracer.TraceInfo("Set SanitizeConveyor Speed Failed! " + "Value: " + _sanitizeConveySpeed.ToString());
                        }
                        boolSetResult = ControlService.ServicePart.SetSanitizeConveyorStartTime((ushort)_sanitizeConveyStartTime);
                        if (!boolSetResult)
                        {
                            Tracer.TraceInfo("Set SanitizeConveyor Start Time Failed! " + "Value: " + _sanitizeConveyStartTime.ToString());
                        }
                        boolSetResult = ControlService.ServicePart.SetSanitizeConveyorStopTime((ushort)_sanitizeConveyStopTime);
                        if (!boolSetResult)
                        {
                            Tracer.TraceInfo("Set SanitizeConveyor Stop Time Failed! " + "Value: " + _sanitizeConveyStopTime.ToString());
                        }
                    }
                }
                catch (Exception e)
                {
                    Tracer.TraceException(e);
                }
                finally
                {
                    ControlService.ServicePart.Close();
                }
            }
        }
    }
}
