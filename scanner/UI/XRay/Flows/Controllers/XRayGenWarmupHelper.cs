using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Services.Db;

namespace UI.XRay.Flows.Controllers
{
    public class XRayGenWarmupHelper
    {
        /// <summary>
        /// 保存当前预热记录：将当前时间保存为上次预热时间
        /// </summary>
        public static void SaveWarmupRecord()
        {
            bool lastWarmupResult;
            ScannerConfig.Read(ConfigPath.XRayGenLastWarmupResult, out lastWarmupResult);

            //上次预热成功了，才记录当前的关机时间
            if (lastWarmupResult)
            {
                ScannerConfig.Write(ConfigPath.XRayGenLastWarmupTime, DateTime.Now.Ticks);
            }
        }

        public static void SaveWarmupResult(bool result)
        {
            ScannerConfig.Write(ConfigPath.XRayGenLastWarmupResult, result);
        }

        /// <summary>
        /// 获取射线源此次需要预热的时长
        /// </summary>
        /// <returns>需要预热的时长；null表示不需要预热</returns>
        public static TimeSpan? GetWarmupDuration()
        {
            bool alwaysWarmup = false;
            if (!ScannerConfig.Read(ConfigPath.XRayGenAlwaysWarmup, out alwaysWarmup))
            {
                alwaysWarmup = false;
            }

            if (!alwaysWarmup)
                return null;

            long lastTime;
            if (!ScannerConfig.Read(ConfigPath.XRayGenLastWarmupTime, out lastTime))
            {
                lastTime = (DateTime.Now - TimeSpan.FromDays(15)).Ticks;
            }

            var duration = CalculateDurationFromLastWarmupTime(new DateTime(lastTime));
            return duration;
        }

        /// <summary>
        /// 根据上次预热的时刻，计算此次预热的时长
        /// </summary>
        /// <returns></returns>
        public static TimeSpan? CalculateDurationFromLastWarmupTime(DateTime lastTime)
        {
            // 根据时间差，计算本次需要预热的时长

            var ts = DateTime.Now - lastTime;

            if (ts >= TimeSpan.FromDays(90))
            {
                return TimeSpan.FromMinutes(50);
            }
            if (ts >= TimeSpan.FromDays(30))
            {
                return TimeSpan.FromMinutes(10);
            }          
            if (ts >= TimeSpan.FromDays(3))
            {
                return TimeSpan.FromMinutes(5);
            }
            return null;
        }
    }
}
