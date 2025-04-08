using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Business.Entities
{
    /// <summary>
    /// 设备部件工作日志基类：表示指定日期（某一天）中指定一个小时（1-24）的工作的时长（以秒统计）
    /// </summary>
    public class DevicePartWorkLog : PropertyNotifiableObject
    {
        private DateTime _date;

        private int _hour;

        private double _seconds;

        /// <summary>
        /// 统计日期（仅适用日期部分，不使用小时、分、秒等）
        /// </summary>
        public DateTime Date
        {
            get { return _date; }
            set { _date = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 统计日期中的小时，取值范围为0-23
        /// </summary>
        public int Hour
        {
            get { return _hour; }
            set { _hour = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 当前统计日期的小时内，射线工作的总秒数
        /// </summary>
        public double Seconds
        {
            get { return _seconds; }
            set { _seconds = value; RaisePropertyChanged(); }
        }

        public DevicePartWorkLog()
        {
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="date">日期，不包括时间</param>
        /// <param name="hour">指定的小时，0-23</param>
        /// <param name="seconds">具体时长，秒</param>
        public DevicePartWorkLog(DateTime date, int hour, double seconds)
        {
            Date = date;
            Hour = hour;
            Seconds = seconds;
        }
    }
}
