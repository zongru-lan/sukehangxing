using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Flows.Services
{
    /// <summary>
    /// 控制系统服务
    /// </summary>
    public static class ControlService
    {
        public static IControlServicePart ServicePart { get; private set; }

        static ControlService()
        {
            ServicePart = new NetControlServicePart();
        }
    }
}
