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
using XRayAcquisitionSys;
using XRayAcquisitionSys.TYM;

namespace UI.XRay.Flows.Services
{
    public class TYMCaptureServicePart : ICaptureServicePart
    {
        #region Fields
        #region 运行参数
        private int viewCount;
        private int daqCount;

        /// <summary>
        /// 交换两个视角的数据顺序
        /// </summary>
        private bool exchangeViewsOrder = false;

        /// <summary>
        /// 预处理线程退出信号
        /// </summary>
        private bool preprocessThreadExitSignal = true;

        /// <summary>
        /// 本底均值
        /// </summary>
        private ushort background;

        /// <summary>
        /// 每60000线记录一下时间
        /// </summary>
        private const int oneRoundMaxRecordCount = 60000;

        /// <summary>
        /// 一轮记录（大概60000线数据）的数量记录
        /// </summary>
        private int view1LineDataOneRoundRecord = 0;
        private int view2LineDataOneRoundRecord = 0;

        /// <summary>
        /// 新一轮记录的起始时间
        /// </summary>
        private DateTime newView1RecordRoundStartTime = DateTime.Now;
        private DateTime newView2RecordRoundStartTime = DateTime.Now;

        private string[] daqIPs = new string[2];
        private int[] daqCmdPorts = new int[2];
        private int[] daqImgPorts = new int[2];

        private int[] view1CardCount = new int[2];
        private int[] view2CardCount = new int[2];
        #endregion

        #region 运行工具变量
        TYMDAQControl tymDAQControl;

        /// <summary>
        /// 预处理线程
        /// </summary>
        private Thread preProcessingThread;

        private ConcurrentQueue<DAQScanline> waitingProcessQueue = new ConcurrentQueue<DAQScanline>();

        /// <summary>
        /// 双视角数据锁
        /// </summary>
        private object viewDataLock = new object();

        private ConcurrentQueue<RawScanlineData> view1LinesCache;
        private ConcurrentQueue<RawScanlineData> view2LinesCache;
        #endregion

        #region 运行中间变量
        DAQScanline daqScanline = null;
        #endregion

        #region Events
        public event EventHandler<RawScanlineDataBundle> ScanlineCaptured;
        public event EventHandler<bool> ConnectionStateUpdated;

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
        #endregion
        #endregion

        #region Properties
        public bool Alive => throw new NotImplementedException();

        public bool IsOpened { get; private set; }

        public bool IsDualView { get; private set; }

        public int IntegrationTime { get; private set; }
        #endregion

