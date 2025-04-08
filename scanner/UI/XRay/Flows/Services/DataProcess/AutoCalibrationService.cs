using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using UI.Common.Tracers;
using UI.XRay.Business.Algo;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;

namespace UI.XRay.Flows.Services.DataProcess
{
    /// <summary>
    /// 本底满度自动更新服务
    /// </summary>
    public class AutoCalibrationService : AutoCalibrationServiceBase
    {
        public AutoCalibrationService()
        {
            base.InDynamicUpdateMode = true;
            View1AirRoutine = new AirDataAutoUpdateRoutine(DetectViewIndex.View1);
            View2AirRoutine = new AirDataAutoUpdateRoutine(DetectViewIndex.View2);
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
            return base.TryCalibrateAirBeforeScanning(rawData, out airResult);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override bool TryUpdateCurrentAir(RawScanlineDataBundle rawData, ScanlineDataBundle normaizedData, out ScanlineDataBundle airResult)
        {
            airResult = null;
            return false;
        }
        public override void Clearup()
        {

        }
    }
}