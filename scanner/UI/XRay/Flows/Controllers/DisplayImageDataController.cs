using Emgu.CV.Flann;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Common.Utilities;
using UI.XRay.Control;
using UI.XRay.Flows.Services;
using UI.XRay.RenderEngine;
using System.Threading.Tasks;
using System.IO;
using System.Drawing.Imaging;
using UI.XRay.Flows.TRSNetwork;
using UI.XRay.Business.Algo;
using System.Drawing;
using System.Runtime.InteropServices;
using Emgu.CV;
using UI.XRay.Flows.Services.DataProcess;
using System.Threading;

namespace UI.XRay.Flows.Controllers
{
    /// <summary>
    /// 当前显示的图像数据的控制器：分三种应用场景，分别是扫描图像数据、培训图像数据、回放图像数据
    /// </summary>
    public class DisplayImageDataController
    {
        [DllImport("ImageEdgeEnhanceLib.dll", CallingConvention = CallingConvention.Cdecl)]
        private extern static int Sifenbian(IntPtr src, IntPtr dst, int height, int width, int stride);


        /// <summary>
        /// 卷轴图像数据更新控制器，通过此接口更新数据图像
        /// </summary>
        public RollingImageDataUpdateController ImageDataUpdateController { get; private set; }

        /// <summary>
        /// 流式图像的数据控制器：管理对图像的数据填充
        /// </summary>
        internal ScanningImageDataProvider ScanningDataProvider { get; private set; }
        public ScanningImageDataProvider ScanningDataProviderSimulator { get => ScanningDataProvider; }

        /// <summary>
        /// 培训图像数据控制器
        /// </summary>
        internal TrainingImageDataProvider TrainingDataProvider { get; private set; }

        /// <summary>
        /// 回放图像数据控制器
        /// </summary>
        internal PlaybackImageDataProvider PlaybackDataProvider { get; private set; }

        /// <summary>
        /// Tip注入流程
        /// </summary>
        internal TipInjectionFlow _TipInjectionFlow { get; private set; }

        /// <summary>
        /// 事件：Tip图像注入完毕
        /// </summary>
        public event Action<TipInjection> TipImageInjected;

        /// <summary>
        /// 事件：Tip漏判（Tip插入完成后，用户未在规定时间内发现）
        /// </summary>
        public event Action TipMissed;

        /// <summary>
        /// 事件：Tip识别成功
        /// </summary>
        public event Action TipIdentified;

        public event EventHandler<int> SessionBagCountChangedWeakEvent
        {
            add { _sessionBagCountChangedWeakEvent.Add(value); }
            remove { _sessionBagCountChangedWeakEvent.Remove(value); }
        }

        private SmartWeakEvent<EventHandler<int>> _sessionBagCountChangedWeakEvent = new SmartWeakEvent<EventHandler<int>>();

        /// <summary>
        /// 当前是否为培训模式：非培训模式与培训模式下，都支持切换到回放模式
        /// </summary>
        public bool IsTrainingMode { get; private set; }

        private bool _showImageBasedOnMotorDirection = false;
        /// <summary>
        /// 显示图像的方向基于电机的方向
        /// </summary>
        public bool ShowImageBasedOnMotorDirection
        {
            get { return _showImageBasedOnMotorDirection; }
            private set { _showImageBasedOnMotorDirection = value; }
        }

        /// <summary>
        /// 是否是双向扫描
        /// </summary>
        private bool _bidirectionalScan = false;

        /// <summary>
        /// 当前是否为图像回放模式
        /// </summary>
        public bool IsPlayingback { get; private set; }

        /// <summary>
        /// 高密度报警时，是否停止输送机
        /// </summary>
        private bool _hdiStopConveyor;

        /// <summary>
        /// 毒品报警时，是否停止输送机
        /// </summary>
        private bool _deiStopConveyor;

        /// <summary>
        /// 爆炸物报警时，是否停止输送机
        /// </summary>
        private bool _eiStopConveyor;

        public bool CanClearTip { get; private set; }

        public bool IsTipInjecting
        {
            get
            {
                return _TipInjectionFlow.IsTipInjecting;
            }
        }

        public bool HasTipInjectEntity
        {
            get
            {
                return _TipInjectionFlow.HasTipInjectEntity;
            }
        }

        private bool _anchorNewWhenEnd;

        private int _sifenbianIndex = 0;

        private bool _isTwoBadChannelFlags;

        /// <summary>
        /// 是否启用自动报警功能
        /// </summary>
        public bool IsIntelliSenseEnabled
        {
            get { return ImageDataUpdateController.ShowIntelliSenseMarkers; }
            set
            {
                ImageDataUpdateController.ShowIntelliSenseMarkers = value;
                ContrabandAlarmService.Service.IsAlarmEnabled = value;
            }
        }

        public bool IsSendDataCompleted
        {
            get { return ImageDataUpdateController.IsShowLinesDataCompleted && ScanningDataProvider.Direction == ConveyorDirection.Stop; }
        }

        /// <summary>
        /// 当前是否正在拉动查看图像
        /// </summary>
        public bool IsPulling
        {
            get { return PullingMode != ImagePullingMode.None; }
        }

        protected ImagePullingMode PullingMode { get; private set; }

        private bool _image1MoveRightToLeft;

        /// <summary>
        /// 当前是否正在扫描成像。当电机转动时，为true，不可以回拉
        /// </summary>
        public bool IsScanning { get; set; }

        public bool IsTraining { get; set; }

        public int ScreenMaxLinesCount { get; private set; }

        /// <summary>
        /// 视角个数
        /// </summary>
        private int _viewsCount;

        private IRollingImageProcessController _processController;

        //记录上次电机的运动方向。电机方向变化后改变图像显示的方向
        private MotorDirection _curMotorDirection = MotorDirection.Stop;
        private MotorDirection _lastMotorDirection = MotorDirection.Stop;

        private int _histogramBegin = 0;
        private int _histogramEnd = 500 * 16;
        private int _stretchBegin = 1000 * 16;
        private int _stretchEnd = 4000 * 16;
        private int _showThreshold = 2500;
        private int _windowSize = 3;

        private int _unpenetrateHeight = 60;
        private int _unpenetrateWidth = 60;
        private int _unpenetrateLowerlimit = 20000;
        private int _unpenetrateUpperlimit = 40000;

        private int _findendThreshold = 2200;

        private int _calAlphaStart = 20;
        private int _calAlphaEnd = 55;

        private bool _newEnable = false;
        public bool newEnable
        {
            get { return _newEnable; }
        }
        private bool _calHistogramAlpha = false;
        public bool CalAlpha
        {
            get { return _calHistogramAlpha; }
        }

        private int _divideMode = 0;

        private bool _enableFilter = false;

        XRayImageProcessor  _imageProcessor;

        /// <summary>
        ///  是否处于中断恢复状态中
        /// </summary>
        public bool InInterruptRecovering
        {
            get { return ScanningDataProvider != null && ScanningDataProvider.InInterruptBackwardOrRecovering; }
        }

        /// <summary>
        /// 是否处于正常状态(非中断)下
        /// </summary>
        public bool IsInNomalMode
        {
            get { return ScanningDataProvider != null && ScanningDataProvider.IsInNormalMode; }
        }
        public void ExitInterruptRecovering()
        {
            if (ScanningDataProvider != null)
            {
                ScanningDataProvider.ExitInterruptRecovering();
            }
        }

        List<KeyValuePair<DetectViewIndex, List<MarkerRegion>>> _markerRegions = new List<KeyValuePair<DetectViewIndex, List<MarkerRegion>>>();
        public LinkedList<DisplayScanlineDataBundle> CurrentScreenRawScanLines = new LinkedList<DisplayScanlineDataBundle>();

        private bool _isScreenShotViewReverse = false;

        private BanfengAlgo _banfengAlgo0;
        private BanfengAlgo _banfengAlgo1;
        /// <summary>
        /// 图像畸变矫正
        /// </summary>
        private ShapeCorrectionService _shapeCorrection;

        ChannelBadFlagsFileWatchingService _badFlagWatcher = new ChannelBadFlagsFileWatchingService();
        /// <summary>
        /// 物质分类处理
        /// </summary>
        private MatClassifyRoutine _classifier;

        /// <summary>
        /// 坏点插值
        /// </summary>
        private BadChannelInterpolation _badPointsInterpolation;

        public DisplayImageDataController(IRollingImageProcessController processController)
        {
            try
            {
                _processController = processController;
                _imageProcessor = new XRayImageProcessor();
                ScannerConfig.ConfigChanged += ScannerConfigOnConfigChanged;
                LoadSettings();
                InitHistogramConfig();
                InitTrainingMode();
                InitScreenShotConfig();
                IsPlayingback = false;
                PullingMode = ImagePullingMode.None;
                ScreenMaxLinesCount = processController.Image1.MaxLinesCount;
                ImageDataUpdateController = new RollingImageDataUpdateController(processController);
                ImageDataUpdateController.ContrabandDetected += ImageDataUpdateControllerOnContrabandDetected;
                ScanningDataProvider = new ScanningImageDataProvider(ScreenMaxLinesCount);
                ScanningDataProvider.DrawRectAction += PlaybackDataProvider_DrawRectAction;

                _classifier = new MatClassifyRoutine();
                _badPointsInterpolation = new BadChannelInterpolation(_classifier);

                _banfengAlgo0 = new BanfengAlgo(0, true);
                _banfengAlgo1 = new BanfengAlgo(1, true);

                //初始化畸变矫正
                _shapeCorrection = new ShapeCorrectionService();
                int count = ExchangeDirectionConfig.Service.GetView1ImageHeight();
                _shapeCorrection.View1ImageHeight = count;

                count = ExchangeDirectionConfig.Service.GetView2ImageHeight();
                _shapeCorrection.View2ImageHeight = count;


                _badFlagWatcher.View1ChannelBadFlagsUpdated += BadFlagWatcherOnView1SetBadUpdated;
                _badFlagWatcher.View2ChannelBadFlagsUpdated += BadFlagWatcherOnView2SetBadUpdated;
                _TipInjectionFlow = new TipInjectionFlow();
                _TipInjectionFlow.TipImageInjected += TipInjectionFlowOnTipImageInjected;
                _TipInjectionFlow.TipMissed += TipInjectionFlowOnTipMissed;
                _TipInjectionFlow.TipIdentified += TipInjectionFlowOnTipIdentified;

                AIJudgeImageService.Service.BoxReady += Service_BoxReady;

                LoginAccountManager.Service.AccountLogout += Service_AccountLogout;

                HttpNetworkController.Controller.ConveyorDirectionAction += Controller_ConveyorDirectionAction;
                HttpNetworkController.Controller.TipInjectUpdateAction += Controller_TipInjectUpdateAction;
                HttpNetworkController.Controller.RemoteTipPlanUpdateAction += UpdateNetTipPlan;
                HttpNetworkController.Controller.OnScreenshotAction += OnScreenshotExecution;
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception, "Unhandled exception in DisplayImageDataController constructor.");
            }
        }

