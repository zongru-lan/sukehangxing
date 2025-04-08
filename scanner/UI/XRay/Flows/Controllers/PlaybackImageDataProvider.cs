using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.Algo;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Services;

namespace UI.XRay.Flows.Controllers
{
    /// <summary>
    /// 图像回放的数据管理
    /// </summary>
    class PlaybackImageDataProvider : IRollingImageDataProvider
    {
        /// <summary>
        /// 回拉图像数据管理中，不使用此事件
        /// </summary>
        event Action<DisplayScanlineDataBundle> IRollingImageDataProvider.DataReady
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        private LinkedList<string> _imagesFullPath;

        /// <summary>
        /// 当前已经加载的图像，可从中按顺序导航查看被选中的历史图像
        /// </summary>
        private readonly LinkedList<ReplayingNavtigatableImage> _selectedImages = new LinkedList<ReplayingNavtigatableImage>();

        public int ScreenMaxLinesCount { get; private set; }

        public int ShowingMinNumber { get; private set; }

        public int ShowingMaxNumber { get; private set; }

        public event Action<KeyValuePair<DetectViewIndex, List<MarkerRegion>>> DrawRectAction;

        /// <summary>
        /// 存放imagerecord，用于更新数据库
        /// </summary>
        private Dictionary<string, ImageRecord> ImageDictionary = new Dictionary<string, ImageRecord>();

        public PlaybackImageDataProvider(int screenMaxLinesCount, IEnumerable<ImageRecord> records)
        {
            ScreenMaxLinesCount = screenMaxLinesCount;

            if (records != null)
            {
                // 按图像生成时间相反的顺序排列图像。
                records = records.OrderByDescending(record => record.ImageRecordId);
                ImageDictionary.Clear();
                foreach (var image in records)
                {
                   ImageDictionary.Add(image.StorePath,image); 
                }
                PlaybackImages(records.Select(record => record.StorePath));
            }
        }

        public PlaybackImageDataProvider(int screenMaxLinesCount, IEnumerable<string> records)
        {
            ScreenMaxLinesCount = screenMaxLinesCount;

            if (records != null)
            {
                ImageDictionary.Clear();
                foreach (var image in records)
                {
                    ImageDictionary.Add(image, null);
                }
                PlaybackImages(records);
            }
        }


        /// <summary>
        /// 回放指定的图像序列：第一个图像先显示，仅接着显示第二个图像
        /// </summary>
        /// <param name="files"></param>
        private void PlaybackImages(IEnumerable<string> files)
        {
            if (files != null)
            {
                _imagesFullPath = new LinkedList<string>(files);
            }
        }

        public void ResetShowingRange(int minNum, int maxNum)
        {
            ShowingMaxNumber = maxNum;
            ShowingMinNumber = minNum;
        }

