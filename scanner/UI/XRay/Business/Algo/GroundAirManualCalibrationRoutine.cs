using System.Collections.Generic;
using UI.XRay.Business.Entities;

namespace UI.XRay.Business.Algo
{
    /// <summary>
    /// 单个视角的本底或者满度的手动更新程序，用于手动更新单个视角的本底或者满度
    /// </summary>
    public class GroundAirManualCalibrationRoutine
    {
        /// <summary>
        /// 缓存要用来更新本底的数据，分视角和通道
        /// </summary>
        private readonly List<RawScanlineData> _rawDataList = new List<RawScanlineData>();

        /// <summary>
        /// Uses 32 lines to update ground value
        /// </summary>
        private const int LinesCount = 32;

        /// <summary>
        /// 对应的探测视角
        /// </summary>
        public DetectViewIndex ViewIndex { get; private set; }

        public GroundAirManualCalibrationRoutine(DetectViewIndex index)
        {
            ViewIndex = index;
        }

        /// <summary>
        /// 尝试更新本底或者满度
        /// 当缓存的数据达到要求时，即输出本底或者满度的均值
        /// </summary>
        /// <param name="rawdata"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TryUpdate(RawScanlineData rawdata, out ScanlineData result)
        {
            if (rawdata != null && _rawDataList.Count < LinesCount)
            {
                _rawDataList.Add(rawdata);
            }

            // 缓冲区已满，则更新本底
            if (_rawDataList.Count >= LinesCount )
            {
                result = XRayLineDataHelper.GetChannelAvg(_rawDataList, ViewIndex);

                _rawDataList.Clear();
                return true;
            }

            result = null;
            return false;
        }
    }
}
