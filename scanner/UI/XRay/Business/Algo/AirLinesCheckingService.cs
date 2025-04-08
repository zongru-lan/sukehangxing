using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.Entities;
using UI.XRay.Business.DataAccess.Config;
using UI.Common.Tracers;

namespace UI.XRay.Business.Algo
{
    /// <summary>
    /// 白线检测服务 -- 多线
    /// </summary>
    public class AirLinesCheckingService : AirCheckingService
    {
        /// <summary>
        /// 视角1的白线检测
        /// </summary>
        private AirLinesChecker _view1Checker;

        /// <summary>
        /// 视角2的白线检测
        /// </summary>
        private AirLinesChecker _view2Checker;

        public AirLinesCheckingService()
            : base()
        {
            _view1Checker = new AirLinesChecker(DetectViewIndex.View1, _view1MarginBegin, _view1MarginEnd);
            _view2Checker = new AirLinesChecker(DetectViewIndex.View2, _view2MarginBegin, _view2MarginEnd);
            LoadSetting();
        }

        int _linesCacheCount = 24;
        List<ClassifiedLineDataBundle> BundleList = new List<ClassifiedLineDataBundle>();

        object _lock = new object();

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override List<ClassifiedLineDataBundle> CheckAirLine(ClassifiedLineDataBundle line)
        {
            lock (_lock)
            {
                if (BundleList.Count >= _linesCacheCount)
                {
                    BundleList.Clear();
                }
                BundleList.Add(line);
                if (BundleList.Count >= _linesCacheCount)
                {
                    _view1Checker.TestAirLine(BundleList);
                    if (line.View2Data != null)
                    {
                        _view2Checker.TestAirLine(BundleList);
                    }
                    return BundleList;
                }
                else
                {
                    return new List<ClassifiedLineDataBundle>(0);
                }
            }            
        }

        private void LoadSetting()
        {
            try
            {
                ScannerConfig.Read( ConfigPath.PreProcFilterLinesCount, out _linesCacheCount);
                Tracer.TraceInfo($"[AirLinesCheckingService] linesCacheCount: {_linesCacheCount}");
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }
        public override void ClearCacheLines() 
        {
            lock (_lock)
            {
                BundleList.Clear();
            }
        }
    }
}
