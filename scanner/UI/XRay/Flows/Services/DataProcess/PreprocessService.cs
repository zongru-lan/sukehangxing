using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.Algo;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using System.Linq;

namespace UI.XRay.Flows.Services.DataProcess
{
    /// <summary>
    /// 数据预处理服务：输入为采集的裸数据，输出为融合后的数据以及物质分类
    /// </summary>
    public class PreprocessService
    {
        /// <summary>
        /// 存储预处理的结果。调用此队列以读取预处理结果
        /// </summary>
        public ConcurrentQueue<ClassifiedLineDataBundle> OutputQueue { get; private set; }

        #region 预处理子程序

        /// <summary>
        /// 归一化：根据输入的本底、满度，将输入数据归一化到0-65535区间
        /// </summary>
        public DataNormalizationService Normalization { get; private set; }

        /// <summary>
        /// 动态更新满度服务
        /// </summary>
        private AutoCalibrationServiceBase _autoCalibrationService = null;

        /// <summary>
        /// 坏点自动探测处理
        /// </summary>
        private BadChannelAutoDetection _badChannelAutoDetection;

        /// <summary>
        /// 坏点插值
        /// </summary>
        private BadChannelInterpolation _badPointsInterpolation;

        /// <summary>
        /// 图像上下边缘置白处理
        /// </summary>
        private BeltEdgeEliminationService _marginProcessor;
        /// <summary>
        /// 由高低能计算融合值
        /// </summary>
        private DualEnergyFuse _dualEnergyFuse;

        /// <summary>
        /// 线数据维纳滤波
        /// </summary>
        private WienerFilter2 _wienerFilter;

        /// <summary>
        /// 物质分类处理
        /// </summary>
        private MatClassifyRoutine _classifier;

        #endregion 预处理子程序

        /// <summary>
        /// Dedicated work thread for preprocessing
        /// </summary>
        private Thread _preProcessingThread;

        /// <summary>
        /// 当前的满度：用于自动探测坏点
        /// </summary>
        private ScanlineDataBundle _currentAir;

        /// <summary>
        /// 当前的本底：用于自动探测坏点
        /// </summary>
        private ScanlineDataBundle _currentGround;

        /// <summary>
        /// 线程退出信号
        /// </summary>
        private bool _exitSignal = false;

        private bool _autoDetectBadChannel = true;

        private bool _wienerEnabled = false;

        private bool _oldBanFengEnabled = false;

        ChannelBadFlagsFileWatchingService _badFlagWatcher = new ChannelBadFlagsFileWatchingService();

        private BanfengAlgo banfengAlgo;
        private BanfengAlgo banfengAlgo2;

        [MethodImpl(MethodImplOptions.Synchronized)]
        public PreprocessService()
        {
            try
            {
                OutputQueue = new ConcurrentQueue<ClassifiedLineDataBundle>();
                Normalization = new DataNormalizationService();
                _marginProcessor = new BeltEdgeEliminationService();
                _dualEnergyFuse = new DualEnergyFuse();
                _wienerFilter = new WienerFilter2();
                _classifier = new MatClassifyRoutine();
                _badPointsInterpolation = new BadChannelInterpolation(_classifier);

                banfengAlgo = new BanfengAlgo(0);
                banfengAlgo2 = new BanfengAlgo(1);

                _badFlagWatcher.View1ChannelBadFlagsUpdated += BadFlagWatcherOnView1SetBadUpdated;
                _badFlagWatcher.View2ChannelBadFlagsUpdated += BadFlagWatcherOnView2SetBadUpdated;

                InitBadChannelAutoDetection();

                // 分类结束后，将结果存入输出队列中
                _classifier.ScanlineClassified += (sender, bundle) => OutputQueue.Enqueue(bundle);

                LoadChannelsBadFlagsAsync();
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception, "Unexpected exception in PreprocessService constructor.");
            }
        }

        /// <summary>
        /// 异步读取坏点探测通道列表
        /// </summary>
        private void LoadChannelsBadFlagsAsync()
        {
            Task.Run(() =>
            {
                try
                {
                    var service = new ChannelBadFlagsManageService();
                    var view1 = service.GetView1BadChannels();
                    var view2 = service.GetView2BadChannels();

                    if (view1 != null)
                    {
                        _badPointsInterpolation.ResetView1ManualSetBadChannels(view1);
                    }

                    if (view2 != null)
                    {
                        _badPointsInterpolation.ResetView2ManualSetBadChannels(view2);
                    }
                }
                catch (Exception e)
                {
                    Tracer.TraceException(e, "Failed to load ChannelBadFlags from file.");
                }
            });
        }

