using System;
using System.Collections.Generic;
using System.Linq;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;

namespace UI.XRay.Business.Algo
{
    /// <summary>
    /// 坏点插值计算
    /// </summary>
    public class BadChannelInterpolation 
    {
        /// <summary>
        ///记录坏点探测通道及其左右对应的用来插值的探测通道的编号的结构体
        /// </summary>
        struct BadChannelInterpolationPosition
        {
            //坏点探测通道
            public int BadChannelIndex;
            //坏点探测通道左侧用来插值的探测通道编号
            public int LeftInterpolationChannelIndex;
            //坏点探测通道右侧用来插值的探测通道编号
            public int RightInterpolationChannelIndex;
            //是否手动标注
            public bool IsManualSet;
        }

        /// <summary>
        /// 视角1坏点插值表，每次更新汇总坏点链表后总是重新生成坏点插值表，用来插值
        /// </summary>
        private List<BadChannelInterpolationPosition> _view1BadChannelInterpolationPositionList = null;
        /// <summary>
        /// 视角2坏点插值表，每次更新汇总坏点链表后总是重新生成坏点插值表，用来插值
        /// </summary>
        private List<BadChannelInterpolationPosition> _view2BadChannelInterpolationPositionList = null;

        /// <summary>
        /// 视角1的所有坏点索引
        /// </summary>
        private readonly List<BadChannel> _view1BadChannelsList = new List<BadChannel>(20);

        /// <summary>
        /// 视角2的所有坏点索引
        /// </summary>
        private readonly List<BadChannel> _view2BadChannelsList = new List<BadChannel>(20);

        /// <summary>
        /// 多线程同步锁，用于在多线程环境中保护_view1BadChannels和_view2BadChannels
        /// </summary>
        private readonly object _sync = new object();

        private int _background = 60000;

        private int _unpenetratableUpper = 3000;

        private ushort _interpUpper = 48000;

        private int _high = 0;

        private int _low = 0;

        private Random rand = new Random();

        /// <summary>
        /// 物质分类处理
        /// </summary>
        private MatClassifyRoutine _classifierForBad;

        /// <summary>
        /// 构造函数，读取配置项中的探测通道数目
        /// </summary>
        public BadChannelInterpolation(MatClassifyRoutine _classifier)
        {
            if (!ScannerConfig.Read(ConfigPath.PreProcBkgThreshold, out _background))
            {
                _background = 63500;
            }
            if (!ScannerConfig.Read(ConfigPath.PreProcUnpenetratableUpper,out _unpenetratableUpper))
            {
                _unpenetratableUpper = 3000;
            }
            _classifierForBad = _classifier;
            Tracer.TraceInfo($"[BadChannelInterpolation] background: {_background}, unpenetratableUpper: {_unpenetratableUpper}");
        }

