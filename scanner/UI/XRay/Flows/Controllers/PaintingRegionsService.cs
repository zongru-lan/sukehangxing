using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UI.XRay.Business.Entities;
using UI.XRay.Common.Utilities;
using UI.XRay.ImagePlant;
using UI.XRay.ImagePlant.Cpu;
using UI.XRay.RenderEngine;
using UI.XRay.Flows.Services.Db;
using UI.XRay.Flows.TRSNetwork;
using UI.XRay.Flows.Services;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using XRayNetEntities;
using UI.Common.Tracers;

namespace UI.XRay.Flows.Controllers
{
    public class PaintingRegionsService
    {
        public static PaintingRegionsService Service { get; set; }
        static PaintingRegionsService()
        {
            Service = new PaintingRegionsService();
        }
        public PaintingRegionsService()
        {
            _saveTimer = new Timer(SavePaintingRectangles,null,Timeout.Infinite,Timeout.Infinite);
            _imageRecordSet = new ImageRecordDbSet();
        }

        public void InitScreen(int screenwidth, int viewcount)
        {
            CurrentScreenLines = new LinkedList<DisplayScanlineDataBundle>();
            screeWidth = screenwidth / viewcount;
            HttpNetworkController.Controller.ResetCommand += Controller_ResetCommand;
        }

        private void Controller_ResetCommand(object sender, bool e)
        {
            _isProcessing = false;
        }

        public void Close()
        {
            if (_saveTimer != null)
            {
                _saveTimer.Dispose();
                _saveTimer = null;
            }
        }

        /// <summary>
        /// 存放xray图像的地址、开始结束线编号
        /// </summary>
        private List<XRayFileInfoForMark> _xrayFileList = new List<XRayFileInfoForMark>();

        /// <summary>
        /// 视角1数据自动探测的结果
        /// </summary>
        private List<MarkerRegionEx> _markerList = new List<MarkerRegionEx>(10);
        public List<MarkerRegionEx> MarkerList
        {
            get { return _markerList; }
            set { _markerList = value; }
        }

        public List<XRayFileInfoForMark> GetXRayFile
        {
            get { return _xrayFileList; }
        }

        private LinkedList<DisplayScanlineDataBundle> CurrentScreenLines;
        private int screeWidth;

        public IRollingImageProcessController RollingImageProcessController { get; set; }

        private Timer _saveTimer;

        private ImageRecordDbSet _imageRecordSet;

        public void AddXRayFileInfo(XRayFileInfoForMark xray)
        {
            lock (XrayFileListLock)
            {
                _xrayFileList.Add(xray);

                _xrayFileList = _xrayFileList.Where((x, i) => _xrayFileList.FindIndex(z => z.FilePath == x.FilePath) == i).ToList();

                if (GetAllLinesCount() > screeWidth * 5)
                {
                    _xrayFileList.RemoveAt(0);
                }
            }
        }
        public void ClearXRayFileInfo()
        {
            lock (XrayFileListLock)
            {
                _xrayFileList.Clear();
            }
        }

        private int GetAllLinesCount()
        {
            int count = 0;
            lock (XrayFileListLock)
            {
                foreach (var image in _xrayFileList)
                {
                    count += (image.EndLineNumber - image.StartLineNumber + 1);
                }
            }
            return count;
        }

        private bool IsInRange(MarkerRegionEx region, XRayFileInfoForMark xrayfile)
        {
            var start = xrayfile.StartLineNumber;
            var end = xrayfile.EndLineNumber;
            if ((region.FromLine > start && region.FromLine < end) || (region.ToLine > start && region.ToLine < end)) return true;
            else return false;
        }

        private void DrawRectOnBitmap(Bitmap bmp, MarkerRegionEx mark, XRayFileInfoForMark xrayfile)
        {
            var startX = mark.FromLine - xrayfile.StartLineNumber + 1;
            if (!mark.Right2Left)
            {
                startX = bmp.Width - startX - mark.Height;
            }
            if (startX < 0) startX = 0;
            if (startX > bmp.Width) startX = bmp.Width;
            int startY;
            if (mark.VerticalFlip)
            {
                startY = mark.FromChannel;
            }
            else
            {
                startY = bmp.Height - mark.ToChannel;
            }


            int rectWidth = mark.ToLine - xrayfile.StartLineNumber - startX;
            if (!mark.Right2Left)
            {
                rectWidth = Math.Abs(mark.FromLine - mark.ToLine);
            }
            if (rectWidth + startX > bmp.Width)
                rectWidth = bmp.Width - startX;

            Pen pen = new Pen(mark.RegionBorderColor, mark.RegionBorderWidth);
            Graphics gh = Graphics.FromImage(bmp);
            // 画矩形
            gh.DrawRectangle(pen, startX, startY, rectWidth, mark.Width);
            gh.Dispose();
        }

