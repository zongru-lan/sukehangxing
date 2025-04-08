using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Business.Entities
{
    /// <summary>
    /// 表示一条开机、关机的记录，对应数据库中的同名表。
    /// 不要随意改动此类的属性成员
    /// </summary>
    public class BootLog : PropertyNotifiableObject
    {
        private long _bootLogId;

        private string _machineNumber;

        private DateTime _bootTime;

        private DateTime _shutdownTime;

        public BootLog()
        {
            
        }

        public BootLog(DateTime bootTime, DateTime shutdownTime)
        {
            BootTime = bootTime;
            ShutdownTime = shutdownTime;
        }

        /// <summary>
        /// 开关机记录的Id，作为主键，在插入数据库的过程中，由数据库自动生成
        /// </summary>
        [Key]
        public long BootLogId
        {
            get { return _bootLogId; }
            set { _bootLogId = value; RaisePropertyChanged();}
        }

        /// <summary>
        /// 设备的序列号
        /// </summary>
        public string MachineNumber
        {
            get { return _machineNumber; }
            set { _machineNumber = value; RaisePropertyChanged();}
        }

        /// <summary>
        /// 设备开机时间
        /// </summary>
        public DateTime BootTime
        {
            get { return _bootTime; }
            set { _bootTime = value; RaisePropertyChanged();}
        }

        /// <summary>
        /// 设备关机时间
        /// </summary>
        public DateTime ShutdownTime
        {
            get { return _shutdownTime; }
            set { _shutdownTime = value; RaisePropertyChanged();}
        }

        /// <summary>
        /// 此记录表示的工作时长
        /// </summary>
        public TimeSpan WorkingTimeSpan
        {
            get { return ShutdownTime - BootTime; }
        }
    }
}