        #region Contructor & Decontrucor
        public TYMCaptureServicePart()
        {
            Tracer.TraceEnterFunc("UI.XRay.Flows.Services.TYMCaptureServicePart");

            IntegrationTime = 4000;

            try
            {
                ScannerConfig.ConfigChanged += ScannerConfigOnConfigChanged;

                preprocessThreadExitSignal = false;
                if (preProcessingThread == null)
                {
                    preProcessingThread = new Thread(ImageEnhanceThreadRoutine)
                    {
                        IsBackground = true,
                    };
                    preProcessingThread.Start();
                }

                #region Load Settings
                if (!ScannerConfig.Read(ConfigPath.CaptureSysBoardCount, out daqCount))
                {
                    daqCount = 1;
                }

                if (!ScannerConfig.Read(ConfigPath.MachineViewsCount, out viewCount))
                {
                    viewCount = 1;
                }
                IsDualView = viewCount == 2;

                float intTime;
                if (!ScannerConfig.Read(ConfigPath.CaptureSysLineIntegrationTime, out intTime))
                {
                    intTime = 4;
                }
                IntegrationTime = (int)(intTime * 1000);

                string daqIP;
                int daqCmdPort;
                int daqImgPort;
                if (!ScannerConfig.Read(ConfigPath.CaptureSysRemoteIP1, out daqIP))
                {
                    daqIP = "192.168.10.1";
                }
                daqIPs[0] = daqIP;

                if (ScannerConfig.Read(ConfigPath.CaptureSysRemoteCmdPort1, out daqCmdPort))
                {
                    daqCmdPort = 7171;
                }
                daqCmdPorts[0] = daqCmdPort;

                if (ScannerConfig.Read(ConfigPath.CaptureSysRemoteImagePort1, out daqImgPort))
                {
                    daqImgPort = 7474;
                }
                daqImgPorts[0] = daqImgPort;

                string cardCount;
                if (!ScannerConfig.Read(ConfigPath.CaptureSysDTView1CardsDist, out cardCount))
                {
                    cardCount = "1,0,0,0";
                }
                view1CardCount = ParseCardCount(cardCount);
                if (viewCount == 2)
                {
                    if (!ScannerConfig.Read(ConfigPath.CaptureSysRemoteIP2, out daqIP))
                    {
                        // TODO: 我不知道，我胡诌的，确认了再改
                        daqIP = "192.168.10.1";
                    }
                    daqIPs[1] = daqIP;

                    if (ScannerConfig.Read(ConfigPath.CaptureSysRemoteCmdPort2, out daqCmdPort))
                    {
                        daqCmdPort = 7171;
                    }
                    daqCmdPorts[1] = daqCmdPort;

                    if (ScannerConfig.Read(ConfigPath.CaptureSysRemoteImagePort2, out daqImgPort))
                    {
                        daqImgPort = 7474;
                    }
                    daqImgPorts[1] = daqImgPort;

                    if (!ScannerConfig.Read(ConfigPath.CaptureSysDTView2CardsDist, out cardCount))
                    {
                        cardCount = "1,0,0,0";
                    }
                    view2CardCount = ParseCardCount(cardCount);
                }

                if (!ScannerConfig.Read(ConfigPath.PreProcGroundLowAvgUpper, out background))
                {
                    background = 4000;
                }

                LoadExchangeViewsOrderFlag();
                #endregion

                tymDAQControl = new TYMDAQControl(daqCount, viewCount);

                view1LinesCache = new ConcurrentQueue<RawScanlineData>();
                view2LinesCache = new ConcurrentQueue<RawScanlineData>();
            }
            catch (Exception ex)
            {
                Tracer.TraceError("[TYMCapSys] Error occured in ctor");
                Tracer.TraceException(ex);
            }
            finally
            {
                Tracer.TraceExitFunc("UI.XRay.Flows.Services.TYMCaptureServicePart");
            }
        }
        #endregion

        #region Methods
        #region Event
        private void ScannerConfigOnConfigChanged(object sender, EventArgs e)
        {
            try
            {
                LoadExchangeViewsOrderFlag();
            }
            catch (Exception ex)
            {
                Tracer.TraceError("[TYMCapSys] Error occured in ScannerConfigOnConfigChanged");
                Tracer.TraceException(ex);
            }
        }

        private void TymDAQControlOnScanlineCaptured(DAQScanline scanline)
        {
            waitingProcessQueue.Enqueue(scanline);
        }
        #endregion

        #region Work
        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool Open()
        {
            IsOpened = tymDAQControl.Open();
            if (IsOpened)
            {
                if (exchangeViewsOrder && viewCount == 2 && daqCount == 2)
                {
                    tymDAQControl.SetDAQPara(2, IntegrationTime, view1CardCount);
                    tymDAQControl.SetDAQPara(1, IntegrationTime, view2CardCount);
                }
                else if (exchangeViewsOrder && viewCount == 2 && daqCount == 1)
                {
                    var temp = view1CardCount[1];
                    view1CardCount[1] = view1CardCount[0];
                    view1CardCount[0] = temp;
                    tymDAQControl.SetDAQPara(1, IntegrationTime, view1CardCount);
                }
                else if (daqCount == 1)
                {
                    tymDAQControl.SetDAQPara(1, IntegrationTime, view1CardCount);
                }
                else
                {
                    tymDAQControl.SetDAQPara(1, IntegrationTime, view1CardCount);
                    tymDAQControl.SetDAQPara(2, IntegrationTime, view2CardCount);
                }

                tymDAQControl.ScanlineCaptured += TymDAQControlOnScanlineCaptured;
                tymDAQControl.OnDAQError += TymDAQControl_OnDAQError;
                tymDAQControl.OnDAQException += TymDAQControl_OnDAQException;

                _deviceOpendWeakEvent.Raise(this, EventArgs.Empty);
            }
            return IsOpened;
        }

        private void TymDAQControl_OnDAQError(int instanceID, int eventId, string msg)
        {
            Tracer.TraceError($"[TYMCapSys] Instance ID: {instanceID}, Event ID: {eventId}, Details: {msg}");
        }

