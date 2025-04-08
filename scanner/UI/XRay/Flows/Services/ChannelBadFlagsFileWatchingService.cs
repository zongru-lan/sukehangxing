using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.Entities;

namespace UI.XRay.Flows.Services
{

    /// <summary>
    /// 手动标定坏道列表文件监控服务：当文件被增加、删除、修改时，更新坏道列表并输出
    /// </summary>
    public class ChannelBadFlagsFileWatchingService
    {
        /// <summary>
        /// 视角1的人工标定坏点更新事件
        /// </summary>
        public event Action<List<BadChannel>> View1ChannelBadFlagsUpdated;

        /// <summary>
        /// 视角2的人工标定坏点列表更新事件
        /// </summary>
        public event Action<List<BadChannel>> View2ChannelBadFlagsUpdated;

        private FileSystemWatcher _badChannelFileWatcher;

        public ChannelBadFlagsFileWatchingService()
        {
            try
            {
                var path = Path.GetDirectoryName(ConfigPath.View1BadChannelFlagsSettingFilePath);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    Directory.CreateDirectory(path);

                    _badChannelFileWatcher = new FileSystemWatcher(path);
                    _badChannelFileWatcher.Deleted += BadChannelFileWatcherOnDeleted;
                    _badChannelFileWatcher.Changed += BadChannelFileWatcherOnChanged;
                    _badChannelFileWatcher.EnableRaisingEvents = true;
                    _badChannelFileWatcher.Filter = "*.xml";
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);      
            }
        }

        private void ReloadBadChannelFlags()
        {
            var loader = new ChannelBadFlagsManageService();

            var view1Flags = loader.GetChannelBadFlags(1);
            var view2Flags = loader.GetChannelBadFlags(2);

            var view1Bad = GetBadChannels(view1Flags);
            var view2Bad = GetBadChannels(view2Flags);

            if (View1ChannelBadFlagsUpdated != null)
            {
                View1ChannelBadFlagsUpdated(view1Bad);
            }

            if (View2ChannelBadFlagsUpdated != null)
            {
                View2ChannelBadFlagsUpdated(view2Bad);
            }
        }

        /// <summary>
        /// 获取坏道列表
        /// </summary>
        /// <param name="flags"></param>
        /// <returns></returns>
        private List<BadChannel> GetBadChannels(List<ChannelBadFlag> flags)
        {
            List<BadChannel> result = null;

            if (flags != null && flags.Count > 0)
            {
                result = new List<BadChannel>(flags.Count);
                foreach (var flag in flags)
                {
                    if (flag.IsBad)
                    {
                        result.Add(new BadChannel(flag.ChannelNumber-1, true));
                    }
                }
            }

            return result;
        }

        private void BadChannelFileWatcherOnChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            System.Threading.Thread.Sleep(1000);
            ReloadBadChannelFlags();
        }

        private void BadChannelFileWatcherOnDeleted(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            System.Threading.Thread.Sleep(1000);
            ReloadBadChannelFlags();
        }
    }
}
