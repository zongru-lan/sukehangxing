using System;
using System.Collections.Generic;
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
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Services;
using UI.XRay.Flows.Services.DataProcess;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Scanner.ViewModel
{
    /// <summary>
    /// 手动校正本底、满度窗口的视图模型
    /// </summary>
    public class CalibrateWindowViewModel : ViewModelBase
    {
        enum CalibrateStep
        {
            TurningOffXRay,
            CheckCaptureSys,
            CalibratingGround,
            TurningOnXRay,
            CalibratingAir,
            CalibrateSuccess
        }

        #region bindable properties

        private Visibility _calibrateGroundTextVisibility = Visibility.Collapsed;

        private Visibility _groundCalibratedOkTextVisibility = Visibility.Collapsed;

        private Visibility _groundCalibratedFailedTextVisibility = Visibility.Collapsed;

        private Visibility _turnOnXRayTextVisibility = Visibility.Collapsed;

        private Visibility _calibrateAirTextVisibility = Visibility.Collapsed;

        private Visibility _airCalibratedOkTextVisibility = Visibility.Collapsed;

        private Visibility _airCalibratedFailedTextVisibility = Visibility.Collapsed;

        private Visibility _closeButtonVisibility = Visibility.Collapsed;

        private Visibility _windowClosingTextVisibility = Visibility.Collapsed;

        private Visibility _checkCaptureSysTextVisibility = Visibility.Collapsed;

        private Visibility _checkCaptureSysFailedTextVisibility = Visibility.Collapsed;

        private Visibility _checkCaptureSysNormalTextVisibility = Visibility.Collapsed;

        private readonly string _checkCaptureSysDefaultStr;
        private string _checkCaptureSysStr = "Check Capture System";

        public string CheckCaptureSysStr
        {
            get { return _checkCaptureSysStr; }
            set
            {
                _checkCaptureSysStr = value;
                RaisePropertyChanged();
            }
        }

        public Visibility CheckCaptureSysFailedTextVisibility
        {
            get { return _checkCaptureSysFailedTextVisibility; }
            set
            {
                _checkCaptureSysFailedTextVisibility = value;
                RaisePropertyChanged();
            }
        }

        public Visibility CheckCaptureSysNormalTextVisibility
        {
            get { return _checkCaptureSysNormalTextVisibility; }
            set
            {
                _checkCaptureSysNormalTextVisibility = value;
                RaisePropertyChanged();
            }
        }

        public Visibility CheckCaptureSysTextVisibility
        {
            get { return _checkCaptureSysTextVisibility; }
            set
            {
                _checkCaptureSysTextVisibility = value;
                RaisePropertyChanged();
            }
        }

        public Visibility WindowClosingTextVisibility
        {
            get { return _windowClosingTextVisibility; }
            set
            {
                _windowClosingTextVisibility = value;
                RaisePropertyChanged();
            }
        }

        public Visibility CalibrateGroundTextVisibility
        {
            get { return _calibrateGroundTextVisibility; }
            set
            {
                _calibrateGroundTextVisibility = value;
                RaisePropertyChanged();
            }
        }

        public Visibility GroundCalibratedOkTextVisibility
        {
            get { return _groundCalibratedOkTextVisibility; }
            set
            {
                _groundCalibratedOkTextVisibility = value;
                RaisePropertyChanged();
            }
        }

        public Visibility GroundCalibratedFailedTextVisibility
        {
            get { return _groundCalibratedFailedTextVisibility; }
            set
            {
                _groundCalibratedFailedTextVisibility = value;
                RaisePropertyChanged();
            }
        }

        public Visibility TurnOnXRayTextVisibility
        {
            get { return _turnOnXRayTextVisibility; }
            set
            {
                _turnOnXRayTextVisibility = value;
                RaisePropertyChanged();
            }
        }

        public Visibility AirCalibratedOkTextVisibility
        {
            get { return _airCalibratedOkTextVisibility; }
            set
            {
                _airCalibratedOkTextVisibility = value;
                RaisePropertyChanged();
            }
        }

        public Visibility AirCalibratedFailedTextVisibility
        {
            get { return _airCalibratedFailedTextVisibility; }
            set
            {
                _airCalibratedFailedTextVisibility = value;
                RaisePropertyChanged();
            }
        }

        public Visibility CalibrateAirTextVisibility
        {
            get { return _calibrateAirTextVisibility; }
            set
            {
                _calibrateAirTextVisibility = value;
                RaisePropertyChanged();
            }
        }

        public Visibility CloseButtonVisibility
        {
            get { return _closeButtonVisibility; }
            set
            {
                _closeButtonVisibility = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        public RelayCommand<KeyEventArgs> PreviewKeyDownEventCommand { get; set; }
        public RelayCommand ClosedEventCommand { get; set; }

        private CalibrateStep _calibrateStep = CalibrateStep.TurningOffXRay;

        private DispatcherTimer _timer;

        bool checkCaptureSysResult = false;

        private bool _groundCalibrated;

        private bool _airCalibrated;

        private bool _xrayOn;

        private bool _receivedCaptureScanlines = false;

        /// <summary>
        /// 默认5s
        /// </summary>
        private readonly int _checkCaptureSysTimeSpan;

        private readonly int calibrateWaitingXRayTimeSpan_s;

        /// <summary>
        /// 窗口是否已经关闭。由于更新本底及更新满度使用的是异步方法，窗口随时可能被关闭
        /// 在更新过程中，需要及时设置标志位，避免多线程导致状态竞争
        /// </summary>
        private bool _isClosed;

        public CalibrateWindowViewModel()
        {
            Tracer.TraceEnterFunc("ManualCalibrateWindowViewModel");

            int checkCaptureSysTimeSpan;
            if (!ScannerConfig.Read(ConfigPath.CheckCaptureSysTimeSpan, out checkCaptureSysTimeSpan))
            {
                checkCaptureSysTimeSpan = 6;
            }
            if (!ScannerConfig.Read(ConfigPath.CalibrateWaitingXRayTimeSpan, out calibrateWaitingXRayTimeSpan_s))
            {
                calibrateWaitingXRayTimeSpan_s = 3;
            }
            //转换为ms
            _checkCaptureSysTimeSpan = checkCaptureSysTimeSpan * 1000;

            _checkCaptureSysDefaultStr = LanguageResourceExtension.FindTranslation("CalibrationWindow",
                "Checking Imaging System");

            CheckCaptureSysStr = _checkCaptureSysDefaultStr;

            PreviewKeyDownEventCommand = new RelayCommand<KeyEventArgs>(PreviewKeyDownEventCommandExecute);
            ClosedEventCommand = new RelayCommand(ClosedEventCommandExecute);
            try
            {
                ControlService.ServicePart.RadiateXRay(false);
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            _calibrateStep = CalibrateStep.TurningOffXRay;

            _timer = new DispatcherTimer(DispatcherPriority.Normal, Application.Current.Dispatcher);
            _timer.Tick += TimerOnTick;
            _timer.Interval = TimeSpan.FromSeconds(3);
            _timer.Start();

            CaptureService.ServicePart.ScanlineCaptured += ServicePart_ScanlineCaptured;

            // 启动手动标定服务
            ManualCalibrationService.Service.StartService();

            Tracer.TraceExitFunc("ManualCalibrateWindowViewModel");
        }

        private void ServicePart_ScanlineCaptured(object sender, RawScanlineDataBundle e)
        {
            _receivedCaptureScanlines = true;
        }

        /// <summary>
        /// 窗口已经关闭
        /// </summary>
        private void ClosedEventCommandExecute()
        {
            _isClosed = true;

            _timer.Stop();
            _timer.Tick -= TimerOnTick;

            CaptureService.ServicePart.ScanlineCaptured -= ServicePart_ScanlineCaptured;
            LoadConveyorSetting();

            try
            {
                // 确保射线被关闭
                ControlService.ServicePart.RadiateXRay(false);
                ManualCalibrationService.Service.StopService();
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        /// <summary>
        /// 处理按键消息，用户按下F3时关闭窗口
        /// </summary>
        /// <param name="args"></param>
        private void PreviewKeyDownEventCommandExecute(KeyEventArgs args)
        {
            if (args.Key == Key.F3)
            {
                args.Handled = true;
                CloseWindow();
            }
        }

        private void CloseWindow()
        {
            LoadConveyorSetting();
            List<OperationRecord> list = new List<OperationRecord>();
            list.Add(new OperationRecord()
            {
                AccountId = LoginAccountManager.Service.CurrentAccount != null ? LoginAccountManager.Service.CurrentAccount.AccountId : "",
                OperateUI = OperationUI.Calibrate,
                OperateTime = DateTime.Now,
                OperateObject = "CheckCaptureSys",
                OperateCommand = OperationCommand.Open,
                OperateContent = checkCaptureSysResult ? "Success" : "Fail",
            });

            list.Add(new OperationRecord()
            {
                AccountId = LoginAccountManager.Service.CurrentAccount != null ? LoginAccountManager.Service.CurrentAccount.AccountId : "",
                OperateUI = OperationUI.Calibrate,
                OperateTime = DateTime.Now,
                OperateObject = "CalibrateGround",
                OperateCommand = OperationCommand.Setting,
                OperateContent = _groundCalibrated ? "Success" : "Fail",
            });

            list.Add(new OperationRecord()
            {
                AccountId = LoginAccountManager.Service.CurrentAccount != null ? LoginAccountManager.Service.CurrentAccount.AccountId : "",
                OperateUI = OperationUI.Calibrate,
                OperateTime = DateTime.Now,
                OperateObject = "CalibrateAir",
                OperateCommand = OperationCommand.Setting,
                OperateContent = _airCalibrated ? "Success" : "Fail",
            });
            list.Add(new OperationRecord()
            {
                AccountId = LoginAccountManager.Service.CurrentAccount != null ? LoginAccountManager.Service.CurrentAccount.AccountId : "",
                OperateUI = OperationUI.Calibrate,
                OperateTime = DateTime.Now,
                OperateObject = "Calibrate",
                OperateCommand = OperationCommand.Setting,
                OperateContent = _calibrateStep == CalibrateStep.CalibrateSuccess ? "Success" : "Fail",
            });
            new OperationRecordService().AddRecordRange(list);
            this.MessengerInstance.Send(new CloseWindowMessage("CalibrateWindow"));
        }

        /// <summary>
        /// 开始异步更新本底
        /// </summary>
        private async void CalibrateGroundAsync()
        {
            _calibrateStep = CalibrateStep.CalibratingGround;
            CalibrateGroundTextVisibility = Visibility.Visible;

            ControlService.ServicePart.RadiateXRay(false);
            System.Threading.Thread.Sleep(1000);

            // 开始更新本底
            var task = ManualCalibrationService.Service.CalibrateGroundAsync();
            if (task != null)
            {
                var groundResult = await task;

                // 异步方法完成后，如果窗口已经关闭，则不再继续
                if (_isClosed)
                {
                    return;
                }

                // 本底更新成功
                if (groundResult != null)
                {
                    if (groundResult.ResultCode == CalibrationResultCode.Success)
                    {
                        _groundCalibrated = true;
                        SystemStateService.Service.IsPassGroundCalibration = true;
                        GroundCalibratedOkTextVisibility = Visibility.Visible;
                    }
                    else
                    {
                        GroundCalibratedFailedTextVisibility = Visibility.Visible;
                    }
                }
                else
                {
                    GroundCalibratedFailedTextVisibility = Visibility.Visible;
                }
            }

            TurnOnXRay();
        }

        /// <summary>
        /// 开始异步更新满度
        /// </summary>
        private async void CalibrateAirAsync()
        {
            _calibrateStep = CalibrateStep.CalibratingAir;
            CalibrateAirTextVisibility = Visibility.Visible;

            var task = ManualCalibrationService.Service.CalibrateAirAsync();
            if (task != null)
            {
                var air = await task;

                // 满度更新结束后，关闭X射线
                ControlService.ServicePart.RadiateXRay(false);

                // 异步方法完成后，如果窗口已经关闭，则不再继续
                if (_isClosed)
                {
                    return;
                }

                // 更新成功
                if (air != null && air.ResultCode == CalibrationResultCode.Success)
                {
                    _airCalibrated = true;
                    SystemStateService.Service.IsPassAirCalibration = true;
                }

                if (_airCalibrated)
                {
                    AirCalibratedOkTextVisibility = Visibility.Visible;
                }
                else
                {
                    AirCalibratedFailedTextVisibility = Visibility.Visible;
                }

                // 如果本底和满度都更新成功，则定时自动关闭窗口
                if (_airCalibrated && _groundCalibrated)
                {
                    _calibrateStep = CalibrateStep.CalibrateSuccess;
                    WindowClosingTextVisibility = Visibility.Visible;
                    _timer.Interval = TimeSpan.FromSeconds(3);
                    _timer.Start();
                }
                else
                {
                    // 更新失败，显示关闭窗口的按钮
                    CloseButtonVisibility = Visibility.Visible;
                }
            }
        }

        //private CancellationTokenSource _cancellSource = new CancellationTokenSource();

        private async void CheckCaptureSysAsync()
        {
            _calibrateStep = CalibrateStep.CheckCaptureSys;

            CheckCaptureSysTextVisibility = Visibility.Visible;

            try
            {
                //从窗口出现到执行到此处是3s，此处延时3.5s，如果数据到来则不执行wait
                //await Task.Delay(3500, _cancellSource.Token);
                checkCaptureSysResult = await WaitScanlineAsync();
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }

            SystemStatesMonitor.Monitor.CheckedCaptureSysAfterAppStarted = true;

            if (!checkCaptureSysResult)
            {
                CheckCaptureSysFailedTextVisibility = Visibility.Visible;
            }
            else
            {
                CheckCaptureSysNormalTextVisibility = Visibility.Visible;
            }

            // 异步方法完成后，如果窗口已经关闭，则不再继续
            if (_isClosed)
            {
                return;
            }

            // 再次启动定时器，并在定时器到后，开始更新本底。大概等待0.5s左右
            _timer.Interval = TimeSpan.FromSeconds(3);
            _timer.Start();
        }

        private async Task<bool> WaitScanlineAsync()
        {
            int waitTime = 0;
            int count = 0;

            while (!_receivedCaptureScanlines)
            {
                //每次等待200ms
                await Task.Delay(500);
                waitTime += 500;

                if (waitTime >= _checkCaptureSysTimeSpan)
                {
                    return false;
                }
                count++;
                CheckCaptureSysStr = CheckCaptureSysStr + ".";
                if (count > 3)
                {
                    count = 0;
                    CheckCaptureSysStr = _checkCaptureSysDefaultStr;
                }
            }
            return true;
        }


        /// <summary>
        /// 开启射线，启动定时器，并在定时到后更新满度
        /// </summary>
        private void TurnOnXRay()
        {
            _calibrateStep = CalibrateStep.TurningOnXRay;
            TurnOnXRayTextVisibility = Visibility.Visible;

            try
            {
                ControlService.ServicePart.RadiateXRay(true);
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            //射线启动后一定时间后再做空气值校准，防止射线不稳定
            //System.Threading.Thread.Sleep(3000);

            // 再次启动定时器，并在定时器到后，开始更新满度
            _timer.Interval = TimeSpan.FromSeconds(calibrateWaitingXRayTimeSpan_s);
            _timer.Start();
        }

        private void TimerOnTick(object sender, EventArgs eventArgs)
        {
            _timer.Stop();

            switch (_calibrateStep)
            {
                // 射线已经关闭，开始异步更新本底
                case CalibrateStep.TurningOffXRay:
                    CheckCaptureSysAsync();
                    break;
                case CalibrateStep.CheckCaptureSys:
                    CalibrateGroundAsync();
                    break;
                // 射线已经打开，开始异步更新满度
                case CalibrateStep.TurningOnXRay:
                    CalibrateAirAsync();
                    break;

                // 更新成功，关闭窗口
                case CalibrateStep.CalibrateSuccess:
                    CloseWindow();
                    break;
            }
        }

        /// <summary>
        /// 设置变频器，已在configer中设置，此处设置会出现控制板短暂断开连接的情况
        /// </summary>
        private void LoadConveyorSetting()
        {
            try
            {
                //SetSpeedFrequency();
            }
            catch (Exception e)
            {
                Tracer.TraceInfo("Operate Converyor Failed! " + e.ToString());
            }
        }
        private void SetSpeedFrequency()
        {
            bool _canChangeConveyorSpeed = false;
            if (!ScannerConfig.Read(ConfigPath.MachineCanChangeConveyorSpeed, out _canChangeConveyorSpeed))
            {
                _canChangeConveyorSpeed = false;
            }

            if (_canChangeConveyorSpeed)
            {
                float _conveyorSpeed = 0.2f;
                if (!ScannerConfig.Read(ConfigPath.MachineConveyorSpeed, out _conveyorSpeed))
                {
                    _conveyorSpeed = 0.2f;
                }

                bool boolSetResult = ControlService.ServicePart.SetConveyorSpeed(_conveyorSpeed);

                if (!boolSetResult)
                {
                    Tracer.TraceInfo("Set Converyor Speed Failed! " + "Value: " + _conveyorSpeed.ToString());
                }

                //配置电机启动加速时间
                ushort _conveyorStartTime = 3;
                if (!ScannerConfig.Read(ConfigPath.MachineConveyorStartTime, out _conveyorStartTime))
                {
                    _conveyorStartTime = 3;
                }
                boolSetResult = ControlService.ServicePart.SetConveyorStartTime((ushort)_conveyorStartTime);
                if (!boolSetResult)
                {
                    Tracer.TraceInfo("Set Converyor Start Time Failed! " + "Value: " + _conveyorStartTime.ToString());
                }

                //配置电机停止减速时间
                ushort _conveyorStopTime = 3;
                if (!ScannerConfig.Read(ConfigPath.MachineConveyorStopTime, out _conveyorStopTime))
                {
                    _conveyorStopTime = 3;
                }
                boolSetResult = ControlService.ServicePart.SetConveyorStopTime((ushort)_conveyorStopTime);
                if (!boolSetResult)
                {
                    Tracer.TraceInfo("Set Converyor Stop Time Failed! " + "Value: " + _conveyorStopTime.ToString());
                }
            }
        }
    }
}
