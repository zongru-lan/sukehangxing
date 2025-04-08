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

namespace UI.XRay.Flows.Services
{
    /// <summary>
    /// 白线检测服务 -- 单线
    /// </summary>
    public class AirLineCheckingService : AirCheckingService
    {
        /// <summary>
        /// 视角1的白线检测
        /// </summary>
        private AirLineChecker _view1Checker;

        /// <summary>
        /// 视角2的白线检测
        /// </summary>
        private AirLineChecker _view2Checker;

        public AirLineCheckingService(): base()
        {
            _view1Checker = new AirLineChecker(_view1MarginBegin, _view1MarginEnd, background);
            _view2Checker = new AirLineChecker(_view2MarginBegin, _view2MarginEnd, background);
        }

        /// <summary>
        /// 判断一线数据是否为空白
        /// 如果两个视角都有数据，则分别判断两个视角的数据，并取两个视角的与；
        /// 如果只有一个视角有数据，则只判断此视角的数据；
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public override List<ClassifiedLineDataBundle> CheckAirLine(ClassifiedLineDataBundle line)
        {
            if (line != null && (line.View1Data != null || line.View2Data != null))
            {
                bool isView1Air = true;
                bool isView2Air = true;

                if (line.View1Data != null && _view1Checker != null)
                {
                    isView1Air = _view1Checker.TestAirLine(line.View1Data);
                    line.View1Data.IsAir = isView1Air;
                }

                if (line.View2Data != null && _view2Checker != null)
                {
                    isView2Air = _view2Checker.TestAirLine(line.View2Data);
                    line.View2Data.IsAir = isView2Air;
                }

                return new List<ClassifiedLineDataBundle>(1){line};
            }

            return new List<ClassifiedLineDataBundle>(0);
        }

        public override void ClearCacheLines() { }
    }
}