        private void GetViewBeltEdgePosition(DetectViewIndex view, ref BeltEdgePosition viewBeltEdgePosition)
        {
            try
            {
                int postion;
                if (ScannerConfig.Read(view == DetectViewIndex.View1 ? ConfigPath.MachineView1BeltEdge1Start : ConfigPath.MachineView2BeltEdge1Start, out postion))
                {
                    viewBeltEdgePosition.Edge1Start = postion;
                }

                if (ScannerConfig.Read(view == DetectViewIndex.View1 ? ConfigPath.MachineView1BeltEdge1End : ConfigPath.MachineView2BeltEdge1End, out postion))
                {
                    viewBeltEdgePosition.Edge1End = postion;
                }

                if (ScannerConfig.Read(view == DetectViewIndex.View1 ? ConfigPath.MachineView1BeltEdge2Start : ConfigPath.MachineView2BeltEdge2Start, out postion))
                {
                    viewBeltEdgePosition.Edge2Start = postion;
                }

                if (ScannerConfig.Read(view == DetectViewIndex.View1 ? ConfigPath.MachineView1BeltEdge2End : ConfigPath.MachineView2BeltEdge2End, out postion))
                {
                    viewBeltEdgePosition.Edge2End = postion;
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }

        /// <summary>
        /// Add manually set bad channels indices for both view1 and view2.
        /// </summary>
        /// <param name="view1BadChannels"></param>
        /// <param name="view2BadChannels"></param>
        public void ResetManualSetBadChannels(IEnumerable<BadChannel> view1BadChannels,
            IEnumerable<BadChannel> view2BadChannels)
        {
            lock (_sync)
            {
                if (_view1BadChannelsList!=null)
                {
                    //清除汇总链表的视角1所有手动获得的坏点编号
                    _view1BadChannelsList.RemoveAll(badChannel => badChannel.ManualSet);

                    //添加第一视角的坏点探测通道编号到汇总的链表中
                    AddBadChannelsToList(view1BadChannels, _view1BadChannelsList);

                    //更新视角1坏点插值表
                    _view1BadChannelInterpolationPositionList =
                        CreateBadChannelInterpolationPositionsTable(_view1BadChannelsList);
                }

                if (_view2BadChannelsList!=null)
                {
                    _view2BadChannelsList.RemoveAll(badChannel => badChannel.ManualSet);

                    AddBadChannelsToList(view2BadChannels, _view2BadChannelsList);

                    _view2BadChannelInterpolationPositionList =
                        CreateBadChannelInterpolationPositionsTable(_view2BadChannelsList);
                }
            }
        }

        /// <summary>
        /// Add manually set bad channels indices for both view1 and view2.
        /// </summary>
        /// <param name="view1BadChannels"></param>
        /// <param name="view2BadChannels"></param>
        public void ResetView2ManualSetBadChannels(IEnumerable<BadChannel> view2BadChannels)
        {
            lock (_sync)
            {
                if (_view2BadChannelsList != null)
                {
                    _view2BadChannelsList.RemoveAll(badChannel => badChannel.ManualSet);

                    AddBadChannelsToList(view2BadChannels, _view2BadChannelsList);

                    _view2BadChannelInterpolationPositionList =
                        CreateBadChannelInterpolationPositionsTable(_view2BadChannelsList);
                }
            }
        }


        /// <summary>
        /// </summary>
        /// <param name="view1BadChannels"></param>
        public void ResetView1ManualSetBadChannels(IEnumerable<BadChannel> view1BadChannels)
        {
            lock (_sync)
            {
                if (_view1BadChannelsList != null)
                {
                    //清除汇总链表的视角1所有手动获得的坏点编号
                    _view1BadChannelsList.RemoveAll(badChannel => badChannel.ManualSet);

                    //添加第一视角的坏点探测通道编号到汇总的链表中
                    AddBadChannelsToList(view1BadChannels, _view1BadChannelsList);

                    //更新视角1坏点插值表
                    _view1BadChannelInterpolationPositionList =
                        CreateBadChannelInterpolationPositionsTable(_view1BadChannelsList);
                }
            }
        }


        /// <summary>
        /// 将最新自动探测发现的坏点，添加到坏点插值功能中，即添加到保存所有坏点索引的链表中，以便于后续的计算中使用
        /// </summary>
        /// <param name="view1BadChannels">自动探测发现的视角1的坏点链表</param>
        /// <param name="view2BadChannels">自动探测发现的视角2的坏点链表</param>
        public void ResetAutoSetBadChannels(IEnumerable<BadChannel> view1BadChannels, IEnumerable<BadChannel> view2BadChannels)
        {
            lock (_sync)
            {
                if (_view1BadChannelsList != null && view1BadChannels!=null)
                {
                    //清空第一视角自动探测的坏点
                    _view1BadChannelsList.RemoveAll(badChannel => badChannel.ManualSet == false);

                    //添加第一视角的坏点探测通道编号到汇总的链表中
                    AddBadChannelsToList(view1BadChannels, _view1BadChannelsList);

                    //更新视角1坏点插值表
                    _view1BadChannelInterpolationPositionList =
                        CreateBadChannelInterpolationPositionsTable(_view1BadChannelsList);
                }
                
                if (_view2BadChannelsList != null && view2BadChannels != null)
                {
                    //清空第二视角自动探测的坏点
                    _view2BadChannelsList.RemoveAll(badChannel => badChannel.ManualSet == false);

                    //添加第二视角的坏点探测通道编号到汇总的链表中
                     AddBadChannelsToList(view2BadChannels,_view2BadChannelsList);
                     //更新视角2坏点插值表
                     _view2BadChannelInterpolationPositionList =
                         CreateBadChannelInterpolationPositionsTable(_view2BadChannelsList);
                }
            }
        }

        /// <summary>
        /// 将探测到的坏点列表添加到保存所有坏点的列表中
        /// </summary>
        /// <param name="viewBadChannels">要添加的新坏点列表</param>
        /// <param name="summarizedViewBadChannels">一个视角汇总坏点的链表</param>
        private void AddBadChannelsToList(IEnumerable<BadChannel> viewBadChannels, List<BadChannel> summarizedViewBadChannels)
        {
            if (viewBadChannels == null || !viewBadChannels.Any())
            {
                return;
            }

            //将自动检测到的视角1的坏点探测通道编号添加到所有坏点索引链表
            foreach (var viewBadChannel in viewBadChannels)
            {
                //如果在已有的汇总探测通道链表中不存在此探测通道编号，则添加
                if (summarizedViewBadChannels.FindAll(channel => channel.ChannelIndex == viewBadChannel.ChannelIndex).Count == 0)
                {
                    summarizedViewBadChannels.Add(viewBadChannel);
                }
            }
        }

        /// <summary>
        /// 将自动检测的坏点探测通道清空
        /// </summary>
        public void ClearAutoSetBadChannels()
        {
            lock (_sync)
            {
                //清除第一视角自动检测的坏点探测通道，此处修改为如果自动探测坏点存在才进行清空操作
                if (_view1BadChannelsList != null && _view1BadChannelsList.Exists(channel => channel.ManualSet == false))
                {
                    _view1BadChannelsList.RemoveAll(badChannel => badChannel.ManualSet == false);
                    //更新视角1坏点插值表
                    _view1BadChannelInterpolationPositionList =
                        CreateBadChannelInterpolationPositionsTable(_view1BadChannelsList);
                }
                //清除第二视角自动检测的坏点探测通道，此处修改为如果自动探测坏点存在才进行清空操作
                if (_view2BadChannelsList != null && _view2BadChannelsList.Exists(channel => channel.ManualSet == false))
                {
                    _view2BadChannelsList.RemoveAll(badChannel => badChannel.ManualSet == false);
                    //更新视角2坏点插值表
                    _view2BadChannelInterpolationPositionList =
                        CreateBadChannelInterpolationPositionsTable(_view2BadChannelsList);
                }
            }
        }

        /// <summary>
        /// 自动坏点剔除算法中的插值计算
        /// </summary>
        /// <param name="bundle">要处理的一线数据，包含两个视角的数据</param>
        public void InterpolateBadChannels(ScanlineDataBundle bundle)
        {
            lock (_sync)
            {
                //视角1的数据肯定存在，默认处理视角1数据，根据插值表进行插值处理
                InterpolateViewDataBadChannels(bundle.View1LineData, _view1BadChannelInterpolationPositionList,DetectViewIndex.View1);

                //如果视角2的数据不为空，则对视角2的坏点进行插值
                if (bundle.View2LineData!=null)
                {
                    InterpolateViewDataBadChannels(bundle.View2LineData, _view2BadChannelInterpolationPositionList,DetectViewIndex.View2);
                }
            }
        }

        /// <summary>
        /// 对一个视角的线数据进行插值
        /// </summary>
        /// <param name="lineData">某一视角的线数据</param>
        /// <param name="badChannelInterpolationPositions">坏点插值表</param>
        private void InterpolateViewDataBadChannels(ScanlineData lineData, List<BadChannelInterpolationPosition> badChannelInterpolationPositions,DetectViewIndex view)
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
                    if (badChannelIndex > lineData.LineLength - 2 || badChannelIndex < 2)
                    {
                        continue;
                    }
                    // 保证坏点边界（一般是结束边界）不会超出数据范围，如果坏点的结束边界大于实际数据，那么取最后一个点
                    if (endIndex > lineData.LineLength - 2)
                    {
                        endIndex = lineData.LineLength - 2;
                    }

                    ushort matrialOfStart = _classifierForBad.ClassifyPixel(view, lineData.Low[startIndex], lineData.High[startIndex]); 

                    if (CheckMatrial(matrialOfStart))
                    {
                        continue;
                    }

                    if (CheckHighLowUpper(lineData,badChannelIndex,startIndex,endIndex))
                    {
                        continue;
                    }

                    //边缘部分不处理  两边像素差异过大认为是边缘
                    if (CheckEdge(lineData, badChannelIndex, startIndex, endIndex))
                    {
                        //只有全部为穿不透区域，即使差异大也要继续处理
                        if (CheckUnpenetratable(lineData, badChannelIndex, startIndex, endIndex))
                        {
                            continue;
                        }
                    }

                    Interpolate(lineData, badChannelIndex, startIndex, endIndex);

                    //////old
                    ///

                    //if (viewBadChannelInterpolationPosition.IsManualSet)
                    //{
                    //if (lineData.Low[startIndex] > _unpenetratableUpper || lineData.Low[endIndex] > _unpenetratableUpper)
                    //{
                    //    continue;
                    //}
                    //}
                    //if (lineData.High[badChannelIndex] > (lineData.High[startIndex] * 1.05) || lineData.High[badChannelIndex] > (lineData.High[endIndex] * 1.05))
                    //{
                    //    if (lineData.High[badChannelIndex] > 3500 && lineData.High[badChannelIndex] < 10000)
                    //    {
                    //        lineData.High[badChannelIndex] =
                    //        (ushort) ((lineData.High[startIndex]*dis2 + lineData.High[endIndex]*dis1)/(dis1 + dis2));
                    //    }
                    //    else
                    //    {
                    //        if (lineData.High[startIndex] < lineData.High[endIndex])
                    //        {
                    //            int num = rand.Next((int)(lineData.High[startIndex] * 0.05));
                    //            lineData.High[badChannelIndex] = (ushort)(lineData.High[startIndex] * 0.97 + num);
                    //        }
                    //        else
                    //        {
                    //            int num = rand.Next((int)(lineData.High[startIndex] * 0.05));
                    //            lineData.High[badChannelIndex] = (ushort)(lineData.High[endIndex] * 0.97 + num);
                    //        }
                    //    }
                    //}

                    ////对低能数据进行插值
                    //lineData.Low[badChannelIndex] =
                    //    (ushort) ((lineData.Low[startIndex]*dis2 + lineData.Low[endIndex]*dis1)/(dis1 + dis2));
                    ////如果是多能数据，对高能进行处理
                    //if (lineData.XRaySensor == XRaySensorType.Dual)
                    //{
                    //    lineData.High[badChannelIndex] =
                    //        (ushort) ((lineData.High[startIndex]*dis2 + lineData.High[endIndex]*dis1)/(dis1 + dis2));
                    //}
                }
            }
        }

