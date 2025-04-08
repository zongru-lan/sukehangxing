using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.Entities;

namespace UI.XRay.Business.Algo
{
    /// <summary>
    /// 动态满度更新流程
    /// </summary>
    public class DynamicAirDataAutoUpdateRoutine : AirDataAutoUpdateRoutineBase
    {
        /// <summary>
        /// 缓存50线，取中间30线更新满度
        /// </summary>
        private int DynamicUpdateAirCacheLinesCount = 50;

        //动态满度更新数据，包括原始数据和此线数据中的满度区域
        private DynamicUpdateAirDatas _dynamicUpdateAirDatas;

        private readonly DynamicAirDataAutoUpdater _dynamicAirDataAutoUpdater;

        public DynamicAirDataAutoUpdateRoutine(DetectViewIndex viewIndex, ushort airTh, int marginStart, int marginEnd, int avgFilterindowSize, bool onlyAllAirLineUpdate,int cacheLineCount)
        {
            ViewIndex = viewIndex;
            DynamicUpdateAirCacheLinesCount = cacheLineCount;
            _dynamicAirDataAutoUpdater = new DynamicAirDataAutoUpdater(viewIndex);
            _dynamicAirDataAutoUpdater.StartService(airTh, marginStart, marginEnd,  avgFilterindowSize, onlyAllAirLineUpdate);
        }

        /// <summary>
        /// 启用或禁用更新流程。当射线打开并且跳过上升沿后，可以开始更新满度
        /// </summary>
        /// <param name="enable"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public override void Enable(bool enable)
        {
            IsEnabled = enable;
            if (!IsEnabled)
            {
                LinesCache.Clear();
            }
        }

        public override void SetReferenceAir(ScanlineData reference)
        {
            if (_dynamicAirDataAutoUpdater != null)
            {
                _dynamicAirDataAutoUpdater.ReferenceAir = reference;
            }
        }

        /// <summary>
        /// 尝试更新属性数据(满度）
        /// </summary>
        /// <param name="data">新采集的数据，不能为空</param>
        /// <param name="result">更新后的属性数据，满度，如果返回值为false，则为null，表明数据量不够</param>
        /// <param name="normalizedData"></param>
        /// <returns>更新是否成功</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public override bool TryUpdate(RawScanlineData data, out ScanlineData result, ScanlineData normalizedData = null)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (IsEnabled)
            {
                //此时是扫描前更新
                if (normalizedData == null)
                {
                    //扫描前更新
                    return TryUpdateBeforeScanning(data, out result);
                }

                return TryUpdateRefrenceAir(data, normalizedData, out result);
            }

            result = null;
            return false;
        }

        private bool TryUpdateRefrenceAir(RawScanlineData data, ScanlineData normalizedData, out ScanlineData result)
        {
            if (IsEnabled)
            {
                if (_dynamicUpdateAirDatas == null)
                {
                    _dynamicUpdateAirDatas = new DynamicUpdateAirDatas(DynamicUpdateAirCacheLinesCount, data.LineLength);
                }

                if (_dynamicUpdateAirDatas.Count < DynamicUpdateAirCacheLinesCount)
                {
                    _dynamicUpdateAirDatas.CopyData(normalizedData.Low, _dynamicUpdateAirDatas.Count);
                    _dynamicUpdateAirDatas.RawScanlineDatas.Add(data);
                }

                if (_dynamicUpdateAirDatas.Count >= DynamicUpdateAirCacheLinesCount)
                {
                    _dynamicAirDataAutoUpdater.Enqueue(_dynamicUpdateAirDatas);
                    _dynamicUpdateAirDatas = null;

                    if (_dynamicAirDataAutoUpdater.TryGetAirData(out result))
                    {
                        return true;
                    }
                }
            }            

            result = null;
            return false;
        }

        private bool TryUpdateBeforeScanning(RawScanlineData data, out ScanlineData result)
        {
            // 如果当前处于缓存数据阶段，且缓冲区未满，则继续缓存,并在数据存储完毕后开始更新
            if (IsEnabled && (LinesCache.Count < BeforeScanningUpdateCacheLinesCount))
            {
                LinesCache.Add(data);
            }

            // 缓冲区已满，则计算均值并更新
            if (IsEnabled && LinesCache.Count >= BeforeScanningUpdateCacheLinesCount)
            {
                result = XRayLineDataHelper.GetChannelAvg(LinesCache, ViewIndex);
                // 清空缓冲区，等待下次更新
                ClearCache();
                return true;
            }

            result = null;
            return false;
        }

        public override void ClearAirData()
        {
            if (_dynamicAirDataAutoUpdater != null)
            {
                _dynamicAirDataAutoUpdater.ClearAirData();
            }
            if (_dynamicUpdateAirDatas !=null)
            {
                _dynamicUpdateAirDatas.ClearCache();
            }
        }
    }
}
