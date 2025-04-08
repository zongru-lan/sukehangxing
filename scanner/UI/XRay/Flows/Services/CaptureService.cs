using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
namespace UI.XRay.Flows.Services
{
    /// <summary>
    /// 图像采集服务。
    /// </summary>
    public static class CaptureService
    {
        /// <summary>
        /// 图像采集服务的单一实例
        /// </summary>
        public static ICaptureServicePart ServicePart { get; private set; }

        static CaptureService ()
        {
            CaptureSysTypeEnum captureSysType;
            if (!ScannerConfig.Read<CaptureSysTypeEnum>(ConfigPath.CaptureSysType, out captureSysType))
            {
                captureSysType = CaptureSysTypeEnum.DtGCUSTD;
            }
            if (captureSysType == CaptureSysTypeEnum.DtGCUSTD)
            {
                ServicePart = new DtGCUSTDCaptureServicePart();
            }
            else if(captureSysType == CaptureSysTypeEnum.TYM)
            {
                ServicePart = new TYMCaptureServicePart();
            }
            else if(captureSysType == CaptureSysTypeEnum.Dt)
            {
                ServicePart = new DtCaptureServicePart();
            }
            else
            {
                ServicePart = new DtGCUSTDCaptureServicePart();
            }
            //            
        }
    }
}
