using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Common.Utilities;
using UI.XRay.Flows.Services.Db;

namespace UI.XRay.Flows.Services
{
    /// <summary>
    /// 旧图像清理服务：在独立的后台线程中运行，不定时清理磁盘空间
    /// </summary>
    public class OldImagesCleanupService
    {
        /// <summary>
        /// 单实例服务
        /// </summary>
        public static OldImagesCleanupService Service { get; private set; }

        static OldImagesCleanupService()
        {
            Service = new OldImagesCleanupService();
        }

        /// <summary>
        /// 弱事件，满度校正结束：如果参数为null则表示校正失败
        /// </summary>
        public event EventHandler<double> AlreadyUsedDiskSpaceChangedWeakEvent
        {
            add { _alreadyUsedDiskSpaceChangedWeakEvent.Add(value); }
            remove { _alreadyUsedDiskSpaceChangedWeakEvent.Remove(value); }
        }

        private readonly SmartWeakEvent<EventHandler<double>> _alreadyUsedDiskSpaceChangedWeakEvent =
            new SmartWeakEvent<EventHandler<double>>();

        /// <summary>
        /// 磁盘剩余存储空间不足的阈值：当磁盘剩余空间低于此阈值时，即刻开始清理旧图像。（10-90）百分比的分子
        /// </summary>
        private int _lowSpaceThreshold = 30;

        private double LowSpaceThresholdPercent
        {
            get { return _lowSpaceThreshold/100.0; }
        }

        /// <summary>
        /// 每次清理旧图像的数量
        /// </summary>
        private int _countOfImagesToDelete = 100;

        private int _stopDiskSpaceCleanupRatio = 60;

        private double StopDiskSpaceCleanupRatio
        {
            get { return _stopDiskSpaceCleanupRatio/100.0; }
        }

        /// <summary>
        /// 紧急开启磁盘清理的比例
        /// </summary>
        private int _startDiskSpaceCleanupEmergencyRatio = 20;
        private double StartDiskSpaceCleanupEmergencyRatio
        {
            get { return _startDiskSpaceCleanupEmergencyRatio/100.0; }
        }

        private DateTime _startDiskSpaceCleanupTime;

        private DateTime _stopDiskSpaceCleanupTime;

        /// <summary>
        /// 执行清理任务的专用线程
        /// </summary>
        private Thread _backgroundThread;

        /// <summary>
        /// 磁盘清理线程退出标志
        /// </summary>
        private bool _exitSignal = false;

        //正在紧急清理磁盘。超过了紧急清理磁盘阈值后，不论时间是否在指定时间内，清理磁盘到停止阈值
        private bool _isEmergencyCleanningDisk;

        /// <summary>
        /// true时表示要求立即清理磁盘
        /// </summary>
        public bool ManualCleanupDisk { get; set; }

        private bool _autoCleanupDisk;

        private ImageRecordDbSet _recordDbSet;

