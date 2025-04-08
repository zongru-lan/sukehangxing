using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Control;
using UI.XRay.ControlWorkflows;
using UI.XRay.EncryptLock.Smart3XServiceLibrary;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Services;
using UI.XRay.Gui.Framework;
using UI.XRay.Parts.Keyboard;
using UI.XRay.Security.Scanner.Converters;
using UI.XRay.Security.Scanner.SettingViews;

namespace UI.XRay.Security.Scanner.ViewModel
{
    public class SystemBarTouchViewModel : ViewModelBase
    {
        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        public extern static IntPtr FindWindow(string lpClassName, string lpWindowName);
        [System.Runtime.InteropServices.DllImportAttribute("user32.dll", EntryPoint = "MoveWindow")]
        public static extern bool MoveWindow(System.IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        #region commands
        public RelayCommand<String> KeyPressCommand { get; private set; }

        //public RelayCommand MoveLeftCommand { get; private set; }
        //public RelayCommand MoveRightCommand { get; private set; }
        //public RelayCommand StopCommand { get; private set; }

        //public RelayCommand<MouseButtonEventArgs> TimeControlMouseDoubleClick { get; private set; }

        #endregion commands

        /// <summary>
        /// 定时检测控制系统及采集系统的状态
        /// </summary>
        private DispatcherTimer _statesCheckTimer;

        private DateTime _currentTime = DateTime.Now;


        private string _dateFormat = "MM/dd/yyyy";

        private string _currentDateStr;

        /// <summary>
        /// 控制系统是否无法通信
        /// </summary>
        private bool _isControlSystemDead;

        /// <summary>
        /// 图像采集系统是否无法通信
        /// </summary>
        private bool _isCaptureSystemDead;

        /// <summary>
        /// 当前的关键的工作状态
        /// </summary>
        private ScannerWorkingStates _scannerWorkingStates;

        /// <summary>
        /// 当前工作模式
        /// </summary>
        private ScannerWorkMode _workMode;

        private BitmapSource MoveLeftIcon;
        private BitmapSource MoveRightIcon;

        private BitmapSource MovingLeftIcon;
        private BitmapSource MovingRightIcon;

        /// <summary>
        /// 工作模式与翻译映射
        /// </summary>
        private Dictionary<ScannerWorkMode, string> _workModeStrMap = new Dictionary<ScannerWorkMode, string>();

        private bool _isCaptureSysAlive = true;

        private int _xrayGenCount = 1;

        /// <summary>
        /// 系统当前时间
        /// </summary>
        public DateTime CurrentTime
        {
            get { return _currentTime; }
            set
            {
                _currentTime = value;
                CurrentDateStr = _currentTime.ToString(_dateFormat);
                RaisePropertyChanged();
            }
        }

        public string CurrentDateStr
        {
            get { return _currentDateStr; }
            set { _currentDateStr = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// X射线状态图标：根据射线状态及时更新
        /// </summary>
        public BitmapSource XrayBmpSource
        {
            get { return _xrayBmpSource; }
            set { _xrayBmpSource = value; RaisePropertyChanged(); }
        }

        private BitmapSource _systemStateBmpSource;

        /// <summary>
        /// 此次会话期间扫描的物体计数
        /// </summary>
        public int SessionBagCount
        {
            get { return _sessionBagCount; }
            set { _sessionBagCount = value; RaisePropertyChanged(); }
        }


        /// <summary>
        /// 历史扫描物体总数
        /// </summary>
        public int TotalBagCount
        {
            get { return _totalBagCount; }
            set { _totalBagCount = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 当前登陆的用户的Id
        /// </summary>
        public string AccountId
        {
            get { return _accountId; }
            set { _accountId = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 当前登陆用户的账户角色
        /// </summary>
        public string AccountRole
        {
            get { return _accountRole; }
            set { _accountRole = value; RaisePropertyChanged(); }
        }

        public string ImageEffectsString
        {
            get { return _imageEffectsString; }
            set { _imageEffectsString = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 当前系统状态字符串
        /// </summary>
        public string SystemStatesString
        {
            get { return _systemStatesString; }
            set { _systemStatesString = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 当前放大倍数
        /// </summary>
        public float ZoomFactorString
        {
            get { return _zoomFactorString; }
            set { _zoomFactorString = value; RaisePropertyChanged(); }
        }

        public BitmapSource ShiftKeyBmpSource
        {
            get { return _shiftKeyBmpSource; }
            set { _shiftKeyBmpSource = value; RaisePropertyChanged(); }
        }

        public BitmapSource SystemStateBmpSource
        {
            get { return _systemStateBmpSource; }
            set { _systemStateBmpSource = value; RaisePropertyChanged(); }
        }

        public BitmapSource MoveLeftIconSource
        {
            get { return _moveLeftIconSource; }
            set { _moveLeftIconSource = value; RaisePropertyChanged(); }
        }

        public BitmapSource MoveRightIconSource
        {
            get { return _moveRightIconSource; }
            set { _moveRightIconSource = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 安检机的机架温度，即控制板的温度
        /// </summary>
        public int Temperature
        {
            get { return _temperature; }
            set { _temperature = value; RaisePropertyChanged(); }
        }

        public string XrayGen1kV
        {
            get { return _xrayGen1Kv; }
            set { _xrayGen1Kv = value; RaisePropertyChanged(); }
        }
        public string XrayGen1mA
        {
            get { return _xrayGen1Ma; }
            set { _xrayGen1Ma = value; RaisePropertyChanged(); }
        }

        public string XrayGen2kV
        {
            get { return _xrayGen2Kv; }
            set { _xrayGen2Kv = value; RaisePropertyChanged(); }
        }
        public string XrayGen2mA
        {
            get { return _xrayGen2Ma; }
            set { _xrayGen2Ma = value; RaisePropertyChanged(); }
        }

        public Visibility XrayStateVisibility
        {
            get { return _xrayStateVisibility; }
            set { _xrayStateVisibility = value; RaisePropertyChanged(); }
        }

        public Visibility Gen2Visibility
        {
            get { return _gen2Visibility; }
            set { _gen2Visibility = value; RaisePropertyChanged(); }
        }

        private int _temperature;

        private int _sessionBagCount;

        private int _totalBagCount;

        private string _accountId;

        private string _accountRole;

        private string _imageEffectsString;

        private string _systemStatesString;

        /// <summary>
        /// 当前放大倍数
        /// </summary>
        private float _zoomFactorString;

        /// <summary>
        /// 用于更新系统时间的定时器
        /// </summary>
        private DispatcherTimer _timer;

        private DispatcherTimer _pullTimer;

        private BitmapSource _xrayBmpSource;

        private BitmapSource _shiftKeyBmpSource;

        private BitmapSource _moveLeftIconSource;

        private BitmapSource _moveRightIconSource;

        private bool _isTrainingMode;

        private bool _reverseMotorDirection;

        public EmergencyWindow emergencyWindow;

        bool isEmergencyShowed = false;

        string _xrayGen1Kv = "0 kV";
        string _xrayGen1Ma = "0 mA";
        string _xrayGen2Kv = "0 kV";
        string _xrayGen2Ma = "0 mA";

        private Visibility _xrayStateVisibility = Visibility.Collapsed;
        private Visibility _gen2Visibility = Visibility.Collapsed;

        private XRayGenSettingController _settingController;

        public SystemBarTouchViewModel()
        {
            CreateWorkModeStrMap();

            InitialMotoIcon();

            if (IsInDesignMode)
            {
                CurrentTime = DateTime.Now;
                XrayBmpSource = new BitmapImage(new Uri("Icons/XRayOn.png", UriKind.Relative));
                ShiftKeyBmpSource = new BitmapImage(new Uri("Icons/ShiftOff.png", UriKind.Relative));
                SystemStateBmpSource = new BitmapImage(new Uri("Icons/SystemStateOk.png", UriKind.Relative));
                MoveLeftIconSource = MoveLeftIcon;
                MoveRightIconSource = MoveRightIcon;
                SessionBagCount = 100;
                TotalBagCount = 1000;
                AccountId = "123";
                AccountRole = "Admin";
                ImageEffectsString = "伪彩色1, 反色，高穿，超级增强,吸收率10，动态灰度变换";
                SystemStatesString = "系统准备就绪";
                ZoomFactorString = 45.1f;
            }
            else
            {
                try
                {
                    CreateCommands();

                    ZoomFactorString = 1.0f;

                    LoginAccountManager.Service.AccountLogin += LoginAccountManagerOnAccountLogin;
                    LoginAccountManager.Service.AccountLogout += LoginAccountManagerOnAccountLogout;
                    BagCounterService.Service.BagCountChangedWeakEvent += ServiceOnBagCountChangedWeakEvent;

                    ControlService.ServicePart.ConveyorDirectionChanged += ServicePart_ConveyorDirectionChanged;

                    MessengerInstance.Register<UpdateImageEffectsResultMessage>(this, UpdateImageEffectsStringMessageAction);
                    MessengerInstance.Register<MotorDirectionMessage>(this, OnMotorDirectionChanged);

                    _dateFormat = DateFormatHelper.GetDateFormatHelper();

                    // 启动时间定时器，显示系统时间
                    StartClockTickTimer();
                    StartPullTimer();
                    LoadSettings();

                    UpdateTrainingMode();

                    ScannerConfig.ConfigChanged += ScannerConfigOnConfigChanged;

                    XrayBmpSource = new BitmapImage(new Uri("Icons/XRayOff.png", UriKind.Relative));
                    ShiftKeyBmpSource = new BitmapImage(new Uri("Icons/ShiftOff.png", UriKind.Relative));
                    SystemStateBmpSource = new BitmapImage(new Uri("Icons/SystemStateOk.png", UriKind.Relative));

                    MoveLeftIconSource = MoveLeftIcon;
                    MoveRightIconSource = MoveRightIcon;

                    SystemStatesMonitor.Monitor.ScannerSystemStatesTick += NotifierOnScannerSystemStatesTick;

                    _settingController = new XRayGenSettingController();
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception);
                }

                InitSystemStates();
            }

            TotalBagCount = BagCounterService.Service.TotalCountSinceInstall;

            emergencyWindow = new EmergencyWindow();

            InitKeyDictory();
        }

        void ServicePart_ConveyorDirectionChanged(object sender, ConveyorDirectionChangedEventArgs e)
        {
            switch(e.New)
            {
                case ConveyorDirection.MoveBackward:
                    MoveRightIconSource = MoveRightIcon;
                    MoveLeftIconSource = MovingLeftIcon;
                    if (_reverseMotorDirection)
                    {
                        MoveRightIconSource = MovingRightIcon;
                        MoveLeftIconSource = MoveLeftIcon;
                    }
                    break;
                case ConveyorDirection.MoveForward:
                    MoveRightIconSource = MovingRightIcon;
                    MoveLeftIconSource = MoveLeftIcon;
                    if (_reverseMotorDirection)
                    {
                        MoveRightIconSource = MoveRightIcon;
                        MoveLeftIconSource = MovingLeftIcon;
                    }
                    break;
                case ConveyorDirection.Stop:
                    MoveRightIconSource = MoveRightIcon;
                    MoveLeftIconSource = MoveLeftIcon;
                    break;
                default:
                    MoveRightIconSource = MoveRightIcon; 
                    MoveLeftIconSource = MoveLeftIcon;
                    break;
            }
        }

        private void OnMotorDirectionChanged(MotorDirectionMessage obj)
        {
            switch (obj.MotorDirection)
            {
                case MotorDirection.MoveLeft:
                    MoveRightIconSource = MoveRightIcon;
                    MoveLeftIconSource = MovingLeftIcon;
                    if (_reverseMotorDirection)
                    {
                        MoveRightIconSource = MovingRightIcon;
                        MoveLeftIconSource = MoveLeftIcon;
                    }
                    break;
                case MotorDirection.MoveRight:
                    MoveRightIconSource = MovingRightIcon;
                    MoveLeftIconSource = MoveLeftIcon;
                    if (_reverseMotorDirection)
                    {
                        MoveRightIconSource = MoveRightIcon;
                        MoveLeftIconSource = MovingLeftIcon;
                    }
                    break;
                case MotorDirection.Stop:
                    MoveRightIconSource = MoveRightIcon;
                    MoveLeftIconSource = MoveLeftIcon;
                    break;
                default:
                    MoveRightIconSource = MoveRightIcon;
                    MoveLeftIconSource = MoveLeftIcon;
                    break;
            }
        }

        private void InitialMotoIcon()
        {
            MoveLeftIcon = new BitmapImage(new Uri("Icons/MoveLeft.png", UriKind.Relative));
            MoveRightIcon = new BitmapImage(new Uri("Icons/MoveRight.png", UriKind.Relative));
            MovingLeftIcon = new BitmapImage(new Uri("Icons/MovingLeft.png", UriKind.Relative));
            MovingRightIcon = new BitmapImage(new Uri("Icons/MovingRight.png", UriKind.Relative));
        }

        private void CreateWorkModeStrMap()
        {
            if (_workModeStrMap == null)
            {
                _workModeStrMap = new Dictionary<ScannerWorkMode, string>();
            }

            try
            {
                _workModeStrMap.Add(ScannerWorkMode.Regular, TranslationService.FindTranslation("TouchUI","Regular Mode"));
                _workModeStrMap.Add(ScannerWorkMode.Continuous, TranslationService.FindTranslation("TouchUI", "Continuous Mode"));
                _workModeStrMap.Add(ScannerWorkMode.Maintenance, TranslationService.FindTranslation("TouchUI", "Maintenance Mode"));
                _workModeStrMap.Add(ScannerWorkMode.AutoReturn, TranslationService.FindTranslation("TouchUI", "AutoReturn Mode"));
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
                _workModeStrMap.Add(ScannerWorkMode.Regular, "Regular Mode");
                _workModeStrMap.Add(ScannerWorkMode.Continuous, "Continuous Mode");
                _workModeStrMap.Add(ScannerWorkMode.Maintenance, "Maintenance Mode");
                _workModeStrMap.Add(ScannerWorkMode.AutoReturn, "AutoReturn Mode");
            }
        }

        private void ScannerConfigOnConfigChanged(object sender, EventArgs eventArgs)
        {
            if (!ScannerConfig.Read(ConfigPath.TrainingIsEnabled, out _isTrainingMode))
            {
                _isTrainingMode = false;
            }
            if (!ScannerConfig.Read(ConfigPath.KeyboardIsConveyorKeyReversed, out _reverseMotorDirection))
            {
                _reverseMotorDirection = false;
            }
            UpdateTrainingMode();
        }

        /// <summary>
        /// 更新培训模式
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void UpdateTrainingMode()
        {
            if (_isTrainingMode)
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SystemStatesString = TranslationService.FindTranslation("Training is Ongoing Now");
                    UpdateSystemStateIcon(true);
                });
            }
            else
            {
                Application.Current.Dispatcher.InvokeAsync(OnScannerWorkingStatesUpdated);
            }
        }

        private void UpdateSystemStateIcon(bool ok)
        {
            if (ok)
            {
                SystemStateBmpSource = new BitmapImage(new Uri("Icons/SystemStateOk.png", UriKind.Relative));
            }
            else
            {
                SystemStateBmpSource = new BitmapImage(new Uri("Icons/SystemStateAlert.png", UriKind.Relative));
            }
        }

        /// <summary>
        /// 定时更新系统的最新状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="scannerSystemStates"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void NotifierOnScannerSystemStatesTick(object sender, ScannerSystemStates scannerSystemStates)
        {
            try
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (scannerSystemStates != null)
                    {
                        _isCaptureSysAlive = scannerSystemStates.IsCaptureSysAlive;
                        _scannerWorkingStates = scannerSystemStates.ControlStates;
                        _workMode = scannerSystemStates.WorkMode;
                        OnScannerWorkingStatesUpdated();
                        //GetWorkingValuesAsync();
                    }
                });
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        /// <summary>
        /// 初始化系统状态，启动定时器更新
        /// </summary>
        private void InitSystemStates()
        {
            try
            {
                CheckAndUpdateSystemStates();
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        /// <summary>
        /// 检查并获取系统的最新状态
        /// </summary>
        private void CheckAndUpdateSystemStates()
        {
            _scannerWorkingStates = SystemStatesMonitor.Monitor.GetWorkingStates();

            //初始工作模式为常规扫描模式
            _workMode = ScannerWorkMode.Regular;

            OnScannerWorkingStatesUpdated();
        }

        /// <summary>
        /// 根据当前的连接状态，更新
        /// </summary>
        private void OnScannerWorkingStatesUpdated()
        {
            if (_isTrainingMode)
            {
                return;
            }

            var builder = new StringBuilder(100);
            bool hasError = false;

            if (_scannerWorkingStates != null)
            {
                Temperature = _scannerWorkingStates.ScannerTemperature;

                OnXRayStateChanged(_scannerWorkingStates.IsXRayGen1Radiating);

                if (!_scannerWorkingStates.IsEmgcySwitchOn)
                {
                    hasError = true;
                    builder.Append("[").Append(TranslationService.FindTranslation("TouchUI","Emergency switch is off")).Append("] ");

                    Tracer.TraceError("Emergency switch is off");

                    if (!isEmergencyShowed)
                    {
                        emergencyWindow.Topmost = true;
                        emergencyWindow.Top = 0;
                        emergencyWindow.Left = 0;
                        emergencyWindow.Show();
                        isEmergencyShowed = true;
                    }

                    //报警
                    BeepLightForEmergencyButtonDownAsync();
                }
                else
                {
                    if (isEmergencyShowed)
                    {
                        emergencyWindow.Hide();
                        isEmergencyShowed = false;
                    }
                }

                if (!_scannerWorkingStates.LimitSwitchesStates.IsDetBoxLimitSwitchOn ||
                    !_scannerWorkingStates.LimitSwitchesStates.IsXRayGenLimitSwitchOn)
                {
                    hasError = true;
                    builder.Append("[").Append(TranslationService.FindTranslation("TouchUI", "Interlock switch is off")).Append("] ");
                    Tracer.TraceError("Interlock switch is off");
                }

                if (!_scannerWorkingStates.IsXRayGen1Alive)
                {
                    hasError = true;
                    builder.Append("[").Append(TranslationService.FindTranslation("TouchUI", "X-Ray Gen 1 is dead")).Append("] ");
                    Tracer.TraceError("X-Ray Gen 1 is dead");
                }

                if (_xrayGenCount > 1 && !_scannerWorkingStates.IsXRayGen2Alive)
                {
                    hasError = true;
                    builder.Append("[").Append(TranslationService.FindTranslation("TouchUI", "X-Ray Gen 2 is dead")).Append("] ");
                    Tracer.TraceError("X-Ray Gen 2 is dead");
                }
            }
            else
            {
                hasError = true;
                builder.Append("[").Append(TranslationService.FindTranslation("TouchUI", "Unable to access Control Board")).Append("] ");
                Tracer.TraceError("Unable to access Control Board");
            }

            if (!_isCaptureSysAlive)
            {
                hasError = true;
                builder.Append("[").Append(TranslationService.FindTranslation("TouchUI", "Unable to access Image Acquisition Board")).Append("] ");
                Tracer.TraceError("Unable to access Image Acquisition Board");
            }

            if (!hasError)
            {
                //恢复后重置此标志，下次按下急停按钮后仍然报警
                _hasBeepForEmergencyButton = false;
                string workModeStr;
                if (!_workModeStrMap.TryGetValue(_workMode, out workModeStr))
                {
                    workModeStr = TranslationService.FindTranslation("Regular Mode");
                }

                SystemStatesString = workModeStr + ": " +
                                     TranslationService.FindTranslation("The system works properly") ;
            }
            else
            {
                SystemStatesString = builder.ToString();
            }

            UpdateSystemStateIcon(!hasError);
        }

        /// <summary>
        /// 因为急停状态是循环查询的，因此设置此标志，限制只响一次
        /// </summary>
        private bool _hasBeepForEmergencyButton;
        /// <summary>
        /// 急停按钮被按下后报警加闪烁射线灯
        /// </summary>
        private void BeepLightForEmergencyButtonDownAsync()
        {
            //一次急停按钮被按下只响一次
            if (!_hasBeepForEmergencyButton)
            {
                _hasBeepForEmergencyButton = true;

                Task.Run(() =>
                {
                    XRayLampControlService.Service.Flare(TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(500), 3);

                    var times = 3;

                    while (times-- > 0)
                    {
                        try
                        {
                            ControlService.ServicePart.Beep(true);

                            Thread.Sleep(TimeSpan.FromMilliseconds(500));

                            ControlService.ServicePart.Beep(false);

                            Thread.Sleep(TimeSpan.FromMilliseconds(500));
                        }
                        catch (Exception e)
                        {
                            Tracer.TraceException(e, " Occured when beep for emergengy button down");
                        }
                    }
                });
            }
        }

        private async void GetWorkingValuesAsync()
        {
            var states = await _settingController.GetXRayGenWorkingStatesAsync();
            if (states != null)
            {
                XrayGen1kV = states.XRayGen1kV.ToString() + " kV";
                XrayGen1mA = states.XRayGen1mA.ToString() + " mA";
                XrayGen2kV = states.XRayGen2kV.ToString() + " kV";
                XrayGen2mA = states.XRayGen2mA.ToString() + " mA";
            }
        }

        private void UpdateImageEffectsStringMessageAction(UpdateImageEffectsResultMessage msg)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                ImageEffectsString = msg.EffectsString;
                ZoomFactorString = msg.ZoomFactor;
                UpdateShiftKeyState(msg.IsShiftKeyOn);
            });
        }

        /// <summary>
        /// 更新Shift功能的开关状态
        /// </summary>
        /// <param name="on"></param>
        private void UpdateShiftKeyState(bool @on)
        {
            if (@on)
            {
                ShiftKeyBmpSource = new BitmapImage(new Uri("Icons/ShiftOn.png", UriKind.Relative));
            }
            else
            {
                ShiftKeyBmpSource = new BitmapImage(new Uri("Icons/ShiftOff.png", UriKind.Relative));
            }
        }

        /// <summary>
        /// 加载系统设置
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                var image1Setting = ImageGeneralSetting.LoadImage1Setting();
                ImageEffectsString = image1Setting.GetEffectsString();

                if (!ScannerConfig.Read(ConfigPath.XRayGenCount, out _xrayGenCount))
                {
                    _xrayGenCount = 1;
                }
                Gen2Visibility = _xrayGenCount == 1 ? Visibility.Collapsed : Visibility.Visible;
                bool isShowXrayState;
                if (!ScannerConfig.Read(ConfigPath.XRayGenShowState, out isShowXrayState))
                {
                    isShowXrayState = false;
                }

                XrayStateVisibility = isShowXrayState ? Visibility.Visible : Visibility.Collapsed;
                if (!ScannerConfig.Read(ConfigPath.TrainingIsEnabled, out _isTrainingMode))
                {
                    _isTrainingMode = false;
                }

                if (!ScannerConfig.Read(ConfigPath.KeyboardIsConveyorKeyReversed, out _reverseMotorDirection))
                {
                    _reverseMotorDirection = false;
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        /// <summary>
        /// 启动时钟定时器，定时更新系统当前时间
        /// </summary>
        private void StartClockTickTimer()
        {
            _timer = new DispatcherTimer(DispatcherPriority.Background);
            _timer.Tick += (sender, args) =>
            {
                CurrentTime = DateTime.Now;
                if (isLeftPull)
                {
                    SendKeyEvents.Press(ScannerKeyboardPart.Keyboard.Left);
                    Console.WriteLine("press" + DateTime.Now.ToString());
                }
                if (isRightPull)
                {
                    SendKeyEvents.Press(ScannerKeyboardPart.Keyboard.Right);
                    Console.WriteLine("press" + DateTime.Now.ToString());
                }
            };
            _timer.Interval = TimeSpan.FromSeconds(0.1);
            _timer.Start();
        }

        /// <summary>
        /// 
        /// </summary>
        private void StartPullTimer()
        {
            _pullTimer = new DispatcherTimer(DispatcherPriority.Background);

            _pullTimer.Tick += (sender, args) =>
            {
                if (isLeftPull)
                {
                    SendKeyEvents.Release(ScannerKeyboardPart.Keyboard.Left);
                    _pullTimer.Stop();
                    isLeftPull = false;
                }
                if (isRightPull)
                {
                    SendKeyEvents.Release(ScannerKeyboardPart.Keyboard.Right);
                    Console.WriteLine("release" + DateTime.Now.ToString());
                    _pullTimer.Stop();
                    isRightPull = false;
                }

            };
            _pullTimer.Interval = TimeSpan.FromSeconds(3);
        }

        /// <summary>
        /// 扫描物体计数发生变化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="bagCountEventArgs"></param>
        private void ServiceOnBagCountChangedWeakEvent(object sender, BagCountEventArgs bagCountEventArgs)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                SessionBagCount = bagCountEventArgs.SessionCount;
                TotalBagCount = bagCountEventArgs.TotalCount;
            });
        }

        /// <summary>
        /// 用户注销事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="account"></param>
        private void LoginAccountManagerOnAccountLogout(object sender, Account account)
        {
            AccountId = "?";
            AccountRole = null;
        }

        /// <summary>
        /// 用户登录成功事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="account"></param>
        private void LoginAccountManagerOnAccountLogin(object sender, Account account)
        {
            AccountId = account.AccountId;
            AccountRole = TranslationService.FindTranslation(account.Role);
        }

        private void CreateCommands()
        {
            KeyPressCommand = new RelayCommand<String>((obj) => KeyPressCommandExe(obj));

            //MoveLeftCommand = new RelayCommand(MoveLeftCommandExecute);
            //MoveRightCommand = new RelayCommand(MoveRightCommandExecute);
            //StopCommand = new RelayCommand(StopCommandExecute);

            //TimeControlMouseDoubleClick = new RelayCommand<MouseButtonEventArgs>(TimeControlMouseDoubleClickExe);
        }

        private void TimeControlMouseDoubleClickExe(MouseButtonEventArgs obj)
        {
            MessengerInstance.Send(new OpenWindowMessage("MainWindow", "ChangeSysDateTimeWindow"));
        }


        /// <summary>
        /// 更新射线的状态图标
        /// </summary>
        /// <param name="on"></param>
        private void OnXRayStateChanged(bool on)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                XrayBmpSource = @on
                    ? new BitmapImage(new Uri("Icons/XRayOn.png", UriKind.Relative))
                    : new BitmapImage(new Uri("Icons/XRayOff.png", UriKind.Relative));
            });
        }

        Dictionary<string, Key> keyDic;
        private void InitKeyDictory()
        {
            keyDic = new Dictionary<string, Key>();
            keyDic.Add("ConveyorLeft", ScannerKeyboardPart.Keyboard.ConveyorLeft);
            keyDic.Add("ConveyorStop", ScannerKeyboardPart.Keyboard.ConveyorStop);
            keyDic.Add("ConveyorRight", ScannerKeyboardPart.Keyboard.ConveyorRight);

            //图像处理
            keyDic.Add("Esc", ScannerKeyboardPart.Keyboard.Esc);
            keyDic.Add("Inverse", ScannerKeyboardPart.Keyboard.Inverse);
            keyDic.Add("BlackWhite", ScannerKeyboardPart.Keyboard.BlackWhite);

            keyDic.Add("IncreaseAbsorb", ScannerKeyboardPart.Keyboard.IncreaseAbsorb);
            keyDic.Add("DecreaseAbsorb", ScannerKeyboardPart.Keyboard.DecreaseAbsorb);
            keyDic.Add("Os", ScannerKeyboardPart.Keyboard.Os);
            keyDic.Add("Ms", ScannerKeyboardPart.Keyboard.Ms);
            keyDic.Add("HighPenetrate", ScannerKeyboardPart.Keyboard.HighPenetrate);
            keyDic.Add("Sen", ScannerKeyboardPart.Keyboard.Sen);

            //功能键
            keyDic.Add("F1", ScannerKeyboardPart.Keyboard.F1);
            keyDic.Add("F2", ScannerKeyboardPart.Keyboard.F2);
            keyDic.Add("F3", ScannerKeyboardPart.Keyboard.F3);

            //左右回拉键
            //keyDic.Add("Left", ScannerKeyboardPart.Keyboard.Left);
            //keyDic.Add("Right", ScannerKeyboardPart.Keyboard.Right);

            //不常用功能键
            keyDic.Add("Auto", ScannerKeyboardPart.Keyboard.Auto);
            keyDic.Add("Mark", ScannerKeyboardPart.Keyboard.Mark);
            keyDic.Add("Save", ScannerKeyboardPart.Keyboard.Save);
            keyDic.Add("ContinuousScan", ScannerKeyboardPart.Keyboard.ContinuousScan);
            keyDic.Add("Ims", ScannerKeyboardPart.Keyboard.Ims);
            keyDic.Add("Menu", ScannerKeyboardPart.Keyboard.Menu);
            keyDic.Add("Z789", ScannerKeyboardPart.Keyboard.Z789);
            keyDic.Add("VFlip", ScannerKeyboardPart.Keyboard.VFlip);
            keyDic.Add("DynamicGST", ScannerKeyboardPart.Keyboard.DynamicGST);
            keyDic.Add("Zoom1X", ScannerKeyboardPart.Keyboard.Zoom1X);
        }

        bool isLeftPull = false;
        bool isRightPull = false;
        public bool IsMenuWindowOpened { get; set; }
        
        private void KeyPressCommandExe(String key)
        {
            var keyString = (string)key;
            if (keyString == "keyboard")
            {
                TouchKeyboardService.Service.OpenKeyboardWindow("HXkeyboard");
                return;
            }

            Key _key;
            //if (!IsMenuWindowOpened)
            {
                if (keyString == "Left")
                {
                    if (isRightPull)
                    {
                        //SendKeyEvents.Release(ScannerKeyboardPart.Keyboard.Right);
                        //isRightPull = false;
                    }
                    if (isLeftPull)
                    {
                        //SendKeyEvents.Release(ScannerKeyboardPart.Keyboard.Left);
                        //isLeftPull = false;
                    }
                    else
                    {
                        SendKeyEvents.Press(ScannerKeyboardPart.Keyboard.Left);
                        isLeftPull = true;
                        _pullTimer.Start();
                        //SendKeyEvents.Release(ScannerKeyboardPart.Keyboard.Left);
                    }
                    return;
                }

                if (keyString == "Right")
                {
                    if (isLeftPull)
                    {
                        //SendKeyEvents.Release(ScannerKeyboardPart.Keyboard.Left);
                        //isLeftPull = false;
                    }
                    if (isRightPull)
                    {
                        //SendKeyEvents.Release(ScannerKeyboardPart.Keyboard.Right);
                        //isRightPull = false;
                    }
                    else
                    {
                        SendKeyEvents.Press(ScannerKeyboardPart.Keyboard.Right);
                        isRightPull = true;
                        Console.WriteLine("press" + DateTime.Now.ToString());
                        _pullTimer.Start();
                        //SendKeyEvents.Release(ScannerKeyboardPart.Keyboard.Right);
                    }
                    return;
                }

                if (keyDic.TryGetValue(keyString, out _key))
                {
                    SendKeyEvents.Press(_key);
                    System.Threading.Thread.Sleep(100);
                    SendKeyEvents.Release(_key);
                }
            }

        }
    }
}
