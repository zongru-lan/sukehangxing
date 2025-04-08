using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Common.Utilities;
using UI.XRay.Control;
using UI.XRay.ControlWorkflows;
using UI.XRay.Flows.Services;
using UI.XRay.Flows.TRSNetwork;
using UI.XRay.Parts.Keyboard;

namespace UI.XRay.Flows.Controllers
{
    public class ScannerSystemStates
    {
        /// <summary>
        /// 控制系统的当前状态
        /// </summary>
        public ScannerWorkingStates ControlStates { get; private set; }

        public ScannerWorkMode WorkMode { get; private set; }

        /// <summary>
        /// 采集系统是否存活
        /// </summary>
        public bool IsCaptureSysAlive { get; private set; }

        /// <summary>
        /// 控制系统是否存活
        /// </summary>
        public bool IsControlSysAlive
        {
            get { return ControlStates != null; }
        }

        public ScannerSystemStates(ScannerWorkingStates controlStates, bool iscaptureAlive, ScannerWorkMode workMode = ScannerWorkMode.Regular)
        {
            ControlStates = controlStates;
            IsCaptureSysAlive = iscaptureAlive;
            WorkMode = workMode;
        }
    }

    /// <summary>
    /// 系统状态监测,并在检测到关键器件异常时,尝试重新连接
    /// </summary>
    public class SystemStatesMonitor
    {
        public static SystemStatesMonitor Monitor { get; private set; }

        static SystemStatesMonitor()
        {
            Monitor = new SystemStatesMonitor();
        }

        /// <summary>
        /// 定时检查系统状态的定时器
        /// </summary>
        private Timer _checkTimer;

        private bool _started;

        /// <summary>等待进行网络状态查询的时间</summary>
        private int waitForNetStateCheckLoopTimes = 0;
        /// <summary>每几次进行网络状态查询</summary>
        private int networkStateCheckLoopTimes;

        /// <summary>
        /// 图像采集系统是否存活，默认是alive的，服务启动5s后才开始判断是否alive，防止采集系统初始化较慢时开机即出现采集异常的问题
        /// </summary>
        private bool _capturingSysAlive = true;

        /// <summary>
        /// 开机后等待几秒
        /// </summary>
        public bool CheckedCaptureSysAfterAppStarted { get; set; }

        /// <summary>
        /// 采集系统被连续检测到断开的次数。如果超过5次，将会自动重新连接
        /// </summary>
        private int _captureSysDeadTimes;

        /// <summary>
        /// 控制系统被连续检测到断开的次数。如果超过5次，将会自动重新连接
        /// </summary>
        private int _controlSysDeadTimes;

        private int _keyboardDeadTimes;

        /// <summary>
        /// 记录键盘寄了的时间，10分钟报一次错
        /// 因为有专用键盘的机型不会这么频繁的断连
        /// 没有专用键盘的机型也不至于高频报错
        /// </summary>
        private DateTime _keyboardDeadLogTime = DateTime.MinValue;
        private TimeSpan _keyboardDeadLogInterval = TimeSpan.FromMinutes(10);

        /// <summary>
        /// 是否在键盘连接断开后自动重新连接
        /// </summary>
        private bool _autoRestartKeyboard;

        /// <summary>
        /// 当前的关键的工作状态
        /// </summary>
        private ScannerWorkingStates _scannerWorkingStates;

        private ScannerWorkMode _workMode = ScannerWorkMode.Regular;

        /// <summary>
        /// 用户同步更新_scannerWorkingStates
        /// </summary>
        private object _statesSync = new object();

        /// <summary>
        /// 脚踏板出发
        /// </summary>
        bool _DetBoxLimitLast = false;
        bool _DetBoxLimitNew = false;

        /// <summary>
        /// 弱事件：定时触发，更新设备的控制状态
        /// </summary>
        public event EventHandler<ScannerSystemStates> ScannerSystemStatesTick
        {
            add { _scannerStatesWeakEvent?.Add(value); }
            remove { _scannerStatesWeakEvent?.Remove(value); }
        }

        private SmartWeakEvent<EventHandler<ScannerSystemStates>> _scannerStatesWeakEvent;

