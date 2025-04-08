using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.ImagePlant;

namespace UI.XRay.Flows.Services
{
    /// <summary>
    /// 智能图像处理算法推荐服务
    /// </summary>
    public class ImageProcessAlgoRecommendService
    {
        /// <summary>
        /// 单例，方便多处调用
        /// </summary>
        private static readonly ImageProcessAlgoRecommendService _service = new ImageProcessAlgoRecommendService();
        public static ImageProcessAlgoRecommendService Service()
        {
            return _service;
        }

        //图像处理算法推荐算法
        private readonly ImageProcessRecommendAlgo _imageProcessRecommendAlgo = new ImageProcessRecommendAlgo();

        /// <summary>
        /// 用于异步缓存处理数据，不阻塞线程
        /// </summary>
        private readonly ConcurrentQueue<DisplayScanlineDataBundle> _cacheQueue =
            new ConcurrentQueue<DisplayScanlineDataBundle>();

        /// <summary>
        /// 处理数据的线程，分离算法服务和正常的数据流程
        /// </summary>
        private Task _processBundleTask = null;

        //有数据时通知处理线程
        private readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(true);
        //退出标志
        private bool _eixt = true;

        private bool _isServiceStarted = false;


        [MethodImpl(MethodImplOptions.Synchronized)]
        public void StartService()
        {
            _isServiceStarted = false;
            bool enableImageProcessRecommend = false;
            if (!ScannerConfig.Read(ConfigPath.AlgoEnableImageProcessRecommend,out enableImageProcessRecommend))
            {
                enableImageProcessRecommend = false;
            }

            if (enableImageProcessRecommend)
            {
                if (_processBundleTask == null)
                {
                    Init();

                    _eixt = false;
                    _processBundleTask = Task.Factory.StartNew(ProcessBundleExe);
                    Tracer.TraceInfo("ImageProcessAlgoRecommendService has been started.");
                }
                _isServiceStarted = true;
            }
        }

        private void Init()
        {
            ushort valueUpperLimit;
            if (!ScannerConfig.Read(ConfigPath.AlgoImageValueUpperLimit, out valueUpperLimit))
            {
                valueUpperLimit = 62530;
            }
            ushort valueLowerLimit;
            if (!ScannerConfig.Read(ConfigPath.AlgoImageValueLowerLimit, out valueLowerLimit))
            {
                valueLowerLimit = 4000;
            }
            ushort organicAndMixtureZLimit;
            if (!ScannerConfig.Read(ConfigPath.AlgoOrganicAndMixtureZLimit, out organicAndMixtureZLimit))
            {
                organicAndMixtureZLimit = 10;
            }
            ushort mixtureAndInOrganicZLimit;
            if (!ScannerConfig.Read(ConfigPath.AlgoMixtureAndInOrganicZLimit, out mixtureAndInOrganicZLimit))
            {
                mixtureAndInOrganicZLimit = 18;
            }
            double unshowInOrganicLimit;
            if (!ScannerConfig.Read(ConfigPath.AlgoUnshowInOrganicLimit, out unshowInOrganicLimit))
            {
                unshowInOrganicLimit = 0.3;
            }
            double unshowOrganicLimit;
            if (!ScannerConfig.Read(ConfigPath.AlgoUnshowOrganicLimit, out unshowOrganicLimit))
            {
                unshowOrganicLimit = 0.3;
            }

            _imageProcessRecommendAlgo.Init(valueUpperLimit, valueLowerLimit, organicAndMixtureZLimit,
                mixtureAndInOrganicZLimit, unshowInOrganicLimit, unshowOrganicLimit);
        }

        private void ProcessBundleExe()
        {
            while (!_eixt)
            {
                _autoResetEvent.WaitOne();

                DisplayScanlineDataBundle bundle;
                while (_cacheQueue.TryDequeue(out bundle))
                {
                    _imageProcessRecommendAlgo.Process(bundle);
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void StopService()
        {
            if (_isServiceStarted)
            {
                Tracer.TraceInfo("ImageProcessAlgoRecommendService is stopping.");
                _eixt = true;
                _autoResetEvent.Set();
                if (_processBundleTask != null)
                {
                    _processBundleTask.Wait();
                }
                _processBundleTask = null;
                Tracer.TraceInfo("ImageProcessAlgoRecommendService has been stopped.");
            }
            _isServiceStarted = false;
        }

        //用来缓存分析数据
        public void CacheData(DisplayScanlineDataBundle bundle)
        {
            if (_isServiceStarted)
            {
                _cacheQueue.Enqueue(bundle);
                _autoResetEvent.Set();
            }
        }

        public void ProcessIntelligenceEvent(XRayViewCadRegion suspiciousRegion)
        {
            if (_isServiceStarted)
            {
                _imageProcessRecommendAlgo.ProcessIntelligenceEvent(suspiciousRegion);
            }
        }

        public void HandleObjectSeperated()
        {
            if (_isServiceStarted)
            {
                _imageProcessRecommendAlgo.HandleObjectSeperated();
            }
        }

        public ImageEffectsComposition GetRecommendAlgos()
        {
            if (_isServiceStarted)
            {
                return _imageProcessRecommendAlgo.GetRecommendAlgos();
            }
            return new ImageEffectsComposition(null, null, null, null);
        }
    }
}
