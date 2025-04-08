using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using UI.XRay.Business.Algo;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Services;
using UI.XRay.ImagePlant;
using UI.XRay.ImagePlant.Gpu;
using UI.XRay.RenderEngine;
using Color = System.Drawing.Color;

namespace UI.XRay.Security.Scanner
{
    /// <summary>
    /// RenderEngine22.xaml 的交互逻辑
    /// </summary>
    public partial class RenderEngine22 : Window
    {

        private float HorizonalScale1;
        private float HorizonalScale2;
        private int View1Channels;
        private int View2Channels;
        public void LoadSettings()
        {
            if(!ScannerConfig.Read(ConfigPath.ImagesImage1HorizonalScale,out HorizonalScale1))
            {
                HorizonalScale1 = 1.0f;
            }
            if (!ScannerConfig.Read(ConfigPath.ImagesImage2HorizonalScale, out HorizonalScale2))
            {
                HorizonalScale2 = 1.0f;
            }
            if (!ScannerConfig.Read(ConfigPath.ImagesImage1Height, out View1Channels))
            {
                View1Channels=832;
            }
            if (!ScannerConfig.Read(ConfigPath.ImagesImage2Height, out View2Channels))
            {
                View2Channels=780;
            }
        }
        public RenderEngine22()
        {
            LoadSettings();
            InitializeComponent();
           
            var image1Setting = new RollingImageSetting(
               View1Channels,
               1,
               HorizonalScale1,
               false,
               false,
               true,
               false,
               null,
               ImageAnchor.Center);
            var image2Setting = 2 > 1 ? new RollingImageSetting(
                View2Channels,
                1,
                HorizonalScale2,
                false,
                false,
                true,
                false,
                null,
                ImageAnchor.Center) : null;
            _controller = ImageControl.Initialize(1920, 922);
            _controller.Initialize(image1Setting, image2Setting);
            _controller.RightToLeft = false;
            _controller.CanManualDraw = true;
            _controller.Image1.ColorMode = DisplayColorMode.MaterialColor; 
            if(_controller.Image2!=null)
            _controller.Image2.ColorMode = DisplayColorMode.MaterialColor;
            
            _controller.GetImageLines += ControllerOnGetImageLines;
            _controller.StartService();
            ImportXRayFile(Transmission.ImagePath1);

        }

        private IRollingImageProcessController _controller;
        private object _object = new object();
        private readonly ConcurrentQueue<DisplayScanlineData> _view1ImageCache =
        new ConcurrentQueue<DisplayScanlineData>();

        private readonly ConcurrentQueue<DisplayScanlineData> _view2ImageCache =
           new ConcurrentQueue<DisplayScanlineData>();

        string _currentImagePath = string.Empty;
        public int ShowingLinesCount { get; private set; }
        readonly DataProcessInAirport2 calc = new DataProcessInAirport2();
       
        private List<DisplayScanlineDataBundle> _lines = new List<DisplayScanlineDataBundle>();
        private List<DisplayScanlineDataBundle> _allLines = new List<DisplayScanlineDataBundle>();
        public LinkedList<DisplayScanlineData> CurrentScreenView1ScanLines = new LinkedList<DisplayScanlineData>();
        public LinkedList<DisplayScanlineData> CurrentScreenView2ScanLines = new LinkedList<DisplayScanlineData>();
        private List<DisplayScanlineDataBundle> imageScanLine = new List<DisplayScanlineDataBundle>();
        int _lineNumber = 0;

        int maxNum = 0;

        private int MaxLinesCount { get; set; }
        public int MinLineNumber { get; private set; }

        /// <summary>
        /// 当前绘制的扫描线的最大编号
        /// </summary>
        public int MaxLineNumber { get; private set; }

        /// <summary>
        /// 当前正在显示的图像列的实际数量
        /// </summary>
        /// 
        bool ReverseAppending = false;
        public int CurrentScreenLinesThreashold
        {
            get
            {
                if (_controller.Image2 != null)
                {
                    return (int)(_controller.Width / 2 / HorizonalScale1);
                }
                else
                {
                    return (int)(_controller.Width / HorizonalScale1);
                }
            }
        }