        protected SystemStatesMonitor()
        {
            try
            {
                StartService();

                if (!ScannerConfig.Read(ConfigPath.KeyboardAutoDetect, out _autoRestartKeyboard))
                {
                    _autoRestartKeyboard = true;
                }

                if (!ScannerConfig.Read(ConfigPath.NetworkStateCheckLoopTimes, out networkStateCheckLoopTimes))
                {
                    // 查询网络状态，定时器每0.5秒执行一次操作
                    networkStateCheckLoopTimes = 10;
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        /// <summary>
        /// 启动系统状态监测
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void StartService()
        {
            if (_started)
                return;

            _checkTimer = new Timer();
            _checkTimer.AutoReset = false;
            _checkTimer.Interval = 500;
            _checkTimer.Elapsed += CheckTimerOnElapsed;
            _checkTimer.Start();

            ControlService.ServicePart.ScannerWorkingStatesUpdated += ServicePartOnScannerWorkingStatesUpdated;
            ControlService.ServicePart.ConveyorDirectionChanged += ServicePartOnConveyorDirectionChanged;
            ControlService.ServicePart.XRayStateChanged += ServicePartOnXRayStateChanged;
            ControlService.ServicePart.WorkModeChanged += ServicePartOnWorkModeChanged;
            CaptureService.ServicePart.ScanlineCaptured += ServicePartOnScanlineCaptured;
            _scannerStatesWeakEvent = new SmartWeakEvent<EventHandler<ScannerSystemStates>>();
        }

        private void ServicePartOnWorkModeChanged(object sender, WorkModeChangedEventArgs workModeChangedEventArgs)
        {
            _workMode = workModeChangedEventArgs.Current;
            Tracer.TraceInfo("Current work mode is " + _workMode.ToString());
        }

        /// <summary>
        /// 输送机方向发生变化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void ServicePartOnConveyorDirectionChanged(object sender, ConveyorDirectionChangedEventArgs args)
        {
            if (args.New == ConveyorDirection.Stop)
            {
                DevicePartsWorkTimingService.ServicePart.RecordConveyorStoppedTime();
            }
            else
            {
                DevicePartsWorkTimingService.ServicePart.RecordConveyorStartedTime();
            }
        }

        /// <summary>
        /// X射线状态发生变化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void ServicePartOnXRayStateChanged(object sender, XRayStateChangedEventArgs args)
        {
            if (args.State == XRayState.Radiating)
            {
                DevicePartsWorkTimingService.ServicePart.RecordXRayOnTime();
            }
            else if (args.State == XRayState.Closed)
            {
                DevicePartsWorkTimingService.ServicePart.RecordXRayOffTime();
            }
        }

        private void ServicePartOnScannerWorkingStatesUpdated(object sender, ScannerWorkingStates scannerWorkingStates)
        {
            lock (_statesSync)
            {
                _scannerWorkingStates = scannerWorkingStates;
                if (_scannerWorkingStates != null)
                {
                    _DetBoxLimitNew = _scannerWorkingStates.LimitSwitchesStates.IsDetBoxLimitSwitchOn;
                    if (!_DetBoxLimitNew && _DetBoxLimitLast)
                    {
                        switch (_scannerWorkingStates.ConveyorMovingDirection)
                        {
                            case ConveyorDirection.MoveBackward:
                                SendKeyEvents.Press(Key.F5);
                                break;
                            case ConveyorDirection.Stop:
                                SendKeyEvents.Press(Key.F6);
                                break;
                            case ConveyorDirection.MoveForward:
                                SendKeyEvents.Press(Key.F5);
                                break;
                            default:
                                break;
                        }
                    }
                    _DetBoxLimitLast = _DetBoxLimitNew;
                }
            }
        }

        /// <summary>
        /// 根据实时采集的扫描线状态，判断图像采集系统的状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="rawScanlineDataBundle"></param>
        private void ServicePartOnScanlineCaptured(object sender, RawScanlineDataBundle rawScanlineDataBundle)
        {
            _capturingSysAlive = true;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void StopService()
        {
            try
            {
                if (_started)
                {
                    _checkTimer.Stop();
                    _checkTimer.Dispose();
                    _checkTimer = null;
                    CaptureService.ServicePart.ScanlineCaptured -= ServicePartOnScanlineCaptured;
                    ControlService.ServicePart.XRayStateChanged -= ServicePartOnXRayStateChanged;
                    ControlService.ServicePart.ConveyorDirectionChanged -= ServicePartOnConveyorDirectionChanged;
                    ControlService.ServicePart.ScannerWorkingStatesUpdated -= ServicePartOnScannerWorkingStatesUpdated;
                    ControlService.ServicePart.WorkModeChanged -= ServicePartOnWorkModeChanged;
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            _started = false;
        }

        /// <summary>
        /// 定时查询并更新系统状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="elapsedEventArgs"></param>
        private void CheckTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            lock (this)
            {
                if (_checkTimer == null)
                {
                    // 防止在程序结束时，再次触发事件导致异常
                    return;
                }
            }

            _checkTimer.Stop();

            CheckAndRestartCaptureService();

            if (_autoRestartKeyboard)
            {
                CheckAndRestartKeyboard();
            }

            ScannerWorkingStates controlState = null;
            lock (_statesSync)
            {
                controlState = _scannerWorkingStates;
                if (controlState == null)
                {
                    _controlSysDeadTimes++;
                }
                else
                {
                    _controlSysDeadTimes = 0;
                }
            }

            if (waitForNetStateCheckLoopTimes++ >= networkStateCheckLoopTimes)
            {
                waitForNetStateCheckLoopTimes = 0;
                NetCommandService.Instance.SendGetNetworkState();
            }

            if (_controlSysDeadTimes >= 10)
            {
                RestartControlService();
            }

            if (controlState != null)
            {
                // 向外抛出系统当前状态的事件
                _scannerStatesWeakEvent.Raise(this, new ScannerSystemStates(controlState, _capturingSysAlive, _workMode));

                UpdateState(controlState);
            }

            //开机后矫正时检查采集系统后开始激发采集系统的问题
            if (CheckedCaptureSysAfterAppStarted)
            {
                // 此检查周期结束后，将检查信号重置，重启定时器，待下次再检查这两个信号
                _capturingSysAlive = false;
            }

            _scannerWorkingStates = null;

            _checkTimer.Start();
        }

        private void UpdateState(ScannerWorkingStates controlState)
        {
            SystemStatus.Instance.WorkMode = (int)_workMode;
            SystemStatus.Instance.CtrlBoardStatus = _scannerWorkingStates != null;
            SystemStatus.Instance.GCUConnection = _capturingSysAlive;

            if (controlState != null)
            {
                SystemStatus.Instance.CtrlBoardTemperature = controlState.ScannerTemperature;

                SystemStatus.Instance.Generator1Online = controlState.IsXRayGen1Alive;
                SystemStatus.Instance.Generator2Online = controlState.IsXRayGen2Alive;
                SystemStatus.Instance.Generator1Scanning = controlState.IsXRayGen1Radiating;
                SystemStatus.Instance.Generator2Scanning = controlState.IsXRayGen2Radiating;
                SystemStatus.Instance.EmergencyStop = controlState.IsEmgcySwitchOn;
            }
        }


        [Conditional("RELEASE")]
        private void CheckAndRestartKeyboard()
        {
            try
            {
                if (!ScannerKeyboardPart.Keyboard.IsAlive)
                {
                    _keyboardDeadTimes++;
                }
                else
                {
                    _keyboardDeadTimes = 0;
                }

                if (_keyboardDeadTimes >= 6)
                {
                    _keyboardDeadTimes = 0;

                    // 避免没有专用键盘的机型过于频繁的报错
                    if (DateTime.Now - _keyboardDeadLogTime > _keyboardDeadLogInterval)
                    {
                        _keyboardDeadLogTime = DateTime.Now;
                        Tracer.TraceWarning("Keyboard Service is dead, try to restart now.");
                    }
                    ScannerKeyboardPart.Keyboard.Open();
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        /// <summary>
        /// 检查采集系统的连接状态，并且当判断采集系统已经断开时，重新连接
        /// </summary>
        private void CheckAndRestartCaptureService()
        {
            if (!_capturingSysAlive)
            {
                _captureSysDeadTimes++;
            }
            else
            {
                _captureSysDeadTimes = 0;
            }

            if (_captureSysDeadTimes >= 10)
            {
                RestartCaptureService();
            }
        }

        /// <summary>
        /// 重新启动控制服务
        /// </summary>
        private void RestartControlService()
        {
            _controlSysDeadTimes = 0;
            try
            {
                Tracer.TraceWarning("Control Service is dead, try to restart now.");
                ControlService.ServicePart.Close();
                ControlService.ServicePart.Open();
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        /// <summary>
        /// 重新启动图像采集服务
        /// </summary>
        private void RestartCaptureService()
        {
            _captureSysDeadTimes = 0;

            try
            {
                Tracer.TraceWarning("Capture Service is dead, try to restart now.");
                CaptureService.ServicePart.StopCapture();
                CaptureService.ServicePart.Close();

                Tracer.TraceInfo("Capture Service is Closed now.");

                if (CaptureService.ServicePart.Open())
                {
                    CaptureService.ServicePart.StartCapture();

                    Tracer.TraceInfo("Capture Service is restarted now.");
                }
                else
                {
                    Tracer.TraceInfo("Capture Service can not be opened.");
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        /// <summary>
        /// 异步获取当前最新状态
        /// </summary>
        /// <returns>如果返回值为null，则表示控制系统连接失败</returns>
        public Task<ScannerWorkingStates> GetWorkingStatesAsync()
        {
            return Task.Run(() =>
            {
                lock (this)
                {
                    ControlService.ServicePart.GetWorkingStates(out _scannerWorkingStates);
                    return _scannerWorkingStates;
                }
            });
        }

        /// <summary>
        /// 异步获取当前最新状态
        /// </summary>
        /// <returns>如果返回值为null，则表示控制系统连接失败</returns>
        public ScannerWorkingStates GetWorkingStates()
        {
            lock (this)
            {
                ControlService.ServicePart.GetWorkingStates(out _scannerWorkingStates);
                return _scannerWorkingStates;
            }
        }

        /// <summary>
        /// 查询并更新系统状态
        /// </summary>
        //private void CheckAndUpdateSystemStates()
        //{
        //    lock (this)
        //    {
        //        try
        //        {
        //            ControlService.ServicePart.GetWorkingStates(out _scannerWorkingStates);
        //            _scannerStatesWeakEvent.Raise(this, new ScannerSystemStates(_scannerWorkingStates, _capturingSysAlive));
        //        }
        //        catch (Exception exception)
        //        {
        //            Tracer.TraceException(exception);
        //        }
        //    }
        //}
    }
}
