using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using UI.Common.Tracers;
using UI.XRay.Business.Algo;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Common.Utilities;
using UI.XRay.Flows.Services;
using UI.XRay.Flows.TRSNetwork;
using UI.XRay.Flows.TRSNetwork.Models;
using Emgu.CV;
using System.Text;
using UI.XRay.Flows.HttpServices;
using static UI.XRay.Flows.HttpServices.HttpAiJudgeServices;
using System.Threading.Tasks;
namespace UI.XRay.Flows.Controllers
{
    struct ExportJpgInfo
    {
        #region Fields
        public string path;                                 // 源文件路径
        public string fileNameWithoutExtension;             // 文件名
        public string destPath;                             // 目标地址
        public ImageFormat format;                          // 文件类型
        public string ext;                                  // 文件后缀
        public ExportImageEffects effect;                   // 导出类型
        public bool dumpToNet;                              // 是否是导出到网络的图片
        public int retryCount;                              // 重试保存次数
        #endregion

        #region Constructor
        public ExportJpgInfo(string path, string destPath, ImageFormat format, string ext, ExportImageEffects effect = ExportImageEffects.Regular,
            bool dumpToNet = false, int retryCount = 0)
        {
            this.path = path;
            this.fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            this.destPath = destPath;
            this.format = format;
            // TODO: ImageFormat.Jpeg这种东西居然不是常量或枚举,懒得判断所以我传入了
            this.ext = ext;
            this.effect = effect;
            this.dumpToNet = dumpToNet;
            this.retryCount = retryCount;
        }
        #endregion
    }

    public class Save4ImagesService
    {
        #region Instance & Constructors
        public static Save4ImagesService Service { get; private set; }

        static Save4ImagesService()
        {
            Service = new Save4ImagesService();
        }
        public Save4ImagesService()
        {
            if (!ScannerConfig.Read(ConfigPath.SystemDateFormat, out systemDateformat))
            {
                systemDateformat = 0;
            }
            if (!ScannerConfig.Read(ConfigPath.CommonImageQuality, out _imageQuality))
            {
                _imageQuality = 75;
            }
            if (!ScannerConfig.Read(ConfigPath.ImagesExportImage5Effect, out _image5Effect))
            {
                _image5Effect = ExportImageEffects.None;
            }
            Left2Right = !ConfigHelper.GetIsRight2Left();
            View1Vertical = !ConfigHelper.GetView1IsVerticalFlip();
            View2Vertical = !ConfigHelper.GetView2IsVerticalFlip();
            dp = new DataProcessInAirport2();
            ip = new XRayImageProcessor();

            _retrySaveQueue = new ConcurrentQueue<ExportJpgInfo>();
            Thread retrySaveThread = new Thread(RetrySave);
            retrySaveThread.IsBackground = true;
            retrySaveThread.Priority = ThreadPriority.BelowNormal;
            retrySaveThread.Start();

            _isSendSingleViewImage = ExchangeDirectionConfig.Service.IsSendSingleViewImage;
        }
        #endregion

        #region Fields & Properties
        private int systemDateformat;
        private int _imageQuality;
        private DataProcessInAirport2 dp;
        private XRayImageProcessor ip;
        private ExportImageEffects _image5Effect;

        // 用于重新保存或传输图片的队列
        private ConcurrentQueue<ExportJpgInfo> _retrySaveQueue;
        // 最大队列长度
        private int _maxQueueLength = 20;
        // 最大重试次数
        private int _maxRetryTimes = 5;
        // 结束重试标志
        private bool _exit = false;

        private bool _isSendSingleViewImage = false;

        public bool Left2Right { get; set; }
        public bool View1Vertical { get; set; }
        public bool View2Vertical { get; set; }

        XRayImageProcessor ip1 = new XRayImageProcessor();
        XRayImageProcessor ip2 = new XRayImageProcessor();

        #endregion

        #region Methods
        #region private
        /// <summary>
        /// 重新尝试保存队列中未保存/转存成功的图片
        /// </summary>
        private void RetrySave()
        {
            while (!_exit)
            {
                ExportJpgInfo eJI = new ExportJpgInfo();
                try
                {
                    while (_retrySaveQueue.TryDequeue(out eJI))
                    {
                        GetBitmapAndSave(eJI);
                        Thread.Sleep(50);
                    }
                }
                catch (Exception ex)
                {
                    Tracer.TraceError("[SaveImage] Exception occured when retrying save");
                    Tracer.TraceException(ex);
                }
                Thread.Sleep(500);
            }
        }

