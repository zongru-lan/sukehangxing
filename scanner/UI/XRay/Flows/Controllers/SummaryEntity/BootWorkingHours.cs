using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Flows.Controllers.SummaryEntity
{
    public class BootWorkingHours : StatisticResultBase
    {
        /// <summary>
        /// 以浮点数表示的小时数
        /// </summary>
        public double Hours { get; set; }

        public string H { get; set; }  //yxc
        /// <summary>
        /// 包裹数量
        /// </summary>
        public int BagCount{ get; set; }
    }
}