        private void BadFlagWatcherOnView1SetBadUpdated(List<BadChannel> list)
        {
            _badPointsInterpolation.ResetView1ManualSetBadChannels(list);
            if (_banfengAlgo0 != null)
                _banfengAlgo0.Init(0, true);
        }

        private void BadFlagWatcherOnView2SetBadUpdated(List<BadChannel> list)
        {
            _badPointsInterpolation.ResetView2ManualSetBadChannels(list);
            if (_banfengAlgo1 != null)
            {
                _banfengAlgo1.Init(1, true);
            }
        }

        void Service_BoxReady(List<MarkBox> boxs)
        {
            foreach (var box in boxs)
            {
                MarkerRegion mr = new MarkerRegion(MarkerRegionType.Knife, box.X, box.X + box.Width, box.Y, box.Y + box.Height);
                mr.Name = box.Name;
                mr.RectColor = box.RectColor;
                ImageDataUpdateController.AddRegionMark(mr, box.View);

                ImageDataUpdateControllerOnContrabandDetected(new KeyValuePair<DetectViewIndex, MarkerRegion>(box.View, mr));
            }
        }

        /// <summary>
        /// 参数:int 0:水平视角 1:垂直视角 2:全视角
        /// </summary>
        /// <param name="view"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void OnScreenshotExecution(int view)
        {
            var lines = ImageDataUpdateController.GetCurrentScreenData();
            if (lines.Count < 1) return;
            try
            {
                switch (view)
                {
                    case 2:
                        if (lines.Select(l => l.View2Data).Contains(null))
                        {
                            Tracer.TraceInfo(string.Format("View2Data has no data.(Func:Controller_OnScreenshotAction,view:{0})", view));
                            return;
                        }
                        _imageProcessor.AttachImageData(lines.Select(l => l.View1Data).ToList());
                        var image1 = _imageProcessor.GetBitmap(ExportImageEffects.Regular);
                        Bitmap image2 = null, combineImage;
                        if (!lines.Select(l => l.View2Data).Contains(null))
                        {
                            _imageProcessor.AttachImageData(lines.Select(l => l.View2Data).ToList());
                            image2 = _imageProcessor.GetBitmap(ExportImageEffects.Regular);
                        }

                        if (image2 != null)
                        {
                            if (!_isScreenShotViewReverse)
                            {
                                combineImage = BitmapHelper.CombineBmp(image1, image2);
                            }
                            else
                            {
                                combineImage = BitmapHelper.CombineBmp(image2, image1);
                            }
                        }
                        else
                        {
                            combineImage = image1;
                        }

                        SendData(combineImage, view);
                        break;
                    case 0:
                        if (!_isScreenShotViewReverse)
                        {
                            _imageProcessor.AttachImageData(lines.Select(l => l.View1Data).ToList());
                        }
                        else
                        {
                            if (lines.Select(l => l.View2Data).Contains(null))
                            {
                                Tracer.TraceInfo(string.Format("View2Data has no data.(Func:Controller_OnScreenshotAction,view:{0})", view));
                                return;
                            }
                            _imageProcessor.AttachImageData(lines.Select(l => l.View2Data).ToList());
                        }
                        
                        var image_1 = _imageProcessor.GetBitmap(ExportImageEffects.Regular);
                        SendData(image_1, view);
                        break;
                    case 1:
                        if (!_isScreenShotViewReverse)
                        {
                            if (lines.Select(l => l.View2Data).Contains(null))
                            {
                                Tracer.TraceInfo(string.Format("View2Data has no data.(Func:Controller_OnScreenshotAction,view:{0})", view));
                                return;
                            }
                            _imageProcessor.AttachImageData(lines.Select(l => l.View2Data).ToList());
                        }
                        else
                        {
                            _imageProcessor.AttachImageData(lines.Select(l => l.View1Data).ToList());
                        }
                        
                        var image_2 = _imageProcessor.GetBitmap(ExportImageEffects.Regular);
                        SendData(image_2, view);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Tracer.TraceInfo("Error occur in Controller_OnScreenshotAction function");
                Tracer.TraceException(ex);
            }           
        }
        
        private void SendData(Bitmap image,int view)
        {
			if(image == null)
				return;
			using (MemoryStream ms = new MemoryStream())
			{
				image.Save(ms, ImageFormat.Jpeg);
				ms.Seek(0, SeekOrigin.Begin);
				byte[] data = new byte[ms.Length];
				ms.Read(data, 0, data.Length);
				TRSNetWorkService.Service.SendScreenshotFile(data, view);
				Tracer.TraceInfo(string.Format("SendScreenshotFile function send {0} bytes to view:{1}.", data?.Length, view));
			}
        }

        private void SendScreenShotFile(List<DisplayScanlineData> lines,int view)
        {
            _imageProcessor.AttachImageData(lines);
            var image = _imageProcessor.GetBitmap(ExportImageEffects.Regular);
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Jpeg);
                ms.Seek(0, SeekOrigin.Begin);
                byte[] data = new byte[ms.Length];
                ms.Read(data, 0, data.Length);
                TRSNetWorkService.Service.SendScreenshotFile(data, view);
            }
        }

        /// <summary>
        /// 根据本地Tip计划和图像，更新Tip数据
        /// </summary>
        public void UpdateNetTipPlan()
        {
            try
            {
                Tracer.TraceInfo("MainViewModel update net tip plan");
                var _imagecontroller = new TipPlanandImageController();
                var _controller = new TipPlansController();
                //获取数据库中的TipPlanImage并从数据库中删除
                List<TipPlanandImage> allplanandImages = new List<TipPlanandImage>(_imagecontroller.GetAllPlans());
                if (allplanandImages.Count > 0)
                {
                    foreach (var item in allplanandImages)
                        _imagecontroller.Remove(item);
                }
                //获取数据库中的TipPlan并从数据库中删除
                List<TipPlan> allplans = new List<TipPlan>(_controller.GetAllPlans());
                //if (allplans.Count > 0)
                //{
                //    foreach (var item in allplans)
                //        _controller.Remove(item);
                //}

                //解析接收的TipPlan计划并添加到数据库
                List<TipPlan> tipplanList = UpdateTipPlan();
                foreach (var item in tipplanList)
                {
                    var same=allplans.FirstOrDefault(i=> (i.Alias==item.Alias)||(i.TipPlanId==item.TipPlanId));
                    if (same==null)
                    {
                        _controller.AddOrUpdate(item);             
                    } 
                    else
                    {
                        Tracer.TraceInfo("update net tip plan error,plan id:"+item.TipPlanId+",plan alias:"+item.Alias);
                    }
                }   
                //解析接收的TipPlanImage并添加到数据库
                List<TipPlanandImage> tipplanandimageList = UpdateTipPlanandimage();
                foreach (var item1 in tipplanandimageList)
                    _imagecontroller.AddOrUpdate(item1);


                Controller_TipInjectUpdateAction();

                Transmission.RemoteTipUpdated = false;
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
        /// 从xml文件中解析接收的TipPlan计划
        /// </summary>
        /// <returns></returns>
        private List<TipPlan> UpdateTipPlan()
        {
            List<TipPlan> tipplanList = new List<TipPlan>();
            try
            {
                string _accountxmlFile = "D:\\SecurityScanner\\Database\\" + "plan.xml";
                tipplanList = ReadWriteXml.ReadXmlFromFile<List<TipPlan>>(_accountxmlFile, System.Text.Encoding.UTF8);
                Tracer.TraceInfo("MainViewModel update tip plans count " + tipplanList.Count);
                return tipplanList;
            }
            catch (Exception e)
            {
                return tipplanList;
            }
        }

        /// <summary>
        /// 从xml文件中解析接收的TipPlanImage
        /// </summary>
        /// <returns></returns>
        private List<TipPlanandImage> UpdateTipPlanandimage()
        {
            List<TipPlanandImage> tipplanandimageList = new List<TipPlanandImage>();
            try
            {
                string _accountxmlFile = "D:\\SecurityScanner\\Database\\" + "planandimage.xml";
                tipplanandimageList = ReadWriteXml.ReadXmlFromFile<List<TipPlanandImage>>(_accountxmlFile, System.Text.Encoding.UTF8);
                Tracer.TraceInfo("MainViewModel update tip plan images count " + tipplanandimageList.Count);
                return tipplanandimageList;
            }
            catch (Exception e)
            {
                return tipplanandimageList;
            }
        }

        /// <summary>
        /// 扫描图像时，如果处于放大状态就聚焦到新图像上
        /// </summary>
        void ScanningDataProvider_NewBagComeAction()
        {
            if (_anchorNewWhenEnd)
            {
                if (_processController != null)
                {
                    var multiply = _processController.Image1.ZoomMultiples;
                    if (multiply > 1.0)
                    {
                        var image1 = _processController.Image1;
                        var image2 = _processController.Image2;
                        if (_processController.RightToLeft)
                        {
                            image1.ImageMultipleAnchor = UI.XRay.ImagePlant.Gpu.ImageMultipleAnchor.Right;
                            image1.Zoom(image1.ZoomMultiples);
                            if (image2 != null)
                            {
                                image2.ImageMultipleAnchor = UI.XRay.ImagePlant.Gpu.ImageMultipleAnchor.Right;
                                image2.Zoom(image1.ZoomMultiples);
                            }
                        }
                        else
                        {
                            image1.ImageMultipleAnchor = UI.XRay.ImagePlant.Gpu.ImageMultipleAnchor.Left;
                            image1.Zoom(image1.ZoomMultiples);
                            if (image2 != null)
                            {
                                image2.ImageMultipleAnchor = UI.XRay.ImagePlant.Gpu.ImageMultipleAnchor.Left;
                                image2.Zoom(image1.ZoomMultiples);
                            }
                        }
                    }
                }
            }
        }

        void Service_AccountLogout(object sender, Account e)
        {
            //当前账户退出后，需要清空屏幕
            ImageDataUpdateController.DropUnshownLines();
            ImageDataUpdateController.ClearAndAppend(null);
            ImageDataUpdateController.ClearMarkerBoxes();

            //清空未显示的数据
            if (TrainingDataProvider != null)
            {
                TrainingDataProvider.ClearCachedlines();
            }
        }

        /// <summary>
        /// 刷新tip
        /// </summary>
        public void Controller_TipInjectUpdateAction()
        {
            UpdatTipPlan();
            //刷新TipFlow
            if (_TipInjectionFlow != null)
            {
                _TipInjectionFlow.TipImageInjected -= TipInjectionFlowOnTipImageInjected;
                _TipInjectionFlow.TipMissed -= TipInjectionFlowOnTipMissed;
                _TipInjectionFlow.TipIdentified -= TipInjectionFlowOnTipIdentified;
                _TipInjectionFlow.Cleanup();

                _TipInjectionFlow = new TipInjectionFlow();
                _TipInjectionFlow.TipImageInjected += TipInjectionFlowOnTipImageInjected;
                _TipInjectionFlow.TipMissed += TipInjectionFlowOnTipMissed;
                _TipInjectionFlow.TipIdentified += TipInjectionFlowOnTipIdentified;
            }
        }

        private void UpdatTipPlan()
        {
            var _controller = new TipPlansController();

            var Plans = _controller.GetAllPlans();
            foreach (var plan in Plans)
            {
                if (plan.IsEnabled)
                {
                    UpdateSelectTipLib(plan);
                }

            }
        }
        private void UpdateSelectTipLib(TipPlan plan)
        {
            var _imagecontroller = new TipPlanandImageController();
            var plansandImages = _imagecontroller.GetAllPlans();
            List<string> tipImageName = new List<string>();

            var _managementController1 = new TipImagesManagementController(TipLibrary.Explosives);
            _managementController1.DeleteSelectAllImages();
            var planwithImage = plansandImages.Where(p => p.TipPlanId == plan.TipPlanId && p.Library == "Explosives").ToList();
            foreach (var planwithimage in planwithImage)
            {
                tipImageName.Add(planwithimage.ImageName);
            }
            _managementController1.ChangeSelectTipLib(TipLibrary.Explosives, tipImageName);
            planwithImage.Clear();
            tipImageName.Clear();

            _managementController1 = new TipImagesManagementController(TipLibrary.Guns);
            _managementController1.DeleteSelectAllImages();
            planwithImage = plansandImages.Where(p => p.TipPlanId == plan.TipPlanId && p.Library == "Guns").ToList();
            foreach (var planwithimage in planwithImage)
            {
                tipImageName.Add(planwithimage.ImageName);
            }
            _managementController1.ChangeSelectTipLib(TipLibrary.Guns, tipImageName);
            planwithImage.Clear();
            tipImageName.Clear();

            _managementController1 = new TipImagesManagementController(TipLibrary.Knives);
            _managementController1.DeleteSelectAllImages();
            planwithImage = plansandImages.Where(p => p.TipPlanId == plan.TipPlanId && p.Library == "Knives").ToList();
            foreach (var planwithimage in planwithImage)
            {
                tipImageName.Add(planwithimage.ImageName);
            }
            _managementController1.ChangeSelectTipLib(TipLibrary.Knives, tipImageName);
            planwithImage.Clear();
            tipImageName.Clear();

            _managementController1 = new TipImagesManagementController(TipLibrary.Others);
            _managementController1.DeleteSelectAllImages();
            planwithImage = plansandImages.Where(p => p.TipPlanId == plan.TipPlanId && p.Library == "Others").ToList();
            foreach (var planwithimage in planwithImage)
            {
                tipImageName.Add(planwithimage.ImageName);
            }
            _managementController1.ChangeSelectTipLib(TipLibrary.Others, tipImageName);
            planwithImage.Clear();
            tipImageName.Clear();

        }

        void Controller_ConveyorDirectionAction(ConveyorDirection obj)
        {
            switch (obj)
            {
                case ConveyorDirection.MoveBackward:
                    OnConveyorLeftKeyDown();
                    break;
                case ConveyorDirection.MoveForward:
                    OnConveyorRightKeyDown();
                    break;
                case ConveyorDirection.Stop:
                    OnConveyorStopRequest();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 获取当前正在显示的图像
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ImageRecord> GetShowingImages()
        {
            if (IsPlayingback)
            {
                // 图像回放，暂时不支持获取当前显示的图像
                return null;
            }

            if (IsTrainingMode)
            {
                // 培训模式，暂时不支持获取当前显示的图像
                return null;
            }

            if (ScanningDataProvider != null)
            {
                return ScanningDataProvider.GetShowingImages();
            }

            return null;
        }

        /// <summary>
        /// Tip事件：用户成功标记出一个Tip图像
        /// </summary>
        /// <param name="args">此次Tip注入事件的具体信息</param>
        private void TipInjectionFlowOnTipIdentified(TipInjectionEventArgs args)
        {
            // 成功标记出Tip，弹出提示框，但是不停止电机
            ShowTipMarker(args);
            CanClearTip = true;
            if (TipIdentified != null)
            {
                TipIdentified();
            }
        }

        /// <summary>
        /// Tip事件：用户错过了Tip图像。（在规定时间内未标记出来）
        /// </summary>
        /// <param name="args">此次Tip注入事件的具体信息</param>
        private void TipInjectionFlowOnTipMissed(TipInjectionEventArgs args)
        {
            LoginAccountManager.Service.AddMissTipCount();

            // 显示Tip
            ShowTipMarker(args);
            CanClearTip = true;
            if (TipMissed != null)
            {
                TipMissed();
            }
        }

        /// <summary>
        /// Tip事件：Tip图像注入完毕
        /// </summary>
        /// <param name="tipInsertion">刚刚完成的Tip注入信息</param>
        private void TipInjectionFlowOnTipImageInjected(TipInjectionEventArgs tipInsertion)
        {
            LoginAccountManager.Service.AddTipInjectionCount();
            CanClearTip = false;

            // 如果是培训模式，则不处理；
            // 如果是正常检查模式，则将Tip图像加入到与注入起始位置相关联的图像数据中，随图像保存到磁盘
            if (!IsTrainingMode)
            {
                ScanningDataProvider.SaveCurrentTipImage(tipInsertion.TipImage, tipInsertion.InjectRegion);
            }
        }

        /// <summary>
        /// 当用户识别或漏判，显示Tip标记
        /// </summary>
        /// <param name="args"></param>
        private void ShowTipMarker(TipInjectionEventArgs args)
        {
            if (!IsTrainingMode)
            {
                ScanningDataProvider.ShowCurrentTipMarker();
            }

            ImageDataUpdateController.AddRegionMark(args.InjectRegion, DetectViewIndex.View1);
            ImageDataUpdateController.AddRegionMark(args.InjectRegion2, DetectViewIndex.View2);
        }

        /// <summary>
        /// 初始化培训模式设置
        /// </summary>
        private void InitTrainingMode()
        {
            bool isTraining = false;
            ScannerConfig.Read(ConfigPath.TrainingIsEnabled, out isTraining);
            IsTrainingMode = isTraining;

            if (IsTrainingMode)
            {
                TrainingDataProvider = new TrainingImageDataProvider();
                TrainingDataProvider.DataReady += TrainingDataProviderOnDataReady;
                TrainingDataProvider.NewImageIsReady += TrainingDataProviderOnNewImageIsReady;
            }
        }

        /// <summary>
        /// 一个培训图像已经准备完毕：尝试加载Tip图像
        /// </summary>
        private void TrainingDataProviderOnNewImageIsReady()
        {
            //ImageProcessAlgoRecommendService.Service().HandleObjectSeperated();

            if (_TipInjectionFlow != null)
            {
                _TipInjectionFlow.TryLoadNextTipImageAsync();
            }
        }

        /// <summary>
        /// 发现违禁品：报警
        /// </summary>
        /// <param name="markerRegion"></param>
        private void ImageDataUpdateControllerOnContrabandDetected(KeyValuePair<DetectViewIndex, MarkerRegion> markerRegion)
        {
            if (markerRegion.Value.RegionType == MarkerRegionType.UnPenetratable)
            {
                ContrabandAlarmService.Service.HdiAlert();

                if (_hdiStopConveyor && IsScanning && !IsTrainingMode)
                {
                    OnConveyorStopRequest();
                }
            }
            else if (markerRegion.Value.RegionType == MarkerRegionType.Drug)
            {
                ContrabandAlarmService.Service.DeiAlert();

                if (_deiStopConveyor && IsScanning && !IsTrainingMode)
                {
                    OnConveyorStopRequest();
                }
            }
            else if (markerRegion.Value.RegionType == MarkerRegionType.Explosives)
            {
                ContrabandAlarmService.Service.EiAlert();

                if (_eiStopConveyor && IsScanning && !IsTrainingMode)
                {
                    OnConveyorStopRequest();
                }
            }
            if (ImageDataUpdateController.ShowIntelliSenseMarkers)
            {
                //将探测区域信息传输到图像保存中去，方便图像保存完转成jpg等格式图像时可以获得探测区域信息
                ScanningDataProvider.OnContrabandDetected(markerRegion.Value, markerRegion.Key);
            }
        }

        /// <summary>
        /// 配置项发生变化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void ScannerConfigOnConfigChanged(object sender, EventArgs eventArgs)
        {
            LoadSettings();
            UpdateTrainingMode();
            InitHistogramConfig();
        }

        /// <summary>
        /// 读取图像默认的设置
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                if (!ScannerConfig.Read(ConfigPath.MachineViewsCount, out _viewsCount))
                {
                    _viewsCount = 1;
                }

                if (!ScannerConfig.Read(ConfigPath.Image1MoveRightToLeft, out _image1MoveRightToLeft))
                {
                    _image1MoveRightToLeft = false;
                }

                if (!ScannerConfig.Read(ConfigPath.ShowImageBasedOnMotorDirection, out _showImageBasedOnMotorDirection))
                {
                    _showImageBasedOnMotorDirection = false;
                }

                if (!ScannerConfig.Read(ConfigPath.MachineBiDirectionScan, out _bidirectionalScan))
                {
                    _bidirectionalScan = false;//默认单向扫描
                }

                if (!ScannerConfig.Read(ConfigPath.IntellisenseHdiStopConveyor, out _hdiStopConveyor))
                {
                    _hdiStopConveyor = false;
                }

                if (!ScannerConfig.Read(ConfigPath.IntellisenseDeiStopConveyor, out _deiStopConveyor))
                {
                    _deiStopConveyor = false;
                }

                if (!ScannerConfig.Read(ConfigPath.IntellisenseEiStopConveyor, out _eiStopConveyor))
                {
                    _eiStopConveyor = false;
                }

                if (!ScannerConfig.Read(ConfigPath.ImageAnchorNewWhenEnd, out _anchorNewWhenEnd))
                {
                    _anchorNewWhenEnd = false;
                }
                if (!ScannerConfig.Read(ConfigPath.PreProcHistogramSifenbian, out _sifenbianIndex))
                {
                    _sifenbianIndex = 0x01 | 0x02;
                }
                if (!ScannerConfig.Read(ConfigPath.EnableTwoBadChannelFlags, out _isTwoBadChannelFlags))
                {
                    _isTwoBadChannelFlags = false;
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        private void InitHistogramConfig()
        {
            if (!ScannerConfig.Read(ConfigPath.PreProcHistogramBegin,out _histogramBegin))
            {
                _histogramBegin = 0;
            }
            if (!ScannerConfig.Read(ConfigPath.PreProcHistogramEnd, out _histogramEnd))
            {
                _histogramEnd = 500*16;
            }
            if (!ScannerConfig.Read(ConfigPath.PreProcHistogramStretchBegin, out _stretchBegin))
            {
                _stretchBegin = 1000*16;
            }
            if (!ScannerConfig.Read(ConfigPath.PreProcHistogramStretchEnd, out _stretchEnd))
            {
                _stretchEnd = 4000*16;
            }
            if (!ScannerConfig.Read(ConfigPath.PreProcHistogramShowThreshold, out _showThreshold))
            {
                _showThreshold = 2500;
            }
            if (!ScannerConfig.Read(ConfigPath.PreProcHistogramEnableFilter, out _enableFilter))
            {
                _enableFilter = false;
            }
            if (!ScannerConfig.Read(ConfigPath.PreProcHistogramWindowSize,out _windowSize))
            {
                _windowSize = 3;
            }
            if (!ScannerConfig.Read(ConfigPath.PreProcHistogramUnpenetrateHeight, out _unpenetrateHeight))
            {
                _unpenetrateHeight = 60;
            }
            if (!ScannerConfig.Read(ConfigPath.PreProcHistogramUnpenetrateWidth, out _unpenetrateWidth))
            {
                _unpenetrateWidth = 60;
            }
            if (!ScannerConfig.Read(ConfigPath.PreProcHistogramUnpenetrateUnpenetrateLowerlimit, out _unpenetrateLowerlimit))
            {
                _unpenetrateLowerlimit = 20000;
            }
            if (!ScannerConfig.Read(ConfigPath.PreProcHistogramUnpenetrateUnpenetrateUpperlimit, out _unpenetrateUpperlimit))
            {
                _unpenetrateUpperlimit = 40000;
            }
            if (!ScannerConfig.Read(ConfigPath.PreProcHistogramFindEndThreshold, out _findendThreshold))
            {
                _findendThreshold = 2200;
            }
            if (!ScannerConfig.Read(ConfigPath.PreProcHistogramNewEnable, out _newEnable))
            {
                _newEnable = false;
            }
            if (!ScannerConfig.Read(ConfigPath.PreProcHistogramDivideMode, out _divideMode))
            {
                _divideMode = 0;
            }
            if (!ScannerConfig.Read(ConfigPath.PreProcHistogramCalHistogramAlpha, out _calHistogramAlpha))
            {
                _calHistogramAlpha = false;
            }

        }

        private void InitScreenShotConfig()
        {
            if (_viewsCount > 1)
            {
                if (!ScannerConfig.Read(ConfigPath.SingleViewIsScreenShotViewReverse, out _isScreenShotViewReverse))
                {
                    _isScreenShotViewReverse = false;
                    ScannerConfig.Write(ConfigPath.SingleViewIsScreenShotViewReverse, _isScreenShotViewReverse);
                }
            }
        }

        /// <summary>
        /// 初始化图像数据控制器
        /// </summary>
        public void Initialize()
        {
            ImageDataUpdateController.Initialize();
            ScanningDataProvider.DataReady += ScanningDataProviderOnDataReady;
            ScanningDataProvider.BagImageSaved += ScanningDataProviderOnBagImageSaved;
            ScanningDataProvider.StartService();
        }

 
        /// <summary>
        /// 是否开启图像畸变矫正
        /// </summary>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SetIsShapeCorrection(bool value)
        {
            if (ImageDataUpdateController != null)
            {
                if (!IsScanning && !IsPlayingback && !InInterruptRecovering && !IsPulling)
                {
                    ScanningDataProvider.IsShapeCorrection = value;
                }
            }

        }

        /// <summary>
        /// 是否用原始数据填充控件
        /// </summary>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SetIsShowScreenWithOriginXRayData(bool value)
        {
            ImageDataUpdateController.ShowImageWithOriginXRayData(value);
        }

        /// <summary>
        /// 是否显示穿不透区域增强
        /// </summary>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SetIsShowScreenWithUnPenetrationXRayData(bool value)
        {
            if (value)
            {
                if (ImageDataUpdateController != null)
                {
                    var lines = UnPenetrationRegionService.Service.TestboxSpecialPenetration(ImageDataUpdateController.CurrentScreenView1ScanLines, ImageDataUpdateController.CurrentScreenView2ScanLines,
                        ImageDataUpdateController.MinLineNumber, ImageDataUpdateController.MaxLineNumber);

                    ImageDataUpdateController.ClearScreenWithPenetration(lines);
                }
            }
            else
            {
                ImageDataUpdateController.ShowImageWithOriginXRayData(false);
            }
        }

        /// <summary>
        /// 穿不透区域直方图均衡
        /// </summary>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SetImageHistogramEquation(bool value)
        {
            try
            {
                if (!IsSendDataCompleted) return;
                if (value)
                {
                    var bundleTemp = ImageDataUpdateController.GetCurrentScreenData();
                    if (bundleTemp == null || bundleTemp.Count < 1) return;
                    if (_isTwoBadChannelFlags)
                    {
                        if (_banfengAlgo0.HasBadPoints())
                        {
                            _banfengAlgo0.ProcessViewBanFengImgHist(0, bundleTemp);
                        }
                        if (_banfengAlgo1.HasBadPoints())
                        {
                            _banfengAlgo1.ProcessViewBanFengImgHist(1, bundleTemp);
                        }
                    }

                    List<bool> isAirOrNotList = new List<bool>();
                    for (int i = 0; i < bundleTemp.Count; i++)
                    {
                        isAirOrNotList.Add(TestAirLine(bundleTemp[i].View1Data));
                    }

                    List<int> allGap = new List<int>();
                    for (int i = 0; i < isAirOrNotList.Count - 2; i++)
                    {
                        if (!isAirOrNotList[i] && isAirOrNotList[i + 1])
                        {
                            allGap.Add(i + 1);
                        }
                    }
                    if (allGap.Count > 0)
                    {
                        if (bundleTemp.Count - allGap[allGap.Count - 1] > 10)
                        {
                            allGap.Add(bundleTemp.Count - 1);
                        }
                    }
                    else
                    {
                        allGap.Add(bundleTemp.Count - 1);
                    }


                    //List<DisplayScanlineDataBundle> afterProc = new List<DisplayScanlineDataBundle>();
                    for (int i = 0; i < allGap.Count; i++)
                    {
                        List<DisplayScanlineDataBundle> frag = null;
                        if (i == 0)
                        {
                            frag = bundleTemp.Take(allGap[i]).ToList();
                        }
                        else
                        {
                            frag = bundleTemp.Skip(allGap[i - 1]).Take(allGap[i] - allGap[i - 1]).ToList();
                        }

                        var length = frag.Count;
                        if (length > 50)
                        {
                            var view1Lines = frag.Select(l => l.View1Data).ToList();
                            EnhenceImage(view1Lines, length, view1Lines[0].XRayData.Length, _histogramBegin, _histogramEnd, _stretchBegin, _stretchEnd, _isTwoBadChannelFlags);

                            if (bundleTemp[0].View2Data != null)
                            {
                                var view2Lines = frag.Select(l => l.View2Data).ToList();
                                EnhenceImage(view2Lines, length, view2Lines[0].XRayData.Length, _histogramBegin, _histogramEnd, _stretchBegin, _stretchEnd, _isTwoBadChannelFlags);
                            }
                        }
                    }

                    if (!_isTwoBadChannelFlags)
                    {
                        ImageDataUpdateController.PasteScreen(bundleTemp);
                        return;
                    }
                    DisplayScanlineDataBundle displayData = null;
                    List<DisplayScanlineDataBundle> afterShapeList = new List<DisplayScanlineDataBundle>(bundleTemp.Count);

                    foreach (var line in bundleTemp)
                    {
                        var shapedView1 = _shapeCorrection.HistDisplayScanlineCorrection(line.View1Data, DetectViewIndex.View1);
                        var shapedView2 = _shapeCorrection.HistDisplayScanlineCorrection(line.View2Data, DetectViewIndex.View2);
                        displayData = new DisplayScanlineDataBundle(shapedView1, shapedView2);
                        afterShapeList.Add(displayData);
                    }

                    ImageDataUpdateController.PasteOriFuseScreen(afterShapeList);
                }
                else
                {
                    var bundles = ImageDataUpdateController.GetCurrentScreenData();
                    ImageDataUpdateController.PasteScreen(bundles);
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceInfo("error in old SetImageHistogramEquation");
            }
        }

        public void NewSetImageHistogramEquation(bool value)
        {
            if (!IsSendDataCompleted) return;
            if (value)
            {
                var bundleTemp = ImageDataUpdateController.GetCurrentScreenData();
                if (bundleTemp == null || bundleTemp.Count < 1) return;

                List<int> allGap = new List<int>();
                allGap.Add(bundleTemp.Count);

                for (int i = 0; i < allGap.Count; i++)
                {
                    List<DisplayScanlineDataBundle> frag = null;
                    if (i == 0)
                    {
                        frag = bundleTemp.Take(allGap[i]).ToList();
                    }
                    else
                    {
                        frag = bundleTemp.Skip(allGap[i - 1]).Take(allGap[i] - allGap[i - 1]).ToList();
                    }

                    var length = frag.Count;
                    if (length > 50)
                    {
                        Parallel.Invoke(
                            () =>
                            {
                                var view1Lines = frag.Select(l => l.View1Data).ToList();
                                EnhenceImageDivide(view1Lines, length, view1Lines[0].XRayData.Length, _histogramBegin, _histogramEnd, _stretchBegin, _stretchEnd);

                            },
                            () =>
                            {
                                if (bundleTemp[0].View2Data != null)
                                {
                                    var view2Lines = frag.Select(l => l.View2Data).ToList();
                                    EnhenceImageDivide(view2Lines, length, view2Lines[0].XRayData.Length, _histogramBegin, _histogramEnd, _stretchBegin, _stretchEnd);
                                }
                            });

                    }
                }
                ImageDataUpdateController.PasteScreen(bundleTemp);
            }
            else
            {
                var bundles = ImageDataUpdateController.GetCurrentScreenData();
                ImageDataUpdateController.PasteScreen(bundles);
            }
        }




        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool TestAirLine(DisplayScanlineData line)
        {
            var start = 0;
            var end = line.XRayData.Length - 1;
            int block = 32;
            for (int i = start; i <= end - block; i += block)
            {
                long sumLow = 0;
                var count = 0;
                for (int j = 0; j < block; j++)
                {
                    sumLow += line.XRayData[i + j];
                    count++;
                }
                if (count > 0)
                {
                    // 将均值与阈值进行比较，判断是否为空气值
                    if (sumLow / count <= 60000)
                        return false;
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void EnhenceImage(List<DisplayScanlineData> InputDataList, int ImageWidth, int ImageHeight,
            int HistoBegin, int HistoEnd, int StretchBegin, int StretchEnd,bool IsTwoBadChannelFlags)
        {
            List<ushort[]> middatalist = new List<ushort[]>();
            List<ushort[]> midoutputdatalist = new List<ushort[]>();
            foreach (var u in InputDataList)
            {
                middatalist.Add(IsTwoBadChannelFlags ? u.OriginalFused : u.XRayData);
            }
            if (HistoBegin < 0 || StretchBegin < 0 || HistoBegin >= HistoEnd || StretchBegin >= StretchEnd)
                return;

            float[] histogram = new float[65536];
            float[] sum_histogram = new float[65536];
            Array.Clear(histogram, 0, 65536);
            Array.Clear(sum_histogram, 0, 65536);

            int size = 0;
            foreach (var f in InputDataList)
                size += f.Material.Length;
            int imagesize = ImageWidth * ImageHeight;
            int sum = 0;
            int v = 0;

            // 局部灰度区间统计直方图并归一化
            //foreach (ushort[] f in InputDataList)
            foreach (ushort[] f in middatalist)
            {
                foreach (ushort data in f)
                {
                    if (data >= HistoBegin && data <= HistoEnd)
                    {
                        if (data < 0.0f)
                            v = 0;
                        else if (data > 65535.0f)
                            v = 65535;
                        else
                            v = Convert.ToInt32(data);

                        histogram[v]++;
                        sum++;
                    }
                }
            }
            int count = 0;
            for (int i = HistoBegin; i <= 65535; i++)
            {
                histogram[i] /= sum;

                if (i == HistoBegin)
                    sum_histogram[i] = histogram[i];
                else if (i <= HistoEnd)
                    sum_histogram[i] = sum_histogram[i - 1] + histogram[i];
                else
                    sum_histogram[i] = 1.0F;
            }

            //均衡化
            //foreach (ushort[] f in InputDataList)
            foreach (ushort[] f in middatalist)
            {
                ushort[] des = new ushort[f.Length];
                int i = 0;
                foreach (ushort data in f)
                {
                    if (data < 0.0f)
                        v = 0;
                    else if (data > 65535.0f)
                        v = 65535;
                    else
                        v = Convert.ToInt32(data);
                    int tempint = (int)(sum_histogram[v] * 65535);
                    if (tempint > 65535)
                        tempint = 65535;
                    des[i] = (ushort)tempint;
                    i++;
                }
                midoutputdatalist.Add(des);
            }

            //灰度拉伸
            for (int i = 0; i < midoutputdatalist.Count; i++)
            {
                for (int j = 0; j < midoutputdatalist[i].Length; j++)
                {
                    if (midoutputdatalist[i][j] < StretchBegin)
                        midoutputdatalist[i][j] = 0;
                    else if (midoutputdatalist[i][j] > StretchEnd)
                        midoutputdatalist[i][j] = 65535;
                    else
                        midoutputdatalist[i][j] = (ushort)(65535.0f / (StretchEnd - StretchBegin) * midoutputdatalist[i][j] - 65535.0f / (StretchEnd - StretchBegin) * StretchBegin);
                }
            }

            if (_enableFilter)
            {
                SquareMeanFilter(midoutputdatalist);
            }

            if (IsTwoBadChannelFlags)
            {
                for (int i = 0; i < midoutputdatalist.Count; i++)
                {
                    for (int j = 0; j < midoutputdatalist[i].Length; j++)
                    {
                        if (InputDataList[i].OriginalFused[j] < _showThreshold)
                        {
                            InputDataList[i].OriginalFused[j] = midoutputdatalist[i][j];
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < midoutputdatalist.Count; i++)
                {
                    for (int j = 0; j < midoutputdatalist[i].Length; j++)
                    {
                        if (InputDataList[i].XRayData[j] < _showThreshold)
                        {
                            InputDataList[i].XRayDataEnhanced[j] = midoutputdatalist[i][j];
                        }
                    }
                }
            }
        }

        public void EnhenceImageDivide(List<DisplayScanlineData> InputDataList, int ImageWidth, int ImageHeight,
            int HistoBegin, int HistoEnd, int StretchBegin, int StretchEnd)
        {
            //这个函数是EnhenceImage()的升级版本，因为之前EnhenceImage()写在了这个类，我就继续写了
            //后续可以将这部分单独提取一个类仔细处理，比较费精力
            //我尽量尝试将这部分函数提取小函数进行优化，但互相依赖的变量太多，较难处理
            try
            {
                List<ushort[]> middatalist = new List<ushort[]>();
                List<ushort[]> midoutputdatalist = new List<ushort[]>();
                int numOfUnpenetratePixels = 0;//每一线穿不透像素个数统计
                int numOfUnpenetrateLines = 0;//连续含有穿不透区域的线数
                int numOfConsecutivePoints = 5;//连续5个点均为穿不透区域，才算找到了穿不透区域的起始坐标，消除细小穿不透区域的影响

                int startIndex = 0;
                int endIndex = 0;

                bool findstart = false;

                int unHeight = 0;

                List<int> heightList = new List<int>();
                List<int> startList = new List<int>();

                if (HistoBegin < 0 || StretchBegin < 0 || HistoBegin >= HistoEnd || StretchBegin >= StretchEnd)
                    return;

                foreach (DisplayScanlineData inputData in InputDataList)
                {
                    //numOfUnpenetratePixels = CalUnpenNumOfOneLine(inputData, ref startIndex, ref findstart);
                    //寻找一线穿不透区域的起始坐标
                    //因为需要向下多查找numOfConsecutivePoints个点，防止溢出
                    for (int index = 0; index < inputData.XRayData.Length - numOfConsecutivePoints; ++index)
                    {
                        if (checkUnpenStartIndex(inputData, index, numOfConsecutivePoints))
                        {
                            ++numOfUnpenetratePixels;
                            if (!findstart)
                            {
                                findstart = true;
                                startIndex = index;
                                startList.Add(startIndex);//每一线的起始坐标都进行记录，后面从该list寻找真正的起始坐标
                            }
                        }
                    }

                    //找到该线起始坐标后，标志位清空，以便下一线继续寻找
                    findstart = false;

                    //如果这一线穿不透像素很多，则需要记录进middatalist
                    if (numOfUnpenetratePixels > this._unpenetrateHeight)
                    {
                        unHeight = numOfUnpenetratePixels;
                        numOfUnpenetratePixels = 0;
                        middatalist.Add(inputData.XRayData);
                        ++numOfUnpenetrateLines;
                        heightList.Add(unHeight);
                    }

                    //如果这一线穿不透像素变少，则代表寻找到了结束位置，要开始进行直方图处理
                    else
                    {
                        //如果累计的穿不透线数超过阈值，开始进行直方图处理
                        if (numOfUnpenetrateLines > this._unpenetrateWidth)
                        {
                            startIndex = startList[(int)startList.Count / 2];
                            endIndex = startIndex + heightList.Max();
                            if (endIndex > inputData.XRayData.Length)
                                endIndex = inputData.XRayData.Length;
                            numOfUnpenetrateLines = 0;
                            // 处理middatalist

                            if (middatalist.Count > 50)
                            {  
                                //todo：后续增加标志位，不启用该算法，可应用于6550，100等机器
                                if (CalAlpha)
                                {
                                    float[] HistogramAlpha = new float[inputData.XRayData.Length];
                                    HistogramAlpha = CalHistogramAlpha(middatalist, inputData.XRayData.Length, startIndex, endIndex);

                                    foreach (ushort[] origin in middatalist)
                                    {
                                        for (int i = startIndex; i < endIndex; i++)
                                        {
                                            if (HistogramAlpha[i] < 0.95)
                                                HistogramAlpha[i] = 0.95f;
                                            if (HistogramAlpha[i] > 1.05)
                                                HistogramAlpha[i] = 1.05f;
                                            origin[i] = (ushort)(origin[i] / HistogramAlpha[i]);
                                        }
                                    }
                                }

                                float[] regionHistogram = this.StatisticalHistogram(middatalist, HistoBegin, HistoEnd);

                                foreach (ushort[] f in middatalist)
                                {
                                    ushort[] des = new ushort[f.Length];
                                    int index1 = 0;
                                    foreach (ushort data in f)
                                    {
                                        int v = 0;
                                        if (data < 0.0f)
                                            v = 0;
                                        else if (data > 65535.0f)
                                            v = 65535;
                                        else
                                            v = Convert.ToInt32(data);
                                        int tempint = (int)((double)regionHistogram[v] * 65535);
                                        if (tempint > 65535)
                                            tempint = 65535;
                                        des[index1] = (ushort)tempint;
                                        ++index1;
                                    }
                                    midoutputdatalist.Add(des);
                                }
                                middatalist.Clear();
                                heightList.Clear();
                                startList.Clear();
                            }
                        }

                        //之前累计的穿不透线数没有超过阈值，不进行直方图处理
                        else
                        {
                            foreach (ushort[] f in middatalist)
                                midoutputdatalist.Add(f);
                            middatalist.Clear();
                            heightList.Clear();
                            startList.Clear();
                        }

                        //这一线穿不透数据很少，则需要把原始的数据加入midoutputdatalist
                        midoutputdatalist.Add(inputData.XRayData);
                    }

                }

                GrayStretch(ref midoutputdatalist, StretchBegin, StretchEnd);

                CalXRayDataEnhanced(InputDataList, midoutputdatalist);
            }
            catch (Exception exception)
            {
                Tracer.TraceInfo("error in EnhenceImageDivide");
            }
        }

        private bool checkUnpenStartIndex(DisplayScanlineData lineData, int index, int NumberofConsecutivePoints)
        {
            for(int i = 0; i < NumberofConsecutivePoints; i++)
            {
                if (lineData.XRayData[index + i] > this._findendThreshold)
                    return false;
            }
            return true;
        }

        private float[] StatisticalHistogram(List<ushort[]> middatalist,int HistoBegin,int HistoEnd)
        {
            float[] histogram = new float[65536];
            float[] sum_histogram = new float[65536];
            Array.Clear((Array)histogram, 0, 65536);
            Array.Clear((Array)sum_histogram, 0, 65536);
            int sum = 0;

            int v = 0;
            foreach (ushort[] f in middatalist)
            {
                foreach (ushort data in f)
                {
                    if (data >= HistoBegin && data <= HistoEnd)
                    {
                        if (data < 0.0f)
                            v = 0;
                        else if (data > 65535.0f)
                            v = 65535;
                        else
                            v = Convert.ToInt32(data);

                        ++histogram[v];
                        ++sum;
                    }
                }
            }
            for (int index = HistoBegin; index <= (int)ushort.MaxValue; ++index)
            {
                histogram[index] /= (float)sum;
                sum_histogram[index] = index != HistoBegin ? (index > HistoEnd ? 1f : sum_histogram[index - 1] + histogram[index]) : histogram[index];
            }
            return sum_histogram;
        }

        /// <summary>
        /// 均值处理
        /// </summary>
        /// <param name="datas"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void SquareMeanFilter(List<ushort[]> datas)
        {
            if (datas == null) return;
            int height = datas.Count;
            if (height < 3) return;
            int width = datas[0].Length;
            if (width < 3) return;

            var allLength = datas.Select(l => l.Length).Distinct().ToList();
            if (allLength.Count > 1)
            {
                return;
            }

            long sum = 0;
            int halfWindow = _windowSize >> 1;
            lock (datas)
            {
                var copyData = DeepCopy.DeepCopyByBin(datas);
                for (int i = halfWindow; i < height - halfWindow; i++)
                {
                    for (int j = halfWindow; j < width - halfWindow; j++)
                    {
                        sum = 0;
                        for (int hidx = -halfWindow; hidx <= halfWindow; hidx++)
                        {
                            for (int vidx = -halfWindow; vidx <= halfWindow; vidx++)
                            {
                                sum += copyData[i + hidx][j + vidx];
                            }
                        }
                        datas[i][j] = (ushort)(sum / (_windowSize * _windowSize));
                    }
                }
            }
        }

        /// <summary>
        /// 灰度拉伸
        /// </summary>
        /// <param name="midoutputdatalist"></param>
        /// <param name="StretchBegin"></param>
        /// <param name="StretchEnd"></param>
        private void GrayStretch(ref List<ushort[]> midoutputdatalist, int StretchBegin, int StretchEnd)
        {
            for (int i = 0; i < midoutputdatalist.Count; i++)
            {
                for (int j = 0; j < midoutputdatalist[i].Length; j++)
                {
                    if (midoutputdatalist[i][j] < StretchBegin)
                        midoutputdatalist[i][j] = 0;
                    else if (midoutputdatalist[i][j] > StretchEnd)
                        midoutputdatalist[i][j] = 65535;
                    else
                        midoutputdatalist[i][j] = (ushort)(65535.0f / (StretchEnd - StretchBegin) * midoutputdatalist[i][j] - 65535.0f / (StretchEnd - StretchBegin) * StretchBegin);
                }
            }
        }

        private int CalUnpenNumOfOneLine(DisplayScanlineData lineData, ref int startIndex, ref bool findstart)
        {
            int result = 0;
            for (int index = 0; index < lineData.XRayData.Length - 5; ++index)
            {
                //if ((int)inputData.XRayData[index] < this._findendThreshold && (int)inputData.XRayData[index+1] < this._findendThreshold &&
                //    (int)inputData.XRayData[index + 2] < this._findendThreshold&& (int)inputData.XRayData[index + 3] < this._findendThreshold &&
                //    (int)inputData.XRayData[index + 5] < this._findendThreshold)
                if ((int)lineData.XRayData[index] < _findendThreshold)
                {
                    ++result;
                    if (!findstart)
                    {
                        startIndex = index;
                        findstart = true;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 计算每个像素点额外的系数，该系数理论应该在0.95-1.05之间，用以消除横向条纹
        /// </summary>
        /// <param name="middatalist"></param>
        /// <param name="XRayDataLength"></param>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <returns></returns>
        private float[] CalHistogramAlpha(List<ushort[]> middatalist, int XRayDataLength,int startIndex,int endIndex)
        {
            float[] averageArray = new float[XRayDataLength];
            float[] result = new float[XRayDataLength];

            float AVGALL = 0;

            for (int i = 0; i < XRayDataLength; i++)
            {
                float sum = 0;
                for (int j = _calAlphaStart; j < _calAlphaEnd; j++)
                {
                    sum += middatalist[j][i];
                }
                for (int j = middatalist.Count - _calAlphaEnd; j < middatalist.Count - _calAlphaStart; j++)
                {
                    sum += middatalist[j][i];
                }
                averageArray[i] = sum / ((_calAlphaEnd - _calAlphaStart) * 2);
            }

            float sum1 = 0;
            int numForCal = 0;
            for (int i = startIndex; i < endIndex; i++)
            {
                if (averageArray[i] < _findendThreshold)
                {
                    sum1 += averageArray[i];
                    numForCal++;
                }

            }


            AVGALL = sum1 / numForCal;

            result = averageArray.Select(x => x / AVGALL).ToArray();

            return result;
        }

        /// <summary>
        /// 根据直方图修改XRayDataEnhanced
        /// </summary>
        /// <param name="InputDataList"></param>
        /// <param name="midoutputdatalist"></param>
        private void CalXRayDataEnhanced(List<DisplayScanlineData> InputDataList, List<ushort[]> midoutputdatalist)
        {
            for (int i = 0; i < midoutputdatalist.Count; ++i)
            {
                for (int j = 0; j < midoutputdatalist[i].Length; ++j)
                {
                    if ((int)InputDataList[i].XRayData[j] < _showThreshold)
                    {
                        if ((int)midoutputdatalist[i][j] < this._unpenetrateLowerlimit)
                            midoutputdatalist[i][j] = (ushort)0;
                        if ((int)midoutputdatalist[i][j] > this._unpenetrateUpperlimit)
                            midoutputdatalist[i][j] = ushort.MaxValue;
                        //InputDataList[i].XRayDataEnhanced[j] = datas[i][j];
                        InputDataList[i].XRayDataEnhanced[j] = midoutputdatalist[i][j];
                        InputDataList[i].ColorIndex[j] = 0;
                    }
                }
            }
        }

        #region sifenbian
        /// <summary>
        /// 丝分辨加强
        /// </summary>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SetSifenbianPublic(bool value)
        {
            try
            {
                if (!IsSendDataCompleted) return;
                if (value)
                {
                    int err1 = 0;
                    int err2 = 0;
                    var bundleTemp = ImageDataUpdateController.GetCurrentScreenData();
                    if (bundleTemp == null || bundleTemp.Count < 1) return;

                    List<bool> isAirOrNotList = new List<bool>();
                    for (int i = 0; i < bundleTemp.Count; i++)
                    {
                        isAirOrNotList.Add(TestAirLine(bundleTemp[i].View1Data));
                    }

                    List<int> allGap = new List<int>();
                    for (int i = 0; i < isAirOrNotList.Count - 2; i++)
                    {
                        if (!isAirOrNotList[i] && isAirOrNotList[i + 1])
                        {
                            allGap.Add(i + 1);
                        }
                    }
                    if (allGap.Count > 0)
                    {
                        if (bundleTemp.Count - allGap[allGap.Count - 1] > 10)
                        {
                            allGap.Add(bundleTemp.Count - 1);
                        }
                    }
                    else
                    {
                        allGap.Add(bundleTemp.Count - 1);
                    }

                    List<DisplayScanlineData> DisplayScanlineDataview1 = null;
                    List<DisplayScanlineData> DisplayScanlineDataview2 = null;
                    List<DisplayScanlineDataBundle> list = new List<DisplayScanlineDataBundle>();
                    for (int i = 0; i < allGap.Count; i++)
                    {
                        List<DisplayScanlineDataBundle> frag = null;
                        if (i == 0)
                        {
                            frag = bundleTemp.Take(allGap[i]).ToList();
                        }
                        else
                        {
                            frag = bundleTemp.Skip(allGap[i - 1]).Take(allGap[i] - allGap[i - 1]).ToList();
                        }

                        if (i == allGap.Count - 1)
                        {
                            frag.Add(bundleTemp[0]);
                        }

                        var length = frag.Count;
                        if (length > 200)
                        {
                            if ((_sifenbianIndex & 0x01) == 0x01 )
                            {
                                var view1Lines = frag.Select(l => l.View1Data).ToList();
                                err1 = SifenbianEnhance(view1Lines, out DisplayScanlineDataview1);
                                if (err1 == 0)
                                    DisplayScanlineDataview1 = view1Lines;
                            }
                            else
                            {
                                DisplayScanlineDataview1 = frag.Select(l => l.View1Data).ToList();
                            }
                            

                            if (bundleTemp[0].View2Data != null)
                            {
                                if ((_sifenbianIndex & 0x02) == 0x02)
                                {
                                    var view2Lines = frag.Select(l => l.View2Data).ToList();
                                    err2 = SifenbianEnhance(view2Lines, out DisplayScanlineDataview2);
                                    if (err2 == 0)
                                        DisplayScanlineDataview2 = view2Lines;
                                }
                                else
                                {
                                    DisplayScanlineDataview2 = frag.Select(l => l.View2Data).ToList();
                                }
                            }
                        }

                        else
                        {
                            DisplayScanlineDataview1 = frag.Select(l => l.View1Data).ToList();

                            if (bundleTemp[0].View2Data != null)
                            {
                                DisplayScanlineDataview2 = frag.Select(l => l.View2Data).ToList();
                            }

                        }

                        for (int k = 0; k < frag.Count; k++)
                        {
                            if (DisplayScanlineDataview2 != null)
                            {
                                var bundle = new DisplayScanlineDataBundle(DisplayScanlineDataview1[k], DisplayScanlineDataview2[k]);
                                list.Add(bundle);
                            }
                            else
                            {
                                var bundle = new DisplayScanlineDataBundle(DisplayScanlineDataview1[k], null);
                                list.Add(bundle);
                            }
                        }
                    }
                    ImageDataUpdateController.PasteScreen(list);
                }
                else
                {
                    var bundles = ImageDataUpdateController.GetCurrentScreenData();
                    ImageDataUpdateController.PasteScreen(bundles);
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceInfo("error in old SetImageHistogramEquation");
            }
        }


        [MethodImpl(MethodImplOptions.Synchronized)]
        private int SifenbianEnhance(List<DisplayScanlineData> inputdata, out List<DisplayScanlineData> outputdata)
        {
            var dataview = new List<ushort[]>();

            outputdata = new List<DisplayScanlineData>();

            int _rows = inputdata.Count;
            if (_rows < 1)
                return 0;
            if (inputdata[0] == null)
            {
                return 0;
            }
            int _cols = inputdata[0].XRayData.Length;
            if (_cols < 1)
                return 0;

            //强度
            Matrix<ushort> RawData = new Matrix<ushort>(_rows, _cols);
            Matrix<ushort> DstData = new Matrix<ushort>(_rows, _cols);
            for (int i = 0; i < _rows; i++)
            {
                Buffer.BlockCopy(inputdata[i].XRayData, 0, RawData.Data, i * _cols * sizeof(ushort), _cols * sizeof(ushort));
            }

            int err = Sifenbian(RawData.Mat.DataPointer, DstData.Mat.DataPointer, RawData.Mat.Height, RawData.Mat.Width, RawData.Mat.Step);
            if (err == 0)
                return 0;

            for (int i = 0; i < _rows; i++)
            {
                ushort[] tempdst1 = new ushort[_cols];
                Buffer.BlockCopy(DstData.Data, i * _cols * sizeof(ushort), tempdst1, 0, _cols * sizeof(ushort));
                dataview.Add(tempdst1);
            }

            for (int i = 0; i < _rows; i++)
            {
                ClassifiedLineData cld = new ClassifiedLineData(inputdata[i].ViewIndex, inputdata[i].XRayData, dataview[i], inputdata[i].Material);
                DisplayScanlineData ds = new DisplayScanlineData(cld, inputdata[i].ColorIndex, inputdata[i].LineNumber);
                outputdata.Add(ds);
            }
            return 1;
        }
        #endregion


        /// <summary>
        /// 事件响应：一个物体的扫描图像存储完毕
        /// </summary>
        private void ScanningDataProviderOnBagImageSaved()
        {
            if (_TipInjectionFlow != null)
            {
                // 图像存储完毕后，尝试加载下一个tip图像
                _TipInjectionFlow.TryLoadNextTipImageAsync();
            }

            //ImageProcessAlgoRecommendService.Service().HandleObjectSeperated();
        }

        /// <summary>
        /// 当配置发生变化时，启动或关闭培训模式
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void UpdateTrainingMode()
        {
            bool isTraining = false;
            if (ScannerConfig.Read(ConfigPath.TrainingIsEnabled, out isTraining) && isTraining != IsTrainingMode)
            {
                // 培训开关已经发生变化：打开或关闭,先清空屏幕
                ImageDataUpdateController.ClearAndAppend(null);

                IsTrainingMode = isTraining;

                if (IsTrainingMode)
                {
                    TrainingDataProvider = new TrainingImageDataProvider();
                    TrainingDataProvider.DataReady += TrainingDataProviderOnDataReady;
                    TrainingDataProvider.NewImageIsReady += TrainingDataProviderOnNewImageIsReady;
                }
                else
                {
                    if (TrainingDataProvider != null)
                    {
                        TrainingDataProvider.DataReady -= TrainingDataProviderOnDataReady;
                        TrainingDataProvider.NewImageIsReady -= TrainingDataProviderOnNewImageIsReady;
                        TrainingDataProvider.CleanTimer();
                        TrainingDataProvider = null;
                    }
                }
            }
        }

        /// <summary>
        /// 回放指定的图像，并进入回放模式
        /// </summary>
        /// <param name="records"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void BeginPlayback(IEnumerable<ImageRecord> records)
        {
            if (records != null)
            {
                if (PlaybackDataProvider != null)
                {
                    PlaybackDataProvider.DrawRectAction -= PlaybackDataProvider_DrawRectAction;
                    PlaybackDataProvider = null;
                }
                PlaybackDataProvider = new PlaybackImageDataProvider(ScreenMaxLinesCount, records);
                PlaybackDataProvider.DrawRectAction += PlaybackDataProvider_DrawRectAction;

                if (PaintingRegionsService.Service.RollingImageProcessController.MarkerList != null)
                    PaintingRegionsService.Service.RollingImageProcessController.MarkerList.Clear();
                _markerRegions.Clear();
                var firstScreen = PlaybackDataProvider.GetFirstScreen().ToList();
                if (firstScreen.Any())
                {
                    ImageDataUpdateController.ClearAndReverseAppend(firstScreen);

                    IsPlayingback = true;
                    AddDrawRegionsToDisplayController();
                }
            }
        }

        void PlaybackDataProvider_DrawRectAction(KeyValuePair<DetectViewIndex, List<MarkerRegion>> regions)
        {
            this._markerRegions.Add(regions);
        }

        void AddDrawRegionsToDisplayController()
        {
            if (!default(KeyValuePair<DetectViewIndex, List<MarkerRegion>>).Equals(_markerRegions))
            {
                foreach (var item in _markerRegions)
                {
                    foreach (var box in item.Value)
                    {
                        ImageDataUpdateController.AddRegionMark(box, item.Key);
                    }
                }
            }
        }

        /// <summary>
        /// 回放指定的图像，并进入回放模式
        /// </summary>
        /// <param name="records"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void BeginPlayback(IEnumerable<string> records)
        {
            if (records != null)
            {
                if (PlaybackDataProvider != null)
                {
                    PlaybackDataProvider.DrawRectAction -= PlaybackDataProvider_DrawRectAction;
                    PlaybackDataProvider = null;
                }
                PlaybackDataProvider = new PlaybackImageDataProvider(ScreenMaxLinesCount, records);
                PlaybackDataProvider.DrawRectAction += PlaybackDataProvider_DrawRectAction;

                if (PaintingRegionsService.Service.RollingImageProcessController.MarkerList != null)
                    PaintingRegionsService.Service.RollingImageProcessController.MarkerList.Clear();
                PaintingRegionsService.Service.ClearXRayFileInfo();
                _markerRegions.Clear();

                var firstScreen = PlaybackDataProvider.GetFirstScreen().ToList();
                if (firstScreen.Any())
                {
                    ImageDataUpdateController.ClearAndReverseAppend(firstScreen);
                    //ImageDataUpdateController.AppendLines(firstScreen);
                    IsPlayingback = true;
                    AddDrawRegionsToDisplayController();
                }
            }
        }

        /// <summary>
        /// 结束图像回放模式
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void EndPlayback()
        {
            IsPlayingback = false;
            ImageDataUpdateController.ChangeMovingSpeed(false);
            if (IsTrainingMode)
            {
                // 培训模式下，结束回放时，清空屏幕
                ImageDataUpdateController.ClearAndAppend(null);
            }
            else
            {
                // 培训模式下，结束回放时，清空屏幕
                if (ShowImageBasedOnMotorDirection)
                {
                    ImageDataUpdateController.ClearAndAppend(null);
                }
                else
                {
                    // 使用最新扫描的数据清屏。如果没有进行过扫描，则清空为白色
                    var lastImage = ScanningDataProvider.PullToLastScreen();
                    ImageDataUpdateController.ClearAndAppend(lastImage);
                }
            }
            PlaybackDataProvider.DrawRectAction -= PlaybackDataProvider_DrawRectAction;
            PlaybackDataProvider = null;
        }

        /// <summary>
        /// 扫描数据准备完毕，填充显示
        /// </summary>
        /// <param name="bundle"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void ScanningDataProviderOnDataReady(DisplayScanlineDataBundle bundle)
        {
            if (IsPlayingback)
            {
                EndPlayback();
            }

            // 在培训模式下，不显示实时采集的图像数据
            if (!IsTrainingMode)
            {
                if (IsPulling)
                {
                    ImageDataUpdateController.ChangeMovingSpeed(false);
                    // 如果当前正在回拉，则立即结束回拉
                    if (ShowImageBasedOnMotorDirection)
                    {
                        ImageDataUpdateController.ClearAndAppend(null);
                    }
                    else
                    {
                        // 使用最新扫描的数据清屏。如果没有进行过扫描，则清空为白色
                        var lastImage = ScanningDataProvider.PullToLastScreen();
                        ImageDataUpdateController.ClearAndAppend(lastImage);
                    }
                    PullingMode = ImagePullingMode.None;
                }

                //为ViewData添加时间戳
                if (bundle.View1Data != null) bundle.View1Data.CreatedTime = DateTime.Now;
                if (bundle.View2Data != null) bundle.View2Data.CreatedTime = DateTime.Now;
                CurrentScreenRawScanLines.AddLast(bundle);
                if (CurrentScreenRawScanLines.Count > ImageDataUpdateController.CurrentScreenLinesThreashold)
                    CurrentScreenRawScanLines.RemoveFirst();
                DisplayScanlineDataBundle bundle_show = DeepCopy.DeepCopyByBin(bundle);
                if (_TipInjectionFlow != null)
                {
                    _TipInjectionFlow.TryInsertToScanLine(bundle_show, ImageDataUpdateController.MinLineNumber);
                }
                if (ImageDataUpdateController.IsShowLinesDataCompleted)
                {
                    ScanningDataProvider_NewBagComeAction();
                }
                ImageDataUpdateController.AppendLines(new List<DisplayScanlineDataBundle>(1) { bundle_show });

                //智能图像推荐算法缓存数据
                //ImageProcessAlgoRecommendService.Service().CacheData(bundle);
            }
        }

        /// <summary>
        /// 培训数据准备完毕，填充显示
        /// </summary>
        /// <param name="bundle"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void TrainingDataProviderOnDataReady(DisplayScanlineDataBundle bundle)
        {
            if (IsTrainingMode && !IsPlayingback)
            {
                CurrentScreenRawScanLines.AddLast(bundle);
                if (CurrentScreenRawScanLines.Count > ImageDataUpdateController.CurrentScreenLinesThreashold)
                    CurrentScreenRawScanLines.RemoveFirst();
                DisplayScanlineDataBundle bundle_show = DeepCopy.DeepCopyByBin(bundle);
                if (_TipInjectionFlow != null)
                {
                    _TipInjectionFlow.TryInsertToScanLine(bundle_show, ImageDataUpdateController.MinLineNumber);
                }
                ImageDataUpdateController.AppendLines(new List<DisplayScanlineDataBundle>(1) { bundle_show });

                //智能图像推荐算法缓存数据
                //ImageProcessAlgoRecommendService.Service().CacheData(bundle);
            }
        }

        /// <summary>
        /// 处理电机停止键
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void OnConveyorStopRequest()
        {

            if (IsPlayingback)
            {
                EndPlayback();
            }

            if (_TipInjectionFlow != null)
            {
                _TipInjectionFlow.OnConveyorStopped();
            }

            if (IsTrainingMode)
            {
                TrainingDataProvider.OnConveyorStop();
                IsTraining = false;
            }
            else
            {
                if (IsPulling)
                {
                    ImageDataUpdateController.ChangeMovingSpeed(false);
                    // 回拉至最后一屏数据
                    var lines = ScanningDataProvider.PullToLastScreen();
                    ImageDataUpdateController.ClearAndAppend(lines);
                    //ImageDataUpdateController.ClearAndAppend(null);
                    ScanningDataProvider.ClearPullBackCache();
                    PullingMode = ImagePullingMode.None;
                }

                IsScanning = false;
                ControlService.ServicePart.DriveConveyor(ConveyorDirection.Stop);
                ControlService.ServicePart.RadiateXRay(false);
                ScanningDataProvider.OnConveyorStopped();
            }
        }

        /// <summary>
        /// 处理电机前进键消息
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void OnConveyorRightKeyDown()
        {
            if (IsPlayingback)
            {
                EndPlayback();
            }

            if (IsTrainingMode)
            {
                ImageDataUpdateController.ChangeMovingSpeed(false);
                if (_curMotorDirection != MotorDirection.MoveRight && ShowImageBasedOnMotorDirection && _bidirectionalScan)
                {
                    TrainingDataProvider.ClearCachedlines();
                }
                _curMotorDirection = MotorDirection.MoveRight;
                TrainingDataProvider.OnConveyorStart();
                IsTraining = true;
            }
            else
            {
                // 如果正在回拉图像，则先回拉至最后一屏数据，结束回拉，然后再启动电机，继续扫描
                if (IsPulling)
                {
                    ImageDataUpdateController.ChangeMovingSpeed(false);
                    var lines = ScanningDataProvider.PullToLastScreen();
                    ImageDataUpdateController.ClearAndAppend(lines);
                    ScanningDataProvider.ClearPullBackCache();
                    PullingMode = ImagePullingMode.None;
                }

                if (ScanningDataProvider.InTestDataMode || ConveyorController.Controller.MoveRight())
                {
                    IsScanning = true;
                    _curMotorDirection = MotorDirection.MoveRight;
                    ScanningDataProvider.OnConveyorStarted(true);
                }
            }

            if (_lastMotorDirection == MotorDirection.Stop)
            {
                _lastMotorDirection = _curMotorDirection;
            }

            ImageMoveDirectionChangedBasedOnMotorDirection();

            _lastMotorDirection = _curMotorDirection;
        }

        /// <summary>
        /// 响应用户按下标记键
        /// </summary>
        /// <returns>如果当前处于tip注入流程，则认定标记成功，返回true；如果当前没有tip注入，则认定标定失败，返回false</returns>
        public bool OnMarkKeyDown(List<MarkerRegionEx> markerList)
        {
            if (_TipInjectionFlow != null)
            {
                LoginAccountManager.Service.AddMarkCount();

                return _TipInjectionFlow.MarkIdentifyTip(markerList);
            }

            return false;
        }

        /// <summary>
        /// 处理电机后退键消息
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void OnConveyorLeftKeyDown()
        {
            if (IsPlayingback)
            {
                EndPlayback();
            }

            if (IsTrainingMode)
            {
                ImageDataUpdateController.ChangeMovingSpeed(false);
                if (_curMotorDirection != MotorDirection.MoveRight && ShowImageBasedOnMotorDirection && _bidirectionalScan)
                {
                    TrainingDataProvider.ClearCachedlines();
                }
                _curMotorDirection = MotorDirection.MoveLeft;
                TrainingDataProvider.OnConveyorStart();
                IsTraining = true;
            }
            else
            {
                // 如果正在回拉图像，则先回拉至最后一屏数据，然后再启动电机，继续扫描
                if (IsPulling)
                {
                    ImageDataUpdateController.ChangeMovingSpeed(false);
                    var lines = ScanningDataProvider.PullToLastScreen();
                    ImageDataUpdateController.ClearAndAppend(lines);
                    ScanningDataProvider.ClearPullBackCache();
                    PullingMode = ImagePullingMode.None;
                }

                // 这里的MoveLeft()会直接驱动电机
                if (ConveyorController.Controller.MoveLeft())
                {
                    IsScanning = true;
                    _curMotorDirection = MotorDirection.MoveLeft;
                    ScanningDataProvider.OnConveyorStarted(false);
                }
            }
            if (_lastMotorDirection == MotorDirection.Stop)
            {
                _lastMotorDirection = _curMotorDirection;
            }
            ImageMoveDirectionChangedBasedOnMotorDirection();

            _lastMotorDirection = _curMotorDirection;
        }

        private void ImageMoveDirectionChangedBasedOnMotorDirection()
        {
            //if (ShowImageBasedOnMotorDirection && _bidirectionalScan)
            //{
            //    //方向变化后图像的显示方向也要发生变化，但是首先要刷新屏幕成白色,更改图像显示方向
            //    if (_curMotorDirection != _lastMotorDirection)
            //    {
            //        ImageDataUpdateController.ClearAndAppend(null);
            //        ImageDataUpdateController.RightToLeft = !ImageDataUpdateController.RightToLeft;
            //    }
            //}
        }

        /// <summary>
        /// 后拉图像
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void PullBackwardImage()
        {
            var time1 = DateTime.Now;
            Tracer.TraceDebug($"[PullBackTimeoutTracked] DisplayImageDataController.PullBackwardImage()");
            // 在扫描期间，不允许拉动图像
            if (IsScanning)
            {
                return;
            }
            if (!ImageDataUpdateController.IsShowLinesDataCompleted)
            {
                return;
            }

            IEnumerable<DisplayScanlineDataBundle> linesUpdate = null;

            if (IsPlayingback)
            {
                // 如果当前正处于前拉中，并且有尚未显示完毕的前拉数据，则返回
                if (PullingMode == ImagePullingMode.PullingForward && ImageDataUpdateController.HasUnshownLines())
                {
                    return;
                }
                ImageDataUpdateController.ChangeMovingSpeed(true);
                PullingMode = ImagePullingMode.PullingBack;
                linesUpdate = PlaybackDataProvider.PullBack();
            }
            else
            {
                time1 = DateTime.Now;
                ImageDataUpdateController.ChangeMovingSpeed(true);
                Tracer.TraceDebug($"[PullBackTimeoutTracked] Invoke ImageDataUpdateController.ChangeMovingSpeed execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
                if (IsTrainingMode)
                {

                }
                else
                {
                    // 如果当前不是回拉模式，则放弃尚未显示完毕的显示缓存，并同步更新数据缓存
                    if (PullingMode != ImagePullingMode.PullingBack)
                    {
                        time1 = DateTime.Now;
                        ImageDataUpdateController.DropUnshownLines();
                        Tracer.TraceDebug($"[PullBackTimeoutTracked] Invoke ImageDataUpdateController.DropUnshownLines execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
                        if (ImageDataUpdateController.HasLine)
                        {
                            time1 = DateTime.Now;
                            ScanningDataProvider.UpdateShowingRange(ImageDataUpdateController.MinLineNumber, ImageDataUpdateController.MaxLineNumber);
                            Tracer.TraceDebug($"[PullBackTimeoutTracked] Invoke ScanningDataProvider.UpdateShowingRange execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
                        }

                        PullingMode = ImagePullingMode.PullingBack;
                    }
                    time1 = DateTime.Now;
                    linesUpdate = ScanningDataProvider.PullBack();
                    Tracer.TraceDebug($"[PullBackTimeoutTracked] Invoke ScanningDataProvider.PullBack execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
                    time1 = DateTime.Now;
                    AddDrawRegionsToDisplayController();
                    Tracer.TraceDebug($"[PullBackTimeoutTracked] Invoke AddDrawRegionsToDisplayController execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
                }
            }

            if (linesUpdate != null)
            {
                ImageDataUpdateController.ReverseAppendLines(linesUpdate);
            }
        }

        /// <summary>
        /// 前拉图像
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void PullForwardImage()
        {
            var time1 = DateTime.Now;
            Tracer.TraceDebug($"[PullBackTimeoutTracked] DisplayImageDataController.PullForwardImage()");
            // 在扫描期间，不允许拉动图像
            if (IsScanning)
            {
                return;
            }
            if (!ImageDataUpdateController.IsShowLinesDataCompleted)
            {
                return;
            }

            IEnumerable<DisplayScanlineDataBundle> linesUpdate = null;

            if (IsPlayingback)
            {
                // 如果当前正在回拉，且有尚未显示完毕的回拉数据，则不允许前拉，否则会导致数据显示错乱
                if (PullingMode == ImagePullingMode.PullingBack && ImageDataUpdateController.HasUnshownLines())
                {
                    return;
                }

                ImageDataUpdateController.ChangeMovingSpeed(true);
                PullingMode = ImagePullingMode.PullingForward;
                linesUpdate = PlaybackDataProvider.PullForward();
            }
            else
            {
                time1 = DateTime.Now;
                ImageDataUpdateController.ChangeMovingSpeed(true);
                Tracer.TraceDebug($"[PullBackTimeoutTracked] Invoke ImageDataUpdateController.ChangeMovingSpeed execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
                if (IsTrainingMode)
                {

                }
                else
                {
                    if (PullingMode != ImagePullingMode.PullingForward)
                    {
                        time1 = DateTime.Now;
                        ImageDataUpdateController.DropUnshownLines();
                        Tracer.TraceDebug($"[PullBackTimeoutTracked] Invoke ImageDataUpdateController.DropUnshownLines execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
                        if (ImageDataUpdateController.HasLine)
                        {
                            time1 = DateTime.Now;
                            ScanningDataProvider.UpdateShowingRange(ImageDataUpdateController.MinLineNumber, ImageDataUpdateController.MaxLineNumber);
                            Tracer.TraceDebug($"[PullBackTimeoutTracked] Invoke ScanningDataProvider.UpdateShowingRange execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
                        }

                        PullingMode = ImagePullingMode.PullingForward;
                    }

                    time1 = DateTime.Now;
                    linesUpdate = ScanningDataProvider.PullForward();
                    Tracer.TraceDebug($"[PullBackTimeoutTracked] Invoke ScanningDataProvider.PullForward execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
                    time1 = DateTime.Now;
                    AddDrawRegionsToDisplayController();
                    Tracer.TraceDebug($"[PullBackTimeoutTracked] Invoke AddDrawRegionsToDisplayController execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
                }
            }
            if (linesUpdate != null && linesUpdate.Any())
            {
                ImageDataUpdateController.AppendLines(linesUpdate);
            }
        }

        /// <summary>
        /// 向左拉动图像：根据图像移动方向，决定前拉或后来
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void PullLeftImage()
        {
            if (ImageDataUpdateController.RightToLeft)
            {
                PullBackwardImage();
            }
            else
            {
                PullForwardImage();
            }
        }

        /// <summary>
        /// 向左拉动图像结束：用户弹起左拉按键
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void PullLeftImageEnd()
        {
            OnPullingImageEnd();
        }

        /// <summary>
        /// 处理图像回拉请求
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void PullRightImage()
        {
            if (ImageDataUpdateController.RightToLeft)
            {
                PullForwardImage();
            }
            else
            {
                PullBackwardImage();
            }
        }

        /// <summary>
        /// 向右拉动图像结束，用户弹起右拉按键
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void PullRightImageEnd()
        {
            OnPullingImageEnd();
        }

        /// <summary>
        /// 图像拉动结束：用户弹起图像拉动按键
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void OnPullingImageEnd()
        {
            if (IsPlayingback)
            {
                // 图像回放的实现机制中，不支持立即停止回拉
                ImageDataUpdateController.DropUnshownLines();

                if (ImageDataUpdateController.HasLine)
                {
                    PlaybackDataProvider.UpdateShowingRange(ImageDataUpdateController.MinLineNumber, ImageDataUpdateController.MaxLineNumber);
                }
            }
            else
            {
                if (!IsTrainingMode)
                {
                    ImageDataUpdateController.DropUnshownLines();

                    if (ImageDataUpdateController.HasLine)
                    {
                        ScanningDataProvider.UpdateShowingRange(ImageDataUpdateController.MinLineNumber, ImageDataUpdateController.MaxLineNumber);
                    }
                }
            }
        }

        /// <summary>
        /// 程序结束时，清理资源退出
        /// </summary>
        public void Cleanup()
        {
            ScannerConfig.ConfigChanged -= ScannerConfigOnConfigChanged;
            if (_TipInjectionFlow != null)
            {
                _TipInjectionFlow.Cleanup();
                _TipInjectionFlow.TipImageInjected -= TipInjectionFlowOnTipImageInjected;
                _TipInjectionFlow.TipMissed -= TipInjectionFlowOnTipMissed;
                _TipInjectionFlow.TipIdentified -= TipInjectionFlowOnTipIdentified;
            }

            if (ScanningDataProvider != null)
            {
                ScanningDataProvider.StopService();
                ScanningDataProvider.DataReady -= ScanningDataProviderOnDataReady;
                ScanningDataProvider.DrawRectAction -= PlaybackDataProvider_DrawRectAction;
                ScanningDataProvider.BagImageSaved -= ScanningDataProviderOnBagImageSaved;
            }

            if (TrainingDataProvider != null)
            {
                TrainingDataProvider.DataReady -= TrainingDataProviderOnDataReady;
                TrainingDataProvider.NewImageIsReady -= TrainingDataProviderOnNewImageIsReady;
            }

            HttpNetworkController.Controller.ConveyorDirectionAction -= Controller_ConveyorDirectionAction;
            HttpNetworkController.Controller.TipInjectUpdateAction -= Controller_TipInjectUpdateAction;
            HttpNetworkController.Controller.RemoteTipPlanUpdateAction -= UpdateNetTipPlan;
        }







        public void ClearTipSelected()
        {
            CurrentScreenRawScanLines = new LinkedList<DisplayScanlineDataBundle>();
            if (_TipInjectionFlow != null)
                _TipInjectionFlow.ClearTipSelected();
        }

        /// <summary>
        /// 图像拉动模式
        /// </summary>
        public enum ImagePullingMode
        {
            /// <summary>
            /// 当前未进行图像拉动
            /// </summary>
            None,

            /// <summary>
            /// 正在回拉图像
            /// </summary>
            PullingBack,

            /// <summary>
            /// 正在前拉图像
            /// </summary>
            PullingForward
        }
    }
}