        private void TymDAQControl_OnDAQException(int instanceID, int eventId, Exception ex)
        {
            Tracer.TraceError($"[TYMCapSys] Instance ID: {instanceID}, Event ID: {eventId}");
            Tracer.TraceException(ex);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Close()
        {
            tymDAQControl.ScanlineCaptured -= TymDAQControlOnScanlineCaptured;

            tymDAQControl.StopCapture();
            tymDAQControl.Close();
            IsOpened = false;

            _deviceClosedWeakEvent.Raise(this, EventArgs.Empty);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool StartCapture()
        {
            return tymDAQControl.StartCapture();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void StopCapture()
        {
            preprocessThreadExitSignal = true;
            tymDAQControl.StopCapture();
            if (preProcessingThread != null)
            {
                preProcessingThread.Join();
                preProcessingThread = null;
            }
        }

        public void ClearLinesCache()
        {
            while (view1LinesCache.Count > 0)
            {
                view1LinesCache.TryDequeue(out _);
            }
            while (view2LinesCache.Count > 0)
            {
                view2LinesCache.TryDequeue(out _);
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
            if (viewLineDataOneRoundRecord >= oneRoundMaxRecordCount)
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
                        if (view1LinesCache.Count > 0 && view2LinesCache.Count > 0)
                        {
                            RawScanlineData scanline;
                            if (view1LinesCache.TryDequeue(out scanline))
                            {
                                line1 = scanline;
                            }

                            if (view2LinesCache.TryDequeue(out scanline))
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

                    List<RawScanlineData> notGroundDatas = new List<RawScanlineData>();

                    //是否丢弃过本底数据
                    bool hasDumpGroundData = false;

                    // 保护机制：防止某个视角没有数据，导致内存泄露
                    if (view1LinesCache.Count >= 65 && view2LinesCache.Count == 0)
                    {
                        RawScanlineData scanline;

                        while (view1LinesCache.TryDequeue(out scanline))
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
                            Tracer.TraceWarning("[TYMCapSys] view1LinesCache.count >= 65 && view2LinesCache.Count == 0, dump ground linedata of view1LinesCache!");
                        }

                        //剔除白线后多数据的视角数据量还是大于129线，则每4线丢一线
                        if (notGroundDatas.Count > 129)
                        {
                            Tracer.TraceWarning("[TYMCapSys] view1LinesCache.count >= 129 && view2LinesCache.Count == 0, dump linedata every 4 linedata of view1LinesCache!");

                            int deleteIndex = 1;

                            foreach (RawScanlineData t in notGroundDatas)
                            {
                                if (deleteIndex % 4 != 0)
                                {
                                    view1LinesCache.Enqueue(t);
                                }

                                deleteIndex++;
                            }
                        }
                        else
                        {
                            foreach (var rawScanlineData in notGroundDatas)
                            {
                                view1LinesCache.Enqueue(rawScanlineData);
                            }
                        }
                    }

                    //清空视角1计算中的缓存
                    notGroundDatas.Clear();
                    hasDumpGroundData = false;


                    if (view2LinesCache.Count >= 65 && view1LinesCache.Count == 0)
                    {
                        RawScanlineData scanline;
                        while (view2LinesCache.TryDequeue(out scanline))
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
                            Tracer.TraceWarning("[TYMCapSys] view2LinesCache.count >= 65 && view1LinesCache.Count == 0, dump ground linedata of view2LinesCache!");
                        }

                        //剔除白线后多数据的视角数据量还是大于129线，则每4线丢一线
                        if (notGroundDatas.Count > 129)
                        {
                            Tracer.TraceWarning("[TYMCapSys] view2LinesCache.count >= 129 && view1LinesCache.Count == 0, dump linedata every 4 linedata of view2LinesCache!");

                            int deleteIndex = 1;

                            foreach (RawScanlineData t in notGroundDatas)
                            {
                                if (deleteIndex % 4 != 0)
                                {
                                    view2LinesCache.Enqueue(t);
                                }

                                deleteIndex++;
                            }
                        }
                        else
                        {
                            foreach (var rawScanlineData in notGroundDatas)
                            {
                                view2LinesCache.Enqueue(rawScanlineData);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Tracer.TraceError("[TYMCapSys] Error occured in TryOutputDualViewData");
                    Tracer.TraceException(e);
                }
                finally
                {
                    //Monitor.Exit(this);
                }
            }
        }

        /// <summary>
        /// 加载是否交换视角1和视角2数据的标志
        /// </summary>
        private void LoadExchangeViewsOrderFlag()
        {
            if (viewCount > 1)
            {
                exchangeViewsOrder = ExchangeDirectionConfig.Service.IsExchangeDetector;

                Tracer.TraceInfo("[TYMCapSys] ExchangeViewsOrder is " + exchangeViewsOrder.ToString());
            }
        }
        #endregion

        #region Process
        private void ImageEnhanceThreadRoutine()
        {
            while (!preprocessThreadExitSignal)
            {
                if (!waitingProcessQueue.IsEmpty)
                {
                    while (waitingProcessQueue.TryDequeue(out daqScanline))
                    {
                        if (!IsDualView)
                        {
                            var line = new RawScanlineData(DetectViewIndex.View1, daqScanline.Low, daqScanline.High);

                            // 对于单视角，直接将数据输出
                            if (ScanlineCaptured != null)
                            {
                                ScanlineCaptured(this, new RawScanlineDataBundle(line, null));
                                //WatcherCaptureSys(ref _lastCaptureView1LineDataTime, ref _maxView1TwoLineDataSpan, DetectViewIndex.View1);
                            }
                        }
                        else
                        {
                            lock (viewDataLock)
                            {
                                // 对于双视角，先将两个视角的数据分别缓存到队列中，再根据两个队列的长度情况同步向外输出
                                if (daqScanline.View == 1)
                                {
                                    if (exchangeViewsOrder)
                                    {
                                        RawScanlineData data = new RawScanlineData(DetectViewIndex.View2, daqScanline.Low, daqScanline.High);
                                        data.IsGround = ViewLineDataIsGround(data);

                                        // 交换视角1和视角2的数据
                                        view2LinesCache.Enqueue(data);

                                        RecordNViewDataTime(ref view2LineDataOneRoundRecord, ref newView2RecordRoundStartTime, DetectViewIndex.View2);

                                    }
                                    else
                                    {
                                        RawScanlineData data = new RawScanlineData(DetectViewIndex.View1, daqScanline.Low, daqScanline.High);
                                        data.IsGround = ViewLineDataIsGround(data);
                                        view1LinesCache.Enqueue(data);

                                        RecordNViewDataTime(ref view1LineDataOneRoundRecord, ref newView1RecordRoundStartTime, DetectViewIndex.View1);

                                    }
                                }
                                else
                                {
                                    if (exchangeViewsOrder)
                                    {
                                        // 交换视角1和视角2的数据
                                        RawScanlineData data = new RawScanlineData(DetectViewIndex.View1, daqScanline.Low, daqScanline.High);
                                        data.IsGround = ViewLineDataIsGround(data);
                                        view1LinesCache.Enqueue(data);

                                        //WatcherCaptureSys(ref _lastCaptureView1LineDataTime, ref _maxView1TwoLineDataSpan, DetectViewIndex.View1);

                                        RecordNViewDataTime(ref view1LineDataOneRoundRecord, ref newView1RecordRoundStartTime, DetectViewIndex.View1);
                                    }
                                    else
                                    {
                                        RawScanlineData data = new RawScanlineData(DetectViewIndex.View2, daqScanline.Low, daqScanline.High);
                                        data.IsGround = ViewLineDataIsGround(data);

                                        // 交换视角1和视角2的数据
                                        view2LinesCache.Enqueue(data);

                                        //WatcherCaptureSys(ref _lastCaptureView2LineDataTime, ref _maxView2TwoLineDataSpan, DetectViewIndex.View2);

                                        RecordNViewDataTime(ref view2LineDataOneRoundRecord, ref newView2RecordRoundStartTime, DetectViewIndex.View2);
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

        private bool ViewLineDataIsGround(RawScanlineData lineData)
        {
            try
            {
                return lineData.Low.Average(i => i) < background;
            }
            catch (Exception e)
            {
                Tracer.TraceException(e, "occured when calculate view data is ground or not in DTcaptureservicePart!");
            }
            return false;
        }
        #endregion

        #region Set
        public void SetIntegrationTime(float integrationTime)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Tools
        private int[] ParseCardCount(string cardCountStr)
        {
            int[] cardCount = new int[2];
            var cardCountStrArr = cardCountStr.Split(',');
            for (int i = 0; i < 2; i++)
            {
                int.TryParse(cardCountStrArr[i], out cardCount[i]);
            }
            return cardCount;
        }
        #endregion
        #endregion
    }
}
