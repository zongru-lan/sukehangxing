//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 
// 
// Description: ScanningScanLinesPool.cs定义了ScanningScanLinesPool类，该类实现了抽象类NavigatableScanLinesPool，用于表示扫描期间的
// 图像数据缓冲池，用于图像导航期间的回拉、前拉
// 
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Services;
using UI.XRay.Flows.Services.Db;
using UI.XRay.Flows.HttpServices;
namespace UI.XRay.Flows.Controllers
{
    /// <summary>
    /// 表示一系列在时间轴上存在先后顺序的图像,其中包括当前屏幕正在显示的ScanLines
    /// </summary>
    class ScanningScanLinesPool : OrderedImageDataPool
    {
        private ImageAsyncStoreController _imageAsyncStoreController;

        /// <summary>
        /// 当前已经存储完毕的图像，可从中按顺序导航查看历史图像
        /// </summary>
        private LinkedList<NavigatableScanLinesCache> RecentImagesCache { get; set; }
        private LinkedList<NavigatableScanLinesCache> _pullBeforeImagesCache;

        /// <summary>
        /// pool中将能够cache的未存储图像列数上限。如果超过此值，将会逐步丢弃数据
        /// </summary>
        private const int UnsavedLinesThreashold = 4000;

        /// <summary>
        /// 数据同步锁。此类的实例可能被gui线程、数据处理线程等调用，需要对内部数据进行同步
        /// </summary>
        private readonly object _sync = new object();

        private XRayImageProcessor _imageProcessor;

        /// <summary>
        /// 尚未存储完毕的ScanLines，编号从小到大
        /// </summary>
        public LinkedList<DisplayScanlineDataBundle> UnsavedScanLines
        {
            get { return RecentImagesCache.Last.Value.ScanLines; }
        }

        /// <summary>
        /// 事件：从图像中加载了一个Tip注入，其中的位置信息为全局位置信息
        /// </summary>
        public event Action<MarkerRegion> TipInjectionLoaded;

        /// <summary>
        /// 显示图像中的框信息
        /// </summary>
        public event Action<KeyValuePair<DetectViewIndex, List<MarkerRegion>>> DrawRectAction;

        /// <summary>
        /// 当前最新注入的Tip图像。在保存新图像时，将与新图像数据一同保存。
        /// 在回拉查看旧数据时，如果加载的旧数据中有tip，需要与此新注入的tip进行对比：如果旧数据中的tip
        /// 刚好是此新注入的tip图像，则不主动显示此tip标记框（因为用户有可能尚未识别完成，因此其显示最终由tip注入流程完成）
        /// </summary>
        private XRayScanlinesImage _currentTipImage;

        /// <summary>
        /// 当前最新注入的Tip图像在全局数据区中的位置信息。
        /// </summary>
        private MarkerRegion _currentTipRegion;

        /// <summary>
        /// 保存两个视角所有探测区域，用于后续图片输出时的报警或者标记
        /// </summary>
        private XrayCadRegions _currentCadRegions;

        /// <summary>
        /// 将xrayimage图像转换为jpg等格式保存的一个扩展，主要用于和华泰诺安的合作
        /// </summary>
        private HtnovaImageSaveController _htnovaImageSaveController;


        private readonly bool _enableAutoStoreUpfImage = false;
        /// <summary>
        /// 自动保存jpg等通用格式图像
        /// </summary>
        private readonly AutoStoreUpfImage _autoStoreUpfImage;

        /// <summary>
        /// 当前注入的tip区域是否可见
        /// </summary>
        private bool _currentTipVisible;

        /// <summary>
        /// 设备型号
        /// </summary>
        public string _machineType;

        /// <summary>
        /// 设备编号
        /// </summary>
        private string _machineNumber;

        /// <summary>
        /// 是否启用htnova合作项目：主要是将危险品信息保存到指定目录的xml文件中，将图像转换为jpg保存到指定目录
        /// </summary>
        private readonly bool _htnovaEnabled = false;

        private int _bagMinLinesCount;

        /// <summary>
        /// 存储高低能原图
        /// </summary>
        private bool _hasHighAndLow;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="screenMaxLines">屏幕能够显示的扫描线的最大数量</param>
        public ScanningScanLinesPool(int screenMaxLines)
            :base(screenMaxLines)
        {
            RecentImagesCache = new LinkedList<NavigatableScanLinesCache>();
            _pullBeforeImagesCache = new LinkedList<NavigatableScanLinesCache>();
            // 最后一个节点为尚未存储完毕的图像数据缓存
            RecentImagesCache.AddLast(new MemoryScanLinesCache());

            _imageProcessor = new XRayImageProcessor();

            ScannerConfig.Read(ConfigPath.SystemModel, out _machineType);
            ScannerConfig.Read(ConfigPath.SystemMachineNum, out _machineNumber);

            _currentCadRegions = new XrayCadRegions();

            ScannerConfig.Read(ConfigPath.EnableHtnova, out _htnovaEnabled);
            if (_htnovaEnabled)
            {
                _htnovaImageSaveController = new HtnovaImageSaveController();
            }

            ScannerConfig.Read(ConfigPath.AutoStoreUpfImage, out _enableAutoStoreUpfImage);
            if (_enableAutoStoreUpfImage)
            {
                _autoStoreUpfImage = new AutoStoreUpfImage();
            }

            if (!ScannerConfig.Read(ConfigPath.BagMinLinesCount, out _bagMinLinesCount))
            {
                _bagMinLinesCount = 16;
            }

            if (!ScannerConfig.Read(ConfigPath.SaveHighLowInXRay, out _hasHighAndLow))
            {
                _hasHighAndLow = false;
            }

            //开启异步保存图像功能
            _imageAsyncStoreController = new ImageAsyncStoreController();
        }

