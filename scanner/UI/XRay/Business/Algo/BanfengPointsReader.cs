using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UI.XRay.Business.Entities;

namespace UI.XRay.Business.Algo
{
    public struct BadChannelInterpolationPosition
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
    public class BanfengPointsReader
    {
        string ViewBadChannelFlagsSettingFilePath = ConfigPath.View1BadChannelFlagsSettingFilePath;

        private List<ChannelBadFlag> _viewBadChannelFlags;

        /// <summary>
        /// 视角的所有坏点索引
        /// </summary>
        private readonly List<BadChannel> _viewBadChannelsList = new List<BadChannel>(20);

        /// <summary>
        /// 视角坏点插值表，每次更新汇总坏点链表后总是重新生成坏点插值表，用来插值
        /// </summary>
        private List<BadChannelInterpolationPosition> _viewBadChannelInterpolationPositionList = null;

        public BanfengPointsReader(int view)
        {
            if (view == 0)
            {
                ViewBadChannelFlagsSettingFilePath = ConfigPath.View1BadChannelFlagsSettingFilePath;
            }
            else
            {
                ViewBadChannelFlagsSettingFilePath = ConfigPath.View2BadChannelFlagsSettingFilePath;
            }

            LoadBadChannelFlags();
            var badchannels = GetBadChannels(_viewBadChannelFlags);
            ResetView1ManualSetBadChannels(badchannels);
        }

        public List<int[]> GetViewPositon()
        {
            var rtn = new List<int[]>();
            if (_viewBadChannelInterpolationPositionList == null || _viewBadChannelInterpolationPositionList.Count < 1)
                return rtn;

            for (int i = 0; i < _viewBadChannelInterpolationPositionList.Count; i++)
            {
                var bad = _viewBadChannelInterpolationPositionList[i];
                int[] temp = new int[bad.RightInterpolationChannelIndex - bad.LeftInterpolationChannelIndex - 1];
                for (int j = 0; j < temp.Length; j++)
                {
                    temp[j] = bad.LeftInterpolationChannelIndex + 1 + j;
                }

                bool isContaint = false;
                for (int k = 0; k < rtn.Count; k++)
                {
                    var index = rtn[k];
                    if (index.Length == temp.Length && index[0] == temp[0])
                    {
                        isContaint = true;
                        break;
                    }
                }
                if (!isContaint)
                {
                    rtn.Add(temp);
                }
            }
            return rtn;
        }

        public List<int[]> GetViewPositon(List<BadChannelInterpolationPosition> badChannelPositionList)
        {
            var rtn = new List<int[]>();
            if (badChannelPositionList == null || badChannelPositionList.Count < 1)
                return rtn;

            for (int i = 0; i < badChannelPositionList.Count; i++)
            {
                var bad = badChannelPositionList[i];
                int[] temp = new int[bad.RightInterpolationChannelIndex - bad.LeftInterpolationChannelIndex - 1];
                for (int j = 0; j < temp.Length; j++)
                {
                    temp[j] = bad.LeftInterpolationChannelIndex + 1 + j;
                }

                bool isContaint = false;
                for (int k = 0; k < rtn.Count; k++)
                {
                    var index = rtn[k];
                    if (index.Length == temp.Length && index[0] == temp[0])
                    {
                        isContaint = true;
                        break;
                    }
                }
                if (!isContaint)
                {
                    rtn.Add(temp);
                }
            }
            return rtn;
        }


        private void ResetView1ManualSetBadChannels(IEnumerable<BadChannel> view1BadChannels)
        {
            {
                if (_viewBadChannelsList != null)
                {
                    //添加第一视角的坏点探测通道编号到汇总的链表中
                    AddBadChannelsToList(view1BadChannels, _viewBadChannelsList);

                    //更新视角1坏点插值表
                    _viewBadChannelInterpolationPositionList =
                        CreateBadChannelInterpolationPositionsTable(_viewBadChannelsList);
                }
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
                if (badChannels.FindAll(channel => channel.ChannelIndex == start).Count == 0)
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

        private List<BadChannel> GetBadChannels(List<ChannelBadFlag> flags)
        {
            if (flags != null && flags.Count > 0)
            {
                var result = new List<BadChannel>(flags.Count);
                foreach (var flag in flags)
                {
                    if (flag.IsBad)
                    {
                        result.Add(new BadChannel(flag.ChannelNumber - 1, true));
                    }
                }

                return result;
            }

            return null;
        }

        private void LoadBadChannelFlags()
        {
            try
            {
                if (File.Exists(ViewBadChannelFlagsSettingFilePath))
                {
                    using (var stream = new FileStream(ViewBadChannelFlagsSettingFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 100000))
                    {
                        var formatter = new XmlSerializer(typeof(List<ChannelBadFlag>));
                        _viewBadChannelFlags = formatter.Deserialize(stream) as List<ChannelBadFlag>;
                    }
                }
            }
            catch (Exception exception)
            {

            }
        }
    }
}
