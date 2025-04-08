using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interactivity;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Services;

namespace UI.XRay.Flows.Controllers
{
    /// <summary>
    /// 提供培训图像数据的显示
    /// </summary>
    internal class TrainingImageDataProvider : IRollingImageDataProvider
    {
        public event Action<DisplayScanlineDataBundle> DataReady;

        /// <summary>
        /// 一个新图像已经准备完毕，即将显示
        /// </summary>
        public event Action NewImageIsReady;

        /// <summary>
        /// 当前扫描线的最大编号，每增加一线新数据，编号+1
        /// </summary>
        private int MaxLineNumber { get; set; }

        /// <summary>
        /// 缓存的尚未显示的数据线（一次性加载一个图像文件，但是先缓存起来，按照模拟时钟输出）
        /// </summary>
        private LinkedList<DisplayScanlineDataBundle> _cachedLines = new LinkedList<DisplayScanlineDataBundle>();

        /// <summary>
        /// 模拟定时器：根据配置的采集硬件的积分时间，按同样的速率模拟过包
        /// </summary>
        private Timer _simulatingTimer;

        /// <summary>
        /// 两个模拟图像之间的间隔
        /// </summary>
        private int _imageInterval;

        /// <summary>
        /// 上一个模拟图像的生成时间
        /// </summary>
        private DateTime? _lastImageFinishTime;

        /// <summary>
        /// 线积分时间，单位为毫秒
        /// </summary>
        private float _lineIntegrationTime;

        /// <summary>
        /// 输送机是否正在运转
        /// </summary>
        private bool _isConveyorRunning = false;

        private TrainingImageSelector _imageSelector;

        public TrainingImageDataProvider()
        {
            InitImageSelector();
            InitSimuTimer();
        }

        /// <summary>
        /// 初始化培训图像选择组件
        /// </summary>
        private void InitImageSelector()
        {
            TrainingImageLoopMode loopMode;
            if (!ScannerConfig.Read(ConfigPath.TrainingLoopMode, out loopMode))
            {
                loopMode = TrainingImageLoopMode.RandomLoop;
            }

            int interval = 0;
            if (!ScannerConfig.Read(ConfigPath.TrainingImageInterval, out interval))
            {
                interval = 1;
            }

            interval = Math.Max(0, interval);
            interval = Math.Min(interval, 100);

            _imageInterval = interval;

            _imageSelector = new TrainingImageSelector(loopMode);
        }
      
