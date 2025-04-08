using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UI.Common.Tracers;
using UI.XRay.Business.Algo;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;

namespace UI.XRay.Flows.Services.DataProcess
{
    public class RemoveInstableChannelsOperator : IDisposable
    {
        #region constructors and initial
        public RemoveInstableChannelsOperator()
        {
            try
            {
                LoadSetting();

                _view1Channels = FindRegionEdge(_view1InstableChannels);
                _view2Channels = FindRegionEdge(_view2InstableChannels);

                StartFileWatcher();
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }

        private void StartFileWatcher()
        {
            var path = Path.GetDirectoryName(ConfigPath.View1InstableChennelsSettingFilePath);
            _channelFileWatcher = new FileSystemWatcher(path);
            _channelFileWatcher.Deleted += FileWatcherOnDeleted;
            _channelFileWatcher.Changed += FileWatcherOnChanged;
            _channelFileWatcher.EnableRaisingEvents = true;
            _channelFileWatcher.Filter = "*.xml";
        }

        private void ReloadChannelFlags()
        {
            LoadSetting();

            _view1Channels = FindRegionEdge(_view1InstableChannels);
            _view2Channels = FindRegionEdge(_view2InstableChannels);
        }

        private void LoadSetting()
        {
            if (File.Exists(ConfigPath.View1InstableChennelsSettingFilePath))
            {
                using (var stream = new FileStream(ConfigPath.View1InstableChennelsSettingFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 100000))
                {
                    var formatter = new XmlSerializer(typeof(List<ChannelBadFlag>));
                    _view1InstableChannels = formatter.Deserialize(stream) as List<ChannelBadFlag>;
                }
            }
            else
            {
                int height = ExchangeDirectionConfig.Service.GetView1ImageHeight();
                _view1InstableChannels = new List<ChannelBadFlag>();
                for (int i = 0; i < height; i++)
                {
                    _view1InstableChannels.Add(new ChannelBadFlag(i, false));
                }
            }

            if (File.Exists(ConfigPath.View2InstableChennelsSettingFilePath))
            {
                using (var stream = new FileStream(ConfigPath.View2InstableChennelsSettingFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 100000))
                {
                    var formatter = new XmlSerializer(typeof(List<ChannelBadFlag>));
                    _view2InstableChannels = formatter.Deserialize(stream) as List<ChannelBadFlag>;
                }
            }
            else
            {
                int height = ExchangeDirectionConfig.Service.GetView2ImageHeight();
                _view2InstableChannels = new List<ChannelBadFlag>();
                for (int i = 0; i < height; i++)
                {
                    _view2InstableChannels.Add(new ChannelBadFlag(i, false));
                }
            }

            if (!ScannerConfig.Read(ConfigPath.MachineBeltEdgeAirThreshold, out _airValue))
            {
                _airValue = 62000;
            }

            if (!ScannerConfig.Read(ConfigPath.MachineBeltEdgeRoundPixelCount,out _roundPixelCount))
            {
                _roundPixelCount = 5;
            }
        }
        #endregion


        #region public
        public void RemoveInstableChennelOper(DisplayScanlineDataBundle line)
        {
            for (int i = 0; i < _view1Channels.Count; i++)
            {
                InstableChannel ic = _view1Channels[i];
                Oper(ic, line.View1Data, line.View1Data.XRayData.Length);
            }
            if (line.View2Data != null)
            {
                for (int i = 0; i < _view2Channels.Count; i++)
                {
                    InstableChannel ic = _view2Channels[i];
                    Oper(ic, line.View2Data, line.View2Data.XRayData.Length);
                }
            }
        }
        /// <summary>
        /// 坏点剔除窗口
        /// </summary>
        /// <param name="viewData"></param>
        /// <param name="viewChennels"></param>
        public void RemoveInstableChennelForUI(ImageViewData viewData, List<ChannelBadFlag> viewChennels)
        {
            var instableChennels = FindRegionEdge(viewChennels);
            RemoveInstableChennelOper(viewData, instableChennels);
        }
        #endregion

        #region Fields
        private FileSystemWatcher _channelFileWatcher;

        private List<ChannelBadFlag> _view1InstableChannels = new List<ChannelBadFlag>();
        private List<ChannelBadFlag> _view2InstableChannels = new List<ChannelBadFlag>();

        public List<InstableChannel> _view1Channels { get; set; }
        public List<InstableChannel> _view2Channels { get; set; }

        ushort _airValue = 62000;

        int _roundPixelCount = 5;

        private bool _disposed = false;

        #endregion

        #region private
        private List<InstableChannel> FindRegionEdge(List<ChannelBadFlag> badlist)
        {
            List<InstableChannel> hpList = new List<InstableChannel>();
            List<ChannelBadFlag> tempList = new List<ChannelBadFlag>();
            badlist.ForEach(i => tempList.Add(i));

            tempList.Add(new ChannelBadFlag(tempList.Count, false));
            tempList.Insert(0, new ChannelBadFlag(-1, false));

            InstableChannel hp = null;
            for (int i = 0; i < tempList.Count - 1; i++)
            {
                if (!tempList[i].IsBad && tempList[i + 1].IsBad)
                {
                    hp = new InstableChannel()
                    {
                        AirValue = _airValue,
                        EdgeStart = tempList[i + 1].ChannelNumber,
                        PixelsUsed = _roundPixelCount,
                        EdgeEnd = tempList[i + 1].ChannelNumber
                    };
                }
                else if (tempList[i].IsBad && !tempList[i + 1].IsBad)
                {
                    hp.EdgeEnd = tempList[i].ChannelNumber;
                    hpList.Add(hp);
                }
            }
            return hpList;
        }

        private void Oper(InstableChannel bp, DisplayScanlineData data, int length)
        {
            if (bp.EdgeStart > length || bp.EdgeEnd > length)
            {
                return;
            }

            //var maxArray = new ushort[bp.EdgeEnd - bp.EdgeStart + 1];
            //GetMax(data.XRayData, maxArray, bp.EdgeStart, bp.EdgeEnd, bp.PixelsUsed);

            //var blackCount = maxArray.Where(x => x < bp.AirValue).ToList().Count;
            //var halfSize = (bp.PixelsUsed - 1) / 2;
            //if (blackCount < 1)
            //{
                for (int i = bp.EdgeStart; i < bp.EdgeEnd; i++)
                {
                    if (data.XRayData[i] < (bp.AirValue + 3000)&& data.XRayData[bp.EdgeStart-1] > bp.AirValue && data.XRayData[bp.EdgeEnd + 1] > bp.AirValue)
                    {
                        data.XRayData[i] = bp.AirValue;
                        if (data.XRayDataEnhanced != null)
                        {
                            data.XRayDataEnhanced[i] = bp.AirValue;
                        }
                    }
                }
            //}
        }

        private void RemoveInstableChennelOper(ImageViewData viewData, List<InstableChannel> viewHorizonalChennel)
        {
            var dataLength = viewData.ScanLines[0].XRayData.Length;

            for (int i = 0; i < viewHorizonalChennel.Count; i++)
            {
                InstableChannel ic = viewHorizonalChennel[i];
                Oper(ic, viewData.ScanLines, dataLength);
            }
        }
        private void Oper(InstableChannel bp, ClassifiedLineData[] dataArray, int length)
        {
            if (bp.EdgeStart > length || bp.EdgeEnd > length)
            {
                return;
            }
            var halfSize = (bp.PixelsUsed - 1) / 2;
            for (int i = 0; i < dataArray.Length; i++)
            {
                var data = dataArray[i];
                //var maxArray = new ushort[bp.EdgeEnd - bp.EdgeStart + 1];
                //GetMax(data.XRayData, maxArray, bp.EdgeStart, bp.EdgeEnd, bp.PixelsUsed);

                //var blackCount = maxArray.Where(x => x < bp.AirValue).ToList().Count;

                //if (blackCount < 1)
                //{
                    for (int j = bp.EdgeStart; j < bp.EdgeEnd; j++)
                    {
                        if (data.XRayData[j] < (bp.AirValue+3000) && data.XRayData[bp.EdgeStart - 1] > bp.AirValue && data.XRayData[bp.EdgeEnd + 1] > bp.AirValue)
                        {
                            data.XRayData[j] = 65530;
                            if (data.XRayDataEnhanced != null)
                            {
                                data.XRayDataEnhanced[j] = 65530;
                            }
                        }
                    }
                //}
            }
        }

        private void GetMax(ushort[] originalArray, ushort[] destinationArray,int start,int end, int filterSize)
        {
            int cols = originalArray.Length;
            int filterRadius = (filterSize - 1) / 2;
            ushort max = 0;
            try
            {
                for (int col = start; col <= end; col++)
                {
                    max = 0;
                    for (int j = col - filterRadius; j <= col + filterRadius; j++)
                    {
                        if ((j < 0) || (j >= cols))
                        {
                            continue;
                        }

                        max = Math.Max(max, originalArray[j]);
                    }

                    destinationArray[col - start] = max;
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }

        private void FileWatcherOnChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            System.Threading.Thread.Sleep(1000);
            ReloadChannelFlags();
        }

        private void FileWatcherOnDeleted(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            System.Threading.Thread.Sleep(1000);
            ReloadChannelFlags();
        }
        #endregion

        #region dispose
        public void Close()
        {
            Dispose();
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~RemoveInstableChannelsOperator()
        {
            Dispose(false);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            if (disposing)
            {
                if (_channelFileWatcher != null)
                {
                    _channelFileWatcher.Deleted -= FileWatcherOnDeleted;
                    _channelFileWatcher.Changed -= FileWatcherOnChanged;
                    _channelFileWatcher.Dispose();
                    _channelFileWatcher = null;
                }
            }
            _disposed = true;
        }
        #endregion
    }

    [Serializable]
    public class InstableChannel
    {
        public InstableChannel()
        {

        }
        public ushort AirValue { get; set; }
        public int EdgeStart { get; set; }
        public int EdgeEnd { get; set; }
        public int PixelsUsed { get; set; }
    }
}
