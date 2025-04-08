using System.Collections.Generic;
using UI.Common.Tracers;
using UI.XRay.Business.Entities;

namespace UI.XRay.Business.Algo
{
    /// <summary>
    /// 坏点自动探测：在每次更新完满度或本底后，根据最新的本底、满度进行坏点判断
    /// </summary>
    public class BadChannelAutoDetection
    {
        /// <summary>
        /// 单个高能探测输出满度的下限，在更新时，如果某探测器的满度低于此值，将其认定为坏点
        /// </summary>
        private readonly ushort _airValueHighSingleLower;

        /// <summary>
        /// 单个低能探测输出满度的下限，在更新时，如果某探测器的满度低于此值，将其认定为坏点
        /// </summary>
        private readonly ushort _airValueLowSingleLower;

        /// <summary>
        /// 单个高能探测输出本底的上限，在更新时，如果某探测器的本底高于此值，将其认定为坏点
        /// </summary>
        private readonly ushort _groundValueHighSingleUpper;

        /// <summary>
        /// 单个低能探测输出本底的上限，在更新时，如果某探测器的本底高于此值，将其认定为坏点
        /// </summary>
        private readonly ushort _groundValueLowSingleUpper;

        /// <summary>
        /// 读取配置项中的本底满度界限，用于自动探测坏点
        /// </summary>
        public BadChannelAutoDetection(ushort airHighSingleLower, ushort airLowSingleLower, ushort groundHighSingleUpper, ushort groundLowSingleUpper)
        {
            _airValueHighSingleLower = airHighSingleLower;
            _airValueLowSingleLower = airLowSingleLower;
            _groundValueHighSingleUpper = groundHighSingleUpper;
            _groundValueLowSingleUpper = groundLowSingleUpper;
        }

        /// <summary>
        /// 根据本底值的规律，寻找坏点
        /// </summary>
        /// <param name="groundValue">最新的本底值</param>
        /// <returns>坏点探测通道位置的索引列表</returns>
        public List<BadChannel> DetectBadChannelsFromGround(ScanlineData groundValue)
        {
            if (groundValue == null)
            {
                Tracer.TraceError("The input parameter--groundValue is null when detect bad channels from ground!");
                return null;
            }

            //保存坏点探测通道编号的链表
            var detectedBadChannelsFromGroud=new List<BadChannel>();
            //根据本底值寻找坏点
            for (int i = 0; i < groundValue.LineLength; i++)
            {
                //单个高能探测输出本底的上限，在更新时，如果某探测器的本底高于此值，将其认定为坏点
                //单个低能探测输出本底的上限，在更新时，如果某探测器的本底高于此值，将其认定为坏点
                if (groundValue.Low[i]>_groundValueLowSingleUpper||(groundValue.High!=null && groundValue.High[i]>_groundValueHighSingleUpper))
                {
                    var badChannel=new BadChannel(i,false);

                    detectedBadChannelsFromGroud.Add(badChannel);
                }
            }

            return detectedBadChannelsFromGroud;
        }

        /// <summary>
        /// 根据满度值的规律，寻找坏点
        /// </summary>
        /// <param name="airValue">最新的满度值</param>
        /// <returns>坏点探测通道位置的索引列表</returns>
        public List<BadChannel> DetectBadChannelsFromAir(ScanlineData airValue)
        {
            if (airValue == null)
            {
                Tracer.TraceError("The input parameter--airValue is null when detect bad channels from air!");
                return null;
            }

            // 保存坏点探测通道编号的链表
            var detectedBadChannelsFromAir = new List<BadChannel>();
            //根据本底值寻找坏点
            for (int i = 0; i < airValue.LineLength; i++)
            {
                //单个高能探测输出满度的下限，在更新时，如果某探测器的满度低于此值，将其认定为坏点
                //单个低能探测输出满度的下限，在更新时，如果某探测器的满度低于此值，将其认定为坏点
                if (airValue.Low[i] < _airValueLowSingleLower || (airValue.High != null && (airValue.High[i] < _airValueHighSingleLower)))
                {
                    var badChannel = new BadChannel(i, false);

                    detectedBadChannelsFromAir.Add(badChannel);
                }
            }

            return detectedBadChannelsFromAir;
        }
    }
}