        /// <summary>
        /// 加载配置信息
        /// </summary>
        private void InitSimuTimer()
        {
            try
            {
                if (!ScannerConfig.Read(ConfigPath.CaptureSysLineIntegrationTime, out _lineIntegrationTime))
                {
                    _lineIntegrationTime = 4.0f;
                }

                // 每隔100ms运行一次定时器
                _simulatingTimer = new Timer(SimuTimerOnCallback, null, 100, Timeout.Infinite);
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }

        /// <summary>
        /// 模拟定时器：根据积分时间，定时将数据发送出去
        /// </summary>
        /// <param name="state"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void SimuTimerOnCallback(object state)
        {
            if (_isConveyorRunning)
            {
                var linesCount = (int)(125 / _lineIntegrationTime);
                for (int i = 0; i < linesCount; i++)
                {
                    var line = SimulateNextLine();
                    if (line != null)
                    {
                        ShowLine(line);
                    }
                }
            }
            if (_simulatingTimer != null)
                _simulatingTimer.Change(100, Timeout.Infinite);
        }

        /// <summary>
        /// 模拟产生下一线扫描数据
        /// </summary>
        /// <returns></returns>
        private DisplayScanlineDataBundle SimulateNextLine()
        {
            if (_cachedLines.Count == 0)
            {
                if (_lastImageFinishTime == null)
                {
                    LoadAndFillNextImage();
                }
                else if (DateTime.Now - _lastImageFinishTime >= TimeSpan.FromSeconds(_imageInterval))
                {
                    LoadAndFillNextImage();
                }
            }

            DisplayScanlineDataBundle result = null;

            // 从头部取第一条扫描线用于显示
            if (_cachedLines.Count > 0)
            {
                result = _cachedLines.First.Value;
                _cachedLines.RemoveFirst();

                if (_cachedLines.Count == 0)
                {
                    // 一个图像显示完毕，计数增加
                    BagCounterService.Service.Increase(false);
                    _lastImageFinishTime = DateTime.Now;
                }
            }

            return result;
        }
        private void LoadAndFillNextImage()
        {
            string path = string.Empty;
            var image = _imageSelector.LoadNextImage(out path);
            if (image != null)
            {
                FillImageLinesToCache(image,path);

                if (NewImageIsReady != null)
                {
                    NewImageIsReady();
                }
            }
        }

        private void FillImageLinesToCache(XRayScanlinesImage image,string path)
        {
            // 将图像数据转换为编号后的队列，并取当前最大编号+image.View1.ScanLinesCount作为该图像中数据的最大编号
            var newLines = image.ToDisplayXRayMatLineDataBundles(MaxLineNumber + image.View1Data.ScanLinesCount);
            PaintingRegionsService.Service.AddXRayFileInfo(new XRayFileInfoForMark(null,path, MaxLineNumber, MaxLineNumber + image.View1Data.ScanLinesCount));
            if (newLines != null && newLines.Count > 0)
            {
                MaxLineNumber += newLines.Count;
                foreach (var line in newLines)
                {
                    _cachedLines.AddLast(line);
                }
            }
        }

        public void ClearCachedlines()
        {
            _cachedLines.Clear();
        }

        /// <summary>
        /// 用户按下输送机启动键，继续培训进程
        /// </summary>
        public void OnConveyorStart()
        {
            _isConveyorRunning = true;
        }

        /// <summary>
        /// 用户按下输送机停止键，暂停培训进程
        /// </summary>
        public void OnConveyorStop()
        {
            _isConveyorRunning = false;
        }

        public void CleanTimer()
        {
            if (_simulatingTimer!=null)
            {
                _simulatingTimer.Dispose();
                _simulatingTimer = null;
            }
        }

        /// <summary>
        /// 培训图像不支持前拉、回拉
        /// </summary>
        /// <returns></returns>
        public IEnumerable<DisplayScanlineDataBundle> PullBack()
        {
            return null;
        }

        /// <summary>
        /// 培训图像不支持前拉、回拉
        /// </summary>
        /// <returns></returns>
        public IEnumerable<DisplayScanlineDataBundle> PullForward()
        {
            return null;
        }

        /// <summary>
        /// 显示图像列
        /// </summary>
        /// <param name="bundle"></param>
        private void ShowLine(DisplayScanlineDataBundle bundle)
        {
            if (DataReady != null)
            {
                DataReady(bundle);
            }
        }

        /// <summary>
        /// 培训图像选择：根据所选定的培训模式，选择培训图像
        /// </summary>
        class TrainingImageSelector : IDisposable
        {
            /// <summary>
            /// 培训图像的存储目录
            /// </summary>
            private const string TrainingImageLibRootPath = @"D:\SecurityScanner\TrainingImages";

            private FileSystemWatcher _trainingImagesLibWatcher;

            /// <summary>
            /// 培训图像文件信息列表
            /// </summary>
            private List<FileInfo> _trainingImagesInfos;

            private TrainingImageLoopMode _loopMode;

            /// <summary>
            /// 上一个文件的索引
            /// </summary>
            private int _lastFileIndex = -1;

            private Random _random = new Random();

            public TrainingImageSelector(TrainingImageLoopMode loopMode)
            {
                _loopMode = loopMode;
                LoadTrainingImageInfos();
                SetupFileSystemWatcher();
            }

            ~TrainingImageSelector()
            {
                Dispose(false);
            }

            /// <summary>
            /// 建立监控培训图像的watcher
            /// </summary>
            private void SetupFileSystemWatcher()
            {
                try
                {
                    _trainingImagesLibWatcher = new FileSystemWatcher(TrainingImageLibRootPath);
                    _trainingImagesLibWatcher.EnableRaisingEvents = true;
                    _trainingImagesLibWatcher.Filter = "*.xray";
                    _trainingImagesLibWatcher.Created += OnTrainingImageCreatedOrDeleted;
                    _trainingImagesLibWatcher.Deleted += OnTrainingImageCreatedOrDeleted;
                }
                catch (Exception e)
                {
                    Tracer.TraceException(e);
                }
            }

            /// <summary>
            /// 加载所有培训图像
            /// </summary>
            private void LoadTrainingImageInfos()
            {
                try
                {
                    var di = new DirectoryInfo(TrainingImageLibRootPath);
                    _trainingImagesInfos = di.GetFiles("*.xray").ToList();
                }
                catch (Exception e)
                {
                    Tracer.TraceException(e);
                }
            }

            /// <summary>
            /// 处理某个tip库文件的增加或删除事件
            /// </summary>
            /// <param name="fileSystemEventArgs"></param>
            [MethodImpl(MethodImplOptions.Synchronized)]
            private void OnTrainingImageCreatedOrDeleted(object sender, FileSystemEventArgs fileSystemEventArgs)
            {
                var fileInfos = _trainingImagesInfos;
                if (fileInfos == null)
                {
                    return;
                }

                try
                {
                    if (fileSystemEventArgs.ChangeType == WatcherChangeTypes.Created)
                    {
                        var fi = new FileInfo(fileSystemEventArgs.FullPath);
                        fileInfos.Add(fi);
                    }
                    else if (fileSystemEventArgs.ChangeType == WatcherChangeTypes.Deleted)
                    {
                        var fi =
                            fileInfos.Find(
                                info =>
                                    string.Equals(info.FullName, fileSystemEventArgs.FullPath,
                                        StringComparison.OrdinalIgnoreCase));
                        if (fi != null)
                        {
                            fileInfos.Remove(fi);
                        }
                    }
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception);
                }
            }

            /// <summary>
            /// 根据当前的图像模拟策略，读入下一个模拟图像到内存
            /// </summary>
            [MethodImpl(MethodImplOptions.Synchronized)]
            public XRayScanlinesImage LoadNextImage(out string path)
            {
                path = string.Empty;
                if (_disposed)
                {
                    return null;
                }

                if (_trainingImagesInfos != null && _trainingImagesInfos.Count > 0)
                {
                    if (_loopMode == TrainingImageLoopMode.NoLoop)
                    {
                        _lastFileIndex++;
                    }
                    else if (_loopMode == TrainingImageLoopMode.RandomLoop)
                    {
                        _lastFileIndex = _random.Next(_trainingImagesInfos.Count);
                    }
                    else if (_loopMode == TrainingImageLoopMode.SequentialLoop)
                    {
                        _lastFileIndex++;
                        _lastFileIndex = _lastFileIndex % _trainingImagesInfos.Count;
                    }

                    if (_lastFileIndex < _trainingImagesInfos.Count)
                    {
                        var filePath = _trainingImagesInfos[_lastFileIndex].FullName;
                        path = filePath;
                        return XRayScanlinesImage.LoadFromDiskFile(filePath);
                    }
                }

                return null;
            }

            private bool _disposed = false;

            private void Dispose(bool disposing)
            {
                if (_disposed)
                {
                    return;
                }

                if (disposing)
                {
                    lock (this)
                    {
                        _trainingImagesLibWatcher.Created -= OnTrainingImageCreatedOrDeleted;
                        _trainingImagesLibWatcher.Deleted -= OnTrainingImageCreatedOrDeleted; _trainingImagesLibWatcher.Dispose();
                        _trainingImagesLibWatcher.Dispose();
                        _trainingImagesLibWatcher = null;
                    }
                }

                _disposed = true;
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        public IEnumerable<ImageRecord> GetShowingImages()
        {
            throw new NotImplementedException();
        }
    }
}
