using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Services;
using UI.XRay.Flows.TRSNetwork.Models;
using UI.XRay.Gui.Framework;
using UI.XRay.Parts.Keyboard;
using UI.XRay.Security.Scanner.Converters;
using UI.XRay.Security.Scanner.SettingViews;
using Application = System.Windows.Application;

namespace UI.XRay.Security.Scanner.ViewModel
{
    /// <summary>
    /// 系统状态栏视图模型
    /// </summary>
    public class SystemBarViewModel : ViewModelBase
    {
        #region commands
        public RelayCommand MoveLeftCommand { get; private set; }
        public RelayCommand MoveRightCommand { get; private set; }
        public RelayCommand StopCommand { get; private set; }

        public RelayCommand<MouseButtonEventArgs> TimeControlMouseDoubleClick { get; private set; }

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

        public bool IsDiagnosing { get; set; }

        private bool _tempState = true;

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

        private double _workTime = 0;
        /// <summary>
        /// 工作时间
        /// </summary>
        public Double WorkTime
        {
            get { return _workTime; }
            set { _workTime = value; RaisePropertyChanged(); }
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

        private Visibility _lockInputVisibility;

        public Visibility LockInputVisibility
        {
            get { return _lockInputVisibility; }
            set { _lockInputVisibility = value; RaisePropertyChanged(); }
        }


        public BitmapSource LockInputIconSource
        {
            get { return _lockInputIconSource; }
            set { _lockInputIconSource = value; RaisePropertyChanged(); }
        }


        public string LockInputStateStr
        {
            get { return _lockInputStateStr; }
            set { _lockInputStateStr = value; RaisePropertyChanged(); }
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

        /// <summary>
        /// 离港信息可见性
        /// </summary>
        public Visibility LeaveHarborVisibility
        {
            get { return _leaveHarborVisibility; }
            set { _leaveHarborVisibility = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 诊断时闪烁
        /// </summary>
        public bool IsInDiagnose
        {
            get { return _isInDiagnose; }
            set { _isInDiagnose = value; RaisePropertyChanged(); }
        }

        /// <summary>目前设置为FTP服务器的连接状态</summary>
        public bool IsNetworkConnected
        {
            get { return _isNetworkConnected; }
            set { _isNetworkConnected = value; RaisePropertyChanged(); }
        }

        //private DcsBagInfoDto _departingPassengerInfo;
        //public DcsBagInfoDto DepartingPassengerInfo { get { return _departingPassengerInfo; } set { _departingPassengerInfo = value; RaisePropertyChanged(); } }
        private ViewModelDcsBagInfo dcsBagInfo;
        public ViewModelDcsBagInfo DcsBagInfo { get { return dcsBagInfo; } set { dcsBagInfo = value; RaisePropertyChanged(); } }

        /// <summary>航班号</summary>
        private string _flightNo;
        public string FlightNo { get { return _flightNo; } set { _flightNo = value; RaisePropertyChanged(); } }
        /// <summary>离港日期</summary>
        private string _departDate;
        public string DepartDate { get { return _departDate; } set { _departDate = value; RaisePropertyChanged(); } }
        /// <summary>始发站3字码</summary>
        private string _departAirPort;
        public string DepartAirPort { get { return _departAirPort; } set { _departAirPort = value; RaisePropertyChanged(); } }
        /// <summary>目的航站3字码</summary>
        private string _destAirPort;
        public string DestAirPort { get { return _destAirPort; } set { _destAirPort = value; RaisePropertyChanged(); } }
        /// <summary>行李牌号</summary>
        private string _iataCode;
        public string IataCode { get { return _iataCode; } set { _iataCode = value; RaisePropertyChanged(); } }
        /// <summary>旅客姓名(全拼)</summary>
        private string _psgNameEn;
        public string PsgNameEn { get { return _psgNameEn; } set { _psgNameEn = value; RaisePropertyChanged(); } }
        /// <summary>登机号</summary>
        private string _psgBoardNo;
        public string PsgBoardNo { get { return _psgBoardNo; } set { _psgBoardNo = value; RaisePropertyChanged(); } }
        /// <summary>座位号</summary>
        private string _psgSeatNo;
        public string PsgSeatNo { get { return _psgSeatNo; } set { _psgSeatNo = value; RaisePropertyChanged(); } }
        /// <summary>值机柜台号</summary>
        private string _counterNo;
        public string CounterNo { get { return _counterNo; } set { _counterNo = value; RaisePropertyChanged(); } }
        /// <summary>行李件数</summary>
        private int _bagCount;
        public int BagCount { get { return _bagCount; } set { _bagCount = value; RaisePropertyChanged(); } }
        /// <summary>行李重量，单位公斤</summary>
        private string _bagWeight;
        public string BagWeight { get { return _bagWeight; } set { _bagWeight = value; RaisePropertyChanged(); } }
        /// <summary>值机时间</summary>
        private string _checkinTime;
        public string CheckinTime { get { return _checkinTime; } set { _checkinTime = value; RaisePropertyChanged(); } }


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

        private DispatcherTimer _systemTimeTimer;

        private BitmapSource _xrayBmpSource;

        private BitmapSource _shiftKeyBmpSource;

        private BitmapSource _moveLeftIconSource;

        private BitmapSource _moveRightIconSource;

        private BitmapSource _lockInputIconSource;

        private string _lockInputStateStr;

        private bool _isInDiagnose;

        private bool _isNetworkConnected;

        private bool _isTrainingMode;

        private bool _isConveyorKeyReversed;

        private EmergencyWindow emergencyWindow;

        bool isEmergencyShowed = false;

        string _xrayGen1Kv = "0 kV";
        string _xrayGen1Ma = "0 mA";
        string _xrayGen2Kv = "0 kV";
        string _xrayGen2Ma = "0 mA";

        bool _isMotorMoving = false;

        private Visibility _xrayStateVisibility = Visibility.Collapsed;
        private Visibility _gen2Visibility = Visibility.Collapsed;
        private Visibility _leaveHarborVisibility = Visibility.Collapsed;
        private XRayGenSettingController _settingController;

        private bool _isWorkReminder; // 是否开启换班提醒
        private TimeIntervalEnum _timeIntervalEnum; // 换班提醒间隔市场(单位：小时)
        private int _intervalSeconds { get; set; }// 间隔秒数
        private double _intervalSeconds2 { get; set; }// 间隔秒数
        private bool isInitTime;
        private DateTime _lastUpdateTime { get; set; }

        
        public SystemBarViewModel()
        {
            IsDiagnosing = true;
            CreateWorkModeStrMap();

            InitialMotoIcon();
            var isLeaveHarborEnable = false;
            if (!ScannerConfig.Read(ConfigPath.IsLeaveHarborEnable, out isLeaveHarborEnable))
            {
                isLeaveHarborEnable = false;
            }
            LeaveHarborVisibility = Visibility.Collapsed;
            HttpNetworkController.Controller.BarcodeMetaDataCommand += Controller_BarcodeMetaDataCommand;
            emergencyWindow = new EmergencyWindow();
            Messenger.Default.Register<WorkReminderChangedMessage>(this, UpdateWorkReminderCount);
            if (IsInDesignMode)
            {
                CurrentTime = DateTime.Now;
                ShiftKeyBmpSource = new BitmapImage(new Uri("Icons/ShiftOff.png", UriKind.Relative));
                SystemStateBmpSource = new BitmapImage(new Uri("Icons/SystemStateOk.png", UriKind.Relative));
                XrayBmpSource = new BitmapImage(new Uri("Icons/XRayOn.png", UriKind.Relative));
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

                    MessengerInstance.Register<UpdateImageEffectsResultMessage>(this, UpdateImageEffectsStringMessageAction);
                    MessengerInstance.Register<MotorDirectionMessage>(this, OnMotorDirectionChanged);

                    _dateFormat = DateFormatHelper.GetDateFormatHelper();

                    if (!ScannerConfig.Read(ConfigPath.IsWorkIntervalReminder, out _isWorkReminder))
                    {
                        _isWorkReminder = false;
                    }
                    if (!ScannerConfig.Read(ConfigPath.WorkReminderTime, out _timeIntervalEnum))
                    {
                        _timeIntervalEnum = TimeIntervalEnum.HalfHour;
                    }
                    _intervalSeconds = (int)TimeIntervalEnumString.GetEnumToSecond(_timeIntervalEnum);
                    //_intervalSeconds2 = TimeIntervalEnumString.GetEnumToSecond2(_timeIntervalEnum);
                    // 启动时间定时器，显示系统时间
                    StartClockTickTimer();
                    isInitTime = false;
                    StartClockTickTimer2();
                    LoadSettings();

                    UpdateTrainingMode();

                    ScannerConfig.ConfigChanged += ScannerConfigOnConfigChanged;

                    XrayBmpSource = new BitmapImage(new Uri("Icons/XRayOff.png", UriKind.Relative));
                    ShiftKeyBmpSource = new BitmapImage(new Uri("Icons/ShiftOff.png", UriKind.Relative));
                    SystemStateBmpSource = new BitmapImage(new Uri("Icons/SystemStateOk.png", UriKind.Relative));

                    LockInputVisibility = Visibility.Collapsed;
                    LockInputIconSource = new BitmapImage(new Uri("Icons/Unlock.png", UriKind.Relative));
                    LockInputStateStr = TranslationService.FindTranslation("Unlock");

                    MoveLeftIconSource = MoveLeftIcon;
                    MoveRightIconSource = MoveRightIcon;

                    ControlService.ServicePart.ConveyorDirectionChanged += ServicePart_ConveyorDirectionChanged;
                    SystemStatesMonitor.Monitor.ScannerSystemStatesTick += NotifierOnScannerSystemStatesTick;

                    LockInputService.Service.LockInputEvent += Service_LockInputEvent;

                    _settingController = new XRayGenSettingController();
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception);
                }

                InitSystemStates();
            }

            TotalBagCount = BagCounterService.Service.TotalCountSinceInstall;
            //ShowPassengerLeaveMsg();

        }
        private void ShowPassengerLeaveMsg()
        {
            //Task.Run(() =>
            //{
            //    while (true)
            //    {
            //        var info = new DcsBagInfoDto()
            //        {
            //            BAGCOUNT = 3,
            //            BAGWEIGHT = 56,
            //            CHECKINTIME = new DateTime(2024, 02, 08, 21, 32, 55),
            //            COUNTERNO = "A12",
            //            DEPARTAIRPORT = "CRK",
            //            DEPARTDATE = new DateTime(2024, 02, 09, 12, 02, 00),
            //            DESTAIRPORT = "WNZ",
            //            FLIGHTNO = "MU2882",
            //            FromBoardNo = "23",
            //            FROMDATE = new DateTime(2024, 02, 06, 17, 34, 00),
            //            FROMDEPARTAIRPORT = "SEA",
            //            FROMDESTAIRPORT = "TLV",
            //            FROMFLIGHTNO = "HU407",
            //            FROMSeatNO = "20F",
            //            IATACODE = "456476451654654",
            //            PSGNAMEEN = "ZHU/DUORU",
            //            PSGSEATNO = "54L",
            //        };
            //        DcsBagInfo = new ViewModelDcsBagInfo(info);
            //        Thread.Sleep(3000);
            //    }
            //});
            //FlightNumber = "HU407";// 航班号
            //LeaveHarborTime = new DateTime(2019, 10, 12);//离港日期
            //DepartureStation = "PVG"; //始发站
            //DestinationStation = "TLV";//目的站
            //BaggageNumber = "3880926140";//行李条码
            //PassengerName = "QIAN/LI";//旅客姓名
            //RegisterNumber = "190";//登记序号
            //SeatNumber = "38B";//座位号
        }


        private void Service_LockInputEvent(object sender, LockInputEventArgs e)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                LockInputVisibility = e.State
                ? Visibility.Visible
                : Visibility.Collapsed;

                LockInputIconSource = e.State
                    ? new BitmapImage(new Uri("Icons/Lock.png", UriKind.Relative))
                    : new BitmapImage(new Uri("Icons/Unlock.png", UriKind.Relative));

                LockInputStateStr = e.State
                ? TranslationService.FindTranslation("InputLock")
                : TranslationService.FindTranslation("InputUnlock");
            });
        }

