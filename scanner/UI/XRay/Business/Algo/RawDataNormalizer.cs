using UI.XRay.Business.Entities;

namespace UI.XRay.Business.Algo
{
    /// <summary>
    /// 归一化处理组件：对某个视角的输入的一列探测数据，进行归一化处理：去除本底、满度影响
    /// 
    /// <remarks>需要为每个视角分别构造一个归一化处理对象，因此使用NonShared模式。
    /// 本类设计不是多线程安全的，因此应该避免在多线程中对类实例的调用</remarks>
    /// </summary>
    public class RawDataNormalizer : IRawXRayDataNormalizer
    {
        /// <summary>
        /// 低能数据的归一化因子，即归一化系数，通过本底、满度进行计算， 通道A
        /// </summary>
        private double[] _lowNmlFactor;

        /// <summary>
        /// 高能数据的归一化因子，通道A
        /// </summary>
        private double[] _highNmlFactor;

        /// <summary>
        /// 满度数据，通道A
        /// </summary>
        private ScanlineData _airValue;

        /// <summary>
        /// 本底数据，通道A
        /// </summary>
        private ScanlineData _groundValue;

        /// <summary>
        /// 同步锁：在更新满度或本底时，与归一化过程互斥
        /// </summary>
        private readonly object _sync = new object();

        /// <summary>
        /// 灰度本底：设定的归一化输出的最小值，实际输出可能会小于此值
        /// </summary>
        private const ushort NormalizedLower = 2000;

        /// <summary>
        /// 归一化后的最大输出值：在计算归一化系数时，将归一化输出限定为[2000, 62258+2000]，但由于射线波动等原因，实际的输出可能
        /// 超过此范围，即可能小于2000，也可能大于62258+2000，这是正常的光子涨落噪声
        /// </summary>
        private const ushort NormalizedUpper = 62258;

        /// <summary>
        /// 最大输出值，如果某输出大于此值，将认定为此值
        /// </summary>
        private const ushort MaxOutput = 65530;

        /// <summary>
        /// 归一化具体操作：直接修改输入的数据后作为输出。
        /// </summary>
        /// <param name="rawLe">输入的一列原始探测数据</param>
        /// <param name="rawHe">输入的一列原始探测数据</param>
        public void Normalize(ushort[] rawLe, ushort[] rawHe)
        {
            lock (_sync)
            {
                if (rawLe != null && _lowNmlFactor != null && _groundValue.Low != null)
                {
                    InternalNormalize(rawLe, _groundValue.Low, _lowNmlFactor);
                }

                if (rawHe != null && _highNmlFactor != null && _groundValue.High != null)
                {
                    InternalNormalize(rawHe, _groundValue.High, _highNmlFactor);
                }
            }
        }

        /// <summary>
        /// 执行实际的归一化操作
        /// </summary>
        /// <param name="rawXRayData">原始的低能或高能数据</param>
        /// <param name="ground">本底数据</param>
        /// <param name="nmlFactors">对应的归一化系数</param>
        private void InternalNormalize(ushort[] rawXRayData, ushort[] ground, double[] nmlFactors)
        {
            if (rawXRayData != null && nmlFactors != null && ground != null)
            {
                if (rawXRayData.Length != ground.Length || rawXRayData.Length != nmlFactors.Length || ground.Length != nmlFactors.Length)
                    return;

                for (var i = 0; i < rawXRayData.Length; i++)
                {
                    var result = (int)((rawXRayData[i] - ground[i]) * nmlFactors[i] + NormalizedLower);

                    // 将输出的取值范围限制在：[0, 65530] 之内 
                    result = result < 0 ? 0 : result;
                    rawXRayData[i] = result > MaxOutput ? (ushort)MaxOutput : (ushort)result;
                }
            }
        }

        /// <summary>
        /// 根据最新的本底、满度更新归一化系数。仅限内部使用
        /// </summary>
        /// <param name="captureChannel">通道类型，分为A、B</param>
        private void UpdateNormalizationFactors()
        {
            if (_groundValue != null && _airValue != null)
            {
                // 分别计算高能、低能的归一化系数
                CalcNmlFactors(out _lowNmlFactor, _groundValue.Low, _airValue.Low);
                CalcNmlFactors(out _highNmlFactor, _groundValue.High, _airValue.High);
            }
        }

        /// <summary>
        /// 根据本底、满度，计算归一化系数
        /// </summary>
        /// <param name="nmlFactors"></param>
        /// <param name="ground"></param>
        /// <param name="air"></param>
        private void CalcNmlFactors(out double[] nmlFactors, ushort[] ground, ushort[] air)
        {
            if (ground != null && air != null)
            {
                nmlFactors = new double[ground.Length];

                for (var i = 0; i < ground.Length; i++)
                {
                    int diff = air[i] - ground[i];

                    // todo: 对于本底、满度差异太小的探测器，应该将其标记为异常点
                    if (diff <= 0)
                    {
                        diff = 1;
                    }

                    nmlFactors[i] = (float)(NormalizedUpper) / diff;
                }

                return;
            }

            nmlFactors = null;
        }

        /// <summary>
        /// 更新满度数据：更新归一化系数。
        /// </summary>
        /// <param name="airValue">低能、高能的满度数据</param>
        //public void ResetAirValue(XRayLineData airValue)
        //{
        //    if (airValue != null)
        //    {
        //        lock (_sync)
        //        {
        //            if (airValue.CaptureChannel == CaptureChannel.A)
        //            {
        //                _airA = airValue;
        //            }
        //            else
        //            {
        //                _airB = airValue;
        //            }
        //            UpdateNormalizationFactors(airValue.CaptureChannel);
        //        }
        //    }
        //}

        public void ResetAir(ScanlineData airValue)
        {
            if (airValue != null)
            {
                lock (_sync)
                {
                    _airValue = airValue;
                    UpdateNormalizationFactors();
                }
            }
        }

        public void ResetGround(ScanlineData groundValue)
        {
            if (groundValue != null)
            {
                lock (_sync)
                {
                    _groundValue = groundValue;
                    UpdateNormalizationFactors();
                }
            }
        }
    }
}