        /// <summary>
        /// 获取第一个屏幕的图像
        /// </summary>
        /// <returns></returns>
        public IEnumerable<DisplayScanlineDataBundle> GetFirstScreen()
        {
            PaintingRegionsService.Service.ClearXRayFileInfo();
            var result = new List<DisplayScanlineDataBundle>(ScreenMaxLinesCount);

            // 图像数据线的最大编号
            int maxNum = -1;

            // 从第一个图像开始
            var firstImagePath = _imagesFullPath.First;

            while (firstImagePath != null && result.Count < result.Capacity)
            {
                // 循环加载图像，直到加载到一个正确图像，或者以没有图像可加载
                while (true)
                {
                    try
                    {
                        // 读取图像文件并解压缩
                        var image = XRayScanlinesImage.LoadFromDiskFile(firstImagePath.Value);

                        //构造TIP报警框，并保存到链表中
                        //AddTipCadRegionToList(maxNum, image);

                        // 将图像数据转换为编号后的队列（转换后小号在前大号在后），并取当前最小编号-1作为该图像中数据的最大编号
                        var imageScanLines = image.ToDisplayXRayMatLineDataBundles(maxNum);
                        maxNum -= image.View1Data.ScanLinesCount;

                        ///////////// 测试
                        //AirLinesChecker air = new AirLinesChecker(DetectViewIndex.View1, 0, 0);
                        //List<ClassifiedLineDataBundle> data = new List<ClassifiedLineDataBundle>();
                        //for (int i = 0; i < 120; i++)
                        //{
                        //    imageScanLines.RemoveFirst();
                        //}
                        //for (int i = 0; i < 24; i++)
                        //{
                        //    var first = imageScanLines.First.Value;
                        //    ClassifiedLineDataBundle classify = new ClassifiedLineDataBundle(new ClassifiedLineData(DetectViewIndex.View1, first.View1Data.XRayData, first.View1Data.XRayDataEnhanced, first.View1Data.Material),
                        //       new ClassifiedLineData(DetectViewIndex.View2, first.View1Data.XRayData, first.View1Data.XRayDataEnhanced, first.View1Data.Material));
                        //    data.Add(classify);
                        //    imageScanLines.RemoveFirst();
                        //}
                        //air.TestAirLine(data);
                        ///////////////

                        if (ImageDictionary.ContainsKey(firstImagePath.Value) &&  (ImageDictionary[firstImagePath.Value] == null ||ImageDictionary[firstImagePath.Value].IsManualSaved))
                        {
                            var view1Rect = PaintingRegionsService.Service.GetMarkRegionsFromXRay(image, DetectViewIndex.View1, maxNum);
                            var view2Rect = PaintingRegionsService.Service.GetMarkRegionsFromXRay(image, DetectViewIndex.View2, maxNum);
                            if (DrawRectAction != null)
                            {
                                if (view1Rect.Count > 0)
                                    DrawRectAction(new KeyValuePair<DetectViewIndex, List<MarkerRegion>>(DetectViewIndex.View1, view1Rect));
                                if (view2Rect.Count > 0)
                                    DrawRectAction(new KeyValuePair<DetectViewIndex, List<MarkerRegion>>(DetectViewIndex.View2, view2Rect));
                            }
                        }

                        PaintingRegionsService.Service.AddXRayFileInfo(new XRayFileInfoForMark()
                        {
                            ImageRecord = ImageDictionary.ContainsKey(firstImagePath.Value) ? ImageDictionary[firstImagePath.Value] : null,
                            FilePath = firstImagePath.Value,
                            StartLineNumber = maxNum + 1,
                            EndLineNumber = maxNum + image.View1Data.ScanLinesCount
                        });

                        var newImageNode = new ReplayingNavtigatableImage(firstImagePath.Value, imageScanLines);

                        _selectedImages.AddLast(newImageNode);
                        firstImagePath = firstImagePath.Next;
                        break;
                    }
                    catch (Exception exception)
                    {
                        Tracer.TraceException(exception);
                        Tracer.TraceError("Failed to load file from disk for replaying." + firstImagePath.Value);

                        // 如果图像加载失败，则加载下一个
                        firstImagePath = firstImagePath.Next;

                        // 如果没有下一个图像，则退出
                        if (firstImagePath == null)
                        {
                            _imagesFullPath.RemoveLast();
                            break;
                        }
                        _imagesFullPath.Remove(firstImagePath.Previous);
                    }
                }

                LinkedListNode<DisplayScanlineDataBundle> lastLine = null;

                // 从此图像的最后一列（最大编号）开始
                if (_selectedImages.Last != null)
                {
                    lastLine = _selectedImages.Last.Value.ScanLines.Last;
                }

                // 从查找到的线依次往后寻找添加用于显示的数据线，最多ScreenMaxLinesCount线，直到已添加ScreenMaxLinesCount线数据，或者没有更多数据可显示
                while (result.Count < result.Capacity && lastLine != null)
                {
                    result.Add(lastLine.Value);
                    lastLine = lastLine.Previous;
                }
            }

            // 更新当前显示的最小编号和最大编号，最大编号必定是-1，最小编号是显示的线数的负数
            ShowingMinNumber = -result.Count;
            ShowingMaxNumber = -1;

            return result;
        }