        void ServicePart_ConveyorDirectionChanged(object sender, ConveyorDirectionChangedEventArgs e)
        {
            if (!_isTrainingMode)
            {
                switch (e.New)
                {
                    case ConveyorDirection.MoveBackward:
                        MoveRightIconSource = MoveRightIcon;
                        MoveLeftIconSource = MovingLeftIcon;
                        if (_isConveyorKeyReversed)
                        {
                            MoveRightIconSource = MovingRightIcon;
                            MoveLeftIconSource = MoveLeftIcon;
                        }
                        _isMotorMoving = true;
                        break;
                    case ConveyorDirection.MoveForward:
                        MoveRightIconSource = MovingRightIcon;
                        MoveLeftIconSource = MoveLeftIcon;
                        if (_isConveyorKeyReversed)
                        {
                            MoveRightIconSource = MoveRightIcon;
                            MoveLeftIconSource = MovingLeftIcon;
                        }
                        _isMotorMoving = true;
                        break;
                    case ConveyorDirection.Stop:
                        MoveRightIconSource = MoveRightIcon;
                        MoveLeftIconSource = MoveLeftIcon;
                        _isMotorMoving = false;
                        break;
                    default:
                        MoveRightIconSource = MoveRightIcon;
                        MoveLeftIconSource = MoveLeftIcon;
                        break;
                }
            }
        }

