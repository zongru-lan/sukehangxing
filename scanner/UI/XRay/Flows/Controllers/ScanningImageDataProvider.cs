
using Emgu.CV;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using UI.Common.Tracers;
using UI.XRay.Business.Algo;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Control;
using UI.XRay.ControlWorkflows;
using UI.XRay.Flows.Services;
using UI.XRay.Flows.Services.DataProcess;
using UI.XRay.Flows.TRSNetwork;
using UI.XRay.RenderEngine;
using UI.XRay.Test.TestData;

namespace UI.XRay.Flows.Controllers
{
    /// <summary>
    /// 扫描图像数据控制：控制扫描数据的预处理与显示流程
    /// </summary>
    public class ScanningImageDataProvider : IRollingImageDataProvider
    {
        public event Action<DisplayScanlineDataBundle> DataReady;

        /// <summary>
        /// 事件：当前物体图像存储完毕
        /// </summary>
        public event Action BagImageSaved;

        /// <summary>
        /// 回拉时显示框信息
        /// </summary>
        public event Action<KeyValuePair<DetectViewIndex, List<MarkerRegion>>> DrawRectAction;

        //public event Action<int> SessionBagCountChanged;
        /// <summary>
        /// 等待预处理的数据队列：数据一旦进入此队列，将必定会被处理并显示（射线未发射时的数据不会进入此队列）
        /// </summary>
        private ConcurrentQueue<RawScanlineDataBundle> _waitingProcessQueue = new ConcurrentQueue<RawScanlineDataBundle>();

        private ConcurrentQueue<ClassifiedLineDataBundle> _afterProcessQueue = new ConcurrentQueue<ClassifiedLineDataBundle>();

        private ConcurrentQueue<DisplayScanlineDataBundle> _imageEnhanceQueue = new ConcurrentQueue<DisplayScanlineDataBundle>();
        /// <summary>
        /// Dedicated work thread for preprocessing
        /// </summary>
        private Thread _preProcessingThread;

        private Thread _imageEnhanceThread;

        /// <summary>
        /// 预处理线程退出信号
        /// </summary>
        private bool _preprocessThreadExitSignal = true;

        /// <summary>
        /// 数据预处理服务
        /// </summary>
        public PreprocessService Preprocess { get; private set; }

        /// <summary>
        /// 本底、满度更新服务：支持自动更新以及强制更新
        /// </summary>
        public AutoCalibrationServiceBase AutoCalibration { get; private set; }

        /// <summary>
        /// 当前的电机方向
        /// </summary>
        public ConveyorDirection Direction { get; private set; }

        /// <summary>
        /// 记录上一次电机不是停止状态的方向
        /// </summary>
        private ConveyorDirection _lastNotStpMoveDirection = ConveyorDirection.MoveBackward;

        /// <summary>
        /// 是否翻转电机键
        /// </summary>
        private bool _reverseMotorDirection;

        /// <summary>
        /// 当前的射线状态
        /// </summary>
        public bool IsXRayOn { get; private set; }

        /// <summary>
        /// 射线上一次由关闭变为发射的时刻
        /// </summary>
        public DateTime LastXRayOnTime { get; private set; }

        /// <summary>
        /// 急停开关是否接通
        /// </summary>
        public bool _isEmgcSwitchOn = true;

        /// <summary>
        /// 当前是否处于X射线刚打开的上升沿期间：射线刚刚打开的一小会时间
        /// </summary>
        private bool IsXRayRisingNow
        {
            get { return IsXRayOn && (DateTime.Now - LastXRayOnTime) <= XRayRisingTimeSpan; }
        }

        /// <summary>
        /// 探测视角的个数
        /// </summary>
        private int _viewsCount = 1;

        public bool IsDualView
        {
            get { return _viewsCount > 1; }
        }

        public bool GetCurrentInterruptState()
        {
            return (_interruptAndMatchService == null || _interruptAndMatchService.InterruptState == InterruptMode.NotInInterrrupt);
        }

        private DualViewMatchingService _dualViewMatchingService;

        /// <summary>
        /// 中断拼图服务
        /// </summary>
        private InterruptAndMatchService _interruptAndMatchService;

        /// <summary>
        /// 射线源的上升沿时间，单位为毫秒.todo:上升沿时间没有从配置中获取
        /// </summary>
        public readonly TimeSpan XRayRisingTimeSpan = TimeSpan.FromMilliseconds(400);

        private ScanningScanLinesPool _scanlinesPool;

        /// <summary>
        /// 当前扫描线的最大编号，每增加一线新数据，编号+1
        /// </summary>
        private int NextLineNumber { get; set; }

        private int LineNumber { get; set; }

        /// <summary>
        /// 物体图像分离控制
        /// </summary>
        private ObjectSeparater _separater;

        /// <summary>
        /// 白线检测
        /// </summary>
        private AirCheckingService _airLineChecking;

        /// <summary>
        /// 测试数据类
        /// </summary>
        private readonly TestDataProvider _testDataProvider;
        /// <summary>
        /// 是否处于测试模式
        /// </summary>
        public bool InTestDataMode { get { return _testDataProvider != null; } }

        /// <summary>
        /// 是否处于中断恢复状态中
        /// </summary>
        public bool InInterruptBackwardOrRecovering
        {
            get
            {
                return _interruptAndMatchService != null &&
                    (_interruptAndMatchService.InterruptState == InterruptMode.Recovering ||
                    _interruptAndMatchService.InterruptState == InterruptMode.Backward);
            }
        }

        /// <summary>
        /// 是否处于非中断模式下
        /// </summary>
        public bool IsInNormalMode
        {
            get
            {
                return _interruptAndMatchService == null || _interruptAndMatchService.InterruptState == InterruptMode.NotInInterrrupt;
            }
        }

        public void ExitInterruptRecovering()
        {
            if (_interruptAndMatchService != null)
            {
                Tracer.TraceInfo($"[InterruptTailAfter] emergency stop triggered - Exit Interrupt status!");
                _interruptAndMatchService.InterruptState = InterruptMode.NotInInterrrupt;
                _interruptAndMatchService.ClearCaches();
            }
        }

        /// <summary>
        /// 设备编号
        /// </summary>
        private string _machineNumber;

        private AutoCalibrationMode _airAutoCalibrationMode;
        /// <summary>
        private bool _addPureWhiteSeperateLinesBetweenObject = false;
        private int _bagMinLinesCount = 100;

