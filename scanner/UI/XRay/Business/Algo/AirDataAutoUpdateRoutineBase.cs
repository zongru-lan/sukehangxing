using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.Entities;

namespace UI.XRay.Business.Algo
{
    public abstract class AirDataAutoUpdateRoutineBase
    {
        /// <summary>
        /// 缓存要用来更新满度的数据
        /// </summary>
        protected readonly List<RawScanlineData> LinesCache = new List<RawScanlineData>();

        protected int BeforeScanningUpdateCacheLinesCount = 24;

        /// <summary>
        /// 对应的探测视角,将用于输出
        /// </summary>
        public DetectViewIndex ViewIndex { get; protected set; }

        /// <summary>
        /// 是否启用了自动更新功能.如果启用，则一直尝试更新
        /// </summary>
        public bool IsEnabled { get; protected set; }

        public abstract void SetReferenceAir(ScanlineData reference);

        /// <summary>
        /// 尝试更新属性数据(满度）
        /// </summary>
        /// <param name="data">新采集的数据，不能为空</param>
        /// <param name="result">更新后的属性数据，满度，如果返回值为false，则为null，表明数据量不够</param>
        /// <param name="normalizedData"></param>
        /// <returns>更新是否成功</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public virtual bool TryUpdate(RawScanlineData data, out ScanlineData result, ScanlineData normalizedData = null)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (!IsEnabled)
            {
                result = null;
                return false;
            }

            // 如果当前处于缓存数据阶段，且缓冲区未满，则继续缓存,并在数据存储完毕后开始更新
            if (IsEnabled && (LinesCache.Count < BeforeScanningUpdateCacheLinesCount))
            {
                LinesCache.Add(data);
            }

            // 缓冲区已满，则计算均值并更新
            if (IsEnabled && LinesCache.Count >= BeforeScanningUpdateCacheLinesCount)
            {
                result = XRayLineDataHelper.GetChannelAvg(LinesCache, ViewIndex);

                IsEnabled = false;
                // 清空缓冲区，等待下次更新
                ClearCache();
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// 启用或禁用更新流程。当射线打开并且跳过上升沿后，可以开始更新满度
        /// </summary>
        /// <param name="enable"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public virtual void Enable(bool enable)
        {
            IsEnabled = enable;
            //if (!IsEnabled)
            //{
            //    ClearCache();
            //}
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        protected void ClearCache()
        {
            LinesCache.Clear();
        }

        public abstract void ClearAirData();
    }
}
