using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.Entities;
using UI.XRay.ImagePlant;

namespace UI.XRay.Flows.Services
{
    /// <summary>
    /// 单个视角的图像分析信息，主要时各个物质种类的比例以及是否有报警信息
    /// </summary>
    class ImageAnalyzedInfo
    {
        public int ValidOrganicPixelCount { get; set; }
        public int ValidInOrganicPixelCount { get; set; }
        public int ValidMixturePixelCount { get; set; }
        public int ValidPixelCount { get; set; }         //有效的像素数目，去除亮点和暗点之后

        public List<XRayViewCadRegion> SuspiciousRegions = new List<XRayViewCadRegion>();

        public double ValidOrganicPixelRatio()
        {
            return (double)ValidOrganicPixelCount / ValidPixelCount;
        }

        public double ValidInOrganicPixelRatio()
        {
            return (double)ValidInOrganicPixelCount / ValidPixelCount;
        }

        public double ValidMixturePixelRatio()
        {
            return (double)ValidMixturePixelCount / ValidPixelCount;
        }

        public int SuspiciousRegionCount(MarkerRegionType type)
        {
            return SuspiciousRegions.Count(region => region.Region.RegionType == type);
        }

        public void Clean()
        {
            ValidPixelCount = 0;
            ValidInOrganicPixelCount = 0;
            ValidOrganicPixelCount = 0;
            SuspiciousRegions.Clear();
        }
    }

    public struct ImageProcessRecommendParameters
    {
        /// <summary>
        /// 上下限之间的数据才是valid数据，可以计算
        /// </summary>
        public ushort ValueUpperLimit;
        public ushort ValueLowerLimit;

        /// <summary>
        /// 有机物、无机物和混合物的分界线
        /// </summary>
        public ushort OrganicAndMixtureZLimit;
        public ushort MixtureAndInOrganicZLimit;
    }


    /// <summary>
    /// 图像处理算法推荐算法
    /// </summary>
    public class ImageProcessRecommendAlgo
    {
        /// <summary>
        /// 无机物阈值小于此值，执行无机剔除
        /// </summary>
        private double _unshowInOrganicLimit = 0.3;
        /// <summary>
        /// 有机物阈值小于此值，执行有机剔除
        /// </summary>
        private double _unshowOrganicLimit = 0.3;

        //记录当前的信息
        private ImageAnalyzedInfo _view1ImageAnalyzedInfo = new ImageAnalyzedInfo();
        private ImageAnalyzedInfo _view2ImageAnalyzedInfo = new ImageAnalyzedInfo();

        //记录上一幅图像的信息
        private ImageAnalyzedInfo _lastView1AnalyzedInfo = null;
        private ImageAnalyzedInfo _lastView2AnalyzedInfo = null;

        /// <summary>
        /// 上下限之间的数据才是valid数据，可以计算
        /// </summary>
        private ushort _valueUpperLimit = 62530;
        private ushort _valueLowerLimit = 4000;

        /// <summary>
        /// 有机物、无机物和混合物的分界线
        /// </summary>
        private ushort _organicAndMixtureZLimit = 10;
        private ushort _mixtureAndInOrganicZLimit = 18;

