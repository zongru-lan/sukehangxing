using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using UI.Common.Tracers;
using UI.XRay.Control;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Services;
using UI.XRay.Gui.Framework;
using UI.XRay.Parts.Keyboard;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;

namespace UI.XRay.Security.Scanner.ViewModel
{
    public class XRayGenWarmupWindowViewModel : ViewModelBase
    {
        #region commands
        public RelayCommand<KeyEventArgs> PreviewKeyDownEventCommand { get; set; }

        public RelayCommand ClosedEventCommand { get; private set; }

        #endregion commands

        /// <summary>
        /// 预热流程完成的百分比
        /// </summary>
        public double PercentDone
        {
            get { return _percentDone; }
            set { _percentDone = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 当前射线源的高压
        /// </summary>
        public double Voltage
        {
            get { return _voltage; }
            set { _voltage = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 当前射线源的束流
        /// </summary>
        public double Current
        {
            get { return _current; }
            set { _current = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 剩余时间
        /// </summary>
        public double RemainingSeconds
        {
            get { return _remainingSeconds; }
            set { _remainingSeconds = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 控制系统异常字符串是否可见
        /// 当控制系统出现异常时，显示，并且暂停预热
        /// </summary>
        public Visibility ControlSysErrorStringVisibility
        {
            get { return _controlSysErrorStringVisibility; }
            set { _controlSysErrorStringVisibility = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 1号射线源连接中断的错误提示文字可见性
        /// </summary>
        public Visibility XrayGen1DeadStringVisibility
        {
            get { return _xrayGen1DeadStringVisibility; }
            set { _xrayGen1DeadStringVisibility = value; RaisePropertyChanged(); }
        }

        private Visibility _interlockOffStringVisibility = Visibility.Collapsed;

        public Visibility InterlockOffStringVisibility
        {
            get { return _interlockOffStringVisibility; }
            set
            {
                _interlockOffStringVisibility = value;
                RaisePropertyChanged();
            }
        }


        private Visibility _emgcSwitchOffStringVisibility = Visibility.Collapsed;

        public Visibility EmgcSwitchOffStringVisibility
        {
            get { return _emgcSwitchOffStringVisibility; }
            set
            {
                _emgcSwitchOffStringVisibility = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 预热总时长
        /// </summary>
        private readonly TimeSpan _duration;

        /// <summary>
        /// 已经预热的时长
        /// </summary>
        private TimeSpan _timeUsed;

        /// <summary>
        /// 定时器，周期为1秒，定时检测
        /// </summary>
        private DispatcherTimer _timer;

        private double _percentDone;

        private double _voltage;

        private double _current;

        private double _remainingSeconds;

        /// <summary>
        /// 当前是否有错误发生，如果有错误发生，则暂停计时，并在错误结束后继续
        /// </summary>
        private bool _hasError;

        /// <summary>
        /// 无法访问控制系统的错误提示文字可见性
        /// </summary>
        private Visibility _controlSysErrorStringVisibility = Visibility.Collapsed;

        /// <summary>
        /// 1号射线源连接中断的错误提示文字可见性
        /// </summary>
        private Visibility _xrayGen1DeadStringVisibility = Visibility.Collapsed;

        /// <summary>
        /// 射线源类型
        /// </summary>
        XRayGeneratorType xrayGenType = XRayGeneratorType.XRayGen_KWD;
        /// <summary>
        /// 预设电压电流
        /// </summary>
        float xrayKV;
        float xrayMA;
        float xrayKV2;
        float xrayMA2;
        int genCount = 1;

        private double _stepIntervalTime;
        private float[] _stepVoltageArray;
        private float[] _stepCurrentArray;
        private bool[] _stepSettingFlag = new bool[10] { false, false, false, false, false, false, false, false, false, false };

        private XRayGenSettingController _settingController;

        public XRayGenWarmupWindowViewModel()
        {
            _settingController = new XRayGenSettingController();
            ControlService.ServicePart.DriveConveyor(ConveyorDirection.Stop);
            PreviewKeyDownEventCommand = new RelayCommand<KeyEventArgs>(PreviewKeyDownEventCommandExecute);
            ClosedEventCommand = new RelayCommand(ClosedEventCommandExecute);

            if (IsInDesignMode)
            {
                PercentDone = 30;
                Voltage = 150.0111;
                Current = 0.45;
                RemainingSeconds = 100;
                XrayGen1DeadStringVisibility = Visibility.Visible;
                ControlSysErrorStringVisibility = Visibility.Visible;
            }
            else
            {
                try
                {
                    LoadXGenSetting();
                    var duration = XRayGenWarmupHelper.GetWarmupDuration();
                    _duration = duration == null ? TimeSpan.FromSeconds(30) : duration.Value;

                    RemainingSeconds = _duration.TotalSeconds;
                    _stepIntervalTime = RemainingSeconds / 10;

                    SystemStatesMonitor.Monitor.ScannerSystemStatesTick += MonitorOnScannerSystemStatesTick;                    

                    SetKvMa(_stepVoltageArray[0], _stepCurrentArray[0]);
                    _stepSettingFlag[0] = true;

                    _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                    _timer.Tick += TimerOnTick;
                    _timer.Start();                    


                    // 开始预热，则发射X射线
                    RadiateXRay(true);
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception);
                }
            }
        }

        private void LoadXGenSetting()
        {
            try
            {
                if (!ScannerConfig.Read(ConfigPath.XRayGenType, out xrayGenType))
                {
                    xrayGenType = XRayGeneratorType.XRayGen_KWD;
                }
                if (!ScannerConfig.Read(ConfigPath.XRayGenKV, out xrayKV))
                {
                    xrayKV = 160f;
                }
                if (!ScannerConfig.Read(ConfigPath.XRayGenMA, out xrayMA))
                {
                    xrayMA = 0.5f;
                }
                if (!ScannerConfig.Read(ConfigPath.XRayGenKV2, out xrayKV2))
                {
                    xrayKV2 = 160f;
                }
                if (!ScannerConfig.Read(ConfigPath.XRayGenMA2, out xrayMA2))
                {
                    xrayMA2 = 0.5f;
                }
                if (!ScannerConfig.Read(ConfigPath.XRayGenCount, out genCount))
                {
                    genCount = 1;
                }
                if (genCount == 1)
                {
                    ConfigHelper.XRayGen1Voltage = xrayKV;
                    ConfigHelper.XRayGen1Current = xrayMA;
                    ConfigHelper.XRayGen2Voltage = 0;
                    ConfigHelper.XRayGen2Current = 0;
                }
                else
                {
                    ConfigHelper.XRayGen1Voltage = xrayKV;
                    ConfigHelper.XRayGen1Current = xrayMA;
                    ConfigHelper.XRayGen2Voltage = xrayKV2;
                    ConfigHelper.XRayGen2Current = xrayMA2;
                }


                string kv, ma;
                switch (xrayGenType)
                {
                    case XRayGeneratorType.XRayGen_KWD:
                        ScannerConfig.Read(ConfigPath.WarmupKvKWD, out kv);
                        ScannerConfig.Read(ConfigPath.WarmupMaKWD, out ma);
                        break;
                    case XRayGeneratorType.XRayGen_SAXG200:
                        ScannerConfig.Read(ConfigPath.WarmupKvSAXG, out kv);
                        ScannerConfig.Read(ConfigPath.WarmupMaSAXG, out ma);
                        break;
                    case XRayGeneratorType.XRayGen_SAXG110:
                        ScannerConfig.Read(ConfigPath.WarmupKvSAXG, out kv);
                        ScannerConfig.Read(ConfigPath.WarmupMaSAXG, out ma);
                        break;
                    case XRayGeneratorType.XRayGen_Spellman160:
                        ScannerConfig.Read(ConfigPath.WarmupKvSpellman160, out kv);
                        ScannerConfig.Read(ConfigPath.WarmupMaSpellman160, out ma);
                        break;
                    case XRayGeneratorType.XRayGen_VJ160:
                    case XRayGeneratorType.XRayGen_VJ160_A:
                        ScannerConfig.Read(ConfigPath.WarmupKvVJ160, out kv);
                        ScannerConfig.Read(ConfigPath.WarmupMaVJ160, out ma);
                        break;
                    case XRayGeneratorType.XRayGen_VJ200:
                        ScannerConfig.Read(ConfigPath.WarmupKvVJ200, out kv);
                        ScannerConfig.Read(ConfigPath.WarmupMaVJ200, out ma);
                        break;
                    case XRayGeneratorType.XRayGen_KWA:
                    case XRayGeneratorType.XRayGen_Spellman80:
                    default:
                        ScannerConfig.Read(ConfigPath.WarmupKvKWD, out kv);
                        ScannerConfig.Read(ConfigPath.WarmupMaKWD, out ma);
                        break;
                }
                if (kv == null || string.IsNullOrEmpty(kv.Trim()) || kv.Split(',').Count() != 10)
                {
                    kv = "140, 140, 145, 145, 150, 150, 155, 155, 160, 160 ";
                }
                if (ma == null || string.IsNullOrEmpty(ma.Trim()) || ma.Split(',').Count() != 10)
                {
                    ma = "0.5, 0.55, 0.6, 0.65, 0.7, 0.75, 0.8, 0.85, 0.9, 0.95";
                }

                string[] tempKv = kv.Split(',');
                _stepVoltageArray = Array.ConvertAll<string, float>(tempKv, s => float.Parse(s));
                string[] tempMa = ma.Split(',');
                _stepCurrentArray = Array.ConvertAll<string, float>(tempMa, s => float.Parse(s));
            }
            catch (Exception)
            {
                _stepVoltageArray = new float[] { 140, 140, 145, 145, 150, 150, 155, 155, 160, 160 };
                _stepCurrentArray = new float[] { 0.5f, 0.55f, 0.6f, 0.65f, 0.7f, 0.75f, 0.8f, 0.85f, 0.9f, 0.95f };
            }
            
        }

        /// <summary>
        /// 系统状态更新：根据控制板和射线源的最新状态，更新界面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="scannerSystemStates"></param>
        private void MonitorOnScannerSystemStatesTick(object sender, ScannerSystemStates scannerSystemStates)
        {

            if (scannerSystemStates != null)
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _hasError = false;

                    if (!scannerSystemStates.IsControlSysAlive)
                    {
                        // 控制板连接断开或者急停开关断开，均提示控制系统异常
                        ControlSysErrorStringVisibility = Visibility.Visible;
                        _hasError = true;
                    }
                    else
                    {
                        ControlSysErrorStringVisibility = Visibility.Collapsed;

                        if (!scannerSystemStates.ControlStates.IsEmgcySwitchOn)
                        {
                            EmgcSwitchOffStringVisibility = Visibility.Visible;
                            _hasError = true;
                        }
                        else if (!scannerSystemStates.ControlStates.IsXRayGen1Alive)
                        {
                            XrayGen1DeadStringVisibility = Visibility.Visible;
                            _hasError = true;
                        }
                        else if (!scannerSystemStates.ControlStates.LimitSwitchesStates.IsXRayGenLimitSwitchOn ||
                                 !scannerSystemStates.ControlStates.LimitSwitchesStates.IsDetBoxLimitSwitchOn)
                        {
                            InterlockOffStringVisibility = Visibility.Visible;
                            _hasError = true;
                        }
                        else
                        {
                            XrayGen1DeadStringVisibility = Visibility.Collapsed;
                            EmgcSwitchOffStringVisibility = Visibility.Collapsed;
                            XrayGen1DeadStringVisibility = Visibility.Collapsed;

                            // 进一步判断射线是否发射，如果未发射，则发射X射线
                            if (!scannerSystemStates.ControlStates.IsXRayGen1Radiating)
                            {
                                RadiateXRay(true);
                            }
                        }
                    }
                });
            }
        }

        /// <summary>
        /// 预热计时器，每隔1秒响应一次
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void TimerOnTick(object sender, EventArgs eventArgs)
        {
            // 如果当前发生了错误，则不计入总时间。
            if (_hasError)
                return;

            _timeUsed += _timer.Interval;

            PercentDone = _timeUsed.TotalMilliseconds / _duration.TotalMilliseconds * 100;
            RemainingSeconds = _duration.TotalSeconds - _timeUsed.TotalSeconds;

            UpdateXRayGenStates();
            int index = (int)Math.Floor(PercentDone / 10);
            if (index < 10)
            {
                if (!_stepSettingFlag[index])
                {
                    SetKvMa(_stepVoltageArray[index], _stepCurrentArray[index]);
                    _stepSettingFlag[index] = true;
                }
            }

            if (_timeUsed >= _duration)
            {
                RadiateXRay(false);
                _timer.Stop();
                _timer = null;
                CloseWindow();

                // 预热正常结束，则认定预热成功，记录此次预热时间
                //XRayGenWarmupHelper.SaveWarmupRecord();

                //改预热时间为关机时间
                XRayGenWarmupHelper.SaveWarmupResult(true);
            }
            else
            {
                XRayGenWarmupHelper.SaveWarmupResult(false);
            }
        }

        /// <summary>
        /// 获取当前射线源的最新状态
        /// </summary>
        private void UpdateXRayGenStates()
        {
            XRayGeneratorWorkingStates states;
            if (ControlService.ServicePart.GetXRayGenWorkingStates(out states))
            {
                if (states != null)
                {
                    Voltage = states.XRayGen1kV;
                    Current = states.XRayGen1mA;
                }
            }
        }

        private void PreviewKeyDownEventCommandExecute(KeyEventArgs args)
        {
            // 当用户按下Esc或F3按键时，终止预热过程
            if (args.Key == ScannerKeyboardPart.Keyboard.Esc || args.Key == ScannerKeyboardPart.Keyboard.F3)
            {
                CloseWindow();
            }
        }

        /// <summary>
        /// 打开或关闭X射线
        /// </summary>
        /// <param name="on"></param>
        private void RadiateXRay(bool on)
        {
            try
            {
                ControlService.ServicePart.RadiateXRay(on);
                System.Threading.Thread.Sleep(100);
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        void SetDefaultKvMa()
        {
            _settingController.ChangeXRayGenSettings(xrayKV, xrayMA, XRayGeneratorIndex.XRayGenerator1);

            if (genCount > 1)
            {
                System.Threading.Thread.Sleep(100);
                _settingController.ChangeXRayGenSettings(xrayKV2, xrayMA2, XRayGeneratorIndex.XRayGenerator2);
            }
        }
        void SetKvMa(float kv, float ma)
        {
            _settingController.ChangeXRayGenSettings(kv, ma, XRayGeneratorIndex.XRayGenerator1);
            if (genCount > 1)
            {
                _settingController.ChangeXRayGenSettings(kv, ma, XRayGeneratorIndex.XRayGenerator2);
            }
        }

        private void CloseWindow()
        {
            SetDefaultKvMa();
            new OperationRecordService().AddRecord(new OperationRecord()
            {
                AccountId = LoginAccountManager.Service.CurrentAccount != null ? LoginAccountManager.Service.CurrentAccount.AccountId : "",
                OperateUI = OperationUI.XRayGenWarmup,
                OperateTime = DateTime.Now,
                OperateObject = "XRayGenWarmup",
                OperateCommand = OperationCommand.Open,
                OperateContent = _timer == null ? "True" : String.Format("{0}/{1}", (int)_timeUsed.TotalSeconds, (int)_duration.TotalSeconds),
            });

            MessengerInstance.Send(new CloseWindowMessage("XRayGenWarmupWindow"));
        }

        /// <summary>
        /// 窗口已经关闭：清理资源等
        /// </summary>
        private void ClosedEventCommandExecute()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= TimerOnTick;
            }

            // 窗口关闭时，必须注销此事件，防止射线因为弱事件暂时未自动清除而导致射线被再次打开
            SystemStatesMonitor.Monitor.ScannerSystemStatesTick -= MonitorOnScannerSystemStatesTick;

            RadiateXRay(false);   
            SetDefaultKvMa();         
        }

        public override void Cleanup()
        {
            base.Cleanup();
        }
    }
}
