using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Common.Utilities;

namespace UI.XRay.Flows.Controllers
{
    /// <summary>
    /// 磁盘空间管理控制器
    /// </summary>
    public class DiskSpaceManageController
    {
        private int _emergencyFreeDiskSpaceRatio;

        /// <summary>
        /// 紧急清理磁盘剩余空间比例阈值：当低于此阈值时将会自动删除最早的图像（1-99）
        /// </summary>
        public int EmergencyFreeDiskSpaceRatio
        {
            get { return _emergencyFreeDiskSpaceRatio; }
            set
            {
                _emergencyFreeDiskSpaceRatio = value;
            }
        }

        private int _startCleanupFreeDiskSpaceRatioThreshold;

        /// <summary>
        /// 磁盘剩余空间比例阈值：当低于此阈值时将会自动删除最早的图像（1-99）
        /// </summary>
        public int StartCleanupFreeDiskSpaceRatioThreshold
        {
            get { return _startCleanupFreeDiskSpaceRatioThreshold; }
            set
            {
                _startCleanupFreeDiskSpaceRatioThreshold = value; 
            }
        }

        private int _countOfImagesToDelete;

        /// <summary>
        /// 自动清理旧图像以释放空间时，每次需要删除的图像总数
        /// </summary>
        public int CountOfImagesToDelete
        {
            get { return _countOfImagesToDelete; }
            set
            {
                _countOfImagesToDelete = value;
            }
        }

        private int _stopDiskSpaceCleanupRatio;

        public int StopDiskSpaceCleanupRatio
        {
            get{ return _stopDiskSpaceCleanupRatio; }
            set { _stopDiskSpaceCleanupRatio = value; }
        }

        private DateTime _startDiskSpaceCleanupTime;

        public DateTime StartDiskSpaceCleanupTime
        {
            get { return _startDiskSpaceCleanupTime; }
            set { _startDiskSpaceCleanupTime = value; }
        }

        private DateTime _stopDiskSpaceCleanupTime;

        public DateTime StopDiskSpaceCleanupTime
        {
            get { return _stopDiskSpaceCleanupTime; }
            set { _stopDiskSpaceCleanupTime = value; }
        }

        public DiskSpaceManageController()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                if (!ScannerConfig.Read(ConfigPath.StartDiskSpaceCleanupEmergencyRatio, out _emergencyFreeDiskSpaceRatio))
                {
                    _emergencyFreeDiskSpaceRatio = 20;
                }

                if (!ScannerConfig.Read(ConfigPath.StartDiskSpaceCleanupRatio, out _startCleanupFreeDiskSpaceRatioThreshold))
                {
                    _startCleanupFreeDiskSpaceRatioThreshold = 30;
                }
                if (!ScannerConfig.Read(ConfigPath.StopDiskSpaceCleanupRatio, out _stopDiskSpaceCleanupRatio))
                {
                    _stopDiskSpaceCleanupRatio = 60;
                }
                string diskSpaceCleanupTime;
                StartDiskSpaceCleanupTime = DateTimeHelper.GetDateTime(
                        !ScannerConfig.Read(ConfigPath.StartDiskSpaceCleanupTime, out diskSpaceCleanupTime)
                            ? "20:00:00"
                            : diskSpaceCleanupTime, "20:00:00",20);

                StopDiskSpaceCleanupTime = DateTimeHelper.GetDateTime(
                        !ScannerConfig.Read(ConfigPath.StopDiskSpaceCleanupTime, out diskSpaceCleanupTime)
                            ? "22:00:00"
                            : diskSpaceCleanupTime, "22:00:00", 22);

                if (!ScannerConfig.Read(ConfigPath.ImagesCountToDeleteWhenCleanup, out _countOfImagesToDelete))
                {
                    _countOfImagesToDelete = 100;
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        public void SaveSettings()
        {
            EmergencyFreeDiskSpaceRatio = Math.Min(90, EmergencyFreeDiskSpaceRatio);
            EmergencyFreeDiskSpaceRatio = Math.Max(10, EmergencyFreeDiskSpaceRatio);

            StartCleanupFreeDiskSpaceRatioThreshold = Math.Min(90, StartCleanupFreeDiskSpaceRatioThreshold);
            StartCleanupFreeDiskSpaceRatioThreshold = Math.Max(10, StartCleanupFreeDiskSpaceRatioThreshold);

            StopDiskSpaceCleanupRatio = Math.Min(90, StopDiskSpaceCleanupRatio);
            StopDiskSpaceCleanupRatio = Math.Max(10, StopDiskSpaceCleanupRatio);

            StopDiskSpaceCleanupRatio = Math.Min(90, StopDiskSpaceCleanupRatio);
            StopDiskSpaceCleanupRatio = Math.Max(10, StopDiskSpaceCleanupRatio);

            CountOfImagesToDelete = Math.Min(1000, CountOfImagesToDelete);
            CountOfImagesToDelete = Math.Max(10, CountOfImagesToDelete);

            ScannerConfig.Write(ConfigPath.StartDiskSpaceCleanupEmergencyRatio, _emergencyFreeDiskSpaceRatio);
            ScannerConfig.Write(ConfigPath.StartDiskSpaceCleanupRatio, _startCleanupFreeDiskSpaceRatioThreshold);
            ScannerConfig.Write(ConfigPath.StopDiskSpaceCleanupRatio, _stopDiskSpaceCleanupRatio);

            ScannerConfig.Write(ConfigPath.StartDiskSpaceCleanupTime, _startDiskSpaceCleanupTime.ToString("HH:mm:ss"));
            ScannerConfig.Write(ConfigPath.StopDiskSpaceCleanupTime, _stopDiskSpaceCleanupTime.ToString("HH:mm:ss"));

            ScannerConfig.Write(ConfigPath.ImagesCountToDeleteWhenCleanup, _countOfImagesToDelete);
        }
    }
}