        private DateTime _addNewLinesDateTime = DateTime.Now;
        private void ClearScreen()
        {
            MinLineNumber = 0;
            MaxLineNumber = 0;
            ShowingLinesCount = 0;
            _controller.ClearImages(null, null, 0);
            _controller.MaxLineNumber = 0;
            _controller.MinLineNumber = 0;
            if (_controller.MarkerList != null)
                _controller.MarkerList.Clear();
        }

        public IEnumerable<DisplayScanlineDataBundle> GetLinesFromImageFile(string path)
        {
            var result = new List<DisplayScanlineDataBundle>();
            // 图像数据线的最大编号

            //for (int i = 0; i < path.Count(); i++)
            {
                var image = XRayScanlinesImage.LoadFromDiskFile(path);
                var alllines = image.ToDisplayXRayMatLineDataBundles(maxNum);
                maxNum += image.View1Data.ScanLines.Count();
                result.AddRange(alllines);
                imageScanLine.AddRange(alllines);
            }

            return result;
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
            }
            if (bundle.View2Data != null)
            {
                _view2ImageCache.Enqueue(bundle.View2Data);
            }
        }
        public void AppendLines(List<DisplayScanlineDataBundle> lines)
        {
            foreach (var bundle in lines)
            {
                lock (_object)
                {
                    //  bpo.RemoveBadPointOper(bundle);
                    AppendSingleLine(bundle);
                }
            }
        }
        
        private void ControllerOnGetImageLines(object sender, GetImageLinesEventArgs args)
        {
            
                lock (_object)
                {
                    GetScanningData(args);
                }
            
        }
        private void AppendLineToCurrentScreen(DisplayScanlineData data, DetectViewIndex view, bool ReverseAppending)
        {
            if (data != null)
            {
                if (ReverseAppending)
                {
                    if (view == DetectViewIndex.View1)
                    {
                        CurrentScreenView1ScanLines.AddFirst(data);
                        if (CurrentScreenView1ScanLines.Count > CurrentScreenLinesThreashold)
                            CurrentScreenView1ScanLines.RemoveLast();
                    }
                    else
                    {
                        if (data != null)
                        {
                            CurrentScreenView2ScanLines.AddFirst(data);
                            if (CurrentScreenView2ScanLines.Count > CurrentScreenLinesThreashold)
                                CurrentScreenView2ScanLines.RemoveLast();
                        }
                    }
                }
                else
                {
                    if (view == DetectViewIndex.View1)
                    {
                        CurrentScreenView1ScanLines.AddLast(data);
                        if (CurrentScreenView1ScanLines.Count > CurrentScreenLinesThreashold)
                            CurrentScreenView1ScanLines.RemoveFirst();
                    }
                    else
                    {
                        if (data != null)
                        {
                            CurrentScreenView2ScanLines.AddLast(data);
                            if (CurrentScreenView2ScanLines.Count > CurrentScreenLinesThreashold)
                                CurrentScreenView2ScanLines.RemoveFirst();
                        }
                    }
                }
            }
        }

