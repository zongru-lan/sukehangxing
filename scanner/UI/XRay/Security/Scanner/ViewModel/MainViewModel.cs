using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Remoting.Lifetime;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Threading;
using UI.Common.Tracers;
using UI.XRay.Business.Algo;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Common.Utilities;
using UI.XRay.Control;
using UI.XRay.ControlWorkflows;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Services;
using UI.XRay.Flows.Services.Db;
using UI.XRay.Gui.Framework;
using UI.XRay.ImagePlant;
using UI.XRay.ImagePlant.Gpu;
using UI.XRay.Parts.Keyboard;
using UI.XRay.RenderEngine;
using Application = System.Windows.Application;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Path = System.IO.Path;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using System.Windows.Media.Effects;
using XRayNetEntities;

namespace UI.XRay.Security.Scanner.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {

        private TipPlansController _controller;  //yxc
        private TipPlanandImageController _imagecontroller; //yxc
        public RelayCommand<KeyEventArgs> PreviewKeyDownCommand { get; set; }

        public RelayCommand<KeyEventArgs> PreviewKeyUpCommand { get; set; }

        public RelayCommand<MouseButtonEventArgs> MouseRightButtonDownEventCommand { get; set; }

        public RelayCommand<MouseButtonEventArgs> OnMouseDownEventCommand { get; set; }

        public RelayCommand LoadedEventCommand { get; set; }

        /// <summary>
        /// �����ڼ����رյ��¼�
        /// </summary>
        public RelayCommand ClosingEventCommand { get; set; }

        /// <summary>
        /// ��ʽͼ����ͼ������������
        /// </summary>
        public IRollingImageProcessController RollingImagesController { get; private set; }

        /// <summary>
        /// ��ʽͼ�������ݿ��ƣ�����ɨ��ͼ�����ݡ���ѵͼ�������Լ��ط�ͼ�����ݵĿ���
        /// </summary>
        public DisplayImageDataController ImageDataController { get; private set; }

        /// <summary>
        /// ͼ��1��Ĭ������
        /// </summary>
        private ImageGeneralSetting _image1DefaultSetting;

        /// <summary>
        /// ͼ��2��Ĭ������
        /// </summary>
        private ImageGeneralSetting _image2DefaultSetting;

        /// <summary>
        /// ���ڼ�¼�û����޸�ͼ��1����Ч֮ǰ����Ч
        /// </summary>
        private ImageEffectsComposition _image1LastEffectsComposition;

        /// <summary>
        /// ���ڼ�¼�û����޸�ͼ��2����Ч֮ǰ����Ч
        /// </summary>
        private ImageEffectsComposition _image2LastEffectsComposition;

        /// <summary>
        /// �Ƿ��п������İ��������£�ͼ����Ч�������������ϼ�����Auto������
        /// �������ֿ��ذ�������1���Ӻ󣬵���ʱ�������ָ����ذ���֮ǰ�Ĺ���
        /// </summary>
        private bool _isToggleKeyDown;

        /// <summary>
        /// �Ŵ󾵹��ܼ��Ƿ񱻰���
        /// </summary>
        private bool _isMagnifyKeyDown = false;
        private bool _isConveyorCanMove;

        /// <summary>
        /// ��ǰ�����µĿ��ذ����ļ�ֵ
        /// </summary>
        private Key _lastToggleKey = Key.None;

        /// <summary>
        /// ���ذ����״α����µ�ʱ��
        /// </summary>
        private DateTime _lastToggleKeyDownTime;

        /// <summary>
        /// 是否增强
        /// </summary>
        private bool _isEnhanced = true;

        private bool _isClearTip = false;

        public static bool IsShaped;

        /// </summary>
        float xrayKV;
        float xrayMA;
        float xrayKV2;
        float xrayMA2;
        int genCount = 1;

        bool _zoom1XWhenStart = false;

        int _backwardKeyDownCount = 0;

        int _backwardKeydownCountThr = 6;

        DateTime _lastSaveDateTime = DateTime.Now;

        int _imageBrighterAbsorp = -15;
        int _imageDarkerAbsorp = 25;

        private bool isFlowLineMode;

        private bool lockConveyorReverseKey;

        private bool testMode = false;
        private float _colorBlockParam1;

        private float _colorBlockParam2;

        private float _colorBlockParam3;

        private float _colorBlockParam4;

        private bool _isEnableShiftKey = true;

        /// <summary>
        /// 当前是否本地判图
        /// true:Marke按键作为开包检查按键，Auto按键作为放行按键
        /// false:Marke按键作为标记按键,Auto按键作为自动报警开关按键
        /// </summary>
        private bool _isLocalManualInspection = false;

