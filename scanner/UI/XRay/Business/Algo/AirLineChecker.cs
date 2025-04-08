using System;
using System.Collections.Generic;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;

namespace UI.XRay.Business.Algo
{
    /// <summary>
    /// 空气扫描线校验：对输入的一线数据进行判断，如果其均值及方差等特性满足空气值条件，则判定为空气扫描
    /// </summary>
    public class AirLineChecker
    {
        /// <summary>
        /// 背景阈值的下限，一般为64000左右。
        /// </summary>
        private readonly int _backgroundLower;

        private int _startMarginCount;

        private int _endMarginCount;

        /// <summary>
        /// 一维滤波窗口
        /// </summary>
        int _filterSize = 9;
        /// <summary>
        /// 阈值
        /// </summary>
        int _pixelCountThr = 5;
        /// <summary>
        /// 窗口半径
        /// </summary>
        int _radius = 4;
        /// <summary>
        /// 局部最大值
        /// </summary>
        ushort _localMax = 0;
        /// <summary>
        /// 低于阈值的点数量
        /// </summary>
        int _lowThanThrcount = 0;

        /// <summary>
        /// 构造空气扫描线判定实例
        /// </summary>
        /// <param name="startMarginCount">计算起始的探测通道号，最小值为0</param>
        /// <param name="endMarginCount">计算终结的探测通道号，最小值为通道总数-1</param>
        /// <param name="backgroundLower">空气扫描线均值的下限</param>
        public AirLineChecker(int startMarginCount, int endMarginCount, int backgroundLower = 64000)
        {
            if (startMarginCount < 0)
            {
                throw new ArgumentException("startMarginCount");
            }

            _startMarginCount = startMarginCount;
            _endMarginCount = endMarginCount;

            _backgroundLower = backgroundLower;

            InitConfig();
            _radius = (_filterSize - 1) / 2;
        }

        /// <summary>
        /// 判断一线数据是否为空气扫描,同时更新其内部属性
        /// </summary>
        /// <param name="line">要进行判定的一线数据</param>
        public bool TestAirLine(ClassifiedLineData line)
        {
            int length = line.XRayData.Length;
            _lowThanThrcount = 0;
            for (int i = 0; i < length; i++)
            {
                _localMax = 0;
                for (int j = i - _radius; j <= i + _radius; j++)
                {
                    if (j >= 0 && j < length)
                    {
                        _localMax = Math.Max(_localMax, line.XRayData[j]);
                    }
                }
                if (_localMax < _backgroundLower)
                {
                    _lowThanThrcount++;
                }
            }
            return _lowThanThrcount < _pixelCountThr ? true : false;
        }

        //public bool TestAirLine(ClassifiedLineData line)
        //{
        //    var start = 0;
        //    var end = line.XRayData.Length - 1;
        //    int block = 32;
        //    for (int i = start; i <= end - block; i += block)
        //    {
        //        long sumLow = 0;
        //        var count = 0;
        //        for (int j = 0; j < block; j++)
        //        {
        //            sumLow += line.XRayData[i + j];
        //            count++;
        //        }
        //        if (count > 0)
        //        {
        //            // 将均值与阈值进行比较，判断是否为空气值
        //            if (sumLow / count <= _backgroundLower)
        //                return false;
        //        }
        //    }
        //    return true;
        //}

        private void InitConfig()
        {
            if (!ScannerConfig.Read(ConfigPath.PreProcTestFilterSize,out _filterSize))
            {
                this._filterSize = 9;
            }
            if (!ScannerConfig.Read(ConfigPath.PreProcTestPixelCountThr, out _pixelCountThr))
            {
                _pixelCountThr = 5;
            }
            Tracer.TraceInfo($"[AirLineChecker] filterSize: {_filterSize}, pixelCountThr: {_pixelCountThr}");
        }
    }
}