        public void ClearRecentImageCache()
        {
            while (RecentImagesCache.Count > 1)
            {
                RecentImagesCache.RemoveFirst();
            }
            ShowingMinNumber = 0;
            ShowingMaxNumber =0;
            _minNumber = 0;
            RecentImagesCache.First.Value.ScanLines.Clear();
        }

        /// <summary>
        /// 获取所有当前正在显示中的图像的记录
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public IEnumerable<ImageRecord> GetShowingImages()
        {
            var resultList = new List<ImageRecord>();

            if (RecentImagesCache.Count > 0)
            {
                resultList.AddRange(from image in RecentImagesCache where image.Record != null select image.Record);
            }

            return resultList;
        }

        /// <summary>
        /// 保存最新注入的Tip信息。
        /// </summary>
        /// <param name="tipImage"></param>
        /// <param name="tipRegion"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SaveCurrentTipImage(XRayScanlinesImage tipImage, MarkerRegion tipRegion)
        {
            _currentTipImage = tipImage;
            _currentTipRegion = tipRegion;

            // 首次保存时，tip刚插入完毕，尚不可见
            _currentTipVisible = false;
        }

        /// <summary>
        /// 将当前注入的tip图像区域设置为可见状态
        /// 当用户识别或错过tip时，tip区域变为可见
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SetCurrentTipImageVisible()
        {
            _currentTipVisible = true;
        }

        /// <summary>
        /// 清空回拉缓存(指存储历史图像记录的图像记录缓存)
        /// 本清空缓存函数在拉动方向改变、按下电机控制按键清空回拉图像并显示当前最后扫描图像时调用
        /// </summary>
        public void ClearPullBackCache()
        {
            _pullbackCache = null;
            _lastPullBack = 0;
        }
        
        private LinkedList<ImageRecord> _pullbackCache;

        /// <summary>
        /// 上一次回拉的方向
        /// 0:退出回拉
        /// 1:前拉
        /// 2:后拉
        /// </summary>
        private int _lastPullBack = 0;