        /// <summary>
        /// 是否屏蔽输送机前进。默认：false
        /// true:屏蔽前进键
        /// false:不屏蔽前进键
        /// </summary>
        private bool _isShieldedConveyorRight = false;
        #region ���ܼ�
        public ImageEffectsComposition F1EffectsComposition
        {
            get { return _f1EffectsComposition; }
            set { _f1EffectsComposition = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// ���ܼ�F2��ͼ����Ч����
        /// </summary>
        public ImageEffectsComposition F2EffectsComposition
        {
            get { return _f2EffectsComposition; }
            set { _f2EffectsComposition = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// ���ܼ�F3��ͼ����Ч����
        /// </summary>
        public ImageEffectsComposition F3EffectsComposition
        {
            get { return _f3EffectsComposition; }
            set { _f3EffectsComposition = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// Shift����״̬
        /// </summary>
        public bool IsShiftKeyOn
        {
            get { return _isShiftKeyOn; }
            set { _isShiftKeyOn = value; RaisePropertyChanged(); }
        }

        private ImageEffectsComposition _f1EffectsComposition;

        /// <summary>
        /// ���ܼ�F2��ͼ����Ч����
        /// </summary>
        private ImageEffectsComposition _f2EffectsComposition;

        /// <summary>
        /// ���ܼ�F3��ͼ����Ч����
        /// </summary>
        private ImageEffectsComposition _f3EffectsComposition;

        private bool _isActionKeyEnable = false;

        private ActionKey? _f1KeyAction;
        private ActionKey? _f2KeyAction;
        private ActionKey? _f3KeyAction;
        /// <summary>
        /// 功能键F1的动作行为
        /// </summary>
        public ActionKey? F1KeyAction
        {
            get { return _f1KeyAction; }
            set { _f1KeyAction = value; RaisePropertyChanged(); }
        }
        /// <summary>
        /// 功能键F2的动作行为
        /// </summary>
        public ActionKey? F2KeyAction
        {
            get { return _f2KeyAction; }
            set { _f2KeyAction = value; RaisePropertyChanged(); }
        }
        /// <summary>
        /// 功能键F3的动作行为
        /// </summary>
        public ActionKey? F3KeyAction
        {
            get { return _f3KeyAction; }
            set { _f3KeyAction = value; RaisePropertyChanged(); }
        }

        private bool _isShiftKeyOn;

        /// <summary>
        /// 穿不透区域增强（针对圆饼）
        /// </summary>
        private bool _isUnPenerationRegion = false;

        private DateTime _lastMotorMoveTime = DateTime.Now;

        /// <summary>
        /// 最近一次前后运行按键时间
        /// </summary>
        private DateTime _lastNotStopMotorMoveTime = DateTime.Now;

        MotorDirection _currentDirection = MotorDirection.Stop;

        MotorDirection _lastDirection = MotorDirection.Stop;

        DateTime _leftPressedDateTime = DateTime.Now;

        DateTime _rightPressedDateTime = DateTime.Now;

        System.Timers.Timer _tickTimer;

        private bool _isKeyboardReversed = false;

        private TimeSpan _keyPressInterval = TimeSpan.FromMilliseconds(350);

        /// <summary>
        /// 直方图增强
        /// </summary>
        private bool _isImageHistogram = false;

        private bool _isSifenbian = false;

        private bool _doubleEnhance = false;

        private bool _isUSBCommonKeyboard;

        /// <summary>
        /// 图像放大情况下，true移动图像窗口，默认false是回拉，
        /// </summary>
        private bool _canMoveZoomWhenMagnify = false;
     

        #endregion ���ܼ�

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(IRollingImageProcessController rollingImageController)
        {
            Tracer.TraceEnterFunc("UI.XRay.Security.Scanner.ViewModel.MainViewModel");

            CreateCommands();
            RegisterMessengers();

            ReadKeyBoardType();

            LoadImageGeneralSettings();
            LoadXGenSetting();

            _controller = new TipPlansController();
            _imagecontroller = new TipPlanandImageController();

            ScannerConfig.ConfigChanged += ScannerConfigOnConfigChanged;
            ControlService.ServicePart.ConveyorDirectionChanged += ServicePart_ConveyorDirectionChanged;
            HttpNetworkController.Controller.RemoteTipPlanUpdateAction += updateNetTipPlan; //yxc
            HttpNetworkController.Controller.ShowWindowAction += showwindow;  //yxc
            HttpNetworkController.Controller.CloseWindowAction += closewindow;  //yxc
            HttpNetworkController.Controller.ResetCommand += Controller_ResetCommand;
            //TRSNetWorkService.Serviece.ControlConveyorAction += Serviece_ControlConveyorAction;
            // ��ʼ����������ʾ����
            try
            {
                RollingImagesController = rollingImageController;
                RollingImagesController.ZoomFactorChanged += RollingImagesControllerOnZoomFactorChanged;

                var welcome = SkinHelper.LoadSkinImage(SkinPaths.WelcomeImage);
                if (welcome != null)
                {
                    welcome.RotateFlip(RotateFlipType.Rotate180FlipX);
                }

                // �������зֱ����������ӽǵĵ������ã����ֱ���ʼ��ͼ����ʾ��ͼ�����ݿ�����
                var view1ChannelsCount = ExchangeDirectionConfig.Service.GetView1ImageHeight();
                var image1Setting = new RollingImageSetting(view1ChannelsCount, _image1DefaultSetting.VerticalScale, _image1DefaultSetting.HorizonalScale, _image1DefaultSetting.MoveFromRightToLeft,
                    _image1DefaultSetting.VerticalFlip,
                    false, _image1DefaultSetting.ShowUnpenetrateRed, welcome, _image1DefaultSetting.ImageAnchor);
                image1Setting.ColorBlockParam1 = _colorBlockParam1;
                image1Setting.ColorBlockParam2 = _colorBlockParam2;
                image1Setting.ColorBlockParam3 = _colorBlockParam3;
                image1Setting.ColorBlockParam4 = _colorBlockParam4;

                RollingImageSetting image2Setting = null;
                if (_image2DefaultSetting != null)
                {
                    var view2ChannelsCount = ExchangeDirectionConfig.Service.GetView2ImageHeight();

                    image2Setting = new RollingImageSetting(view2ChannelsCount, _image2DefaultSetting.VerticalScale, _image2DefaultSetting.HorizonalScale, _image2DefaultSetting.MoveFromRightToLeft, _image2DefaultSetting.VerticalFlip,
                        false, _image2DefaultSetting.ShowUnpenetrateRed, welcome, _image2DefaultSetting.ImageAnchor);
                    image2Setting.ColorBlockParam1 = _colorBlockParam1;
                    image2Setting.ColorBlockParam2 = _colorBlockParam2;
                    image2Setting.ColorBlockParam3 = _colorBlockParam3;
                    image2Setting.ColorBlockParam4 = _colorBlockParam4;
                }
                
                RollingImagesController.Initialize(image1Setting, image2Setting);
                LoadImageSetting();
                //配置控件宽度和视角
                var viewCount = _image2DefaultSetting != null ? 2 : 1;
                PaintingRegionsService.Service.InitScreen((int)RollingImagesController.Width, viewCount);
                //设定方向
                RollingImagesController.RightToLeft = _image1DefaultSetting.MoveFromRightToLeft;
                rollingImageController.HorizonalScale = _image1DefaultSetting.HorizonalScale;
                if (!ScannerConfig.Read(ConfigPath.SystemIsAllowManualInspection, out bool isLocalManualInspection))
                {
                    isLocalManualInspection = false;
                }
                _isLocalManualInspection = isLocalManualInspection;
                if (!ScannerConfig.Read(ConfigPath.DefaultZoomMultiples, out float defaultZoomMultiples))
                {
                    defaultZoomMultiples = 1;
                }
                if (defaultZoomMultiples > 1.0)
                {
                    var image1 = RollingImagesController.Image1;
                    var image2 = RollingImagesController.Image2;
                    image1.Zoom(defaultZoomMultiples);
                    if (image2 != null)
                    {
                        image2.Zoom(defaultZoomMultiples);
                    }
                    int moveCount = (int)Math.Ceiling((defaultZoomMultiples - 1) * 12.5);
                    if (!ScannerConfig.Read(ConfigPath.DefaultZoomLocation, out string moveStr))
                    {
                        moveStr = string.Empty;
                    }
                    if (!string.IsNullOrEmpty(moveStr))
                    {
                        if (moveStr.Contains("U"))
                        {
                            for (int i = 0; i < moveCount; i++)
                            {
                                image1.MoveZoomWindowUp();
                                image2?.MoveZoomWindowUp();
                            }
                        }
                        else if (moveStr.Contains("D"))
                        {
                            for (int i = 0; i < moveCount; i++)
                            {
                                image1.MoveZoomWindowDown();
                                image2?.MoveZoomWindowDown();
                            }
                        }
                        if (moveStr.Contains("L"))
                        {
                            for (int i = 0; i < moveCount; i++)
                            {
                                image1.MoveZoomWindowLeft();
                                image2?.MoveZoomWindowLeft();
                            }
                        }
                        else if (moveStr.Contains("R"))
                        {
                            for (int i = 0; i < moveCount; i++)
                            {
                                image1.MoveZoomWindowRight();
                                image2?.MoveZoomWindowRight();
                            }
                        }
                    }
                    UpdateImageEffectsString();
                }
                // Set image effects.
                InitImageEffects();

                var cargoNames = InitCargoItems();
                var le = InitLabelEntity();
                if (cargoNames.Count > 0)
                    RollingImagesController.InitMarkerBoxTextRender(le.Width, le.Height, new Font(le.FontType, le.FontSize, System.Drawing.FontStyle.Bold), le.ForeColor, le.BackColor, cargoNames);

                RollingImagesController.StartService();
                PaintingRegionsService.Service.RollingImageProcessController = RollingImagesController;

                ImageDataController = new DisplayImageDataController(RollingImagesController);
                ImageDataController.Initialize();
                ImageDataController.TipMissed += ImageDataControllerOnTipMissed;
                ImageDataController.TipIdentified += ImageDataControllerOnTipIdentified;
                ImageDataController.SetIsShapeCorrection(true);//默认开启图像畸变矫正


                if (!ScannerConfig.Read(ConfigPath.LockConveyorReverseKey, out lockConveyorReverseKey))
                {
                    lockConveyorReverseKey = false;
                }

                if (!ScannerConfig.Read(ConfigPath.IsRemoveEdgePointsInUI,out IsShaped))
                {
                    IsShaped = true;
                }

                if (!lockConveyorReverseKey)
                {
                    _tickTimer = new System.Timers.Timer();
                    _tickTimer.Enabled = true;
                    _tickTimer.Interval = 100;
                    _tickTimer.Elapsed += _tickTimer_Elapsed;
                    _tickTimer.Start();
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception, "Unexpected exception in MainViewModel constructor");
            }

            Tracer.TraceExitFunc("UI.XRay.Security.Scanner.ViewModel.MainViewModel");

        }

        private void Controller_ResetCommand(object sender, bool e)
        {
            _isShieldedConveyorRight = false;
        }

        private void LoadImageSetting()
        {
            if(!ScannerConfig.Read(ConfigPath.ImageSESifenbianRegionLower, out double sESifenbianRegionLower))
            {
                sESifenbianRegionLower = 0.763;
                ScannerConfig.Write(ConfigPath.ImageSESifenbianRegionLower, sESifenbianRegionLower);
            }
            if (!ScannerConfig.Read(ConfigPath.ImageSESifenbianRegionUpper, out double sESifenbianRegionUpper))
            {
                sESifenbianRegionUpper = 0.9;
                ScannerConfig.Write(ConfigPath.ImageSESifenbianRegionUpper, sESifenbianRegionUpper);
            }
            if(!ScannerConfig.Read(ConfigPath.PreProcHistogramTestMode, out testMode))
            {
                testMode = false;
                ScannerConfig.Write(ConfigPath.PreProcHistogramTestMode, testMode);
            }

            RollingImagesController.Image1.SESifenbianRegionLower = sESifenbianRegionLower;
            RollingImagesController.Image1.SESifenbianRegionUpper = sESifenbianRegionUpper;
            if (RollingImagesController.Image2 != null)
            {
                RollingImagesController.Image2.SESifenbianRegionLower = sESifenbianRegionLower;
                RollingImagesController.Image2.SESifenbianRegionUpper = sESifenbianRegionUpper;
            }
        }

        ~MainViewModel()
        {
            ScannerConfig.ConfigChanged -= ScannerConfigOnConfigChanged;
            ControlService.ServicePart.ConveyorDirectionChanged -= ServicePart_ConveyorDirectionChanged;
            HttpNetworkController.Controller.RemoteTipPlanUpdateAction -= updateNetTipPlan; //yxc
            HttpNetworkController.Controller.ShowWindowAction -= showwindow;  //yxc
            HttpNetworkController.Controller.CloseWindowAction -= closewindow;  //yxc
        }

        public void showwindow()
        {

            Application.Current.Dispatcher.InvokeAsync(() => { MessengerInstance.Send(new OpenWindowMessage("MainWindow", "RemoteDiagnose")); });
            //给Mainwindow.xaml.cs发消息


        }

        public void closewindow()
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                this.MessengerInstance.Send(new CloseWindowMessage("RemoteDiagnose"));  //给RemoteDiagnose窗口发消息
                Transmission.IsRemoteDiagnosing = false;
            });

        }


        public void updateNetTipPlan()
        {
            try
            {
                Tracer.TraceInfo("MainViewModel update net tip plan");
                //获取数据库中的TipPlanImage并从数据库中删除
                List<TipPlanandImage> allplanandImages = new List<TipPlanandImage>(_imagecontroller.GetAllPlans());
                if (allplanandImages.Count > 0)
                {
                    foreach (var item in allplanandImages)
                        _imagecontroller.Remove(item);
                }
                //获取数据库中的TipPlan并从数据库中删除
                List<TipPlan> allplans = new List<TipPlan>(_controller.GetAllPlans());
                if (allplans.Count > 0)
                {
                    foreach (var item in allplans)
                        _controller.Remove(item);
                }
                Transmission.RemoteTipUpdated = true;
            }
            catch (Exception ex)
            {
                Tracer.TraceException(ex);
            }
            finally
            {
                Transmission.IsRemoteTipProcessing = false;
            }
        }

        /// <summary>
        /// 读取键盘类型
        /// </summary>
        /// <returns></returns>
        private void ReadKeyBoardType()
        {
            _isUSBCommonKeyboard = ReadConfigService.Service.IsUseUSBCommandKeyboard;
        }


        void _tickTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_isKeyboardReversed)
            {
                if (_currentDirection == MotorDirection.MoveRight)
                {
                    if (DateTime.Now - _rightPressedDateTime > _keyPressInterval)
                    {
                        _lastDirection = _currentDirection;
                        _currentDirection = MotorDirection.Stop;
                        //KeyEventArgs keyargs = new KeyEventArgs(null, null, 0, ScannerKeyboardPart.Keyboard.ConveyorStop);
                        //PreviewKeyDownCommandExecute(keyargs);

                        ImageDataController.OnConveyorStopRequest();
                        _lastMotorMoveTime = DateTime.Now;


                        MessengerInstance.Send(new MotorDirectionMessage(MotorDirection.Stop));
                    }
                }
            }
            else
            {
                if (_currentDirection == MotorDirection.MoveLeft)
                {
                    if (DateTime.Now - _leftPressedDateTime > _keyPressInterval)
                    {
                        _lastDirection = _currentDirection;
                        _currentDirection = MotorDirection.Stop;
                        //KeyEventArgs keyargs = new KeyEventArgs(null, null, 0, ScannerKeyboardPart.Keyboard.ConveyorStop);
                        //PreviewKeyDownCommandExecute(keyargs);

                        ImageDataController.OnConveyorStopRequest();
                        _lastMotorMoveTime = DateTime.Now;


                        MessengerInstance.Send(new MotorDirectionMessage(MotorDirection.Stop));
                    }
                }
            }
        }

        void Serviece_ControlConveyorAction(object sender, MotorDirection e)
        {
            switch (e)
            {
                case MotorDirection.Stop:
                    PreviewKeyDownCommandExecute(new KeyEventArgs(null, null, 0, ScannerKeyboardPart.Keyboard.ConveyorStop));
                    break;
                case MotorDirection.MoveLeft:
                    PreviewKeyDownCommandExecute(new KeyEventArgs(null, null, 0, ScannerKeyboardPart.Keyboard.ConveyorLeft));
                    break;
                case MotorDirection.MoveRight:
                    PreviewKeyDownCommandExecute(new KeyEventArgs(null, null, 0, ScannerKeyboardPart.Keyboard.ConveyorRight));
                    break;
                default:
                    break;
            }

        }

        void ServicePart_ConveyorDirectionChanged(object sender, Control.ConveyorDirectionChangedEventArgs e)
        {
            var status = e.New;
            if (status != Control.ConveyorDirection.Stop)
            {
                CloseSpecialPenetration(RollingImagesController);
            }
        }

        private void SendTipLog()
        {
            //var sessionSet = new WorkSessionDbSet();
            //var session = sessionSet.GetLast();
            if (LoginAccountManager.Service.CurrentAccount == null)
            { return; }
            UI.XRay.Flows.TRSNetwork.Models.TipLogStatisticResult tipLog = new Flows.TRSNetwork.Models.TipLogStatisticResult()
            {
                AccountId = LoginAccountManager.Service.CurrentAccount.ToString(),
                LoginDateTime = LoginAccountManager.Service.LoginTime,
                BagCount = BagCounterService.Service.SessionCount,
                TotalMarkCount = LoginAccountManager.Service.TotalMarkCount,
                TipInjectionCount = LoginAccountManager.Service.TipInjectionCount,
                MissTipCount = LoginAccountManager.Service.MissTipCount,
            };
            tipLog.MissRate = tipLog.TipInjectionCount > 0 ? (tipLog.MissTipCount / (double)tipLog.TipInjectionCount * 100) : 0;
            UI.XRay.Flows.TRSNetwork.NetCommandService.Instance.SendTipLog(tipLog);
        }

        #region Tip识别成功
        /// <summary>
        /// �¼���Ӧ���û����ǳ�һ��tipͼ�񣬷�����ʾ��Ϣ
        /// </summary>
        private void ImageDataControllerOnTipIdentified()
        {
            Messenger.Default.Send(new ShowFlyoutMessage("MainWindow",
                TranslationService.FindTranslation("Dangerous goods!Press Shutdown to clear before continuing."), MessageIcon.Information, 7));
            SendTipLog();
            ScannerKeyboardPart.Keyboard.StartBeep(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(70), 2);
        }
        #endregion

        #region Tip漏判
        /// <summary>
        /// �¼���Ӧ���û�����һ��Tipͼ��,������ʾ��Ϣ
        /// </summary>
        private void ImageDataControllerOnTipMissed()
        {
            Messenger.Default.Send(new ShowFlyoutMessage("MainWindow",
                TranslationService.FindTranslation("Missed dangerous goods!"), MessageIcon.Warning, 7));
            //Messenger.Default.Send(new ShowFlyoutMessage("MainWindow",
            //    TranslationService.FindTranslation("Please note that there is an unrecognized simulated dangerous goods image, Pressing the stop button will clear it."), MessageIcon.Warning, 7));
            SendTipLog();
            //增加声音报警 wjj
            ScannerKeyboardPart.Keyboard.StartBeep(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(70), 2);
        }
        #endregion

        /// <summary>
        /// �Ŵ����������仯
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="f"></param>
        private void RollingImagesControllerOnZoomFactorChanged(object sender, float f)
        {
            UpdateImageEffectsString(true);
        }

        /// <summary>
        /// ���÷����仯�����¿��ݼ�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void ScannerConfigOnConfigChanged(object sender, EventArgs eventArgs)
        {
            try
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    //LoadFunctionKeys();
                    LoadImageGeneralSettings();
                });
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        /// <summary>
        /// �������м��ؿ��ݼ��Ķ���
        /// </summary>
        private void LoadFunctionKeys()
        {
            try
            {
                if (!ScannerConfig.Read(ConfigPath.KeyboardIsFuctionKeyActionEnable, out _isActionKeyEnable))
                {
                    _isActionKeyEnable = false;
                }


                ImageEffectsComposition composition;
                string effectsCompositions = LoginAccountManager.Service.CurrentAccount.EffectsCompositions;
                string[] e = effectsCompositions.Split(';');

                F1EffectsComposition = ImageEffectsComposition.TryParse(e[0], out composition)
                    ? composition
                    : new ImageEffectsComposition();


                F2EffectsComposition = ImageEffectsComposition.TryParse(e[1], out composition)
                    ? composition
                    : new ImageEffectsComposition();


                F3EffectsComposition = ImageEffectsComposition.TryParse(e[2], out composition)
                    ? composition
                    : new ImageEffectsComposition();



                if (_isActionKeyEnable)
                {
                    string actionTypes = LoginAccountManager.Service.CurrentAccount.ActionTypes;
                    string[] a = actionTypes.Split(';');
                    if(a.Length>0)                        
                    {
                        ActionKey keyAction;
                        //if (!ScannerConfig.Read(ConfigPath.KeyboardF1ActionType, out keyActionStr))
                        //{
                        //    keyActionStr = String.Empty;
                        //}

                        if (!Enum.TryParse(a[0], out keyAction))
                        {
                            F1KeyAction = null;
                        }
                        else
                        {
                            F1KeyAction = keyAction;
                        }

                        //if (!ScannerConfig.Read(ConfigPath.KeyboardF2ActionType, out keyActionStr))
                        //{
                        //    keyActionStr = String.Empty;
                        //}

                        if (!Enum.TryParse(a[1], out keyAction))
                        {
                            F2KeyAction = null;
                        }
                        else
                        {
                            F2KeyAction = keyAction;
                        }

                        //if (!ScannerConfig.Read(ConfigPath.KeyboardF3ActionType, out keyActionStr))
                        //{
                        //    keyActionStr = String.Empty;
                        //}

                        if (!Enum.TryParse(a[2], out keyAction))
                        {
                            F3KeyAction = null;
                        }
                        else
                        {
                            F3KeyAction = keyAction;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        /// <summary>
        /// ��ʼ��ͼ��1��ͼ��2����Ч
        /// </summary>
        private void InitImageEffects()
        {
            var image1 = RollingImagesController.Image1;
            var image2 = RollingImagesController.Image2;

            if (image1 != null && _image1DefaultSetting != null)
            {
                image1.AbsorptivityIndex = _image1DefaultSetting.Absorbtivity;
                image1.IsEdgeEnhanceEnabled = false;
                image1.IsSuperEnhanceEnabled = _image1DefaultSetting.SuperEnhance;
                image1.ColorMode = _image1DefaultSetting.ColorMode;
                image1.PenetrationMode = _image1DefaultSetting.Penetration;
                image1.IsInversed = _image1DefaultSetting.Inversed;

                _image1LastEffectsComposition = new ImageEffectsComposition(image1.ColorMode, image1.PenetrationMode,
                    image1.IsInversed, image1.IsSuperEnhanceEnabled);
            }

            if (image2 != null && _image2DefaultSetting != null)
            {
                image2.AbsorptivityIndex = _image2DefaultSetting.Absorbtivity;
                image2.IsEdgeEnhanceEnabled = false;
                image2.IsSuperEnhanceEnabled = _image2DefaultSetting.SuperEnhance;
                image2.ColorMode = _image2DefaultSetting.ColorMode;
                image2.PenetrationMode = _image2DefaultSetting.Penetration;
                image2.IsInversed = _image2DefaultSetting.Inversed;

                _image2LastEffectsComposition = new ImageEffectsComposition(image2.ColorMode, image2.PenetrationMode,
                    image2.IsInversed, image2.IsSuperEnhanceEnabled);
            }
        }

        private List<string> InitCargoItems()
        {
            List<string> cargoName = new List<string>();
            var cargoList = AIJudgeImageService.Service.ReadDangerItems();
            var drug = new DangerEntity(100, "drug", "FFFF0000", 0.5);
            var unpenetratable = new DangerEntity(101, "unPenetratable", "FFFF0000", 0.5);
            var explosives = new DangerEntity(102, "explosives", "FFFF0000", 0.5);
            cargoList.Add(drug);
            cargoList.Add(unpenetratable);
            cargoList.Add(explosives);
            if (cargoList != null)
            {
                foreach (var cargo in cargoList)
                {
                    cargoName.Add(TranslationService.FindTranslation("Danger", cargo.Name));
                }
            }

            return cargoName;
        }
        private LabelEntity InitLabelEntity()
        {
            LabelEntity le = new LabelEntity();
            le.Width = 100;
            le.Height = 30;
            le.FontSize = 18;
            le.FontType = "Calibri";
            le.ForeColor = Color.FromArgb(Convert.ToInt32("FFFF0000", 16));
            le.BackColor = Color.FromArgb(Convert.ToInt32("00FFFFFF", 16));
            int value;
            if (ScannerConfig.Read(ConfigPath.AutoDetectionLabelWidth, out value))
            {
                le.Width = value;
            }
            if (ScannerConfig.Read(ConfigPath.AutoDetectionLabelHeight, out value))
            {
                le.Height = value;
            }
            if (ScannerConfig.Read(ConfigPath.AutoDetectionLabelFontSize, out value))
            {
                le.FontSize = value;
            }
            string fonttype = "";
            if (ScannerConfig.Read(ConfigPath.AutoDetectionLabelFontType, out fonttype))
            {
                le.FontType = fonttype;
            }
            string color = "";
            if (ScannerConfig.Read(ConfigPath.AutoDetectionLabelForeColor, out color))
            {
                le.ForeColor = Color.FromArgb(Convert.ToInt32(color, 16));
            }
            if (ScannerConfig.Read(ConfigPath.AutoDetectionLabelBackColor, out color))
            {
                le.BackColor = Color.FromArgb(Convert.ToInt32(color, 16));
            }
            return le;
        }
        struct LabelEntity
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public string FontType { get; set; }
            public int FontSize { get; set; }
            public Color ForeColor { get; set; }
            public Color BackColor { get; set; }
        }

        /// <summary>
        /// ����ͼ����Ĭ��������Ϣ
        /// </summary>
        private void LoadImageGeneralSettings()
        {
            try
            {
                _image1DefaultSetting = ImageGeneralSetting.LoadImage1Setting();

                
                int imagesCount;

                if (!ScannerConfig.Read(ConfigPath.ImagesCount, out imagesCount))
                {
                    imagesCount = 1;
                }

                if (imagesCount > 1)
                {
                    _image2DefaultSetting = ImageGeneralSetting.LoadImage2Setting();
                }

                if (!ScannerConfig.Read(ConfigPath.KeyboardIsKeyboardReversed, out _isKeyboardReversed))
                {
                    _isKeyboardReversed = false;
                }

                if (!ScannerConfig.Read(ConfigPath.ImagesZoom1XWhenStart, out _zoom1XWhenStart))
                {
                    _zoom1XWhenStart = false;
                }

                if (!ScannerConfig.Read(ConfigPath.KeyboardBackwardKeydownCountThr, out _backwardKeydownCountThr))
                {
                    _backwardKeydownCountThr = 6;
                }

                if (!ScannerConfig.Read(ConfigPath.ImageBrighterAbsorptivity, out _imageBrighterAbsorp))
                {
                    _imageBrighterAbsorp = -15;
                }

                if (!ScannerConfig.Read(ConfigPath.ImageDarkerAbsorptivity, out _imageDarkerAbsorp))
                {
                    _imageDarkerAbsorp = 25;
                }

                if (!ScannerConfig.Read(ConfigPath.ImageCanMoveWhenZoomIn, out _canMoveZoomWhenMagnify))
                {
                    _canMoveZoomWhenMagnify = false;
                    ScannerConfig.Write(ConfigPath.ImageCanMoveWhenZoomIn, _canMoveZoomWhenMagnify);
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        private void CreateCommands()
        {
            PreviewKeyDownCommand = new RelayCommand<KeyEventArgs>(PreviewKeyDownCommandExecute);
            PreviewKeyUpCommand = new RelayCommand<KeyEventArgs>(PreviewKeyUpCommandExecute);
            MouseRightButtonDownEventCommand = new RelayCommand<MouseButtonEventArgs>(MouseRightButtonDownEventCommandExecute);
            OnMouseDownEventCommand = new RelayCommand<MouseButtonEventArgs>(OnMouseDownEventCommandExecute);
            ClosingEventCommand = new RelayCommand(ClosingEventCommandExecute);
            LoadedEventCommand = new RelayCommand(LoadedEventCommandExecute);
        }

        private void OnMouseDownEventCommandExecute(MouseButtonEventArgs obj)
        {
            if (obj.ChangedButton == MouseButton.Middle)
            {
                var image1 = RollingImagesController.Image1;
                var image2 = RollingImagesController.Image2;

                //�����������ǷŴ󾵹���
                _isMagnifyKeyDown = !_isMagnifyKeyDown;
                //���������˷Ŵ󾵹��ܼ�
                if (_isMagnifyKeyDown)
                {
                    // ȡ�����ţ�����ͼ���������ţ���Ӱ���Ŵ�����ʾЧ��
                    Zoom1X(image1, image2);

                    RollingImagesController.EnableLocalZoom = true;
                }
                else
                {
                    //�ڶ��ε����Ŵ󾵹��ܼ�
                    RollingImagesController.EnableLocalZoom = false;
                    Zoom1X(image1, image2);
                }
            }
        }

        private void RegisterMessengers()
        {
            Messenger.Default.Register<ClearUpTipSelectedMessage>(this, ClearUpTipSelectedAction);
            // ע����ʹ�����ڽ��ջط�ͼ���б�
            Messenger.Default.Register<PlaybackImageRecordsMessage>(this, (msg) =>
            {
                if (msg != null)
                {
                    try
                    {
                        Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                ImageDataController.BeginPlayback(msg.Records);
                            });

                    }
                    catch (Exception exception)
                    {
                        Tracer.TraceException(exception);
                        //throw;
                    }
                }
            });

            Messenger.Default.Register<LogoutMessage>(this, (msg) => this.ImageDataController.Controller_TipInjectUpdateAction());

            Messenger.Default.Register<EmergencyMessage>(this, (msg) =>
            {
                _currentDirection = MotorDirection.Stop;
                ImageDataController.ExitInterruptRecovering();
            });
            Messenger.Default.Register<LoadKeyFunctionMessage>(this, (msg) => LoadFunctionKeys());

        }

        private void ClearUpTipSelectedAction(ClearUpTipSelectedMessage obj)
        {
            if (ImageDataController != null)
                try
                {
                    ImageDataController.ClearTipSelected();
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception);
                    //throw;
                }
        }

        /// <summary>
        /// �����Ѿ��������ϣ���ʾ��½���ڻ��Զ���¼
        /// </summary>
        private async void LoadedEventCommandExecute()
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                Tracer.TraceInfo("Try to login automatically.");
                AutoLoginController.TryAutoLogin();

                if (!LoginAccountManager.Service.HasLogin)
                {
                    Thread.Sleep(300);
                    ShowLoginWindow();
                }
                else
                {
                    bool isWorkReminder;
                    if (!ScannerConfig.Read(ConfigPath.IsWorkIntervalReminder, out isWorkReminder))
                    {
                        isWorkReminder = false;
                    }
                    TimeIntervalEnum timeIntervalEnum;
                    if (!ScannerConfig.Read(ConfigPath.WorkReminderTime, out timeIntervalEnum))
                    {
                        timeIntervalEnum = TimeIntervalEnum.HalfHour;
                    }
                    Messenger.Default.Send<WorkReminderChangedMessage>(new WorkReminderChangedMessage(timeIntervalEnum, isWorkReminder));
                    Tracer.TraceInfo("Auto login Success.");
                }

                MessengerInstance.Send(new LoadKeyFunctionMessage());

                #region Init DAQ System
#pragma warning disable CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
                Task.Run(() =>
                {
                    // 启动数采程序
                    try
                    {
                        if (!CaptureService.ServicePart.Open())
                        {
                            Tracer.TraceError("Failed to open image capture device.");
                        }
                        else
                        {
                            if (!CaptureService.ServicePart.StartCapture())
                            {
                                Tracer.TraceError("Failed to call StartCapture for image capture device.");
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        Tracer.TraceException(exception, "Failed to start image capture service on app start.");
                    }
                });
#pragma warning restore CS4014 // 由于此调用不会等待，因此在调用完成前将继续执行当前方法
                #endregion

                if (IsTimeToRemind())
                {
                    ShowRegularRemindWindow();
                    Properties.Settings.Default.HasMaintenanceReminded = true;
                    Properties.Settings.Default.Save();
                }

                if (!ScannerConfig.Read(ConfigPath.IsFlowLineMode, out isFlowLineMode))
                {
                    isFlowLineMode = false;
                }

                if (!isFlowLineMode)
                {
                    await Application.Current.Dispatcher.InvokeAsync(CleanTunnel);
                    await Application.Current.Dispatcher.InvokeAsync(CheckAndWarmupXRayGen);
                }
                else
                {
                    if (!ScannerConfig.Read(ConfigPath.EntryConveyorTriggerSensorIndex, out int entryESPEIndex))
                    {
                        entryESPEIndex = 2;
                    }
                    ControlService.ServicePart.PowerOnPESensors(true, entryESPEIndex);
                }

                // ��¼���ɺ󣬿�ʼУ�����׺�����
                ShowCalibrationWindow();
            });
        }

        /// <summary>
        /// 维护保养提示
        /// </summary>
        /// <returns></returns>
        private bool IsTimeToRemind()
        {
            bool isLoopRemind;
            if (!ScannerConfig.Read(ConfigPath.IsLoopRemind, out isLoopRemind))
            {
                isLoopRemind = false;
            }
            // 如果不是循环提示且已经提示过了，就不再提示
            if (!isLoopRemind && Properties.Settings.Default.HasMaintenanceReminded)
            {
                return false;
            }
            int timeInterval;
            if (!ScannerConfig.Read(ConfigPath.SystemRemindTimeInterval, out timeInterval))
            {
                timeInterval = -1;
            }
            if (timeInterval < 0)
            {
                return false;
            }

            long lastTime;
            if (!ScannerConfig.Read(ConfigPath.LastMaintenanceTime, out lastTime))
            {
                lastTime = DateTime.Now.Ticks;
            }

            if (lastTime == 0)
            {
                lastTime = DateTime.Now.Ticks;
            }

            var lastTimeDataTime = new DateTime(lastTime);
            var ts = DateTime.Now - lastTimeDataTime;

            if (ts >= TimeSpan.FromDays(timeInterval))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private void LoadXGenSetting()
        {
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
            if(!ScannerConfig.Read(ConfigPath.ColorBlockParam1,out _colorBlockParam1))
            {
                _colorBlockParam1 = 0.0381f;
            }
            if (!ScannerConfig.Read(ConfigPath.ColorBlockParam2, out _colorBlockParam2))
            {
                _colorBlockParam2 = 0.0468f;
            }
            if (!ScannerConfig.Read(ConfigPath.ColorBlockParam3, out _colorBlockParam3))
            {
                _colorBlockParam3 = 5.0f;
            }
            if (!ScannerConfig.Read(ConfigPath.ColorBlockParam4, out _colorBlockParam4))
            {
                _colorBlockParam4 = 0.003f;
            }
            if (!ScannerConfig.Read(ConfigPath.SystemEnableShiftKey, out _isEnableShiftKey))
            {
                _isEnableShiftKey = true;
            }
        }
        private void CleanTunnel()
        {
            MessengerInstance.Send(new OpenWindowMessage("MainWindow", "CleanTunnelWindow"));
        }

        /// <summary>
        /// �����Ƿ���ҪԤ�Ȳ�ִ��Ԥ��
        /// </summary>
        private void CheckAndWarmupXRayGen()
        {
            var duration = XRayGenWarmupHelper.GetWarmupDuration();
            if (duration != null)
            {
                // ��ʾԤ�ȴ��ڣ���ʼԤ��
                MessengerInstance.Send(new OpenWindowMessage("MainWindow", "XRayGenWarmupWindow"));
            }
            else
            {
                XRayGenSettingController _settingController = new XRayGenSettingController();
                _settingController.ChangeXRayGenSettings(xrayKV, xrayMA, XRayGeneratorIndex.XRayGenerator1);
                if (genCount > 1)
                {
                    _settingController.ChangeXRayGenSettings(xrayKV2, xrayMA2, XRayGeneratorIndex.XRayGenerator2);
                }
            }
        }

        /// <summary>
        /// ���������ڹر��¼�������ֹͣͼ�����Ʒ���
        /// </summary>
        private void ClosingEventCommandExecute()
        {
            if (ImageDataController != null)
            {
                ImageDataController.Cleanup();
            }

            if (RollingImagesController != null)
            {
                RollingImagesController.StopService();
            }
        }

        /// <summary>
        /// �������ס����ȱ궨����
        /// </summary>
        private void ShowCalibrationWindow()
        {
            MessengerInstance.Send(new OpenWindowMessage("MainWindow", "CalibrateWindow"));
        }

        /// <summary>
        /// ��ʱ���Ѵ���
        /// </summary>
        private void ShowRegularRemindWindow()
        {
            MessengerInstance.Send(new OpenWindowMessage("MainWindow", "RegularRemindWindow"));
        }

        private void ShowLoginWindow()
        {
            MessengerInstance.Send(new OpenWindowMessage("MainWindow", "LoginWindow"));
        }

        private int keyConuter = 0;

        /// <summary>
        /// �û����¹��ܼ�F1��F2��F3
        /// </summary>
        /// <param name="key"></param>
        private void PreviewFunctionKeyDown(Key key)
        {
            ImageEffectsComposition compostion = null;
            ActionKey? actionType = null;

            compostion = F1EffectsComposition;
            actionType = F1KeyAction;

            if (key == Key.F2)
            {
                compostion = F2EffectsComposition;
                actionType = F2KeyAction;
            }
            else if (key == Key.F3)
            {
                compostion = F3EffectsComposition;
                actionType = F3KeyAction;
            }

            if (actionType != null)
            {
                switch (actionType)
                {
                    case ActionKey.ImagePenetration:
                        {
                            if (ImageDataController.IsScanning || (ImageDataController.IsTrainingMode && ImageDataController.IsTraining) || !ImageDataController.IsSendDataCompleted)
                            {
                                return;
                            }
                            var image1 = RollingImagesController.Image1;
                            var image2 = RollingImagesController.Image2;

                            if (!testMode)
                            {
                                if (_isEnhanced && !_isImageHistogram)
                                {
                                    image1.IsSuperEnhanceEnabled = false;
                                    if (image2 != null)
                                    {
                                        image2.IsSuperEnhanceEnabled = image1.IsSuperEnhanceEnabled;
                                    }
                                    ImageDataController.SetIsShowScreenWithOriginXRayData(true);
                                    _isEnhanced = false;
                                }
                                else if (!_isEnhanced && !_isImageHistogram)
                                {
                                    _isEnhanced = false;
                                    _isImageHistogram = true;
                                    ImageDataController.SetImageHistogramEquation(_isImageHistogram);
                                }
                                else
                                {
                                    image1.IsSuperEnhanceEnabled = false;
                                    if (image2 != null)
                                    {
                                        image2.IsSuperEnhanceEnabled = image1.IsSuperEnhanceEnabled;
                                    }
                                    ImageDataController.SetIsShowScreenWithOriginXRayData(false);
                                    _isEnhanced = true;
                                    _isImageHistogram = false;
                                }
                            }
                            else
                            {
                                if (!_isImageHistogram)
                                {
                                    _isEnhanced = false;
                                    image1.IsSuperEnhanceEnabled = false;
                                    if (image2 != null)
                                    {
                                        image2.IsSuperEnhanceEnabled = image1.IsSuperEnhanceEnabled;
                                    }
                                    _isImageHistogram = true;
                                    //ImageDataController.SetIsShowScreenWithOriginXRayData(true);
                                    ImageDataController.NewSetImageHistogramEquation(_isImageHistogram);
                                }
                                else
                                {
                                    image1.IsSuperEnhanceEnabled = false;
                                    if (image2 != null)
                                    {
                                        image2.IsSuperEnhanceEnabled = image1.IsSuperEnhanceEnabled;
                                    }
                                    ImageDataController.SetIsShowScreenWithOriginXRayData(false);
                                    _isEnhanced = true;
                                    _isImageHistogram = false;
                                }
                            }

                            break;
                        }
                    case ActionKey.StartShapeCorrection:
                        {
                            if (ImageDataController.IsScanning || (ImageDataController.IsTrainingMode && ImageDataController.IsTraining) || !ImageDataController.IsSendDataCompleted)
                            {
                                return;
                            }
                            if (ImageDataController != null)
                            {
                                //开启图像矫正
                                ImageDataController.SetIsShapeCorrection(true);
                                IsShaped = true;
                            }
                            break;
                        }
                    case ActionKey.StopShapeCorrection:
                        {
                            if (ImageDataController.IsScanning || (ImageDataController.IsTrainingMode && ImageDataController.IsTraining) || !ImageDataController.IsSendDataCompleted)
                            {
                                return;
                            }
                            if (ImageDataController != null)
                            {
                                //关闭图像矫正
                                ImageDataController.SetIsShapeCorrection(false);
                                IsShaped = false;
                            }
                            break;
                        }
                    case ActionKey.ImageBrighter:
                        {
                            var image1 = RollingImagesController.Image1;
                            var image2 = RollingImagesController.Image2;
                            if (image1.AbsorptivityIndex == _imageBrighterAbsorp)
                            {
                                image1.AbsorptivityIndex = 0;
                                if (image2 != null)
                                {
                                    image2.AbsorptivityIndex = image1.AbsorptivityIndex;
                                }
                            }
                            else
                            {
                                image1.AbsorptivityIndex = _imageBrighterAbsorp;
                                if (image2 != null)
                                {
                                    image2.AbsorptivityIndex = image1.AbsorptivityIndex;
                                }
                            }
                            break;
                        }
                    case ActionKey.ImageDarker:
                        {
                            var image1 = RollingImagesController.Image1;
                            var image2 = RollingImagesController.Image2;
                            if (image1.AbsorptivityIndex == _imageDarkerAbsorp)
                            {
                                image1.AbsorptivityIndex = 0;
                                if (image2 != null)
                                {
                                    image2.AbsorptivityIndex = image1.AbsorptivityIndex;
                                }
                            }
                            else
                            {
                                image1.AbsorptivityIndex = _imageDarkerAbsorp;
                                if (image2 != null)
                                {
                                    image2.AbsorptivityIndex = image1.AbsorptivityIndex;
                                }
                            }
                            break;
                        }
                }
                UpdateImageEffectsString();
                return;
            }

            if (compostion != null)
            {
                if (!_isToggleKeyDown)
                {
                    // �״μ��⵽������Ϣ����¼��ֵ��ʱ��
                    _isToggleKeyDown = true;
                    _lastToggleKey = key;
                    _lastToggleKeyDownTime = DateTime.Now;

                    var image1 = RollingImagesController.Image1;
                    var image2 = RollingImagesController.Image2;

                    // ���浱ǰ��ͼ����Ч
                    CacheCurrentImageEffects();

                    // ���ݹ��ܼ��е���Ч���ã�Ӧ����Ӧ����Ч
                    if (compostion.ColorMode != null)
                    {
                        if (image1 != null)
                        {
                            image1.ColorMode = compostion.ColorMode.Value;
                        }

                        if (image2 != null)
                        {
                            image2.ColorMode = compostion.ColorMode.Value;
                        }
                    }

                    if (compostion.Penetration != null)
                    {
                        if (image1 != null)
                        {
                            image1.PenetrationMode = compostion.Penetration.Value;
                        }

                        if (image2 != null)
                        {
                            image2.PenetrationMode = compostion.Penetration.Value;
                        }
                    }

                    if (compostion.IsInversed != null)
                    {
                        if (image1 != null)
                        {
                            image1.IsInversed = compostion.IsInversed.Value;
                        }

                        if (image2 != null)
                        {
                            image2.IsInversed = compostion.IsInversed.Value;
                        }
                    }

                    if (compostion.IsSenEnabled != null)
                    {
                        keyConuter++;
                        if (keyConuter >= int.MaxValue)
                            keyConuter = 1;
                        if (keyConuter % 2 == 0)
                        {
                            if (image1 != null)
                            {
                                image1.IsSuperEnhanceEnabled = compostion.IsSenEnabled.Value;
                            }

                            if (image2 != null)
                            {
                                image2.IsSuperEnhanceEnabled = compostion.IsSenEnabled.Value;
                            }
                        }
                        else
                        {
                            ImageEffectRestoration(image1, image2);
                        }
                    }
                    UpdateImageEffectsString();
                }
            }
        }

        /// <summary>
        /// ���浱ǰ��ͼ����Ч
        /// </summary>
        private void CacheCurrentImageEffects()
        {
            var image1 = RollingImagesController.Image1;
            var image2 = RollingImagesController.Image2;

            if (image1 != null)
            {
                _image1LastEffectsComposition.ColorMode = image1.ColorMode;
                _image1LastEffectsComposition.IsInversed = image1.IsInversed;
                _image1LastEffectsComposition.Penetration = image1.PenetrationMode;
                _image1LastEffectsComposition.IsSenEnabled = image1.IsSuperEnhanceEnabled;
            }

            if (image2 != null)
            {
                _image2LastEffectsComposition.ColorMode = image2.ColorMode;
                _image2LastEffectsComposition.IsInversed = image2.IsInversed;
                _image2LastEffectsComposition.Penetration = image2.PenetrationMode;
                _image2LastEffectsComposition.IsSenEnabled = image2.IsSuperEnhanceEnabled;
            }
        }

        /// <summary>
        /// �ָ�ͼ������Ч��֮ǰ��״̬
        /// </summary>
        private void RestoreImageEffects()
        {
            var image1 = RollingImagesController.Image1;
            var image2 = RollingImagesController.Image2;

            if (image1 != null)
            {
                if (_image1LastEffectsComposition.ColorMode != null)
                {
                    image1.ColorMode = _image1LastEffectsComposition.ColorMode.Value;
                }

                if (_image1LastEffectsComposition.IsInversed != null)
                {
                    image1.IsInversed = _image1LastEffectsComposition.IsInversed.Value;
                }

                if (_image1LastEffectsComposition.Penetration != null)
                {
                    image1.PenetrationMode = _image1LastEffectsComposition.Penetration.Value;
                }

                if (_image1LastEffectsComposition.IsSenEnabled != null)
                {
                    image1.IsSuperEnhanceEnabled = _image1LastEffectsComposition.IsSenEnabled.Value;
                }
            }

            if (image2 != null)
            {
                if (_image2LastEffectsComposition.ColorMode != null)
                {
                    image2.ColorMode = _image2LastEffectsComposition.ColorMode.Value;
                }

                if (_image2LastEffectsComposition.IsInversed != null)
                {
                    image2.IsInversed = _image2LastEffectsComposition.IsInversed.Value;
                }

                if (_image2LastEffectsComposition.Penetration != null)
                {
                    image2.PenetrationMode = _image2LastEffectsComposition.Penetration.Value;
                }

                if (_image2LastEffectsComposition.IsSenEnabled != null)
                {
                    image2.IsSuperEnhanceEnabled = _image2LastEffectsComposition.IsSenEnabled.Value;
                }
            }
            UpdateImageEffectsString();
        }

        /// <summary>
        ///  ��������
        /// </summary>
        /// <param name="args"></param>
        private void PreviewKeyUpCommandExecute(KeyEventArgs args)
        {
            //记录按键信息
            RecordImageProcess(args.Key);

            if (_isToggleKeyDown)
            {
                _isToggleKeyDown = false;
                if (DateTime.Now - _lastToggleKeyDownTime >= TimeSpan.FromSeconds(0.7))
                {
                    // ���ذ�������1���Ӻ����������ָ������ذ�������֮ǰ��Ч��
                    if (IsFunctionKey(args.Key) || IsReversibleImageEffectsKey(args.Key))
                    {
                        RestoreImageEffects();
                    }
                    else if (IsReversibleToggleKey(args.Key))
                    {
                        if (args.Key == ScannerKeyboardPart.Keyboard.Auto)
                        {
                            ImageDataController.IsIntelliSenseEnabled = !ImageDataController.IsIntelliSenseEnabled;
                        }
                    }
                }
            }
            else
            {
                //if (_currentDirection == MotorDirection.Stop)
                //{
                //    var image1 = RollingImagesController.Image1;
                //    var image2 = RollingImagesController.Image2;

                //    if (args.Key == ScannerKeyboardPart.Keyboard.Left)
                //    {
                //        if (!image1.CanMoveZoomWindowLeft)
                //        {
                //            ImageDataController.PullLeftImageEnd();
                //        }

                //        args.Handled = true;
                //    }
                //    else if (args.Key == ScannerKeyboardPart.Keyboard.Right)
                //    {
                //        if (!image1.CanMoveZoomWindowLeft)
                //        {
                //            ImageDataController.PullRightImageEnd();
                //        }

                //        args.Handled = true;
                //    }

                //}
            }
        }

        /// <summary>
        /// �������濪�ذ���
        /// </summary>
        /// <param name="key"></param>
        private void PreviewReversibleToggleKeyDown(Key key)
        {
            var image1 = RollingImagesController.Image1;
            var image2 = RollingImagesController.Image2;

            if (!_isToggleKeyDown)
            {
                // �״μ��⵽������Ϣ����¼��ֵ��ʱ�̣�������
                _isToggleKeyDown = true;
                _lastToggleKey = key;
                _lastToggleKeyDownTime = DateTime.Now;

                if (key == ScannerKeyboardPart.Keyboard.Auto)
                {
                    if (!_isLocalManualInspection)
                    {
                        ImageDataController.IsIntelliSenseEnabled = !ImageDataController.IsIntelliSenseEnabled;
                    }
                    else
                    {
                        PaintingRegionsService.Service.SendManualJudgeResult(JudgeResult.Accept);
                    }
                }
                else if (key == ScannerKeyboardPart.Keyboard.VFlip)
                {
                    image1.VerticalFlip = !image1.VerticalFlip;
                    if (image2 != null)
                    {
                        image2.VerticalFlip = !image2.VerticalFlip;
                    }
                    RollingImagesController.VerticalFlip = image1.VerticalFlip;
                }
            }
        }

        private void PreviewReversibleImageEffectsKeyDown(Key key)
        {

            var image1 = RollingImagesController.Image1;
            var image2 = RollingImagesController.Image2;


            if (!_isToggleKeyDown)
            {
                // �״μ��⵽������Ϣ����¼��ֵ��ʱ�̣�������
                _isToggleKeyDown = true;
                _lastToggleKey = key;
                _lastToggleKeyDownTime = DateTime.Now;

                CacheCurrentImageEffects();



                if (key == ScannerKeyboardPart.Keyboard.Inverse)
                {
                    if (IsShiftKeyOn)
                    {
                        // TODO: TEST
                        image1.IsEnableColorBlock = !image1.IsEnableColorBlock;
                        if (image2 != null)
                        {
                            image2.IsEnableColorBlock = image1.IsEnableColorBlock;
                        }
                    }
                    else
                    {
                        image1.IsInversed = !image1.IsInversed;
                        if (image2 != null)
                        {
                            image2.IsInversed = image1.IsInversed;
                        }
                        UpdateImageEffectsString();
                    }
                }
                else if (key == ScannerKeyboardPart.Keyboard.HighPenetrate)
                {

                    if (IsShiftKeyOn)
                    {
                        if (image1.PenetrationMode == PenetrationMode.Standard)
                        {
                            image1.PenetrationMode = PenetrationMode.SuperPenetrate;
                        }
                        else if (image1.PenetrationMode == PenetrationMode.SuperPenetrate)
                        {
                            image1.PenetrationMode = PenetrationMode.SlicePenetrate;
                        }
                        else
                        {
                            image1.PenetrationMode = PenetrationMode.Standard;
                        }

                        if (image2 != null)
                        {
                            image2.PenetrationMode = image1.PenetrationMode;
                        }
                    }
                    else
                    {
                        if (image1.PenetrationMode == PenetrationMode.HighPenetrate)
                        {
                            image1.PenetrationMode = PenetrationMode.LowPenetrate;
                        }
                        else if (image1.PenetrationMode == PenetrationMode.LowPenetrate)
                        {
                            image1.PenetrationMode = PenetrationMode.Standard;
                        }
                        else
                        {
                            image1.PenetrationMode = PenetrationMode.HighPenetrate;
                        }

                        if (image2 != null)
                        {
                            image2.PenetrationMode = image1.PenetrationMode;
                        }
                    }
                    UpdateImageEffectsString();

                }
                else if (key == ScannerKeyboardPart.Keyboard.BlackWhite)
                {
                    {

                        image1.ColorMode = image1.ColorMode == DisplayColorMode.MaterialColor
                                                                    ? DisplayColorMode.Grey
                                                                    : DisplayColorMode.MaterialColor;
                        if (image2 != null)
                        {
                            image2.ColorMode = image1.ColorMode;
                        }
                        UpdateImageEffectsString();

                    }

                }
                else if (key == ScannerKeyboardPart.Keyboard.Z789)
                {

                    if (image1.ColorMode == DisplayColorMode.Zeff7)
                    {
                        image1.ColorMode = DisplayColorMode.Zeff8;
                    }
                    else if (image1.ColorMode == DisplayColorMode.Zeff8)
                    {
                        image1.ColorMode = DisplayColorMode.Zeff9;
                    }
                    else if (image1.ColorMode == DisplayColorMode.Zeff9)
                    {
                        image1.ColorMode = DisplayColorMode.Zeff7;
                    }
                    else
                    {
                        image1.ColorMode = DisplayColorMode.Zeff7;
                    }

                    if (image2 != null)
                    {
                        image2.ColorMode = image1.ColorMode;
                    }
                    UpdateImageEffectsString();

                }
                else if (key == ScannerKeyboardPart.Keyboard.Os)
                {
                    if (_isShiftKeyOn)
                    {
                        if (!ImageDataController.newEnable)
                        {
                            return;
                        }
                        if (ImageDataController.IsScanning || ImageDataController.IsTrainingMode || !ImageDataController.IsSendDataCompleted)
                        {
                            return;
                        }
                        if (!ImageDataController.IsInNomalMode || ImageDataController.InInterruptRecovering)
                        {
                            return;
                        }
                        if (!_isImageHistogram)
                        {
                            _isImageHistogram = true;
                            DateTime start = DateTime.Now;
                            ImageDataController.NewSetImageHistogramEquation(_isImageHistogram);
                            DateTime stop = DateTime.Now;
                            TimeSpan tspan = stop - start;
                            string time = tspan.TotalMilliseconds.ToString();
                            Tracer.TraceInfo("New his time:" + time);
                            UpdateImageEffectsString();
                        }
                        else
                        {
                            _isImageHistogram = false;
                            ImageDataController.SetIsShowScreenWithOriginXRayData(true);
                            UpdateImageEffectsString();
                        }

                    }
                    else
                    {

                        image1.ColorMode = DisplayColorMode.OS;
                        if (image2 != null)
                        {
                            image2.ColorMode = image1.ColorMode;
                        }
                        UpdateImageEffectsString();

                    }
                }
                else if (key == ScannerKeyboardPart.Keyboard.Ms)
                {
                    if (_isShiftKeyOn)
                    {
                        if (!ImageDataController.newEnable)
                        {
                            return;
                        }
                        if (ImageDataController.IsScanning || ImageDataController.IsTrainingMode || !ImageDataController.IsSendDataCompleted)
                        {
                            return;
                        }
                        if (!ImageDataController.IsInNomalMode || ImageDataController.InInterruptRecovering)
                        {
                            return;
                        }
                        if (!_isSifenbian)
                        {
                            _isSifenbian = true;
                            DateTime start = DateTime.Now;
                            ImageDataController.SetSifenbianPublic(_isSifenbian);
                            DateTime stop = DateTime.Now;
                            TimeSpan tspan = stop - start;
                            string time = tspan.TotalMilliseconds.ToString();
                            Tracer.TraceInfo("Sifenbian cost time:" + time);
                            UpdateImageEffectsString();
                        }
                        else
                        {
                            _isSifenbian = false;
                            ImageDataController.SetIsShowScreenWithOriginXRayData(true);
                            UpdateImageEffectsString();
                        }
                    }
                    else
                    {

                        image1.ColorMode = DisplayColorMode.MS;
                        if (image2 != null)
                        {
                            image2.ColorMode = image1.ColorMode;
                        }
                        UpdateImageEffectsString();

                    }
                }
                else if (key == ScannerKeyboardPart.Keyboard.Sen)
                {
                    //test
                    //if(ImageDataController.IsScanning || ImageDataController.IsTraining ||ImageDataController.IsPulling)
                    //{
                    //    image1.IsSuperEnhanceEnabled = !image1.IsSuperEnhanceEnabled;
                    //    if (image2 != null)
                    //    {
                    //        image2.IsSuperEnhanceEnabled = image1.IsSuperEnhanceEnabled;
                    //    }
                    //    UpdateImageEffectsString();
                    //    return;
                    //}

                    //if (_isImageHistogram == true && !_doubleEnhance)
                    //{
                    //    _doubleEnhance = true;
                    //    image1.IsSuperEnhanceEnabled = true;
                    //    if (image2 != null)
                    //    {
                    //        image2.IsSuperEnhanceEnabled = image1.IsSuperEnhanceEnabled;
                    //    }
                    //    UpdateImageEffectsString();
                    //    return;
                    //}
                    //else if(_isImageHistogram == true && _doubleEnhance)
                    //{
                    //    _doubleEnhance = false;
                    //    image1.IsSuperEnhanceEnabled = false;
                    //    if (image2 != null)
                    //    {
                    //        image2.IsSuperEnhanceEnabled = image1.IsSuperEnhanceEnabled;
                    //    }
                    //    UpdateImageEffectsString();
                    //    return;
                    //}

                    //if (image1.IsSuperEnhanceEnabled == false)
                    //{
                    //    ImageDataController.SetIsShowScreenWithOriginXRayData(true);
                    //    image1.IsSuperEnhanceEnabled = true;
                    //    if (image2 != null)
                    //    {
                    //        image2.IsSuperEnhanceEnabled = image1.IsSuperEnhanceEnabled;
                    //    }
                    //    _isEnhanced = false;
                    //}
                    //else
                    //{
                    //    ImageDataController.SetIsShowScreenWithOriginXRayData(false);
                    //    image1.IsSuperEnhanceEnabled = false;
                    //    if (image2 != null)
                    //    {
                    //        image2.IsSuperEnhanceEnabled = image1.IsSuperEnhanceEnabled;
                    //    }
                    //    _isEnhanced = true;
                    //}
                    //test end

                    image1.IsSuperEnhanceEnabled = !image1.IsSuperEnhanceEnabled;
                    if (image2 != null)
                    {
                        image2.IsSuperEnhanceEnabled = image1.IsSuperEnhanceEnabled;
                    }

                    UpdateImageEffectsString();

                }
            }
        }

        /// <summary>
        /// ���ݵ�ǰ�ķ����������������������Ŵ����ƶ�
        /// </summary>
        /// <param name="keyEventArgs"></param>
        private void MoveZoomWindowByDirectionKeysDown(KeyEventArgs keyEventArgs)
        {
            var image1 = RollingImagesController.Image1;
            var image2 = RollingImagesController.Image2;

            var left = keyEventArgs.KeyboardDevice.IsKeyDown(ScannerKeyboardPart.Keyboard.Left);
            var right = keyEventArgs.KeyboardDevice.IsKeyDown(ScannerKeyboardPart.Keyboard.Right);
            var up = keyEventArgs.KeyboardDevice.IsKeyDown(ScannerKeyboardPart.Keyboard.Up);
            var down = keyEventArgs.KeyboardDevice.IsKeyDown(ScannerKeyboardPart.Keyboard.Down);

            if (left)
            {
                image1.MoveZoomWindowLeft();
                if (image2 != null)
                {
                    image2.MoveZoomWindowLeft();
                }
            }

            if (right)
            {
                image1.MoveZoomWindowRight();
                if (image2 != null)
                {
                    image2.MoveZoomWindowRight();
                }
            }

            if (up)
            {
                image1.MoveZoomWindowUp();
                if (image2 != null)
                {
                    image2.MoveZoomWindowUp();
                }
            }

            if (down)
            {
                image1.MoveZoomWindowDown();
                if (image2 != null)
                {
                    image2.MoveZoomWindowDown();
                }
            }
        }

        private void MoveZoomWindowTwoDirection(Key key)
        {
            var image1 = RollingImagesController.Image1;
            var image2 = RollingImagesController.Image2;

            switch (key)
            {
                case Key.N:
                    image1.MoveZoomWindowLeft();
                    image1.MoveZoomWindowUp();
                    if (image2 != null)
                    {
                        image2.MoveZoomWindowLeft();
                        image2.MoveZoomWindowUp();
                    }
                    break;
                case Key.Y:
                    image1.MoveZoomWindowLeft();
                    image1.MoveZoomWindowDown();
                    if (image2 != null)
                    {
                        image2.MoveZoomWindowLeft();
                        image2.MoveZoomWindowDown();
                    }
                    break;
                case Key.Q:
                    image1.MoveZoomWindowRight();
                    image1.MoveZoomWindowUp();
                    if (image2 != null)
                    {
                        image2.MoveZoomWindowRight();
                        image2.MoveZoomWindowUp();
                    }
                    break;
                case Key.E:
                    image1.MoveZoomWindowRight();
                    image1.MoveZoomWindowDown();
                    if (image2 != null)
                    {
                        image2.MoveZoomWindowRight();
                        image2.MoveZoomWindowDown();
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// PreviewKeyDown ������������ͼ�񰴼���
        /// </summary>
        /// <param name="keyEventArgs"></param>
        private void PreviewKeyDownCommandExecute(KeyEventArgs keyEventArgs)
        {
            if (IsFunctionKey(keyEventArgs.Key))
            {
                PreviewFunctionKeyDown(keyEventArgs.Key);
                keyEventArgs.Handled = true;
                return;
            }

            if (IsReversibleImageEffectsKey(keyEventArgs.Key))
            {
                PreviewReversibleImageEffectsKeyDown(keyEventArgs.Key);
                keyEventArgs.Handled = true;
                return;
            }

            if (IsReversibleToggleKey(keyEventArgs.Key))
            {
                PreviewReversibleToggleKeyDown(keyEventArgs.Key);
                keyEventArgs.Handled = true;
                return;
            }

            var image1 = RollingImagesController.Image1;
            var image2 = RollingImagesController.Image2;

            var key = keyEventArgs.Key;
            if (key == ScannerKeyboardPart.Keyboard.ConveyorLeft || key == ScannerKeyboardPart.Keyboard.ConveyorRight)
            {
                if (_isKeyboardReversed)
                {
                    if (key != ScannerKeyboardPart.Keyboard.ConveyorRight)
                    {
                        _backwardKeyDownCount = 0;
                    }
                    if (DateTime.Now - _rightPressedDateTime > TimeSpan.FromSeconds(2))
                    {
                        _backwardKeyDownCount = 0;
                    }
                }
                else
                {
                    if (key != ScannerKeyboardPart.Keyboard.ConveyorLeft)
                    {
                        _backwardKeyDownCount = 0;
                    }
                    if (DateTime.Now - _leftPressedDateTime > TimeSpan.FromSeconds(2))
                    {
                        _backwardKeyDownCount = 0;
                    }
                }
            }

            // Shift�����£��л�Shift���ܿ���
            if (key == Key.LeftShift || key == Key.RightShift)
            {
                if (!_isEnableShiftKey) return;
                IsShiftKeyOn = !IsShiftKeyOn;
                _isUnPenerationRegion = false;
                _isEnhanced = true;
                //ImageDataController.SetIsShowScreenWithUnPenetrationXRayData(false);
                keyEventArgs.Handled = true;
                UpdateImageEffectsString();
            }
            else if (key == ScannerKeyboardPart.Keyboard.Shift)
            {
                if (!_isEnableShiftKey) return;
                IsShiftKeyOn = !IsShiftKeyOn;
                _isUnPenerationRegion = false;
                _isEnhanced = true;
                //ImageDataController.SetIsShowScreenWithUnPenetrationXRayData(false);
                keyEventArgs.Handled = true;
                UpdateImageEffectsString();
            }
            else if (key == ScannerKeyboardPart.Keyboard.Menu)
            {
                if (_currentDirection == MotorDirection.Stop && ImageDataController.IsInNomalMode)
                {
                    // �û����²˵�������ʾ���ô���
                    MessengerInstance.Send(new OpenWindowMessage("MainWindow", "SettingWindow",
                        new PageNavigation("SystemMenu", "MenuPage", "Menu")));
                }

                keyEventArgs.Handled = true;

            }
            else if (key == ScannerKeyboardPart.Keyboard.Ims)
            {
                if (_currentDirection == MotorDirection.Stop && ImageDataController.IsInNomalMode)
                {
                    MessengerInstance.Send(new OpenWindowMessage("MainWindow", "SettingWindow",
                                        new PageNavigation("ImageMenu", "ImsPage", "Image Management")));
                }

                keyEventArgs.Handled = true;
            }
            else if (key == ScannerKeyboardPart.Keyboard.ContinuousScan)
            {
                if (_currentDirection != MotorDirection.Stop || !ImageDataController.IsInNomalMode)
                {
                    keyEventArgs.Handled = true;
                    return;
                }
                bool isEnableContinousScan = false;
                //民航版需要屏蔽其他模式
                ScannerConfig.Read(ConfigPath.MachineContinuousScan, out isEnableContinousScan);

                if (isEnableContinousScan)
                {
                    if (ControlService.ServicePart.WorkMode == ScannerWorkMode.Regular)
                    {
                        ControlService.ServicePart.SetWorkMode(ScannerWorkMode.Continuous);
                    }
                    else
                    {
                        ControlService.ServicePart.SetWorkMode(ScannerWorkMode.Regular);
                    }
                }

                keyEventArgs.Handled = true;
            }
            else if (key == ScannerKeyboardPart.Keyboard.ZoomIn)
            {
                // �Ŵ���
                image1.ZoomIn();
                if (image2 != null)
                {
                    image2.ZoomIn();
                }
                keyEventArgs.Handled = true;
                UpdateImageEffectsString();
            }
            else if (key == ScannerKeyboardPart.Keyboard.ZoomOut)
            {
                // ��С
                image1.ZoomOut();
                if (image2 != null)
                {
                    image2.ZoomOut();
                }
                keyEventArgs.Handled = true;
                UpdateImageEffectsString();
            }
            else if (key == ScannerKeyboardPart.Keyboard.Zoom1X)
            {
                // ȡ������
                image1.Zoom(1.0f);
                if (image2 != null)
                {
                    image2.Zoom(1.0f);
                }
                keyEventArgs.Handled = true;
                UpdateImageEffectsString();
            }
            else if (key == ScannerKeyboardPart.Keyboard.Left)
            {
                var time1 = DateTime.Now;
                Tracer.TraceDebug($"[PullBackTimeoutTracked] Left Key Down");
                if (_currentDirection == MotorDirection.Stop)
                {
                    if (image1.CanMoveZoomWindowLeft && _canMoveZoomWhenMagnify)
                    {
                        MoveZoomWindowByDirectionKeysDown(keyEventArgs);
                    }
                    else
                    {
                        time1 = DateTime.Now;
                        CloseSpecialPenetration(RollingImagesController);
                        Tracer.TraceDebug($"[PullBackTimeoutTracked] CloseSpecialPenetration execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
                        time1 = DateTime.Now;
                        // �������Ŵ����Ѿ��ﵽ�����ߣ���ִ��ͼ������
                        ImageDataController.PullLeftImage();
                        Tracer.TraceDebug($"[PullBackTimeoutTracked] PullLeftImage execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
                    }
                    time1 = DateTime.Now;
                    keyEventArgs.Handled = true;
                    UpdateImageEffectsString();
                    Tracer.TraceDebug($"[PullBackTimeoutTracked] UpdateImageEffectsString execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
                    System.Threading.Thread.Sleep(200);
                }
            }
            else if (key == ScannerKeyboardPart.Keyboard.Right)
            {
                var time1 = DateTime.Now;
                Tracer.TraceDebug($"[PullBackTimeoutTracked] Right Key Down");
                if (_currentDirection == MotorDirection.Stop)
                {
                    if (image1.CanMoveZoomWindowRight && _canMoveZoomWhenMagnify)
                    {
                        MoveZoomWindowByDirectionKeysDown(keyEventArgs);
                    }
                    else
                    {
                        time1 = DateTime.Now;
                        CloseSpecialPenetration(RollingImagesController);
                        Tracer.TraceDebug($"[PullBackTimeoutTracked] CloseSpecialPenetration execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
                        time1 = DateTime.Now;
                        // �������Ŵ����Ѿ��ﵽ���ұߣ���ִ��ͼ��ǰ��
                        ImageDataController.PullRightImage();
                        Tracer.TraceDebug($"[PullBackTimeoutTracked] PullRightImage execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
                    }
                    time1 = DateTime.Now;
                    keyEventArgs.Handled = true;
                    UpdateImageEffectsString();
                    Tracer.TraceDebug($"[PullBackTimeoutTracked] UpdateImageEffectsString execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
                    System.Threading.Thread.Sleep(200);
                }
            }
            else if (image1.ZoomMultiples != 1.0f && key == ScannerKeyboardPart.Keyboard.Up)
            {
                MoveZoomWindowByDirectionKeysDown(keyEventArgs);
                keyEventArgs.Handled = true;
            }
            else if (image1.ZoomMultiples != 1.0f && key == ScannerKeyboardPart.Keyboard.Down)
            {
                MoveZoomWindowByDirectionKeysDown(keyEventArgs);
                keyEventArgs.Handled = true;
            }
            else if (key == ScannerKeyboardPart.Keyboard.ConveyorStop)
            {
                if (_isClearTip && ImageDataController.CanClearTip && !ImageDataController.IsTipInjecting)
                {
                    keyEventArgs.Handled = true;
                    ImageDataController.ImageDataUpdateController.ClearAndAppend(ImageDataController.CurrentScreenRawScanLines.ToList());
                    _isClearTip = false;
                    UpdateImageEffectsString();
                }
                else
                {
                    if (DateTime.Now - _lastMotorMoveTime < _keyPressInterval)
                    {
                        Tracer.TraceInfo("[Conveyor] Stop returned: key press interval less than threshold");
                        keyEventArgs.Handled = true;
                        return;
                    }

                    if (ImageDataController.InInterruptRecovering)
                    {
                        Tracer.TraceInfo("[Conveyor] Stop returned: InInterruptRecovering");
                        keyEventArgs.Handled = true;
                        return;
                    }
                    ImageDataController.OnConveyorStopRequest();
                    _currentDirection = MotorDirection.Stop;
                    _lastDirection = _currentDirection;

                    _lastMotorMoveTime = DateTime.Now;
                    if (LoginAccountManager.Service.HasLogin && LoginAccountManager.Service.CurrentAccount.Role == AccountRole.Operator)
                        _isClearTip = true;
                    else
                        _isClearTip = false;

                    MessengerInstance.Send(new MotorDirectionMessage(MotorDirection.Stop));
                    keyEventArgs.Handled = true;
                    UpdateImageEffectsString();
                }
            }
            else if (key == ScannerKeyboardPart.Keyboard.ConveyorLeft)
            {
                if (LoginAccountManager.Service.HasLogin && LoginAccountManager.Service.CurrentAccount.Role != AccountRole.Operator)
                    _isClearTip = false;
                if (_isClearTip && ImageDataController.HasTipInjectEntity && ImageDataController.CanClearTip && !ImageDataController.IsTipInjecting)
                {
                    Messenger.Default.Send(new ShowFlyoutMessage("MainWindow",
              TranslationService.FindTranslation("Press Shutdown to clear dangerous goods, then continue."), MessageIcon.Warning, 2));
                    keyEventArgs.Handled = true;
                    UpdateImageEffectsString();
                }
                else
                {
                    _leftPressedDateTime = DateTime.Now;
                    if (_currentDirection == MotorDirection.MoveLeft || DateTime.Now - _lastMotorMoveTime < _keyPressInterval)
                    {
                        keyEventArgs.Handled = true;
                        Tracer.TraceInfo("ConveyorLeft returned,_currentDirection is:", _currentDirection);
                        return;
                    }
                    if (ImageDataController.InInterruptRecovering)
                    {
                        keyEventArgs.Handled = true;
                        Tracer.TraceInfo("ConeyorLeft Returned: InInterruptRecovering");
                        return;
                    }
                    _isClearTip = false;

                    _leftPressedDateTime = DateTime.Now;

                    if (_currentDirection != MotorDirection.MoveLeft)
                    {
                        if (!_isKeyboardReversed && !lockConveyorReverseKey)
                        {
                            _backwardKeyDownCount++;

                            if (_backwardKeyDownCount < _backwardKeydownCountThr)
                            {
                                keyEventArgs.Handled = true;
                                return;
                            }
                        }

                        if (_currentDirection == MotorDirection.MoveRight)
                        {
                            ImageDataController.OnConveyorStopRequest();
                            _currentDirection = MotorDirection.Stop;
                            _lastDirection = _currentDirection;

                            _lastMotorMoveTime = DateTime.Now;

                            MessengerInstance.Send(new MotorDirectionMessage(MotorDirection.Stop));
                            keyEventArgs.Handled = true;
                            return;
                        }

                        Zoom1X(image1, image2);
                        ImageDataController.OnConveyorLeftKeyDown();

                        _lastMotorMoveTime = DateTime.Now;
                        CloseSpecialPenetration(RollingImagesController);
                        MessengerInstance.Send(new MotorDirectionMessage(MotorDirection.MoveLeft));

                        _currentDirection = MotorDirection.MoveLeft;
                        _lastDirection = _currentDirection;
                    }

                    keyEventArgs.Handled = true;
                    UpdateImageEffectsString();
                }
            }
            else if (key == ScannerKeyboardPart.Keyboard.ConveyorRight)
            {
                if (_isShieldedConveyorRight)
                {
                    Messenger.Default.Send(new ShowFlyoutMessage("MainWindow",
                        TranslationService.FindTranslation("可疑行李，等待复位！"), MessageIcon.Warning, 2));
                    return;
                }
                if (LoginAccountManager.Service.HasLogin && LoginAccountManager.Service.CurrentAccount.Role != AccountRole.Operator)
                    _isClearTip = false;
                if (_isClearTip && ImageDataController.HasTipInjectEntity && ImageDataController.CanClearTip && !ImageDataController.IsTipInjecting)
                {

                    Messenger.Default.Send(new ShowFlyoutMessage("MainWindow",
               TranslationService.FindTranslation("Press Shutdown to clear dangerous goods, then continue."), MessageIcon.Warning, 2));
                    keyEventArgs.Handled = true;
                    UpdateImageEffectsString();

                }
                else
                {
                    _rightPressedDateTime = DateTime.Now;
                    if (_currentDirection == MotorDirection.MoveRight || DateTime.Now - _lastMotorMoveTime < _keyPressInterval)
                    {
                        keyEventArgs.Handled = true;
                        Tracer.TraceInfo("ConveyorRight returned,_currentDirection is:", _currentDirection);
                        return;
                    }
                    if (ImageDataController.InInterruptRecovering)
                    {
                        keyEventArgs.Handled = true;
                        Tracer.TraceInfo("ConeyorRight Returned: InInterruptRecovering");
                        return;
                    }
                    _isClearTip = false;
                    _rightPressedDateTime = DateTime.Now;



                    if (_currentDirection != MotorDirection.MoveRight)
                    {
                        if (_isKeyboardReversed && !lockConveyorReverseKey)
                        {
                            _backwardKeyDownCount++;

                            if (_backwardKeyDownCount < _backwardKeydownCountThr)
                            {
                                keyEventArgs.Handled = true;
                                return;
                            }
                        }

                        if (_currentDirection == MotorDirection.MoveLeft)
                        {
                            ImageDataController.OnConveyorStopRequest();
                            _currentDirection = MotorDirection.Stop;
                            _lastDirection = _currentDirection;

                            _lastMotorMoveTime = DateTime.Now;

                            MessengerInstance.Send(new MotorDirectionMessage(MotorDirection.Stop));
                            keyEventArgs.Handled = true;
                            return;
                        }

                        Zoom1X(image1, image2);
                        ImageDataController.OnConveyorRightKeyDown();

                        _lastMotorMoveTime = DateTime.Now;
                        CloseSpecialPenetration(RollingImagesController);
                        MessengerInstance.Send(new MotorDirectionMessage(MotorDirection.MoveRight));

                        _currentDirection = MotorDirection.MoveRight;
                        _lastDirection = _currentDirection;
                    }

                    keyEventArgs.Handled = true;
                    UpdateImageEffectsString();
                }

            }
            else if (key == ScannerKeyboardPart.Keyboard.Mark)
            {
                var MarkerList = RollingImagesController.MarkerList;
                //var MarkerList = PaintingRegionsService.Service.MarkerList;
                if (_isLocalManualInspection)
                {
                    // 提示可疑行李
                    Messenger.Default.Send(new ShowFlyoutMessage("MainWindow",
                        TranslationService.FindTranslation("可疑包裹!"), MessageIcon.Information, 7));
                    try
                    {
                        PaintingRegionsService.Service.SendManualJudgeResult(JudgeResult.Reject);
                        _isShieldedConveyorRight = true;
                    }
                    catch (Exception e)
                    {
                        Tracer.TraceException(e);
                    }
                }
                else
                {
                    if (MarkerList != null && MarkerList.Count > 0)
                    {
                        Task.Run(() =>
                        {
                            PaintingRegionsService.Service.SavePaintingRectangles();
                        });

                        ImageDataController.OnMarkKeyDown(MarkerList);
                    }
                }

                keyEventArgs.Handled = true;
                //if (!ImageDataController.OnMarkKeyDown())
                //{
                //    // Tip����ʧ�ܣ�����ʾ��Ļͼ����������
                //    //ShowScreenImgOprWindow();
                //}
                //PaintingRegionsService.Service.SavePaintingRectangles();
            }
            else if (key == ScannerKeyboardPart.Keyboard.Save)
            {
                ShowScreenImgOprWindow();

            }
            else if (key == ScannerKeyboardPart.Keyboard.Esc)
            {
                ImageEffectRestoration(image1,image2);
                keyEventArgs.Handled = true;
            }
            else if (key == ScannerKeyboardPart.Keyboard.IncreaseAbsorb)
            {
                image1.AbsorptivityIndex++;
                if (image2 != null)
                {
                    image2.AbsorptivityIndex = image1.AbsorptivityIndex;
                }
                keyEventArgs.Handled = true;
                UpdateImageEffectsString();
            }
            else if (key == ScannerKeyboardPart.Keyboard.DecreaseAbsorb)
            {
                image1.AbsorptivityIndex--;
                if (image2 != null)
                {
                    image2.AbsorptivityIndex = image1.AbsorptivityIndex;
                }
                keyEventArgs.Handled = true;
                UpdateImageEffectsString();
            }
            else if (key == ScannerKeyboardPart.Keyboard.DynamicGST)
            {
                if (_isUSBCommonKeyboard)
                {

                    //�����������ǷŴ󾵹���
                    _isMagnifyKeyDown = !_isMagnifyKeyDown;
                    //���������˷Ŵ󾵹��ܼ�
                    if (_isMagnifyKeyDown)
                    {
                        // ȡ�����ţ�����ͼ���������ţ���Ӱ���Ŵ�����ʾЧ��
                        Zoom1X(image1, image2);

                        RollingImagesController.EnableLocalZoom = true;
                    }
                    else
                    {
                        //�ڶ��ε����Ŵ󾵹��ܼ�
                        RollingImagesController.EnableLocalZoom = false;
                        Zoom1X(image1, image2);
                    }
                }
                else
                {
                    // �������رջҶ�ɨ��
                    image1.IsDynamicGrayTransformEnabled = !image1.IsDynamicGrayTransformEnabled;
                    if (image2 != null)
                    {
                        image2.IsDynamicGrayTransformEnabled = image1.IsDynamicGrayTransformEnabled;
                    }

                }

                keyEventArgs.Handled = true;
                UpdateImageEffectsString();
            }
            else if (key == Key.F7)
            {
                if (!Properties.Settings.Default.CanOpenImgWithF7)
                {
                    keyEventArgs.Handled = true;
                    return;
                }
                SelectAndReplayImages();
                CloseSpecialPenetration(RollingImagesController);
                keyEventArgs.Handled = true;
            }
            else if (key == ScannerKeyboardPart.Keyboard.LeftTop)
            {
                MoveZoomWindowTwoDirection(Key.N);
                keyEventArgs.Handled = true;
            }
            else if (key == ScannerKeyboardPart.Keyboard.LeftBot)
            {
                MoveZoomWindowTwoDirection(Key.Y);
                keyEventArgs.Handled = true;
            }
            else if (key == ScannerKeyboardPart.Keyboard.RightTop)
            {
                MoveZoomWindowTwoDirection(Key.Q);
                keyEventArgs.Handled = true;
            }
            else if (key == ScannerKeyboardPart.Keyboard.RightBot)
            {
                MoveZoomWindowTwoDirection(Key.E);
                keyEventArgs.Handled = true;
            }
            else if (key == ScannerKeyboardPart.Keyboard.PushLeft)
            {
                CloseSpecialPenetration(RollingImagesController);
                if (image1.CanMoveZoomWindowLeft)
                {
                    MoveZoomWindowByDirectionKeysDown(keyEventArgs);
                }
                else
                {
                    CloseSpecialPenetration(RollingImagesController);
                    // �������Ŵ����Ѿ��ﵽ�����ߣ���ִ��ͼ������
                    ImageDataController.PullLeftImage();
                }

                keyEventArgs.Handled = true;
                UpdateImageEffectsString();
                System.Threading.Thread.Sleep(200);
                keyEventArgs.Handled = true;
            }
            else if (key == ScannerKeyboardPart.Keyboard.PushRight)
            {
                CloseSpecialPenetration(RollingImagesController);
                if (image1.CanMoveZoomWindowRight)
                {
                    MoveZoomWindowByDirectionKeysDown(keyEventArgs);
                }
                else
                {
                    CloseSpecialPenetration(RollingImagesController);
                    // �������Ŵ����Ѿ��ﵽ���ұߣ���ִ��ͼ��ǰ��
                    ImageDataController.PullRightImage();
                }

                keyEventArgs.Handled = true;
                UpdateImageEffectsString();
                System.Threading.Thread.Sleep(200);
            }
            else if (key == Key.F8)   // for test
            {
                if (!Properties.Settings.Default.CanOpenSimulator)
                {
                    keyEventArgs.Handled = true;
                    return;
                }
                if (!ImageDataController.ScanningDataProviderSimulator.IsSimulate && string.IsNullOrEmpty(ImageDataController.ScanningDataProviderSimulator.SimuDataPath))
                {
                    FolderBrowserDialog dialog = new FolderBrowserDialog();
                    dialog.Description = "请选择一个文件夹";
                    dialog.RootFolder = Environment.SpecialFolder.Desktop;
                    // 显示对话框并获取用户操作结果
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        ImageDataController.ScanningDataProviderSimulator.SimuDataPath = dialog.SelectedPath;
                        ImageDataController.ScanningDataProviderSimulator.SimuImageInfo();
                    }
                }
                if (!string.IsNullOrEmpty(ImageDataController.ScanningDataProviderSimulator.SimuDataPath))
                {
                    ImageDataController.ScanningDataProviderSimulator.SimuCalibrateGround();
                    ImageDataController.ScanningDataProviderSimulator.SimuCalibrateAir();
                    ImageDataController.ScanningDataProviderSimulator.Simulate(Properties.Settings.Default.SimulatorSleepInterval);
                }
            }

            //else if (key == Key.F9)
            //{
            //    showwindow();
            //}
            //else if (key == Key.F10)
            //{
            //    closewindow();
            //}
            //UpdateImageEffectsString();
            //else if(key == ScannerKeyboardPart.Keyboard.Magnify)
            //{
            //    //�����������ǷŴ󾵹���
            //    _isMagnifyKeyDown = !_isMagnifyKeyDown;
            //    //���������˷Ŵ󾵹��ܼ�
            //    if (_isMagnifyKeyDown)
            //    {
            //        // ȡ�����ţ�����ͼ���������ţ���Ӱ���Ŵ�����ʾЧ��
            //        Zoom1X(image1, image2);

            //        RollingImagesController.EnableLocalZoom = true;
            //    }
            //    else
            //    {
            //        //�ڶ��ε����Ŵ󾵹��ܼ�
            //        RollingImagesController.EnableLocalZoom = false;
            //        Zoom1X(image1, image2);
            //    }
            //    keyEventArgs.Handled = true;
            //}
            //else if (key == Key.F9)
            //{
            //    HttpNetworkController.Controller.LoginInNetwork("234", AccountRole.Operator, 3);
            //}
        }

        private void ImageEffectRestoration(IRollingImageProcessor image1, IRollingImageProcessor image2)
        {
            image1.IsEnableColorBlock = false;
            // �ָ���ԭʼͼ����ȡ����Ч���Ŵ���
            //image1.Zoom(1.0f);
            image1.ColorMode = _image1DefaultSetting.ColorMode;
            image1.PenetrationMode = _image1DefaultSetting.Penetration;
            image1.IsInversed = _image1DefaultSetting.Inversed;
            image1.IsSuperEnhanceEnabled = _image1DefaultSetting.SuperEnhance;
            image1.IsEdgeEnhanceEnabled = false;
            image1.AbsorptivityIndex = _image1DefaultSetting.Absorbtivity;
            if (image2 != null && _image2DefaultSetting != null)
            {
                image2.IsEnableColorBlock = false;
                //image2.Zoom(1.0f);
                image2.ColorMode = _image2DefaultSetting.ColorMode;
                image2.PenetrationMode = _image2DefaultSetting.Penetration;
                image2.IsInversed = _image2DefaultSetting.Inversed;
                image2.IsSuperEnhanceEnabled = _image2DefaultSetting.SuperEnhance;
                image2.IsEdgeEnhanceEnabled = false;
                image2.AbsorptivityIndex = _image2DefaultSetting.Absorbtivity;
            }

            CloseSpecialPenetration(RollingImagesController);
            //ImageDataController.SetIsShowScreenWithUnPenetrationXRayData(false);
            UpdateImageEffectsString();
        }

        /// <summary>
        /// ȡ������
        /// </summary>
        /// <param name="image1"></param>
        /// <param name="image2"></param>
        private void Zoom1X(IRollingImageProcessor image1, IRollingImageProcessor image2)
        {
            // ȡ������
            if (_zoom1XWhenStart)
            {
                image1.Zoom(1.0f);
                if (image2 != null)
                {
                    image2.Zoom(1.0f);
                }
            }

            UpdateImageEffectsString();
        }

        /// <summary>
        /// ��ʾ��Ļͼ���������ڣ��ɲ�����Ա�Ե�ǰ��Ļ����ʾ��ͼ�����в�������ӡ���洢�ȣ�
        /// </summary>
        private void ShowScreenImgOprWindow()
        {
            //防止快速多次按键
            if (DateTime.Now - _lastSaveDateTime < _keyPressInterval)
            {
                return;
            }

            // �Ƚ�ͼ���б�������cache�У�Ȼ���ڹ�����Ļͼ������������ͼģ�͵�ʱ�򣬴�cache��ȡ��ͼ���б�
            var list = ImageDataController.GetShowingImages();

            // �Ƚ�ͼ���б�������cache�У�Ȼ���ڹ�����Ļͼ������������ͼģ�͵�ʱ�򣬴�cache��ȡ��ͼ���б�
            var manualStoreXrayPath = ConfigPath.ManualStorePath;
            if (!Directory.Exists(manualStoreXrayPath))
            {
                System.IO.Directory.CreateDirectory(manualStoreXrayPath);//不存在就创建目录
            }
            if (list==null|| !list.Any())
            {
                return;
            }
            else
            {
                var image = list.Last();
                if (!File.Exists(image.StorePath))
                {
                    //若队列中最新图像还没有保存，则判断队列长度，尝试读取倒数第二个图像
                    var length = list.Count();
                    if (length < 2)
                    {
                        //无倒数第二个图像
                        return;
                    }
                    image = list.ElementAt(length - 2);
                }
                

                try
                {
                    var ip = new XRayImageProcessor();
                    var fileName = Path.GetFileName(image.StorePath);
                    var dstFileName = Path.Combine(manualStoreXrayPath, fileName);

                    if (File.Exists(image.StorePath))
                    {
                        if (File.Exists(dstFileName))
                        {
                            //弹出已保存过
                            Messenger.Default.Send(new ShowFlyoutMessage("MainWindow",
                                      TranslationService.FindTranslation("Images dump completed"), MessageIcon.None, 3));
                            return;
                        }
                        if (!File.Exists(dstFileName))
                        {
                            File.Copy(image.StorePath, dstFileName,true);
                        }
                        if (!File.Exists(dstFileName))
                        {
                            Tracer.TraceWarning(string.Format("Failed to save image file {0} to {0}.", image.StorePath, dstFileName));
                            return;
                        }
                        var img = XRayScanlinesImage.LoadFromDiskFile(dstFileName);
                        if (img == null)
                        {
                            Tracer.TraceWarning(string.Format("Failed to read image file {0}.", dstFileName));
                            return;
                        }
                        ip.AttachImageData(img.View1Data);
                        var bmp = ip.GetBitmap();

                        Bitmap bmp2 = null;
                        if (img.View2Data != null)
                        {
                            ip.AttachImageData(img.View2Data);
                            bmp2 = ip.GetBitmap();
                        }

                        if (bmp != null && Save4ImagesService.Service.View1Vertical)
                        {
                            bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                        }
                        if (bmp2 != null && Save4ImagesService.Service.View2Vertical)
                        {
                            bmp2.RotateFlip(RotateFlipType.RotateNoneFlipY);
                        }
                        if (Save4ImagesService.Service.Left2Right)
                        {
                            bmp.RotateFlip(RotateFlipType.RotateNoneFlipX);
                            if (bmp2 != null)
                            {
                                bmp2.RotateFlip(RotateFlipType.RotateNoneFlipX);
                            }
                        }

                        var combinebmp = BitmapHelper.CombineBmp(bmp, bmp2);
                        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                        // 民航要求，在保存的图片上显示设备编号、扫描时间和操机员
                        PrintInfomationOnImage.PrintOnImage(combinebmp, fileNameWithoutExtension, dateTime2String(img.ScanningTime));
                        combinebmp.Save(Path.Combine(manualStoreXrayPath, fileNameWithoutExtension + ".jpg"), ImageFormat.Jpeg);
                        new OperationRecordService().AddRecord(new OperationRecord()
                        {
                            AccountId = LoginAccountManager.Service.CurrentAccount != null ? LoginAccountManager.Service.CurrentAccount.AccountId : "",
                            OperateUI = OperationUI.ScreenImagesOperation,
                            OperateTime = DateTime.Now,
                            OperateObject = fileName,
                            OperateCommand = OperationCommand.Export,
                            OperateContent = ConfigHelper.AddQuotationForPath(dstFileName),
                        });

                    }

                    var set = new ImageRecordDbSet();

                    var r = set.ImageRecordsSet.Find(image.ImageRecordId);
                    if (r != null)
                    {
                        r.IsLocked = true;
                    }
                    set.SaveChanges();
                    _lastSaveDateTime = DateTime.Now;
                    Messenger.Default.Send(new ShowFlyoutMessage("MainWindow",
                                      TranslationService.FindTranslation("Images dump completed"), MessageIcon.None, 3));
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception, "Failed to save image file: " + image.StorePath);
                }
            }
        }

        private string dateTime2String(DateTime dt)
        {
            int systemDateformat;
            if (!ScannerConfig.Read(ConfigPath.SystemDateFormat, out systemDateformat))
            {
                systemDateformat = 0;
            }
            switch (systemDateformat)
            {
                case 0:
                    return dt.ToString("MM.dd.yyyy  HH:mm:ss");
                case 1:
                    return dt.ToString("dd.MM.yyyy  HH:mm:ss");
                case 2:
                    return dt.ToString("yyyy.MM.dd  HH:mm:ss");
                default:
                    return string.Format("MM.dd.yyyy  HH:mm:ss", dt);
            }
        }

        /// <summary>
        /// �����Ի��������û�����ѡ������ͼ�����ڻط�
        /// </summary>
        private void SelectAndReplayImages()
        {
            var dlg = new OpenFileDialog();
            dlg.Multiselect = true;
            dlg.Filter = "XRay Image | *.xray";
            if (dlg.ShowDialog() == true)
            {
                CloseSpecialPenetration(RollingImagesController);
                var fileNames = dlg.FileNames;
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ImageDataController.BeginPlayback(fileNames);
                    if (RollingImagesController.Image1.IsSuperEnhanceEnabled == true)
                    {
                        ImageDataController.SetIsShowScreenWithOriginXRayData(true);
                        _isEnhanced = false;
                    }
                });

                foreach (var imgfile in fileNames)
                {
                    new OperationRecordService().AddRecord(new OperationRecord()
                    {
                        AccountId = LoginAccountManager.Service.CurrentAccount != null ? LoginAccountManager.Service.CurrentAccount.AccountId : "",
                        OperateUI = OperationUI.MainUI,
                        OperateTime = DateTime.Now,
                        OperateObject = Path.GetFileName(imgfile),
                        OperateCommand = OperationCommand.Import,
                        OperateContent = "",
                    });
                }
            }

        }

        private bool IsFunctionKey(Key key)
        {
            return key == Key.F1 || key == Key.F2 || key == Key.F3;
        }

        /// <summary>
        /// �ж�һ�������Ƿ��ǹ��ܿ�����ͼ����Ч��
        /// ���ڴ��ఴ�������³���0.7�����ٵ��𣬻ָ���֮ǰ�Ĺ���
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private bool IsReversibleImageEffectsKey(Key key)
        {
            return ScannerKeyboardPart.Keyboard.IsReversibleImageEffectsKey(key);
        }

        /// <summary>
        /// �Ƿ��ǹ��ܿ����Ŀ��ؼ����Զ�ʶ����
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private bool IsReversibleToggleKey(Key key)
        {
            return key == ScannerKeyboardPart.Keyboard.Auto || key == ScannerKeyboardPart.Keyboard.VFlip;
        }

        private void UpdateImageEffectsString(bool isMulti = false)
        {
            var image1 = RollingImagesController.Image1;
            var str = ImageGeneralSetting.FormatImageEffectsString(image1.IsInversed,
                image1.IsSuperEnhanceEnabled, image1.ColorMode, image1.PenetrationMode,
                image1.IsDynamicGrayTransformEnabled, image1.AbsorptivityIndex, _isEnhanced, _isUnPenerationRegion || _isImageHistogram, isMulti);

            // ͨ����Ϣ����ʽ֪ͨϵͳ״̬���ȸ���ͼ����Ч��ʾ
            MessengerInstance.Send(new UpdateImageEffectsResultMessage(str, image1.ZoomMultiples, IsShiftKeyOn));
        }

        /// <summary>
        /// �û���ϵͳ״̬���һ����꣬���ᵯ��ϵͳ���ò˵�
        /// </summary>
        /// <param name="args"></param>
        private void MouseRightButtonDownEventCommandExecute(MouseButtonEventArgs args)
        {
            if (Transmission.IsRemoteDiagnosing)  //yxc 禁止远程诊断状态下进入设置界面
            {
                return;
            }
            if (_currentDirection == MotorDirection.Stop && ImageDataController.IsInNomalMode)
            {
                MessengerInstance.Send(new OpenWindowMessage("MainWindow", "SettingWindow",
                        new PageNavigation("SystemMenu", "MenuPage", "Menu")));
            }
        }

        private void CloseSpecialPenetration(IRollingImageProcessController rollingImagesController)
        {
            if (rollingImagesController != null)
            {
                if (!_isEnhanced)
                {
                    _isEnhanced = true;
                    //使用增强后数据填充控件
                    ImageDataController.SetIsShowScreenWithOriginXRayData(!_isEnhanced);
                }
                //if (_isUnPenerationRegion)
                //{
                //    ImageDataController.SetIsShowScreenWithUnPenetrationXRayData(false);
                //    _isUnPenerationRegion = false;
                //}
                if (_isImageHistogram)
                {
                    ImageDataController.SetImageHistogramEquation(false);
                    _isImageHistogram = false;
                }
                if (_isSifenbian)
                {
                    ImageDataController.SetSifenbianPublic(false);
                    _isSifenbian = false;
                }
            }
        }

        private void RecordImageProcess(Key key)
        {
            if (key == Key.System) return;
            Tracer.TraceInfo(string.Format("Key:{0} press up.", key.ToString()));
            try
            {
                new OperationRecordService().AddRecord(new OperationRecord()
                {
                    AccountId = LoginAccountManager.Service.CurrentAccount != null ? LoginAccountManager.Service.CurrentAccount.AccountId : "",
                    OperateUI = OperationUI.MainUI,
                    OperateTime = DateTime.Now,
                    OperateObject = key.ToString(),
                    OperateCommand = OperationCommand.KeyPress,
                    OperateContent = "",
                });
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }

        }

        //public void WorkReminder()
        //{
        //    WorkReminderTimerStop();
        //    bool isWorkIntervalRemind = false;
        //    if (ScannerConfig.Read(ConfigPath.IsWorkIntervalReminder,out isWorkIntervalRemind))
        //    {
        //        float workReminderCount;
        //        if (isWorkIntervalRemind && ScannerConfig.Read(ConfigPath.WorkReminderTime, out workReminderCount))
        //        {
        //            Tracer.TraceDebug($"IsWorkIntervalRemind:{isWorkIntervalRemind},WorkReminderCount:{workReminderCount}");
        //            float workSeconds = workReminderCount * 3600;
        //            Tracer.TraceDebug($"时间：{workSeconds}秒");
        //            _workReminderTime = new DispatcherTimer();
        //            _workReminderTime.Tick += (sender, args) =>
        //            {
        //                /MessengerInstance.Send(new OpenWindowMessage("MainWindow", "WorkReminderWindow", workReminderCount * 60));
        //            };
        //            _workReminderTime.Interval = TimeSpan.FromSeconds(workSeconds);
        //            _workReminderTime.Start();
        //        }
        //    }
        //}

        //public void WorkReminderTimerStop()
        //{
        //    if(_workReminderTime != null)
        //    {
        //        _workReminderTime.Stop();
        //        _workReminderTime = null;
        //    }
        //}
    }
}