        private bool CheckMatrial(ushort matrial)
        {
            if (matrial > 100 && matrial < 140)
            {
                return true;
            }
            return false;
        }

        private bool CheckHighLowUpper(ScanlineData lineData, int badChannelIndex, int startIndex, int endIndex)
        {
            if (lineData.Low[badChannelIndex] > _interpUpper
                || lineData.Low[startIndex] > _interpUpper
                || lineData.Low[endIndex] > _interpUpper)
            {
                return true;
            }
            return false;
        }

        private bool CheckEdge(ScanlineData lineData, int badChannelIndex, int startIndex, int endIndex)
        {
            //if (lineData.High[startIndex] > (lineData.High[endIndex] * 1.20) || lineData.High[endIndex] > (lineData.High[startIndex] * 1.20)
            //    || lineData.High[badChannelIndex] > (lineData.High[startIndex] * 1.40) || lineData.High[startIndex] > (lineData.High[badChannelIndex] * 1.40)
            //    || lineData.High[badChannelIndex] > (lineData.High[endIndex] * 1.40) || lineData.High[endIndex] > (lineData.High[badChannelIndex] * 1.40)
            //    || lineData.High[badChannelIndex] > (lineData.High[badChannelIndex - 1] * 1.40) || lineData.High[badChannelIndex - 1] > (lineData.High[badChannelIndex] * 1.40)
            //    || lineData.High[badChannelIndex] > (lineData.High[badChannelIndex + 1] * 1.40) || lineData.High[badChannelIndex + 1] > (lineData.High[badChannelIndex] * 1.40))
            if (lineData.High[startIndex] > (lineData.High[endIndex] * 1.20) || lineData.High[endIndex] > (lineData.High[startIndex] * 1.20))
            {
                return true;
            }

            bool isIncreasing = true;
            bool isDecreasing = true;

            for (int i = badChannelIndex - 1; i < badChannelIndex + 2; i++)
            {
                if (lineData.High[i] > lineData.High[i - 1])
                {
                    isDecreasing = false;
                }
                else if (lineData.High[i] < lineData.High[i - 1])
                {
                    isIncreasing = false;
                }
            }

            bool isSpace = false;

            ushort matrialOfBad0 = _classifierForBad.ClassifyPixel(DetectViewIndex.View1, lineData.Low[badChannelIndex - 1], lineData.High[badChannelIndex - 1]);
            ushort matrialOfBad1 = _classifierForBad.ClassifyPixel(DetectViewIndex.View1, lineData.Low[badChannelIndex], lineData.High[badChannelIndex]);
            ushort matrialOfBad2 = _classifierForBad.ClassifyPixel(DetectViewIndex.View1, lineData.Low[badChannelIndex + 1], lineData.High[badChannelIndex + 1]);

            if (Math.Abs(matrialOfBad0 - matrialOfBad1) > 20 || Math.Abs(matrialOfBad0 - matrialOfBad2) > 20 || Math.Abs(matrialOfBad1 - matrialOfBad2) > 20)
            {
                isSpace = true;
            }

            return isIncreasing || isDecreasing || isSpace;
        }