        /// <summary>
        /// 前拉查看150列数据
        /// </summary>
        /// <returns>返回较新的最多150列数据（数据编号：从小往大）</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        override public IEnumerable<DisplayScanlineDataBundle> NavigateFront()
        {
            lock (_sync)
            {
                if (_lastPullBack != 1)
                    ClearPullBackCache();
                _lastPullBack = 1;
                var result = new List<DisplayScanlineDataBundle>();

                // 从最旧的一幅图像开始搜索
                var firstImage = RecentImagesCache.First;
                var time1 = DateTime.Now;
                while (firstImage != null &&
                    (firstImage.Value.Empty || firstImage.Value.MaxLineNumber <= ShowingMaxNumber))
                {
                    // 由于添加了释放图像，因此图像前拉可能需要加载图像，判断依据就是下一幅图像是最后一幅图像
                    if (firstImage.Next == RecentImagesCache.Last)
                    {
                        LoadLaterImage2();
                    }
                    // 移至下一个图像继续查找
                    firstImage = firstImage.Next;
                }
                Tracer.TraceDebug($"[PullBackTimeoutTracked] NavigateBack one while execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
                // 找到的目标图像
                if (firstImage == null) return null;
                time1 = DateTime.Now;
                var firstLine = firstImage.Value.ScanLines.First;

                // 继续往队列尾部搜索，一直找到第一个大于showingLinesMaxNumber的扫描线
                while (firstLine != null && firstLine.Value.LineNumber <= ShowingMaxNumber)
                {
                    firstLine = firstLine.Next;
                }
                Tracer.TraceDebug($"[PullBackTimeoutTracked] NavigateBack search last line execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
                // 如果已经前拉至文件的最后一线，则移至下一个图像的第一线数据
                //if (firstLine == null && firstImage.Next != null)
                //{
                //    // 由于添加了释放图像，因此图像前拉可能需要加载图像，判断依据就是下一幅图像是最后一幅图像
                //    if (firstImage.Next == RecentImagesCache.Last)
                //    {
                //        LoadLaterImage2();
                //    }
                //    // 取下一个图像的第一线数据
                //    firstImage = firstImage.Next;
                //    firstLine = firstImage.Value.ScanLines.First;
                //}
                time1 = DateTime.Now;
                while (firstLine != null)
                {
                    // 往后搜索至多100列数据，用于前拉显示
                    while (firstLine != null)
                    {
                        result.Add(firstLine.Value);
                        firstLine = firstLine.Next;
                    }

                    if (result.Count > 0)
                    {
                        // 前拉后，最后一线编号最大
                        ShowingMaxNumber = result.Last().LineNumber;

                        // 超过一屏，表明新数据已经超过一屏
                        if (ShowingMaxNumber >= ScreenMaxLinesCount)
                        {
                            ShowingMinNumber = ShowingMaxNumber - ScreenMaxLinesCount + 1;
                        }
                        else
                        {
                            ShowingMinNumber += result.Count;
                            ShowingMinNumber = Math.Min(0, ShowingMinNumber);
                        }
                    }

                    // 释放不再显示的图像，防止内存泄露
                    //ReleaseInvisibleImages();
                    Tracer.TraceDebug($"[PullBackTimeoutTracked] NavigateBack to end execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// 回拉查看旧数据150列
        /// </summary>
        /// <returns>返回之前的最多150列旧数据（数据编号：从大往小）</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        override public IEnumerable<DisplayScanlineDataBundle> NavigateBack()
        {
            lock (_sync)
            {
                if (_lastPullBack != 2)
                    ClearPullBackCache();
                _lastPullBack = 2;
                var result = new List<DisplayScanlineDataBundle>();
                var time1 = DateTime.Now;
                // 从最后一个图像开始搜索，最后一个图像为未存储完毕的图像，因此以下的while循环肯定能进入
                if(_pullBeforeImagesCache != null && _pullBeforeImagesCache.Count <= 0)
                {
                    var node = RecentImagesCache.Last;
                    while (node != null)
                    {
                        _pullBeforeImagesCache.AddLast(node.Value);
                        node = node.Next;
                    }
                }
                Tracer.TraceDebug($"[PullBackTimeoutTracked] NavigateBack one while execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
                var lastImage = RecentImagesCache.Last;
                time1 = DateTime.Now;
                ////////////////////////////////////////////////////////////////////////////////////////
                // 先搜索目标图像，直到找到包含有效数据范围的图像，如果没有找到，则一直循环读取历史图像
                // 若当前图像为空，即内存中没有尚未存储的数据，或者有尚未存储的数据时，其最小编号超过当前显示的最小编号时，需要
                // 向前寻找前面一幅图像的数据来显示
                while (lastImage != null && 
                    (lastImage.Value.Empty || lastImage.Value.MinLineNumber >= ShowingMinNumber))
                {
                    // 没有更多图像了，需要重新读取一个图像
                    if (lastImage.Previous == null)
                    {
                        LoadEarlierImage2();
                    }

                    // 重新读取后仍然为空，则说明已经回拉到头，再也没有历史数据可查看
                    if (lastImage.Previous == null)
                    {
                        return null;
                    }

                    lastImage = lastImage.Previous;
                }
                Tracer.TraceDebug($"[PullBackTimeoutTracked] NavigateBack load image while execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
                // 根据上面的逻辑，到此处：lastImage肯定不为空，并且一定是找到的目标图像
                if (lastImage == null)
                {
                    return null;
                }
                time1 = DateTime.Now;
                ////////////////////////////////////////////////////////////////////////////////////////
                // 找到第一个匹配数据编号范围的图像，开始取回拉数据

                // 从此图像的最后一列开始搜索
                var lastLine = lastImage.Value.ScanLines.Last;

                // 从尾部开始搜索，直到移动至编号小于showingMinNumber的第一线数据
                while (lastLine != null && lastLine.Value.LineNumber >= ShowingMinNumber)
                {
                    lastLine = lastLine.Previous;
                }
                Tracer.TraceDebug($"[PullBackTimeoutTracked] NavigateBack search start line execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
                time1 = DateTime.Now;
                // 取回拉数据，结果中编号从大往小排列
                while (lastLine != null)
                {
                    result.Add(lastLine.Value);

                    // 继续往前搜索
                    lastLine = lastLine.Previous;
                }
                Tracer.TraceDebug($"[PullBackTimeoutTracked] NavigateBack obtain result execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
                // 如果数据列数不够，继续加载文件进行搜索，直到没有文件可加载为止
                //while (result.Count < result.Capacity)
                //{
                // 没有更多图像了，需要重新读取一个图像
                //if (lastImage.Previous == null)
                //{
                //    LoadEarlierImage();
                //}
                time1 = DateTime.Now;
                // 重新读取后仍然为空，则说明已经回拉到头，再也没有历史数据可查看
                if (lastImage.Previous == null)
                {
                    goto ReturnResult;
                }

                // 往前导航一个图像
                lastImage = lastImage.Previous;

                lastLine = lastImage.Value.ScanLines.Last;

                //    // 往前搜寻最多100列数据，结果中编号从大往小排列
                //    while (result.Count < result.Capacity && lastLine != null)
                //    {
                //        result.Add(lastLine.Value);

                //        // 继续往前搜索
                //        lastLine = lastLine.Previous;
                //    }
                //}

            ReturnResult:
                if (result.Count > 0)
                {
                    // 回拉结果中的最后一线数据，将会是屏幕中显示的最小编号
                    ShowingMinNumber = result.Last().LineNumber;

                    // 数据区中的最大编号 - 回拉后的最小编号 如果超过一屏
                    if (ShowingMinNumber + ScreenMaxLinesCount <= 0)
                    {
                        ShowingMaxNumber = ShowingMinNumber + ScreenMaxLinesCount - 1;
                    }
                    else
                    {
                        ShowingMaxNumber -= result.Count;
                        ShowingMaxNumber = Math.Max(ShowingMaxNumber, -1);
                    }
                }

                // 释放不再显示的图像，防止内存泄露
                //ReleaseInvisibleImages();
                Tracer.TraceDebug($"[PullBackTimeoutTracked] NavigateBack to end execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
                return result;
            }
        }

        /// <summary>
        /// 结束导航，返回至最后一屏采集的数据
        /// </summary>
        /// <returns>最后采集的一屏幕的数据（有可能不足一屏），从小到大进行编号</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        override public IEnumerable<DisplayScanlineDataBundle> NavigateToLastScreen()
        {
            // 返回队列，最大长度为当前屏幕所能同时显示的扫描线的数量
            var lastScreen = new LinkedList<DisplayScanlineDataBundle>();
            int maxLines = ScreenMaxLinesCount;

            lock (_sync)
            {
                // 考虑到加载上一幅图像的逻辑不好更改，并且可能不连续，此处先清空所有保存完毕的图像，然后重新加载
                //while (RecentImagesCache.Count > 1)
                //{
                //    //清空图像中的Tip区域标识框
                //    //DeleteTipCadRegion(RecentImagesCache.First.Value);

                //    RecentImagesCache.RemoveFirst();
                //}
                if(_pullBeforeImagesCache != null && _pullBeforeImagesCache.Count > 0)
                {
                    RecentImagesCache = _pullBeforeImagesCache;
                    _pullBeforeImagesCache = new LinkedList<NavigatableScanLinesCache>();
                }

                // 从最后一个图像的最后一列开始往前搜寻数据
                // 返回至最后一屏图像时，只显示本次开机以来的数据，不显示以前的历史数据，因此要判断图像是否是本次扫描的图像
                var lastImage = RecentImagesCache.Last;
                while (lastScreen.Count < maxLines &&
                    lastImage != null && lastImage.Value.ScannedThisTime)
                {
                    var lastLine = lastImage.Value.ScanLines.Last;
                    while (lastScreen.Count < maxLines && lastLine != null)
                    {
                        lastScreen.AddFirst(lastLine.Value);
                        lastLine = lastLine.Previous;
                    }

                    // 判断线数如果不够，并且前一幅图像还未加载，继续往前加载图像，然后进入下一次读入数据循环
                    if (lastScreen.Count < maxLines && lastImage.Previous == null)
                    {
                        LoadEarlierImage();
                        // 判断是否成功加载新图像以及新加载的图像ScannedThisTime是否为true todo 这里是多余的
                        //if (lastImage.Previous == null || !lastImage.Previous.Value.ScannedThisTime)
                        //{
                        //    break;
                        //}
                    }

                    // 移动至前一个图像
                    lastImage = lastImage.Previous;
                }

                // 显示最后采集的一屏数据，更新最大最小编号
                if (lastScreen.Count > 0)
                {
                    ShowingMaxNumber = lastScreen.Last.Value.LineNumber;
                    ShowingMinNumber = lastScreen.First.Value.LineNumber;
                }
                else
                {
                    ShowingMaxNumber = 0;
                    ShowingMinNumber = 0;
                }

                // 释放不再显示的图像，防止内存泄露 todo 此处不再需要
                //ReleaseSomeRecentImages();
            }

            return lastScreen;
        }

        /// <summary>
        /// 清除缓存数据，在光障分包时使用
        /// </summary>
        public void ClearUnsavedScanLines()
        {
            lock (_sync)
            {
                UnsavedScanLines.Clear();
            }
        }

        /// <summary>
        /// 添加新的扫描线，即新扫描的一线数据
        /// <remarks>此函数应该由数据预处理线程调用。</remarks>
        /// </summary>
        /// <param name="bundle">新的扫描线数据</param>
        public void AppendNewScanLine(DisplayScanlineDataBundle bundle)
        {
            lock (_sync)
            {
                // 将新采集的数据加入到尚未存储完毕的图像数据队列的尾部，等待存储完毕
                UnsavedScanLines.AddLast(bundle);

                // 处理极端情况：等待存储的数据过多，需要及时清理，防止占用过多内存
                if (UnsavedScanLines.Count >= UnsavedLinesThreashold)
                {
                    UnsavedScanLines.RemoveFirst();

                    // 先将最后一个节点保存，待队列清空后，再将尚未存储完毕的缓存添加到队列末尾
                    var lastValue = RecentImagesCache.Last.Value;

                    // 为了避免编号混乱，同时清空近期图像列表；待回拉访问时，再重新加载近期图像
                    RecentImagesCache.Clear();
                    RecentImagesCache.AddLast(lastValue);

                    Tracer.TraceError(
                        "Unsaved image scan lines count is greater than 4000, there must be error in image store process.");
                }

                // 当前显示扫描线的最大编号
                ShowingMaxNumber = bundle.LineNumber;

                // 新数据超过一屏幕后，更新最小编号，否则最小编号一直为0
                if (ShowingMaxNumber >= ScreenMaxLinesCount)
                {
                    ShowingMinNumber = ShowingMaxNumber - ScreenMaxLinesCount + 1;
                }
                else
                {
                    ShowingMinNumber = 0;
                }

                // 尝试释放一些历史图像以节约内存
                ReleaseInvisibleImages();
            }
        }

        /// <summary>
        /// 将所有通过AppendNewScanLine新添加的尚未存储的图像列，保存输入参数指定的文件中，同时写入数据库
        /// 同时将当前插入的Tip保存到图像中
        /// </summary>
        /// <param name="filePath">保存新图像的完整文件路径</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool SaveNewLinesIntoImage(string filePath, int endLineNo, DateTime endTime, string accountId)
        {
            lock (_sync)
            {
                if (UnsavedScanLines == null || UnsavedScanLines.Count < _bagMinLinesCount)
                {
                    //Tracer.TraceWarning("UnsavedScanlines is null or smaller than bagMinLinesCount " +
                    //                    _bagMinLinesCount.ToString() +
                    //                    ". There are too little scanlines in pool when save image! Do not save file!");
                    UnsavedScanLines.Clear();
                    return false;
                }

                //Task.Run(() =>
                //{
                    //AIJudgeImageService.Service.AddXRayImage(DeepCopy.DeepCopyByBin(UnsavedScanLines), filePath);
                //});

                var bundleList = UnsavedScanLines;
                var matBundles = new List<ClassifiedLineDataBundle>(bundleList.Count);

                if (!_hasHighAndLow)
                {
                    matBundles.AddRange(
                    bundleList.Select(
                        orderedbundle =>
                            new ClassifiedLineDataBundle(orderedbundle.View1Data.ToXRayMatLineData(),
                                orderedbundle.View2Data == null ? null : orderedbundle.View2Data.ToXRayMatLineData())));
                }
                else
                {
                   matBundles.AddRange(
                   bundleList.Select(
                       orderedbundle =>
                           new ClassifiedLineDataBundle(orderedbundle.View1Data.ToXRayMatLineDataWithHighLow(),
                               orderedbundle.View2Data == null ? null : orderedbundle.View2Data.ToXRayMatLineDataWithHighLow())));
                    HttpAiJudgeServices.ProcessHighLowData(bundleList,HttpAiJudgeServices.deviceInfo.IsBigEnd);
                    int i = 1;
                    Tracer.TraceInfo($"第{i}次命中{filePath}");
                    i++;
                }



                var image = new XRayScanlinesImage(matBundles, 
                    _machineType,
                    _machineNumber, 
                    LoginAccountManager.Service.AccountId, 
                    ConfigHelper.XRayGen1Current, 
                    ConfigHelper.XRayGen1Voltage, 
                    ConfigHelper.XRayGen2Current,
                    ConfigHelper.XRayGen2Voltage, 
                    1);

                // 图像数据在全局数据区中的起止线编号
                int lineMinNum = UnsavedScanLines.First.Value.LineNumber;
                int lineMaxNum = UnsavedScanLines.Last.Value.LineNumber;                

                if (_currentTipImage != null && _currentTipRegion != null &&
                    _currentTipRegion.FromLine >= lineMinNum && _currentTipRegion.FromLine <= lineMaxNum)
                {
                    // 插入线位置转换为在图像中的相对位置
                    var fromLine = _currentTipRegion.FromLine - lineMinNum;
                    var toLine = _currentTipRegion.ToLine - lineMinNum;

                    image.TipInjection = new TipInjection(_currentTipImage,
                        new MarkerRegion(MarkerRegionType.Tip, fromLine, toLine, 
                            _currentTipRegion.FromChannel, _currentTipRegion.ToChannel));
                }
                try
                {
                    _imageProcessor.AttachImageData(image.View1Data);
                    var bmp = _imageProcessor.GetBitmap();
                    image.View1Data.Thumbnail = XRayImageHelper.GenerateThumbnail(bmp);

                    if (image.View2Data != null)
                    {
                        _imageProcessor.AttachImageData(image.View2Data);
                        var bmp2 = _imageProcessor.GetBitmap();
                        image.View2Data.Thumbnail = XRayImageHelper.GenerateThumbnail(bmp2);
                    }
                }
                catch (Exception ex)
                {
                    Tracer.TraceException(ex);
                }                

                var record = new ImageRecord { StorePath = filePath, ScanTime = image.ScanningTime };
                record.AccountId = LoginAccountManager.Service.AccountId;
                record.MachineNumber = _machineNumber;

                PaintingRegionsService.Service.AddXRayFileInfo(new XRayFileInfoForMark(record,filePath, lineMinNum, lineMaxNum));

                TRSNetwork.TRSNetWorkService.Service.UpdateBagEndInfo(endTime, endLineNo, filePath, accountId);

                //异步保存记录
                _imageAsyncStoreController.SaveImage(new ImageStoreInfo(record, filePath, image));

                // 将当前所有未存储的图像列，转移至图像记录record中
                var linesToMove = new LinkedList<DisplayScanlineDataBundle>(UnsavedScanLines);

                // 将保存完毕的图像存储在图像队列的末尾
                var imageNode = new NavtigatableImage(record, linesToMove);

                // 最后一个节点永远是尚未存储完毕的数据缓存，在此节点之前添加最后一幅图像
                RecentImagesCache.AddBefore(RecentImagesCache.Last, imageNode);

                //ReleaseInvisibleImages();

                // 记录当前最小线编号（在队列中没有任何完整图像时使用）
                _minNumber = imageNode.MaxLineNumber + 1;

                UnsavedScanLines.Clear();

                
                _currentCadRegions.FileName = filePath;

                //如果启动了htnova项目，才将探测区域数据发送到后续处理
                if (_htnovaEnabled)
                {
                    _htnovaImageSaveController.Enqueue(
                        new KeyValuePair<XrayCadRegions, XRayScanlinesImage>(_currentCadRegions.Clone(), image));
                }

                if (_enableAutoStoreUpfImage)
                {
                    _autoStoreUpfImage.Enqueue(new XrayImageInfo(filePath, _currentCadRegions.Clone(), image));
                }

                _currentCadRegions.Clear();
            }
            return true;
        }


        public void OnContrabandDetected(MarkerRegion region, DetectViewIndex viewIndex)
        {
            _currentCadRegions.Add(region, viewIndex);
        }

        /// <summary>
        /// 从内存中释放在屏幕中不可见的图像
        /// <remarks></remarks>
        /// </summary>
        public void ReleaseInvisibleImages()
        {
            try
            {
                lock (_sync)
                {
                    // 清除较老的不再显示的图像
                    while (RecentImagesCache.First.Value.MaxLineNumber < ShowingMinNumber)
                    {
                        RecentImagesCache.RemoveFirst();
                    }

                    // 清除较新的不再显示的图像
                    while (RecentImagesCache.Count >= 2 && RecentImagesCache.Last.Previous.Value.MinLineNumber > ShowingMaxNumber)
                    {
                        RecentImagesCache.Remove(RecentImagesCache.Last.Previous);
                    }
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e, "Occurred while releasing some recent images ScanningScanLinesPool.ReleaseSomeRecentImages");
            }
        }

        /// <summary>
        /// 当前数据区中的数据的最小编号，和MinNumber不是完全配对。该值用于记录在未保存图像线为空，并且图像队列中无图像时使用，
        /// 因此，每次保存图像都会更新该值，该值为保存图像最大线编号加1，用于加载图像时生成具有编号的线
        /// </summary>
        private int _minNumber;

        /// <summary>
        /// 当前数据区中的数据的最小编号
        /// </summary>
        private int MinNumber
        {
            get
            {
                if (RecentImagesCache.Count >= 2)
                {
                    // 取图像缓存中第一个图像的第一列数据
                    return RecentImagesCache.First.Value.ScanLines.First.Value.LineNumber;
                }
                else
                {
                    // 如果没有数据，则最小编号为0
                    return UnsavedScanLines.Count == 0 ? _minNumber : UnsavedScanLines.First.Value.LineNumber;
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void LoadEarlierImage2()
        {
            var time1 = DateTime.Now;
            if (_pullbackCache == null || _pullbackCache.Count <= 0)
            {
                ImageRecord earliestRecord = null;
                if (RecentImagesCache.Count >= 2)
                {
                    var imageCache = RecentImagesCache.First.Value as NavtigatableImage;
                    if (imageCache != null)
                    {
                        earliestRecord = imageCache.Record;
                    }
                }
                var imageRecordsManager = new ImageRecordDbSet();
                imageRecordsManager.PullBackAccountId = new List<string>() { LoginAccountManager.Service.CurrentAccount.AccountId };
                List<ImageRecord> records = null;
                time1 = DateTime.Now;
                if (earliestRecord != null)
                {
                    records = imageRecordsManager.TakeRecordsBefore(100, earliestRecord);
                }
                else
                {
                    records = imageRecordsManager.PullbackTakeLatest(100);
                }
                Tracer.TraceDebug($"[PullBackTimeoutTracked] LoadEarlierImage2 find image record execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
                if (records == null || records.Count <= 0)
                    return;
                _pullbackCache = new LinkedList<ImageRecord>(records);
            }
            time1 = DateTime.Now;
            while (_pullbackCache != null && _pullbackCache.Count > 0)
            {
                var record = _pullbackCache.First();
                if (!File.Exists(record.StorePath))
                {
                    Tracer.TraceInfo("File has not been Created but Recorded in DB already!");
                    _pullbackCache.RemoveFirst();
                    continue;
                }
                var time2 = DateTime.Now;
                // 读取图像文件并解压缩
                var image = XRayScanlinesImage.LoadFromDiskFile(record.StorePath);

                // 将图像数据转换为编号后的队列，并取当前最小编号-1作为该图像中数据的最大编号
                var imageScanLines = image.ToDisplayXRayMatLineDataBundles(MinNumber - 1);
                Tracer.TraceDebug($"[PullBackTimeoutTracked] LoadEarlierImage2 load disk file execution time:{(DateTime.Now - time2).TotalMilliseconds} Milliseconds");
                PaintingRegionsService.Service.AddXRayFileInfo(new XRayFileInfoForMark(
                    record,
                    record.StorePath,
                    MinNumber - imageScanLines.Count,
                    MinNumber - 1));
                if (record.IsManualSaved)
                {
                    var view1Rect = PaintingRegionsService.Service.GetMarkRegionsFromXRay(image, DetectViewIndex.View1, MinNumber - imageScanLines.Count);
                    var view2Rect = PaintingRegionsService.Service.GetMarkRegionsFromXRay(image, DetectViewIndex.View2, MinNumber - imageScanLines.Count);
                    if (DrawRectAction != null)
                    {
                        if (view1Rect.Count > 0)
                            DrawRectAction(new KeyValuePair<DetectViewIndex, List<MarkerRegion>>(DetectViewIndex.View1, view1Rect));
                        if (view2Rect.Count > 0)
                            DrawRectAction(new KeyValuePair<DetectViewIndex, List<MarkerRegion>>(DetectViewIndex.View2, view2Rect));
                    }
                }
                if (image.TipInjection != null)
                {
                    OutputTipRegions(imageScanLines, image.TipInjection);
                }

                // 根据扫描线，构造图像节点，并加入到RecentImages队列的头部
                var newImageNode = new NavtigatableImage(record, imageScanLines);
                RecentImagesCache.AddFirst(newImageNode);
                _pullbackCache.RemoveFirst();
                break;
            }
            Tracer.TraceDebug($"[PullBackTimeoutTracked] LoadEarlierImage2 to end while execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
        }

        /// <summary>
        /// 从磁盘中再加载一个先前的图像
        /// 加载顺序：如果当前未加载任何图像，则选择距离当前时间最近的一个图像；
        /// 如果当前已经加载了一些图像，则选择已加载图像中最老的一个作为时间点，寻找比该图像更早的第一个图像
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void LoadEarlierImage()
        {
            ImageRecord earliestRecord = null;

            // 如果图像队列中已经有保存完毕的图像，取图像队列中最早的一个图像作为参照
            if (RecentImagesCache.Count >= 2)
            {
                var imageCache = RecentImagesCache.First.Value as NavtigatableImage;
                if (imageCache != null)
                {
                    earliestRecord = imageCache.Record;
                }
            }

            var imageRecordsManager = new ImageRecordDbSet();
            imageRecordsManager.PullBackAccountId = LoginAccountManager.Service.GetCurrentAndLessThanId();

            while (true)
            {
                // 从数据库中读取图像记录
                List<ImageRecord> resultRecords = null;
                try
                {
                    if (earliestRecord != null)
                    {
                        resultRecords = imageRecordsManager.TakeRecordsBefore(1, earliestRecord);
                    }
                    else
                    {
                        resultRecords = imageRecordsManager.PullbackTakeLatest(1);
                    }
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception);
                    Tracer.TraceError(
                        "Unexpected exception caught when calling imageRecordsManager.TakeRecordsBefore(,) with params:");
                    throw;
                }

                if (resultRecords != null && resultRecords.Count >= 1)
                {
                    try
                    {
                        int loupcount = 0;
                        while (loupcount < 5)
                        {
                            if (!File.Exists(resultRecords[0].StorePath))  //如果文件在数据库中有记录但是实际上还不存在输出日志
                            {
                                Tracer.TraceInfo("File has not been Created but Recorded in DB already!");

                                Thread.Sleep(500);
                                loupcount++;
                            }
                            else
                            {
                                break;
                            }
                        }
                        // 读取图像文件并解压缩
                        var image = XRayScanlinesImage.LoadFromDiskFile(resultRecords[0].StorePath);

                        // 将图像数据转换为编号后的队列，并取当前最小编号-1作为该图像中数据的最大编号
                        var imageScanLines = image.ToDisplayXRayMatLineDataBundles(MinNumber - 1);

                        PaintingRegionsService.Service.AddXRayFileInfo(new XRayFileInfoForMark(
                            resultRecords[0],
                            resultRecords[0].StorePath,
                            MinNumber - imageScanLines.Count,
                            MinNumber-1));
                        if (resultRecords[0].IsManualSaved)
                        {
                            var view1Rect = PaintingRegionsService.Service.GetMarkRegionsFromXRay(image, DetectViewIndex.View1, MinNumber - imageScanLines.Count);
                            var view2Rect = PaintingRegionsService.Service.GetMarkRegionsFromXRay(image, DetectViewIndex.View2, MinNumber - imageScanLines.Count);
                            if (DrawRectAction != null)
                            {
                                if (view1Rect.Count > 0)
                                    DrawRectAction(new KeyValuePair<DetectViewIndex, List<MarkerRegion>>(DetectViewIndex.View1, view1Rect));
                                if (view2Rect.Count > 0)
                                    DrawRectAction(new KeyValuePair<DetectViewIndex, List<MarkerRegion>>(DetectViewIndex.View2, view2Rect));
                            }
                        }
                        if (image.TipInjection != null)
                        {
                            OutputTipRegions(imageScanLines, image.TipInjection);
                        }

                        // 根据扫描线，构造图像节点，并加入到RecentImages队列的头部
                        var newImageNode = new NavtigatableImage(resultRecords[0], imageScanLines);
                        RecentImagesCache.AddFirst(newImageNode);
                        break;
                    }
                    catch (Exception exception)
                    {
                        Tracer.TraceException(exception);
                        Tracer.TraceError("Failed to load file from disk for navigating: " + resultRecords[0].StorePath);
                        earliestRecord = resultRecords[0];
                    }
                }
                else
                {
                    // 没有查找到记录，不再继续，退出
                    break;
                }
            }
        }

        /// <summary>
        /// 输出与读入的图像关联的tip区域
        /// </summary>
        /// <param name="imageLines">图像转换为内存中编号的数据线，其编号为从小到大</param>
        /// <param name="tipInjection">图像中存储的Tip注入信息</param>
        private void OutputTipRegions(LinkedList<DisplayScanlineDataBundle> imageLines, TipInjection tipInjection)
        {
            try
            {
                if (imageLines != null && imageLines.Count > 0 && tipInjection != null && tipInjection.TipImage != null)
                {
                    var regionInImage = tipInjection.RegionInImage;

                    // 图像数据在全局数据区中的位置
                    var imageMinLineNumber = imageLines.First.Value.LineNumber;

                    var fromLine = imageMinLineNumber + regionInImage.FromLine;

                    // 如果图像中的tip区域，与当前的Tip区域重合，即表示同一个区域.
                    // 如果此区域尚未由用户识别或漏判（即不可见）则不显示（该区域将由注入流程自动显示）
                    if (_currentTipRegion != null && _currentTipRegion.FromLine == fromLine && !_currentTipVisible)
                    {
                        return;
                    }

                    var toLine = imageMinLineNumber + regionInImage.ToLine;
                    var fromChannel = regionInImage.FromChannel;
                    var toChannel = regionInImage.ToChannel;

                    // 此tip在全局数据区中的位置
                    var globalRegion = new MarkerRegion(MarkerRegionType.Tip, fromLine, toLine, fromChannel, toChannel);

                    if (TipInjectionLoaded != null)
                    {
                        TipInjectionLoaded(globalRegion);
                    }
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void LoadLaterImage2()
        {
            var time1 = DateTime.Now;
            if (_pullbackCache == null || _pullbackCache.Count <= 0)
            {
                ImageRecord latestRecord = null;
                if (RecentImagesCache.Count < 2)
                    return;
                var imageCache = RecentImagesCache.Last.Previous.Value as NavtigatableImage;
                if (imageCache == null)
                    return;
                latestRecord = imageCache.Record;
                if (latestRecord == null)
                    return;
                var imageRecordsManager = new ImageRecordDbSet();
                //imageRecordsManager.PullBackAccountId = LoginAccountManager.Service.GetCurrentAndLessThanId();
                imageRecordsManager.PullBackAccountId = new List<string>() { LoginAccountManager.Service.CurrentAccount.AccountId };
                time1 = DateTime.Now;
                var records = imageRecordsManager.TakeRecordsAfter(100, latestRecord);
                Tracer.TraceDebug($"[PullBackTimeoutTracked] LoadLaterImage2 find image record execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
                if (records == null || records.Count <= 0)
                    return;
                _pullbackCache = new LinkedList<ImageRecord>(records);
            }
            time1 = DateTime.Now;
            while (_pullbackCache != null && _pullbackCache.Count > 0)
            {
                var record = _pullbackCache.First();
                if (!File.Exists(record.StorePath))
                {
                    _pullbackCache.RemoveFirst();
                    continue;
                }
                var time2 = DateTime.Now;
                // 读取图像文件并解压缩
                var image = XRayScanlinesImage.LoadFromDiskFile(record.StorePath);

                // 将图像数据转换为编号后的队列，并取当前最小编号-1作为该图像中数据的最大编号
                //var maxLineNum = RecentImagesCache.Last.Previous.Value.MaxLineNumber + image.View1Data.ScanLinesCount;
                //var imageScanLines = image.ToDisplayXRayMatLineDataBundles(maxLineNum);
                var imageScanLines =
                    image.ToDisplayXRayMatLineDataBundles(RecentImagesCache.Last.Previous.Value.MaxLineNumber +
                                          image.View1Data.ScanLinesCount);
                Tracer.TraceDebug($"[PullBackTimeoutTracked] LoadLaterImage2 load disk file execution time:{(DateTime.Now - time2).TotalMilliseconds} Milliseconds");
                if (image.TipInjection != null)
                {
                    OutputTipRegions(imageScanLines, image.TipInjection);
                }

                PaintingRegionsService.Service.AddXRayFileInfo(new XRayFileInfoForMark(
                    record,
                    record.StorePath,
                    RecentImagesCache.Last.Previous.Value.MaxLineNumber + 1,
                    RecentImagesCache.Last.Previous.Value.MaxLineNumber + image.View1Data.ScanLinesCount
                    ));
                if (record.IsManualSaved)
                {
                    var view1Rect = PaintingRegionsService.Service.GetMarkRegionsFromXRay(image, DetectViewIndex.View1, RecentImagesCache.Last.Previous.Value.MaxLineNumber);
                    var view2Rect = PaintingRegionsService.Service.GetMarkRegionsFromXRay(image, DetectViewIndex.View2, RecentImagesCache.Last.Previous.Value.MaxLineNumber);
                    if (DrawRectAction != null)
                    {
                        if (view1Rect.Count > 0)
                            DrawRectAction(new KeyValuePair<DetectViewIndex, List<MarkerRegion>>(DetectViewIndex.View1, view1Rect));
                        if (view2Rect.Count > 0)
                            DrawRectAction(new KeyValuePair<DetectViewIndex, List<MarkerRegion>>(DetectViewIndex.View2, view2Rect));
                    }
                }

                // 根据扫描线，构造图像节点，并加入到RecentImages队列的头部
                var newImageNode = new NavtigatableImage(record, imageScanLines);
                RecentImagesCache.AddBefore(RecentImagesCache.Last, newImageNode);
                _pullbackCache.RemoveFirst();
                break;
            }
            Tracer.TraceDebug($"[PullBackTimeoutTracked] LoadLaterImage2 to end while execution time:{(DateTime.Now - time1).TotalMilliseconds} Milliseconds");
        }

        /// <summary>
        /// 从磁盘中再加载一个新的图像
        /// 加载顺序：如果当前未加载任何图像，则不加载图像；
        /// 如果当前已经加载了一些图像，则选择已加载图像中最新的一个作为时间点，寻找比该图像更新的第一个图像
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void LoadLaterImage()
        {
            ImageRecord latestRecord = null;

            // 如果图像队列中已经有保存完毕的图像，取图像队列中最新的一个图像作为参照
            if (RecentImagesCache.Count >= 2)
            {
                var imageCache = RecentImagesCache.Last.Previous.Value as NavtigatableImage;
                if (imageCache != null)
                {
                    //timeSeperator = imageCache.Record.ScanTime;
                    latestRecord = imageCache.Record;
                }
                else
                {
                    Tracer.TraceError(
                        "The last second iamge in RecentImages ScanningScanLinesPool is not NavtigatableImage");
                    return;
                }
            }
            else
            {
                return;
            }

            var imageRecordsManager = new ImageRecordDbSet();
            imageRecordsManager.PullBackAccountId = LoginAccountManager.Service.GetCurrentAndLessThanId();

            while (true)
            {
                // 从数据库中读取图像记录
                List<ImageRecord> resultRecords = null;
                try
                {
                    if (latestRecord != null)
                    {
                        resultRecords = imageRecordsManager.TakeRecordsAfter(1, latestRecord);
                    }
                    else
                    {
                        break;
                    }
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception);
                    Tracer.TraceError(
                        "Unexpected exception caught when calling _recordsManager.SelectSomeAfterDateTime(,) with params:");
                    throw;
                }

                if (resultRecords != null && resultRecords.Count >= 1)
                {
                    try
                    {
                        // 读取图像文件并解压缩
                        var image = XRayScanlinesImage.LoadFromDiskFile(resultRecords[0].StorePath);

                        // 将图像数据转换为编号后的队列，并取当前最小编号-1作为该图像中数据的最大编号
                        //var maxLineNum = RecentImagesCache.Last.Previous.Value.MaxLineNumber + image.View1Data.ScanLinesCount;
                        //var imageScanLines = image.ToDisplayXRayMatLineDataBundles(maxLineNum);
                        var imageScanLines =
            image.ToDisplayXRayMatLineDataBundles(RecentImagesCache.Last.Previous.Value.MaxLineNumber +
                                                  image.View1Data.ScanLinesCount);
                        if (image.TipInjection != null)
                        {
                            OutputTipRegions(imageScanLines, image.TipInjection);
                        }

                        PaintingRegionsService.Service.AddXRayFileInfo(new XRayFileInfoForMark(
                            resultRecords[0],
                            resultRecords[0].StorePath,
                            RecentImagesCache.Last.Previous.Value.MaxLineNumber+1,
                            RecentImagesCache.Last.Previous.Value.MaxLineNumber + image.View1Data.ScanLinesCount
                            ));
                        if (resultRecords[0].IsManualSaved)
                        {
                            var view1Rect = PaintingRegionsService.Service.GetMarkRegionsFromXRay(image, DetectViewIndex.View1, RecentImagesCache.Last.Previous.Value.MaxLineNumber);
                            var view2Rect = PaintingRegionsService.Service.GetMarkRegionsFromXRay(image, DetectViewIndex.View2, RecentImagesCache.Last.Previous.Value.MaxLineNumber);
                            if (DrawRectAction != null)
                            {
                                if (view1Rect.Count > 0)
                                    DrawRectAction(new KeyValuePair<DetectViewIndex, List<MarkerRegion>>(DetectViewIndex.View1, view1Rect));
                                if (view2Rect.Count > 0)
                                    DrawRectAction(new KeyValuePair<DetectViewIndex, List<MarkerRegion>>(DetectViewIndex.View2, view2Rect));
                            }
                        }

                        // 根据扫描线，构造图像节点，并加入到RecentImages队列的头部
                        var newImageNode = new NavtigatableImage(resultRecords[0], imageScanLines);
                        RecentImagesCache.AddBefore(RecentImagesCache.Last, newImageNode);
                        break;
                    }
                    catch (Exception exception)
                    {
                        Tracer.TraceException(exception);
                        Trace.TraceError("Failed to load file from disk for navigating.", resultRecords[0].StorePath);
                        latestRecord = resultRecords[0];
                    }
                }
                else
                {
                    // 没有查找到记录，不再继续，退出
                    break;
                }
            }
        }

        public void ShutDown()
        {
            if (_htnovaImageSaveController != null)
            {
                _htnovaImageSaveController.Exit();
            }

            if (_autoStoreUpfImage != null)
            {
                _autoStoreUpfImage.Shutdown();
            }

            if (_imageAsyncStoreController != null)
            {
                _imageAsyncStoreController.ShutDown();
            }
        }
    }
}
