using System;
using System.Collections.Generic;
using UI.XRay.Business.Algo;
using UI.XRay.Business.Entities;
using UI.XRay.ImagePlant.Classify;

namespace UI.XRay.Flows.Services
{
    public static class DisplayXRayMatLineDataConverter
    {
        static DisplayXRayMatLineDataConverter()
        {
            dp = new DataProcessInAirport2();
        }

        static DataProcessInAirport2 dp;

        /// <summary>
        /// 将一个XRayImage图像中的数据，提取出来并转换为一个DisplayXRayMatLineDataBundle链表
        /// 链表中的编号顺序：从小往大，其中最大编号由参数maxNumber指定。
        /// </summary>
        /// <param name="image">图像对象</param>
        /// <param name="maxNumber">输出链表中扫描线的最大编号</param>
        /// <returns></returns>
        public static LinkedList<DisplayScanlineDataBundle> ToDisplayXRayMatLineDataBundles(this XRayScanlinesImage image, int maxNumber)
        {
            if (image == null)
            {
                throw new ArgumentNullException("image");
            }

            var resultList = new LinkedList<DisplayScanlineDataBundle>();

            var list = new List<DisplayScanlineDataBundle>();

            int numberOff = maxNumber - image.View1Data.ScanLinesCount + 1;
            for (int i = 0; i < image.View1Data.ScanLinesCount; i++)
            {
                var view1ResultLine = image.View1Data.ScanLines[i].ToDisplayXRayMatLineData(numberOff + i);
                var view2ResultLine = image.View2Data == null ? null : image.View2Data.ScanLines[i].ToDisplayXRayMatLineData(numberOff + i);

                list.Add(new DisplayScanlineDataBundle(view1ResultLine, view2ResultLine));
            }

            CalcEnhancedData(list);
            foreach (var line in list)
            {
                resultList.AddLast(line);
            }
            return resultList;
        }

        public static void CalcEnhancedData(List<DisplayScanlineDataBundle> bundles)
        {
            dp.CalcEnhancedData(bundles);
        }

        /// <summary>
        /// 将XRayMatLineData转换为DisplayXRayMatLineData，同时计算颜色索引，并对线进行编号
        /// </summary>
        /// <param name="lineData">要进行转换的一线物质分类结果数据</param>
        /// <param name="lineNumber">返回结果数据的线编号</param>
        /// <returns>转换成功后的一线数据，包含颜色索引及线编号</returns>
        public static DisplayScanlineData ToDisplayXRayMatLineData(this ClassifiedLineData lineData, int lineNumber)
        {
            ushort[] colorIndex = null;
            MatColorMappingService.Service.Map(lineData.Material, out colorIndex);
            return new DisplayScanlineData(lineData, colorIndex, lineNumber);
        }
    }
}