        private bool CheckUnpenetratable(ScanlineData lineData, int badChannelIndex, int startIndex, int endIndex)
        {
            if (lineData.Low[startIndex] > _unpenetratableUpper || lineData.Low[endIndex] > _unpenetratableUpper)
            {
                return true;//非穿不透区域
            }
            //ushort matrialOfStart = _classifierForBad.ClassifyPixel(DetectViewIndex.View1, lineData.Low[startIndex], lineData.High[startIndex]);
            //if(matrialOfStart < 180)
            //{
            //    return true;//非钢阶梯
            //}

            return false;//穿不透区域 和钢阶梯
        }

        private void Interpolate(ScanlineData lineData, int badChannelIndex, int startIndex, int endIndex)
        {
            int dis1 = badChannelIndex - startIndex;
            int dis2 = endIndex - badChannelIndex;

            if (lineData.High[badChannelIndex] > lineData.High[startIndex] && lineData.High[badChannelIndex] < lineData.High[endIndex])
            {
                if (lineData.High[startIndex] < lineData.High[endIndex])
                {
                    int num = rand.Next((int)(lineData.High[endIndex] - lineData.High[startIndex]));
                    lineData.High[badChannelIndex] = (ushort)(lineData.High[startIndex] + num);
                }
                else
                {
                    int num = rand.Next((int)(lineData.High[startIndex] - lineData.High[endIndex]));
                    lineData.High[badChannelIndex] = (ushort)(lineData.High[endIndex] + num);
                }
            }
            else
            {
                lineData.High[badChannelIndex] =
                    (ushort)((lineData.High[startIndex] * dis2 + lineData.High[endIndex] * dis1) / (dis1 + dis2));
            }

            if (lineData.Low[badChannelIndex] > lineData.Low[startIndex] && lineData.Low[badChannelIndex] < lineData.Low[endIndex])
            {
                if (lineData.Low[startIndex] < lineData.Low[endIndex])
                {
                    int num = rand.Next((int)(lineData.Low[endIndex] - lineData.Low[startIndex]));
                    lineData.Low[badChannelIndex] = (ushort)(lineData.Low[startIndex] + num);
                }
                else
                {
                    int num = rand.Next((int)(lineData.Low[startIndex] - lineData.Low[endIndex]));
                    lineData.Low[badChannelIndex] = (ushort)(lineData.Low[endIndex] + num);
                }
            }

            else
            {
                lineData.Low[badChannelIndex] =
                    (ushort)((lineData.Low[startIndex] * dis2 + lineData.Low[endIndex] * dis1) / (dis1 + dis2));
            }
        }

