using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;

namespace UI.XRay.Flows.Controllers.SummaryEntity
{
    public class StatisticResultBase : ObservableObject
    {

        /// <summary>
        /// 统计日期：如果按季度统计，则1，2，3，4分别表示四个季度；
        /// 如果按月份统计，则1-12分别表示12个月
        /// 如果按日统计，则1-31分别表示31天
        /// 如果按周统计，则1-52分别表示52周
        /// </summary>
        private string _date;

        /// <summary>
        /// 统计日期：如果按季度统计，则1，2，3，4分别表示四个季度；
        /// 如果按月份统计，则1-12分别表示12个月
        /// 如果按日统计，则1-31分别表示31天
        /// 如果按周统计，则1-52分别表示52周
        /// </summary>
        public string Date
        {
            get { return _date; }
            set { _date = value; RaisePropertyChanged(); }
        }
    }
}
