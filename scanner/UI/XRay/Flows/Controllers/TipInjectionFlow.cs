using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Services;
using UI.XRay.Flows.Services.Db;
using UI.XRay.RenderEngine;

namespace UI.XRay.Flows.Controllers
{
    /// <summary>
    /// Tip注入流程管理: 注入流程结束包括用户识别成功或漏判
    /// </summary>
    public class TipInjectionFlow
    {
        #region Properties
        /// <summary>
        /// Tip图像随机选择器
        /// </summary>
        private TipImageRandomSelector _imageSelector;

        /// <summary>
        /// 传送带是否停止
        /// </summary>
        private bool _isConveyorStopped;

        /// <summary>
        /// 传送带停机时间
        /// </summary>
        private DateTime _conveyorStopTime;

        /// <summary>
        /// Tip图像插入定位器
        /// </summary>
        private TipInjectPointLocator _locator;

        /// <summary>
        /// 判图计时器，包括在线判和离线判
        /// </summary>
        private System.Threading.Timer timer;

        private int _view1BeltMarginAtBegin;
        public int View1BeltMarginAtBegin
        {
            get { return _view1BeltMarginAtBegin; }
            set { _view1BeltMarginAtBegin = value; }
        }

        private int _view1BeltMarginAtEnd;
        public int View1BeltMarginAtEnd
        {
            get { return _view1BeltMarginAtEnd; }
            set { _view1BeltMarginAtEnd = value; }
        }

        private int _view2BeltMarginAtBegin;
        public int View2BeltMarginAtBegin
        {
            get { return _view2BeltMarginAtBegin; }
            set { _view2BeltMarginAtBegin = value; }
        }

        private int _view2BeltMarginAtEnd;
        public int View2BeltMarginAtEnd
        {
            get { return _view2BeltMarginAtEnd; }
            set { _view2BeltMarginAtEnd = value; }
        }

        private int _bagthreshold;
        public int Bagthreshold
        {
            get { return _bagthreshold; }
            set { _bagthreshold = value; }
        }

        private int _backgroundthreshold;
        public int Backgroundthreshold
        {
            get { return _backgroundthreshold; }
            set { _backgroundthreshold = value; }
        }

        /// <summary>
        /// 当前登录用户的Id
        /// </summary>
        private string CurrentAccountId
        {
            get { return LoginAccountManager.Service.HasLogin ? LoginAccountManager.Service.CurrentAccount.AccountId : null; }
        }

        /// <summary>
        /// 注入次数编号：递增，从0开始计数。用于在超时判断时，甄别超时针对的编号
        /// </summary>
        private int _currentInjectionNumber;

        /// <summary>
        /// 注入流程任务列表
        /// </summary>
        private List<TipInjectEntity> _tipInjectEntityList;

        private TipInjectEntity _currenttipInjectEntity;

        private int _showingLinesMinNumber;

        /// <summary>
        /// 事件：Tip图像注入完毕
        /// </summary>
        public event Action<TipInjectionEventArgs> TipImageInjected;

        /// <summary>
        /// 事件：Tip漏判
        /// </summary>
        public event Action<TipInjectionEventArgs> TipMissed;

        /// <summary>
        /// 事件：Tip识别成功
        /// </summary>
        public event Action<TipInjectionEventArgs> TipIdentified;

        public bool IsTipInjecting { get; private set; }
        public bool HasTipInjectEntity { get; private set; }

        // 单视角设备Tip注入时是否使用视角1，true：使用视角1数据，false：使用视角2数据
        private bool _isUseTipImage1;
        #endregion

        public TipInjectionFlow()
        {
            LoadSettings();
            _tipInjectEntityList = new List<TipInjectEntity>();
            _imageSelector = new TipImageRandomSelector();
            timer = new System.Threading.Timer(new System.Threading.TimerCallback(Timer_Identify), null, 10, System.Threading.Timeout.Infinite);
        }