        private void OnMotorDirectionChanged(MotorDirectionMessage obj)
        {
            if (_isTrainingMode)
            {
                switch (obj.MotorDirection)
                {
                    case MotorDirection.MoveLeft:
                        MoveRightIconSource = MoveRightIcon;
                        MoveLeftIconSource = MovingLeftIcon;
                        if (_isConveyorKeyReversed)
                        {
                            MoveRightIconSource = MovingRightIcon;
                            MoveLeftIconSource = MoveLeftIcon;
                        }
                        break;
                    case MotorDirection.MoveRight:
                        MoveRightIconSource = MovingRightIcon;
                        MoveLeftIconSource = MoveLeftIcon;
                        if (_isConveyorKeyReversed)
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
                _workModeStrMap.Add(ScannerWorkMode.Regular, TranslationService.FindTranslation("Regular Mode"));
                _workModeStrMap.Add(ScannerWorkMode.Continuous, TranslationService.FindTranslation("Continuous Mode"));
                _workModeStrMap.Add(ScannerWorkMode.Maintenance, TranslationService.FindTranslation("Maintenance Mode"));
                _workModeStrMap.Add(ScannerWorkMode.AutoReturn, TranslationService.FindTranslation("AutoReturn Mode"));
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
            SystemStatus.Instance.IsTraining = _isTrainingMode;

            if (!ScannerConfig.Read(ConfigPath.KeyboardIsConveyorKeyReversed, out _isConveyorKeyReversed))
            {
                _isConveyorKeyReversed = false;
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
                        SystemStateService.Service.IsCaptureBoardAlive = _isCaptureSysAlive;
                        _scannerWorkingStates = scannerSystemStates.ControlStates;
                        _workMode = scannerSystemStates.WorkMode;
                        OnScannerWorkingStatesUpdated();
                        GetWorkingValuesAsync();
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

                SystemStateService.Service.IsControlBoardConnected = true;
                SystemStateService.Service.IsEmergencyTriggered = !_scannerWorkingStates.IsEmgcySwitchOn;
                SystemStateService.Service.ControlBoardTemperature = _scannerWorkingStates.ScannerTemperature;
                SystemStateService.Service.IsInterlockTriggered = !_scannerWorkingStates.LimitSwitchesStates.IsXRayGenLimitSwitchOn;

                SystemStateService.Service.IsXrayGen1Alive = _scannerWorkingStates.IsXRayGen1Alive;
                SystemStateService.Service.IsXrayGen2Alive = _scannerWorkingStates.IsXRayGen2Alive;
                SystemStateService.Service.IsPESensor1Triggered = _scannerWorkingStates.PESensorsStates.PES1State;
                SystemStateService.Service.IsPESensor2Triggered = _scannerWorkingStates.PESensorsStates.PES3State;


                Temperature = _scannerWorkingStates.ScannerTemperature;

                OnXRayStateChanged(_scannerWorkingStates.IsXRayGen1Radiating);

                if (!_scannerWorkingStates.IsEmgcySwitchOn)
                {
                    hasError = true;
                    builder.Append("[").Append(TranslationService.FindTranslation("Emergency switch is off")).Append("] ");

                    Tracer.TraceError("Emergency switch is off");

                    if (!isEmergencyShowed)
                    {
                        bool x = true;  //yxc
                        Transmission.emergency2(x);//yxc
                        ControlService.ServicePart.RadiateXRay(false);
                        ControlService.ServicePart.DriveConveyor(ConveyorDirection.Stop);
                        Messenger.Default.Send(new EmergencyMessage());
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

                if (!_scannerWorkingStates.LimitSwitchesStates.IsXRayGenLimitSwitchOn)
                {
                    hasError = true;
                    builder.Append("[").Append(TranslationService.FindTranslation("Interlock switch is off")).Append("] ");
                    Tracer.TraceError("Interlock switch is off");
                }

                if (!_scannerWorkingStates.IsXRayGen1Alive)
                {
                    hasError = true;
                    builder.Append("[").Append(TranslationService.FindTranslation("X-Ray Gen 1 is dead")).Append("] ");
                    Tracer.TraceError("X-Ray Gen 1 is dead");
                }

                if (_xrayGenCount > 1 && !_scannerWorkingStates.IsXRayGen2Alive)
                {
                    hasError = true;
                    builder.Append("[").Append(TranslationService.FindTranslation("X-Ray Gen 2 is dead")).Append("] ");
                    Tracer.TraceError("X-Ray Gen 2 is dead");
                }

            }
            else
            {
                hasError = true;
                builder.Append("[").Append(TranslationService.FindTranslation("Unable to access Control Board")).Append("] ");
                Tracer.TraceError("Unable to access Control Board");
                SystemStateService.Service.IsControlBoardConnected = false;
            }

            if (!_isCaptureSysAlive)
            {
                hasError = true;
                builder.Append("[").Append(TranslationService.FindTranslation("Unable to access Image Acquisition Board")).Append("] ");
                Tracer.TraceError("Unable to access Image Acquisition Board");
            }

            if (!hasError)
            {
                string netConnect = "";
                if (HttpNetworkController.Controller.IsConnected)
                {
                    netConnect = " [" + TranslationService.FindTranslation("Network Connected") + "]";
                }

                //恢复后重置此标志，下次按下急停按钮后仍然报警
                _hasBeepForEmergencyButton = false;
                string workModeStr;
                if (!_workModeStrMap.TryGetValue(_workMode, out workModeStr))
                {
                    workModeStr = TranslationService.FindTranslation("Regular Mode");
                }

                SystemStatesString = workModeStr + ": " +
                                     TranslationService.FindTranslation("The system works properly") + netConnect;
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
                XrayGen1kV = states.XRayGen1kV.ToString("#0.000").PadLeft(7, ' ') + " kV";
                XrayGen1mA = states.XRayGen1mA.ToString("#0.000").PadLeft(7, ' ') + " mA";
                XrayGen2kV = states.XRayGen2kV.ToString("#0.000").PadLeft(7, ' ') + " kV";
                XrayGen2mA = states.XRayGen2mA.ToString("#0.000").PadLeft(7, ' ') + " mA";
                SystemStateService.Service.XrayGen1RealTimeKV = states.XRayGen1kV;
                SystemStateService.Service.XrayGen2RealTimeKV = states.XRayGen2kV;
                SystemStateService.Service.XrayGen1RealTimeErrorCode = states.XRayGen1ErrorCode;
                SystemStateService.Service.XrayGen1RealTimeMA = states.XRayGen1mA;
                SystemStateService.Service.XrayGen2RealTimeMA = states.XRayGen2mA;
                SystemStateService.Service.XrayGen2RealTimeErrorCode = states.XRayGen2ErrorCode;

                SystemStatus.Instance.Generator1Voltage = states.XRayGen1kV;
                SystemStatus.Instance.Generator1Current = states.XRayGen1mA;
                SystemStatus.Instance.Generator1ErrorCode = states.XRayGen1ErrorCode;
                SystemStatus.Instance.Generator2Voltage = states.XRayGen2kV;
                SystemStatus.Instance.Generator2Current = states.XRayGen2mA;
                SystemStatus.Instance.Generator2ErrorCode = states.XRayGen2ErrorCode;
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

                if (!ScannerConfig.Read(ConfigPath.KeyboardIsConveyorKeyReversed, out _isConveyorKeyReversed))
                {
                    _isConveyorKeyReversed = false;
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

                //if (SystemStatus.Instance.HasLogin)
                //{
                //    WorkTime = Math.Round((DateTime.Now - LoginAccountManager.Service.LoginTime).TotalHours, 4);
                //    //换班提醒
                //    int totalSeconds = (int)(WorkTime * 60 * 60);
                //    if (_isWorkReminder && totalSeconds > 0 && totalSeconds % _intervalSeconds < 1)
                //    {
                //        Messenger.Default.Send<ShowFlyoutMessage>(new ShowFlyoutMessage("MainWindow",
                //            "你该换班了", MessageIcon.Warning, 2));
                //        //TranslationService.FindTranslation(""), MessageIcon.Warning, 2));
                //        //MessengerInstance.Send(new OpenWindowMessage("MainWindow", "WorkReminderWindow", _workReminderCount));
                //    }
                //}
                //else
                //{
                //    WorkTime = 0;
                //}
                if (SystemStatus.Instance.IsFTPServerConnected)
                {
                    IsNetworkConnected = true;
                }
                else
                {
                    IsNetworkConnected = false;
                }
                if (SystemStateService.Service.IsDiagnosing)
                {
                    _tempState = !_tempState;
                    if (_tempState)
                    {
                        IsInDiagnose = true;
                    }
                    else
                    {
                        IsInDiagnose = false;
                    }
                }
                else
                {
                    IsInDiagnose = false;
                }
            };
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Start();
        }

        private void StartClockTickTimer2()
        {
            _systemTimeTimer = new DispatcherTimer(DispatcherPriority.Background);
            _systemTimeTimer.Tick += (sender, args) =>
            {
                if (SystemStatus.Instance.HasLogin)
                {
                    DateTime now = DateTime.Now;
                    WorkTime = Math.Round((now - LoginAccountManager.Service.LoginTime).TotalHours, 4);
                    if (!isInitTime && (WorkTime * 60 * 60) < _intervalSeconds)
                    {
                        isInitTime = true;
                        _lastUpdateTime = now;
                    }
                    //换班提醒
                    TimeSpan timeSpan = now - _lastUpdateTime;
                    if (_isWorkReminder && (timeSpan > TimeSpan.FromSeconds(_intervalSeconds)))
                    {
                        Messenger.Default.Send<ShowFlyoutMessage>(new ShowFlyoutMessage("MainWindow",
                            TranslationService.FindTranslation("It's time for you to change shifts"), MessageIcon.Warning, 7));
                        _lastUpdateTime = now;
                    }
                }
                else
                {
                    WorkTime = 0;
                }
            };
            _systemTimeTimer.Interval = TimeSpan.FromSeconds(1);
            _systemTimeTimer.Start();
        }

        /// <summary>
        /// 更新换班提醒时间间隔
        /// </summary>
        /// <param name="message"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void UpdateWorkReminderCount(WorkReminderChangedMessage message)
        {
            _timeIntervalEnum = message.IntervalEnum;
            _isWorkReminder = message.IsShifts;
            _intervalSeconds = (int)TimeIntervalEnumString.GetEnumToSecond(_timeIntervalEnum);
            _lastUpdateTime = DateTime.Now;
            //_intervalSeconds2 = TimeIntervalEnumString.GetEnumToSecond2(_timeIntervalEnum);
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
            MoveLeftCommand = new RelayCommand(MoveLeftCommandExecute);
            MoveRightCommand = new RelayCommand(MoveRightCommandExecute);
            StopCommand = new RelayCommand(StopCommandExecute);
            
            TimeControlMouseDoubleClick = new RelayCommand<MouseButtonEventArgs>(TimeControlMouseDoubleClickExe);
        }

        private void Controller_BarcodeMetaDataCommand(object sender, DcsBagInfoDto e)
        {
            if (e == null)
            {
                Tracer.TraceDebug($"【HttpNetworkService】 SystemBar DcsBagInfoDto is null:{e == null}");
                LeaveHarborVisibility = Visibility.Collapsed;
                return;
            }
            Tracer.TraceDebug($"【NetworkService】 SENDERID:{e.SENDERID},PSGNAMEEN:{e.PSGNAMEEN},DESTAIRPORT:{e.DESTAIRPORT},FROMDESTAIRPORT:{e.FROMDESTAIRPORT},BAGCOUNT:{e.BAGCOUNT}");
            LeaveHarborVisibility = Visibility.Visible;
            FlightNo = e.FLIGHTNO;
            DepartDate = e.DEPARTDATE != null ? e.DEPARTDATE.Value.ToString("yyyy-MM-dd HH:mm:ss") : "";
            DepartAirPort = e.DEPARTAIRPORT;
            DestAirPort = e.DESTAIRPORT;
            IataCode = e.IATACODE;
            PsgNameEn = e.PSGNAMEEN;
            PsgBoardNo = e.PSGBOARDNO;
            PsgSeatNo = e.PSGSEATNO;
            CounterNo = e.COUNTERNO;
            BagCount = e.BAGCOUNT != null ? e.BAGCOUNT.Value : 0;
            BagWeight = e.BAGWEIGHT != null ? e.BAGWEIGHT.Value.ToString() : "0";
            CheckinTime = e.CHECKINTIME != null ? e.DEPARTDATE.Value.ToString("yyyy-MM-dd HH:mm:ss") : "";
        }

        private void TimeControlMouseDoubleClickExe(MouseButtonEventArgs obj)
        {
            // if (LoginAccountManager.Service.IsCurrentRoleEqualOrGreaterThan(Business.Entities.AccountRole.Maintainer))
            //   {
            //      MessengerInstance.Send(new OpenWindowMessage("MainWindow", "ChangeSysDateTimeWindow"));
            //  }              //yxc 不让显示
        }

        private void MoveLeftCommandExecute()
        {
            if (ReadConfigService.Service.IsUseUSBCommandKeyboard)
            {
                SendKeyEvents.Press(Key.O);
                return;
            }
            SendKeyEvents.Press(Key.F4);
        }

        private void MoveRightCommandExecute()
        {
            if (ReadConfigService.Service.IsUseUSBCommandKeyboard)
            {
                SendKeyEvents.Press(Key.I);
                return;
            }
            SendKeyEvents.Press(Key.F6);
        }

        private void StopCommandExecute()
        {
            if (ReadConfigService.Service.IsUseUSBCommandKeyboard)
            {
                SendKeyEvents.Press(Key.S);
                return;
            }
            SendKeyEvents.Press(Key.F5);
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

        public override void Cleanup()
        {
            if (emergencyWindow != null)
            {
                emergencyWindow.Close();
            }
            LoginAccountManager.Service.AccountLogin -= LoginAccountManagerOnAccountLogin;
            LoginAccountManager.Service.AccountLogout -= LoginAccountManagerOnAccountLogout;
            BagCounterService.Service.BagCountChangedWeakEvent -= ServiceOnBagCountChangedWeakEvent;

            ScannerConfig.ConfigChanged -= ScannerConfigOnConfigChanged;

            ControlService.ServicePart.ConveyorDirectionChanged -= ServicePart_ConveyorDirectionChanged;
            SystemStatesMonitor.Monitor.ScannerSystemStatesTick -= NotifierOnScannerSystemStatesTick;
        }
    }

    public class ViewModelDcsBagInfo
    {
        /// <summary>航班号</summary>
        public string FlightNo { get; set; }
        /// <summary>离港日期</summary>
        public string DepartDate { get; set; }
        /// <summary>始发站3字码</summary>
        public string DepartAirPort { get; set; }
        /// <summary>目的航站3字码</summary>
        public string DestAirPort { get; set; }
        /// <summary>行李牌号</summary>
        public string IataCode { get; set; }
        /// <summary>旅客姓名(全拼)</summary>
        public string PsgNameEn { get; set; }
        /// <summary>登机号</summary>
        public string PsgBoardNo { get; set; }
        /// <summary>座位号</summary>
        public string PsgSeatNo { get; set; }
        /// <summary>值机柜台号</summary>
        public string CounterNo { get; set; }
        /// <summary>行李件数</summary>
        public int BagCount { get; set; }
        /// <summary>行李重量，单位公斤</summary>
        public string BagWeight { get; set; }
        /// <summary>值机时间</summary>
        public string CheckinTime { get; set; }

        public ViewModelDcsBagInfo(DcsBagInfoDto info)
        {
            FlightNo = info.FLIGHTNO;
            DepartDate = info.DEPARTDATE != null ? info.DEPARTDATE.Value.ToString("yyyy-MM-dd HH:mm:ss"):"";
            DepartAirPort = info.DEPARTAIRPORT;
            DestAirPort = info.DESTAIRPORT;
            IataCode = info.IATACODE;
            PsgNameEn = info.PSGNAMEEN;
            PsgBoardNo = info.PSGBOARDNO;
            PsgSeatNo = info.PSGSEATNO;
            CounterNo = info.COUNTERNO;
            BagCount = info.BAGCOUNT != null ? info.BAGCOUNT.Value : 0;
            BagWeight = info.BAGWEIGHT != null ? info.BAGWEIGHT.Value.ToString() : "0";
            CheckinTime = info.CHECKINTIME != null ? info.DEPARTDATE.Value.ToString("yyyy-MM-dd HH:mm:ss"):"";
        }
    }
}