        /// <summary>
        /// 是否保存过图像，主要用于分隔图像
        /// </summary>
        private bool _hasSavedImage = true;

        /// <summary>
        /// 边缘增强和材料滤波
        /// </summary>
        private DataProcessInAirport2 dpa;

        /// <summary>
        /// 图像畸变矫正
        /// </summary>
        private bool _isShapeCorrection;

        public bool IsShapeCorrection
        {
            get { return _isShapeCorrection; }
            set
            {
                if (dpa != null)
                {
                    dpa.ClearCache();
                }
                if (Preprocess != null)
                {
                    Preprocess.ClearLinesCache();
                    Preprocess.ClearWienerCache();
                }
                if (_separater != null)
                {
                    _separater.ClearLinesCache();
                    _separater.ResetImageLinesCache();
                }
                _isShapeCorrection = value;
            }
        }

        /// <summary>
        /// 图像畸变矫正
        /// </summary>
        private ShapeCorrectionService _shapeCorrection;

        private string _filePath;
        private string _fileName;

        private int _darkFiledAve = 8000;

        RemoveInstableChannelsOperator _removeBadChannel;
        ChannelBadFlagsFileWatchingService _badFlagWatcher = new ChannelBadFlagsFileWatchingService();
        /// <summary>
        /// 线体联动工作模式
        /// </summary>
        private bool isFlowLineMode;

        public ScanningImageDataProvider(int screenMaxLines)
        {
            _scanlinesPool = new ScanningScanLinesPool(screenMaxLines);
            _scanlinesPool.DrawRectAction += _scanlinesPool_DrawRectAction;
            Direction = ConveyorDirection.Stop;

            if (!ScannerConfig.Read(ConfigPath.KeyboardIsConveyorKeyReversed, out _reverseMotorDirection))
            {
                _reverseMotorDirection = false;
            }

            if (!ScannerConfig.Read(ConfigPath.PreProcDarkFieldAverage, out _darkFiledAve))
            {
                _darkFiledAve = 8000;
            }

            double risingTimeSpan;
            if (ScannerConfig.Read(ConfigPath.XRayGenRisingTimespan, out risingTimeSpan))
            {
                XRayRisingTimeSpan = TimeSpan.FromSeconds(risingTimeSpan);
            }

            ScannerConfig.Read(ConfigPath.SystemMachineNum, out _machineNumber);

            if (!ScannerConfig.Read(ConfigPath.AddPureWhiteSeperateLinesBetweenObject, out _addPureWhiteSeperateLinesBetweenObject))
            {
                _addPureWhiteSeperateLinesBetweenObject = false;
            }


            if (!ScannerConfig.Read(ConfigPath.BagMinLinesCount, out _bagMinLinesCount))
            {
                _bagMinLinesCount = 100;
            }

            if (!ScannerConfig.Read(ConfigPath.BagMinLinesCount, out _bagMinLinesCount))
            {
                _bagMinLinesCount = 100;
            }

            ScannerConfig.Read(ConfigPath.SystemMachineNum, out _machineNumber);

            if (!ScannerConfig.Read(ConfigPath.IsFlowLineMode, out isFlowLineMode))
            {
                isFlowLineMode = false;


            }

            ScannerConfig.ConfigChanged += ScannerConfigOnConfigChanged;

            ResetSeperator();

            //testdata，todo：后续从配置表中读取参数
            bool enableTestData;
            try
            {
                ScannerConfig.Read(ConfigPath.EnableTestData, out enableTestData);
                ScannerConfig.Read(ConfigPath.PreProcAirUpdateMode, out _airAutoCalibrationMode);
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
                enableTestData = false;
                _airAutoCalibrationMode = AutoCalibrationMode.BeforeScanning;
            }
            if (enableTestData)
            {
                _testDataProvider = new TestDataProvider();
            }

            //初始化畸变矫正
            _shapeCorrection = new ShapeCorrectionService();
            int count = ExchangeDirectionConfig.Service.GetView1ImageHeight();
            _shapeCorrection.View1ImageHeight = count;

            count = ExchangeDirectionConfig.Service.GetView2ImageHeight();
            _shapeCorrection.View2ImageHeight = count;

            //民航算法初始化
            dpa = new DataProcessInAirport2();
            _removeBadChannel = new RemoveInstableChannelsOperator();
            LoginAccountManager.Service.AccountLogout += AccountLogoutEventExecute;
        }

        void _scanlinesPool_DrawRectAction(KeyValuePair<DetectViewIndex, List<MarkerRegion>> obj)
        {
            if (DrawRectAction != null)
            {
                DrawRectAction(obj);
            }
        }


        /// <summary>
        /// 若切换账户，保存待保存的线数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AccountLogoutEventExecute(object sender, Account e)
        {
            SaveCurrentImage();
            NextLineNumber = 0;
            LineNumber = 0;
            _scanlinesPool.ClearRecentImageCache();
        }