        private void LoadSettings()
        {
            try
            {
                if (!ScannerConfig.Read(ConfigPath.IsUseTipImage1, out _isUseTipImage1))
                {
                    _isUseTipImage1 = false;
                }

                if (!ScannerConfig.Read(ConfigPath.MachineView1BeltEdgeAtBegin, out _view1BeltMarginAtBegin))
                {
                    _view1BeltMarginAtBegin = 32;
                }

                if (!ScannerConfig.Read(ConfigPath.MachineView1BeltEdgeAtEnd, out _view1BeltMarginAtEnd))
                {
                    _view1BeltMarginAtEnd = 32;
                }
                if (!ScannerConfig.Read(ConfigPath.MachineView2BeltEdgeAtBegin, out _view2BeltMarginAtBegin))
                {
                    _view1BeltMarginAtBegin = 32;
                }

                if (!ScannerConfig.Read(ConfigPath.MachineView2BeltEdgeAtEnd, out _view2BeltMarginAtEnd))
                {
                    _view1BeltMarginAtEnd = 32;
                }

                if (!ScannerConfig.Read(ConfigPath.PreProcBkgThreshold, out _backgroundthreshold))
                {
                    _backgroundthreshold = 61000;
                }

                if (!ScannerConfig.Read(ConfigPath.PreProcBgbThreshold, out _bagthreshold))
                {
                    _bagthreshold = 52500;
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        private void Timer_Identify(object sender)
        {
            if (_tipInjectEntityList.Count > 0)
            {
                var rm = from tipInjectEntity in _tipInjectEntityList
                         where tipInjectEntity.HasIdentified == true
                         select tipInjectEntity;
                var temp = rm.ToList();
                //移除 hasIdntified 的tipInjectEntity
                for (int i = 0; i < temp.Count(); i++)
                {
                    _tipInjectEntityList.Remove(temp[i]);
                }
                for (int i = 0; i < _tipInjectEntityList.Count(); i++)
                {
                    // Tip注入已经完成,尚未被识别，且已经不在当前屏幕显示范围内，即认定为用户错过tip
                    // Tip注入已经完成,尚未被识别，且停机后超过停机判图时间，即认定为用户错过tip
                    var _tipInjectEntity = _tipInjectEntityList[i];
                    if (_tipInjectEntity.InjectionStep == TipInjectionStep.Injected && !_tipInjectEntity.HasIdentified && (_tipInjectEntity.Injector.EndLineNumber.Value < _showingLinesMinNumber ||
                        (_isConveyorStopped && DateTime.Now - _tipInjectEntity.InjectionTime > TimeSpan.FromSeconds(_tipInjectEntity.Tipplan.OfflineRecMaxSeconds))))
                    {
                        if (TipMissed != null)
                            TipMissed(new TipInjectionEventArgs(_tipInjectEntity.TipImageselection.TipImage,
                                _tipInjectEntity.Injector.GetInjectRegion(), _tipInjectEntity.Injector.GetInjectRegion2()));
                        SaveInjectionRecordAsync(_tipInjectEntity);

                        // 移除错过的tip
                        _tipInjectEntityList.Remove(_tipInjectEntity);
                        i -= 1;
                    }
                }
            }
            timer.Change(200, System.Threading.Timeout.Infinite);
        }

        /// <summary>
        /// 用户停机
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void OnConveyorStopped()
        {
            _conveyorStopTime = DateTime.Now;
            _isConveyorStopped = true;
            if (_tipInjectEntityList.Where(p => p.InjectionStep == TipInjectionStep.Injected).Count() > 0)
                HasTipInjectEntity = true;
            else
                HasTipInjectEntity = false;
        }

        /// <summary>
        /// 用户按下标记键，标记Tip
        /// </summary>
        //[MethodImpl(MethodImplOptions.Synchronized)]
        //public bool IdentifyTip()
        //{
        //    bool _hasidentifyTip = false;
        //    List<TipInjectEntity> _temptipInjectEntityList = new List<TipInjectEntity>();
        //    for (int i = 0; i < tipInjectEntityList.Count(); i++)
        //    {
        //        var _tipInjectEntity = tipInjectEntityList[i];
        //        if (_tipInjectEntity.InjectionStep == TipInjectionStep.Injected && !_tipInjectEntity.HasIdentified)
        //        {
        //            _tipInjectEntity.HasIdentified = true;
        //            _tipInjectEntity.RecognizedTime = DateTime.Now;
        //            if (TipIdentified != null)
        //                TipIdentified(new TipInjectionEventArgs(_tipInjectEntity.TipImageselection.TipImage,
        //                    _tipInjectEntity.Injector.GetInjectRegion(), _tipInjectEntity.Injector.GetInjectRegion2()));
        //            _hasidentifyTip = true;
        //            SaveInjectionRecordAsync(_tipInjectEntity);
        //        }
        //        else
        //            _temptipInjectEntityList.Add(_tipInjectEntity);
        //    }
        //    tipInjectEntityList = _temptipInjectEntityList;
        //    _temptipInjectEntityList = null;
        //    return _hasidentifyTip;
        //}

        /// <summary>
        /// 用户画框识别Tip
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool MarkIdentifyTip(List<MarkerRegionEx> RectangleLists)
        {
            bool _hasidentifyTip = false;
            List<TipInjectEntity> _temptipInjectEntityList = new List<TipInjectEntity>();
            for (int i = 0; i < _tipInjectEntityList.Count(); i++)
            {
                var _tipInjectEntity = _tipInjectEntityList[i];
                if (_tipInjectEntity.InjectionStep == TipInjectionStep.Injected && !_tipInjectEntity.HasIdentified && _isConveyorStopped)
                {
                    foreach (var rectangleList in RectangleLists)
                    {
                        if (rectangleList.View == RenderViewIndex.View1)
                        {
                            _hasidentifyTip = IouCalculate(_tipInjectEntity, rectangleList, _tipInjectEntity.Injector.StartChannel, _tipInjectEntity.Injector.EndChannel);
                            if (_hasidentifyTip)
                                break;
                        }
                        else
                        {
                            _hasidentifyTip = IouCalculate(_tipInjectEntity, rectangleList, _tipInjectEntity.Injector.StartChannel2, _tipInjectEntity.Injector.EndChannel2);
                            if (_hasidentifyTip)
                                break;
                        }

                    }
                    if (!_tipInjectEntity.HasIdentified)
                    {
                        if (TipMissed != null)
                            TipMissed(new TipInjectionEventArgs(_tipInjectEntity.TipImageselection.TipImage,
                                _tipInjectEntity.Injector.GetInjectRegion(), _tipInjectEntity.Injector.GetInjectRegion2()));
                        SaveInjectionRecordAsync(_tipInjectEntity);
                    }
                }
                else
                    _temptipInjectEntityList.Add(_tipInjectEntity);
            }
            _tipInjectEntityList = _temptipInjectEntityList;
            _temptipInjectEntityList = null;
            return _hasidentifyTip;
        }

        private bool IouCalculate(TipInjectEntity tipInjectEntity, MarkerRegionEx rectangleList, int startChannel, int endChannel)
        {
            int minlinenum = Math.Max(tipInjectEntity.Injector.StartLineNumber, rectangleList.FromLine);
            int minchannel = Math.Max(startChannel, rectangleList.FromChannel);

            int maxlinenum = Math.Min(tipInjectEntity.Injector.EndLineNumber.Value, rectangleList.ToLine);
            int maxchannel = Math.Min(endChannel, rectangleList.ToChannel);

            if (maxchannel > minchannel && maxlinenum > minlinenum)
            {
                var inter_area = (maxlinenum - minlinenum) * (maxchannel - minchannel);
                var s1 = (tipInjectEntity.Injector.EndLineNumber.Value - tipInjectEntity.Injector.StartLineNumber) * (endChannel - startChannel);
                var s2 = rectangleList.Height * rectangleList.Width;
                float iou = (float)inter_area / (s1 + s2 - inter_area);
                if (iou > 0.4)
                {
                    tipInjectEntity.HasIdentified = true;
                    tipInjectEntity.RecognizedTime = DateTime.Now;
                    if (TipIdentified != null)
                        TipIdentified(new TipInjectionEventArgs(tipInjectEntity.TipImageselection.TipImage,
                            tipInjectEntity.Injector.GetInjectRegion(), tipInjectEntity.Injector.GetInjectRegion2()));
                    rectangleList.ManualTag = true;
                    SaveInjectionRecordAsync(tipInjectEntity);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 加载一张用于注入的Tip图像：每次触发光障或者加载新的XRAY图像时调用该函数
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void TryLoadNextTipImageAsync()
        {
            Task.Run(() =>
            {
                TipImageSelection selection = _imageSelector.RandomGetImageSelection();
                TipPlan plan = _imageSelector.CurrentPlan;
                if (selection != null)
                {
                    lock (this)
                    {
                        _currenttipInjectEntity = new TipInjectEntity(selection, plan);
                        _currenttipInjectEntity.InjectionStep = TipInjectionStep.Preparing;
                        _tipInjectEntityList.Add(_currenttipInjectEntity);
                    }
                }
            });
        }

        public void ClearTipSelected()
        {
            if (_currenttipInjectEntity != null)
            {
                lock (this)
                {
                    _currenttipInjectEntity = null;
                }
            }
        }

        /// <summary>
        /// 尝试将Tip数据插入到当前扫描线中。
        /// </summary>
        /// <param name="scanline"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void TryInsertToScanLine(DisplayScanlineDataBundle scanline, int showingLinesMinNumber)
        {
            _showingLinesMinNumber = showingLinesMinNumber;
            if (_currenttipInjectEntity != null && _currenttipInjectEntity.InjectionStep == TipInjectionStep.Preparing)
            {
                if (_locator == null)
                {
                    _locator = new TipInjectPointLocator(_bagthreshold, _view1BeltMarginAtBegin, _view1BeltMarginAtEnd, _view2BeltMarginAtBegin, _view2BeltMarginAtEnd,_isUseTipImage1);
                    _locator.SetCurrentTipImage(_currenttipInjectEntity.TipImageselection.TipImage);
                }
                int startChannel = 0;
                int startChannel2 = 0;
                int startLineNumber = 0;
                _isConveyorStopped = false;
                if (_locator.FindInsertLocation(scanline, out startChannel, out startChannel2, out startLineNumber))
                {
                    if (_currenttipInjectEntity.Injector == null)
                        _currenttipInjectEntity.Injector = new TipImageInjector(scanline, _currenttipInjectEntity.TipImageselection.TipImage, _backgroundthreshold, startChannel, startChannel2, startLineNumber,_isUseTipImage1);
                    _currenttipInjectEntity.InjectionStep = TipInjectionStep.Injecting;
                    IsTipInjecting = true;
                    _currentInjectionNumber++;
                    _locator = null;
                }
                return;
            }

            if (_currenttipInjectEntity != null && _currenttipInjectEntity.InjectionStep == TipInjectionStep.Injecting)
            {
                if (_currenttipInjectEntity.Injector != null)
                {
                    _currenttipInjectEntity.Injector.Inject(scanline);
                    if (_currenttipInjectEntity.Injector.InjectionFinished)
                    {
                        if (TipImageInjected != null)
                            TipImageInjected(new TipInjectionEventArgs(_currenttipInjectEntity.TipImageselection.TipImage,
                                _currenttipInjectEntity.Injector.GetInjectRegion(), _currenttipInjectEntity.Injector.GetInjectRegion2()));
                        _currenttipInjectEntity.InjectionStep = TipInjectionStep.Injected;
                        IsTipInjecting = false;
                        _currenttipInjectEntity.InjectionTime = DateTime.Now;
                        _currenttipInjectEntity = null;
                    }
                }
            }
        }

        /// <summary>
        /// 异步存储当前的Tip注入记录
        /// </summary>
        private void SaveInjectionRecordAsync(TipInjectEntity tipInjectEntity)
        {
            if (tipInjectEntity.TipImageselection.TipImage != null)
            {
                var record = new TipEventRecord()
                {
                    InjectionTime = tipInjectEntity.InjectionTime,
                    RecognizedTime = tipInjectEntity.RecognizedTime,
                    IsConveyorStopped = _isConveyorStopped,
                    EvaluatedAccountId = CurrentAccountId,
                    FileName = tipInjectEntity.TipImageselection.FileName,
                    Library = tipInjectEntity.TipImageselection.Library
                };

                Task.Run(() =>
                {
                    try
                    {
                        var tipManager = new TipEventRecordDbSet();
                        tipManager.Add(record);
                    }
                    catch (Exception e)
                    {
                        Tracer.TraceException(e, "Exception when try to save tip event record.");
                    }
                });
            }
        }

        /// <summary>
        /// 程序退出清理资源
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Cleanup()
        {
            // 在退出前，如果仍有注入流程未结束，则先结束
            for (int i = 0; i < _tipInjectEntityList.Count(); i++)
            {
                var _tipInjectEntity = _tipInjectEntityList[i];
                if (_tipInjectEntity.InjectionStep == TipInjectionStep.Injected && !_tipInjectEntity.HasIdentified)
                {
                    if (TipMissed != null)
                        TipMissed(new TipInjectionEventArgs(_tipInjectEntity.TipImageselection.TipImage,
                            _tipInjectEntity.Injector.GetInjectRegion(), _tipInjectEntity.Injector.GetInjectRegion2()));
                    SaveInjectionRecordAsync(_tipInjectEntity);
                }
                _tipInjectEntityList.Remove(_tipInjectEntity);
            }
            timer.Dispose();
        }

        /// <summary>
        /// Tip图像注入：将一个指定的Tip图像，注入到扫描的图像数据中的指定位置
        /// </summary>
        class TipImageInjector
        {
            /// <summary>
            /// 当前准备用于插入的Tip图像
            /// </summary>
            private XRayScanlinesImage TipImage { get; set; }
            /// <summary>
            /// 用于判断背景点的阈值
            /// </summary>
            private int _backgroundThreshold;

            /// <summary>
            /// 注入的起始探测通道位置
            /// </summary>
            public int StartChannel { get; private set; }

            /// <summary>
            /// 注入的结束探测通道位置
            /// </summary>
            public int EndChannel { get; private set; }

            /// <summary>
            /// 注入的起始探测通道位置
            /// </summary>
            public int StartChannel2 { get; private set; }

            /// <summary>
            /// 注入的结束探测通道位置
            /// </summary>
            public int EndChannel2 { get; private set; }

            /// <summary>
            /// 画框的起始通道号
            /// </summary>
            public int RectStartChannel { get; private set; }

            /// <summary>
            /// 画框的结束通道号
            /// </summary>
            public int RectEndChannel { get; private set; }

            /// <summary>
            /// 画框的起始通道号
            /// </summary>
            public int RectStartChannel2 { get; private set; }

            /// <summary>
            /// 画框的结束通道号
            /// </summary>
            public int RectEndChannel2 { get; private set; }

            /// <summary>
            /// 注入的起始线号
            /// </summary>
            public int StartLineNumber { get; private set; }

            /// <summary>
            /// 注入的结束线号。如果注入未开始，则为空
            /// </summary>
            public int? EndLineNumber { get; private set; }

            /// <summary>
            /// 当前Tip图像是否已经全部注入完成
            /// </summary>
            public bool InjectionFinished { get; private set; }

            /// <summary>
            /// 获取当前侧视角Tip注入的区域
            /// 如果尚未开始注入数据，则返回为空
            /// </summary>
            /// <returns>返回当前侧视角的Tip注入区域</returns>
            public MarkerRegion GetInjectRegion()
            {
                if (EndLineNumber != null)
                {
                    return new MarkerRegion(MarkerRegionType.Tip,
                    StartLineNumber, EndLineNumber.Value,
                    RectStartChannel, RectEndChannel);
                }
                return null;
            }
            /// <summary>
            /// 获取当前主视角Tip注入的区域
            /// 如果尚未开始注入数据，则返回为空
            /// </summary>
            /// <returns>返回当前主视角的Tip注入区域</returns>
            public MarkerRegion GetInjectRegion2()
            {
                if (EndLineNumber != null)
                {
                    return new MarkerRegion(MarkerRegionType.Tip,
                    StartLineNumber, EndLineNumber.Value,
                    RectStartChannel2, RectEndChannel2);
                }
                return null;
            }

            private bool _isUseTipImage1;

            public TipImageInjector(DisplayScanlineDataBundle scanline, XRayScanlinesImage image, int backgroundThreshold, int startChannel, int startChannel2, int startLineNumber,bool isUseTipImage1)
            {
                TipImage = image;
                _backgroundThreshold = backgroundThreshold;
                
                if (scanline.View2Data != null)
                {
                    var TipImagemargin = image.View1Data.ChannelsCount;
                    StartChannel = startChannel;
                    EndChannel = StartChannel + TipImagemargin;
                    RectStartChannel = StartChannel;
                    RectEndChannel = EndChannel;
                    var TipImagemargin2 = image.View2Data.ChannelsCount;
                    StartChannel2 = startChannel2;
                    EndChannel2 = StartChannel2 + TipImagemargin2;
                    RectStartChannel2 = StartChannel2;
                    RectEndChannel2 = EndChannel2;
                }
                else
                {
                    if (_isUseTipImage1)
                    {
                        var TipImagemargin = image.View1Data.ChannelsCount;
                        StartChannel = startChannel;
                        EndChannel = StartChannel + TipImagemargin;
                        RectStartChannel = StartChannel;
                        RectEndChannel = EndChannel;
                    }
                    else
                    {
                        var TipImagemargin = image.View2Data.ChannelsCount;
                        StartChannel = startChannel;
                        EndChannel = StartChannel + TipImagemargin;
                        RectStartChannel = StartChannel;
                        RectEndChannel = EndChannel;
                    }
                }
                //var TipImagemargin = image.View1Data.ChannelsCount;
                //StartChannel = startChannel;
                //EndChannel = StartChannel + TipImagemargin;
                //RectStartChannel = StartChannel;
                //RectEndChannel = EndChannel;
                //if (scanline.View2Data != null)
                //{
                //    var TipImagemargin2 = image.View2Data.ChannelsCount;
                //    StartChannel2 = startChannel2;
                //    EndChannel2 = StartChannel2 + TipImagemargin2;
                //    RectStartChannel2 = StartChannel2;
                //    RectEndChannel2 = EndChannel2;
                //}
                StartLineNumber = startLineNumber;
                EndLineNumber = null;
                _isUseTipImage1 = isUseTipImage1;
            }

            /// <summary>
            /// 将当前Tip图像注入到实时扫描的数据中
            /// </summary>
            /// <param name="scanline"></param>           
            public void Inject(DisplayScanlineDataBundle scanline)
            {
                var lineNumber = scanline.LineNumber;
                if (lineNumber < StartLineNumber)
                    return;
                //Tracer.TraceInfo(string.Format("[Tip] -> currentLine:{0}, StartLine{1}, StartChnl1:{2}, StartChnl2:{3}, EndLine{4}, EndChnl1:{5}, EndChnl2:{6}",
                //    lineNumber, StartLineNumber, StartChannel, StartChannel2, EndLineNumber, EndChannel, EndChannel2));
                if (scanline.View2Data != null)
                {// 双视角设备
                    datafuse(scanline.View1Data, TipImage.View1Data, lineNumber, StartChannel);
                    datafuse(scanline.View2Data, TipImage.View2Data, lineNumber, StartChannel2);
                }
                else
                {// 单视角设备
                    if (_isUseTipImage1)
                    {// 使用Tip图像的视角1数据
                        datafuse(scanline.View1Data, TipImage.View1Data, lineNumber, StartChannel);
                    }
                    else
                    {// 使用Tip图像的视角2数据
                        datafuse(scanline.View1Data, TipImage.View2Data, lineNumber, StartChannel);
                    }
                }
                //datafuse(scanline.View1Data, TipImage.View1Data, lineNumber, StartChannel);
                //EndLineNumber = lineNumber;
                //if (scanline.View2Data != null)
                //    datafuse(scanline.View2Data, TipImage.View2Data, lineNumber, StartChannel2);
                EndLineNumber = lineNumber;
                if (lineNumber >= StartLineNumber + TipImage.View1Data.ScanLines.Length)
                    InjectionFinished = true;
            }

            private void datafuse(DisplayScanlineData viewData, ImageViewData tipviewData, int lineNumber, int startChannel)
            {
                var tipLines = tipviewData.ScanLines;
                if (lineNumber >= StartLineNumber && lineNumber < StartLineNumber + tipLines.Length)
                {
                    ushort[] sourceLineData = viewData.XRayData;
                    ushort[] sourceLineDataEnhanced = viewData.XRayDataEnhanced;
                    var materialData = viewData.Material;

                    var tipLine = tipLines[lineNumber - StartLineNumber];
                    ushort[] tipLineData = tipLine.XRayData;
                    ushort[] tipLineMaterial = tipLine.Material;

                    // 包裹图像数据 = 包裹图像数据 * tip 数据 / 满度
                    for (int i = 1; i < tipLine.XRayData.Length - 1; i++)
                    {
                        sourceLineData[startChannel + i] = (ushort)((sourceLineData[startChannel + i] * tipLineData[i]) >> 16);
                        sourceLineDataEnhanced[startChannel + i] = (ushort)((sourceLineDataEnhanced[startChannel + i] * tipLineData[i]) >> 16);


                        //物质分类数据暂时用Tip的物质分类，为去除Tip白色部分的影响，设定一个阈值，当Tip图像某点处数据值小于阈值，
                        //用Tip的物质分类代替融合后图像的物质分类
                        if (tipLineData[i] < _backgroundThreshold)
                            materialData[startChannel + i] = tipLineMaterial[i];
                    }
                    //重新生成颜色索引
                    ushort[] newColorIndex;
                    MatColorMappingService.Service.Map(viewData.Material, out newColorIndex);
                    newColorIndex.CopyTo(viewData.ColorIndex, 0);
                }
            }
        }

        /// <summary>
        /// Tip图像插入点定位器：根据当前的tip图像大小以及扫描线数据中物体的位置，找出可以插入Tip的位置
        /// 此类的实例仅用于TipInjectionFlow中，不是多线程安全的
        /// </summary>
        class TipInjectPointLocator
        {
            private int _view1BeltMarginAtBegin;
            private int _view1BeltMarginAtEnd;
            private int _view2BeltMarginAtBegin;
            private int _view2BeltMarginAtEnd;
            private int _bagthreshold;
            /// <summary>
            /// 随机确定起始插入的线编号
            /// </summary>
            private Random _random = new Random();

            /// <summary>
            /// 当前准备插入的Tip图像
            /// </summary>
            private XRayScanlinesImage _tipImage { get; set; }
            private bool _isUseTipImage1;

            /// <summary>
            /// 更新当前用于注入的Tip图像
            /// </summary>
            public void SetCurrentTipImage(XRayScanlinesImage tipImage)
            {
                _tipImage = tipImage;
            }

            public TipInjectPointLocator(int bagthreshold, int view1BeltMarginAtBegin, int view1BeltMarginAtEnd, int view2BeltMarginAtBegin, int view2BeltMarginAtEnd, bool isUseTipImage1)
            {
                _bagthreshold = bagthreshold;
                _view1BeltMarginAtBegin = view1BeltMarginAtBegin;
                _view1BeltMarginAtEnd = view1BeltMarginAtEnd;
                _view2BeltMarginAtBegin = view2BeltMarginAtBegin;
                _view2BeltMarginAtEnd = view2BeltMarginAtEnd;
                _isUseTipImage1 = isUseTipImage1;
            }

            /// <summary>
            /// 根据当前扫描线数据，计算Tip图像插入的起始通道号
            /// </summary>
            /// <param name="scanline">当前扫描线数据</param>
            /// <param name="startChannelIndex1">返回视角1（侧视角）Tip图像插入起始通道号</param>
            /// <param name="startChannelIndex2">返回视角2（主视角）Tip图像插入起始通道号</param>
            /// <returns>如果从当前线中找到足够的插入空间，则返回true，且输出可插入的起始通道号；否则返回false</returns>
            public bool FindInsertLocation(DisplayScanlineDataBundle scanline, out int startChannelIndex1, out int startChannelIndex2, out int startLineNumber)
            {
                startChannelIndex1 = -1;
                startChannelIndex2 = -1;
                startLineNumber = scanline.LineNumber + _random.Next(30, 100);
                if (_tipImage == null)
                    return false;

                if (scanline.View2Data != null)
                {// 双视角设备
                    bool margin1 = boundarysearch(scanline.View1Data, _tipImage.View1Data, _view1BeltMarginAtBegin, _view1BeltMarginAtEnd, out startChannelIndex1);
                    bool margin2 = boundarysearch(scanline.View2Data, _tipImage.View2Data, _view2BeltMarginAtBegin, _view2BeltMarginAtEnd, out startChannelIndex2);
                    if (margin1 && margin2)
                        return true;
                }
                else
                {// 单视角设备
                    bool margin1 = false;
                    if (_isUseTipImage1)
                    {
                        margin1 = boundarysearch(scanline.View1Data, _tipImage.View1Data, _view1BeltMarginAtBegin, _view1BeltMarginAtEnd, out startChannelIndex1);
                    }
                    else
                    {
                        margin1 = boundarysearch(scanline.View1Data, _tipImage.View2Data, _view1BeltMarginAtBegin, _view1BeltMarginAtEnd, out startChannelIndex1);
                    }
                    if (margin1)
                    {
                        return true;
                    }
                }
                return false;

                //bool margin1 = boundarysearch(scanline.View1Data, _tipImage.View1Data, _view1BeltMarginAtBegin, _view1BeltMarginAtEnd, out startChannelIndex1);

                //// 视角View2：双视角时为主视角
                //if (scanline.View2Data != null)
                //{
                //    bool margin2 = boundarysearch(scanline.View2Data, _tipImage.View2Data, _view2BeltMarginAtBegin, _view2BeltMarginAtEnd, out startChannelIndex2);
                //    if (margin1 && margin2)
                //        return true;
                //}
                //else
                //{
                //    if (margin1)
                //        return true;
                //}
                //return false;
            }

            private bool boundarysearch(DisplayScanlineData viewdata, ImageViewData Tipviewdata, int BeltMarginAtBegin, int BeltMarginAtEnd, out int startChannelIndex)
            {
                startChannelIndex = -1;
                var bagEndChannel = -1;
                var tipChannelsCount = Tipviewdata.ScanLineLength;
                for (int j = BeltMarginAtBegin; j < viewdata.XRayData.Length - BeltMarginAtEnd - 5; j += 7)
                {
                    if (viewdata.XRayData[j] < _bagthreshold && viewdata.XRayData[j + 3] < _bagthreshold && viewdata.XRayData[j + 6] < _bagthreshold)
                    {
                        if (startChannelIndex == -1)
                        {
                            startChannelIndex = j;
                            bagEndChannel = j;
                        }
                        else
                            bagEndChannel = j + 6;
                    }
                }
                var margin = bagEndChannel - startChannelIndex + 1 - tipChannelsCount;
                if (margin > 0)
                {
                    startChannelIndex += (margin >> 1);
                    return true;
                }
                else
                    return false;
            }
        }

        class TipInjectEntity
        {
            /// <summary>
            /// 当前注入状态：Preparing, Injecting, Injected 
            /// </summary>
            public TipInjectionStep InjectionStep { set; get; }

            /// <summary>
            /// Tip图像注入器
            /// </summary>
            public TipImageInjector Injector { set; get; }

            /// <summary>
            /// 最近一次Tip注入完成的时间
            /// </summary>
            public DateTime InjectionTime { set; get; }

            /// <summary>
            /// 最近一次识别出tip的时刻
            /// </summary>
            public DateTime? RecognizedTime { set; get; }

            /// <summary>
            /// 是否识别出Tip
            /// </summary>
            public bool HasIdentified { set; get; }

            /// <summary>
            /// 当前运行的计划
            /// </summary>
            public TipPlan Tipplan { set; get; }

            /// <summary>
            /// 当前选择的Tip图像
            /// </summary>
            public TipImageSelection TipImageselection { set; get; }

            public TipInjectEntity(TipImageSelection tipImageSelection, TipPlan tipPlan)
            {
                TipImageselection = tipImageSelection;
                Tipplan = tipPlan;
            }
        }
    }
}