        private void BadFlagWatcherOnView2SetBadUpdated(List<BadChannel> badChannels)
        {
            _badPointsInterpolation.ResetView2ManualSetBadChannels(badChannels);
            if (banfengAlgo2 != null)
            {
                banfengAlgo2.Init(1);
            }
        }

        private void BadFlagWatcherOnView1SetBadUpdated(List<BadChannel> badChannels)
        {
            _badPointsInterpolation.ResetView1ManualSetBadChannels(badChannels);
            if (banfengAlgo != null)
                banfengAlgo.Init(0);
        }

        /// <summary>
        /// 初始化坏点自动探测子程序
        /// </summary>
        private void InitBadChannelAutoDetection()
        {
            int airValueHighSingleLower = 7500;
            int airValueLowSingleLower = 7500;
            int groundValueHighSingleUpper = 5000;
            int groundValueLowSingleUpper = 5000;

            if (!ScannerConfig.Read(ConfigPath.PreProcAirHighSingleLower, out airValueHighSingleLower))
            {
                airValueHighSingleLower = 7500;
            }

            if (!ScannerConfig.Read(ConfigPath.PreProcAirLowSingleLower, out airValueLowSingleLower))
            {
                airValueLowSingleLower = 7500;
            }

            if (!ScannerConfig.Read(ConfigPath.PreProcGroundHighSingleUpper, out groundValueHighSingleUpper))
            {
                groundValueHighSingleUpper = 5000;
            }

            if (!ScannerConfig.Read(ConfigPath.PreProcGroundLowSingleUpper, out groundValueLowSingleUpper))
            {
                groundValueLowSingleUpper = 5000;
            }

            if (!ScannerConfig.Read(ConfigPath.PreProcAutoDetectBadChannels, out _autoDetectBadChannel))
            {
                _autoDetectBadChannel = true;
            }
            if (!ScannerConfig.Read(ConfigPath.PreProcWienerEnabled, out _wienerEnabled))
            {
                _wienerEnabled = false;
            }
            if (!ScannerConfig.Read(ConfigPath.AlgoBanFengOldEnable, out _oldBanFengEnabled))
            {
                _oldBanFengEnabled = false;
            }
            _badChannelAutoDetection = new BadChannelAutoDetection((ushort)airValueHighSingleLower,
                (ushort)airValueLowSingleLower, (ushort)groundValueHighSingleUpper, (ushort)groundValueLowSingleUpper);
            Tracer.TraceInfo($"[PreProc] wiener enabled: {_wienerEnabled}");
        }

        /// <summary>
        /// 更新最新的满度数据
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void ResetAir(ScanlineDataBundle bundle)
        {
            // 分别设置双通道的满度
            Normalization.ResetAir(bundle);
            //Normalization.ResetAirChannelB(bundle.ChannelBXRayLineDataBundle);

            _currentAir = bundle;

            UpdateAutoSetBadChannels();
        }

        /// <summary>
        /// 更新最新的本底数据
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void ResetGround(ScanlineDataBundle bundle)
        {
            // 分别设置双通道的本底
            Normalization.ResetGround(bundle);
            //Normalization.ResetGroundChannelB(bundle.ChannelBXRayLineDataBundle);

            _currentGround = bundle;

            // 本底更新后，重新自动探测坏点
            UpdateAutoSetBadChannels();
        }

        /// <summary>
        /// 根据当前的本底、满度，自动判断坏点
        /// </summary>
        private void UpdateAutoSetBadChannels()
        {
            var view1BadChannels = new List<BadChannel>();
            var view2BadChannels = new List<BadChannel>();

            //自动探测本底中的坏点
            if (_currentGround != null)
            {
                if (_currentGround.View1LineData != null)
                {
                    var v1 = _badChannelAutoDetection.DetectBadChannelsFromGround(_currentGround.View1LineData);
                    if (v1 != null)
                    {
                        view1BadChannels.AddRange(v1);
                    }
                }

                if (_currentGround.View2LineData != null)
                {
                    var v2 = _badChannelAutoDetection.DetectBadChannelsFromGround(_currentGround.View2LineData);
                    if (v2 != null)
                    {
                        view2BadChannels.AddRange(v2);
                    }
                }
            }

            //自动探测满度中的坏点
            if (_currentAir != null)
            {
                if (_currentAir.View1LineData != null)
                {
                    var v1 = _badChannelAutoDetection.DetectBadChannelsFromAir(_currentAir.View1LineData);
                    if (v1 != null)
                    {
                        view1BadChannels.AddRange(v1);
                    }

                }

                if (_currentAir.View2LineData != null)
                {
                    var v2 = _badChannelAutoDetection.DetectBadChannelsFromAir(_currentAir.View2LineData);
                    if (v2 != null)
                    {
                        view2BadChannels.AddRange(v2);
                    }

                }
            }

            if (_autoDetectBadChannel)
            {
                SystemStateService.Service.View1BadChannels = view1BadChannels;
                SystemStateService.Service.View2BadChannels = view2BadChannels;
                // 更新坏点
                _badPointsInterpolation.ResetAutoSetBadChannels(view1BadChannels, view2BadChannels);
            }
        }

