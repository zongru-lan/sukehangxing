using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.Algo;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Services;
using UI.XRay.PB;
using XRayAcquisitionSys.DT;
using XRayAcquisitionSys.TYM;

namespace UI.XRay.Security.Configer
{
    public static class TYMCaptureUpdater
    {
        /// <summary>
        /// 根据注册表中的当前配置，更新PB硬件的配置
        /// </summary>
        /// <returns>true 表示更新成功，false表示更新失败</returns>
        public static bool Update()
        {
            try
            {
                int daqCount;
                if (!ScannerConfig.Read(ConfigPath.CaptureSysBoardCount, out daqCount))
                {
                    return false;
                }

                int viewsCount;
                if (!ScannerConfig.Read(ConfigPath.MachineViewsCount, out viewsCount))
                {
                    return false;
                }

                float lineIntegrationTime;
                if (!ScannerConfig.Read(ConfigPath.CaptureSysLineIntegrationTime, out lineIntegrationTime))
                {
                    return false;
                }

                using (var tymControl = new TYMDAQControl(daqCount, viewsCount))
                {
                    if (tymControl.Open())
                    {
                        int[] view1Cards, view2Cards;
                        bool ret = false;

                        // 视角1不允许为空
                        if (ExchangeDirectionConfig.Service.GetView1CardsDist(out view1Cards))
                        {
                            ret = tymControl.SetDAQPara(1, (int)(lineIntegrationTime * 1000), view1Cards);
                        }
                        else
                        {
                            return ret;
                        }


                        if (daqCount == 2)
                        {
                            if (ExchangeDirectionConfig.Service.GetView2CardsDist(out view2Cards))
                            {
                                ret = tymControl.SetDAQPara(2, (int)(lineIntegrationTime * 1000), view2Cards);
                            }
                        }
                        return ret;
                    }
                }

            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
            return false;
        }
    }
}