        /// <summary>
        /// 加载下一幅（较老的）图像数据
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void LoadNextImage()
        {
            var imagePath = _imagesFullPath.Find(_selectedImages.Last.Value.FullPath).Next;
            if (imagePath == null)
            {
                return;
            }
            // 循环加载图像，直到加载到一个正确图像，或者以没有图像可加载
            while (true)
            {
                try
                {
                    // 读取图像文件并解压缩
                    var image = XRayScanlinesImage.LoadFromDiskFile(imagePath.Value);
                    // 图像数据线的最大编号 todo 如果当前没有加载图像岂不是要异常？
                    int maxNum = _selectedImages.Last.Value.MinLineNumber - 1;

                    //构造TIP报警框，并保存到链表中
                    //AddTipCadRegionToList(maxNum, image);

                    // 将图像数据转换为编号后的队列，并取当前最小编号-1作为该图像中数据的最大编号
                    var imageScanLines = image.ToDisplayXRayMatLineDataBundles(maxNum);
                    maxNum -= imageScanLines.Count;
                    if (ImageDictionary.ContainsKey(imagePath.Value) && (ImageDictionary[imagePath.Value] == null || ImageDictionary[imagePath.Value].IsManualSaved))
                    {
                        var view1Rect = PaintingRegionsService.Service.GetMarkRegionsFromXRay(image, DetectViewIndex.View1, maxNum);
                        var view2Rect = PaintingRegionsService.Service.GetMarkRegionsFromXRay(image, DetectViewIndex.View2, maxNum);
                        if (DrawRectAction != null)
                        {
                            if (view1Rect.Count > 0)
                                DrawRectAction(new KeyValuePair<DetectViewIndex, List<MarkerRegion>>(DetectViewIndex.View1, view1Rect));
                            if (view2Rect.Count > 0)
                                DrawRectAction(new KeyValuePair<DetectViewIndex, List<MarkerRegion>>(DetectViewIndex.View2, view2Rect));
                        }
                    }

                    PaintingRegionsService.Service.AddXRayFileInfo(new XRayFileInfoForMark()
                    {
                        ImageRecord = ImageDictionary.ContainsKey(imagePath.Value) ? ImageDictionary[imagePath.Value] : null,
                        FilePath = imagePath.Value,
                        StartLineNumber = maxNum +1,
                        EndLineNumber = maxNum + image.View1Data.ScanLinesCount
                    });

                    var newImageNode = new ReplayingNavtigatableImage(imagePath.Value, imageScanLines);
                    _selectedImages.AddLast(newImageNode);
                    break;
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception);
                    Tracer.TraceError("Failed to load file from disk for replaying.");

                    // 如果图像加载失败，则加载下一个
                    imagePath = imagePath.Next;
                    // 如果没有下一个图像，则退出
                    if (imagePath == null)
                    {
                        _imagesFullPath.RemoveLast();
                        break;
                    }
                    _imagesFullPath.Remove(imagePath.Previous);
                }
            }
        }

        /// <summary>
        /// 加载上一幅(较新的)图像数据
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void LoadPreviousImage()
        {
            var imagePath = _imagesFullPath.Find(_selectedImages.First.Value.FullPath).Previous;
            if (imagePath == null)
            {
                return;
            }
            try
            {
                // 读取图像文件并解压缩
                var image = XRayScanlinesImage.LoadFromDiskFile(imagePath.Value);
                // 图像数据线的最大编号
                int maxNum = _selectedImages.First.Value.MaxLineNumber + image.View1Data.ScanLinesCount;

                //添加Tip标识框到链表
                //AddTipCadRegionToList(maxNum, image);

                // 将图像数据转换为编号后的队列，并取当前最大编号+图像线数作为该图像中数据的最大编号
                var imageScanLines = image.ToDisplayXRayMatLineDataBundles(maxNum);
                maxNum -= imageScanLines.Count;
                if (ImageDictionary.ContainsKey(imagePath.Value) && (ImageDictionary[imagePath.Value] == null || ImageDictionary[imagePath.Value].IsManualSaved))
                {
                    var view1Rect = PaintingRegionsService.Service.GetMarkRegionsFromXRay(image, DetectViewIndex.View1, maxNum);
                    var view2Rect = PaintingRegionsService.Service.GetMarkRegionsFromXRay(image, DetectViewIndex.View2, maxNum);
                    if (DrawRectAction != null)
                    {
                        if (view1Rect.Count > 0)
                            DrawRectAction(new KeyValuePair<DetectViewIndex, List<MarkerRegion>>(DetectViewIndex.View1, view1Rect));
                        if (view2Rect.Count > 0)
                            DrawRectAction(new KeyValuePair<DetectViewIndex, List<MarkerRegion>>(DetectViewIndex.View2, view2Rect));
                    }
                }
                PaintingRegionsService.Service.AddXRayFileInfo(new XRayFileInfoForMark()
                {
                    ImageRecord = ImageDictionary.ContainsKey(imagePath.Value) ? ImageDictionary[imagePath.Value] : null,
                    FilePath = imagePath.Value,
                    StartLineNumber = maxNum,
                    EndLineNumber = maxNum - imageScanLines.Count 
                });

                var newImageNode = new ReplayingNavtigatableImage(imagePath.Value, imageScanLines);
                _selectedImages.AddFirst(newImageNode);
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
                Tracer.TraceError("Failed to load file from disk for replaying.");
                throw;
            }
        }

