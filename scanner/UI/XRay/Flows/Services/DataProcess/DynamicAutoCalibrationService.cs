using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.Algo;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;

namespace UI.XRay.Flows.Services.DataProcess
{
    public class DynamicAutoCalibrationService : AutoCalibrationServiceBase
    {
        public DynamicAutoCalibrationService()
        {
            base.InDynamicUpdateMode = true;

            LoadSettingsAndInit();
        }

        private void LoadSettingsAndInit()
        {
            int view1MarginBegin;
            int view1MarginEnd;
            int view2MarginBegin;
            int view2MarginEnd;
            ushort airPixelTh;

            bool onlyAllAirLineUpdate = false;
            int cacheLineCount = 24;
            int avgFilterWinSize = 5;

            int viewCount;
            try
            {
                if (!ScannerConfig.Read(ConfigPath.MachineView1BeltEdgeAtBegin, out view1MarginBegin))
                {
                    view1MarginBegin = 5;
                }

                if (!ScannerConfig.Read(ConfigPath.MachineView1BeltEdgeAtEnd, out view1MarginEnd))
                {
                    view1MarginEnd = 5;
                }

                if (!ScannerConfig.Read(ConfigPath.MachineView2BeltEdgeAtBegin, out view2MarginBegin))
                {
                    view2MarginBegin = 5;
                }

                if (!ScannerConfig.Read(ConfigPath.MachineView2BeltEdgeAtEnd, out view2MarginEnd))
                {
                    view2MarginEnd = 5;
                }

                if (!ScannerConfig.Read(ConfigPath.PreProcAirPixelThreshold, out airPixelTh))
                {
                    airPixelTh = 63500;
                }

                if (!ScannerConfig.Read(ConfigPath.MachineViewsCount, out viewCount))
                {
                    viewCount = 1;
                }

                if (!ScannerConfig.Read(ConfigPath.PreProcOnlyAllAirLine, out onlyAllAirLineUpdate))
                {
                    onlyAllAirLineUpdate = false;
                }

                if (!ScannerConfig.Read(ConfigPath.PreProcDynamicUpdateLineCount,out cacheLineCount))
                {
                    cacheLineCount = 24;
                }
                if (!ScannerConfig.Read(ConfigPath.PreProcAvgFilterWindowSize, out avgFilterWinSize))
                {
                    avgFilterWinSize = 5;
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
                view1MarginBegin = 5;
                view1MarginEnd = 5;
                view2MarginBegin = 5;
                view2MarginEnd = 5;
                airPixelTh = 63500;
                viewCount = 1;
            }



            View1AirRoutine = new DynamicAirDataAutoUpdateRoutine(DetectViewIndex.View1, airPixelTh, view1MarginBegin,
                view1MarginEnd,  avgFilterWinSize, onlyAllAirLineUpdate, cacheLineCount);

            if (viewCount == 2)
            {

                View2AirRoutine = new DynamicAirDataAutoUpdateRoutine(DetectViewIndex.View2, airPixelTh,
                    view2MarginBegin,
                    view2MarginEnd,  avgFilterWinSize, onlyAllAirLineUpdate, cacheLineCount);
            }
        }

        /// <summary>
        /// 尝试更新视角1或视角2的满度
        /// </summary>
        /// <param name="rawData"></param>
        /// <param name="airResult">如果返回值为true，则存储至少视角1或视角2中之一的新满度</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public override bool TryCalibrateAir(RawScanlineDataBundle rawData, out ScanlineDataBundle airResult)
        {
            if (!base.HasUpdateReferenceAir)
            {
                if (base.TryCalibrateAirBeforeScanning(rawData, out airResult))
                {
                    //base.SetReferenceAir(airResult);
                    return true;
                }
            }
            airResult = null;
            return false;
        }


        [MethodImpl(MethodImplOptions.Synchronized)]
        public override bool TryUpdateCurrentAir(RawScanlineDataBundle rawDataBundle,
            ScanlineDataBundle normalizedDataBundle,
            out ScanlineDataBundle airResult)
        {
            if (IsXRayOn)
            {
                ScanlineData view1Air = null;
                ScanlineData view2Air = null;

                if (View1AirRoutine != null)
                {
                    view1Air = TryUpdateViewAir(rawDataBundle.View1RawData, normalizedDataBundle.View1LineData,
                        DetectViewIndex.View1, View1AirRoutine, View1AirVerification);
                }

                if (View2AirRoutine != null)
                {
                    view2Air = TryUpdateViewAir(rawDataBundle.View2RawData,
                        normalizedDataBundle.View2LineData,
                        DetectViewIndex.View2, View2AirRoutine, View2AirVerification);
                }

                // 如果有一个视角的已经更新，则输出
                if (view1Air != null && view2Air != null)
                {
                    airResult = new ScanlineDataBundle(view1Air, view2Air);
                    return true;
                }
            }
            airResult = null;
            return false;
        }

        private ScanlineData TryUpdateViewAir(RawScanlineData rawData, ScanlineData normalizedData,
            DetectViewIndex viewIndex, AirDataAutoUpdateRoutineBase airRoutine, GroundAirVerification verification)
        {
            ScanlineData viewAir = null;

            if (rawData != null)
            {
                if (airRoutine.TryUpdate(rawData, out viewAir, normalizedData))
                {
                    if (!IsViewNewAirValid(viewIndex, verification, viewAir))
                    {
                        viewAir = null;
                    }
                }
            }
            return viewAir;
        }

        public override void Clearup()
        {
            if (View1AirRoutine!=null)
            {
                View1AirRoutine.ClearAirData();
            }
            if (View2AirRoutine!=null)
            {
                View2AirRoutine.ClearAirData();
            }
        }
    }
}
