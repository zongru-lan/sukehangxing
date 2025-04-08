using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.Algo;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Common.Utilities;
using UI.XRay.Flows.Services;
using UI.XRay.Flows.Services.Db;

namespace UI.XRay.Flows.Controllers
{
    public class ImageStoreInfo
    {
        public ImageRecord ImageRecord { get; private set; }

        public string ImageStorePath { get; private set; }

        public XRayScanlinesImage Image { get; private set; }

        public ImageStoreInfo(ImageRecord imageRecord, string path, XRayScanlinesImage image)
        {
            ImageRecord = imageRecord;
            ImageStorePath = path;
            Image = image;
        }

        //public string XrayFilePath { get; set; }

        //public int StartLineNo { get; set; }

        //public DateTime StartTime { get; set; }

        //public int EndLineNo { get; set; }

        //public DateTime EndTime { get; set; }

        //public string AccountId { get; set; }

    }

    /// <summary>
    /// 异步保存图像的控制器。软件运行过程中发现，图像文件和图像记录保存时会对数据处理产生影响，因此考虑异步处理图像文件和记录的保存
    /// </summary>
    public class ImageAsyncStoreController
    {
        private readonly ConcurrentQueue<ImageStoreInfo> _queue = new ConcurrentQueue<ImageStoreInfo>();

        private Thread _processImageStoreTask;

        private bool _exit;

        private int _exportFormat = 0;

        private int _exportTimeDelay = 0;

        HiPerfTimer hp = new HiPerfTimer();
        public ImageAsyncStoreController()
        {
            _processImageStoreTask = new Thread(ImageStoreTaskExe);
            _processImageStoreTask.Start();
            ReadConfig();
        }
        private void ReadConfig()
        {
            if (!ScannerConfig.Read(ConfigPath.ImagesExportImageFormat,out _exportFormat))
            {
                _exportFormat = 0;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesExportTimeDelay, out _exportTimeDelay))
            {
                _exportTimeDelay = 0;
            }
        }
        private void ImageStoreTaskExe()
        {
            ImageStoreInfo imageStoreInfo;
            while (!_exit)
            {
                try
                {
                    if (!_queue.TryDequeue(out imageStoreInfo))
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    try
                    {
                        DateTime datetime = DateTime.Now + TimeSpan.FromMilliseconds(_exportTimeDelay);
                        imageStoreInfo.ImageRecord.ScanTime = datetime;
                        imageStoreInfo.Image.ScanningTime = datetime;

                        BagCounterService.Service.Increase();

                        AddRecordToDb(imageStoreInfo.ImageRecord);

                        SaveXray(imageStoreInfo.Image, imageStoreInfo.ImageStorePath);

                        SaveJpg(imageStoreInfo.Image, imageStoreInfo.ImageStorePath);

                        Tracer.TraceInfo($"[ImageStore] Save new image to {imageStoreInfo.ImageStorePath}, session count: {BagCounterService.Service.SessionCount}, " +
                            $"queue size: {_queue.Count}");
                    }
                    catch (Exception e)
                    {
                        Tracer.TraceError("[ImageStore] Error occured in ImageStoreTaskExe");
                        Tracer.TraceException(e);
                    }
                    while (_queue.Count > 3)
                    {
                        _queue.TryDequeue(out _);
                        Trace.TraceWarning("[ImageStore] Queue count too large, will clear");
                    }
                }
                catch (Exception e)
                {
                    Tracer.TraceException(e);
                }

                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// 保存xray文件到本地
        /// </summary>
        /// <param name="img"></param>
        /// <param name="filePath"></param>
        private void SaveXray(XRayScanlinesImage img, string filePath)
        {
            var lineCount = img.View1Data.ScanLines.Length;

            //将XRayDataEnhanced数据暂时放在队列中
            List<ushort[]> view1Enhanced = new List<ushort[]>();
            List<ushort[]> view2Enhanced = new List<ushort[]>();
            for (int i = 0; i < lineCount; i++)
            {
                view1Enhanced.Add(img.View1Data.ScanLines[i].XRayDataEnhanced);
                if (img.View2Data != null)
                {
                    view2Enhanced.Add(img.View2Data.ScanLines[i].XRayDataEnhanced);
                }
            }


            // 保存XRay文件前，清空XRayDataEnhanced数据
            for (int i = 0; i < img.View1Data.ScanLines.Length; i++)
            {
                img.View1Data.ScanLines[i].XRayDataEnhanced = null;
            }
            if (img.View2Data != null)
            {
                for (int i = 0; i < img.View2Data.ScanLines.Length; i++)
                {
                    img.View2Data.ScanLines[i].XRayDataEnhanced = null;
                }
            }
            // 保存至磁盘
            XRayScanlinesImage.SaveToDiskFile(img, filePath);

            //把XRayEnhanced数据加回来
            if (view1Enhanced.Count == lineCount)
            {
                for (int i = 0; i < lineCount; i++)
                {
                    img.View1Data.ScanLines[i].XRayDataEnhanced = view1Enhanced[i];
                    if (img.View2Data != null)
                    {
                        img.View2Data.ScanLines[i].XRayDataEnhanced = view2Enhanced[i];
                    }
                }
            }
        }


        /// <summary>
        /// 存为jpg
        /// </summary>
        /// <param name="img"></param>
        /// <param name="filePath"></param>
        private void SaveJpg(XRayScanlinesImage img, string filePath)
        {
            try
            {
                var destpath = Path.GetDirectoryName(filePath);

                hp.ClearTimer();
                hp.Start();
                Save4ImagesService.Service.SaveAllImageToDisk(img,filePath, destpath, _exportFormat, true);
                hp.Stop();
                Tracer.TraceInfo(string.Format("file:{0} save jpg cost {1}", filePath, hp.Duration));
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }

        }

        private void AddRecordToDb(ImageRecord record)
        {
            //先将记录保存到数据库
            var imageRecordController = new ImageRecordDbSet();

            imageRecordController.Add(record);
        }

        public void SaveImage(ImageStoreInfo imageStoreInfo)
        {
                _queue.Enqueue(imageStoreInfo);
        }

        public void ShutDown()
        {
            _exit = true;

            if (_processImageStoreTask != null)
            {
                _processImageStoreTask.Abort();
                _processImageStoreTask = null;
            }
        }
    }
}
