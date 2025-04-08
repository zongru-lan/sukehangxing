using System.Collections.Generic;
using UI.XRay.Business.Entities;

namespace UI.XRay.Business.Algo
{
    /// <summary>
    /// 计算多线数据的均值：作为本底、满度更新的工具类
    /// </summary>
    public class XRayLineDataHelper
    {
        /// <summary>
        /// 根据多列输入数据，计算计算每个探测点输出的均值。
        /// </summary>
        /// <param name="dataLinesList"></param>
        /// <param name="view"></param>
        /// <returns></returns>
        public static ScanlineData GetChannelAvg(List<RawScanlineData> dataLinesList, DetectViewIndex view)
        {
            if (dataLinesList == null || dataLinesList.Count == 0)
            {
                return null;
            }

            int[] leSum = null;
            int[] heSum = null;

            if (dataLinesList.Count != 0)
            {
                if (dataLinesList[0].Low != null)
                {
                    leSum = new int[dataLinesList[0].LineLength];
                }
                if (dataLinesList[0].High != null)
                {
                    heSum = new int[dataLinesList[0].LineLength];
                }

                // 对所有线数据的相同位置探测器的输出进行累加，求和
                foreach (var line in dataLinesList)
                {
                    if (line.Low != null)
                    {
                        for (int i = 0; i < line.LineLength; i++)
                        {
                            leSum[i] += line.Low[i];
                        }
                    }

                    if (line.High != null)
                    {
                        for (int i = 0; i < line.LineLength; i++)
                        {
                            heSum[i] += line.High[i];
                        }
                    }
                }
            }

            ushort[] leAvg = null;
            ushort[] heAvg = null;
            int linesCount = dataLinesList.Count;

            // 对累加和除以线数，得到均值
            if (leSum != null)
            {
                leAvg = new ushort[leSum.Length];
                for (int i = 0; i < leAvg.Length; i++)
                {
                    leAvg[i] = (ushort)(leSum[i] / linesCount);
                }
            }

            // 计算高能
            if (heSum != null)
            {
                heAvg = new ushort[heSum.Length];
                for (int i = 0; i < heAvg.Length; i++)
                {
                    heAvg[i] = (ushort)(heSum[i] / linesCount);
                }
            }

            return new ScanlineData(view, leAvg, heAvg);
        }
    }
}