        private void ScannerConfigOnConfigChanged(object sender, EventArgs eventArgs)
        {
            // 配置发生变化后，重置图像分离器
            ResetSeperator();

            if (!ScannerConfig.Read(ConfigPath.KeyboardIsConveyorKeyReversed, out _reverseMotorDirection))
            {
                _reverseMotorDirection = false;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void ResetSeperator()
        {
            bool showBlankSpace;
            if (!ScannerConfig.Read(ConfigPath.ImagesShowBlankSpace, out showBlankSpace))
            {
                showBlankSpace = false;
            }

            _separater = new ObjectSeparater(showBlankSpace);
        }

        /// <summary>
        /// 启动数据预处理服务：可以接收新数据，并在后台线程中进行处理，之后再以事件的方式外传
        /// </summary>
        /// <returns></returns>
        public bool StartService()
        {
            AutoCalibration = GetAutoCalibrationService(_airAutoCalibrationMode);
            Preprocess = new PreprocessService();                //图像预处理类
            Preprocess.SetDynamicUpdateAirService(AutoCalibration);

            if (!ScannerConfig.Read(ConfigPath.MachineViewsCount, out _viewsCount))
            {
                _viewsCount = 1;
            }

            if (_viewsCount > 1)
            {
                _dualViewMatchingService = new DualViewMatchingService();     //双视角匹配类
            }

            //开启中断模式
            bool enableInterruptMode;

            if (!ScannerConfig.Read(ConfigPath.MachineAutoRewind, out enableInterruptMode))
            {
                enableInterruptMode = false;
            }
            if (enableInterruptMode)
            {
                _interruptAndMatchService = new InterruptAndMatchService();
                _interruptAndMatchService.LeaveEventHandler += InterruptAndMatchService_LeaveEventHandler;
            }

            bool _enableFilter;
            if (!ScannerConfig.Read(ConfigPath.PreProcFilterEnable, out _enableFilter))
            {
                _enableFilter = false;
            }
            if (_enableFilter)
                _airLineChecking = new AirLinesCheckingService();  //
            else
                _airLineChecking = new AirLineCheckingService();   //
            _preprocessThreadExitSignal = false;

            // 初始化数据预处理专用线程
            if (_preProcessingThread == null)
            {
                _preProcessingThread = new Thread(PreProcessingThreadRoutine)
                {
                    IsBackground = true
                };
                _preProcessingThread.Start();
            }

            if (_imageEnhanceThread == null)
            {
                _imageEnhanceThread = new Thread(ImageEnhanceThreadRoutine)
                {
                    IsBackground = true
                };
                _imageEnhanceThread.Start();
            }

            CaptureService.ServicePart.ScanlineCaptured += ServicePartOnScanlineCaptured;

            ControlService.ServicePart.XRayStateChanged += ServicePartOnXRayStateChanged;
            ControlService.ServicePart.ConveyorDirectionChanged += ServicePartOnConveyorDirectionChanged;
            ControlService.ServicePart.SwitchStateChanged += ServicePartOnSwitchStateChanged;
            ControlService.ServicePart.EnterInterruptMode += ServicePart_EnterInterruptMode;
            ControlService.ServicePart.WorkModeChanged += ServicePart_WorkModeChanged;

            if (_testDataProvider != null)
            {
                _testDataProvider.SimuScanlineCaptured += ServicePartOnScanlineCaptured;
                _testDataProvider.SimuXRayStateChanged += OnSimuXrayStateChanged;
                _testDataProvider.SimuMotorStateChanged += OnSimuMotorStateChanged;
            }

            ManualCalibrationService.Service.AirCalibratedWeakEvent += ServiceOnAirCalibratedWeakEvent;
            ManualCalibrationService.Service.GroundCalibratedWeakEvent += ServiceOnGroundCalibratedWeakEvent;
            return true;
        }

        private void InterruptAndMatchService_LeaveEventHandler(List<RawScanlineDataBundle> obj)
        {
            foreach (var bundle in obj)
            {
                DualViewMatch(bundle);
            }
        }

        void Service_ClearDataUpdated()
        {
            try
            {
                if (_scanlinesPool != null)
                {
                    _scanlinesPool.ClearUnsavedScanLines();
                }
                Tracer.TraceInfo("Clear UnsavedScanLines.");
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }

        private AutoCalibrationServiceBase GetAutoCalibrationService(AutoCalibrationMode mode)
        {
            switch (mode)
            {
                case AutoCalibrationMode.None:
                case AutoCalibrationMode.BeforeScanning:
                    return new AutoCalibrationService();
                case AutoCalibrationMode.DynamicUpdate:
                    return new DynamicAutoCalibrationService();
                default:
                    return new AutoCalibrationService();
            }
        }

        /// <summary>
        /// 相应中断拼图模式改变事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ServicePart_EnterInterruptMode(object sender, EventArgs e)
        {
            if (_interruptAndMatchService != null)
            {
                _interruptAndMatchService.ServicePart_EnterInterruptMode();
            }
        }

        /// <summary>切换工作模式，需要重置中断拼图状态</summary>
        void ServicePart_WorkModeChanged(object sender, WorkModeChangedEventArgs e)
        {
            if (_interruptAndMatchService != null)
            {
                _interruptAndMatchService.ServicePart_WorkModeChanged(e.Current);
            }
        }

        private void ServicePartOnSwitchStateChanged(object sender, SwitchStateChangedEventArgs args)
        {
            if (args.Switch == CtrlSysSwitch.EmergencySwitch)
            {
                //// 
                //ControlService.ServicePart.DriveConveyor(ConveyorDirection.Stop);

                // 急停开关被按下
                _isEmgcSwitchOn = args.New;
            }
        }

        /// <summary>
        /// 停止数据预处理服务：不再接收新数据，也不再向外传输处理结果
        /// </summary>
        public void StopService()
        {
            _preprocessThreadExitSignal = true;
            if (_preProcessingThread != null)
                _preProcessingThread.Join();
            _preProcessingThread = null;

            if (_imageEnhanceThread != null)
            {
                _imageEnhanceThread.Join();
            }
            _imageEnhanceThread = null;

            SaveCurrentImage();

            ControlService.ServicePart.XRayStateChanged -= ServicePartOnXRayStateChanged;
            ControlService.ServicePart.ConveyorDirectionChanged -= ServicePartOnConveyorDirectionChanged;
            ControlService.ServicePart.EnterInterruptMode -= ServicePart_EnterInterruptMode;

            ManualCalibrationService.Service.AirCalibratedWeakEvent -= ServiceOnAirCalibratedWeakEvent;
            ManualCalibrationService.Service.GroundCalibratedWeakEvent -= ServiceOnGroundCalibratedWeakEvent;

            //ImageProcessController.GetImageLines -= ControllerOnGetImageLines;
            CaptureService.ServicePart.ScanlineCaptured -= ServicePartOnScanlineCaptured;

            if (_scanlinesPool != null)
            {
                _scanlinesPool.DrawRectAction -= _scanlinesPool_DrawRectAction;
                _scanlinesPool.ShutDown();
            }


            if (_testDataProvider != null)
            {
                _testDataProvider.SimuScanlineCaptured -= ServicePartOnScanlineCaptured;
                _testDataProvider.SimuXRayStateChanged -= OnSimuXrayStateChanged;
                _testDataProvider.SimuMotorStateChanged -= OnSimuMotorStateChanged;
            }
            if (_interruptAndMatchService != null)
            {
                _interruptAndMatchService.LeaveEventHandler -= InterruptAndMatchService_LeaveEventHandler;
            }
        }

        /// <summary>
        /// 手动校正本底成功
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ground"></param>
        private void ServiceOnGroundCalibratedWeakEvent(object sender, ScanlineDataBundle ground)
        {
            if (ground != null)
            {
                AutoCalibration.ResetManualCalibratedGround(ground);
                Preprocess.ResetGround(ground);
            }
        }

        /// <summary>
        /// 手动校正满度成功
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="air"></param>
        private void ServiceOnAirCalibratedWeakEvent(object sender, ScanlineDataBundle air)
        {
            if (air != null)
            {
                AutoCalibration.ResetManualCalibratedAir(air);
                Preprocess.ResetAir(air);
            }
        }

        /// <summary>
        /// 控制系统事件：输送机方向发生变化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void ServicePartOnConveyorDirectionChanged(object sender, ConveyorDirectionChangedEventArgs args)
        {
            Direction = args.New;
            SystemStatus.Instance.ConveyorState = (int)Direction;
            Tracer.TraceInfo("Converyor direction changed! New direction is " + Direction.ToString());

            if (_interruptAndMatchService != null)
            {
                _interruptAndMatchService.OnConveyorDirectionChanged(Direction);
                if (Direction == ConveyorDirection.Stop && _interruptAndMatchService.InterruptState == InterruptMode.Backward)
                {
                    Tracer.TraceInfo($"[InterruptTailAfter] ServicePartOnConveyorDirectionChanged - Enter into interrrupt status!");
                    _interruptAndMatchService.InterruptState = InterruptMode.InInterrrupt;
                }
                else if (_interruptAndMatchService.InterruptState == InterruptMode.NotInInterrrupt)
                {
                    ClearCache();
                }
            }
        }

        private object savelock3 = new object();  //yxc
        /// <summary>
        /// 控制系统事件：X射线状态发生变化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void ServicePartOnXRayStateChanged(object sender, XRayStateChangedEventArgs args)
        {
            if (args.State == XRayState.Radiating)
            {
                if (_interruptAndMatchService == null || _interruptAndMatchService.InterruptState == InterruptMode.NotInInterrrupt)
                {
                    AutoCalibration.Clearup();
                    ClearCache();
                    if (IsDualView && _dualViewMatchingService != null)
                    {
                        _dualViewMatchingService.ResetToUnmatched();
                    }
                }
                LastXRayOnTime = DateTime.Now;
                IsXRayOn = true;
                if (_interruptAndMatchService != null)
                {
                    AutoCalibration.InInterruptMode = _interruptAndMatchService.InterruptState;
                }
                AutoCalibration.OnXRayStateChanged(true);
                Tracer.TraceInfo("The Xray state changed! Xray is Opened! XrayGeneratorIndex is " + args.XRayGen.ToString());
            }
            else if (args.State == XRayState.Closing)
            {
                IsXRayOn = false;
                if (_interruptAndMatchService != null && _interruptAndMatchService.InterruptState == InterruptMode.Recovering)
                {
                    _interruptAndMatchService.FireEventHandler();
                }
                Tracer.TraceInfo("The Xray state changed! Xray is Closing! XrayGeneratorIndex is " + args.XRayGen.ToString());
            }
            else if (args.State == XRayState.Closed)
            {
                IsXRayOn = false;

                AutoCalibration.OnXRayStateChanged(false);

                if (_interruptAndMatchService == null || _interruptAndMatchService.InterruptState == InterruptMode.NotInInterrrupt)
                {
                    lock (savelock3) //yxc
                    { SaveObjectAndFireEvent(); }
                }

                Tracer.TraceInfo("The Xray state changed! Xray is Closed! XrayGeneratorIndex is " + args.XRayGen.ToString());
            }
            TRSNetWorkService.Service.UpdateXrayGenState(args);
        }

        // 每次取出一线数据进行处理,然后填充到显示队列中
        RawScanlineDataBundle scanlineData;
        /// <summary>
        /// 预处理专用线程：将待处理队列中的数据取出，进行处理，填充进显示队列中
        /// </summary>
        private void PreProcessingThreadRoutine()
        {
            while (!_preprocessThreadExitSignal)
            {
                try
                {
                    while (_waitingProcessQueue.TryDequeue(out scanlineData))
                    {
                        //if(scanlineData.DataTag<lPreProcessPreTag)
                        //{
                        //    Tracer.TraceInfo("preprocessing thread tag error pretag:"+lPreProcessPreTag+ ",scanlineDatatag:" + scanlineData.DataTag);  //tuxiujia
                        //}
                        // 填充数据，进行预处理
                        Preprocess.Feed(scanlineData, GetCurrentInterruptState());

                        if (!Preprocess.OutputQueue.IsEmpty)
                        {
                            ClassifiedLineDataBundle bundle;
                            while (Preprocess.OutputQueue.TryDequeue(out bundle))   //从队列里取出一个元素塞给bundle
                            {
                                // 进行白线判断
                                var bundleList = _airLineChecking.CheckAirLine(bundle);                              

                                foreach (var line in bundleList)
                                {
                                    CacheLineAndShow(line);
                                }
                                bundleList = null;
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception, "Unexpected exception in preProcessingThreadRoutine");
                }

                Thread.Sleep(1);
            }
        }


        DisplayScanlineDataBundle _enhancebundle = null;
        bool _objScanOver = false;
        bool ObjScanOver
        {
            get
            {
                return _objScanOver;
            }
            set
            {
                if (_objScanOver != value)
                {
                    _objScanOver = value;
                    Tracer.TraceInfo($"[ObjSeparater] ObjScanOver: {_objScanOver}");
                }
            }
        }
        private object savelock2 = new object();  //yxc
        private void ImageEnhanceThreadRoutine()
        {
            while (!_preprocessThreadExitSignal)
            {
                try
                {
                    if (!_imageEnhanceQueue.IsEmpty)
                    {
                        while (_imageEnhanceQueue.TryDequeue(out _enhancebundle))
                        {
                            //图像增强
                            var lines = dpa.AirPortDataProcess(_enhancebundle);

                            foreach (DisplayScanlineDataBundle displayLine in lines)
                            {
                                ObjScanOver = _separater.Separate(displayLine);

                                while (_separater.OutputQueue.Count > 0)
                                {
                                    var line = _separater.OutputQueue.Dequeue();

                                    //设置数据编号
                                    line.LineNumber = NextLineNumber;
                                    // 数据编号递增
                                    NextLineNumber++;
                                    LineNumber++;
                                    // 将显示数据加入显示缓存，同时输出显示
                                    _scanlinesPool.AppendNewScanLine(line);

                                    if (DataReady != null)
                                    {
                                        DataReady(line);
                                    }

                                    if (_hasSavedImage)
                                    {
                                        if (LineNumber > _bagMinLinesCount)
                                        {
                                            _hasSavedImage = false;
                                            GenFileName();
                                            Tracer.TraceInfo(string.Format("[TRS]...--->> 收到任务start_0 : {0}", 1));

                                            TRSNetWorkService.Service.UpdateBagStartInfo(DateTime.Now, NextLineNumber - LineNumber, Path.Combine(_filePath, _fileName));
                                        }
                                    }
                                }
                                
                                // 物体扫描结束，则将当前尚未存储的数据全部存储
                                if (ObjScanOver)
                                {
                                    //保存图像后添加一段空白线
                                    if (!_hasSavedImage)
                                    {
                                        lock (savelock2)  //yxc
                                        { SaveObjectAndFireEvent(); }
                                    }
                                }
                            }
                            TRSNetWorkService.Service.AddScanlineData(lines);
                        }
                    }
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception, "Unexpected exception in ImageEnhanceThreadRoutine");
                }
                Thread.Sleep(1);
            }
        }

        /// <summary>
        ///  保存图像并发送事件通知
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void SaveObjectAndFireEvent()
        {
            if (SaveCurrentImage())
            {
                if (BagImageSaved != null)
                {
                    BagImageSaved();
                }
            }
        }

        private void GenFileName()
        {
            // 根据当前时间，分别生成文件存储路径和文件名
            var scanTime = DateTime.Now;

            _filePath = ImageFileStorePathHelper.GenerateFilePath(scanTime);
            _fileName = ImageFileStorePathHelper.GenerateFileName(_machineNumber, LoginAccountManager.Service.AccountId,
                scanTime);
            // Http协议中的Package ID


            Directory.CreateDirectory(_filePath);
        }

        /// <summary>
        /// 将缓冲区中当前尚未存储的图像，全部存储到磁盘中
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private bool SaveCurrentImage()
        {
            if (!string.IsNullOrWhiteSpace(_fileName) && !string.IsNullOrWhiteSpace(_filePath))
            {
                string account = LoginAccountManager.Service.HasLogin ? LoginAccountManager.Service.CurrentAccount.AccountId : string.Empty;
                //GenFileName();
                var res = _scanlinesPool.SaveNewLinesIntoImage(Path.Combine(_filePath, _fileName), NextLineNumber - 1, DateTime.Now, account);
                if (res)
                {
                    //string account = LoginAccountManager.Service.HasLogin ? LoginAccountManager.Service.CurrentAccount.AccountId : string.Empty;
                    //Tracer.TraceInfo(string.Format("[TRS]...--->> 收到任务结束_0 : {0}", 1));
                    //TRSNetWorkService.Service.UpdateBagEndInfo(DateTime.Now, LineNumber, Path.Combine(_filePath, _fileName), account);
                    LineNumber = 0;
                    _hasSavedImage = true;
                }
                return res;
            }
            return false;
        }

        /// <summary>
        /// 缓存并显示图像列
        /// </summary>
        /// <param name="bundle"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void CacheLineAndShow(ClassifiedLineDataBundle bundle)
        {
            if (DataReady != null)
            {
                if (bundle.View1Data != null)
                {
                    if (DetectConstantLine.Service.HaveConstantLine(bundle.View1Data.XRayData, DetectViewIndex.View1))
                    {
                        return;
                    }
                }
                if (bundle.View2Data != null)
                {
                    if (DetectConstantLine.Service.HaveConstantLine(bundle.View2Data.XRayData, DetectViewIndex.View2))
                    {
                        return;
                    }
                }


                // 对数据进行编号，以便于在显示报警框及回拉时进行数据定位
                var view1Ordered = bundle.View1Data != null
                    ? bundle.View1Data.ToDisplayXRayMatLineData(1)
                    : null;
                var view2Ordered = bundle.View2Data != null
                    ? bundle.View2Data.ToDisplayXRayMatLineData(1)
                    : null;

                DisplayScanlineDataBundle displayData = null;
                if (_isShapeCorrection)
                {
                    var shapedView1 = _shapeCorrection.DisplayScanlineCorrection(view1Ordered, DetectViewIndex.View1);
                    var shapedView2 = _shapeCorrection.DisplayScanlineCorrection(view2Ordered, DetectViewIndex.View2);
                    shapedView1.SetOriginalFusedData(view1Ordered.OriginalFused);
                    if(shapedView2 != null)
                        shapedView2.SetOriginalFusedData(view2Ordered.OriginalFused);
                    displayData = new DisplayScanlineDataBundle(shapedView1, shapedView2);
                }
                else
                {
                    displayData = new DisplayScanlineDataBundle(view1Ordered, view2Ordered);
                }

                _removeBadChannel.RemoveInstableChennelOper(displayData);
                //图像增强需要缓存，单独开一个线程
                _imageEnhanceQueue.Enqueue(displayData);
            }
        }

        /// <summary>
        /// 数据采集服务的数据采集事件：得到实时采集的图像数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="scanlineData"></param>
        //[MethodImpl(MethodImplOptions.Synchronized)]
        private void ServicePartOnScanlineCaptured(object sender, RawScanlineDataBundle scanlineData)
        {
            if (!_isEmgcSwitchOn)
            {
                // 急停开关断开，不处理此期间的数据
                return;
            }

            if (IsXRayRisingNow)
            {
                // 跳过上升沿的数据,对处于上升沿的数据不做任何处理
                return;
            }

            // 对于射线打开期间，尝试自动更新满度，
            if (IsXRayOn && (_interruptAndMatchService == null || (_interruptAndMatchService != null && _interruptAndMatchService.InterruptState == InterruptMode.NotInInterrrupt)))
            {
                //开启动态更新后，不进行包前校正
                //if (_airAutoCalibrationMode != AutoCalibrationMode.DynamicUpdate)
                //{
                    //这里对电机状态加以限制，即在电机前进或倒退时，并且是在显示图像流程中，才更新
                    if (Direction != ConveyorDirection.Stop && (IsShowNewImageEnabled || isFlowLineMode))
                    {
                        if (_interruptAndMatchService == null || _interruptAndMatchService.InterruptState == InterruptMode.NotInInterrrupt)
                        {
                            ScanlineDataBundle air;
                            if (AutoCalibration.TryCalibrateAir(scanlineData, out air))
                            {
                                if (air != null)
                                {
                                    Tracer.TraceInfo("zyx before bag cali.reset air.");
                                    AutoCalibration.ResetManualCalibratedAir(air);
                                    Preprocess.ResetAir(air);
                                }
                            }
                        }
                    }
                //}
            }
            else
            {
                // 对于射线关闭期间，尝试自动更新本底
                ScanlineDataBundle ground;
                if (AutoCalibration.TryCalibrateGround(scanlineData, out ground))
                {
                    Preprocess.ResetGround(ground);
                }

            }

            // 射线打开，并且设置为显示新图像，则将数据填充到待处理队列中
            if (IsXRayOn && Direction != ConveyorDirection.Stop)
            {
                if (IsShowNewImageEnabled || isFlowLineMode)
                {
                    if (_interruptAndMatchService != null)
                    {
                        RawScanlineDataBundle singleBundle;
                        List<RawScanlineDataBundle> multipleBundles;

                        _interruptAndMatchService.Match(scanlineData, out singleBundle, out multipleBundles);

                        if (singleBundle != null)
                        {
                            DualViewMatch(singleBundle);
                        }
                        else if (multipleBundles != null && multipleBundles.Any())
                        {
                            foreach (var bundle in multipleBundles)
                            {
                                DualViewMatch(bundle);
                            }
                        }
                    }
                    else
                    {
                        DualViewMatch(scanlineData);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void DualViewMatch(RawScanlineDataBundle scanlineData)
        {
            if (IsDualView && _dualViewMatchingService != null)
            {
                // 对于双视角，先进行配准, 只将配准后的数据加入处理队列
                var matchedLine = _dualViewMatchingService.Match(scanlineData, Direction);
                if (matchedLine != null)
                {
                    lock (_waitingProcessQueue)
                    {
                        _waitingProcessQueue.Enqueue(matchedLine);
                    }
                }
            }
            else
            {
                lock (_waitingProcessQueue)
                {
                    // 对于单视角，直接显示
                    _waitingProcessQueue.Enqueue(scanlineData);
                }
            }
        }

        /// <summary>
        /// 清理各图像处理缓存池中数据
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void ClearCache()
        {
            if (_airLineChecking != null)
            {
                _airLineChecking.ClearCacheLines();
            }

            if (Preprocess != null)
            {
                Preprocess.ClearWienerCache();
                Preprocess.ClearLinesCache();
            }

            if (dpa != null)
            {
                dpa.ClearCache();
            }
            if (AutoCalibration != null)
            {
                AutoCalibration.Clearup();
            }
            if (_separater != null)
            {
                _separater.ClearLinesCache();
                _separater.ResetImageLinesCache();
            }
        }

        /// <summary>
        /// 是否显示新图像数据：true则实时显示新采集的图像数据，false 则不再显示
        /// </summary>
        private bool IsShowNewImageEnabled { get; set; }

        /// <summary>
        /// 输送机已经停止，不再显示新图像
        /// </summary>
        public void OnConveyorStopped()
        {
            IsShowNewImageEnabled = false;
        }

        private object savelock1 = new object();
        /// <summary>
        /// 输送机转动，可能是向左或向右
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void OnConveyorStarted(bool moveRight)
        {
            if (!IsShowNewImageEnabled)
            {
                ConveyorDirection realDirection = moveRight
                    ? (_reverseMotorDirection ? ConveyorDirection.MoveBackward : ConveyorDirection.MoveForward)
                    : (_reverseMotorDirection ? ConveyorDirection.MoveForward : ConveyorDirection.MoveBackward);

                InterruptMode beforeMotorActionInterruptMode = InterruptMode.NotInInterrrupt;

                if (_interruptAndMatchService != null)
                {
                    _interruptAndMatchService.OnUserControlMotor(realDirection);
                    beforeMotorActionInterruptMode = _interruptAndMatchService.InterruptState;
                }

                //只记录非停止状态下的方向
                if (realDirection != ConveyorDirection.Stop)
                {
                    if (_lastNotStpMoveDirection != realDirection)
                    {
                        if (beforeMotorActionInterruptMode == InterruptMode.NotInInterrrupt)
                        {
                            lock (savelock1)  //yxc
                            { SaveObjectAndFireEvent(); }
                        }
                        if (_separater != null)
                        {
                            _separater.ClearLinesCache();
                            _separater.ResetImageLinesCache();
                        }

                        if (Preprocess != null)
                            Preprocess.ClearLinesCache();
                        if (_airLineChecking != null)
                            _airLineChecking.ClearCacheLines();

                    }
                    _lastNotStpMoveDirection = realDirection;
                }

                // 输送机刚转动，清除双视角配准缓存，重新开始配准
                if (IsDualView && _dualViewMatchingService != null)
                {
                    if (_interruptAndMatchService == null || _interruptAndMatchService.InterruptState == InterruptMode.NotInInterrrupt || _interruptAndMatchService.LastMoveDirection != realDirection)
                    {
                        Tracer.TraceInfo("Dual view match caches will clear!");
                        // 输送机停止后，将双视角配准服务重置为初始状态
                        _dualViewMatchingService.ResetToUnmatched();
                    }
                    //Tracer.TraceInfo("Interrupt Mode: InterruptMode do not clear dual view match caches!");
                }

                IsShowNewImageEnabled = true;
            }

            if (_testDataProvider != null)
            {
                _testDataProvider.MotoStart();
            }
        }

        /// <summary>
        /// 回拉至最新一屏幕的数据
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public List<DisplayScanlineDataBundle> PullToLastScreen()
        {
            var result = _scanlinesPool.NavigateToLastScreen().ToList();

            // 回拉至最后一屏数据后，从内存中释放不在显示范围内的图像
            _scanlinesPool.ReleaseInvisibleImages();

            return result;
        }

        /// <summary>
        /// 更新当前显示的数据范围
        /// </summary>
        /// <param name="minNum"></param>
        /// <param name="maxNum"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void UpdateShowingRange(int minNum, int maxNum)
        {
            Tracer.TraceDebug($"[PullBackTimeoutTracked] ScanningImageDataProvider.UpdateShowingRange(int minNum, int maxNum)");
            var time1 = DateTime.Now;
            _scanlinesPool.ResetShowingRange(minNum, maxNum);
            Tracer.TraceDebug($"[PullBackTimeoutTracked] Invoke _scanlinesPool.ResetShowingRange execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
            time1 = DateTime.Now;
            _scanlinesPool.ReleaseInvisibleImages();
            Tracer.TraceDebug($"[PullBackTimeoutTracked] Invoke _scanlinesPool.ReleaseInvisibleImages execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
        }

        /// <summary>
        /// 清空回拉缓存(指存储历史图像记录的图像记录缓存)
        /// 本清空缓存函数在拉动方向改变、按下电机控制按键清空回拉图像并显示当前最后扫描图像时调用
        /// </summary>
        public void ClearPullBackCache()
        {
            _scanlinesPool.ClearPullBackCache();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public IEnumerable<DisplayScanlineDataBundle> PullBack()
        {
            _scanlinesPool.ReleaseInvisibleImages();
            var time1 = DateTime.Now;
            var result = _scanlinesPool.NavigateBack();
            Tracer.TraceDebug($"[PullBackTimeoutTracked] NavigateBack total execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
            return result;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public IEnumerable<DisplayScanlineDataBundle> PullForward()
        {
            _scanlinesPool.ReleaseInvisibleImages();
            var time1 = DateTime.Now;
            var result = _scanlinesPool.NavigateFront();
            Tracer.TraceDebug($"[PullBackTimeoutTracked] NavigateFront total execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
            return result;
        }

        /// <summary>
        /// 获取所有当前正在显示中的图像
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public IEnumerable<ImageRecord> GetShowingImages()
        {
            return _scanlinesPool.GetShowingImages();
        }

        /// <summary>
        /// 处理一次Tip图像注入事件，将新注入的Tip图像缓存，并与新图像一起存储至磁盘
        /// </summary>
        /// <param name="tipImage">被注入的Tip图像</param>
        /// <param name="globalRegion">Tip图像注入的全局位置信息</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SaveCurrentTipImage(XRayScanlinesImage tipImage, MarkerRegion globalRegion)
        {
            _scanlinesPool.SaveCurrentTipImage(tipImage, globalRegion);
        }

        /// <summary>
        /// 当用户错过或识别出tip，将当前注入的tip区域设置为可见
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void ShowCurrentTipMarker()
        {
            _scanlinesPool.SetCurrentTipImageVisible();
        }


        public void OnContrabandDetected(MarkerRegion region, DetectViewIndex viewIndex)
        {
            _scanlinesPool.OnContrabandDetected(region, viewIndex);
        }

        private List<ClassifiedLineDataBundle> GetWhiteDisplayBundles(int linesCount, int view1ChannelsCount, int view2ChannelsCount)
        {
            List<ClassifiedLineDataBundle> bundles = new List<ClassifiedLineDataBundle>();
            ushort[] view1Data = new ushort[view1ChannelsCount];

            for (int i = 0; i < view1ChannelsCount; i++)
            {
                view1Data[i] = 65535;
            }
            ClassifiedLineData classifiedView1LineData = new ClassifiedLineData(DetectViewIndex.View1, view1Data, view1Data, view1Data, view1Data, view1Data, true);

            ClassifiedLineData classifiedView2LineData = null;
            if (view2ChannelsCount > 0)
            {
                var view2Data = new ushort[view2ChannelsCount];
                for (int i = 0; i < view2ChannelsCount; i++)
                {
                    view2Data[i] = 65535;
                }
                classifiedView2LineData = new ClassifiedLineData(DetectViewIndex.View2, view2Data, view2Data, view2Data, view2Data, view2Data, true);
            }

            for (int i = 0; i < linesCount; i++)
            {
                bundles.Add(new ClassifiedLineDataBundle(classifiedView1LineData, classifiedView2LineData));
            }
            return bundles;
        }

        #region 模拟数据

        private Random _grayDataRandom = new Random();

        /// <summary>
        /// 填充模拟数据图像
        /// </summary>
        /// <param name="args"></param>
        private void GetSimulatedData(GetImageLinesEventArgs args)
        {
            var image1 = args.Image1Updater;
            var image2 = args.Image2Updater;

            if (IsShowNewImageEnabled)
            {
                // 内部默认的探测通道数为800
                var pixelsCount = 800;

                // 要填充的组合数据，其具体格式为：1个灰度值后，紧跟一个颜色值，并依此循环排列

                var rows = 4;
                var composedData = new ushort[rows * (pixelsCount << 1)];

                // 随机生成灰度值和颜色值
                var gray = _grayDataRandom.Next(10000, 45000);
                var colorIndex = _grayDataRandom.Next(0, 31);

                // 将灰度值和颜色值，按指定的排列方式，分别填充到组合数据中
                for (var i = 0; i < composedData.Length; i += 2)
                {
                    composedData[i] = (ushort)gray;
                    composedData[i + 1] = (ushort)colorIndex;
                }

                // 将组合数据，填充到图像缓存中
                image1.AppendImageRows(composedData, rows);

                if (image2 != null)
                {
                    image2.AppendImageRows(composedData, rows);
                }
            }
        }


        private void OnSimuMotorStateChanged(object sender, TestDataProvider.SimuMotorState e)
        {
            Direction = e == TestDataProvider.SimuMotorState.Moveforward
                ? ConveyorDirection.MoveForward
                : ConveyorDirection.Stop;
        }

        private void OnSimuXrayStateChanged(object sender, TestDataProvider.SimuXRayState e)
        {
            if (e == TestDataProvider.SimuXRayState.On)
            {
                LastXRayOnTime = DateTime.Now;
                IsXRayOn = true;
                AutoCalibration.OnXRayStateChanged(true);
            }
            else if (e == TestDataProvider.SimuXRayState.Off)
            {
                IsXRayOn = false;
                AutoCalibration.OnXRayStateChanged(false);
            }
        }

        #endregion 模拟数据


        #region 离线数据
        public string SimuDataPath { get; set; }
        //前面低能，后面高能
        int nlength = 1280;
        int _CalibrateLen = 128;
        int nChannelView1 = 1152;
        int nChannelView2 = 1216;
        //            ushort[][] rawData = new ushort[this.arrayRows][];
        ushort[][] view1HeData;
        ushort[][] view1LeData;
        ushort[][] view2HeData;
        ushort[][] view2LeData;

        List<string> _viewPathList = new List<string>();

        private Thread _simulatorThread = null;
        public bool IsSimulate { get; set; } = false;
        static int nSimuLineId = 0;
        static int nSimuFileId = 0;
        int _simulatorSleepInterval = 5;

        public void Simulate(int _simuInterval = 4)
        {
            _simulatorSleepInterval = _simuInterval;
            IsSimulate = !IsSimulate;
            if (_simulatorThread == null)
            {
                ReadOfflineFileData(nSimuFileId);
                _simulatorThread = new Thread(SimulateScanLineRoutine);
                _simulatorThread.IsBackground = true;
                _simulatorThread.Start();
            }
            if (IsSimulate)
            {
                nSimuLineId = 0;
                _dualViewMatchingService.ResetToUnmatched();
                this.Direction = ConveyorDirection.MoveForward;
                Tracer.TraceInfo("simulator start");
            }
            else
            {
                this.Direction = ConveyorDirection.Stop;
                Tracer.TraceInfo("simulator stop");
            }
        }

        private int GetViewLineLength(string strPath)
        {
            string fileName = Path.Combine(SimuDataPath, strPath);
            FileInfo fileInfo = new FileInfo(fileName);
            int nLineLength = (int)(fileInfo.Length / nlength / 2 / sizeof(ushort));
            return nLineLength;
        }
        private List<string> GetViewFiles(string strPath)
        {
            return Directory.GetFiles(SimuDataPath, "*view0*", SearchOption.AllDirectories).ToList();
        }

        public void SimuImageInfo()
        {
            _viewPathList = GetViewFiles(SimuDataPath);
            nChannelView1 = GetViewLineLength("0Ground");
            nChannelView2 = GetViewLineLength("1Ground");
            view1HeData = new ushort[nlength][];
            view1LeData = new ushort[nlength][];
            view2HeData = new ushort[nlength][];
            view2LeData = new ushort[nlength][];
        }

        public void SimuCalibrateGround()
        {

            string fileName1 = Path.Combine(SimuDataPath, "0Ground");
            string fileName2 = Path.Combine(SimuDataPath, "1Ground");
            ReadScanlineData(fileName1, ref view1HeData, ref view1LeData, nChannelView1, _CalibrateLen);
            ReadScanlineData(fileName2, ref view2HeData, ref view2LeData, nChannelView2, _CalibrateLen);

            for (int i = 0; i < _CalibrateLen; i++)
            {
                RawScanlineData view1Data = new RawScanlineData(DetectViewIndex.View1, view1LeData[i], view1HeData[i]);
                RawScanlineData view2Data = new RawScanlineData(DetectViewIndex.View2, view2LeData[i], view2HeData[i]);
                RawScanlineDataBundle rawScanlineDataBundle = new RawScanlineDataBundle(view1Data, view2Data);
                ManualCalibrationService.Service.CalibrateGround(rawScanlineDataBundle);
            }
        }

        public void SimuCalibrateAir()
        {
            string fileName1 = Path.Combine(SimuDataPath, "0Air");
            string fileName2 = Path.Combine(SimuDataPath, "1Air");
            ReadScanlineData(fileName1, ref view1HeData, ref view1LeData, nChannelView1, _CalibrateLen);
            ReadScanlineData(fileName2, ref view2HeData, ref view2LeData, nChannelView2, _CalibrateLen);

            for (int i = 0; i < _CalibrateLen; i++)
            {
                RawScanlineData view1Data = new RawScanlineData(DetectViewIndex.View1, view1LeData[i], view1HeData[i]);
                RawScanlineData view2Data = new RawScanlineData(DetectViewIndex.View2, view2LeData[i], view2HeData[i]);
                RawScanlineDataBundle rawScanlineDataBundle = new RawScanlineDataBundle(view1Data, view2Data);
                ManualCalibrationService.Service.CalibrateAir(rawScanlineDataBundle);
            }
        }

        private Matrix<ushort> getViewSingleEnergyData(ushort[][] viewEnergyData, int nlength, int nChannelView)
        {
            Matrix<ushort> _viewSingleEnergyData = new Matrix<ushort>(nlength, nChannelView);
            for (int i = 0; i < viewEnergyData.Length; i++)
            {
                for (int j = 0; j < viewEnergyData[i].Length; j++)
                {
                    _viewSingleEnergyData.Data[i, j] = viewEnergyData[i][j];
                }
            }
            return _viewSingleEnergyData;
        }

        private void ReadOfflineFileData(int nFileId)
        {
            if (nFileId < _viewPathList.Count)
            {
                string fileName1 = _viewPathList[nFileId];
                string fileName2 = fileName1.Replace("view0", "view1");
                ReadScanlineData(fileName1, ref view1HeData, ref view1LeData, nChannelView1, nlength);
                ReadScanlineData(fileName2, ref view2HeData, ref view2LeData, nChannelView2, nlength);
            }
        }

        private void SimulateScanLineRoutine()
        {
            while (true)
            {
                if (IsSimulate)
                {
                    RawScanlineData view1Data = new RawScanlineData(DetectViewIndex.View1, view1LeData[nSimuLineId], view1HeData[nSimuLineId]);
                    RawScanlineData view2Data = new RawScanlineData(DetectViewIndex.View2, view2LeData[nSimuLineId], view2HeData[nSimuLineId]);
                    RawScanlineDataBundle rawScanlineDataBundle = new RawScanlineDataBundle(view1Data, view2Data);
                    //DualViewMatch(rawScanlineDataBundle);
                    lock (_waitingProcessQueue)
                    {
                        // 对于单视角，直接显示
                        _waitingProcessQueue.Enqueue(rawScanlineDataBundle);
                    }
                    nSimuLineId++;
                    if (nSimuLineId >= nlength)
                    {
                        nSimuLineId = 0;
                        _dualViewMatchingService.ResetToUnmatched();
                        IsSimulate = true;
                        nSimuFileId++;
                        if (nSimuFileId >= _viewPathList.Count)
                        {
                            nSimuFileId = 0;
                        }
                        ReadOfflineFileData(nSimuFileId);
                    }
                }
                if (nSimuLineId == 0 || !IsSimulate)
                {
                    Thread.Sleep(_simulatorSleepInterval);
                }
            }
        }


        private void ReadScanlineData(string filename, ref ushort[][] heData, ref ushort[][] leData, int nChannels, int lineCount)
        {
            FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
            BinaryReader binaryReader = new BinaryReader(fileStream);
            for (int i = 0; i < lineCount; i++)
            {
                leData[i] = new ushort[nChannels];
                for (int j = 0; j < nChannels; j++)
                {
                    leData[i][j] = binaryReader.ReadUInt16();
                }
                heData[i] = new ushort[nChannels];
                for (int j = 0; j < nChannels; j++)
                {
                    heData[i][j] = binaryReader.ReadUInt16();
                }
            }
            binaryReader.Close();
            fileStream.Close();
        }
        #endregion

    }
}
