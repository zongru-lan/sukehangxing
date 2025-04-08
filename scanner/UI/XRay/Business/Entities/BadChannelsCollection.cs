using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace UI.XRay.Business.Entities
{
    /// <summary>
    /// 表示一个探测通道坏点
    /// </summary>
    [Serializable]
    public class BadChannel
    {
        /// <summary>
        /// 无参构造函数，用于xml序列化
        /// </summary>
        public BadChannel()
        {
            
        }
        /// <summary>
        /// 带参构造函数
        /// </summary>
        /// <param name="channelIndex">探测通道</param>
        /// <param name="manualSet">手动还是自动</param>
        public BadChannel(int channelIndex, bool manualSet)
        {
            ChannelIndex = channelIndex;
            ManualSet = manualSet;
        }
        /// <summary>
        /// 通道索引号，从0开始，取值范围为[0, 探测通道总数-1]
        /// </summary>
        public int ChannelIndex { get;  set; }

        /// <summary>
        /// 手动/自动设置的坏点。true表示是人为设定的坏点，false表示是软件自动探测的坏点
        /// </summary>
        public bool ManualSet { get;  set; }
    }

    /// <summary>
    /// 表示所有视角的坏点的集合
    /// </summary>
    [XmlRoot]
    public class BadChannelsCollection
    {
        /// <summary>
        /// 无参构造函数，用于xml序列化
        /// </summary>
        public BadChannelsCollection()
        {
            
        }
        /// <summary>
        /// 有参构造函数
        /// </summary>
        /// <param name="view1BadChannels">视角1的坏点探测通道链表</param>
        /// <param name="view2BadChannels">视角2的坏点探测通道链表</param>
        public BadChannelsCollection(List<BadChannel> view1BadChannels, List<BadChannel> view2BadChannels)
        {
            View1BadChannels = view1BadChannels;
            View2BadChannels = view2BadChannels;
        }
        [XmlArray]
        public List<BadChannel> View1BadChannels { get; set; }

        [XmlArray]
        public List<BadChannel> View2BadChannels { get; set; } 
    }
}
