using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.Algo;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;

namespace UI.XRay.Flows.Services.DataProcess
{
    public class RemoveBadChannelsOperator
    {
        struct BadChannelInterpolationPosition
        {
            //坏点探测通道
            public int BadChannelIndex;
            //坏点探测通道左侧用来插值的探测通道编号
            public int LeftInterpolationChannelIndex;
            //坏点探测通道右侧用来插值的探测通道编号
            public int RightInterpolationChannelIndex;
        }
        public RemoveBadChannelsOperator()
        {
            LoadSetting();
            banfengAlgo = new BanfengAlgo(0);
            he = new HistogramEquation();
        }
        private BanfengAlgo banfengAlgo;
        private int _background = 60000;
        private int _unpenetratableUpper = 3000;
        private ushort _interpUpper = 48000;
        HistogramEquation he;

        public void RemoveBadChannelForUI(ImageViewData viewData, List<ChannelBadFlag> viewChennels)
        {
            //var allPoints = viewChennels.Where(C => C.IsBad).ToList();
            //var _badChannelPositionList = CreateBadChannelInterpolationPositionsTable(allPoints);
            banfengAlgo.GetViewBadList(viewChennels);
            if (viewData != null&& banfengAlgo.HasBadPoints())
            {
                banfengAlgo.ProcessViewBanFengImgOldClass(viewData.ScanLines);
                 
                //InterpolateViewDataBadChannels(viewData, _badChannelPositionList);
            }
            he.Histogram(viewData);
        }

        public void HistogramForUI(ImageViewData viewData)
        {
            he.Histogram(viewData);
        }

        private List<BadChannelInterpolationPosition> CreateBadChannelInterpolationPositionsTable(
            List<ChannelBadFlag> badChannels)
        {
            //处理视角数据
            if (badChannels.Count != 0)
            {
                //生成临时链表
                var badChannelInterpolationPositions = new List<BadChannelInterpolationPosition>();
                foreach (var badChannel in badChannels)
                {
                    //用来保存坏点区域的起始位置和结束位置
                    int startIndex;
                    int endIndex;

                    // 坏点所在的探测通道
                    int badChannelIndex = badChannel.ChannelNumber;

                    //查找边界
                    FindBadChannelsEdge(badChannelIndex, out startIndex, out endIndex, badChannels);

                    //生成坏点探测通道及其左右对应的用来插值的探测通道的编号结构
                    var badChannelInterpolationPosition = new BadChannelInterpolationPosition
                    {
                        //坏点探测通道
                        BadChannelIndex = Math.Max(badChannelIndex - 1, 0),
                        //坏点左侧用来插值的位置
                        LeftInterpolationChannelIndex = Math.Max(startIndex - 1, 0),
                        //坏点右侧用来插值的位置
                        RightInterpolationChannelIndex = Math.Max(endIndex - 1, 0)
                    };
                    //添加到坏点插值表
                    badChannelInterpolationPositions.Add(badChannelInterpolationPosition);
                }
                //返回坏点插值表
                return badChannelInterpolationPositions;
            }
            return null;
        }

