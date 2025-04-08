using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using UI.Common.Tracers;
using UI.XRay.Business.Algo;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Common.Utilities;
using UI.XRay.Flows.Services.DataProcess;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace UI.XRay.Flows.Services
{
    public class DtGCUSTDCaptureServicePart : ICaptureServicePart
    {
        private DTDetSysType _dtDetSysType;

        private DTGCUControlClr _dtGCUSTDControl;

        DTErrorScanline dtNowError = new DTErrorScanline(0, "");

        DTErrorScanline dtPreError = new DTErrorScanline(0, "");

        /// <summary>
        /// 视角1的采集配置信息
        /// </summary>
        public DTCaptureDesc View1Desc { get; private set; }

        /// <summary>
        /// 视角2的采集配置信息
        /// </summary>
        public DTCaptureDesc View2Desc { get; private set; }

        private int _viewsCount;

        private int _captureCardCount;

        /// <summary>
        /// 交换两个视角的数据顺序
        /// </summary>
        private bool _exchangeViewsOrder = false;

        /// <summary>
        /// 探测视角个数，只能是1或者2
        /// </summary>
        public int ViewsCount
        {
            get { return _viewsCount; }
            private set { _viewsCount = value; }
        }

        /// <summary>
        /// 视角1的数据缓存
        /// </summary>
        private ConcurrentQueue<RawScanlineData> View1LinesCache { get; set; }

        /// <summary>
        /// 视角2的数据缓存
        /// </summary>
        private ConcurrentQueue<RawScanlineData> View2LinesCache { get; set; }

        //记录接收到线数据的时间
        private DateTime? _lastCaptureView1LineDataTime = null;

        private TimeSpan _maxView1TwoLineDataSpan = TimeSpan.MinValue;

        private DateTime? _lastCaptureView2LineDataTime = null;

        private TimeSpan _maxView2TwoLineDataSpan = TimeSpan.MinValue;

        /// <summary>
        /// 两线数据之间的间隔，当两线数据之间的间隔大于此值时记录
        /// </summary>
        private readonly TimeSpan _twoLineDataSpanThres = TimeSpan.FromMilliseconds(200);

        /// <summary>
        /// 本底均值
        /// </summary>
        private ushort _background = 4000;

        /// <summary>
        /// 双视角数据锁
        /// </summary>
        private object _viewDataLock = new object();

        /// <summary>
        /// 一轮记录（大概60000线数据）的数量记录
        /// </summary>
        private int _view1LineDataOneRoundRecord = 0;
        private int _view2LineDataOneRoundRecord = 0;

        /// <summary>
        /// 新一轮记录的起始时间
        /// </summary>
        private DateTime _newView1RecordRoundStartTime = DateTime.Now;
        private DateTime _newView2RecordRoundStartTime = DateTime.Now;

        /// <summary>
        /// 每60000线记录一下时间
        /// </summary>
        private const int OneRoundMaxRecordCount = 60000;
        /// <summary>
        /// X-GCU 有3通道和5通道两种
        /// </summary>
        private int _channelsCount = 5;

        /// <summary>
        /// 预处理线程
        /// </summary>
        private Thread _preProcessingThread;
        /// <summary>
        /// 预处理线程退出信号
        /// </summary>
        private bool _preprocessThreadExitSignal = true;

        private ConcurrentQueue<DTScanline> _waitingProcessQueue = new ConcurrentQueue<DTScanline>();
        private DislocationCorrector _corrector1;
        private DislocationCorrector _corrector2;

        public DtGCUSTDCaptureServicePart()
        {
            Tracer.TraceEnterFunc("UI.XRay.Flows.Services.DtGCUSTDCaptureServicePart");

            IntegrationTime = 4000;

            try
            {
                ScannerConfig.ConfigChanged += ScannerConfigOnConfigChanged;

                _preprocessThreadExitSignal = false;
                if (_preProcessingThread == null)
                {
                    _preProcessingThread = new Thread(ImageEnhanceThreadRoutine)
                    {
                        IsBackground = true
                    };
                    _preProcessingThread.Start();
                }

                LoadSettings();
                
                if (_captureCardCount == 2)
                {
                    _dtGCUSTDControl = new DTGCUControlClr(2, IsDualView);

                }
                else
                {
                    _dtGCUSTDControl = new DTGCUControlClr(1, IsDualView);
                }


                View1LinesCache = new ConcurrentQueue<RawScanlineData>();
                View2LinesCache = new ConcurrentQueue<RawScanlineData>();
                if (View1Desc != null)
                {
                    View1ChannelsCount = (View1Desc.Video1CardCount + View1Desc.Video2CardCount +
                                          View1Desc.Video3CardCount + View1Desc.Video4CardCount) * 64;
                }

                if (View2Desc != null)
                {
                    View2ChannelsCount = (View2Desc.Video1CardCount + View2Desc.Video2CardCount +
                                          View2Desc.Video3CardCount + View2Desc.Video4CardCount) * 64;
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception, "Unexpected exception in DtGCUSTDCaptureServicePart constructor.");
            }

            Tracer.TraceExitFunc("UI.XRay.Flows.Services.DtGCUSTDCaptureServicePart");
        }

        private void ScannerConfigOnConfigChanged(object sender, EventArgs eventArgs)
        {
            try
            {
                LoadExchangeViewsOrderFlag();
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        /// <summary>
        /// 加载各个探测视角的具体配置
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void LoadSettings()
        {
            if (!ScannerConfig.Read(ConfigPath.CaptureSysDTDetSysType, out _dtDetSysType))
            {
                _dtDetSysType = DTDetSysType.F3_C4_XDAQ;
            }

            var dualEnergy = true;

            if (!ScannerConfig.Read(ConfigPath.CaptureSysIsDualEnergy, out dualEnergy))
            {
                // 默认为双能
                dualEnergy = true;
            }

            // 加载启用的探测视角个数
            if (!ScannerConfig.Read(ConfigPath.CaptureSysBoardCount, out _captureCardCount))
            {
                _captureCardCount = 1;
            }

            // 加载启用的探测视角个数
            if (!ScannerConfig.Read(ConfigPath.MachineViewsCount, out _viewsCount))
            {
                _viewsCount = 1;
            }

            _viewsCount = Math.Min(2, _viewsCount);
            _viewsCount = Math.Max(1, _viewsCount);

            int[] view1Offsets = LoadDislocationOffsets(ConfigPath.MachineView1DislocationOffsets);
            if(view1Offsets != null)
            {
                _corrector1 = new DislocationCorrector(64, view1Offsets);
            }
            // 加载视角1的配置
            var view1Cards = LoadCardsDist(ConfigPath.CaptureSysDTView1CardsDist);
            if (view1Cards != null)
            {
                View1Desc = new DTCaptureDesc(view1Cards[0], view1Cards[1], view1Cards[2], view1Cards[3], dualEnergy, 250);
            }
            else
            {
                View1Desc = new DTCaptureDesc(2, 0, 0, 0, dualEnergy, 250);
            }

            // 如果配置为双视角，则加载视角2的配置
            if (IsDualView)
            {
                if (_captureCardCount > 1)
                {
                    int[] view2Offsets = LoadDislocationOffsets(ConfigPath.MachineView2DislocationOffsets);
                    if (view2Offsets != null)
                    {
                        _corrector2 = new DislocationCorrector(64, view2Offsets);
                    }
                    var view2Cards = LoadCardsDist(ConfigPath.CaptureSysDTView2CardsDist);
                    if (view2Cards != null)
                    {
                        View2Desc = new DTCaptureDesc(view2Cards[0], view2Cards[1], view2Cards[2], view2Cards[3], dualEnergy, 250);
                    }
                    else
                    {
                        View2Desc = new DTCaptureDesc(2, 0, 0, 0, dualEnergy, 160);
                    }
                }
                else
                {
                    View2Desc = new DTCaptureDesc(0, 0, 0, 0, dualEnergy, 160);
                }
            }

            float lineIntegrationTime;
            if (ScannerConfig.Read(ConfigPath.CaptureSysLineIntegrationTime, out lineIntegrationTime))
            {
                IntegrationTime = (int)(lineIntegrationTime * 1000);
            }
            string _hostIp = "192.168.1.2";
            if (ScannerConfig.Read(ConfigPath.CaptureSysHostIP, out _hostIp))
            {
                RemoteIP = _hostIp;
            }

            if (!ScannerConfig.Read(ConfigPath.PreProcGroundLowAvgUpper, out _background))
            {
                _background = 4000;
            }

            if (!ScannerConfig.Read(ConfigPath.CaptureSysDTChannels, out _channelsCount))
            {
                _channelsCount = 5;
            }

            LoadExchangeViewsOrderFlag();
        }

        /// <summary>
        /// 加载是否交换视角1和视角2数据的标志
        /// </summary>
        private void LoadExchangeViewsOrderFlag()
        {
            if (_viewsCount > 1)
            {
                _exchangeViewsOrder = ExchangeDirectionConfig.Service.IsExchangeDetector;

                Tracer.TraceInfo("CaptureSys: ExchangeViewsOrder is " + _exchangeViewsOrder.ToString());
            }
        }

        /// <summary>
        /// 加载某个视角的错茬情况
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private int[] LoadDislocationOffsets(string configPath)
        {
            var offsetStr = string.Empty;
            if(ScannerConfig.Read(configPath,out offsetStr))
            {
                var offsetArr = offsetStr.Split(new[] { ',' });
                int[] offset = new int[offsetArr.Length];
                for (int i = 0; i < offsetArr.Length; i++)
                {
                    offset[i] = int.Parse(offsetArr[i]);
                }
                return offset;
            }
            return null;
        }


        /// <summary>
        /// 加载某个视角的探测板分布情况
        /// </summary>
        /// <param name="configPath">视角的探测板配置路径</param>
        /// <returns>成功则返回其探测板分布数组，数组长度为4；失败则返回null</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private int[] LoadCardsDist(string configPath)
        {
            var cards = "0,0,0,0";
            if (ScannerConfig.Read(configPath, out cards))
            {
                var cardsStr = cards.Split(new[] { ',' });
                if (cardsStr.Length == 4)
                {
                    var result = new int[4];
                    for (int i = 0; i < 4; i++)
                    {
                        int.TryParse(cardsStr[i], out result[i]);
                    }
                    return result;
                }
            }

            return null;
        }

        // TODO: 用于测试开软件第一个包延迟出图的问题
        private bool isFirstLine = true;
        /// <summary>
        /// DT数据采集事件响应
        /// </summary>
        /// <param name="dtScanline">视角1或视角2采集的一线数据</param>
        private void DtControlOnScanlineCaptured(DTScanline dtScanline)
        {
            if (isFirstLine)
            {
                Tracer.TraceDebug("[BagDelayTest] FIRST LINE Received!");
                isFirstLine = false;
            }
            _waitingProcessQueue.Enqueue(dtScanline);
        }

        /// <summary>
        /// DT错误报错事件响应
        /// </summary>
        /// <param name="dtScanline">视角1或视角2采集的一线数据</param>
        private void DtControlOnErrorCaptured(DTErrorScanline dtError)
        {
            dtNowError.Event_Id = dtError.Event_Id;
            if (dtNowError.Event_Id != dtPreError.Event_Id)
            {
                Tracer.TraceError("dt error,event id " + dtNowError.Event_Id + ",event error:" + dtNowError.Event_Info);
            }
            dtPreError.Event_Id = dtNowError.Event_Id;
        }

        DTScanline dtScanline = null;
        private void ImageEnhanceThreadRoutine()
        {
            while (!_preprocessThreadExitSignal)
            {
                if (!_waitingProcessQueue.IsEmpty)
                {
                    while (_waitingProcessQueue.TryDequeue(out dtScanline))
                    {
                        if (_corrector1 != null)
                        {
                            ushort[][] scanLineData = null;
                            switch (dtScanline.View)
                            {
                                case DTView.View1:
                                    scanLineData = _corrector1.GetCorrectedData(dtScanline.Low, dtScanline.High);
                                    break;
                                case DTView.View2:
                                    scanLineData = _corrector2.GetCorrectedData(dtScanline.Low, dtScanline.High);
                                    break;
                            }
                            if (scanLineData == null)
                                continue;
                            DTScanline dtLine = new DTScanline(scanLineData[0], scanLineData[1], dtScanline.View);
                            dtScanline = dtLine;
                        }
                        if (!IsDualView)
                        {
                            var line = new RawScanlineData(DetectViewIndex.View1, dtScanline.Low, dtScanline.High);

                            // 对于单视角，直接将数据输出
                            if (ScanlineCaptured != null)
                            {
                                ScanlineCaptured(this, new RawScanlineDataBundle(line, null));
                                //WatcherCaptureSys(ref _lastCaptureView1LineDataTime, ref _maxView1TwoLineDataSpan, DetectViewIndex.View1);
                            }
                        }
                        else
                        {
                            lock (_viewDataLock)
                            {
                                // 对于双视角，先将两个视角的数据分别缓存到队列中，再根据两个队列的长度情况同步向外输出
                                if (dtScanline.View == DTView.View1)
                                {
                                    if (_exchangeViewsOrder)
                                    {
                                        RawScanlineData data = new RawScanlineData(DetectViewIndex.View2, dtScanline.Low, dtScanline.High);
                                        data.IsGround = ViewLineDataIsGround(data);

                                        // 交换视角1和视角2的数据
                                        View2LinesCache.Enqueue(data);

                                        //WatcherCaptureSys(ref _lastCaptureView2LineDataTime, ref _maxView2TwoLineDataSpan, DetectViewIndex.View2);

                                        RecordNViewDataTime(ref _view2LineDataOneRoundRecord, ref _newView2RecordRoundStartTime, DetectViewIndex.View2);

                                    }
                                    else
                                    {
                                        RawScanlineData data = new RawScanlineData(DetectViewIndex.View1, dtScanline.Low, dtScanline.High);
                                        data.IsGround = ViewLineDataIsGround(data);
                                        View1LinesCache.Enqueue(data);

                                        //WatcherCaptureSys(ref _lastCaptureView1LineDataTime, ref _maxView1TwoLineDataSpan, DetectViewIndex.View1);
                                        RecordNViewDataTime(ref _view1LineDataOneRoundRecord, ref _newView1RecordRoundStartTime, DetectViewIndex.View1);

                                    }
                                }
                                else
                                {
                                    if (_exchangeViewsOrder)
                                    {
                                        // 交换视角1和视角2的数据
                                        RawScanlineData data = new RawScanlineData(DetectViewIndex.View1, dtScanline.Low, dtScanline.High);
                                        data.IsGround = ViewLineDataIsGround(data);
                                        View1LinesCache.Enqueue(data);

                                        //WatcherCaptureSys(ref _lastCaptureView1LineDataTime, ref _maxView1TwoLineDataSpan, DetectViewIndex.View1);

                                        RecordNViewDataTime(ref _view1LineDataOneRoundRecord, ref _newView1RecordRoundStartTime, DetectViewIndex.View1);
                                    }
                                    else
                                    {
                                        RawScanlineData data = new RawScanlineData(DetectViewIndex.View2, dtScanline.Low, dtScanline.High);
                                        data.IsGround = ViewLineDataIsGround(data);

                                        // 交换视角1和视角2的数据
                                        View2LinesCache.Enqueue(data);

                                        //WatcherCaptureSys(ref _lastCaptureView2LineDataTime, ref _maxView2TwoLineDataSpan, DetectViewIndex.View2);

                                        RecordNViewDataTime(ref _view2LineDataOneRoundRecord, ref _newView2RecordRoundStartTime, DetectViewIndex.View2);
                                    }
                                }

                                TryOutputDualViewData();
                            }
                        }
                    }
                }
                System.Threading.Thread.Sleep(1);
            }
        }

        private void RecordNViewDataTime(ref int viewLineDataOneRoundRecord, ref DateTime newViewRecordRoundStartTime, DetectViewIndex index)
        {
            if (viewLineDataOneRoundRecord == 0)
            {
                //数据为空时记录新一轮的时间
                newViewRecordRoundStartTime = DateTime.Now;
            }

            viewLineDataOneRoundRecord++;
            //超过60000线，则记录一下60000线所用的总时间
            if (viewLineDataOneRoundRecord >= OneRoundMaxRecordCount)
            {
                TimeSpan oneRoundTimeSpan = DateTime.Now - newViewRecordRoundStartTime;

                Tracer.TraceInfo("Record 60000 LineData Time: " + index.ToString() + " in this round record cost " + oneRoundTimeSpan.TotalMilliseconds);

                viewLineDataOneRoundRecord = 0;
                newViewRecordRoundStartTime = DateTime.Now;
            }
        }

        /// <summary>
        /// 输出双视角的数据
        /// </summary>
        private void TryOutputDualViewData()
        {
            RawScanlineData line1 = null;
            RawScanlineData line2 = null;

            // 此处不可以 使用 lock（this），因为会导致死锁发生
            //if (Monitor.TryEnter(this, 10))
            {
                try
                {
                    while (true)
                    {
                        // 必须将两个视角的数据同步输出
                        if (View1LinesCache.Count > 0 && View2LinesCache.Count > 0)
                        {
                            RawScanlineData scanline;
                            if (View1LinesCache.TryDequeue(out scanline))
                            {
                                line1 = scanline;
                            }

                            if (View2LinesCache.TryDequeue(out scanline))
                            {
                                line2 = scanline;
                            }

                            if (line1 != null && ScanlineCaptured != null && line2 != null)
                            {
                                ScanlineCaptured(this, new RawScanlineDataBundle(line1, line2));
                            }
                        }
                        else
                        {
                            break;
                        }
                    }


                    //todo:test 高琦： 2016.11.16 13：54 修改DT数据采集一帧为8线，重新写此处丢数据的逻辑：
                    //todo：如果某一视角数据超过65线，另一视角数据为0，首先丢弃其中的黑线（本底线）；如果丢弃了黑线后数据多的视角数据仍超过129线，每4线丢一线

                    List<RawScanlineData> notGroundDatas = new List<RawScanlineData>();

                    //是否丢弃过本底数据
                    bool hasDumpGroundData = false;

                    // 保护机制：防止某个视角没有数据，导致内存泄露
                    if (View1LinesCache.Count >= 65 && View2LinesCache.Count == 0)
                    {
                        //Tracer.TraceWarning("CaptureSys: View1LinesCache.count >= 65 && View2LinesCache.Count == 0!");

                        RawScanlineData scanline;

                        while (View1LinesCache.TryDequeue(out scanline))
                        {
                            if (!scanline.IsGround)
                            {
                                notGroundDatas.Add(scanline);
                            }
                            else
                            {
                                hasDumpGroundData = true;
                            }
                        }

                        if (hasDumpGroundData)
                        {
                            Tracer.TraceWarning("CaptureSys: View1LinesCache.count >= 65 && View2LinesCache.Count == 0, dump ground linedata of view1LinesCache!");
                        }

                        //剔除白线后多数据的视角数据量还是大于129线，则每4线丢一线
                        if (notGroundDatas.Count > 129)
                        {
                            Tracer.TraceWarning("CaptureSys: View1LinesCache.count >= 129 && View2LinesCache.Count == 0, dump linedata every 4 linedata of view1LinesCache!");

                            int deleteIndex = 1;

                            foreach (RawScanlineData t in notGroundDatas)
                            {
                                if (deleteIndex % 4 != 0)
                                {
                                    View1LinesCache.Enqueue(t);
                                }

                                deleteIndex++;
                            }
                        }
                        else
                        {
                            foreach (var rawScanlineData in notGroundDatas)
                            {
                                View1LinesCache.Enqueue(rawScanlineData);
                            }
                        }
                    }

                    //清空视角1计算中的缓存
                    notGroundDatas.Clear();
                    hasDumpGroundData = false;


                    if (View2LinesCache.Count >= 65 && View1LinesCache.Count == 0)
                    {
                        //Tracer.TraceWarning("CaptureSys: View2LinesCache.count >= 65 && View1LinesCache.Count == 0!");

                        RawScanlineData scanline;
                        while (View2LinesCache.TryDequeue(out scanline))
                        {
                            if (!scanline.IsGround)
                            {
                                notGroundDatas.Add(scanline);
                            }
                            else
                            {
                                hasDumpGroundData = true;
                            }
                        }

                        if (hasDumpGroundData)
                        {
                            Tracer.TraceWarning("CaptureSys: View2LinesCache.count >= 65 && View1LinesCache.Count == 0, dump ground linedata of view2LinesCache!");
                        }

                        //剔除白线后多数据的视角数据量还是大于129线，则每4线丢一线
                        if (notGroundDatas.Count > 129)
                        {
                            Tracer.TraceWarning("CaptureSys: View2LinesCache.count >= 129 && View1LinesCache.Count == 0, dump linedata every 4 linedata of view2LinesCache!");

                            int deleteIndex = 1;

                            foreach (RawScanlineData t in notGroundDatas)
                            {
                                if (deleteIndex % 4 != 0)
                                {
                                    View2LinesCache.Enqueue(t);
                                }

                                deleteIndex++;
                            }
                        }
                        else
                        {
                            foreach (var rawScanlineData in notGroundDatas)
                            {
                                View2LinesCache.Enqueue(rawScanlineData);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Tracer.TraceException(e);
                    //throw;
                }
                finally
                {
                    //Monitor.Exit(this);
                }
            }
        }

        private bool ViewLineDataIsGround(RawScanlineData lineData)
        {
            try
            {
                return lineData.Low.Average(i => i) < _background;
            }
            catch (Exception e)
            {
                Tracer.TraceException(e, "occured when calculate view data is ground or not in DTcaptureservicePart!");
            }
            return false;
        }

        /// <summary>
        /// 检测线数据的接收
        /// </summary>
        private void WatcherCaptureSys(ref DateTime? lastCaptureLineDateTime, ref TimeSpan maxTwoLineDataSpan, DetectViewIndex viewIndex)
        {
            if (lastCaptureLineDateTime == null)
            {
                lastCaptureLineDateTime = DateTime.Now;
                return;
            }

            var newCaptureDataTime = DateTime.Now;
            var twoLineDataSpan = newCaptureDataTime - lastCaptureLineDateTime;

            if (twoLineDataSpan > maxTwoLineDataSpan)
            {
                maxTwoLineDataSpan = twoLineDataSpan.Value;
                Tracer.TraceWarning("CaptureSysWatcher: Now the max timespan of two linedata of " + viewIndex.ToString() +
                                    " is " +
                                    maxTwoLineDataSpan.TotalMilliseconds);
            }

            if (twoLineDataSpan >= _twoLineDataSpanThres)
            {
                Tracer.TraceWarning("CaptureSysWatcher: The max timespan of two line data of " + viewIndex.ToString() +
                                    " is larger than " +
                                    _twoLineDataSpanThres.TotalMilliseconds + ", and the span is " +
                                    twoLineDataSpan.Value.TotalMilliseconds);
            }

            lastCaptureLineDateTime = newCaptureDataTime;
        }

        #region 接口IXRayImagingPart 的实现

        public event EventHandler<RawScanlineDataBundle> ScanlineCaptured;

        /// <summary>
        /// 设备打开的弱事件
        /// </summary>
        private readonly SmartWeakEvent<EventHandler> _deviceOpendWeakEvent = new SmartWeakEvent<EventHandler>();
        public event EventHandler DeviceOpenedWeakEvent
        {
            add { _deviceOpendWeakEvent.Add(value); }
            remove { _deviceOpendWeakEvent.Remove(value); }
        }

        /// <summary>
        /// 设备关闭的弱事件
        /// </summary>
        private readonly SmartWeakEvent<EventHandler> _deviceClosedWeakEvent = new SmartWeakEvent<EventHandler>();
        public event EventHandler DeviceClosedWeakEvent
        {
            add { _deviceClosedWeakEvent.Add(value); }
            remove { _deviceClosedWeakEvent.Remove(value); }
        }

        public event System.EventHandler<bool> ConnectionStateUpdated;

        public bool Alive { get; private set; }

        public bool IsOpened { get; private set; }

        public bool IsDualView
        {
            get { return ViewsCount == 2; }
        }

        /// <summary>
        /// Detect channels count of view1
        /// </summary>
        public int View1ChannelsCount { get; private set; }

        /// <summary>
        /// Detect channels count of view2
        /// </summary>
        public int View2ChannelsCount { get; private set; }

        /// <summary>
        /// Line integration time, in microsecond.
        /// </summary>
        public int IntegrationTime { get; private set; }
        /// <summary>
        /// 高速采集板IP
        /// </summary>
        public string RemoteIP { get; private set; }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool Open()
        {
            IsOpened = _dtGCUSTDControl.Open();
            if (IsOpened)
            {
                int _energyMode = View1Desc.DualEnergy == true ? 1 : 0;
                bool isFiveChannels = _channelsCount == 5 ? true : false;

                if (_exchangeViewsOrder && ViewsCount == 2 && _captureCardCount == 2)
                {
                    _dtGCUSTDControl.SetCardPara(View2Desc, View1Desc, IntegrationTime, false, isFiveChannels);
                }
                else if (_exchangeViewsOrder && ViewsCount == 2 && _captureCardCount == 1)
                {
                    var second = View1Desc.Video2CardCount;
                    View1Desc.Video2CardCount = View1Desc.Video1CardCount;
                    View1Desc.Video1CardCount = second;
                    _dtGCUSTDControl.SetCardPara(View1Desc, View2Desc, IntegrationTime, false, isFiveChannels);
                }
                else
                {
                    _dtGCUSTDControl.SetCardPara(View1Desc, View2Desc, IntegrationTime, false, isFiveChannels);
                }


                //if (IsDualView )
                //{
                //   _dtGCUSTDControl.SetViewsInfo(View2Desc.Video1CardCount, View2Desc.Video2CardCount, true);
                //}
                //else
                //{
                //    _dtGCUSTDControl.SetViewsInfo(View1Desc.Video1CardCount + View1Desc.Video2CardCount, 0, false);
                //}

                _dtGCUSTDControl.ScanlineCaptured += DtControlOnScanlineCaptured;
                _dtGCUSTDControl.ErrorCaptured += DtControlOnErrorCaptured;
                _deviceOpendWeakEvent.Raise(this, EventArgs.Empty);

            }

            return IsOpened;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Close()
        {
            _dtGCUSTDControl.ScanlineCaptured -= DtControlOnScanlineCaptured;
            _dtGCUSTDControl.ErrorCaptured -= DtControlOnErrorCaptured;
            _dtGCUSTDControl.Close();
            IsOpened = false;

            _deviceClosedWeakEvent.Raise(this, EventArgs.Empty);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool StartCapture()
        {
            return _dtGCUSTDControl.StartCapture();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void StopCapture()
        {
            _preprocessThreadExitSignal = true;

            _dtGCUSTDControl.StopCapture();
            if (_preProcessingThread != null)
            {
                _preProcessingThread.Join();
                _preProcessingThread = null;
            }

        }

        public void SetIntegrationTime(float integrationTime)
        {

        }

        public void ClearLinesCache()
        {
            RawScanlineData raw = null;
            while (View1LinesCache.Count > 0)
            {
                View1LinesCache.TryDequeue(out raw);
            }
            while (View2LinesCache.Count > 0)
            {
                View2LinesCache.TryDequeue(out raw);
            }
        }

        #endregion 接口IXRayImagingPart 的实现
    }
}