        /// <summary>
        /// 释放部分已加载图像缓存：随着回放的进行，_selectedImages会逐渐累积，为了避免过度占用内存，
        /// 需要及时清理没有显示的图像
        /// <remarks>在回放时回拉或前拉结束以后，即有新数据填充进来时，调用此函数清理内存</remarks>
        /// </summary>
        private void ReleaseSomeLoadedImages()
        {
            // 及时释放内存，仅保留正在显示的图像。
            // 清理前面的图像
            while (_selectedImages.First.Value.MinLineNumber > ShowingMaxNumber)
            {
                //清除TIP区域链表中要删除图像中的TIP区域，图像中的标记框在标记框不再显示后即被删除
                //DeleteTipCadRegion(_selectedImages.First.Value);

                _selectedImages.RemoveFirst();
            }
            // 清理后面的图像
            while (_selectedImages.Last.Value.MaxLineNumber < ShowingMinNumber)
            {
                //清除TIP区域链表中要删除图像中的TIP区域
                //DeleteTipCadRegion(_selectedImages.Last.Value);

                _selectedImages.RemoveLast();
            }
        }

        public void UpdateShowingRange(int minNum, int maxNum)
        {
            ShowingMinNumber = minNum;
            ShowingMaxNumber = maxNum;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public IEnumerable<DisplayScanlineDataBundle> PullBack()
        {
            var result = new List<DisplayScanlineDataBundle>();

            {
                var lastImage = _selectedImages.Last;
                while (lastImage != null && (lastImage.Value.Empty || 
                    lastImage.Value.MinLineNumber >= ShowingMinNumber))
                {
                    // 没有更多图像了，需要重新读取一个图像，理论上这里应该肯定为null
                    if (lastImage.Next == null)
                    {
                        LoadNextImage();
                    }

                    if (lastImage.Next == null)
                    {
                        // 已经没有图像文件了，直接返回null
                        return null;
                    }

                    lastImage = lastImage.Next;
                }

                // 根据上面的逻辑，到此处：lastImage肯定不为空，并且一定是找到的目标图像
                if (lastImage == null)
                {
                    return result;
                }

                ////////////////////////////////////////////////////////////////////////////////////////
                // 找到第一个匹配数据编号范围的图像，开始取回拉数据

                // 从此图像的最后一列（最大编号）开始搜索
                var lastLine = lastImage.Value.ScanLines.Last;

                // 从头部开始搜索，直到移动至编号大于ShowingMinNumber的第一线数据
                while (lastLine != null && lastLine.Value.LineNumber >= ShowingMinNumber)
                {
                    lastLine = lastLine.Previous;
                }

                // 取回拉数据，结果中编号从大往小排列
                while (lastLine != null)
                {
                    result.Add(lastLine.Value);

                    // 继续往前搜索
                    lastLine = lastLine.Previous;
                }

            }

            // 更新当前显示的最小编号和最大编号，最小编号和最大编号都减去添加的新数据的线数
            ShowingMaxNumber -= result.Count;
            ShowingMinNumber -= result.Count;
            ReleaseSomeLoadedImages();
            return result;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public IEnumerable<DisplayScanlineDataBundle> PullForward()
        {
            var result = new List<DisplayScanlineDataBundle>();
            if (ShowingMinNumber >= 0)
            {
                // 说明已经没有较新的图像可以显示了，直接返回null
                return null;
            }

            var lastImage = _selectedImages.First;
            while (lastImage != null && (lastImage.Value.Empty || lastImage.Value.MaxLineNumber <= ShowingMaxNumber))
            {
                // 没有更多图像了，需要重新读取一个图像
                if (lastImage.Previous == null)
                {
                    LoadPreviousImage();
                }

                if (lastImage.Previous == null)
                {
                    // 已经没有图像文件了，直接返回null
                    return result;
                }

                lastImage = lastImage.Previous;
            }

            if (lastImage == null)
            {
                return result;
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            // 找到第一个匹配数据编号范围的图像，开始取回拉数据

            // 从此图像的第一列（编号最小）开始搜索
            var lastLine = lastImage.Value.ScanLines.First;
            

            // 从头部开始搜索，直到移动至编号小于ShowingMinNumber的第一线数据
            while (lastLine != null && lastLine.Value.LineNumber <= ShowingMaxNumber)
            {
                lastLine = lastLine.Next;
            }

            // 取回拉数据，结果中编号从大往小排列
            while (lastLine != null)
            {
                result.Add(lastLine.Value);

                // 继续往前搜索
                lastLine = lastLine.Next;
            }

            ////////非一次性加载所有图像文件/////////////////////////////////////////////
            // 更新当前显示的最小编号和最大编号，最小编号和最大编号都加上添加的新数据的线数
            ShowingMaxNumber += result.Count;
            ShowingMinNumber += result.Count;
            ReleaseSomeLoadedImages();
            return result;
        }


        public IEnumerable<ImageRecord> GetShowingImages()
        {
            throw new NotImplementedException();
        }
    }
}