        private void InterpolateViewDataBadChannels(ImageViewData lineData, List<BadChannelInterpolationPosition> badChannelInterpolationPositions)
        {
            for (int i = 0; i < lineData.ScanLines.Length; i++)
            {
                //处理视角数据，如果插值表不为null且数据不为空
                if (badChannelInterpolationPositions != null && badChannelInterpolationPositions.Count != 0)
                {
                    foreach (var viewBadChannelInterpolationPosition in badChannelInterpolationPositions)
                    {
                        //用来保存坏点区域的起始位置和结束位置
                        int startIndex = viewBadChannelInterpolationPosition.LeftInterpolationChannelIndex;
                        int endIndex = viewBadChannelInterpolationPosition.RightInterpolationChannelIndex;
                        // 坏点所在的探测通道
                        int badChannelIndex = viewBadChannelInterpolationPosition.BadChannelIndex;
                        // 保证坏点不会超出数据边界，如果超出，则忽略
                        if (badChannelIndex > lineData.ChannelsCount - 1)
                        {
                            continue;
                        }
                        // 保证坏点边界（一般是结束边界）不会超出数据范围，如果坏点的结束边界大于实际数据，那么取最后一个点
                        if (endIndex > lineData.ChannelsCount - 1)
                        {
                            endIndex = lineData.ChannelsCount - 1;
                        }

                        int dis1 = badChannelIndex - startIndex;
                        int dis2 = endIndex - badChannelIndex;

                        if (lineData.ScanLines[i].XRayData[badChannelIndex] > _interpUpper || lineData.ScanLines[i].XRayData[startIndex] > _interpUpper || lineData.ScanLines[i].XRayData[endIndex] > _interpUpper)
                        {
                            continue;
                        }

                        //边缘部分不处理  两边像素差异过大认为是边缘
                        if (lineData.ScanLines[i].XRayData[startIndex] > (lineData.ScanLines[i].XRayData[endIndex] * 1.20) || lineData.ScanLines[i].XRayData[endIndex] > (lineData.ScanLines[i].XRayData[startIndex] * 1.20)
                            || lineData.ScanLines[i].XRayData[badChannelIndex] > (lineData.ScanLines[i].XRayData[startIndex] * 1.40) || lineData.ScanLines[i].XRayData[startIndex] > (lineData.ScanLines[i].XRayData[badChannelIndex] * 1.40)
                            )
                        {
                            //只有全部为穿不透区域，即使差异大也要继续处理
                            if (lineData.ScanLines[i].XRayData[startIndex] > _unpenetratableUpper || lineData.ScanLines[i].XRayData[endIndex] > _unpenetratableUpper)
                            {
                                continue;
                            }
                        }

                        if (lineData.ScanLines[i].XRayData[badChannelIndex] > lineData.ScanLines[i].XRayData[startIndex] && lineData.ScanLines[i].XRayData[badChannelIndex] < lineData.ScanLines[i].XRayData[endIndex])
                        {
                        }
                        else
                        {
                            lineData.ScanLines[i].XRayData[badChannelIndex] =
                                (ushort)((lineData.ScanLines[i].XRayData[startIndex] * dis2 + lineData.ScanLines[i].XRayData[endIndex] * dis1) / (dis1 + dis2));
                        }

                        if (lineData.ScanLines[i].XRayDataEnhanced != null)
                        {
                            if (lineData.ScanLines[i].XRayDataEnhanced[badChannelIndex] > lineData.ScanLines[i].XRayDataEnhanced[startIndex] && lineData.ScanLines[i].XRayDataEnhanced[badChannelIndex] < lineData.ScanLines[i].XRayDataEnhanced[endIndex])
                            {
                            }
                            else
                            {
                                lineData.ScanLines[i].XRayDataEnhanced[badChannelIndex] =
                                    (ushort)((lineData.ScanLines[i].XRayDataEnhanced[startIndex] * dis2 + lineData.ScanLines[i].XRayDataEnhanced[endIndex] * dis1) / (dis1 + dis2));
                            }
                        }

                        //if (lineData.ScanLines[i].LowData != null)
                        //{
                        //    if (lineData.ScanLines[i].LowData[startIndex] > _unpenetratableUpper || lineData.ScanLines[i].LowData[endIndex] > _unpenetratableUpper)
                        //    {
                        //        continue;
                        //    }
                        //}
                        //else
                        //{
                        //    if (lineData.ScanLines[i].XRayData[startIndex] > _unpenetratableUpper || lineData.ScanLines[i].XRayData[endIndex] > _unpenetratableUpper)
                        //    {
                        //        continue;
                        //    }
                        //}


                        ////对低能数据进行插值
                        //lineData.ScanLines[i].XRayData[badChannelIndex] =
                        //    (ushort)((lineData.ScanLines[i].XRayData[startIndex] * dis2 + lineData.ScanLines[i].XRayData[endIndex] * dis1) / (dis1 + dis2));
                        //lineData.ScanLines[i].Material[badChannelIndex] =
                        //    (ushort)((lineData.ScanLines[i].Material[startIndex] * dis2 + lineData.ScanLines[i].Material[endIndex] * dis1) / (dis1 + dis2));
                        ////如果是多能数据，对高能进行处理
                        //if (lineData.ScanLines[i].XRayDataEnhanced != null)
                        //{
                        //    lineData.ScanLines[i].XRayDataEnhanced[badChannelIndex] =
                        //    (ushort)((lineData.ScanLines[i].XRayDataEnhanced[startIndex] * dis2 + lineData.ScanLines[i].XRayDataEnhanced[endIndex] * dis1) / (dis1 + dis2));
                        //}
                    }
                }
            }

        }

        private void FindBadChannelsEdge(int badChannel, out int startIndex, out int endIndex, List<ChannelBadFlag> badChannels)
        {
            //起始点和结束点初始化
            var start = badChannel;
            var end = badChannel;

            //找到起始点
            while (start != 0)
            {
                //起始点自减
                start--;
                // 如果此点不包含在链表中，表明此点处探测通道不是坏点
                if (badChannels.FindAll(channel => channel.ChannelNumber == start).Count == 0)
                {
                    break;
                }
            }
            //找到结束点，考虑到配置的通道数可能和实际不匹配，因此这里不再用通道数做判断，在插值时再对通道数判断
            // 如果最后一个通道是坏点，那么这里会把该坏点的结束边界设为最后一个坏点加1，超过了通道的实际范围，在进行插值时，
            // 判断坏点结束边界，如果结束边界超过范围，则最后一个点作为结束边界
            while (true)
            {
                //起始点自减
                end++;
                // 如果此点不包含在链表中，表明此点处探测通道不是坏点
                if (badChannels.FindAll(channel => channel.ChannelNumber == end).Count == 0)
                {
                    break;
                }
            }

            startIndex = start;
            endIndex = end;
        }
        private void LoadSetting()
        {
            if (!ScannerConfig.Read(ConfigPath.MachineBeltEdgeAirThreshold, out _background))
            {
                _background = 62000;
            }

            if (!ScannerConfig.Read(ConfigPath.PreProcUnpenetratableUpper, out _unpenetratableUpper))
            {
                _unpenetratableUpper = 3000;
            }
        }

    }

}
