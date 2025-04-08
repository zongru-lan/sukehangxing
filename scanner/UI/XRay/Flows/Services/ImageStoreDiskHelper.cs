using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;

namespace UI.XRay.Flows.Services
{
    /// <summary>
    /// 图像存储磁盘的空间助手类
    /// </summary>
    public static class ImageStoreDiskHelper
    {
        /// <summary>
        /// 图像文件存储根路径
        /// </summary>
        private static string _storeRoot;

        /// <summary>
        /// 用于获取磁盘信息
        /// </summary>
        private static DriveInfo _diskDriveInfo;

        /// <summary>
        /// 获取当前的磁盘空间总大小：字节数
        /// </summary>
        public static long TotalSize
        {
            get
            {
                if (_diskDriveInfo == null)
                {
                    _diskDriveInfo = new DriveInfo("D");
                }

                return _diskDriveInfo.TotalSize; 
            }
        }

        /// <summary>
        /// 获取当前的磁盘剩余空间总大小：字节数
        /// </summary>
        public static long TotalFreeSpace
        {
            get
            {
                if (_diskDriveInfo == null)
                {
                    _diskDriveInfo = new DriveInfo("D");
                }
                return _diskDriveInfo.TotalFreeSpace;
            }
        }

        public static double TotalUsedSpaceGB
        {
            get { return TotalSizeGB - TotalFreeGB; }
        }

        /// <summary>
        /// 总剩余GB
        /// </summary>
        public static double TotalFreeGB
        {
            get { return TotalFreeSpace/1024.0/1024.0/1024.0; }
        }

        public static double TotalSizeGB
        {
            get { return TotalSize/1024.0/1024.0/1024.0; }
        }

        public static string DiskName
        {
            get { return _diskDriveInfo.Name; }
        }

        /// <summary>
        /// 获取最新的空闲空间占比
        /// </summary>
        public static double GetFreeSpaceRatio()
        {
            if (TotalSize > 0)
            {
                return TotalFreeSpace / (double)TotalSize;
            }
            else
            {
                return 1;
            }
        }

        static ImageStoreDiskHelper()
        {
            try
            {
                if (!ScannerConfig.Read(ConfigPath.SystemImageStorePath, out _storeRoot))
                {
                    _storeRoot = @"D:\SecurityScanner\Images";
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            try
            {
                var root = Path.GetPathRoot(_storeRoot);
                if (string.IsNullOrEmpty(root))
                {
                    root = "D";
                }

                _diskDriveInfo = new DriveInfo(root);
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }
    }
}
