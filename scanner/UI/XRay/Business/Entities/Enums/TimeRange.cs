using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Business.Entities.Enums
{
    /// <summary>
    /// 检索时使用的时间范围枚举
    /// </summary>
    public enum TimeRange
    {
        /// <summary>
        /// 最近一个小时
        /// </summary>
        LastHour,

        /// <summary>
        /// 当天的
        /// </summary>
        Today,

        /// <summary>
        /// 昨天的
        /// </summary>
        Yestoday,

        /// <summary>
        /// 最近一周的
        /// </summary>
        RecentWeek,

        /// <summary>
        /// 最近一个月的
        /// </summary>
        RecentMonth,

        /// <summary>
        /// 其他指定的时间范围
        /// </summary>
        SpecifiedTimeRange
    }
}