        protected OldImagesCleanupService()
        {
            try
            {
                ScannerConfig.ConfigChanged += ScannerConfigOnConfigChanged;
                LoadSettings();
                _recordDbSet = new ImageRecordDbSet();
                ManualCleanupDisk = false;
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        private void ScannerConfigOnConfigChanged(object sender, EventArgs eventArgs)
        {
            LoadSettings();
        }

        /// <summary>
        /// 从配置中读取阈值参数设定
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                if (!ScannerConfig.Read(ConfigPath.StartDiskSpaceCleanupEmergencyRatio,out _startDiskSpaceCleanupEmergencyRatio))
                {
                    _startDiskSpaceCleanupEmergencyRatio = 20;
                }

                if (!ScannerConfig.Read(ConfigPath.StartDiskSpaceCleanupRatio, out _lowSpaceThreshold))
                {
                    _lowSpaceThreshold = 30;
                }

                if (!ScannerConfig.Read(ConfigPath.ImagesCountToDeleteWhenCleanup, out _countOfImagesToDelete))
                {
                    _countOfImagesToDelete = 100;
                }

                if (!ScannerConfig.Read(ConfigPath.StopDiskSpaceCleanupRatio, out _stopDiskSpaceCleanupRatio))
                {
                    _stopDiskSpaceCleanupRatio = 60;
                }
                string diskSpaceCleanupTime;
                _startDiskSpaceCleanupTime =
                    DateTimeHelper.GetDateTime(
                        !ScannerConfig.Read(ConfigPath.StartDiskSpaceCleanupTime, out diskSpaceCleanupTime)
                            ? "20:00:00"
                            : diskSpaceCleanupTime, "20:00:00", 20);

                _stopDiskSpaceCleanupTime =
                    DateTimeHelper.GetDateTime(
                        !ScannerConfig.Read(ConfigPath.StopDiskSpaceCleanupTime, out diskSpaceCleanupTime)
                            ? "22:00:00"
                            : diskSpaceCleanupTime, "22:00:00", 22);
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        /// <summary>
        /// 启动历史图像清理服务
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void StartService()
        {
            if (_backgroundThread == null)
            {
                _exitSignal = false;

                _backgroundThread = new Thread(BackgroundThreadStart);
                _backgroundThread.IsBackground = true;
                _backgroundThread.Priority = ThreadPriority.BelowNormal;
                _backgroundThread.Start();

                Tracer.TraceInfo("OldImagesCleanupService has been started.");
            }
        }

        /// <summary>
        /// 软件退出时，停止磁盘清理服务
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void StopService()
        {
            if (_backgroundThread != null)
            {
                _exitSignal = true;
                _backgroundThread.Join();
                _backgroundThread = null;

                Tracer.TraceInfo("OldImagesCleanupService has been stopped.");
            }
        }

        /// <summary>
        /// 后台线程执行体
        /// </summary>
        /// <param name="o"></param>
        private void BackgroundThreadStart(object o)
        {
            while (!_exitSignal)
            {
                try
                {
                    while (NeedToCleanDisk())
                    {
                        ReleaseEarliestImages();

                        _alreadyUsedDiskSpaceChangedWeakEvent.Raise(this, ImageStoreDiskHelper.TotalUsedSpaceGB);

                        // 为防止因磁盘空间被其他文件占用等导致在此陷入死循环，每两次循环之间都进行检查，并放弃线程控制权
                        if (_exitSignal)
                        {
                            break;
                        }

                        Thread.Sleep(100);
                    }

                    if (_exitSignal)
                    {
                        break;
                    }

                    // 每隔5分钟检查一次磁盘：每隔0.5秒检查一次退出标志并及时退出等待
                    for (int i = 0; i < 600; i++)
                    {
                        //立即响应手动清理磁盘
                        if (_exitSignal || ManualCleanupDisk)
                        {
                            break;
                        }

                        Thread.Sleep(TimeSpan.FromSeconds(0.5));
                    }
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception, "Unexpected exception in OldImageCleanupService background thread.");
                    Thread.Sleep(10000);
                }
            }
        }

        private bool NeedToCleanDisk()
        {
            var freeRatio = ImageStoreDiskHelper.GetFreeSpaceRatio();

            //如果设置了手动清理，则先以手动清理为准
            if (ManualCleanupDisk)
            {
                //如果是设置了手动开启清理图像，则删除图像到设置的停止阈值或者手动停止清理
                if (freeRatio >= StopDiskSpaceCleanupRatio)
                {
                    ManualCleanupDisk = false;
                }
                return ManualCleanupDisk;
            }

            //如果阈值
            if (freeRatio <= StartDiskSpaceCleanupEmergencyRatio)
            {
                _isEmergencyCleanningDisk = true;
            }

            if (_isEmergencyCleanningDisk)
            {
                if (freeRatio >= StopDiskSpaceCleanupRatio)
                {
                    _isEmergencyCleanningDisk = false;
                }

                return _isEmergencyCleanningDisk;
            }

            //如果没有手动清理，则以自动清理为准
            var now = DateTime.Now;
            //指定时间段内清理磁盘
            if (DateTimeHelper.TimeHourMinuteBigger(now, _startDiskSpaceCleanupTime) &&
                DateTimeHelper.TimeHourMinuteBigger(_stopDiskSpaceCleanupTime, now) &&
                freeRatio <= LowSpaceThresholdPercent)
            {
                _autoCleanupDisk = true;
            }
            if (DateTimeHelper.TimeHourMinuteBigger(now, _stopDiskSpaceCleanupTime) ||
                freeRatio >= StopDiskSpaceCleanupRatio)
            {
                _autoCleanupDisk = false;
            }
            return _autoCleanupDisk;
        }

        /// <summary>
        /// 删除一些较早的图像
        /// </summary>
        private void ReleaseEarliestImages()
        {
            if (_recordDbSet != null)
            {
                try
                {
                    int nSuccessCnt = 0;
                    // 获取最早的若干个图像文件记录
                    var records = _recordDbSet.TakeEarliest(_countOfImagesToDelete).Where(r=>!r.IsLocked).ToList();
                    if (records != null && records.Count > 0)
                    {
                        //删除磁盘文件
                        foreach (var record in records)
                        {
                            try
                            {
                                if (!string.IsNullOrWhiteSpace(record.StorePath) && File.Exists(record.StorePath))
                                {
                                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(record.StorePath);
                                    var filePath = Path.GetDirectoryName(record.StorePath);
                                    IEnumerable files = Directory.EnumerateFiles(filePath).Where(name => Regex.IsMatch(name, $"{fileNameWithoutExtension}*"));
                                    foreach (string file in files)
                                    {
                                        File.Delete(file);
                                    }
                                }
                            }
                            catch (Exception exception)
                            {
                                Tracer.TraceError("Failed To Release Earliest Image: " + exception.Message);
                            }
                            try
                            {
                                if (!File.Exists(record.StorePath))
                                {
                                    _recordDbSet.Remove(record);//删除成功才删数据库记录
                                    nSuccessCnt++;
                                }
                            }
                            catch (Exception ex)
                            {
                                Tracer.TraceError("Failed To Release Image record: " + ex.Message);
                            }  
                        }
                        // 删除数据库索引
                        //_recordDbSet.RemoveRange(records);

                        var builder = new StringBuilder(140);
                        builder.Append("OldImagesCleanupService has removed ").Append(nSuccessCnt) .
                            Append(" images to release space,failure cnt ").Append(records.Count-nSuccessCnt);
                        Tracer.TraceInfo(builder.ToString());
                    }
                    else
                    {
                        ManualCleanupDisk = false;
                        _autoCleanupDisk = false;
                    }
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception, "Failed to take earliest image records from database.");
                }
            }
            else
            {
                Tracer.TraceError("Could not clean up disk space, because OldImagesCleanupService is not initialized properly.");
            }
        }
    }
}