        /// <summary>
        /// 生成坏点插值表
        /// </summary>
        /// <param name="badChannels">某一个视角的坏点汇总集合</param>
        /// <returns>某一个视角的坏点插值表</returns>
        private List<BadChannelInterpolationPosition> CreateBadChannelInterpolationPositionsTable(
            List<BadChannel> badChannels)
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
                    int badChannelIndex = badChannel.ChannelIndex;

                    //查找边界
                    FindBadChannelsEdge(badChannelIndex, out startIndex, out endIndex, badChannels);

                    //生成坏点探测通道及其左右对应的用来插值的探测通道的编号结构
                    var badChannelInterpolationPosition = new BadChannelInterpolationPosition
                    {
                        //坏点探测通道
                        BadChannelIndex = badChannelIndex,
                        //坏点左侧用来插值的位置
                        LeftInterpolationChannelIndex = startIndex,
                        //坏点右侧用来插值的位置
                        RightInterpolationChannelIndex = endIndex,
                        //手动标注
                        IsManualSet = badChannel.ManualSet
                    };
                    //添加到坏点插值表
                    badChannelInterpolationPositions.Add(badChannelInterpolationPosition);
                }
                //返回坏点插值表
                return badChannelInterpolationPositions;
            }
            return null;
        }

        /// <summary>
        /// 查找坏点区域的起始和结束位置，可能存在着连续的坏点区域，对于此类情况要查找坏点区域的边界以进行插值
        /// </summary>
        /// <param name="badChannel">坏点的探测通道编号</param>
        /// <param name="startIndex">坏点区域的起始位置</param>
        /// <param name="endIndex">坏点区域的结束位置</param>
        /// <param name="badChannels">坏点探测通道链表</param>
        private void FindBadChannelsEdge(int badChannel, out int startIndex, out int endIndex, List<BadChannel> badChannels)
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
                if (badChannels.FindAll(channel=>channel.ChannelIndex==start).Count==0)
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
                if (badChannels.FindAll(channel => channel.ChannelIndex == end).Count == 0)
                {
                    break;
                }
            }

            startIndex = start;
            endIndex = end;
        }
    }
}
