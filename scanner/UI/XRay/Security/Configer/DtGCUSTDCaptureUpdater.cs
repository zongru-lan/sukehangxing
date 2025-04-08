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

namespace UI.XRay.Security.Configer
{
    public static class DtGCUSTDCaptureUpdater
    {
        public static bool Update()
        {
            try
            {
                int viewsCount;
                if (!ScannerConfig.Read(ConfigPath.CaptureSysBoardCount, out viewsCount))
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

                int _channelsCount = 5;
                if (!ScannerConfig.Read(ConfigPath.CaptureSysDTChannels, out _channelsCount))
                {
                    _channelsCount = 5;
                }

                bool _isdualView = viewsCount == 2 ? true : false;
                using (var dt = new DTGCUControlClr(viewsCount,_isdualView))
                {
                    if (dt.Open())
                    {
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
   
                        int _energyMode = view1Desc.DualEnergy == true ? 1 : 0;

                        if (viewsCount == 2)
                        {
                            if (ExchangeDirectionConfig.Service.GetView2CardsDist(out view2Cards))
                            {
                                view2Desc = new DTCaptureDesc(view2Cards[0], view2Cards[1], view2Cards[2], view2Cards[3], isDualEnergy, 160);
                            }
                            
                        }
                        bool isFiveChannels = _channelsCount == 5 ? true : false;
                        dt.SetCardPara(view1Desc, view2Desc, (int)(lineIntegrationTime * 1000), true, isFiveChannels);
                        return true;
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
