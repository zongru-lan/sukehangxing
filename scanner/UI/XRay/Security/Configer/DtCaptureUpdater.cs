using System;
using UI.Common.Tracers;
using UI.XRay.Business.Algo;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Services;

namespace UI.XRay.Security.Configer
{
    internal static class DtCaptureUpdater
    {
        /// <summary>
        /// 根据注册表中的当前配置，更新DT硬件的配置
        /// </summary>
        /// <returns>true 表示更新成功，false表示更新失败</returns>
        public static bool Update()
        {
            try
            {
                int viewsCount;
                if (!ScannerConfig.Read(ConfigPath.MachineViewsCount, out viewsCount))
                {
                    return false;
                }

                bool isDualEnergy;
                if (!ScannerConfig.Read(ConfigPath.CaptureSysIsDualEnergy, out isDualEnergy))
                {
                    return false;
                }

                float lineIntegrationTime;
                if (!ScannerConfig.Read(ConfigPath.CaptureSysLineIntegrationTime, out lineIntegrationTime))
                {
                    return false;
                }

                DetSysTypeEnum dtDetSysType;
                if (!ScannerConfig.Read(ConfigPath.CaptureSysDTDetSysType, out dtDetSysType))
                {
                    return false;
                }

                using (var dt = new DTControlClr())
                {
                    if (dt.Open(viewsCount, (DTDetSysType)dtDetSysType))
                    {
                        // 先设置积分时间
                        if (!dt.SetIntegrationTime((int)(lineIntegrationTime * 1000)))
                        {
                            return false;
                        }

                        // 设置探测卡分布
                        DTCaptureDesc view1Desc = null;
                        DTCaptureDesc view2Desc = null;

                        int[] view1Cards, view2Cards;

                        // 视角1不允许为空
                        if (ExchangeDirectionConfig.Service.GetView1CardsDist(out view1Cards))
                        {
                            view1Desc = new DTCaptureDesc(view1Cards[0], view1Cards[1], view1Cards[2], view1Cards[3], isDualEnergy, 160);
                        }
                        else
                        {
                            return false;
                        }

                        if (viewsCount == 2 && ExchangeDirectionConfig.Service.GetView2CardsDist(out view2Cards))
                        {
                            view2Desc = new DTCaptureDesc(view2Cards[0], view2Cards[1], view2Cards[2], view2Cards[3], isDualEnergy, 160);
                        }

                        return dt.SetCardsDesc(view1Desc, view2Desc);
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
