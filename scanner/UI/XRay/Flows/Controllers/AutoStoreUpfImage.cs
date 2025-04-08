using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Common.Utilities;
using UI.XRay.Flows.Services;

namespace UI.XRay.Flows.Controllers
{
    /// <summary>
    /// 将xray图像自动保存成通用格式的图像,保存到指定目录
    /// </summary>
    public class AutoStoreUpfImage
    {
        public enum StoreImageStrategy
        {
            //可疑物
            Suspicious,
            //全部保存
            All
        }

        /// <summary>
        /// 保存通用格式图片的时候是否将原始Xray图像也保存进来，todo：有些客户可能需要xray图像
        /// </summary>
        private bool _autoStoreXrayImage = false;

        private const string AutoStoreUpfDefaultPath = @"D:\SecurityScanner\UPFImages";

        private string _autoStoreUpfImagePath = AutoStoreUpfDefaultPath;

        private StoreImageStrategy _autoStoreUpfImageStrategy = StoreImageStrategy.All;

        private ImageFormat _storeImageFormat = ImageFormat.Jpeg;

        private bool _mergeTwoViewImage = true;

        /// <summary>
        /// string：xray文件全路径；Xraycadregions：此图像包含的所有探测区域，后续可能需要将探测区域
        /// 绘制到图像中
        /// </summary>
        private readonly ConcurrentQueue<XrayImageInfo> _queue = new ConcurrentQueue<XrayImageInfo>();

        /// <summary>
        /// 自动保存的线程
        /// </summary>
        private readonly Task _saveImageTask;
        /// <summary>
        /// 线程退出标志
        /// </summary>
        private bool _exit = false;
        /// <summary>
        /// 视角1处理器
        /// </summary>
        private readonly XRayImageProcessor _xRayImageProcessor1;

        /// <summary>
        /// 视角2处理器
        /// </summary>
        private readonly XRayImageProcessor _xRayImageProcessor2 = null;

        /// <summary>
        /// 锁定保存路径的lock
        /// </summary>
        private readonly object _lockPath = new object();

        private readonly FileWatcher _fileWatcher;

        /// <summary>
        /// 是否限制自动保存通用格式图像的个数
        /// </summary>
        private bool _limitAutoStoreUpfImageCount;
        /// <summary>
        /// 自动保存通用格式图像的个数阈值
        /// </summary>
        private int _autoStoreUpfImageCount;

        private int _viewCount;

        /// <summary>
        /// ImageFormat格式的tostring和默认的图像保存扩展名不一致，保存对照
        /// </summary>
        private readonly Dictionary<ImageFormat, string> _imageFormatToString = new Dictionary<ImageFormat, string>(); 

        private readonly Dictionary<string,ImageFormat> _stringToImageFormat = new Dictionary<string, ImageFormat>();

        public AutoStoreUpfImage()
        {
            _imageFormatToString.Add(ImageFormat.Jpeg, "jpg");
            _imageFormatToString.Add(ImageFormat.Png, "png");
            _imageFormatToString.Add(ImageFormat.Bmp, "bmp");
            _imageFormatToString.Add(ImageFormat.Tiff, "tiff");

            _stringToImageFormat.Add("jpg", ImageFormat.Jpeg);
            _stringToImageFormat.Add("png", ImageFormat.Png);
            _stringToImageFormat.Add("bmp", ImageFormat.Bmp);
            _stringToImageFormat.Add("tiff", ImageFormat.Tiff);

            //加载配置
            LoadSettings();

            //订阅配置变化事件
            ScannerConfig.ConfigChanged += ScannerConfig_ConfigChanged;

            //如果不启用保存策略为null，则
            //创建目录
            CreateDirectory();

            if (_limitAutoStoreUpfImageCount)
            {
                _fileWatcher = new FileWatcher(_autoStoreUpfImagePath, GetAutoStoreUpfImageCount());
            }

            //初始化图像1处理器
            _xRayImageProcessor1 = new XRayImageProcessor();

            //初始化线程
            _saveImageTask = Task.Factory.StartNew(SaveImageTask);
        }

        int GetAutoStoreUpfImageCount()
        {
            var limitCount = _autoStoreUpfImageCount;
            if (_viewCount == 2)
            {
                if (_autoStoreXrayImage)
                {
                    if (_mergeTwoViewImage)
                    {
                        limitCount = limitCount*5;
                    }
                    else
                    {
                        limitCount = limitCount*6;
                    }
                }
                else
                {
                    if (!_mergeTwoViewImage)
                    {
                        limitCount = limitCount*5;
                    }
                }
            }
            else if(_autoStoreXrayImage)
            {
                limitCount = limitCount*5;
            }
            return limitCount;
        }

        //配置项变化时重新加载文件路径
        void ScannerConfig_ConfigChanged(object sender, EventArgs e)
        {
            var oldPath = _autoStoreUpfImagePath;

            lock (_lockPath)
            {
                try
                {
                    if (ScannerConfig.Read(ConfigPath.AutoStoreUpfImagePath, out _autoStoreUpfImagePath))
                    {
                        _autoStoreUpfImagePath = oldPath;
                    }
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception);
                    _autoStoreUpfImagePath = oldPath;
                }
                CreateDirectory();
            }
        }

        public void Enqueue(XrayImageInfo info)
        {
            if (info.FitStrategy(_autoStoreUpfImageStrategy))
            {
                _queue.Enqueue(info);
            }
        }