        /// <summary>
        /// 从XRay图片中获取两个视角的bmp图像并按要求保存
        /// </summary>
        /// <param name="img">传入的XRay图像</param>
        /// <param name="fileNameWithoutExtension">不包含扩展的文件名</param>
        /// <param name="destPath">目标地址</param>
        /// <param name="ext">文件后缀</param>
        /// <param name="format">文件类型</param>
        /// <param name="savedImage">用于向网络传输已保存的图片</param>
        /// <param name="effect">图片处理效果</param>
        /// <param name="dumpToNet">调用方是否为转存到网络</param>
        /// <param name="retryCount">重试保存的次数</param>
        private void GetBitmapAndSave(XRayScanlinesImage img, string path, string destPath, string ext, List<OperationRecord> records, ImageFormat format,
            List<XRayNetEntities.ScannerSavedImage> savedImage = null,
            ExportImageEffects effect = ExportImageEffects.Regular, bool dumpToNet = false, int retryCount = 0)
        {
            bool saveSucceeded = false;
            string effectString;
            switch (effect)
            {
                case ExportImageEffects.Regular:
                    effectString = "";
                    break;
                case ExportImageEffects.Absorptivity:
                    effectString = "_Lighten";
                    break;
                default:
                    effectString = "_" + effect.ToString();
                    break;
            }
            if (img == null)
            {
                img = XRayScanlinesImage.LoadFromDiskFile(path);
            }
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            string combineFilename = fileNameWithoutExtension + effectString + ext;

            try
            {

                Bitmap bmp1 = null;
                Bitmap bmp2 = null;

                bmp1 = ip1.GetBitmap(effect);

                if (img.View2Data != null)
                {
                    bmp2 = ip2.GetBitmap(effect);
                }

                if (bmp1 != null)
                {
                    if (bmp1 != null && View1Vertical)
                    {
                        bmp1.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    }
                    if (bmp2 != null && View2Vertical)
                    {
                        bmp2.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    }
                    if (Left2Right)
                    {
                        bmp1.RotateFlip(RotateFlipType.RotateNoneFlipX);
                        if (bmp2 != null)
                        {
                            bmp2.RotateFlip(RotateFlipType.RotateNoneFlipX);
                        }
                    }

                    var combinebmp = BitmapHelper.CombineBmp(bmp1, bmp2);
                    PrintInfomationOnImage.PrintOnImage(combinebmp, fileNameWithoutExtension, DateTime2String(img.ScanningTime));
                    //保存Jpg图像
                    BitmapHelper.ImageCompress(combinebmp, Path.Combine(destPath, combineFilename), format, _imageQuality);
                    if (!dumpToNet)
                    {
                        if (savedImage != null)
                            savedImage.Add(new XRayNetEntities.ScannerSavedImage(Path.Combine(destPath, combineFilename), XRayNetEntities.SavedImageType.ExtraImage));
                        saveSucceeded = true;

                        // TODO: 记日志保存导致网络存图失败
                        //new OperationRecordService().RecordOperation();
                        records.Add(new OperationRecord()
                        {
                            OperateUI = OperationUI.ImsPage,
                            OperateTime = DateTime.Now,
                            OperateObject = fileNameWithoutExtension + ".xray",
                            OperateCommand = OperationCommand.Saveto,
                            AccountId = PermissionService.Service.CurrentAccount != null ? PermissionService.Service.CurrentAccount.AccountId : string.Empty,
                            OperateContent = ConfigHelper.AddQuotationForPath(Path.Combine(destPath, combineFilename))
                        });
                    }
                    else
                    {
                        string combinePath = Path.Combine(destPath, combineFilename);
                        var rst = HttpNetworkController.Controller.SendFileTo(combineFilename, combinePath, FileType.Image);
                        if (rst)
                        {
                            saveSucceeded = true;
                            //new OperationRecordService().RecordOperation(OperationUI.ImsPage, combineFilename, OperationCommand.Saveto, "DumpToNet");
                            records.Add(new OperationRecord()
                            {
                                AccountId = PermissionService.Service.CurrentAccount != null ? PermissionService.Service.CurrentAccount.AccountId : string.Empty,
                                OperateTime = DateTime.Now,
                                OperateUI = OperationUI.ImsPage,
                                OperateObject = combineFilename,
                                OperateCommand = OperationCommand.Saveto,
                                OperateContent = "DumpToNet"
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Tracer.TraceError("[SaveImage] Error occured in GetBitmapAndSave");
                Tracer.TraceException(ex);
            }
            finally
            {
                // 如果没有保存成功
                if (!saveSucceeded)
                {
                    // 超过最大重试次数则不再重试
                    if (retryCount == _maxRetryTimes)
                    {
                        Tracer.TraceInfo("Retry saving failed: " + combineFilename);
                    }
                    // 否则当队列小于最大长度时加入重试队列
                    else if (_retrySaveQueue.Count() < _maxQueueLength)
                    {
                        _retrySaveQueue.Enqueue(new ExportJpgInfo(path, destPath, format, ext, effect, dumpToNet, retryCount));
                    }
                    else if (_retrySaveQueue.Count() >= _maxQueueLength)
                    {
                        // 最大重试次数
                        int maxTryDequeueTimes = 5;
                        int dequeueTimes = 0;
                        while (dequeueTimes < maxTryDequeueTimes)
                        {
                            if (_retrySaveQueue.TryDequeue(out _))
                            {
                                _retrySaveQueue.Enqueue(new ExportJpgInfo(path, destPath, format, ext, effect, dumpToNet, retryCount));
                                break;
                            }
                            else
                            {
                                dequeueTimes++;
                                if (dequeueTimes >= maxTryDequeueTimes)
                                {
                                    _retrySaveQueue = new ConcurrentQueue<ExportJpgInfo>();
                                    Tracer.TraceError("dequeue failed in GetBitmapAndSave");
                                }
                            }
                        }

                    }
                }
            }
        }

        private void GetSingleBitmapAndSave(XRayScanlinesImage img, string path, string destPath, string ext,
            List<OperationRecord> records, ImageFormat format, DetectViewIndex view,
            List<XRayNetEntities.ScannerSavedImage> savedImage = null,
            ExportImageEffects effect = ExportImageEffects.Regular, bool dumpToNet = false, int retryCount = 0,
            Rectangle[] cropRects = null)
        {
            bool saveSucceeded = false;
            string effectString;
            switch (effect)
            {
                case ExportImageEffects.Regular:
                    effectString = "";
                    break;
                case ExportImageEffects.Absorptivity:
                    effectString = "_Lighten";
                    break;
                default:
                    effectString = "_" + effect.ToString();
                    break;
            }
            if (img == null)
            {
                img = XRayScanlinesImage.LoadFromDiskFile(path);
            }
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            string singleViewImageFilename = fileNameWithoutExtension + ExchangeDirectionConfig.Service.GetSingleViewImageSuffix(view) + ext;

            try
            {

                Bitmap bmp = null;

                if (view == DetectViewIndex.View1)
                {
                    bmp = ip1.GetBitmap(effect);
                }
                else if (view == DetectViewIndex.View2)
                {
                    if (img.View2Data != null)
                    {
                        bmp = ip2.GetBitmap(effect);
                    }
                    else
                    {
                        return;
                    }
                }


                if (bmp != null)
                {
                    if (bmp != null && view == DetectViewIndex.View1 ? View1Vertical : View2Vertical)
                    {
                        bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    }
                    if (Left2Right)
                    {
                        bmp.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    }

                    if (cropRects != null)
                    {
                        var cropRect = cropRects[view - DetectViewIndex.View1];
                        GraphicsUnit unit = GraphicsUnit.Pixel;
                        var bounds = bmp.GetBounds(ref unit);
                        int new_x = !Left2Right ? cropRect.Y : (int)bounds.Width - cropRect.Height - cropRect.Y;
                        int new_y = (view == DetectViewIndex.View1 ? View1Vertical : View2Vertical) ?
                            (int)bounds.Height - cropRect.X - cropRect.Width : cropRect.X;
                        Rectangle newRect = new Rectangle(new_x, new_y, cropRect.Height, cropRect.Width);
                        Bitmap target = new Bitmap(newRect.Width, newRect.Height);
                        using (Graphics g = Graphics.FromImage(target))
                        {
                            g.DrawImage(bmp, new Rectangle(0, 0, target.Width, target.Height), newRect, GraphicsUnit.Pixel);
                        }
                        bmp.Dispose();
                        bmp = target;
                    }

                    PrintInfomationOnImage.PrintOnImage(bmp, fileNameWithoutExtension, DateTime2String(img.ScanningTime));
                    BitmapHelper.ImageCompress(bmp, Path.Combine(destPath, singleViewImageFilename), format, _imageQuality);
                    if (!dumpToNet)
                    {
                        if (savedImage != null)
                            savedImage.Add(new XRayNetEntities.ScannerSavedImage(Path.Combine(destPath, singleViewImageFilename), XRayNetEntities.SavedImageType.ExtraImage));
                        saveSucceeded = true;

                        // TODO: 记日志保存导致网络存图失败
                        //OperationRecordService.Service.RecordOperation();
                        records.Add(new OperationRecord()
                        {
                            OperateUI = OperationUI.ImsPage,
                            OperateTime = DateTime.Now,
                            OperateObject = fileNameWithoutExtension + ".xray",
                            OperateCommand = OperationCommand.Saveto,
                            AccountId = PermissionService.Service.CurrentAccount != null ? PermissionService.Service.CurrentAccount.AccountId : string.Empty,
                            OperateContent = ConfigHelper.AddQuotationForPath(Path.Combine(destPath, singleViewImageFilename))
                        });
                    }
                    else
                    {
                        string combinePath = Path.Combine(destPath, singleViewImageFilename);
                        var rst = HttpNetworkController.Controller.SendFileTo(singleViewImageFilename, combinePath, FileType.Image);
                        if (rst)
                        {
                            saveSucceeded = true;
                            //OperationRecordService.Service.RecordOperation(OperationUI.ImsPage, combineFilename, OperationCommand.Saveto, "DumpToNet");
                            records.Add(new OperationRecord()
                            {
                                AccountId = PermissionService.Service.CurrentAccount != null ? PermissionService.Service.CurrentAccount.AccountId : string.Empty,
                                OperateTime = DateTime.Now,
                                OperateUI = OperationUI.ImsPage,
                                OperateObject = singleViewImageFilename,
                                OperateCommand = OperationCommand.Saveto,
                                OperateContent = "DumpToNet"
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Tracer.TraceError("[SaveImage] Error occured in GetBitmapAndSave");
                Tracer.TraceException(ex);
            }
            finally
            {
                // 如果没有保存成功
                if (!saveSucceeded)
                {
                    // 超过最大重试次数则不再重试
                    if (retryCount == _maxRetryTimes)
                    {
                        Tracer.TraceInfo("Retry saving failed: " + singleViewImageFilename);
                    }
                    // 否则当队列小于最大长度时加入重试队列
                    else if (_retrySaveQueue.Count() < _maxQueueLength)
                    {
                        _retrySaveQueue.Enqueue(new ExportJpgInfo(path, destPath, format, ext, effect, dumpToNet, retryCount));
                    }
                    else if (_retrySaveQueue.Count() >= _maxQueueLength)
                    {
                        // 最大重试次数
                        int maxTryDequeueTimes = 5;
                        int dequeueTimes = 0;
                        while (dequeueTimes < maxTryDequeueTimes)
                        {
                            if (_retrySaveQueue.TryDequeue(out _))
                            {
                                _retrySaveQueue.Enqueue(new ExportJpgInfo(path, destPath, format, ext, effect, dumpToNet, retryCount));
                                break;
                            }
                            else
                            {
                                dequeueTimes++;
                                if (dequeueTimes >= maxTryDequeueTimes)
                                {
                                    _retrySaveQueue = new ConcurrentQueue<ExportJpgInfo>();
                                    Tracer.TraceError("dequeue failed in GetBitmapAndSave");
                                }
                            }
                        }

                    }
                }
            }
        }


        private void GetBitmapAndSaveRetry(XRayScanlinesImage img, string path, string destPath, string ext, ImageFormat format, List<XRayNetEntities.ScannerSavedImage> savedImage = null,
            ExportImageEffects effect = ExportImageEffects.Regular, bool dumpToNet = false, int retryCount = 0)
        {
            bool saveSucceeded = false;
            string effectString;
            switch (effect)
            {
                case ExportImageEffects.Regular:
                    effectString = "";
                    break;
                case ExportImageEffects.Absorptivity:
                    effectString = "_Lighten";
                    break;
                default:
                    effectString = "_" + effect.ToString();
                    break;
            }
            if (img == null)
            {
                img = XRayScanlinesImage.LoadFromDiskFile(path);
            }
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            string combineFilename = fileNameWithoutExtension + effectString + ext;

            try
            {

                Bitmap bmp1 = null;
                Bitmap bmp2 = null;
                ip1.AttachImageData(img.View1Data);
                bmp1 = ip1.GetBitmap(effect);

                if (img.View2Data != null)
                {
                    ip2.AttachImageData(img.View2Data);
                    bmp2 = ip2.GetBitmap(effect);
                }

                if (bmp1 != null)
                {
                    if (bmp1 != null && View1Vertical)
                    {
                        bmp1.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    }
                    if (bmp2 != null && View2Vertical)
                    {
                        bmp2.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    }
                    if (Left2Right)
                    {
                        bmp1.RotateFlip(RotateFlipType.RotateNoneFlipX);
                        if (bmp2 != null)
                        {
                            bmp2.RotateFlip(RotateFlipType.RotateNoneFlipX);
                        }
                    }

                    var combinebmp = BitmapHelper.CombineBmp(bmp1, bmp2);
                    PrintInfomationOnImage.PrintOnImage(combinebmp, fileNameWithoutExtension, DateTime2String(img.ScanningTime));
                    BitmapHelper.ImageCompress(combinebmp, Path.Combine(destPath, combineFilename), format, _imageQuality);
                    if (!dumpToNet)
                    {
                        if (savedImage != null)
                            savedImage.Add(new XRayNetEntities.ScannerSavedImage(Path.Combine(destPath, combineFilename), XRayNetEntities.SavedImageType.ExtraImage));
                        saveSucceeded = true;

                        // TODO: 记日志保存导致网络存图失败
                        new OperationRecordService().RecordOperation(OperationUI.ImsPage, fileNameWithoutExtension + ".xray",
                                                    OperationCommand.Saveto, ConfigHelper.AddQuotationForPath(Path.Combine(destPath, combineFilename)));
                    }
                    else
                    {
                        string combinePath = Path.Combine(destPath, combineFilename);
                        var rst = HttpNetworkController.Controller.SendFileTo(combineFilename, combinePath, FileType.Image);
                        if (rst)
                        {
                            saveSucceeded = true;
                            new OperationRecordService().RecordOperation(OperationUI.ImsPage, combineFilename, OperationCommand.Saveto, "DumpToNet");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Tracer.TraceError("[SaveImage] Error occured in GetBitmapAndSave");
                Tracer.TraceException(ex);
            }
            finally
            {
                // 如果没有保存成功
                if (!saveSucceeded)
                {
                    // 超过最大重试次数则不再重试
                    if (retryCount == _maxRetryTimes)
                    {
                        Tracer.TraceInfo("Retry saving failed: " + combineFilename);
                    }
                    // 否则当队列小于最大长度时加入重试队列
                    else if (_retrySaveQueue.Count() < _maxQueueLength)
                    {
                        _retrySaveQueue.Enqueue(new ExportJpgInfo(path, destPath, format, ext, effect, dumpToNet, retryCount));
                    }
                    else if (_retrySaveQueue.Count() >= _maxQueueLength)
                    {
                        // 最大重试次数
                        int maxTryDequeueTimes = 5;
                        int dequeueTimes = 0;
                        while (dequeueTimes < maxTryDequeueTimes)
                        {
                            if (_retrySaveQueue.TryDequeue(out _))
                            {
                                _retrySaveQueue.Enqueue(new ExportJpgInfo(path, destPath, format, ext, effect, dumpToNet, retryCount));
                                break;
                            }
                            else
                            {
                                dequeueTimes++;
                                if (dequeueTimes >= maxTryDequeueTimes)
                                {
                                    _retrySaveQueue = new ConcurrentQueue<ExportJpgInfo>();
                                    Tracer.TraceError("dequeue failed in GetBitmapAndSave");
                                }
                            }
                        }

                    }
                }
            }
        }

        private void GetSingleBitmapAndSaveRetry(XRayScanlinesImage img, string path, string destPath, string ext, ImageFormat format, DetectViewIndex view,
            List<XRayNetEntities.ScannerSavedImage> savedImage = null, ExportImageEffects effect = ExportImageEffects.Regular,
             bool dumpToNet = false, int retryCount = 0)
        {
            bool saveSucceeded = false;

            if (img == null)
            {
                img = XRayScanlinesImage.LoadFromDiskFile(path);
            }
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);

            string singleView1ImageFilename = fileNameWithoutExtension + ExchangeDirectionConfig.Service.GetSingleViewImageSuffix(view) + ext;

            try
            {

                Bitmap bmp = null;

                if (view == DetectViewIndex.View1)
                {
                    ip1.AttachImageData(img.View1Data);
                    bmp = ip1.GetBitmap();
                }
                else if (view == DetectViewIndex.View2)
                {
                    if (img.View2Data != null)
                    {
                        ip2.AttachImageData(img.View2Data);
                        bmp = ip2.GetBitmap();
                    }
                    else
                    {
                        return;
                    }
                }

                if (bmp != null)
                {
                    if (bmp != null && View1Vertical)
                    {
                        bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    }
                    if (Left2Right)
                    {
                        bmp.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    }

                    PrintInfomationOnImage.PrintOnImage(bmp, fileNameWithoutExtension, DateTime2String(img.ScanningTime));
                    BitmapHelper.ImageCompress(bmp, Path.Combine(destPath, singleView1ImageFilename), format, _imageQuality);
                    if (!dumpToNet)
                    {
                        if (savedImage != null)
                            savedImage.Add(new XRayNetEntities.ScannerSavedImage(Path.Combine(destPath, singleView1ImageFilename), XRayNetEntities.SavedImageType.ExtraImage));
                        saveSucceeded = true;

                        // TODO: 记日志保存导致网络存图失败
                        new OperationRecordService().RecordOperation(OperationUI.ImsPage, fileNameWithoutExtension + ".xray",
                                                    OperationCommand.Saveto, ConfigHelper.AddQuotationForPath(Path.Combine(destPath, singleView1ImageFilename)));
                    }
                    else
                    {
                        string combinePath = Path.Combine(destPath, singleView1ImageFilename);
                        var rst = HttpNetworkController.Controller.SendFileTo(singleView1ImageFilename, combinePath, FileType.Image);
                        if (rst)
                        {
                            saveSucceeded = true;
                            new OperationRecordService().RecordOperation(OperationUI.ImsPage, singleView1ImageFilename, OperationCommand.Saveto, "DumpToNet");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Tracer.TraceError("[SaveImage] Error occured in GetBitmapAndSave");
                Tracer.TraceException(ex);
            }
            finally
            {
                // 如果没有保存成功
                if (!saveSucceeded)
                {
                    // 超过最大重试次数则不再重试
                    if (retryCount == _maxRetryTimes)
                    {
                        Tracer.TraceInfo("Retry saving failed: " + singleView1ImageFilename);
                    }
                    // 否则当队列小于最大长度时加入重试队列
                    else if (_retrySaveQueue.Count() < _maxQueueLength)
                    {
                        _retrySaveQueue.Enqueue(new ExportJpgInfo(path, destPath, format, ext, effect, dumpToNet, retryCount));
                    }
                    else if (_retrySaveQueue.Count() >= _maxQueueLength)
                    {
                        // 最大重试次数
                        int maxTryDequeueTimes = 5;
                        int dequeueTimes = 0;
                        while (dequeueTimes < maxTryDequeueTimes)
                        {
                            if (_retrySaveQueue.TryDequeue(out _))
                            {
                                _retrySaveQueue.Enqueue(new ExportJpgInfo(path, destPath, format, ext, effect, dumpToNet, retryCount));
                                break;
                            }
                            else
                            {
                                dequeueTimes++;
                                if (dequeueTimes >= maxTryDequeueTimes)
                                {
                                    _retrySaveQueue = new ConcurrentQueue<ExportJpgInfo>();
                                    Tracer.TraceError("dequeue failed in GetBitmapAndSave");
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 从XRay图片中获取两个视角的bmp图像并按要求保存，此重载用于重试保存
        /// 这里认为网络服务是可靠的，因此保存时不重传.xray图片
        /// </summary>
        /// <param name="eJI">要重新保存的图片信息</param>
        private void GetBitmapAndSave(ExportJpgInfo eJI)
        {
            List<XRayNetEntities.ScannerSavedImage> savedImage = null;
            if (!eJI.dumpToNet)
            {
                savedImage = new List<XRayNetEntities.ScannerSavedImage>();
            }
            if (_isSendSingleViewImage)
            {
                GetSingleBitmapAndSaveRetry(null, eJI.path, eJI.destPath, eJI.ext, eJI.format, DetectViewIndex.View1, savedImage, eJI.effect, eJI.dumpToNet, eJI.retryCount + 1);
                GetSingleBitmapAndSaveRetry(null, eJI.path, eJI.destPath, eJI.ext, eJI.format, DetectViewIndex.View2, savedImage, eJI.effect, eJI.dumpToNet, eJI.retryCount + 1);
            }
            else
            {
                GetBitmapAndSaveRetry(null, eJI.path, eJI.destPath, eJI.ext, eJI.format, savedImage, eJI.effect, eJI.dumpToNet, eJI.retryCount + 1);
            }

            if (savedImage != null)
                TRSNetWorkService.Service.SendBagSavedInfo(eJI.path, savedImage);
        }

        private void DrawRectOnBitmap(Bitmap bmp, List<PaintingRectangle> marks)
        {
            foreach (var mark in marks)
            {
                var startX = mark.FromLine + 1;
                if (!mark.Right2Left)
                {
                    startX = bmp.Width - startX - Math.Abs(mark.FromLine - mark.ToLine);
                }
                if (startX < 0) startX = 0;
                if (startX > bmp.Width) startX = bmp.Width;
                int startY;
                if (mark.Vertical)
                {
                    startY = mark.FromChannel;
                }
                else
                {
                    startY = bmp.Height - mark.ToChannel;
                }


                int rectWidth = mark.ToLine - startX;
                if (!mark.Right2Left)
                {
                    rectWidth = Math.Abs(mark.FromLine - mark.ToLine);
                }
                if (rectWidth + startX > bmp.Width)
                    rectWidth = bmp.Width - startX;

                Pen pen = new Pen(Color.Red, 3);
                Graphics gh = Graphics.FromImage(bmp);
                // 画矩形
                gh.DrawRectangle(pen, startX, startY, rectWidth, Math.Abs(mark.FromChannel - mark.ToChannel));
                gh.Dispose();
            }
        }

        private Rectangle[] GetBagRectangle(XRayScanlinesImage image)
        {
            Rectangle[] ret = new Rectangle[image.View2Data == null ? 1 : 2];
            Matrix<byte> image1 = new Matrix<byte>(image.View1Data.ScanLines.Length, image.View1Data.ScanLineLength);
            var bytesPerLine1 = image.View1Data.ScanLineLength;
            var copiedBytes = 0;
            for (int i = 0; i < image.View1Data.ScanLinesCount; i++)
            {
                var ratio = ushort.MaxValue / byte.MaxValue;
                var temp = image.View1Data.ScanLines[i].XRayData.Select(point => (byte)(point / ratio)).ToArray();
                Buffer.BlockCopy(temp, 0, image1.Data, copiedBytes, bytesPerLine1);
                copiedBytes += bytesPerLine1;
            }
            CvInvoke.Threshold(image1.Mat, image1.Mat, 233, 255, Emgu.CV.CvEnum.ThresholdType.BinaryInv);
            var rect1 = CvInvoke.BoundingRectangle(image1.Mat);
            ret[0] = rect1;
            if (image.View2Data != null)
            {
                Matrix<byte> image2 = new Matrix<byte>(image.View2Data.ScanLines.Length, image.View2Data.ScanLineLength);
                var bytesPerLine2 = image.View2Data.ScanLineLength;
                copiedBytes = 0;
                for (int i = 0; i < image.View2Data.ScanLinesCount; i++)
                {
                    var ratio = ushort.MaxValue / byte.MaxValue;
                    var temp = image.View2Data.ScanLines[i].XRayData.Select(point => (byte)(point / ratio)).ToArray();
                    Buffer.BlockCopy(temp, 0, image2.Data, copiedBytes, bytesPerLine2);
                    copiedBytes += bytesPerLine2;
                }
                CvInvoke.Threshold(image2.Mat, image2.Mat, 233, 255, Emgu.CV.CvEnum.ThresholdType.BinaryInv);
                var rect2 = CvInvoke.BoundingRectangle(image2.Mat);
                ret[1] = rect2;
            }
            return ret;
        }
        #endregion

        #region public
        public string DateTime2String(DateTime dt)
        {
            switch (systemDateformat)
            {
                case 0:
                    return dt.ToString("MM.dd.yyyy  HH:mm:ss.fff");
                case 1:
                    return dt.ToString("dd.MM.yyyy  HH:mm:ss.fff");
                case 2:
                    return dt.ToString("yyyy.MM.dd  HH:mm:ss.fff");
                default:
                    return string.Format("MM.dd.yyyy  HH:mm:ss.fff", dt);
            }
        }

        /// <summary>
        /// 保存图片到磁盘，同时会上传一份到网络
        /// </summary>
        /// <param name="img">传入的XRay图像</param>
        /// <param name="path">图片地址</param>
        /// <param name="destPath">目标地址</param>
        /// <param name="SelectedDumpFormat">选择的保存格式</param>
        /// <param name="isImageSaved">是否保存图片到网络</param>
        public void SaveAllImageToDisk(XRayScanlinesImage img, string path, string destPath, int SelectedDumpFormat = 0, bool isImageSaved = false)
        {
            List<OperationRecord> records = new List<OperationRecord>();

            // 保存JPG图像时，需向网络服务传递所有保存的图像信息，这里使用img != null作为判断条件
            List<XRayNetEntities.ScannerSavedImage> savedImage = null;
            // 导出图像
            if (img == null)
            {
                img = XRayScanlinesImage.LoadFromDiskFile(path);
            }
            if (isImageSaved)
            {
                savedImage = new List<XRayNetEntities.ScannerSavedImage>();
                savedImage.Add(new XRayNetEntities.ScannerSavedImage(path, XRayNetEntities.SavedImageType.XRayImage));
            }

            var format = ImageFormat.Bmp;
            var ext = ".bmp";
            switch (SelectedDumpFormat)
            {
                case 0:
                    format = ImageFormat.Jpeg;
                    ext = ".jpg";
                    break;
                case 1:
                    format = ImageFormat.Png;
                    ext = ".png";
                    break;
                case 2:
                    format = ImageFormat.Bmp;
                    ext = ".bmp";
                    break;
                case 3:
                    format = ImageFormat.Tiff;
                    ext = ".tiff";
                    break;
            }

            ip1.AttachImageData(img.View1Data);
            if (img.View2Data != null)
            {
                ip2.AttachImageData(img.View2Data);
            }
            // 发送单视角图像
            if (_isSendSingleViewImage)
            {
                Rectangle[] rects = null;
                if (!ScannerConfig.Read(ConfigPath.CropImage, out bool isCropImage))
                {
                    isCropImage = false;
                }
                if (isCropImage)
                {
                    rects = GetBagRectangle(img);
                }
                GetSingleBitmapAndSave(img, path, destPath, ext, records, format, DetectViewIndex.View1, savedImage, ExportImageEffects.Regular, cropRects: rects);
                GetSingleBitmapAndSave(img, path, destPath, ext, records, format, DetectViewIndex.View2, savedImage, ExportImageEffects.Regular, cropRects: rects);
            }
            else
            {
                // 默认效果
                GetBitmapAndSave(img, path, destPath, ext, records, format, savedImage, ExportImageEffects.Regular);

                //亮度增加
                GetBitmapAndSave(img, path, destPath, ext, records, format, savedImage, ExportImageEffects.Absorptivity);

                //灰度
                GetBitmapAndSave(img, path, destPath, ext, records, format, savedImage, ExportImageEffects.Grey);

                //超级增强
                GetBitmapAndSave(img, path, destPath, ext, records, format, savedImage, ExportImageEffects.SuperEnhance);

                //第五个图像
                if (_image5Effect != ExportImageEffects.None)
                {
                    GetBitmapAndSave(img, path, destPath, ext, records, format, savedImage, _image5Effect);
                }
            }

            if (savedImage != null)
            {
                TRSNetWorkService.Service.SendBagSavedInfo(path, savedImage);

                AssignImage(savedImage[1].FilePath);
                
                Task.Run(async () =>
                {
                    try
                    {
                        await DetectHttpService();
                    }
                    catch (Exception exception)
                    {
                        Tracer.TraceException(exception);
                    }
                });



            }
               

            if (records.Count > 0)
            {
                new OperationRecordService().AddRecordRange(records);
            }
        }

        public void SaveAllImageUpf(XrayImageInfo info, string destPath, ImageFormat format)
        {

            string ext = ".jpg";
            if (format == ImageFormat.Png)
            {
                ext = ".png";
            }
            else if (format == ImageFormat.Tiff)
            {
                ext = ".tiff";
            }
            else if (format == ImageFormat.Bmp)
            {
                ext = ".bmp";
            }
            else
            {
                ext = ".jpg";
            }


            // 导出图像
            var img = info.XrayImage;
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(info.SaveFullPath);

            var allbundles = img.ToDisplayXRayMatLineDataBundles(0).ToList();
            dp.CalcEnhancedData(allbundles);

            for (int i = 0; i < allbundles.Count; i++)
            {
                img.View1Data.ScanLines[i].XRayDataEnhanced = allbundles[i].View1Data.XRayDataEnhanced;
                if (img.View2Data != null)
                {
                    img.View2Data.ScanLines[i].XRayDataEnhanced = allbundles[i].View2Data.XRayDataEnhanced;
                }
            }

            ip.AttachImageData(img.View1Data);
            var bmp = ip.GetBitmap();

            Bitmap bmp2 = null;
            if (img.View2Data != null)
            {
                ip.AttachImageData(img.View2Data);
                bmp2 = ip.GetBitmap();
            }

            if (bmp != null)
            {
                if (bmp != null && View1Vertical)
                {
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                }
                if (bmp2 != null && View2Vertical)
                {
                    bmp2.RotateFlip(RotateFlipType.RotateNoneFlipY);
                }
                if (Left2Right)
                {
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    if (bmp2 != null)
                    {
                        bmp2.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    }
                }
                var combinebmp = BitmapHelper.CombineBmp(bmp, bmp2);
                PrintInfomationOnImage.PrintOnImage(combinebmp, fileNameWithoutExtension, DateTime2String(img.ScanningTime));
                BitmapHelper.ImageCompress(combinebmp, Path.Combine(destPath, fileNameWithoutExtension + ext), format, _imageQuality);
            }

            //亮度增加
            ip.AttachImageData(img.View1Data);
            bmp = ip.GetBitmap(ExportImageEffects.Absorptivity);

            bmp2 = null;
            if (img.View2Data != null)
            {
                ip.AttachImageData(img.View2Data);
                bmp2 = ip.GetBitmap(ExportImageEffects.Absorptivity);
            }

            if (bmp != null)
            {
                if (bmp != null && View1Vertical)
                {
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                }
                if (bmp2 != null && View2Vertical)
                {
                    bmp2.RotateFlip(RotateFlipType.RotateNoneFlipY);
                }
                if (Left2Right)
                {
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    if (bmp2 != null)
                    {
                        bmp2.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    }
                }
                var combinebmp = BitmapHelper.CombineBmp(bmp, bmp2);
                PrintInfomationOnImage.PrintOnImage(combinebmp, fileNameWithoutExtension, DateTime2String(img.ScanningTime));
                BitmapHelper.ImageCompress(combinebmp, Path.Combine(destPath, fileNameWithoutExtension + "_Lighten" + ext), format, _imageQuality);
            }

            //灰度
            ip.AttachImageData(img.View1Data);
            bmp = ip.GetBitmap(ExportImageEffects.Grey);

            bmp2 = null;
            if (img.View2Data != null)
            {
                ip.AttachImageData(img.View2Data);
                bmp2 = ip.GetBitmap(ExportImageEffects.Grey);
            }

            if (bmp != null)
            {
                if (bmp != null && View1Vertical)
                {
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                }
                if (bmp2 != null && View2Vertical)
                {
                    bmp2.RotateFlip(RotateFlipType.RotateNoneFlipY);
                }
                if (Left2Right)
                {
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    if (bmp2 != null)
                    {
                        bmp2.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    }
                }
                var combinebmp = BitmapHelper.CombineBmp(bmp, bmp2);
                PrintInfomationOnImage.PrintOnImage(combinebmp, fileNameWithoutExtension, DateTime2String(img.ScanningTime));
                BitmapHelper.ImageCompress(combinebmp, Path.Combine(destPath, fileNameWithoutExtension + "_Grey" + ext), format, _imageQuality);
            }

            //超级增强
            ip.AttachImageData(img.View1Data);
            bmp = ip.GetBitmap(ExportImageEffects.SuperEnhance);

            bmp2 = null;
            if (img.View2Data != null)
            {
                ip.AttachImageData(img.View2Data);
                bmp2 = ip.GetBitmap(ExportImageEffects.SuperEnhance);
            }

            if (bmp != null)
            {
                if (bmp != null && View1Vertical)
                {
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                }
                if (bmp2 != null && View2Vertical)
                {
                    bmp2.RotateFlip(RotateFlipType.RotateNoneFlipY);
                }
                if (Left2Right)
                {
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    if (bmp2 != null)
                    {
                        bmp2.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    }
                }
                var combinebmp = BitmapHelper.CombineBmp(bmp, bmp2);
                PrintInfomationOnImage.PrintOnImage(combinebmp, fileNameWithoutExtension, DateTime2String(img.ScanningTime));
                BitmapHelper.ImageCompress(combinebmp, Path.Combine(destPath, fileNameWithoutExtension + "_SupurEnhance" + ext), format, _imageQuality);
            }

            //第五个图像
            if (_image5Effect == ExportImageEffects.None)
            {
                return;
            }
            ip.AttachImageData(img.View1Data);
            bmp = ip.GetBitmap(_image5Effect);

            bmp2 = null;
            if (img.View2Data != null)
            {
                ip.AttachImageData(img.View2Data);
                bmp2 = ip.GetBitmap(_image5Effect);
            }

            if (bmp != null)
            {
                if (bmp != null && View1Vertical)
                {
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                }
                if (bmp2 != null && View2Vertical)
                {
                    bmp2.RotateFlip(RotateFlipType.RotateNoneFlipY);
                }
                if (Left2Right)
                {
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    if (bmp2 != null)
                    {
                        bmp2.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    }
                }
                var combinebmp = BitmapHelper.CombineBmp(bmp, bmp2);
                PrintInfomationOnImage.PrintOnImage(combinebmp, fileNameWithoutExtension, DateTime2String(img.ScanningTime));
                BitmapHelper.ImageCompress(combinebmp, Path.Combine(destPath, fileNameWithoutExtension + _image5Effect.ToString() + ext), format, _imageQuality);
            }
        }

        public void SaveAllImageToNetwork(string path, string destPath, int SelectedDumpFormat)
        {
            List<OperationRecord> records = new List<OperationRecord>();
            // 导出图像
            var img = XRayScanlinesImage.LoadFromDiskFile(path);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);

            var format = ImageFormat.Bmp;
            var ext = ".bmp";
            switch (SelectedDumpFormat)
            {
                case 0:
                    format = ImageFormat.Jpeg;
                    ext = ".jpg";
                    break;
                case 1:
                    format = ImageFormat.Png;
                    ext = ".png";
                    break;
                case 2:
                    format = ImageFormat.Bmp;
                    ext = ".bmp";
                    break;
                case 3:
                    format = ImageFormat.Tiff;
                    ext = ".tiff";
                    break;
            }

            #region New
            var allbundles = img.ToDisplayXRayMatLineDataBundles(0).ToList();
            dp.CalcEnhancedData(allbundles);

            for (int i = 0; i < allbundles.Count; i++)
            {
                img.View1Data.ScanLines[i].XRayDataEnhanced = allbundles[i].View1Data.XRayDataEnhanced;
                if (img.View2Data != null)
                {
                    img.View2Data.ScanLines[i].XRayDataEnhanced = allbundles[i].View2Data.XRayDataEnhanced;
                }
            }

            ip1.AttachImageData(img.View1Data);
            if (img.View2Data != null)
            {
                ip2.AttachImageData(img.View2Data);
            }

            // 默认效果
            GetBitmapAndSave(img, path, destPath, ext, records, format, null, ExportImageEffects.Regular, true);

            //亮度增加
            GetBitmapAndSave(img, path, destPath, ext, records, format, null, ExportImageEffects.Absorptivity, true);

            //灰度
            GetBitmapAndSave(img, path, destPath, ext, records, format, null, ExportImageEffects.Grey, true);

            //超级增强
            GetBitmapAndSave(img, path, destPath, ext, records, format, null, ExportImageEffects.SuperEnhance, true);

            //第五个图像
            if (_image5Effect != ExportImageEffects.None)
            {
                GetBitmapAndSave(img, path, destPath, ext, records, format, null, _image5Effect, true);
            }
            if (records.Count > 0)
            {
                new OperationRecordService().AddRecordRange(records);
            }
            #endregion
        }

        public void SaveMarkedImageToDisk(List<PaintingRectangle> rects, ImageFormat format, bool isManual = false)
        {
            Dictionary<string, List<PaintingRectangle>> dir = new Dictionary<string, List<PaintingRectangle>>();
            if (rects.Count < 1) return;
            bool manualMark = rects[0].ManualMark;

            foreach (var rect in rects)
            {
                if (dir.Keys.Contains(rect.StorePath))
                {
                    dir[rect.StorePath].Add(rect);
                }
                else
                {
                    dir.Add(rect.StorePath, new List<PaintingRectangle> { rect });
                }
            }

            string ext = ".jpg";
            if (format == ImageFormat.Png)
            {
                ext = ".png";
            }
            else if (format == ImageFormat.Tiff)
            {
                ext = ".tiff";
            }
            else if (format == ImageFormat.Bmp)
            {
                ext = ".bmp";
            }
            else
            {
                ext = ".jpg";
            }

            foreach (var path in dir.Keys)
            {
                if (!File.Exists(path))
                {
                    continue;
                }
                XRayScanlinesImage img;
                try
                {
                    img = XRayScanlinesImage.LoadFromDiskFile(path);
                }
                catch (Exception ex)
                {
                    continue;
                }
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);


                var allbundles = img.ToDisplayXRayMatLineDataBundles(0).ToList();
                if (allbundles.Count < 3) return;
                dp.CalcEnhancedData(allbundles);

                for (int i = 0; i < allbundles.Count; i++)
                {
                    img.View1Data.ScanLines[i].XRayDataEnhanced = allbundles[i].View1Data.XRayDataEnhanced;
                    if (img.View2Data != null)
                    {
                        img.View2Data.ScanLines[i].XRayDataEnhanced = allbundles[i].View2Data.XRayDataEnhanced;
                    }
                }


                ip.AttachImageData(img.View1Data);
                var bmp1 = ip.GetBitmap();

                Bitmap bmp2 = null;
                if (img.View2Data != null)
                {
                    ip.AttachImageData(img.View2Data);
                    bmp2 = ip.GetBitmap();
                }

                if (bmp1 != null)
                {
                    if (bmp1 != null && View1Vertical)
                    {
                        bmp1.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    }
                    if (bmp2 != null && View2Vertical)
                    {
                        bmp2.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    }
                    if (Left2Right)
                    {
                        bmp1.RotateFlip(RotateFlipType.RotateNoneFlipX);
                        if (bmp2 != null)
                        {
                            bmp2.RotateFlip(RotateFlipType.RotateNoneFlipX);
                        }
                    }
                }

                //if (isManual)
                {
                    var view1_rects = dir[path].Where(r => r.View == 1).ToList();
                    var view2_rects = dir[path].Where(r => r.View == 2).ToList();

                    if (bmp1 != null && view1_rects.Count > 0)
                    {
                        DrawRectOnBitmap(bmp1, view1_rects);
                    }
                    if (bmp2 != null && view2_rects.Count > 0)
                    {
                        DrawRectOnBitmap(bmp2, view2_rects);
                    }
                }

                if (!Directory.Exists(ReadConfigService.Service.AutoStoreUpfImagePath))
                {
                    Directory.CreateDirectory(ReadConfigService.Service.AutoStoreUpfImagePath);
                }

                var images = new List<XRayNetEntities.ScannerSavedImage>();
                if (!_isSendSingleViewImage)
                {
                    Bitmap bmp = bmp2 == null ? bmp1 : BitmapHelper.CombineBmp(bmp1, bmp2);
                    SaveMarkedImage(DetectViewIndex.All, bmp, isManual, img.ScanningTime, format, images);
                }
                else
                {
                    if (bmp1 != null)
                    {
                        SaveMarkedImage(DetectViewIndex.View1, bmp1, isManual, img.ScanningTime, format, images);
                    }
                    if (bmp2 != null)
                    {
                        SaveMarkedImage(DetectViewIndex.View2, bmp2, isManual, img.ScanningTime, format, images);
                    }
                }
                
                TRSNetWorkService.Service.SendBagSavedInfo(path, images);
            }
        }

        private void SaveMarkedImage(DetectViewIndex view, Bitmap bmp, bool isManual,
            DateTime scanTime, ImageFormat format, List<XRayNetEntities.ScannerSavedImage> images)
        {
            if (bmp != null)
            {
                string mode = isManual ? "M" : "A";
                StringBuilder fileNameBuilder = new StringBuilder();
                fileNameBuilder.Append(mode);
                fileNameBuilder.Append(".");
                fileNameBuilder.Append(LoginAccountManager.Service.CurrentAccount.AccountId);
                fileNameBuilder.Append(".");
                fileNameBuilder.Append(scanTime.ToString("dd.MM.yyyy.HH.mm.ss"));
                if (view != DetectViewIndex.All)
                {
                    fileNameBuilder.Append(ExchangeDirectionConfig.Service.GetSingleViewImageSuffix(view));
                }
                fileNameBuilder.Append(".jpg");
                string filename = fileNameBuilder.ToString();
                var time = scanTime;
                var savePath = Path.Combine(ReadConfigService.Service.AutoStoreUpfImagePath,
                                            time.Year.ToString().PadLeft(4, '0'),
                                            time.Month.ToString().PadLeft(2, '0'),
                                            time.Day.ToString().PadLeft(2, '0'));
                if (!Directory.Exists(savePath))
                    Directory.CreateDirectory(savePath);
                BitmapHelper.ImageCompress(bmp, Path.Combine(savePath, filename), format, _imageQuality);
                // 保存完画框的图像后，向网络服务推送该图像信息
                images.Add(new XRayNetEntities.ScannerSavedImage(Path.Combine(savePath, filename),
                    isManual ? XRayNetEntities.SavedImageType.ManualJudgeImage : XRayNetEntities.SavedImageType.AutoJudgeImage));
            }
        }

        public void StopService()
        {
            _exit = true;
        }
        #endregion
        #endregion
    }
}
