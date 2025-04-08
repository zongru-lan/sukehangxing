using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using System.Linq;
using UI.XRay.Business.Algo;

namespace UI.XRay.Flows.Services
{
    /// <summary>
    /// 手动标定管理服务
    /// </summary>
    public class ChannelBadFlagsManageService
    {
        /// <summary>
        /// 视角1的人工标定探测通道坏道标记列表
        /// </summary>
        private List<ChannelBadFlag> _view1BadChannelFlags;

        private List<ChannelBadFlag> _view2BadChannelFlags;

        /// <summary>
        /// 视角1的探测通道数
        /// </summary>
        private int _view1ChannelsCount;

        private int _view2ChannelsCount;

        /// <summary>
        /// 探测视角个数
        /// </summary>
        private int _viewsCount;

        public ChannelBadFlagsManageService()
        {
            if (!ScannerConfig.Read(ConfigPath.MachineViewsCount, out _viewsCount))
            {
                _viewsCount = 1;
            }

            LoadBadChannelFlags();

            ExchangeDirectionConfig.Service.GetView1ChannelsCount(out _view1ChannelsCount);
            InitBadChannelFlags(ref _view1BadChannelFlags, _view1ChannelsCount);

            if (_viewsCount > 1)
            {
                ExchangeDirectionConfig.Service.GetView2ChannelsCount(out _view2ChannelsCount);
                InitBadChannelFlags(ref _view2BadChannelFlags, _view2ChannelsCount);
            }            
        }

        /// <summary>
        /// 读取视角1和视角2的坏道标记列表
        /// </summary>
        private void LoadBadChannelFlags()
        {
            try
            {
                if (File.Exists(ConfigPath.View1BadChannelFlagsSettingFilePath))
                {
                    using (var stream = new FileStream(ConfigPath.View1BadChannelFlagsSettingFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 100000))
                    {
                        var formatter = new XmlSerializer(typeof(List<ChannelBadFlag>));
                        _view1BadChannelFlags = formatter.Deserialize(stream) as List<ChannelBadFlag>;
                    }
                    SystemStateService.Service.View1ManualBadChannels = _view1BadChannelFlags.Where(p=>p.IsBad).ToList();
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            if (_viewsCount > 1)
            {
                try
                {
                    if (File.Exists(ConfigPath.View2BadChannelFlagsSettingFilePath))
                    {
                        using (var stream = new FileStream(ConfigPath.View2BadChannelFlagsSettingFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 100000))
                        {
                            var formatter = new XmlSerializer(typeof(List<ChannelBadFlag>));
                            _view2BadChannelFlags = formatter.Deserialize(stream) as List<ChannelBadFlag>;
                        }

                        SystemStateService.Service.View2ManualBadChannels = _view2BadChannelFlags.Where(p=>p.IsBad).ToList();
                    }
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception);
                } 
            }

        }

        /// <summary>
        /// 初始化某个视角的坏道标记。如果列表为空，则初始化为无坏道，如果列表与探测通道数不符，则重置为无坏道
        /// </summary>
        /// <param name="list"></param>
        /// <param name="channelsCount"></param>
        private void InitBadChannelFlags(ref List<ChannelBadFlag> list, int channelsCount)
        {
            if (list != null)
            {
                // 如果读出的坏道标记通道数与实际的通道数不符合，则清空
                if (list.Count != channelsCount)
                {
                    list = null;
                }
            }

            if (list == null)
            {
                list = new List<ChannelBadFlag>(channelsCount);
                for (int i = 1; i <= channelsCount; i++)
                {
                    list.Add(new ChannelBadFlag(i, false));
                }
            }
        }

        /// <summary>
        /// 获取指定视角的探测通道坏道列表
        /// </summary>
        /// <param name="viewNum"></param>
        /// <returns></returns>
        public List<ChannelBadFlag> GetChannelBadFlags(int viewNum)
        {
            if (viewNum == 1)
            {
                return _view1BadChannelFlags;
            }
            else
            {
                return _view2BadChannelFlags;
            }
        }

        /// <summary>
        /// 获取指定视角的探测通道坏道列表
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 获取指定视角的探测通道坏道列表
        /// </summary>
        /// <returns></returns>
        public List<BadChannel> GetView1BadChannels()
        {
            return GetBadChannels(_view1BadChannelFlags);
        }

        /// <summary>
        /// 获取指定视角的探测通道坏道列表
        /// </summary>
        /// <returns></returns>
        public List<BadChannel> GetView2BadChannels()
        {
            return GetBadChannels(_view2BadChannelFlags);
        }

        /// <summary>
        /// 异步存储最新的手动标记坏道信息
        /// </summary>
        public void UpdateBadChannels()
        {
            Task.Run(() =>
            {
                try
                {
                    var dir = Path.GetDirectoryName(ConfigPath.View1BadChannelFlagsSettingFilePath);
                    if (!string.IsNullOrWhiteSpace(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    try
                    {
                        if (_view1BadChannelFlags != null)
                        {
                            using (var stream = new FileStream(ConfigPath.View1BadChannelFlagsSettingFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, 100000))
                            {
                                var formatter = new XmlSerializer(typeof(List<ChannelBadFlag>));
                                formatter.Serialize(stream, _view1BadChannelFlags);
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        Tracer.TraceException(exception);
                    }

                    try
                    {
                        if (_view2BadChannelFlags != null)
                        {
                            using (var stream = new FileStream(ConfigPath.View2BadChannelFlagsSettingFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, 100000))
                            {
                                var formatter = new XmlSerializer(typeof(List<ChannelBadFlag>));
                                formatter.Serialize(stream, _view2BadChannelFlags);
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        Tracer.TraceException(exception);
                    }
                }
                catch (Exception exception )
                {
                    Tracer.TraceException(exception);
                }
            });
            
        }
    }
}
