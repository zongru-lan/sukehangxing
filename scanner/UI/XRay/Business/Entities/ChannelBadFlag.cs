using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Business.Entities
{
    /// <summary>
    /// 坏道标记
    /// </summary>
    [Serializable]
    public class ChannelBadFlag : PropertyNotifiableObject
    {
        private int _channelNumber;

        private bool _isBad;

        /// <summary>
        /// 通道号，从1开始
        /// </summary>
        public int ChannelNumber
        {
            get { return _channelNumber; }
            set { _channelNumber = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 是否损坏的标志
        /// </summary>
        public bool IsBad
        {
            get { return _isBad; }
            set { _isBad = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channelIndex">从1开始，表示探测通道编号</param>
        /// <param name="isBad"></param>
        public ChannelBadFlag(int channelIndex, bool isBad)
        {
            ChannelNumber = channelIndex;
            IsBad = isBad;
        }

        public ChannelBadFlag()
        {

        }
    }

}