        public void Init(ushort valueUpperLimit, ushort valueLowerLimit,
            ushort organicAndMixtureZLimit, ushort mixtureAndInOrganicZLimit,
            double unshowInOrganicLimit, double unshowOrganicLimit)
        {
            _valueUpperLimit = valueUpperLimit;
            _valueLowerLimit = valueLowerLimit;
            _organicAndMixtureZLimit = organicAndMixtureZLimit;
            _mixtureAndInOrganicZLimit = mixtureAndInOrganicZLimit;
            _unshowInOrganicLimit = unshowInOrganicLimit;
            _unshowOrganicLimit = unshowOrganicLimit;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Process(DisplayScanlineDataBundle bundle)
        {
            if (!bundle.IsAir)
            {
                if (_view1ImageAnalyzedInfo == null)
                {
                    _view1ImageAnalyzedInfo = new ImageAnalyzedInfo();
                }
                AnalyzeData(bundle.View1Data,_view1ImageAnalyzedInfo);
                if (bundle.View2Data != null)
                {
                    if (_view2ImageAnalyzedInfo == null)
                    {
                        _view2ImageAnalyzedInfo = new ImageAnalyzedInfo();
                    }
                    AnalyzeData(bundle.View2Data, _view2ImageAnalyzedInfo);
                }
            }
        }

        private void AnalyzeData(ClassifiedLineData viewData, ImageAnalyzedInfo viewInfo)
        {
            if (!viewData.IsAir)
            {
                ushort[] datas = viewData.XRayData; ;
                ushort[] materials = viewData.Material; ;
                ushort value = 0;
                ushort z = 0;

                //处理视角数据
                for (int i = 0; i < datas.Length; i++)
                {
                    value = datas[i];
                    z = materials[i];
                    //数据灰度值必须在有效的范围内才开始计算，太亮或者太暗没有太多意义
                    if (value > _valueLowerLimit && value < _valueUpperLimit)
                    {
                        viewInfo.ValidPixelCount++; //记录有效数据个数
                        //统计物质分类
                        if (z <= _organicAndMixtureZLimit)
                        {
                            viewInfo.ValidOrganicPixelCount++;
                        }
                        else if (z >= _mixtureAndInOrganicZLimit)
                        {
                            viewInfo.ValidInOrganicPixelCount++;
                        }
                        else
                        {
                            viewInfo.ValidMixturePixelCount++;
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void ProcessIntelligenceEvent(XRayViewCadRegion suspiciousRegion)
        {
            var viewImageAnalyzedInfo = suspiciousRegion.ViewIndex == DetectViewIndex.View1
                ? _view1ImageAnalyzedInfo
                : _view2ImageAnalyzedInfo;
            viewImageAnalyzedInfo.SuspiciousRegions.Add(suspiciousRegion);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void HandleObjectSeperated()
        {
            _lastView1AnalyzedInfo = _view1ImageAnalyzedInfo;
            _lastView2AnalyzedInfo = _view2ImageAnalyzedInfo;
            _view1ImageAnalyzedInfo = null;
            _view2ImageAnalyzedInfo = null;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public ImageEffectsComposition GetRecommendAlgos()
        {
            var view1AnalyzedInfo = _lastView1AnalyzedInfo ?? _view1ImageAnalyzedInfo;
            var view2AnalyzedInfo = _lastView2AnalyzedInfo ?? _view2ImageAnalyzedInfo;

            ImageAnalyzedInfo imageInfo = view1AnalyzedInfo;

            int unPeneratedCadRegionCount = 0;

            if (view1AnalyzedInfo != null)
            {
                unPeneratedCadRegionCount = view1AnalyzedInfo.SuspiciousRegionCount(MarkerRegionType.UnPenetratable);
                if (view2AnalyzedInfo != null)
                {
                    if (view2AnalyzedInfo.ValidPixelCount > view1AnalyzedInfo.ValidPixelCount)
                    {
                        imageInfo = view2AnalyzedInfo;
                        unPeneratedCadRegionCount +=
                            view2AnalyzedInfo.SuspiciousRegionCount(MarkerRegionType.UnPenetratable);
                    }
                }
            }

            //有穿不透区域，则使用超增
            if (unPeneratedCadRegionCount > 0)
            {
                return new ImageEffectsComposition(DisplayColorMode.MaterialColor, PenetrationMode.SuperPenetrate, false, false);
            }
            //无机物比例较低，使用无机剔除
            if (imageInfo.ValidInOrganicPixelRatio() < _unshowInOrganicLimit)
            {
                return new ImageEffectsComposition(DisplayColorMode.MS, PenetrationMode.Standard, false,false);
            }
            //有机物比例较低，使用有机剔除
            if (imageInfo.ValidOrganicPixelRatio() < _unshowOrganicLimit)
            {
                return new ImageEffectsComposition(DisplayColorMode.OS, PenetrationMode.Standard, false,false);
            }
            //默认超增
            return new ImageEffectsComposition(DisplayColorMode.MaterialColor, PenetrationMode.Standard, false, true);
        }
    }
}