        private void SaveImageTask()
        {
            while (!_exit)
            {
                XrayImageInfo xrayImageInfo;
                while (_queue.TryDequeue(out xrayImageInfo))
                {
                    if (!IsFileInUse(xrayImageInfo.SaveFullPath))
                    {
                        SaveImage(xrayImageInfo);
                    }
                    else
                    {
                        _queue.Enqueue(xrayImageInfo);
                    }
                }
                Thread.Sleep(500);
            }
        }

        private bool IsFileInUse(string fileName)
        {
            bool inUse = true;

            FileStream fs = null;
            try
            {

                fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.None); inUse = false;
            }
            catch
            {
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }
            return inUse;//true表示正在使用,false没有使用  
        } 

        private void SaveImage(XrayImageInfo info)
        {
            if (info == null) return;
            try
            {
                //临时保存路径的变量
                string storeDir = AutoStoreUpfDefaultPath;

                string fileName = Path.GetFileName(info.SaveFullPath);
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(info.SaveFullPath);
                    if (fileNameWithoutExt != null)
                    {
                        lock (_lockPath)
                        {
                            storeDir = _autoStoreUpfImagePath;
                        }

                        //var destFullPathWithoutExt = Path.Combine(storeDir, fileNameWithoutExt);

                        //var bmpView1 = GetViewBmp(info.XrayImage.View1Data, _xRayImageProcessor1);
                        //var bmpView2 = GetViewBmp(info.XrayImage.View2Data, _xRayImageProcessor2);

                        ////如果需要拼接两视角图像
                        //if (_mergeTwoViewImage)
                        //{
                        //    var bmp = BitmapHelper.CombineBmp(bmpView1, bmpView2);
                        //    if (bmp != null)
                        //    {
                        //        bmp.Save(destFullPathWithoutExt + "." + _imageFormatToString[_storeImageFormat], _storeImageFormat);
                        //    }
                        //}
                        //else
                        //{
                        //    if (bmpView1 != null)
                        //    {
                        //        bmpView1.Save(destFullPathWithoutExt + "." + _imageFormatToString[_storeImageFormat],
                        //            _storeImageFormat);
                        //    }
                        //    if (bmpView2 != null)
                        //    {
                        //        bmpView2.Save(destFullPathWithoutExt + "View2" + "." + _imageFormatToString[_storeImageFormat],
                        //            _storeImageFormat);
                        //    }
                        //}
                        Save4ImagesService.Service.SaveMarkedImageToDisk(new List<PaintingRectangle> { new PaintingRectangle() { StorePath = info.SaveFullPath } }, _storeImageFormat); 
                    }

                    //如果需要自动保存xray图像到文件夹
                    if (_autoStoreXrayImage)
                    {
                        File.Copy(info.SaveFullPath, Path.Combine(storeDir, fileName));
                    }
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }

        private Bitmap GetViewBmp(ImageViewData viewData, XRayImageProcessor processor)
        {
            if (viewData != null)
            {
                if (processor == null)
                {
                    processor = new XRayImageProcessor();
                }
                processor.AttachImageData(viewData);
                return processor.GetBitmap();
            }
            return null;
        }

        private void CreateDirectory()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_autoStoreUpfImagePath))
                {
                    _autoStoreUpfImagePath = AutoStoreUpfDefaultPath;
                }
                if (!Directory.Exists(_autoStoreUpfImagePath))
                {
                    Directory.CreateDirectory(_autoStoreUpfImagePath);
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }

        private void LoadSettings()
        {
            try
            {
                ScannerConfig.Read(ConfigPath.AutoStoreXrayImage, out _autoStoreXrayImage);
                ScannerConfig.Read(ConfigPath.AutoStoreUpfImagePath, out _autoStoreUpfImagePath);
                ScannerConfig.Read(ConfigPath.AutoStoreUpfImageStrategy, out _autoStoreUpfImageStrategy);
                //ScannerConfig.Read(ConfigPath.MergeTwoViewImage, out _mergeTwoViewImage);
                string imageFormat;
                ScannerConfig.Read(ConfigPath.AutoStoreUpfImageFormat, out imageFormat);
                _storeImageFormat = _stringToImageFormat[imageFormat];

                ScannerConfig.Read(ConfigPath.LimitAutoStoreUpfImageCount, out _limitAutoStoreUpfImageCount);
                ScannerConfig.Read(ConfigPath.AutoStoreUpfImageCount, out _autoStoreUpfImageCount);
                ScannerConfig.Read(ConfigPath.MachineViewsCount, out _viewCount);
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }

        public void Shutdown()
        {
            _exit = true;
            _saveImageTask.Wait();
            if (_fileWatcher != null)
            {
                _fileWatcher.Shutdown();
            }
        }
    }

    /// <summary>
    /// XrayImage图像信息类
    /// </summary>
    public class XrayImageInfo
    {
        public string SaveFullPath { get; private set; }

        public XrayCadRegions XrayCadRegions { get; private set; }

        public XRayScanlinesImage XrayImage { get; private set; }

        public XrayImageInfo(string fullPath, XrayCadRegions cadRegions, XRayScanlinesImage image)
        {
            SaveFullPath = fullPath;
            XrayCadRegions = cadRegions;
            XrayImage = image;
        }

        public bool FitStrategy(AutoStoreUpfImage.StoreImageStrategy storeImageStrategy)
        {
            return storeImageStrategy == AutoStoreUpfImage.StoreImageStrategy.All || XrayCadRegions.MarkerRegionsDetected;
        }
    }
}