        private object XrayFileListLock = new object();  //yxc


        private bool _isProcessing = false;

        /// <summary>
        /// 发送本地人工判图
        /// </summary>
        /// <param name="judgeResult"></param>
        public void SendManualJudgeResult(JudgeResult judgeResult)
        {
            try
            {
                if (_isProcessing)
                {
                    Tracer.TraceDebug($"[DeJiangLocalJudgeTail] - _isProcessing:{_isProcessing}");
                    return;
                }
                _isProcessing = true;
                if (_markerList != null)
                {
                    _markerList.Clear();
                }
                if (RollingImageProcessController.MarkerList != null && RollingImageProcessController.MarkerList.Count > 0)
                {
                    lock (RollingImageProcessController.MarkerList)
                    {
                        _markerList = DeepCopy.DeepCopyByBin(RollingImageProcessController.MarkerList);
                    }
                }
                var unsaved = _markerList.Where(r => r.HasSaved == false).Count();
                if (unsaved < 1)
                {
                    XRayFileInfoForMark xRayFileInfoForMark = _xrayFileList.Last();
                    string filePath = xRayFileInfoForMark.FilePath;
                    if (!File.Exists(filePath))
                    {
                        Tracer.TraceDebug($"[DeJiangLocalJudgeTail] - {filePath} is not fount");
                        return;
                    }
                    TRSNetWorkService.Service.UpdateManualJudgeResult(DateTime.Now, judgeResult, null, filePath);
                    var record = _imageRecordSet.TakeRecordByPath(filePath);
                    if (record != null)
                    {
                        record.IsManualSaved = true;
                        _imageRecordSet.Update(new List<ImageRecord>() { record });
                    }
                    _isProcessing = false;
                    Tracer.TraceDebug($"[DeJiangLocalJudgeTail] - SendManualJudgeResult end");
                    return;
                }
                var savedRegion = new List<MarkerRegionEx>();
                var saveRecords = new List<PaintingRectangle>();
                lock (XrayFileListLock)
                {
                    foreach (var xrayfile in _xrayFileList)
                    {
                        List<MarkerRegionEx> currentRegions = new List<MarkerRegionEx>();
                        foreach (var region in _markerList)
                        {
                            if (IsInRange(region, xrayfile) && !region.HasSaved)
                            {
                                currentRegions.Add(region);
                                savedRegion.Add(region);
                            }
                        }
                        if (currentRegions.Count == 0)
                        {
                            continue;
                        }
                        bool _right2Left = RollingImageProcessController.RightToLeft;
                        foreach (var region in currentRegions)
                        {
                            var rect = new PaintingRectangle()
                            {
                                FromLine = Math.Max(region.FromLine - xrayfile.StartLineNumber, 0),
                                ToLine = region.ToLine > xrayfile.EndLineNumber
                                    ? Math.Abs(xrayfile.StartLineNumber - xrayfile.EndLineNumber)
                                    : region.ToLine - xrayfile.StartLineNumber,
                                FromChannel = region.FromChannel,
                                ToChannel = region.ToChannel,
                                View = (int)region.View,
                                Vertical = region.VerticalFlip,
                                Right2Left = _right2Left,
                                StorePath = xrayfile.FilePath
                            };
                            saveRecords.Add(rect);
                        }
                        if (!File.Exists(xrayfile.FilePath))
                        {
                            continue;
                        }
                        var fileName = ConfigHelper.GetManualCopyFileName(ConfigPath.ManualStorePath, xrayfile.FilePath);
                        //File.Copy(xrayfile.FilePath, fileName);

                        var record = _imageRecordSet.TakeRecordByPath(xrayfile.FilePath);
                        if (record != null)
                        {
                            record.IsManualSaved = true;
                            _imageRecordSet.Update(new List<ImageRecord>() { record });
                        }
                    }
                }
                if (saveRecords != null && saveRecords.Count > 0)
                {
                    //if (!Directory.Exists(ConfigPath.ManualStorePath)) Directory.CreateDirectory(ConfigPath.ManualStorePath);

                    //var fileName = ConfigHelper.GetManualIdentifyFileName(ConfigPath.ManualStorePath, "ManualDraw");
                    //using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Read, 100000))
                    //{
                    //    var formatter = new XmlSerializer(typeof(List<PaintingRectangle>));
                    //    formatter.Serialize(stream, saveRecords);
                    //}

                    if (ReadConfigService.Service.IsRuSaveModeEnable)
                    {
                        Save4ImagesService.Service.SaveMarkedImageToDisk(saveRecords, ImageFormat.Jpeg, true);
                    }
                    TRSNetWorkService.Service.UpdateManualJudgeResult(DateTime.Now, judgeResult, saveRecords);

                    foreach (var region in savedRegion)
                    {
                        region.HasSaved = true;

                        var rg = RollingImageProcessController.MarkerList.Where(r => r.RegionAreaSize == region.RegionAreaSize).FirstOrDefault();
                        if (rg != null)
                        {
                            rg.HasSaved = true;
                        }
                    }
                }

                _isProcessing = false;
                //如果有未保存的框，则定时去尝试保存
                if (_markerList != null && _markerList.Count > 0)
                {
                    unsaved = _markerList.Where(r => r.HasSaved == false).Count();
                    if (unsaved > 0)
                    {
                        _saveTimer.Change(2000, Timeout.Infinite);
                    }
                }
                Tracer.TraceDebug($"[DeJiangLocalJudgeTail] - SendManualJudgeResult end includ markers");
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }

        public void SavePaintingRectangles(object obj = null)
        {
            if (_isProcessing)
            {
                return;
            }
            _isProcessing = true;

            if (_markerList != null)
            {
                _markerList.Clear();
            }
            
            if (RollingImageProcessController.MarkerList != null && RollingImageProcessController.MarkerList.Count > 0)
            {
                lock (RollingImageProcessController.MarkerList)
                {
                    _markerList = DeepCopy.DeepCopyByBin(RollingImageProcessController.MarkerList);
                }
            }

            var unsaved = _markerList.Where(r => r.HasSaved == false).Count();
            if (unsaved < 1)
            {
                _isProcessing = false;
                return;
            }

            if (_markerList == null ||_markerList.Count == 0) return;

            var savedRegion = new List<MarkerRegionEx>();
            var saveRecords = new List<PaintingRectangle>();

            lock (XrayFileListLock)
            {
                foreach (var xrayfile in _xrayFileList)
                {
                    List<MarkerRegionEx> currentRegions = new List<MarkerRegionEx>();
                    foreach (var region in _markerList)
                    {
                        if (IsInRange(region, xrayfile) && !region.HasSaved)
                        {
                            currentRegions.Add(region);
                            savedRegion.Add(region);
                        }
                    }
                    if (currentRegions.Count == 0)
                    {
                        continue;
                    }

                    bool _right2Left = RollingImageProcessController.RightToLeft;

                    foreach (var region in currentRegions)
                    {
                        var rect = new PaintingRectangle()
                        {
                            FromLine = Math.Max(region.FromLine - xrayfile.StartLineNumber, 0),
                            ToLine = region.ToLine > xrayfile.EndLineNumber
                                ? Math.Abs(xrayfile.StartLineNumber - xrayfile.EndLineNumber)
                                : region.ToLine - xrayfile.StartLineNumber,
                            FromChannel = region.FromChannel,
                            ToChannel = region.ToChannel,
                            View = (int)region.View,
                            Vertical = region.VerticalFlip,
                            Right2Left = region.Right2Left,
                            StorePath = xrayfile.FilePath
                        };
                        saveRecords.Add(rect);
                    }
                    if (!File.Exists(xrayfile.FilePath))
                    {
                        continue;
                    }
                    // TRS 屏蔽重新存储功能
                    //var image = XRayScanlinesImage.LoadFromDiskFile(xrayfile.FilePath);
                    //image.View1Data.TagRegions = GetRectList(saveRecords.Where(r => r.View == 1));
                    //image.View2Data.TagRegions = GetRectList(saveRecords.Where(r => r.View == 2));

                    //XRayScanlinesImage.SaveToDiskFile(image, xrayfile.FilePath);

                    var fileName = ConfigHelper.GetManualCopyFileName(ConfigPath.ManualStorePath, xrayfile.FilePath);
                    //File.Copy(xrayfile.FilePath, fileName);

                    var record = _imageRecordSet.TakeRecordByPath(xrayfile.FilePath);
                    if (record != null)
                    {
                        record.IsManualSaved = true;
                        _imageRecordSet.Update(new List<ImageRecord>() { record });
                    }

                }
            }
            if (saveRecords != null && saveRecords.Count > 0)
            {
                //if (!Directory.Exists(ConfigPath.ManualStorePath)) Directory.CreateDirectory(ConfigPath.ManualStorePath);

                //var fileName = ConfigHelper.GetManualIdentifyFileName(ConfigPath.ManualStorePath, "ManualDraw");
                //using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Read, 100000))
                //{
                //    var formatter = new XmlSerializer(typeof(List<PaintingRectangle>));
                //    formatter.Serialize(stream, saveRecords);
                //}

                if (ReadConfigService.Service.IsRuSaveModeEnable)
                {
                    Save4ImagesService.Service.SaveMarkedImageToDisk(saveRecords, ImageFormat.Jpeg, true);
                }
                TRSNetWorkService.Service.UpdateManualJudgeResult(DateTime.Now, JudgeResult.Reject,saveRecords);

                foreach (var region in savedRegion)
                {
                    region.HasSaved = true;

                    var rg = RollingImageProcessController.MarkerList.Where(r => r.RegionAreaSize == region.RegionAreaSize).FirstOrDefault();
                    if (rg != null)
                    {
                        rg.HasSaved = true;
                    }
                }
            }

            _isProcessing = false;
            //如果有未保存的框，则定时去尝试保存
            if (_markerList !=null && _markerList .Count > 0)
            {
                unsaved = _markerList.Where(r => r.HasSaved == false).Count();
                if (unsaved > 0)
                {
                    _saveTimer.Change(2000, Timeout.Infinite);
                } 
            }            
        }

        public void AddLine(DisplayScanlineDataBundle bundle)
        {
            CurrentScreenLines.AddLast(bundle);
            if (CurrentScreenLines.Count > screeWidth)
            {
                CurrentScreenLines.RemoveFirst();
            }
        }
        public void AddReverseLine(DisplayScanlineDataBundle bundle)
        {
            CurrentScreenLines.AddFirst(bundle);
            if (CurrentScreenLines.Count > screeWidth)
            {
                CurrentScreenLines.RemoveLast();
            }
        }

        #region tools
        public IEnumerable<DisplayScanlineDataBundle> GetLinesFromImageFile(string[] path)
        {
            var result = new List<DisplayScanlineDataBundle>();

            // 图像数据线的最大编号
            int maxNum = -1;
            for (int i = 0; i < path.Count(); i++)
            {
                var image = XRayScanlinesImage.LoadFromDiskFile(path[i]);
                var imageScanLine = image.ToDisplayXRayMatLineDataBundles(maxNum);
                maxNum += imageScanLine.Count;
                result.AddRange(imageScanLine);
            }
            // 读取图像文件并解压缩


            // 将图像数据转换为编号后的队列（转换后小号在前大号在后），并取当前最小编号-1作为该图像中数据的最大编号

            return result;
        }

        private List<MarkerRegion> GetRectList(IEnumerable<PaintingRectangle> list)
        {
            var markregions = new List<MarkerRegion>();
            if (list == null || list.Count() < 1) return markregions;

            var sb = new StringBuilder();
            foreach (var rect in list)
            {
                var mr = new MarkerRegion(MarkerRegionType.Contraband, rect.FromLine, rect.ToLine, rect.FromChannel, rect.ToChannel, true);
                markregions.Add(mr);
            }
            return markregions;
        }

        public List<MarkerRegion> GetMarkRegionsFromXRay(XRayScanlinesImage image, DetectViewIndex view, int startNum)
        {
            if (!IsDanger(image)) return new List<MarkerRegion>();
            List<MarkerRegion> mrTemp = new List<MarkerRegion>();
            if (view == DetectViewIndex.View1)
            {
                if (image.View1Data != null)
                    mrTemp = image.View1Data.TagRegions;
            }
            else
            {
                if (image.View2Data != null)
                    mrTemp = image.View2Data.TagRegions;
            }
            var mrList = new List<MarkerRegion>();
            foreach (var rect in mrTemp)
            {
                var mr = new MarkerRegion(MarkerRegionType.Contraband, rect.FromLine + startNum, rect.ToLine + startNum,
                    rect.FromChannel, rect.ToChannel, rect.ManualTag);
                mrList.Add(mr);
            }
            return mrList;
        }

        private bool IsDanger(XRayScanlinesImage image)
        {
            var isDanger = image.View1Data.TagRegions.Count > 0;
            if (image.View2Data != null)
            {
                isDanger = image.View1Data.TagRegions.Count > 0 || image.View2Data.TagRegions.Count > 0;
            }
            return isDanger;
        }
        #endregion
    }
   
}