        private IEnumerable<DisplayScanlineData> AdjustScanLinesDataLen(IEnumerable<DisplayScanlineData> scanLines, DetectViewIndex viewIndex)
        {
            int channelsCount = View1Channels;
            if (viewIndex == DetectViewIndex.View2)
                channelsCount = View2Channels;

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

                    var matLineData = new ClassifiedLineData(line.ViewIndex, channelsCount) { IsAir = line.IsAir };

                    // 截取拷贝实际的图像线数据，包括高低能融合数据和颜色
                    Array.Copy(line.XRayData, 0, matLineData.XRayData, 0, channelsCount);
                    if (line.XRayDataEnhanced != null)
                    {
                        Array.Copy(line.XRayDataEnhanced, 0, matLineData.XRayDataEnhanced, 0, channelsCount);
                    }
                    else
                    {
                        matLineData.XRayDataEnhanced = null;
                    }

                    Array.Copy(line.ColorIndex, 0, colorIndex, 0, channelsCount);

                    lenAdjustedScanLines.Add(new DisplayScanlineData(matLineData, colorIndex, line.LineNumber));
                }
                else if (line.XRayData.Length < channelsCount)
                {
                    // 实际数据通道数小于配置（即要显示）的通道数，则实际数据居中，上下补空白
                    var colorIndex = new ushort[channelsCount];
                    var matLineData = new ClassifiedLineData(line.ViewIndex, channelsCount) { IsAir = line.IsAir };
                    int marginChannelsCount = ((channelsCount - line.XRayData.Length) >> 1);
                    // 填充上下（无数据的）置白点
                    for (int i = 0; i < marginChannelsCount; i++)
                    {
                        matLineData.XRayData[i] = 65530;
                        matLineData.XRayData[channelsCount - 1 - i] = 65530;
                        matLineData.Material[i] = 50;
                        matLineData.Material[channelsCount - 1 - i] = 50;
                        colorIndex[i] = 50;
                        colorIndex[channelsCount - 1 - i] = 50;
                        if (line.XRayDataEnhanced != null)
                        {
                            matLineData.XRayDataEnhanced[i] = 65530;
                            matLineData.XRayDataEnhanced[channelsCount - 1 - i] = 65530;
                        }
                    }
                    // 拷贝实际的线数据，包括高低能融合数据和颜色
                    Array.Copy(line.XRayData, 0, matLineData.XRayData, marginChannelsCount, line.XRayData.Length);
                    Array.Copy(line.ColorIndex, 0, colorIndex, marginChannelsCount, line.XRayData.Length);
                    Array.Copy(line.Material, 0, matLineData.Material, marginChannelsCount, line.XRayData.Length);
                    if (line.XRayDataEnhanced != null)
                    {
                        Array.Copy(line.XRayDataEnhanced, 0, matLineData.XRayDataEnhanced, marginChannelsCount, line.XRayData.Length);
                    }
                    else
                    {
                        Array.Copy(matLineData.XRayData, 0, matLineData.XRayDataEnhanced, 0, matLineData.XRayData.Length);
                    }
                    lenAdjustedScanLines.Add(new DisplayScanlineData(matLineData, colorIndex, line.LineNumber));
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
        private ushort[] ComposeXDataEnhancedAndColorIndex(DisplayScanlineData data)
        {
            ushort[] composed;
            if (data.XRayDataEnhanced == null)
            {
                composed = new ushort[data.XRayData.Length << 1];
                for (int i = 0; i < composed.Length; i += 2)
                {
                    composed[i] = data.XRayData[i >> 1];
                    composed[i + 1] = data.ColorIndex[i >> 1];
                }

                return composed;
            }
            else
            {
                composed = new ushort[data.XRayDataEnhanced.Length << 1];
                for (int i = 0; i < composed.Length; i += 2)
                {
                    composed[i] = data.XRayDataEnhanced[i >> 1];
                    composed[i + 1] = data.ColorIndex[i >> 1];
                }
            }


            return composed;
        }

        private void FillScanLinesToImage(IRollingImageUpdater image1, IRollingImageUpdater image2,
            List<DisplayScanlineData> scanLines, DetectViewIndex viewIndex)
        {
            // 将实际的数据长度调整为配置的数据长度
            var lenAdjustedScanLines = AdjustScanLinesDataLen(scanLines, viewIndex);
            var scanlineDataEnhanced = lenAdjustedScanLines.Select(ComposeXDataEnhancedAndColorIndex).ToList();

            // 将所有线数据，统一拷贝至缓冲区data2Fill中
            if (scanlineDataEnhanced.Count > 0)
            {
                var data2FillEnhanced = new ushort[scanlineDataEnhanced.Count * scanlineDataEnhanced[0].Length];
                if (ReverseAppending)
                {
                    for (int j = 0; j < scanlineDataEnhanced.Count; j++)
                    {
                        scanlineDataEnhanced[j].CopyTo(data2FillEnhanced, j * scanlineDataEnhanced[0].Length);
                    }
                    if (viewIndex == DetectViewIndex.View1)
                    {
                        if (image1 != null)
                        {
                            image1.ReverseAppendImageRows(data2FillEnhanced, scanlineDataEnhanced.Count);
                            image1.EndAppending();
                        }
                    }
                    else
                    {
                        if (image2 != null)
                        {
                            image2.ReverseAppendImageRows(data2FillEnhanced, scanlineDataEnhanced.Count);
                            image2.EndAppending();
                        }
                    }
                    if (viewIndex == DetectViewIndex.View1)
                    {
                        ShowingLinesCount += scanLines.Count;
                        ShowingLinesCount = Math.Min(ShowingLinesCount, MaxLinesCount);
                    }

                    // 对于反向填充，最后一线数据的编号最小
                    MinLineNumber = scanLines[scanLines.Count - 1].LineNumber;
                    MaxLineNumber = MinLineNumber + ShowingLinesCount - 1;
                }
                else
                {
                    // 对于正向填充：最后一线被填充的数据的编号最大，但是输入的数据是从小到大排序
                    for (int j = 0; j < scanlineDataEnhanced.Count; j++)
                    {
                        scanlineDataEnhanced[scanlineDataEnhanced.Count - 1 - j].CopyTo(data2FillEnhanced, j * scanlineDataEnhanced[0].Length);
                    }
                    if (viewIndex == DetectViewIndex.View1)
                    {
                        if (image1 != null)
                        {
                            image1.AppendImageRows(data2FillEnhanced, scanlineDataEnhanced.Count);
                            image1.EndAppending();
                        }
                    }
                    else
                    {
                        if (image2 != null)
                        {
                            image2.AppendImageRows(data2FillEnhanced, scanlineDataEnhanced.Count);
                            image2.EndAppending();
                        }
                    }
                    if (viewIndex == DetectViewIndex.View1)
                    {
                        ShowingLinesCount += scanLines.Count;
                        ShowingLinesCount = Math.Min(ShowingLinesCount, MaxLinesCount);
                    }

                    // 对于正向填充，最后一线数据的编号最大
                    MaxLineNumber = scanLines[scanLines.Count - 1].LineNumber;
                    MinLineNumber = MaxLineNumber - ShowingLinesCount + 1;
                }
                if (viewIndex == DetectViewIndex.View1)
                {
                    _controller.MaxLineNumber = MaxLineNumber;
                    _controller.MinLineNumber = MinLineNumber;
                }
            }
        }


        private void FillXRayViewImageCacheDataToImage(GetImageLinesEventArgs args,
            ConcurrentQueue<DisplayScanlineData> imageCache, DetectViewIndex viewIndex)
        {
            var scanLines = new List<DisplayScanlineData>();

            // 一次性取出本次需要填充的所有数据

            // 本次更新时，最后一列被更新的数据
            DisplayScanlineData lastLine = null;
            DisplayScanlineData line = null;
            int i = 0;
            while (i < 8 && imageCache.TryDequeue(out line))
            {
                lastLine = line;
                scanLines.Add(line);
                AppendLineToCurrentScreen(line, viewIndex, ReverseAppending);
                i++;
            }

            FillScanLinesToImage(args.Image1Updater, args.Image2Updater, scanLines, viewIndex);
            _addNewLinesDateTime = DateTime.Now;
        }


        private List<MarkerRegion> _view1Boxes = new List<MarkerRegion>();

        private List<MarkerRegion> _view2Boxes = new List<MarkerRegion>();
        protected void GetScanningData(GetImageLinesEventArgs args)
        {
            // 将未显示的图像数据填充至显存
            if (_view1ImageCache != null && _view1ImageCache.Count > 0)
            {
                // 填充视角1的图像：显示至图像1或图像2中
                FillXRayViewImageCacheDataToImage(args, _view1ImageCache, DetectViewIndex.View1);
            }

            // 将未显示的图像数据填充至显存
            if (_view2ImageCache != null && _view2ImageCache.Count > 0)
            {
                // 填充视角1的图像：显示至图像1或图像2中
                FillXRayViewImageCacheDataToImage(args, _view2ImageCache, DetectViewIndex.View2);
            }

            var image1 = args.Image1Updater;
            image1.EndAppending();

            List<MarkerBox> regions = new List<MarkerBox>();
            for (int i = 0; i < _view1Boxes.Count; i++)
            {
                regions.Add(new MarkerBox()
                {
                    ChannelEnd = _view1Boxes[i].ToChannel,
                    ChannelStart = _view1Boxes[i].FromChannel,
                    FromScanline = MaxLineNumber + imageScanLine.Count - _view1Boxes[i].FromLine,
                    ToScanline = MaxLineNumber + imageScanLine.Count - _view1Boxes[i].ToLine,
                    BorderColor = Color.FromKnownColor((KnownColor)42)
                });
            }
            image1.DrawMarkerBoxes(regions);

            var image2 = args.Image2Updater;
            if (image2 != null)
            {
                image2.EndAppending();

                regions = new List<MarkerBox>();
                for (int i = 0; i < _view2Boxes.Count; i++)
                {
                    regions.Add(new MarkerBox()
                    {
                        ChannelEnd = _view2Boxes[i].ToChannel,
                        ChannelStart = _view2Boxes[i].FromChannel,
                        FromScanline = MaxLineNumber + imageScanLine.Count - _view2Boxes[i].FromLine,
                        ToScanline = MaxLineNumber + imageScanLine.Count - _view2Boxes[i].ToLine,
                        BorderColor = Color.FromKnownColor((KnownColor)42)
                    });
                }
                image2.DrawMarkerBoxes(regions);
            }

            //var mb = new MarkerBox() { ChannelEnd =519, ChannelStart = 356, FromScanline = 300, ToScanline = 120, Tag = "knife", BorderColor = Color.Red };
            //    image1.DrawMarkerBoxes(new List<MarkerBox> { mb });
            //    image2.DrawMarkerBoxes(new List<MarkerBox> { mb });
        }
        private void ClearScreen(List<DisplayScanlineDataBundle> lines)
        {
            ShowingLinesCount = 0;
            MinLineNumber = 0;
            MaxLineNumber = 0;

            while (CurrentScreenView1ScanLines.Count > 0)
            {
                CurrentScreenView1ScanLines.RemoveFirst();
            }
            while (CurrentScreenView2ScanLines.Count > 0)
            {
                CurrentScreenView2ScanLines.RemoveFirst();
            }

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

            if (image1EnhancedData == null)
            {
                _controller.ClearImages(image1Data, image2Data, linesCount);
            }
            else
            {
                if (_isShowEnhanced)
                {
                    _controller.ClearImages(image1EnhancedData, image2EnhancedData, linesCount);
                }
                else
                {
                    _controller.ClearImages(image1Data, image2Data, linesCount);
                }
            }
        }
        private bool _isShowEnhanced = true;
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
                        scanlineData[scanlineData.Count - 1 - j].CopyTo(data2Fill, j * scanlineData[0].Length);
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
                        scanlineData[scanlineData.Count - 1 - j].CopyTo(data2Fill, j * scanlineData[0].Length);
                    }
                }

                return data2Fill;
            }

            return null;
        }

        public void ImportXRayFile(string imagepath)
        {
  
            try
            {
                if (File.Exists(imagepath))
                {
                    _allLines.Clear();
                    while (CurrentScreenView1ScanLines.Count > 0)
                    {
                        CurrentScreenView1ScanLines.RemoveFirst();
                    }
                    while (CurrentScreenView2ScanLines.Count > 0)
                    {
                        CurrentScreenView2ScanLines.RemoveFirst();
                    }
                      ClearScreen();
                    imageScanLine.Clear();
                    _lineNumber = 0;
                    maxNum = 0;
                    _lines = GetLinesFromImageFile(imagepath).ToList();
                    calc.CalcEnhancedData(_lines);
                    _allLines.AddRange(_lines);
                    // SaveImageWithRegionsService.Service.AddXRayFileInfo(new XRayFileInfo(imagepath, _lineNumber, _lineNumber + _lines.Count()));                    
                    _currentImagePath = imagepath;

                 //   AppendLines(_allLines);
                    ClearScreen(_lines);
                    
                }
            }
            catch (Exception ex)
            {

            }
        }

    

        private void Inverse_Click(object sender, RoutedEventArgs e)
        {
            var image1 = _controller.Image1;
            var image2 = _controller.Image2;
            image1.IsInversed = !image1.IsInversed;
            if (image2 != null)
            {
                image2.IsInversed = image1.IsInversed;
            }
        }

        private void SuperEnhance_Click(object sender, RoutedEventArgs e)
        {
            var image1 = _controller.Image1;
            var image2 = _controller.Image2;
            image1.IsSuperEnhanceEnabled = !image1.IsSuperEnhanceEnabled;
            if (image2 != null)
            {
                image2.IsSuperEnhanceEnabled = image1.IsSuperEnhanceEnabled;
            }
        }

        private void Recover_Click(object sender, RoutedEventArgs e)
        {
            var image1 = _controller.Image1;
            var image2 = _controller.Image2;
            image1.Zoom(1.0f);
            image1.ColorMode = DisplayColorMode.MaterialColor;
            image1.PenetrationMode = PenetrationMode.Standard;
            image1.IsInversed = false;
            image1.IsSuperEnhanceEnabled = false;
            image1.IsEdgeEnhanceEnabled = false;
            image1.AbsorptivityIndex = 0;
            image1.IsDynamicGrayTransformEnabled = false;

            if (image2 != null)
            {
                image2.Zoom(1.0f);
                image2.ColorMode = DisplayColorMode.MaterialColor;
                image2.PenetrationMode = PenetrationMode.Standard;
                image2.IsInversed = false;
                image2.IsSuperEnhanceEnabled = false;
                image2.IsEdgeEnhanceEnabled = false;
                image2.AbsorptivityIndex = 0;
                image2.IsDynamicGrayTransformEnabled = false;
            }
        }

        private void BW_Click(object sender, RoutedEventArgs e)
        {
            var image1 = _controller.Image1;
            var image2 = _controller.Image2;
            image1.ColorMode = image1.ColorMode == DisplayColorMode.MaterialColor
                         ? DisplayColorMode.Grey
                         : DisplayColorMode.MaterialColor;
            if (image2 != null)
            {
                image2.ColorMode = image1.ColorMode;
            }
            
        }

        private void HAHA_Click(object sender, RoutedEventArgs e)
        {
            var image1 = _controller.Image1;
            var image2 = _controller.Image2;
            image1.AbsorptivityIndex++;
            if (image2 != null)
            {
                image2.AbsorptivityIndex = image1.AbsorptivityIndex;
            }
        }

        private void XIXI_Click(object sender, RoutedEventArgs e)
        {
            var image1 = _controller.Image1;
            var image2 = _controller.Image2;
            image1.AbsorptivityIndex--;
            if (image2 != null)
            {
                image2.AbsorptivityIndex = image1.AbsorptivityIndex;
            }
        }

        private void HI_Click(object sender, RoutedEventArgs e)
        {
            var image1 = _controller.Image1;
            var image2 = _controller.Image2;
        
            
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

        private void Organic_Click(object sender, RoutedEventArgs e)
        {
            var image1 = _controller.Image1;
            var image2 = _controller.Image2;
            image1.ColorMode = DisplayColorMode.OS;
            if (image2 != null)
            {
                image2.ColorMode = image1.ColorMode;
            }
        }

        private void NonOrganic_Click(object sender, RoutedEventArgs e)
        {
            var image1 = _controller.Image1;
            var image2 = _controller.Image2;
            image1.ColorMode = DisplayColorMode.MS;
            if (image2 != null)
            {
                image2.ColorMode = image1.ColorMode;
            }
        }

        private void ZZ_Click(object sender, RoutedEventArgs e)
        {
            var image1 = _controller.Image1;
            var image2 = _controller.Image2;
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
        }
    }
}
