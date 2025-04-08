using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Services.Db;

namespace UI.XRay.Flows.Controllers
{
    public class WorkHoursController
    {
        private static readonly WorkHoursController Singleton = new WorkHoursController();

        public static WorkHoursController Instance
        {
            get { return Singleton; }
        }

        private WorkHoursController()
        {
        }

        private DateTime _bootTime = DateTime.Now;

        /// <summary>
        /// 开机时间，用于当前
        /// </summary>
        public DateTime BootTime
        {
            get { return _bootTime; } 
            set { _bootTime = value; }
        }

        /// <summary>
        /// 当前设备的工作时间
        /// </summary>
        public double WorkHoursOfMachineSinceBoot
        {
            get { return Math.Round((DateTime.Now - BootTime).TotalHours, 4); }
        }

        /// <summary>
        /// 异步获取开机从当前开机时间向前的开机记录
        /// </summary>
        /// <returns></returns>
        public async Task<IQueryable<BootLog>> GetTotalBootLogs()
        {
            var bootLog = new BootLogDbSet();

            return await bootLog.GetByTime(DateTime.MinValue, BootTime);
        }

        public async Task<double> GetTotalBootHoursBeforeCurrentBoot()
        {
            var bootLogs = await GetTotalBootLogs();
            return await Task.Run(() =>
            {
                double sum = 0;
                foreach (var log in bootLogs)
                {
                    sum += log.WorkingTimeSpan.TotalHours;
                }
                return Math.Round(sum, 4);
            });
        }

        /// <summary>
        /// 异步获取当前开机射线工作时间
        /// </summary>
        /// <returns></returns>
        public async Task<double> GetXRayGenWorkHoursSinceBoot()
        {
            var xrayGenLog = new XRayGenWorkLogDbSet();

            var logs = await xrayGenLog.GetLogs(BootTime, DateTime.Now);

            return await Task.Run(() =>
            {
                double sum = 0;
                foreach (var xRayGenWorkLog in logs)
                {
                    sum += TimeSpan.FromSeconds(xRayGenWorkLog.Seconds).TotalHours;
                }

                return Math.Round(sum, 4);
            });
        }


        public async Task<IQueryable<XRayGenWorkLog>> GetTotalXRayGenWorkLogs()
        {
            var xrayGenLog = new XRayGenWorkLogDbSet();
            return await xrayGenLog.GetLogs(DateTime.MinValue, DateTime.Now);
        }

        public async Task<IQueryable<XRayGenWorkLog>> GetXRayGenWorkLogsBeforeBoot()
        {
            var xrayGenLog = new XRayGenWorkLogDbSet();
            return await xrayGenLog.GetLogs(DateTime.MinValue, BootTime);
        }

        public async Task<double> GetTotalXRayGenWorkHours()
        {
            var logs = await GetTotalXRayGenWorkLogs();
            return await Task.Run(() =>
            {
                double sum = 0;
                foreach (var xRayGenWorkLog in logs)
                {
                    sum += TimeSpan.FromSeconds(xRayGenWorkLog.Seconds).TotalHours;
                }

                return Math.Round(sum, 4);
            });
        }

        public async Task<int> GetTotalXRayUsageCountHours()
        {
            var logs = await GetXRayGenWorkLogsBeforeBoot();
            return logs.Count();
        }
    }
}
