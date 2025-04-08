using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.Entities;

namespace UI.XRay.Business.Algo
{
    /// <summary>
    /// 本底数据自动更新服务
    /// </summary>
    public class GroundDataAutoUpdateRoutine
    {
        /// <summary>
        /// 上次射线由打开到关闭的时刻
        /// 为null表示射线尚未关闭
        /// </summary>
        private DateTime? _lastXRayOffMoment = DateTime.Now;

        /// <summary>
        /// 上次RawDataList缓存满的时间
        /// </summary>
        private DateTime? _lastCacheFullTime = null;

        /// <summary>
        /// 缓存要用来更新本底的数据
        /// </summary>
        private readonly List<RawScanlineData> _rawDataList = new List<RawScanlineData>();

        /// <summary>
        /// Uses 32 lines to update ground value
        /// </summary>
        private const int LinesCount = 32;

        /// <summary>
        /// 射线关闭后,经过7秒钟才开始采集本底数据；TODO：此值是否应该变化，7s是否太长
        /// </summary>
        private const double AfterXRayOff = 7;

        /// <summary>
        /// 仅缓存射线打开前7秒的数据，用于本底更新，TODO：此值是否应该变化，7s是否太长
        /// </summary>
        private const double PreXRayOn = 7;

        public DetectViewIndex ViewIndex { get; private set; }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public GroundDataAutoUpdateRoutine(DetectViewIndex index)
        {
            ViewIndex = index;
        }

        /// <summary>
        /// 射线打开：清除内部数据
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void OnXRayOn()
        {
            _lastXRayOffMoment = null;
            _rawDataList.Clear();
            _lastCacheFullTime = null;
        }

        /// <summary>
        /// 射线由发射变为关闭
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void OnXRayOff()
        {
            // 记录射线由发射到关闭的时刻
            _lastXRayOffMoment = DateTime.Now;
        }

        /// <summary>
        /// 尝试使用输入的数据去更新本底值
        /// </summary>
        /// <param name="rawData">原始采集的数据</param>
        /// <returns>true表示本底成功更新了，false表示没有更新</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool TryUpdate(RawScanlineData rawData, out ScanlineData result)
        {
            if (_lastXRayOffMoment == null)
            {
                // 射线未关闭，则无操作
                result = null;
                return false;
            }

            // 距离上次射线打开超过两秒钟，则将传入的数据进行缓存，缓存达到32线后,再等待两秒钟，如果射线仍然未打开，则更新本底
            if (DateTime.Now - _lastXRayOffMoment.Value >= TimeSpan.FromSeconds(AfterXRayOff) &&
                !CacheIsFull)
            {
                _rawDataList.Add(rawData);

                if (CacheIsFull)
                {
                    // 记录缓存满的时刻
                    _lastCacheFullTime = DateTime.Now;
                }
            }

            // 缓存已满超过两秒钟，则更新本底
            if (CanUpdateNow)
            {
                // 更新本底
                result = XRayLineDataHelper.GetChannelAvg(_rawDataList, ViewIndex);

                // 更新成功后，重置。等待下次更新
                _lastXRayOffMoment = DateTime.Now;
                _rawDataList.Clear();
                _lastCacheFullTime = null;

                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// 当前是否可以更新本底了：缓存满超过两秒钟，且射线在此期间未发射
        /// </summary>
        private bool CanUpdateNow
        {
            get
            {
                if (_lastCacheFullTime == null)
                {
                    return false;
                }
                return DateTime.Now - _lastCacheFullTime.Value >= TimeSpan.FromSeconds(PreXRayOn) && CacheIsFull;
            }
        }

        /// <summary>
        /// 缓冲区是否已经存满
        /// </summary>
        private bool CacheIsFull
        {
            get
            {
                return ((_rawDataList.Count >= LinesCount));
            }
        }
    }
}
