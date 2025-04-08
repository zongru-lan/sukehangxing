using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Business.Entities
{
    /// <summary>
    /// 输送机工作日志
    /// </summary>
    public class ConveyorWorkLog : DevicePartWorkLog
    {
        private long _conveyorWorkLogId;

        [Key]
        public long ConveyorWorkLogId
        {
            get { return _conveyorWorkLogId; }
            set { _conveyorWorkLogId = value; RaisePropertyChanged(); }
        }

        public ConveyorWorkLog()
        {
            
        }

        public ConveyorWorkLog(DateTime date, int hour, double seconds)
            :base(date, hour, seconds)
        {
            
        }
    }
}
