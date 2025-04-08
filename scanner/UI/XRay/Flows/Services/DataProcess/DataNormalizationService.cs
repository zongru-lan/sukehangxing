using UI.XRay.Business.Algo;
using UI.XRay.Business.Entities;

namespace UI.XRay.Flows.Services.DataProcess
{
    /// <summary>
    /// 数据归一化服务
    /// </summary>
    public class DataNormalizationService 
    {
        /// <summary>
        /// 归一化处理器
        /// </summary>
        readonly IRawXRayDataNormalizer _view1Normalizer = new RawDataNormalizer();
        readonly IRawXRayDataNormalizer _view2Normalizer = new RawDataNormalizer();

        /// <summary>
        /// 对原始采集的数据进行归一化处理
        /// </summary>
        /// <param name="bundle"></param>
        /// <returns></returns>
        public ScanlineDataBundle Normalize(RawScanlineDataBundle bundle)
        {
            ScanlineData view1 = null;
            ScanlineData view2 = null;

            if (bundle.View1RawData != null)
            {
                view1 = NormalizeViewData(bundle.View1RawData, _view1Normalizer);
            }

            if (bundle.View2RawData != null)
            {
                view2 = NormalizeViewData(bundle.View2RawData, _view2Normalizer);
            }

            return  new ScanlineDataBundle(view1, view2);
        }

        /// <summary>
        /// 使用特定的归一化器，归一化某一个视角的数据
        /// </summary>
        /// <param name="raw"></param>
        /// <param name="normalizer"></param>
        /// <returns></returns>
        private ScanlineData NormalizeViewData(RawScanlineData raw, IRawXRayDataNormalizer normalizer)
        {
            var result = new ScanlineData(DetectViewIndex.View1, raw.XRaySensor, raw.LineLength);
            if (result.Low != null)
            {
                raw.Low.CopyTo(result.Low, 0);
            }
            if (result.High != null)
            {
                raw.High.CopyTo(result.High, 0);
            }

            normalizer.Normalize(result.Low, result.High);
            return result;
        }

        public void ResetAir(ScanlineDataBundle airValue)
        {
            if (airValue != null)
            {
                _view1Normalizer.ResetAir(airValue.View1LineData);
                _view2Normalizer.ResetAir(airValue.View2LineData);
            }
        }

        public void ResetGround(ScanlineDataBundle groundValue)
        {
            if (groundValue != null)
            {
                _view1Normalizer.ResetGround(groundValue.View1LineData);
                _view2Normalizer.ResetGround(groundValue.View2LineData);
            }
        }
    }
}
