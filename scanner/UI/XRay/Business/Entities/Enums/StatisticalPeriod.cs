using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Business.Entities.Enums
{
    /// <summary>
    /// 数据统计周期
    /// </summary>
    public enum StatisticalPeriod
    {
        /// <summary>
        /// 按小时（1-24）
        /// </summary>
        Hourly,

        /// <summary>
        /// 按天（指定某个月内的所有天）
        /// </summary>
        Dayly,

        /// <summary>
        /// 按周
        /// </summary>
        Weekly,

        /// <summary>
        /// 按月
        /// </summary>
        Monthly,

        /// <summary>
        /// 按季度
        /// </summary>
        Quarterly,

        /// <summary>
        /// 用户自定义查询范围
        /// </summary>
        UserDefined,
    }
}
