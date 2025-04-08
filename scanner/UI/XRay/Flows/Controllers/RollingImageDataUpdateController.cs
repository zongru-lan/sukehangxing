using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Common.Utilities;
using UI.XRay.Flows.Services;
using UI.XRay.ImagePlant.Gpu;
using UI.XRay.RenderEngine;
using UI.XRay.Business.Algo;
using UI.XRay.Flows.Services.DataProcess;
using System.Windows;
using UI.XRay.Flows.TRSNetwork;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace UI.XRay.Flows.Controllers
{
    /// <summary>
    /// 卷轴图像数据控制器
    /// </summary>
    public class RollingImageDataUpdateController
    {
        private int[] movingSpeedArray;

        /// <summary>
        /// 事件：发现违禁品。
        /// </summary>
        public event Action<KeyValuePair<DetectViewIndex, MarkerRegion>> ContrabandDetected;

        /// <summary>
        /// 滚动图像处理控制
        /// </summary>
        private IRollingImageProcessController ImageProcessController { get; set; }

        /// <summary>
        /// 默认的图像显示速率：每16.6ms刷新一次图像时，将会更新的图像的列数
        /// </summary>
        private int _imageDefaultMovingSpeed = 3;

        double _screenRefreshRate = 60;

        /// <summary>
        /// 当前图像刷新的速率，即每次刷新多少列数据，与刷新率、采样率有关
        /// </summary>
        private int ImageMovingSpeed { get; set; }

        private int CurrentMovingSpeed;

        public bool RightToLeft
        {
            get { return ImageProcessController.RightToLeft; }
            set { ImageProcessController.RightToLeft = value; }
        }

        /// <summary>
        /// true表示当前处于向后移动图像的模式，即回拉图像，需要从图像头部更新数据
        /// false表示当前处于向前移动图像的模式，将会把数据填充图像尾部
        /// </summary>
        private bool ReverseAppending { get; set; }

        /// <summary>
        /// 配置文件中记录的视角1的探测通道数
        /// </summary>
        private int View1ChannelsCount { get; set; }

        /// <summary>
        /// 配置文件中记录的视角2的探测通道数
        /// </summary>
        private int View2ChannelsCount { get; set; }

        /// <summary>
        /// 当前绘制的扫描线的最小编号
        /// </summary>
        public int MinLineNumber { get; private set; }

        /// <summary>
        /// 当前绘制的扫描线的最大编号
        /// </summary>
        public int MaxLineNumber { get; private set; }

        /// <summary>
        /// 单个图像可显示的最多的图像线数
        /// </summary>
        private int MaxLinesCount { get; set; }

        /// <summary>
        /// 当前正在显示的图像列的实际数量
        /// </summary>
        public int ShowingLinesCount { get; private set; }

        /// <summary>
        /// 当前是否有扫描线在显示。初始化为false，当填充显示时变为true；当清空图像后，变为false
        /// </summary>
        public bool HasLine
        {
            get { return ShowingLinesCount > 0; }
        }

        /// <summary>
        /// 待显示的视角1的图像数据
        /// </summary>
        private readonly ConcurrentQueue<DisplayScanlineData> _view1ImageCache =
            new ConcurrentQueue<DisplayScanlineData>();

        /// <summary>
        /// 待显示的视角2的图像数据
        /// </summary>
        private readonly ConcurrentQueue<DisplayScanlineData> _view2ImageCache =
            new ConcurrentQueue<DisplayScanlineData>();

        /// <summary>
        /// 视角1数据自动探测的结果
        /// </summary>
        private List<MarkerRegion> _view1IntellisenseResults = new List<MarkerRegion>(10);

        /// <summary>
        /// 视角2数据自动探测的结果
        /// </summary>
        private List<MarkerRegion> _view2IntellisenseResults = new List<MarkerRegion>(10);

        /// <summary>
        /// 子图像1显示的视角编号
        /// </summary>
        private DetectViewIndex Image1ViewIndex;

        /// <summary>
        /// 子图像2显示的视角编号
        /// </summary>
        private DetectViewIndex Image2ViewIndex;

        /// <summary>
        /// 视角1的智能识别
        /// </summary>
        private IntelliSenseService _view1IntelliSense;

        /// <summary>
        /// 视角2的智能识别
        /// </summary>
        private IntelliSenseService _view2IntelliSense;

        /// <summary>
        /// 毒品爆炸物区域边框颜色
        /// </summary>
        private System.Drawing.Color _deiBorderColor;

        private System.Drawing.Color _eiBorderColor;
        
        /// <summary>
        /// 高密度区域边框颜色
        /// </summary>
        private System.Drawing.Color _hdiBorderColor;

        /// <summary>
        /// Tip边框颜色
        /// </summary>
        private System.Drawing.Color _tipBorderColor = System.Drawing.Color.Red;

        /// <summary>
        /// 线积分时间，单位为毫米
        /// </summary>
        private double LineIntegrationTime { get; set; }
        /// <summary>
        /// 是否显示智能识别的标记框
        /// </summary>
        public bool ShowIntelliSenseMarkers { get; set; }

        /// <summary>
        /// 是否显示危险品标示框标签
        /// </summary>
        public bool ShowLabel { get; set; }

        public LinkedList<DisplayScanlineData> CurrentScreenView1ScanLines = new LinkedList<DisplayScanlineData>();
        public LinkedList<DisplayScanlineData> CurrentScreenView2ScanLines = new LinkedList<DisplayScanlineData>();

        private float _horizonalScale = 1.0f;

        private bool _image1AddDataAtEnd = true;
        private bool _image2AddDataAtEnd = true;

        private int _superEnhanceThreshold;

        private int channelsCount;
        private bool addDataAtEnd;

        private bool _isPull = false;

        private float _pullPlaySpeedRatio = 2.0f;

        public List<DisplayScanlineDataBundle> GetCurrentScreenData()
        {
            var bundles = new List<DisplayScanlineDataBundle>();
            int num = 0;
            
            if (CurrentScreenView1ScanLines.Count > 0)
            {
                if (CurrentScreenView2ScanLines.Count > 0)
                {                   
                    List<DisplayScanlineData> view1 = new List<DisplayScanlineData>();
                    List<DisplayScanlineData> view2 = new List<DisplayScanlineData>();
                    lock (CurrentScreenView1ScanLines)
                    {
                        view1 =DeepCopy.DeepCopyByBin( CurrentScreenView1ScanLines.ToList());
                    }
                    lock (CurrentScreenView2ScanLines)
                    {
                        view2 = DeepCopy.DeepCopyByBin(CurrentScreenView2ScanLines.ToList());
                    }

                    num = Math.Min(view1.Count, view2.Count);

                    for (int i = 0; i < num; i++)
                    {
                        if (i < view1.Count && i < view2.Count)  
                        {
                            var bundle = new DisplayScanlineDataBundle(DeepCopy.DeepCopyByBin(view1[i]), DeepCopy.DeepCopyByBin(view2[i]));
                            bundles.Add(bundle);
                        }
                        else
                        {
                            Tracer.TraceInfo("GetCurrentScreenData error index:" + i + ",v1 count:" + view1.Count + ",v2 count:" + view2.Count);
                        }
                    }
                    return bundles;
                }
                else
                {
                    List<DisplayScanlineData> view1 = new List<DisplayScanlineData>(); ;
                    lock (CurrentScreenView1ScanLines)
                    {
                        view1 =DeepCopy.DeepCopyByBin( CurrentScreenView1ScanLines.ToList());
                    }
                    num = view1.Count;
                    for (int i = 0; i < num; i++)
                    {                   
                       if (i < view1.Count)  //yxc
                        { 
                            var bundle = new DisplayScanlineDataBundle(DeepCopy.DeepCopyByBin(view1[i]), null);
                            bundles.Add(bundle);
                        }
                       else
                        {
                            Tracer.TraceInfo("GetCurrentScreenData error index:" + i + ",count:" + view1.Count);
                        }
                    }
                    return bundles;
                }
            }
            else
            {
                
                return bundles;
            }
        }

        /// <summary>
        /// 原有静态类无法并行处理
        /// 在此处新加同样方法，为并行处理提升穿透算法处理速度
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public T DeepCopyByBinForParallel<T>(T obj)
        {
            object retval;
            using (MemoryStream ms = new MemoryStream())
            {

                BinaryFormatter bf = new BinaryFormatter();
                //序列化成流

                bf.Serialize(ms, obj);

                ms.Seek(0, SeekOrigin.Begin);
                //反序列化成对象

                retval = bf.Deserialize(ms);

                ms.Close();


            }

            return (T)retval;
        }


        #region 缓存
        private bool _allowShow = false;
        private int _cacheLength = 150;

        private int _showLinesCompletedInterval;
        private int _appendLinesCompletedInterval;

        private TimeSpan _showLinesCompletedTimespan;
        private TimeSpan _appendLinesCompletedTimespan;

        private DateTime _showLinesDateTime = DateTime.Now;

        public bool IsShowLinesDataCompleted
        {
            get { return DateTime.Now - _showLinesDateTime > _showLinesCompletedTimespan; }
        }

        private DateTime _appendLinesDateTime = DateTime.Now;
        public bool IsAppendLinesDataCompleted
        {
            get { return DateTime.Now - _appendLinesDateTime > _appendLinesCompletedTimespan; }
        }

        #endregion

        /// <summary>
        /// 缓存长度为屏幕宽度，双视角取一半
        /// </summary>
        public int CurrentScreenLinesThreashold
        {
            get
            {
                if (ImageProcessController.Image2 != null)
                {
                    int width = (int)(ImageProcessController.Width / 2 / _horizonalScale);
                    return width >> 4 << 4;
                }
                else
                {
                    int width = (int)(ImageProcessController.Width / _horizonalScale);
                    return width >> 4 << 4;
                }
            }
        }

        /// <summary>
        /// 是否显示最后一个穿不透区域标识框
        /// </summary>
        private bool _isShowLastUnPeneRect = false;

        /// <summary>
        /// 累加器
        /// </summary>
        private int _accumulator = 0;

        /// <summary>
        /// 
        /// </summary>
        public RollingImageDataUpdateController(IRollingImageProcessController imageProcessController)
        {
            try
            {
                ShowIntelliSenseMarkers = true;
                ImageProcessController = imageProcessController;
                MaxLinesCount = imageProcessController.Image1.MaxLinesCount;

                ScannerConfig.ConfigChanged += ScannerConfig_ConfigChanged;

                // 这里的点数，必须与图像中的点数一致，否则可能会在填充显存时导致内存越位

                int count;
                count = ExchangeDirectionConfig.Service.GetView1ImageHeight();
                View1ChannelsCount = count;

                count = ExchangeDirectionConfig.Service.GetView2ImageHeight();

                View2ChannelsCount = count;

                ResetDefaultMovingSpeed();
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesImage1ShowingDetView, out Image1ViewIndex))
            {
                Image1ViewIndex = DetectViewIndex.View1;
            }

            int _imagesCount = 1;
            if (!ScannerConfig.Read(ConfigPath.ImagesCount, out _imagesCount))
            {
                _imagesCount = 1;
            }

            if (_imagesCount > 1)
            {
                if (!ScannerConfig.Read(ConfigPath.ImagesImage2ShowingDetView, out Image2ViewIndex))
                {
                    Image2ViewIndex = DetectViewIndex.View1;
                }
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesImage1HorizonalScale,out _horizonalScale))
            {
                _horizonalScale = 1.0f;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesImage1AddDataAtEnd,out _image1AddDataAtEnd))
            {
                _image1AddDataAtEnd = true;
            }
            if (!ScannerConfig.Read(ConfigPath.ImagesImage2AddDataAtEnd, out _image2AddDataAtEnd))
            {
                _image2AddDataAtEnd = true;
            }
            if (!ScannerConfig.Read(ConfigPath.ImagesSuperEnhanceThreshold, out _superEnhanceThreshold))
            {
                _superEnhanceThreshold = 60000;
            }
            ImageProcessController.SuperEnhanceThreshold = _superEnhanceThreshold;

            InitIntelliSenseModule();

            ShowingLinesCount = 0;

            InitLabelEntity();
            Labels = new Dictionary<DetectViewIndex, LinkedList<LabelRegion>>();
            Labels.Add(DetectViewIndex.View1, new LinkedList<LabelRegion>());
            Labels.Add(DetectViewIndex.View2, new LinkedList<LabelRegion>());

            LoadCacheSetting();
        }

        private int[] GenScreenRefreshSpeed(double intTime_ms, int screenRefreshFrequency, int targetArrayLen)
        {
            int[] speedArray = new int[targetArrayLen];
            double k = 1000.0 / (intTime_ms * screenRefreshFrequency);
            int baseSpeed = (int)Math.Floor(k);
            int actual = 0;
            double expect = 0;
            for (int i = 0; i < targetArrayLen; i++)
            {
                expect += k;
                actual += baseSpeed + 1;
                if (expect >= actual)
                {
                    speedArray[i] = baseSpeed + 1;
                }
                else
                {
                    speedArray[i] = baseSpeed;
                    actual -= 1;
                }
            }
            return speedArray;
        }

        /// <summary>
        /// 配置项变化事件响应函数，主要用于探测区域边框颜色的改变
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        void ScannerConfig_ConfigChanged(object sender, EventArgs e)
        {
            LoadMarkerRegionBorderColorSetting();
        }

        private void LoadMarkerRegionBorderColorSetting()
        {
            try
            {
                MarkerRegionBorderColor colorIndex;
                if (!ScannerConfig.Read(ConfigPath.IntellisenseDeiBorderColor, out colorIndex))
                {
                    colorIndex = MarkerRegionBorderColor.DarkGreen;
                }
                var color = MarkerRegionBorderColorMapper.Mapper(colorIndex);

                _deiBorderColor = color ?? System.Drawing.Color.DarkGreen;

                if (!ScannerConfig.Read(ConfigPath.IntellisenseHdiBorderColor, out colorIndex))
                {
                    colorIndex = MarkerRegionBorderColor.Red;
                }
                color = MarkerRegionBorderColorMapper.Mapper(colorIndex);
                _hdiBorderColor = color ?? System.Drawing.Color.Red;

                if (!ScannerConfig.Read(ConfigPath.IntellisenseEiBorderColor, out colorIndex))
                {
                    colorIndex = MarkerRegionBorderColor.Aqua;
                }
                color = MarkerRegionBorderColorMapper.Mapper(colorIndex);
                _eiBorderColor = color ?? System.Drawing.Color.Aqua;

                bool _showlabel;
                if (!ScannerConfig.Read(ConfigPath.AutoDetectionShowLabel, out _showlabel))
                {
                    _showlabel = true;
                }
                ShowLabel = _showlabel;
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception, "Exception occured when load markerRegion border color setting!");
                _deiBorderColor = System.Drawing.Color.DarkGreen;
                _eiBorderColor = System.Drawing.Color.Red;
                _hdiBorderColor = System.Drawing.Color.Aqua;

            }
        }

        private void LoadCacheSetting()
        {
            if (!ScannerConfig.Read(ConfigPath.PreProcShowCacheCount, out _cacheLength))
            {
                _cacheLength = 1;
            }
            if (!ScannerConfig.Read(ConfigPath.PreProcShowCacheShowTimespan, out _showLinesCompletedInterval))
            {
                _showLinesCompletedInterval = 300;
            }
            _showLinesCompletedTimespan = TimeSpan.FromMilliseconds(_showLinesCompletedInterval);
            if (!ScannerConfig.Read(ConfigPath.PreProcShowCacheAppendTimespan, out _appendLinesCompletedInterval))
            {
                _appendLinesCompletedInterval = 300;
            }
            _appendLinesCompletedTimespan = TimeSpan.FromMilliseconds(_appendLinesCompletedInterval);
        }

        private void InitIntelliSenseModule()
        {
            //加载探测区域颜色
            LoadMarkerRegionBorderColorSetting();

            _view1IntelliSense = new IntelliSenseService();
            _view1IntelliSense.RegionDetected += View1IntelliSenseOnRegionDetected;

            int viewsCount = 1;
            if (!ScannerConfig.Read(ConfigPath.MachineViewsCount, out viewsCount))
            {
                viewsCount = 1;
            }

            if (viewsCount > 1)
            {
                _view2IntelliSense = new IntelliSenseService();
                _view2IntelliSense.RegionDetected += View2IntelliSenseOnRegionDetected;
            }
        }

        /// <summary>
        /// 视角2智能探测结果回传
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="markerRegion"></param>
        private void View2IntelliSenseOnRegionDetected(object sender, MarkerRegion markerRegion)
        {
            AddRegionMark(markerRegion, DetectViewIndex.View2);

            if (ContrabandDetected != null)
            {
                ContrabandDetected(new KeyValuePair<DetectViewIndex, MarkerRegion>(DetectViewIndex.View2, markerRegion));
            }

            //ImageProcessAlgoRecommendService.Service().ProcessIntelligenceEvent(new XRayViewCadRegion(markerRegion,DetectViewIndex.View2));
            TRSNetWorkService.Service.UpdateAutoJudgeResult(DateTime.Now, markerRegion, DetectViewIndex.View2);
        }

        /// <summary>
        /// 视角1智能探测结果回传
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="markerRegion"></param>
        private void View1IntelliSenseOnRegionDetected(object sender, MarkerRegion markerRegion)
        {
            AddRegionMark(markerRegion, DetectViewIndex.View1);

            if (ContrabandDetected != null)
            {
                ContrabandDetected(new KeyValuePair<DetectViewIndex, MarkerRegion>(DetectViewIndex.View1, markerRegion));
            }

            //ImageProcessAlgoRecommendService.Service().ProcessIntelligenceEvent(new XRayViewCadRegion(markerRegion, DetectViewIndex.View1));
            TRSNetWorkService.Service.UpdateAutoJudgeResult(DateTime.Now, markerRegion, DetectViewIndex.View1);
        }

        public void Initialize()
        {
            ImageProcessController.GetImageLines += ControllerOnGetImageLines;
        }

        public void Cleanup()
        {
            ImageProcessController.GetImageLines -= ControllerOnGetImageLines;
        }

        /// <summary>
        /// gpu图像刷新线程，在此填充图像数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void ControllerOnGetImageLines(object sender, GetImageLinesEventArgs args)
        {
            //HiPerfTimer timerCal = new HiPerfTimer();
            //timerCal.ClearTimer();
            //timerCal.Start();
            GetScanlinesAndDrawMarkers(args);
            //timerCal.Stop();
            //if(timerCal.Duration>25)
            //{
            //    Tracer.TraceInfo("rolling image get image lines time out:"+timerCal.Duration+" ms,cache:"+view1cache.Count);
            //}            
        }
        
        /// <summary>
        /// 重新设置默认的图像移动速度
        /// </summary>
        private void ResetDefaultMovingSpeed()
        {
            // 根据采样周期、刷新率计算默认显示速度。
            // 如采样周期为3ms时，1000/3=333.3;及一秒钟产生的扫描线数；333.3 / 刷新率（每秒钟刷新次数，如60）= 5.6线/次
            // 取整后为5线/次，这就是显示速率。

            float ms;
            if (!ScannerConfig.Read(ConfigPath.CaptureSysLineIntegrationTime, out ms))
            {
                ms = 4;
            }

            if (ms <= 0)
            {
                ms = 4;
            }

            LineIntegrationTime = ms;
            _screenRefreshRate = DeviceCaps.RefreshRate;

            _imageDefaultMovingSpeed = (int)Math.Floor((1000.0 / LineIntegrationTime) / _screenRefreshRate);
            ImageMovingSpeed = _imageDefaultMovingSpeed;
            CurrentMovingSpeed = ImageMovingSpeed;

            if (!ScannerConfig.Read(ConfigPath.PullPlaySpeedRatio,out _pullPlaySpeedRatio))
            {
                _pullPlaySpeedRatio = 30.0f;
            }
            int imgMovingSpeedArrLen;
            if (!ScannerConfig.Read(ConfigPath.ImageMovingSpeedArrayLen, out imgMovingSpeedArrLen))
            {
                imgMovingSpeedArrLen = (int)_screenRefreshRate;
            }

            movingSpeedArray = GenScreenRefreshSpeed(LineIntegrationTime, (int)_screenRefreshRate, imgMovingSpeedArrLen);
        }

        /// <summary>
        /// 根据当前缓存的数据量大小，动态更新显示速率
        /// </summary>
        private void ChangeMovingSpeed()
        {
            // 根据当前缓存中的数据量，确定显示速率
            var newSpeed = ImageMovingSpeed;
            if (_view1ImageCache.Count >= (1200))
            {
                newSpeed = CurrentMovingSpeed + 6;
            }
            else if (_view1ImageCache.Count >= (900))
            {
                newSpeed = CurrentMovingSpeed + 4;
            }
            else if (_view1ImageCache.Count >= (600))
            {
                newSpeed = CurrentMovingSpeed + 2;
            }
            else if (_view1ImageCache.Count >= (450))
            {
                newSpeed = CurrentMovingSpeed + 1;
            }
            else if(_view1ImageCache.Count < 150)
            {
                newSpeed = CurrentMovingSpeed;
            }

            ImageMovingSpeed = newSpeed;
        }

        public void ChangeMovingSpeed(bool isPull)
        {
            _isPull = isPull;
            if (isPull)
            {
                ImageMovingSpeed = (int)(_pullPlaySpeedRatio * _imageDefaultMovingSpeed);
            }
            else
            {
                ImageMovingSpeed = _imageDefaultMovingSpeed;
            }
            CurrentMovingSpeed = ImageMovingSpeed;
        }

        /// <summary>
        /// 是否有仍未显示完毕的数据
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool HasUnshownLines()
        {
            return !(_view1ImageCache.Count==0) || !(_view2ImageCache.Count ==0);
        }

        /// <summary>
        /// 舍弃所有尚未被刷新显示的数据
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void DropUnshownLines()
        {
            DisplayScanlineData line;

            if (_view1ImageCache != null)
            {
                while (_view1ImageCache.Count > 0)
                {
                    _view1ImageCache.TryDequeue(out line);
                }            
            }

            if (_view2ImageCache != null)
            {
                while (_view2ImageCache.Count > 0)
                {
                    _view2ImageCache.TryDequeue(out line);
                }
            }
        }

        /// <summary>
        /// 清除所有的标记框
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void ClearMarkerBoxes()
        {
            if (_view1IntellisenseResults != null)
            {
                _view1IntellisenseResults.Clear();
            }

            if (_view2IntellisenseResults != null)
            {
                _view2IntellisenseResults.Clear();
            }
            if (ImageProcessController != null)
            {
                if (ImageProcessController.MarkerList != null)
                {
                    ImageProcessController.MarkerList.Clear();
                }
            }
        }       

        /// <summary>
        /// 反向填充显示图像列
        /// </summary>
        /// <param name="lines"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void ReverseAppendLines(IEnumerable<DisplayScanlineDataBundle> lines)
        {
            ReverseAppending = true;

            foreach (var bundle in lines)
            {
                AppendSingleLine(bundle);
            }
        }

        /// <summary>
        /// 回放历史图像时，使用回放数据清空一屏数据
        /// </summary>
        /// <param name="lines"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void ClearAndReverseAppend(List<DisplayScanlineDataBundle> lines)
        {
            //清屏显示函数在数据多于控件宽度时，只清屏不显示
            //该类继承IRollingImageProcessController，没有配置窗口宽度的函数
            //单个控件宽度为960或1920，
            //if (lines.Count > 960)
            //{
            //    //ClearScreen(null, true);
            //    //以卷轴的形式呈现
            //    ReverseAppendLines(lines);
            //}
            //else

            {
                ClearScreen(lines, true);
            }      

            if (lines != null)
            {
                foreach (var line in lines)
                {
                    if (line.View1Data != null)
                    {
                        _view1IntelliSense.Detect(line.View1Data);
                    }

                    if (line.View2Data != null && _view2IntelliSense != null)
                    {
                        _view2IntelliSense.Detect(line.View2Data);
                    }
                }
            }
        }

        /// <summary>
        /// 显示最新的一屏图像数据
        /// </summary>
        /// <param name="lines"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void ClearAndAppend(List<DisplayScanlineDataBundle> lines)
        {
            if (lines == null || lines.Count <1)
            {
                _view1IntellisenseResults.Clear();
                _view2IntellisenseResults.Clear();
            }
            

            ClearScreen(lines, false);

            if (lines != null)
            {
                foreach (var line in lines)
                {
                    if (line.View1Data != null)
                    {
                        _view1IntelliSense.Detect(line.View1Data);
                    }

                    if (line.View2Data != null && _view2IntelliSense != null)
                    {
                        _view2IntelliSense.Detect(line.View2Data);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void PasteScreen(List<DisplayScanlineDataBundle> lines)
        {
            if (lines == null || lines.Count < 1)
            {
                _view1IntellisenseResults.Clear();
                _view2IntellisenseResults.Clear();
            }

            // 必须先舍弃尚未显示的数据
            DropUnshownLines();
            ReverseAppending = false;

            ushort[] image1Data = null;
            ushort[] image2Data = null;
            ushort[] image1EnhancedData = null;
            ushort[] image2EnhancedData = null;
            int linesCount = 0;

            if (lines != null && lines.Count > 0)
            {
                linesCount = lines.Count;

                var view1Lines = new List<DisplayScanlineData>(linesCount);
                var view2Lines = new List<DisplayScanlineData>(linesCount);

                view1Lines.AddRange(lines.Select(line => line.View1Data));

                if (lines[0].View2Data != null)
                {
                    view2Lines.AddRange(lines.Select(line => line.View2Data));
                }

                ushort[] view1Data = null;
                ushort[] view2Data = null;
                view1Data = PrepareOverriddingImageData(view1Lines, DetectViewIndex.View1);
                if (view2Lines.Count > 0)
                {
                    view2Data = PrepareOverriddingImageData(view2Lines, DetectViewIndex.View2);
                }

                ushort[] view1EnhancedData = null;
                ushort[] view2EnhancedData = null;
                view1EnhancedData = PrepareOverriddingImageEnhancedData(view1Lines, DetectViewIndex.View1);
                if (view2Lines.Count > 0)
                {
                    view2EnhancedData = PrepareOverriddingImageEnhancedData(view2Lines, DetectViewIndex.View2);
                }

                image1Data = Image1ViewIndex == DetectViewIndex.View1 ? view1Data : view2Data;
                image2Data = Image2ViewIndex == DetectViewIndex.View1 ? view1Data : view2Data;

                image1EnhancedData = Image1ViewIndex == DetectViewIndex.View1 ? view1EnhancedData : view2EnhancedData;
                image2EnhancedData = Image2ViewIndex == DetectViewIndex.View1 ? view1EnhancedData : view2EnhancedData;

            }

            //if (ImageProcessController.MarkerList != null)
            //{
            //    ImageProcessController.MarkerList.RemoveAll(p => p.ManualTag == true);

            //    //ImageProcessController.MarkerList.Clear();
            //}

            if (image1EnhancedData == null)
            {
                ImageProcessController.ClearImages(image1Data, image2Data, linesCount);
            }
            else
            {
                ImageProcessController.ClearImages(image1EnhancedData, image2EnhancedData, linesCount);
            }

            if (lines != null)
            {
                foreach (var line in lines)
                {
                    if (line.View1Data != null)
                    {
                        _view1IntelliSense.Detect(line.View1Data);
                    }

                    if (line.View2Data != null && _view2IntelliSense != null)
                    {
                        _view2IntelliSense.Detect(line.View2Data);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void PasteOriFuseScreen(List<DisplayScanlineDataBundle> lines)
        {
            if (lines == null || lines.Count < 1)
            {
                _view1IntellisenseResults.Clear();
                _view2IntellisenseResults.Clear();
            }
            // 必须先舍弃尚未显示的数据
            DropUnshownLines();
            ReverseAppending = false;

            ushort[] image1Data = null;
            ushort[] image2Data = null;
            int linesCount = 0;
            if (lines != null && lines.Count > 0)
            {
                linesCount = lines.Count;

                var view1Lines = new List<DisplayScanlineData>(linesCount);
                var view2Lines = new List<DisplayScanlineData>(linesCount);

                view1Lines.AddRange(lines.Select(line => line.View1Data));

                if (lines[0].View2Data != null)
                {
                    view2Lines.AddRange(lines.Select(line => line.View2Data));
                }

                ushort[] view1Data = null;
                ushort[] view2Data = null;
                view1Data = PrepareOverriddingOriImageData(view1Lines, DetectViewIndex.View1);
                Tracer.TraceInfo("paste view1 ori fuse screen");
                if (view2Lines.Count > 0)
                {
                    view2Data = PrepareOverriddingOriImageData(view2Lines, DetectViewIndex.View2);
                    Tracer.TraceInfo("paste view2 ori fuse screen");
                }
                image1Data = Image1ViewIndex == DetectViewIndex.View1 ? view1Data : view2Data;
                image2Data = Image2ViewIndex == DetectViewIndex.View1 ? view1Data : view2Data;

                ImageProcessController.ClearImages(image1Data, image2Data, linesCount);
            }
            if (lines != null)
            {
                foreach (var line in lines)
                {
                    if (line.View1Data != null)
                    {
                        _view1IntelliSense.Detect(line.View1Data);
                    }

                    if (line.View2Data != null && _view2IntelliSense != null)
                    {
                        _view2IntelliSense.Detect(line.View2Data);
                    }
                }
            }
        }

        private ushort[] PrepareOverriddingOriImageData(List<DisplayScanlineData> scanLines, DetectViewIndex viewIndex)
        {
            // 将实际的数据长度调整为配置的数据长度
            var lenAdjustedScanLines = AdjustScanLinesDataLen(scanLines, viewIndex);
            var scanlineData = lenAdjustedScanLines.Select(ComposeXOriDataAndColorIndex).ToList();
            Tracer.TraceInfo("Prepare Overridding Ori Image Data");

            // 将所有线数据，统一拷贝至缓冲区data2Fill中
            if (scanlineData.Count > 0)
            {
                var data2Fill = new ushort[scanlineData.Count * scanlineData[0].Length];

                if (ReverseAppending)
                {
                    // 对于反向填充，最后一线被填充数据的编号最小
                    for (int j = 0; j < scanlineData.Count; j++)
                    {
                        scanlineData[j].CopyTo(data2Fill, j * scanlineData[0].Length);
                    }
                }
                else
                {
                    // 对于正向填充：最后一线被填充的数据的编号最大，但是输入的数据是从小到大排序
                    for (int j = 0; j < scanlineData.Count; j++)
                    {
                        if (scanlineData.Count - 1 - j >= 0)
                        {
                            scanlineData[scanlineData.Count - 1 - j].CopyTo(data2Fill, j * scanlineData[0].Length);
                        }
                        else
                        {
                            Tracer.TraceInfo("PrepareOverriddingImageData scann line data count:" + scanlineData.Count + ",j:" + j);
                        }
                    }
                }

                return data2Fill;
            }

            return null;
        }

        /// <summary>
        /// 清空当前屏幕，同时清空标记框
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="reverse">true表示反向显示，用于图像回放；false表示正向显示，用于显示最新的图像数据 </param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void ClearScreen(List<DisplayScanlineDataBundle> lines, bool reverse = false)
        {
            // 必须先舍弃尚未显示的数据
            DropUnshownLines();
            while (CurrentScreenView1ScanLines.Count > 0)
            {
                CurrentScreenView1ScanLines.RemoveFirst();
            }
            while (CurrentScreenView2ScanLines.Count > 0)
            {
                CurrentScreenView2ScanLines.RemoveFirst();
            }
            if (lines == null || lines.Count < 1)
            {
                PaintingRegionsService.Service.ClearXRayFileInfo();
            }            

            // 先清空所有报警框
            ClearMarkerBoxes();
            //ClearTipMarkerBoxes();

            ReverseAppending = reverse;
            ShowingLinesCount = 0;
            MinLineNumber = 0;
            MaxLineNumber = 0;

            ushort[] image1Data = null;
            ushort[] image2Data = null;
            ushort[] image1EnhancedData = null;
            ushort[] image2EnhancedData = null;
            int linesCount = 0;

            if (lines != null && lines.Count > 0)
            {
                linesCount = lines.Count;

                var view1Lines = new List<DisplayScanlineData>(linesCount);
                var view2Lines = new List<DisplayScanlineData>(linesCount);

                view1Lines.AddRange(lines.Select(line => line.View1Data));
                foreach (var bundle in lines)
                {
                    AppendLineToCurrentScreen(bundle.View1Data, DetectViewIndex.View1, ReverseAppending);
                }
                if (lines[0].View2Data != null)
                {
                    view2Lines.AddRange(lines.Select(line => line.View2Data));
                    foreach (var bundle in lines)
                    {
                        AppendLineToCurrentScreen(bundle.View2Data, DetectViewIndex.View2, ReverseAppending);
                    }
                }

                ushort[] view1Data = null;
                ushort[] view2Data = null;
                view1Data = PrepareOverriddingImageData(view1Lines, DetectViewIndex.View1);
                if (view2Lines.Count > 0)
                {
                    view2Data = PrepareOverriddingImageData(view2Lines, DetectViewIndex.View2);
                }

                ushort[] view1EnhancedData = null;
                ushort[] view2EnhancedData = null;
                view1EnhancedData = PrepareOverriddingImageEnhancedData(view1Lines, DetectViewIndex.View1);
                if (view2Lines.Count > 0)
                {
                    view2EnhancedData = PrepareOverriddingImageEnhancedData(view2Lines, DetectViewIndex.View2);
                }

                image1Data = Image1ViewIndex == DetectViewIndex.View1 ? view1Data : view2Data;
                image2Data = Image2ViewIndex == DetectViewIndex.View1 ? view1Data : view2Data;

                image1EnhancedData = Image1ViewIndex == DetectViewIndex.View1 ? view1EnhancedData : view2EnhancedData;
                image2EnhancedData = Image2ViewIndex == DetectViewIndex.View1 ? view1EnhancedData : view2EnhancedData;

                // 以下为新增加的代码，标记当前显示的图像的数据的有效范围
                if (reverse)
                {
                    MaxLineNumber = lines.First().LineNumber;
                    MinLineNumber = lines.Last().LineNumber;
                }
                else
                {
                    MaxLineNumber = lines.Last().LineNumber;
                    MinLineNumber = lines.First().LineNumber;
                }
            }
            else
            {
                // 清空图像，但是不填充，需更新当前的线编号管理
                ShowingLinesCount = 0;
                MinLineNumber = 0;
                MaxLineNumber = -1;
            }

            ImageProcessController.MaxLineNumber = MaxLineNumber;
            ImageProcessController.MinLineNumber = MinLineNumber;

            if (ImageProcessController.MarkerList != null)
            {
                ImageProcessController.MarkerList.RemoveAll(p => p.ManualTag == true);
                
                //ImageProcessController.MarkerList.Clear();
            }
            if (image1EnhancedData == null)
            {
                ImageProcessController.ClearImages(image1Data, image2Data, linesCount);
            }
            else
            {
                ImageProcessController.ClearImages(image1EnhancedData, image2EnhancedData, linesCount);
            }            
        }

        public void ShowImageWithOriginXRayData(bool isOrigin)
        {
            if (CurrentScreenView1ScanLines!=null && CurrentScreenView1ScanLines.Count > 0)
            {
                if (CurrentScreenView2ScanLines != null)
                {
                    ClearScreenWithOriginXRayData(CurrentScreenView1ScanLines.ToList(), CurrentScreenView2ScanLines.ToList(), isOrigin);
                }
                else
                {
                    ClearScreenWithOriginXRayData(CurrentScreenView1ScanLines.ToList(), null, isOrigin);
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void ClearScreenWithOriginXRayData(List<DisplayScanlineData> scanlineView1, List<DisplayScanlineData> scanlineView2,bool isOrigin, bool reverse = false)
        {
            ReverseAppending = reverse;
            DetectViewIndex image1ViewIndex;
            DetectViewIndex image2ViewIndex;
            if (!ScannerConfig.Read(ConfigPath.ImagesImage1ShowingDetView, out image1ViewIndex))
            {
                image1ViewIndex = DetectViewIndex.View1;
            }
            if (!ScannerConfig.Read(ConfigPath.ImagesImage2ShowingDetView, out image2ViewIndex))
            {
                image2ViewIndex = DetectViewIndex.View2;
            }

            if (isOrigin)
            {
                ushort[] image1Data = null;
                ushort[] image2Data = null;
                int linesCount = 0;
                if (scanlineView1 != null && scanlineView1.Count > 0)
                {
                    linesCount = scanlineView1.Count;

                    var view1Lines = new List<DisplayScanlineData>(linesCount);
                    var view2Lines = new List<DisplayScanlineData>(linesCount);

                    view1Lines.AddRange(scanlineView1);
                    if (scanlineView2 != null)
                    {
                        view2Lines.AddRange(scanlineView2);
                    }

                    image1Data = PrepareOverriddingImageData(view1Lines, DetectViewIndex.View1);
                    if (view2Lines.Count > 0)
                    {
                        image2Data = PrepareOverriddingImageData(view2Lines, DetectViewIndex.View2);
                    }
                }

                var view1Data = image1ViewIndex == DetectViewIndex.View1 ? image1Data : image2Data;
                var view2Data = image2ViewIndex == DetectViewIndex.View1 ? image1Data : image2Data;
                ImageProcessController.ClearImages(view1Data, view2Data, linesCount);
            }
            else
            {
                ushort[] image1Data = null;
                ushort[] image2Data = null;
                ushort[] image1EnhancedData = null;
                ushort[] image2EnhancedData = null;
                int linesCount = 0;

                if (scanlineView1 != null && scanlineView1.Count > 0)
                {
                    linesCount = scanlineView1.Count;

                    var view1Lines = new List<DisplayScanlineData>(linesCount);
                    var view2Lines = new List<DisplayScanlineData>(linesCount);

                    view1Lines.AddRange(scanlineView1);

                    if (scanlineView2 != null)
                    {
                        view2Lines.AddRange(scanlineView2);
                    }

                    image1Data = PrepareOverriddingImageData(view1Lines, DetectViewIndex.View1);
                    if (view2Lines.Count > 0)
                    {
                        image2Data = PrepareOverriddingImageData(view2Lines, DetectViewIndex.View2);
                    }

                    image1EnhancedData = PrepareOverriddingImageEnhancedData(view1Lines, DetectViewIndex.View1);
                    if (view2Lines.Count > 0)
                    {
                        image2EnhancedData = PrepareOverriddingImageEnhancedData(view2Lines, DetectViewIndex.View2);
                    }
                }

                if (image1EnhancedData != null)
                {
                    var view1Data = image1ViewIndex == DetectViewIndex.View1 ? image1EnhancedData : image2EnhancedData;
                    var view2Data = image2ViewIndex == DetectViewIndex.View1 ? image1EnhancedData : image2EnhancedData;
                    ImageProcessController.ClearImages(view1Data, view2Data, linesCount);
                }
                else
                {
                    var view1Data = image1ViewIndex == DetectViewIndex.View1 ? image1Data : image2Data;
                    var view2Data = image2ViewIndex == DetectViewIndex.View1 ? image1Data : image2Data;
                    ImageProcessController.ClearImages(view1Data, view2Data, linesCount);
                } 
            }            
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void ClearScreenWithPenetration(List<DisplayScanlineDataBundle> lines, bool reverse = false)
        {
            ReverseAppending = reverse;
            
            ushort[] image1Data = null;
            ushort[] image2Data = null;
            ushort[] image1EnhancedData = null;
            ushort[] image2EnhancedData = null;
            int linesCount = 0;

            if (lines != null && lines.Count > 0)
            {
                linesCount = lines.Count;

                var view1Lines = new List<DisplayScanlineData>(linesCount);
                var view2Lines = new List<DisplayScanlineData>(linesCount);

                view1Lines.AddRange(lines.Select(line => line.View1Data));

                if (lines[0].View2Data != null)
                {
                    view2Lines.AddRange(lines.Select(line => line.View2Data));
                }

                ushort[] view1Data = null;
                ushort[] view2Data = null;
                view1Data = PrepareOverriddingImageData(view1Lines, DetectViewIndex.View1);
                if (view2Lines.Count > 0)
                {
                    view2Data = PrepareOverriddingImageData(view2Lines, DetectViewIndex.View2);
                }

                ushort[] view1EnhancedData = null;
                ushort[] view2EnhancedData = null;
                view1EnhancedData = PrepareOverriddingImageEnhancedData(view1Lines, DetectViewIndex.View1);
                if (view2Lines.Count > 0)
                {
                    view2EnhancedData = PrepareOverriddingImageEnhancedData(view2Lines, DetectViewIndex.View2);
                }

                image1Data = Image1ViewIndex == DetectViewIndex.View1 ? view1Data : view2Data;
                image2Data = Image2ViewIndex == DetectViewIndex.View1 ? view1Data : view2Data;

                image1EnhancedData = Image1ViewIndex == DetectViewIndex.View1 ? view1EnhancedData : view2EnhancedData;
                image2EnhancedData = Image2ViewIndex == DetectViewIndex.View1 ? view1EnhancedData : view2EnhancedData;
            }

            if (image1EnhancedData == null)
            {
                ImageProcessController.ClearImages(image1Data, image2Data, linesCount);
            }
            else
            {
                ImageProcessController.ClearImages(image1EnhancedData, image2EnhancedData, linesCount);
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void AppendLines(IEnumerable<DisplayScanlineDataBundle> lines)
        {
            ReverseAppending = false;
            foreach (var bundle in lines)
            {
                AppendSingleLine(bundle);
            }
            _appendLinesDateTime = DateTime.Now;
        }

        private void AppendLineToCurrentScreen(DisplayScanlineData data, DetectViewIndex view, bool ReverseAppending)
        {
            if (data != null)
            {
                if (ReverseAppending)
                {
                    if (view == DetectViewIndex.View1)
                    {
                        lock (CurrentScreenView1ScanLines)
                        {
                            CurrentScreenView1ScanLines.AddFirst(data);
                            if (CurrentScreenView1ScanLines.Count > CurrentScreenLinesThreashold)
                            {
                                CurrentScreenView1ScanLines.RemoveLast();
                            }
                        }
                    }
                    else
                    {
                        if (data != null)
                        {
                            lock (CurrentScreenView2ScanLines)
                            {
                                CurrentScreenView2ScanLines.AddFirst(data);
                                if (CurrentScreenView2ScanLines.Count > CurrentScreenLinesThreashold)
                                {
                                    CurrentScreenView2ScanLines.RemoveLast();
                                }
                            }
                           
                        }
                    }
                }
                else
                {
                    if (view == DetectViewIndex.View1)
                    {
                        CurrentScreenView1ScanLines.AddLast(data);
                        if (CurrentScreenView1ScanLines.Count > CurrentScreenLinesThreashold)
                        {
                            CurrentScreenView1ScanLines.RemoveFirst();
                            
                        }
                    }
                    else
                    {
                        if (data != null)
                        {
                            CurrentScreenView2ScanLines.AddLast(data);
                            if (CurrentScreenView2ScanLines.Count > CurrentScreenLinesThreashold)
                            {
                                CurrentScreenView2ScanLines.RemoveFirst();
                               
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 绘制CadRegions
        /// </summary>
        private void RenderCadRegions(GetImageLinesEventArgs args)
        {
            //if (ShowIntelliSenseMarkers)
            {
                DrawCadRegions(_view1IntellisenseResults, DetectViewIndex.View1, args);
                DrawCadRegions(_view2IntellisenseResults, DetectViewIndex.View2, args);
            }
        }

        /// <summary>
        /// 绘制指定视角数据的报警框
        /// </summary>
        /// <param name="cadRegions">报警框集合</param>
        /// <param name="markerOfView">报警框所属探测数据的视角，为视角1或视角2</param>
        private void DrawCadRegions(List<MarkerRegion> cadRegions, DetectViewIndex markerOfView, GetImageLinesEventArgs args)
        {
            if (cadRegions != null && args != null)
            {
                // 如果图像1显示的是此视角的图像，则在图像1中画报警框

                if (args.Image1Updater != null && Image1ViewIndex == markerOfView)
                {
                    DrawMarkersOnImage(cadRegions, args.Image1Updater);
                }

                if (args.Image2Updater != null && markerOfView == Image2ViewIndex)
                {
                    DrawMarkersOnImage(cadRegions, args.Image2Updater);
                }
            }
        }

        int biggerLineNum = 0;
        int smallerLineNum = 0;
        private void DrawMarkersOnImage(List<MarkerRegion> cadRegions, IRollingImageUpdater image)
        {
            foreach (var region in cadRegions)
            {
                biggerLineNum = Math.Max(region.FromLine, region.ToLine);
                smallerLineNum = Math.Min(region.FromLine, region.ToLine);

                //对于宽度或者高度<5的探测区域不进行显示
                if (region.Width >= 5 && region.Height >= 5 && (MinLineNumber <= biggerLineNum && MaxLineNumber >= smallerLineNum))
                {
                    // 对线编号进行偏移，从而计算出报警框在当前屏幕中的位置
                    var fromLine = MaxLineNumber - region.FromLine;
                    var toLine = MaxLineNumber - region.ToLine;

                    var markerBox = new MarkerBox()
                    {
                        FromScanline = fromLine,
                        ToScanline = toLine,
                        ChannelStart = region.FromChannel,
                        ChannelEnd = region.ToChannel,
                        Tag = region.Name,
                    };

                    var color = System.Drawing.Color.Purple;

                    switch (region.RegionType)
                    {
                        case MarkerRegionType.Drug:
                            color = _deiBorderColor;
                            break;
                        case MarkerRegionType.Explosives:
                            color = _eiBorderColor;
                            break;
                        case MarkerRegionType.UnPenetratable:
                            if (_isShowLastUnPeneRect)
                            {
                                if (region.IsLastRect)
                                {
                                    continue;
                                }                                
                            }
                            color = _hdiBorderColor;
                            break;
                        case MarkerRegionType.Tip:
                            color = _tipBorderColor;
                            break;
                    }

                    if (region.RegionType != MarkerRegionType.Tip)
                    {
                        if (ShowIntelliSenseMarkers)
                        {
                            DrawMarkerBoxOnImage(markerBox, color, image);
                        }
                    }
                    else
                    {
                        // 对于Tip标记区域，总是显示
                        DrawMarkerBoxOnImage(markerBox, color, image);
                    }
                }
            }
        }

        /// <summary>
        /// 在指定图像中，以指定颜色绘制指定的标记框
        /// </summary>
        /// <param name="box"></param>
        /// <param name="color"></param>
        /// <param name="image"></param>
        private void DrawMarkerBoxOnImage(MarkerBox box, System.Drawing.Color color, IRollingImageUpdater image)
        {
            box.BorderColor = color;
            image.DrawMarkerBoxes(new[]
            {
                box
            });          
        }

        /// <summary>
        /// 添加新探测的CadRegion
        /// </summary>
        /// <param name="region">新探测的报警区域</param>
        /// <param name="viewIndex">报警区域所属的探测视角</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void AddRegionMark(MarkerRegion region, DetectViewIndex viewIndex)
        {
            if (region == null)
            {
                return;
            }

            if (viewIndex == DetectViewIndex.View1)
            {
                MergeCadRegion(region, _view1IntellisenseResults);
            }
            else if (viewIndex == DetectViewIndex.View2)
            {
                MergeCadRegion(region, _view2IntellisenseResults);
            }
        }

        /// <summary>
        /// 将新探测到的CadRegion与原有的探测区域进行合并。因包裹形状不规则或者左右拉动图像时会产生有重叠区域的CadRegion，需要将其合并
        /// </summary>
        /// <param name="newRegion">新探测的报警区域</param>
        /// <param name="cadRegionsList">某视角未显示的探测区域</param>
        private void MergeCadRegion(MarkerRegion newRegion, List<MarkerRegion> cadRegionsList)
        {
            //是否被合并的标志位，如果未被合并则直接添加到链表中
            bool beMerged = false;

            //遍历链表中存在的区域
            foreach (MarkerRegion cadRegion in cadRegionsList)
            {
                //合并必须类型相同
                if (cadRegion.RegionType == newRegion.RegionType)
                {
                    //判断两区域是否有交集,调用Rectangle.Intersect(Rectangle, Rectangle)函数，返回两区域相交的部分，如果没有相交则返回空
                    Rectangle rectangle = Rectangle.Intersect(cadRegion.Rect, newRegion.Rect);

                    //如果交集为非空
                    if (!rectangle.IsEmpty)
                    {
                        //判断交集区域大小
                        int intersectSize = rectangle.Height * rectangle.Width;
                        //如果相交区域大小大于某一个区域面积的50%，则将新区域合并到已有区域中
                        if (intersectSize >= newRegion.RegionAreaSize >> 1 || intersectSize >= cadRegion.RegionAreaSize >> 1)
                        {
                            lock (cadRegionsList)
                            {
                                //将新探测区域合并到已有探测区域中
                                cadRegion.InflateWith(newRegion);
                                beMerged = true;
                                if (newRegion.RegionType == MarkerRegionType.UnPenetratable)
                                {
                                    ShowAllRects(cadRegionsList);
                                    cadRegion.IsLastRect = true;
                                }                                
                            }
                        }
                    }
                }
            }
            //如果newRegion与已有探测区域均没有交集或者交集部分较小,则直接将新区域添加到链表中
            if (!beMerged)
            {
                lock (cadRegionsList)
                {
                    if (newRegion.RegionType == MarkerRegionType.UnPenetratable)
                    {
                        ShowAllRects(cadRegionsList);
                        newRegion.IsLastRect = true;
                    }                    
                    cadRegionsList.Add(newRegion);
                }
            }
        }

        private Dictionary<DetectViewIndex, LinkedList<LabelRegion>> Labels;
        private int[] ImageHeight;
        private int _labelWidth = 32;
        private int _labelHeight = 32;

        private double _imageHeightRatio = 0.95;
        private double _labelWidthRatio = 1.2;
        private double _labelHeightRatio = 1.5;

        private MarkerRegion CalcRegionLabelPos(MarkerRegion region, DetectViewIndex view)
        {
            int middleX = region.FromLine;
            int toChannel = (int)(ImageHeight[(int)view - 1] * _imageHeightRatio);

            var tag = string.IsNullOrEmpty(region.Name) ? "  " : region.Name;

            var tagLength = _labelWidth * tag.Length;
            bool flip = view == DetectViewIndex.View2 ? ImageProcessController.Image2.VerticalFlip : ImageProcessController.Image1.VerticalFlip;
            if (flip)
            {
                toChannel = _labelHeight + 20;
            }

            List<MarkerRegion> currentResult = view == DetectViewIndex.View1 ? _view1IntellisenseResults : _view2IntellisenseResults;

            while (Labels[view].Count > 15)
            {
                Labels[view].RemoveFirst();
            }

            if (Labels[view].Count > 0)
            {
                if (!flip)
                {
                    int labelHeightMax = currentResult.Count > 0 ? Math.Max(currentResult.Select(label => label.ToChannel).Max(), region.ToChannel) : region.ToChannel;
                    toChannel = FindFromHigh(toChannel, currentResult, region, labelHeightMax, view);
                    if (toChannel < labelHeightMax)
                    {
                        int labelHeightMin = currentResult.Count > 0 ? Math.Min(currentResult.Select(label => label.ToChannel).Min(), region.FromChannel) : region.FromChannel;
                        toChannel = FindFromLow(toChannel, currentResult, region, labelHeightMin, view);
                    }
                }
                else
                {
                    int labelHeightMin = currentResult.Count > 0 ? Math.Min(currentResult.Select(label => label.ToChannel).Min(), region.FromChannel) : region.FromChannel;
                    toChannel = FindFromLow(toChannel, currentResult, region, labelHeightMin, view);
                    if (toChannel > labelHeightMin)
                    {
                        int labelHeightMax = currentResult.Count > 0 ? Math.Max(currentResult.Select(label => label.ToChannel).Max(), region.ToChannel) : region.ToChannel;
                        toChannel = FindFromHigh(toChannel, currentResult, region, labelHeightMax, view);
                    }
                }
            }

            Labels[view].AddLast(new LabelRegion(middleX, middleX + tagLength, toChannel - _labelHeight, toChannel));
            region.LabelHeightPos = toChannel;
            return region;
        }

        private int FindFromHigh(int toChannel, List<MarkerRegion> currentResult, MarkerRegion region, int labelHeightMax, DetectViewIndex view)
        {
            int middleX = region.FromLine;
            var tagLength = _labelWidth * region.Name.Length;
            for (; toChannel > labelHeightMax; toChannel -= _labelHeight)
            {
                var curLabel = new LabelRegion(middleX, middleX + tagLength, toChannel - _labelHeight, toChannel);
                var labelInter = Labels[view].Where(label => curLabel.IntersectWith(label)).Any();
                if (labelInter)
                {
                    continue;
                }
                break;
            }
            return toChannel;
        }

        private int FindFromLow(int toChannel, List<MarkerRegion> currentResult, MarkerRegion region, int labelHeightMin, DetectViewIndex view)
        {
            toChannel = _labelHeight + 5;
            int middleX = region.FromLine;
            var tagLength = _labelWidth * region.Name.Length;

            for (; toChannel < labelHeightMin; toChannel += _labelHeight)
            {
                var curLabel = new LabelRegion(middleX, middleX + tagLength, toChannel - _labelHeight, toChannel);
                var labelInter = Labels[view].Where(label => curLabel.IntersectWith(label)).Any();
                var imageInter = toChannel > labelHeightMin;
                if (imageInter)
                {
                    break;
                }
                if (labelInter)
                {
                    continue;
                }
                break;
            }
            return toChannel;
        }

        private void InitLabelEntity()
        {
            if (!ScannerConfig.Read(ConfigPath.AutoDetectionLabelHeightRatio, out _labelHeightRatio))
            {
                _labelHeightRatio = 1.5;
                ScannerConfig.Write(ConfigPath.AutoDetectionLabelHeightRatio, _labelHeightRatio);
            }
            if (!ScannerConfig.Read(ConfigPath.AutoDetectionLabelWidthRatio, out _labelWidthRatio))
            {
                _labelWidthRatio = 1.2;
                ScannerConfig.Write(ConfigPath.AutoDetectionLabelWidthRatio, _labelWidthRatio);
            }
            if (!ScannerConfig.Read(ConfigPath.AutoDetectionLabelMaxHeightRatio, out _imageHeightRatio))
            {
                _imageHeightRatio = 0.95;
                ScannerConfig.Write(ConfigPath.AutoDetectionLabelMaxHeightRatio, _imageHeightRatio);
            }

            int value = 30;
            if (!ScannerConfig.Read(ConfigPath.AutoDetectionLabelWidth, out value))
            {
                value = 30;
            }
            _labelWidth = (int)(value * _labelWidthRatio);

            if (!ScannerConfig.Read(ConfigPath.AutoDetectionLabelHeight, out value))
            {
                value = 30;
            }
            _labelHeight = (int)(value * _labelHeightRatio);
        }

        private void ShowAllRects(List<MarkerRegion> cadRegionsList)
        {
            foreach (var rect in cadRegionsList)
            {
                rect.IsLastRect = false;
            }
        }

        /// <summary>
        /// 释放不在当前显示范围内的标记框
        /// </summary>
        private void ReleaseUnvisibleMarkers()
        {
            // 当一个标记框的范围超过当前显示数据编号范围2560个单位时，即释放标记框
            var margin = 2560;
            if (_view1IntellisenseResults != null)
            {
                _view1IntellisenseResults.RemoveAll(
                    t => (t.FromLine < MinLineNumber - margin && t.ToLine < MinLineNumber - margin) ||
                         (t.FromLine > MaxLineNumber + margin && t.ToLine > MaxLineNumber + margin));
            }

            // 清除视角2中不再有效区域内的标记框
            if (_view2IntellisenseResults != null)
            {
                _view2IntellisenseResults.RemoveAll(
                    t => (t.FromLine < MinLineNumber - margin && t.ToLine < MinLineNumber - margin) ||
                         (t.FromLine > MaxLineNumber + margin && t.ToLine > MaxLineNumber + margin));
            }

        }

        /// <summary>
        /// 正向填充显示图像列
        /// </summary>
        /// <param name="bundle"></param>
        private void AppendSingleLine(DisplayScanlineDataBundle bundle)
        {
            if (bundle.View1Data != null)
            {
                _view1ImageCache.Enqueue(bundle.View1Data);
                _view1IntelliSense.Detect(bundle.View1Data);
            }

            if (bundle.View2Data != null)
            {
                _view2ImageCache.Enqueue(bundle.View2Data);
                if (_view2IntelliSense != null)
                {
                    _view2IntelliSense.Detect(bundle.View2Data);
                }
            }
        }

        private List<DisplayScanlineData> view1cache = new List<DisplayScanlineData>();
        private List<DisplayScanlineData> view2cache = new List<DisplayScanlineData>();
        
       

        /// <summary>
        /// 准备用于更新图像的数据，并更新编号等信息
        /// </summary>
        /// <param name="scanLines">要填充的图像数据</param>
        /// <param name="viewIndex">表示scanLines中的数据采集自哪一个探测视角</param>
        private ushort[] PrepareOverriddingImageData(List<DisplayScanlineData> scanLines, DetectViewIndex viewIndex)
        {
            // 将实际的数据长度调整为配置的数据长度
            var lenAdjustedScanLines = AdjustScanLinesDataLen(scanLines, viewIndex);

            var scanlineData = lenAdjustedScanLines.Select(ComposeXDataAndColorIndex).ToList();

            // 将所有线数据，统一拷贝至缓冲区data2Fill中
            if (scanlineData.Count > 0)
            {               
                var data2Fill = new ushort[scanlineData.Count * scanlineData[0].Length];

                if (ReverseAppending)
                {
                    // 对于反向填充，最后一线被填充数据的编号最小
                    for (int j = 0; j < scanlineData.Count; j++)
                    { 
                        scanlineData[j].CopyTo(data2Fill, j * scanlineData[0].Length); 
                    }
                }
                else
                {
                    // 对于正向填充：最后一线被填充的数据的编号最大，但是输入的数据是从小到大排序
                    for (int j = 0; j < scanlineData.Count; j++)
                    {
                        if(scanlineData.Count - 1 - j>=0)
                        { 
                            scanlineData[scanlineData.Count - 1 - j].CopyTo(data2Fill, j * scanlineData[0].Length);
                        }
                        else
                        {
                            Tracer.TraceInfo("PrepareOverriddingImageData scann line data count:"+scanlineData.Count+",j:"+j);
                        }
                    }
                }

                return data2Fill;
            }

            return null;
        }

        private ushort[] PrepareOverriddingImageEnhancedData(List<DisplayScanlineData> scanLines, DetectViewIndex viewIndex)
        {
            if (scanLines.Count > 0 && scanLines[0].XRayDataEnhanced == null)
            {
                return null;
            }
            // 将实际的数据长度调整为配置的数据长度
            var lenAdjustedScanLines = AdjustScanLinesDataLen(scanLines, viewIndex);

            var scanlineData = lenAdjustedScanLines.Select(ComposeXDataEnhancedAndColorIndex).ToList();

            // 将所有线数据，统一拷贝至缓冲区data2Fill中
            if (scanlineData.Count > 0)
            {
                var data2Fill = new ushort[scanlineData.Count * scanlineData[0].Length];

                if (ReverseAppending)
                {
                    // 对于反向填充，最后一线被填充数据的编号最小
                    for (int j = 0; j < scanlineData.Count; j++)
                    {
                        scanlineData[j].CopyTo(data2Fill, j * scanlineData[0].Length);
                    }
                }
                else
                {
                    // 对于正向填充：最后一线被填充的数据的编号最大，但是输入的数据是从小到大排序
                    for (int j = 0; j < scanlineData.Count; j++)
                    {
                        if (scanlineData.Count - 1 - j >= 0)
                        {
                            scanlineData[scanlineData.Count - 1 - j].CopyTo(data2Fill, j * scanlineData[0].Length);
                        }
                        else
                        {
                            Tracer.TraceInfo("PrepareOverriddingImageEnhancedData scann line data count:" + scanlineData.Count + ",j:" + j);
                        }
                    }
                }

                if (viewIndex == DetectViewIndex.View1)
                {
                    // 对于正向填充，最后一线数据的编号最大
                    ShowingLinesCount += scanLines.Count;
                    ShowingLinesCount = Math.Min(ShowingLinesCount, MaxLinesCount);
                }

                return data2Fill;
            }

            return null;
        }

        /// <summary>
        /// 调整数据的长度，将实际的数据长度调整为配置的数据长度，实际数据较长，则截取前部，实际数据较短，则实际数据居中，上下补空白
        /// </summary>
        /// <param name="scanLines">实际的图像数据线集</param>
        /// <param name="viewIndex">图像的视角编号</param>
        /// <returns>长度调整为配置长度的图像数据线集</returns>
        private IEnumerable<DisplayScanlineData> AdjustScanLinesDataLen(IEnumerable<DisplayScanlineData> scanLines, DetectViewIndex viewIndex)
        {
            if (viewIndex == DetectViewIndex.View1)
            {
                channelsCount = View1ChannelsCount;
                addDataAtEnd = _image1AddDataAtEnd;
            }
            else
            {
                channelsCount = View2ChannelsCount;
                addDataAtEnd = _image2AddDataAtEnd;
            }


            // 数据长度调整后的数据
            var lenAdjustedScanLines = new List<DisplayScanlineData>();
            // todo 这里存在一个问题：如果每一线都进行数据长度判断，则效率太低（猜测）；如果只判断一次，
            // 那么存在在一次显示中包含两种长度的数据，此时显示会有问题，尤其是在图像回放第一屏，暂时没有崩溃
            // 目前使用每一线都进行判断的策略
            foreach (var line in scanLines)
            {
                if (line.XRayData.Length > channelsCount)
                {
                    // 实际数据通道数大于配置（即要显示）的通道数，则取实际数据的前部
                    var colorIndex = new ushort[channelsCount];
                    var XOriFuseData = new ushort[channelsCount];
                    var XRayData = new ushort[channelsCount];
                    var XRayDataEnhanced = new ushort[channelsCount];
                    //var matLineData = new ClassifiedLineData(line.ViewIndex, channelsCount) { IsAir = line.IsAir };
                    // 截取拷贝实际的图像线数据，包括高低能融合数据和颜色
                    Array.Copy(line.XRayData, 0, XRayData, 0, channelsCount);
                    if (line.XRayDataEnhanced!=null)
                    {
                        Array.Copy(line.XRayDataEnhanced, 0, XRayDataEnhanced, 0, channelsCount);
                    }
                    else
                    {
                        Array.Copy(line.XRayData, 0, XRayDataEnhanced, 0, channelsCount);
                    }
                    
                    Array.Copy(line.ColorIndex, 0, colorIndex, 0, channelsCount);
                    if (line.OriginalFused != null)
                    {
                        Array.Copy(line.OriginalFused, 0, XOriFuseData, 0, channelsCount);
                    }
                    lenAdjustedScanLines.Add(new DisplayScanlineData(line.ViewIndex,XRayData,XRayDataEnhanced,line.Material, colorIndex, line.LineNumber,line.IsAir));
                }
                else if (line.XRayData.Length < channelsCount)
                {
                    // 实际数据通道数小于配置（即要显示）的通道数，则实际数据居中，上下补空白
                    var colorIndex = new ushort[channelsCount];
                    var XOriFuseData = new ushort[channelsCount];
                    var XRayData = new ushort[channelsCount];
                    var XRayDataEnhanced = new ushort[channelsCount];
                    //var matLineData = new ClassifiedLineData(line.ViewIndex, channelsCount) { IsAir = line.IsAir };
                    int marginChannelsCount = channelsCount - line.XRayData.Length;
                    if (addDataAtEnd)
                    {
                        // 填充上下（无数据的）置白点
                        for (int i = 0; i < marginChannelsCount; i++)
                        {
                            XRayData[line.XRayData.Length + i] = 65530;
                            XRayDataEnhanced[line.XRayData.Length + i] = 65530;
                            colorIndex[line.XRayData.Length + i] = 50;
                            XOriFuseData[line.XRayData.Length + i] = 65530;
                        }
                        // 拷贝实际的线数据，包括高低能融合数据和颜色
                        Array.Copy(line.XRayData, 0, XRayData, 0, line.XRayData.Length);
                        if (line.XRayDataEnhanced != null)
                        {
                            Array.Copy(line.XRayDataEnhanced, 0, XRayDataEnhanced, 0, line.XRayDataEnhanced.Length);
                        }
                        else
                        {
                            Array.Copy(line.XRayData, 0, XRayDataEnhanced, 0, line.XRayData.Length);
                        }
                        Array.Copy(line.ColorIndex, 0, colorIndex, 0, line.XRayData.Length);
                        if (line.OriginalFused != null)
                        {
                            Array.Copy(line.OriginalFused, 0, XOriFuseData, 0, line.OriginalFused.Length);
                        }
                    }
                    else
                    {
                        // 填充上下（无数据的）置白点
                        for (int i = 0; i < marginChannelsCount; i++)
                        {
                            XRayData[i] = 65530;
                            XRayDataEnhanced[i] = 65530;
                            colorIndex[i] = 50;
                            XOriFuseData[i] = 65530;
                        }
                        // 拷贝实际的线数据，包括高低能融合数据和颜色
                        Array.Copy(line.XRayData, 0, XRayData, marginChannelsCount, line.XRayData.Length);
                        if (line.XRayDataEnhanced != null)
                        {
                            Array.Copy(line.XRayDataEnhanced, 0, XRayDataEnhanced, marginChannelsCount, line.XRayDataEnhanced.Length);
                        }
                        else
                        {
                            Array.Copy(line.XRayData, 0, XRayDataEnhanced, marginChannelsCount, line.XRayData.Length);
                        }
                        Array.Copy(line.ColorIndex, 0, colorIndex, marginChannelsCount, line.XRayData.Length);
                        if (line.OriginalFused != null)
                        {
                            Array.Copy(line.OriginalFused, 0, XOriFuseData, marginChannelsCount, line.OriginalFused.Length);
                        }
                    }
                    lenAdjustedScanLines.Add(new DisplayScanlineData(line.ViewIndex, XRayData, XRayDataEnhanced, line.Material, colorIndex, XOriFuseData, line.LineNumber, line.IsAir));
                }
                else
                {
                    // 实际数据通道数等于配置（即要显示）的通道数，不做处理，直接显示
                    // todo 这里没有重新创建线数据，可能会有一定的影响
                    lenAdjustedScanLines.Add(line);
                }
            }
            return lenAdjustedScanLines;
        }

        /// <summary>
        /// 将探测数据与其颜色索引进行组合：转为int型数组，排列方式为：一个点探测数据后紧跟其物质分类对应的颜色索引
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private ushort[] ComposeXDataAndColorIndex(DisplayScanlineData data)
        {
            var composed = new ushort[data.XRayData.Length << 1];
            for (int i = 0; i < composed.Length; i += 2)
            {
                composed[i] = data.XRayData[i >> 1];
                composed[i + 1] = data.ColorIndex[i >> 1];
            }

            return composed;
        }
        private ushort[] ComposeXDataEnhancedAndColorIndex(DisplayScanlineData data)
        {
            if (data.XRayDataEnhanced == null)
            {
                return null;
            }
            var composed = new ushort[data.XRayDataEnhanced.Length << 1];
            for (int i = 0; i < composed.Length; i += 2)
            {
                composed[i] = data.XRayDataEnhanced[i >> 1];
                composed[i + 1] = data.ColorIndex[i >> 1];
            }

            return composed;
        }

        #region old
        private ushort[] ComposeXOriDataAndColorIndex(DisplayScanlineData data)
        {
            var composed = new ushort[data.OriginalFused.Length << 1];
            for (int i = 0; i < composed.Length; i += 2)
            {
                composed[i] = data.OriginalFused[i >> 1];
                composed[i + 1] = data.ColorIndex[i >> 1];
            }

            return composed;
        }

        /// <summary>
        /// 原来流程
        /// </summary>
        /// <param name="args"></param>
        protected void GetScanlinesAndDrawMarkers(GetImageLinesEventArgs args)
        {
            //try
            //{
            _accumulator++;
            if (_accumulator > _screenRefreshRate)
            {
                _isShowLastUnPeneRect = !_isShowLastUnPeneRect;
                _accumulator = 1;
            }
            if (!_allowShow)
            {
                if (_view1ImageCache.Count > _cacheLength)
                {
                    _allowShow = true;
                }
                else if (_view1ImageCache.Count > 0 && IsAppendLinesDataCompleted)
                {
                    _allowShow = true;
                }

                goto UpdateControl;
            }

            if (_view1ImageCache.IsEmpty && IsShowLinesDataCompleted)
            {
                _allowShow = false;
            }

            // 根据当前剩余缓存图像量，更新显示速率等状态
            //ChangeMovingSpeed();
            if (!_isPull)
            {
                ImageMovingSpeed = movingSpeedArray[_accumulator - 1];
            }            

            // 将未显示的图像数据填充至显存
            if (_view1ImageCache != null && _view1ImageCache.Count > 0)
            {
                // 填充视角1的图像：显示至图像1或图像2中
                FillXRayViewImageCacheDataToImage(args, _view1ImageCache, DetectViewIndex.View1);
                _showLinesDateTime = DateTime.Now;
            }

            if (_view2ImageCache != null && _view2ImageCache.Count > 0)
            {
                // 填充视角2的图像：显示至图像1或图像2中
                FillXRayViewImageCacheDataToImage(args, _view2ImageCache, DetectViewIndex.View2);
            }

        UpdateControl:
            // 在图像数据更新完毕后，结束更新
            if (args.Image1Updater != null)
            {
                args.Image1Updater.EndAppending();
            }

            if (args.Image2Updater != null)
            {
                args.Image2Updater.EndAppending();
            }

            RenderCadRegions(args);
            //}
            //catch (Exception exception)
            //{
            //    Tracer.TraceException(exception, "Unexpected exception in ScanLinesRenderEngine.ImageControlOnRenderStarted");
            //}
        }

        DisplayScanlineData line = null;
        int i = 0;
        List<DisplayScanlineData> scanLines = new List<DisplayScanlineData>();
        private void FillXRayViewImageCacheDataToImage(GetImageLinesEventArgs args,
            ConcurrentQueue<DisplayScanlineData> imageCache, DetectViewIndex viewIndex)
        {
            scanLines.Clear();

            i = 0;
            while (i < ImageMovingSpeed && imageCache.Count > 0)
            {
                imageCache.TryDequeue(out line);
                scanLines.Add(line);
                AppendLineToCurrentScreen(line, viewIndex, ReverseAppending);
                i++;
            }

            FillScanLinesToImage(args.Image1Updater, args.Image2Updater, scanLines, viewIndex);

        }

        private void FillScanLinesToImage(IRollingImageUpdater image1, IRollingImageUpdater image2,
            List<DisplayScanlineData> scanLines, DetectViewIndex viewIndex)
        {
            // 将实际的数据长度调整为配置的数据长度
            var lenAdjustedScanLines = AdjustScanLinesDataLen(scanLines, viewIndex);

            var scanlineData = lenAdjustedScanLines.Select(ComposeXDataAndColorIndex).ToList();
            var scanlineDataEnhanced = lenAdjustedScanLines.Select(ComposeXDataEnhancedAndColorIndex).ToList();

            // 将所有线数据，统一拷贝至缓冲区data2Fill中
            if (scanlineData.Count > 0)
            {
                var data2Fill = new ushort[scanlineData.Count * scanlineData[0].Length];
                var data2FillEnhanced = new ushort[scanlineData.Count * scanlineData[0].Length];
                if (ReverseAppending)
                {
                    // 对于反向填充，最后一线被填充数据的编号最小
                    for (int j = 0; j < scanlineData.Count; j++)
                    {
                        scanlineData[j].CopyTo(data2Fill, j * scanlineData[0].Length);
                        scanlineDataEnhanced[j].CopyTo(data2FillEnhanced, j * scanlineData[0].Length);
                    }

                    if (image1 != null && Image1ViewIndex == viewIndex)
                    {
                        image1.ReverseAppendImageRows(data2FillEnhanced, scanlineData.Count);
                    }
                    if (image2 != null && Image2ViewIndex == viewIndex)
                    {
                        image2.ReverseAppendImageRows(data2FillEnhanced, scanlineData.Count);
                    }

                    if (viewIndex == DetectViewIndex.View1)
                    {
                        ShowingLinesCount += scanLines.Count;
                        ShowingLinesCount = Math.Min(ShowingLinesCount, MaxLinesCount);

                        // 对于反向填充，最后一线数据的编号最小
                        MinLineNumber = scanLines[scanLines.Count - 1].LineNumber;
                        MaxLineNumber = MinLineNumber + ShowingLinesCount - 1;
                    }                  
                }
                else
                {
                    // 对于正向填充：最后一线被填充的数据的编号最大，但是输入的数据是从小到大排序
                    for (int j = 0; j < scanlineData.Count; j++)
                    {
                        scanlineData[scanlineData.Count - 1 - j].CopyTo(data2Fill, j * scanlineData[0].Length);
                        scanlineDataEnhanced[scanlineData.Count - 1 - j].CopyTo(data2FillEnhanced, j * scanlineData[0].Length);
                    }

                    if (image1 != null && Image1ViewIndex == viewIndex)
                    {
                        image1.AppendImageRows(data2FillEnhanced, scanlineData.Count);
                    }
                    if (image2 != null && Image2ViewIndex == viewIndex)
                    {
                        image2.AppendImageRows(data2FillEnhanced, scanlineData.Count);
                    }

                    if (viewIndex == DetectViewIndex.View1)
                    {
                        ShowingLinesCount += scanLines.Count;
                        ShowingLinesCount = Math.Min(ShowingLinesCount, MaxLinesCount);

                        // 对于正向填充，最后一线数据的编号最大
                        MaxLineNumber = scanLines[scanLines.Count - 1].LineNumber;
                        MinLineNumber = MaxLineNumber - ShowingLinesCount + 1;
                    }                    
                }
                if (viewIndex == DetectViewIndex.View1)
                {
                    ImageProcessController.MaxLineNumber = MaxLineNumber;
                    ImageProcessController.MinLineNumber = MinLineNumber;
                }
                ReleaseUnvisibleMarkers();
            }
        }
        #endregion
    }


    /****************************************************/
    class DataReadyToFill
    {
        public ushort[] dataFill { private set; get; }
        public ushort[] dataFillEnhanced { private set; get; }
        public int Count { private set; get; }
        public int LineNumber { private set; get; }
        public DataReadyToFill(ushort[] dataFill, ushort[] dataFillEnhanced, int Count, int LineNumber)
        {
            this.dataFill = dataFill;
            this.dataFillEnhanced = dataFillEnhanced;
            this.Count = Count;
            this.LineNumber = LineNumber;
        }
        public DataReadyToFill()
        {

        }
    }
}