        /// <summary>
        /// 预处理
        /// </summary>
        /// <param name="data"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Feed(RawScanlineDataBundle data, bool interruptNormal)
        {
            var normalized = Normalization.Normalize(data);
            //// 坏点插值
            if (_oldBanFengEnabled)
            {
                _badPointsInterpolation.InterpolateBadChannels(normalized);
            }
            // 上下边缘置白
            _marginProcessor.Eliminate(normalized);

            //尝试动态更新满度，在置白和插值之后
            ScanlineDataBundle updatedAir = null;

            if (normalized != null && _autoCalibrationService != null && interruptNormal
                && _autoCalibrationService.TryUpdateCurrentAir(data, normalized, out updatedAir))
            {
                if (updatedAir != null)
                {
                    this.ResetAir(updatedAir);
                }
            }

            //计算融合值
            _dualEnergyFuse.Fuse(normalized);

            // 维纳滤波处理
            if (_wienerEnabled)
            {
                var lines = _wienerFilter.WienerProcess(normalized);
                if(lines.Count > 0)
                {
                    List<ClassifiedLineDataBundle> bundles = new List<ClassifiedLineDataBundle>();
                    foreach (var line in lines)
                    {
                        var bundle = _classifier.GetClassifyBundle(line);
                        bundles.Add(bundle);
                    }
                    ProcessBanfeng(lines, bundles);
                    foreach (var line in lines)
                    {
                        _classifier.ClassifyBundle(line);
                    }
                }
            }
            else
            {
                _classifier.ClassifyBundle(normalized);
            }
        }

        private void SetBanfeng(List<ScanlineDataBundle> bundles, List<ClassifiedLineDataBundle> classBundles)
        {
            if (bundles == null || bundles.Count <= 0)
            {
                return;
            }
            var view1Data = bundles.Select(a => a.View1LineData).ToArray();
            var view1DataClasss = classBundles.Select(a => a.View1Data).ToArray();

            if (banfengAlgo.HasBadPoints())
            {
                banfengAlgo.ProcessViewBanFengImgOldScan(view1Data, view1DataClasss);
            }

            if (bundles.FirstOrDefault().View2LineData != null && banfengAlgo2 != null)
            {
                if (banfengAlgo2.HasBadPoints())
                {
                    var view2Data = bundles.Select(a => a.View2LineData).ToArray();
                    var view2DataClasss = classBundles.Select(a => a.View2Data).ToArray();
                    banfengAlgo2.ProcessViewBanFengImgOldScan(view2Data, view2DataClasss);
                }
            }
        }

        private void ProcessBanfeng(List<ScanlineDataBundle> bundles, List<ClassifiedLineDataBundle> classBundles)
        {
            if (bundles == null || bundles.Count <= 0)
            {
                return;
            }
            var view1Data = bundles.Select(a => a.View1LineData).ToList();
            var view1DataClasss = classBundles.Select(a => a.View1Data).ToList();

            foreach (var bundle in view1Data)
            {
                bundle.BackupFusedData();
            }

            if (banfengAlgo.HasBadPoints())
            {
                banfengAlgo.ProcessViewBanFengHighLow(view1Data, view1DataClasss);
            }

            if (bundles.FirstOrDefault().View2LineData != null && banfengAlgo2 != null)
            {
                var view2Data = bundles.Select(a => a.View2LineData).ToList();
                var view2DataClasss = classBundles.Select(a => a.View2Data).ToList();
                foreach (var bundle in view2Data)
                {
                    bundle.BackupFusedData();
                }
                if (banfengAlgo2.HasBadPoints())
                {
                    banfengAlgo2.ProcessViewBanFengHighLow(view2Data, view2DataClasss);
                }
            }
        }

        /// <summary>
        /// 清除各缓存队列
        /// </summary>
        public void ClearLinesCache()
        {
            ClassifiedLineDataBundle classifyBundle = null;
            while (!OutputQueue.IsEmpty)
            {
                OutputQueue.TryDequeue(out classifyBundle);
            }
        }
        public void ClearWienerCache()
        {
            if (_wienerFilter != null)
            {
                _wienerFilter.ClearCache();
            }
        }

        public void SetDynamicUpdateAirService(AutoCalibrationServiceBase autoCalibrationService)
        {
            _autoCalibrationService = autoCalibrationService;
        }

        /// <summary>
        /// 更新手动设置的坏点列表
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void UpdateManualSetBadChannels()
        {

        }
    }
}
