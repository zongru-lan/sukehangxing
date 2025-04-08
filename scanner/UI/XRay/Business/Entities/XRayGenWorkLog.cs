using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Business.Entities
{
    /// <summary>
    /// 射线源工作日志
    /// </summary>
    public class XRayGenWorkLog : DevicePartWorkLog
    {
        private long _xRayGenWorkLogId;

        [Key]
        public long XRayGenWorkLogId
        {
            get { return _xRayGenWorkLogId; }
            set { _xRayGenWorkLogId = value; RaisePropertyChanged(); }
        }

        public XRayGenWorkLog()
        {}

        public XRayGenWorkLog(DateTime date, int hour, double seconds)
            :base(date, hour, seconds)
        {
        }
    }
}
