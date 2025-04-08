using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using UI.XRay.Business.Entities;
using UI.XRay.Common.Utilities;
using UI.XRay.Flows.Controllers;

namespace TipImageGenerator
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 当前显示的图像的路径
        /// </summary>
        private string _imagePath;

        private XRayScanlinesImage _currentImage;

        /// <summary>
        /// 用于判断背景点的阈值
        /// </summary>
        private const int Threshold = 58000;

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 打开图像
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "XRay image | *.xray";

            if (dlg.ShowDialog() == true)
            {
                _imagePath = dlg.FileName;
                LoadAndShowImage();
            }
        }

        private void LoadAndShowImage()
        {
            _currentImage = XRayScanlinesImage.LoadFromDiskFile(_imagePath);
            ShowImage(_currentImage);
        }

        private void ShowImage(XRayScanlinesImage image)
        {
            if (image != null)
            {
                var processor = new XRayImageProcessor();
                processor.AttachImageData(image.View1Data);

                var bmp = processor.GetBitmap();
                bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                Image.Source = BitmapHelper.ConvertToBitmapImage(bmp);
            }
        }

        /// <summary>
        /// 将当前图像存储为tip图像：（去除空白区域）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveTipMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (_currentImage != null)
            {
                var dlg = new SaveFileDialog();
                dlg.Filter = "XRay image | *.xray";
                if (dlg.ShowDialog() == true)
                {
                    XRayScanlinesImage.SaveToDiskFile(_currentImage, dlg.FileName);
                }
            }
        }

        /// <summary>
        /// 裁剪当前图像
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClipMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (_currentImage != null)
            {
                Clip();
                //ShowImage(_currentImage);
            }
        }

        /// <summary>
        /// 裁剪当前图像
        /// </summary>
        private void Clip()
        {
            if (_currentImage != null)
            {
                if (_currentImage.View1Data != null)
                {
                    ClipLines(_currentImage.View1Data.ScanLines);
                }
            }
        }

        /// <summary>
        /// 裁剪扫描线：去掉空白区域，保留一个包含物体的矩形区域图像
        /// </summary>
        /// <param name="srcLines"></param>
        private void ClipLines(ClassifiedLineData[] srcLines)
        {
            // 寻找物体图像起始列
            int i = 0;
            for (i = 0; i < srcLines.Length; i++)
            {
                if (!IsAirLine(srcLines[i]))
                {
                    // 找到空白与物体边界
                    break;
                }
            }

            // 物体图像区域起始列编号：往前推移两线，即保留一点空白
            var startLineIndex = Math.Max(i-2, 0);

            // 寻找物体图像结束列
            int j = 0;
            for (j = srcLines.Length-1; j >=i; j--)
            {
                if (!IsAirLine(srcLines[j]))
                {
                    // 找到空白与物体边界
                    break;
                }
            }

            // 物体图像区域结束列编号
            var endLineIndex = Math.Min(j + 2, srcLines.Length - 1);

            // 将包含物体区域的图像列，全部加入结果队列
            // resultLines 中已经去除了物体图像前后的空白列
            var resultLines = new List<ClassifiedLineData>();
            for (var n=startLineIndex; n <= endLineIndex; n++)
            {
                resultLines.Add(srcLines[n]);
            }

            var clippedByChannels = RemoveWhiteChannels(resultLines);

            var bundleList = new List<ClassifiedLineDataBundle>();

            foreach (var line in clippedByChannels)
            {
                bundleList.Add(new ClassifiedLineDataBundle(line, null));
            }

            // 显示结果图像
            _currentImage = new XRayScanlinesImage(bundleList);
            var processor = new XRayImageProcessor();
            processor.AttachImageData(_currentImage.View1Data);
            _currentImage.View1Data.Thumbnail = XRayImageHelper.GenerateThumbnail(processor.GetBitmap());

            ShowImage(_currentImage);
        }

        /// <summary>
        /// 移除输入图像列中的空白探测通道，输出为包含有效物体区域的最小矩形的数据
        /// </summary>
        /// <param name="sourceLines"></param>
        /// <returns></returns>
        private List<ClassifiedLineData> RemoveWhiteChannels(List<ClassifiedLineData> sourceLines)
        {
            var channelsCount = sourceLines[0].XRayData.Length;
            var objStartChannel = -1;
            var objEndChannel = -1;

            var backGround = Threshold;

            // 从0号探测通道开始，寻找物体图像起始探测通道号
            for (int c = 0; c < channelsCount; c++)
            {
                for (int j = 0; j < sourceLines.Count-1; j+=2)
                {
                    var lineData = sourceLines[j].XRayData;
                    var nextLineData = sourceLines[j+1].XRayData;

                    // 发现连续两线数据阈值满足要求，认定为找到物体边界
                    if (lineData[c] <= backGround && nextLineData[c] <= backGround)
                    {
                        objStartChannel = c;
                        break;
                    }
                }

                if (objStartChannel != -1)
                {
                    break;
                }
            }

            // 往前退2线，保留一点空白
            objStartChannel = Math.Max(0, objStartChannel - 2);

            // 从最后一个探测通道开始，寻找物体图像起始探测通道号
            for (int c = channelsCount-1; c >=0; c--)
            {
                for (int j = 0; j < sourceLines.Count - 1; j += 2)
                {
                    var lineData = sourceLines[j].XRayData;
                    var nextLineData = sourceLines[j + 1].XRayData;

                    // 发现连续两线数据阈值满足要求，认定为找到物体边界
                    if (lineData[c] <= backGround && nextLineData[c] <= backGround)
                    {
                        objEndChannel = c;
                        break;
                    }
                }

                if (objEndChannel != -1)
                {
                    break;
                }
            }

            // 保留一点空白
            objEndChannel = Math.Min(objEndChannel + 2, channelsCount - 1);

            if (objEndChannel > objStartChannel)
            {
                return ClipByChannelsRange(sourceLines, objStartChannel, objEndChannel);
            }
            else
            {
                return sourceLines;
            }
        }

        /// <summary>
        /// 根据起止探测通道号，从输入的图像列中，截取出包含物体部分的区域，抛弃起止探测通道号之外的数据
        /// </summary>
        /// <param name="sourceLines"></param>
        /// <param name="startChannel"></param>
        /// <param name="endChannel"></param>
        /// <returns></returns>
        private List<ClassifiedLineData> ClipByChannelsRange(List<ClassifiedLineData> sourceLines, int startChannel,
            int endChannel)
        {
            var destLines = new List<ClassifiedLineData>(sourceLines.Count);

            var destLineLength = endChannel - startChannel + 1;

            foreach (var sourceLine in sourceLines)
            {
                var data = new ushort[destLineLength];
                var dataEnhanced = new ushort[destLineLength];
                var mat = new ushort[destLineLength];

                Array.Copy(sourceLine.XRayData, startChannel, data, 0, destLineLength);
                Array.Copy(sourceLine.Material, startChannel, mat, 0, destLineLength);
                if (sourceLine.XRayDataEnhanced != null)
                {
                    Array.Copy(sourceLine.XRayDataEnhanced, startChannel, dataEnhanced, 0, destLineLength);
                }

                var newLine = new ClassifiedLineData(sourceLine.ViewIndex, data,dataEnhanced, mat, sourceLine.IsAir);

                destLines.Add(newLine);
            }

            return destLines;
        }

        /// <summary>
        /// 判断一线数据是否为空白
        // 如果发现连续两个点小于阈值，则认定为不是空白线
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private bool IsAirLine(ClassifiedLineData line)
        {
            var data = line.XRayData;

            for (int j = 0; j < data.Length - 1; j += 2)
            {
                if (data[j] <= Threshold && data[j + 1] <= Threshold)
                {
                    return false;
                }
            }

            return true;
        }

        private void DiscardMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            LoadAndShowImage();
        }
    }
}
