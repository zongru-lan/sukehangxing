using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Services.Db;

namespace UI.XRay.Flows.Services
{
    public static class UpdateInitEvent
    {
        public delegate void updateInitDelegate();
        public static event updateInitDelegate updateInit;
        public static bool SendInit()
        {
            if(updateInit!=null)
            {
                updateInit();
                return true;
            }
            return false;
        }
    }
    /// <summary>
    /// 设备部件工作计时服务（X射线发生器、输送机等）：统计当前日期中指定小时（0-23）内的工作时长。以秒计算
    /// 运行机制：在一个线程定时器中，每隔一分钟左右钟检测一次当前的部件工作时间片队列，若发现有前一个小时的工作记录，则全部记录下来
    /// </summary>
    public class DevicePartsWorkTimingService
    {
        public static DevicePartsWorkTimingService ServicePart { get; private set; }

        static DevicePartsWorkTimingService()
        {
            ServicePart = new DevicePartsWorkTimingService();
        }

        /// <summary>
        /// 小时计时器，每到一个整点，响应一次，记录此整点期间的工作时间
        /// </summary>
        private Timer _tickTimer;

        /// <summary>
        /// X射线发生器工作计时单元链表
        /// </summary>
        private LinkedList<TimingSlice> _xrayGenTimingUnits = new LinkedList<TimingSlice>();
 
        /// <summary>
        /// 输送机工作计时单元链表
        /// </summary>
        private LinkedList<TimingSlice> _conveyorTimingUnits = new LinkedList<TimingSlice>();

        /// <summary>
        /// 上一次射线发射的时刻
        /// </summary>
        private DateTime? _lastXRayOnTime;

        /// <summary>
        /// 上一次输送机开始运转的时刻
        /// </summary>
        private DateTime? _lastConveyorStartTime;

        protected DevicePartsWorkTimingService()
        {
            // 每隔一段时间运行一次，检查当前的计时队列
            _tickTimer = new Timer(HourTimingCallback, null, TimeSpan.FromSeconds(30), Timeout.InfiniteTimeSpan);
            XrayWorkingCountSinceBoot = 0;
            if (!ScannerConfig.Read(ConfigPath.XRayGenWorkCount,out _xrayWorkingCountBeforeBoot))
            {
                _xrayWorkingCountBeforeBoot = 0;
            }
        }

        /// <summary>
        /// 定时器回调：定时检测是否需要保存前一个小时的工作时长记录
        /// </summary>
        /// <param name="state"></param>
        private void HourTimingCallback(object state)
        {
            try
            {
                TryRecordConveyorWorkTimeInPreHour();
                TryRecordXRayGenWorkTimeInPreHour();
            }
            catch (Exception e)
            {
                Tracer.TraceException(e, "Exception happened when try to record Device Part Working Time in timer.");
            }
            
            lock (this)
            {
                if (_tickTimer != null)
                {
                    _tickTimer.Change(TimeSpan.FromSeconds(30), Timeout.InfiniteTimeSpan);
                }
            }
        }

        /// <summary>
        /// 记录输送机开始转动时刻
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void RecordConveyorStartedTime()
        {
            if (_lastConveyorStartTime == null)
            {
                _lastConveyorStartTime = DateTime.Now;
            }
        }

        /// <summary>
        /// 记录输送机停止时刻
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void RecordConveyorStoppedTime()
        {
            if (_lastConveyorStartTime != null)
            {
                _conveyorTimingUnits.AddLast(
                    new LinkedListNode<TimingSlice>(new TimingSlice(_lastConveyorStartTime.Value, DateTime.Now)));
                _lastConveyorStartTime = null;
            }
        }

        /// <summary>
        /// 记录射线打开时刻
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void RecordXRayOnTime()
        {
            if (_lastXRayOnTime == null)
            {
                _lastXRayOnTime = DateTime.Now;
            }
        }

        /// <summary>
        /// 记录射线关闭时刻
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void RecordXRayOffTime()
        {
            if (_lastXRayOnTime != null)
            {
                _xrayGenTimingUnits.AddLast(
                    new LinkedListNode<TimingSlice>(new TimingSlice(_lastXRayOnTime.Value, DateTime.Now)));
                _lastXRayOnTime = null;
                XrayWorkingCountSinceBoot += 1;
                XrayTotalWorkingCount = XrayWorkingCountSinceBoot + _xrayWorkingCountBeforeBoot;
                UpdateInitEvent.SendInit();
            }
        }

        public void Cleanup()
        {
            lock (this)
            {
                if (_tickTimer != null)
                {
                    _tickTimer.Dispose();
                    _tickTimer = null;
                }                
            }
            
            // 结束前，将最后的记录存储下来
            RecordConveyorStoppedTime();
            RecordXRayOffTime();

            // 尝试记录前一个小时的工作时长
            TryRecordConveyorWorkTimeInPreHour();
            TryRecordXRayGenWorkTimeInPreHour();

            // 尝试记录当前所有的工作时长
            TryRecordWorkTimeInCurHour();

            ScannerConfig.Write(ConfigPath.XRayGenWorkCount, XrayTotalWorkingCount);
        }

        /// <summary>
        /// 尝试记录前一个小时的X射线发射日志（按小时进行记录）
        /// </summary>
        private void TryRecordXRayGenWorkTimeInPreHour()
        {
            TimeSpan ts;
            lock (this)
            {
                ts = TryGetPreHourWorkingTime(_xrayGenTimingUnits);
            }

            if (ts.TotalSeconds >= 1)
            {
                var dbSet = new XRayGenWorkLogDbSet();

                // 当前时间，往前推移一个小时
                var date = DateTime.Now - TimeSpan.FromHours(1);
                dbSet.Add(new XRayGenWorkLog(date.Date, date.Hour, ts.TotalSeconds));
            }
        }

        /// <summary>
        /// 尝试记录前一个小时的输送机工作日志
        /// </summary>
        private void TryRecordConveyorWorkTimeInPreHour()
        {
            TimeSpan ts;

            lock (this)
            {
                ts = TryGetPreHourWorkingTime(_conveyorTimingUnits);
            }

            if (ts.TotalSeconds >= 1)
            {
                var dbSet = new ConveyorWorkLogDbSet();

                // 当前时间，往前推移一个小时
                var date = DateTime.Now - TimeSpan.FromHours(1);
                dbSet.Add(new ConveyorWorkLog(date.Date, date.Hour, ts.TotalSeconds));
            }
        }

        private void TryRecordWorkTimeInCurHour()
        {
            TimeSpan ts;
            lock (this)
            {
                ts = GetWorkingTime(_xrayGenTimingUnits);
            }

            if (ts.TotalSeconds >= 1)
            {
                var dbSet = new XRayGenWorkLogDbSet();
                var date = DateTime.Now;
                dbSet.Add(new XRayGenWorkLog(date.Date, date.Hour, ts.TotalSeconds));
            }

            lock (this)
            {
                ts = GetWorkingTime(_conveyorTimingUnits);
            }

            if (ts.TotalSeconds >= 1)
            {
                var dbSet = new ConveyorWorkLogDbSet();
                var date = DateTime.Now;
                dbSet.Add(new ConveyorWorkLog(date.Date, date.Hour, ts.TotalSeconds));
            }
        }

        /// <summary>
        /// 计算获取当前所有统计时长
        /// </summary>
        /// <param name="slices"></param>
        /// <returns></returns>
        private TimeSpan GetWorkingTime(LinkedList<TimingSlice> slices)
        {
            var duration = new TimeSpan();

            if (slices != null && slices.Count > 0)
            {
                var firstUnit = slices.First;

                while (firstUnit != null)
                {
                    duration += firstUnit.Value.Duration;

                    slices.RemoveFirst();
                    firstUnit = slices.First;
                }
            }

            return duration;
        }
        private TimeSpan GetWorkingTimeWithoutRemove(LinkedList<TimingSlice> slices)
        {
            var duration = new TimeSpan();

            if (slices != null && slices.Count > 0)
            {
                foreach (var item in slices)
                {
                    if (item != null)
                    {
                        duration += item.Duration;
                    }
                    
                }
            }
            return duration;
        }
        /// <summary>
        /// 尝试获取前一个小时的工作计时统计
        /// </summary>
        /// <param name="slices">工作时间片链表</param>
        /// <returns></returns>
        private TimeSpan TryGetPreHourWorkingTime(LinkedList<TimingSlice> slices)
        {
            var duration = new TimeSpan();

            if (slices != null && slices.Count > 0)
            {
                var firstUnit = slices.First.Value;

                // 记录的时间对应的小时，已经不是当前的小时，则认定为已经过了一个整点
                while (firstUnit.StartTime.Hour != DateTime.Now.Hour)
                {
                    duration += firstUnit.Duration;

                    if (firstUnit.EndTime.Hour == DateTime.Now.Hour)
                    {
                        // 开始时间与结束时间跨越整点：调整开始时间至当前整点，并从上个整点中扣除此整点部分的时间
                        firstUnit.StartTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
                            DateTime.Now.Hour, 0, 0);
                        duration -= firstUnit.Duration;

                        break;
                    }
                    
                    slices.RemoveFirst();
                    if (slices.Count == 0)
                    {
                        break;
                    }

                    firstUnit = slices.First.Value;
                }
            }

            return duration;
        }

        /// <summary>
        /// 获取本次启动后射线源工作时间
        /// </summary>
        /// <returns></returns>
        public double GetXrayWorkingTimeSinceBoot()
        {
            TimeSpan ts;
            lock (this)
            {
                ts = GetWorkingTimeWithoutRemove(_xrayGenTimingUnits);
            }
            return Math.Round(ts.TotalHours, 4);
        }
        /// <summary>
        /// 开机后射线工作次数
        /// </summary>
        public int XrayWorkingCountSinceBoot { get; set; }
        /// <summary>
        /// 射线工作总次数
        /// </summary>
        public int XrayTotalWorkingCount { get; set; }
        /// <summary>
        /// 本次启动前射线工作次数
        /// </summary>
        private int _xrayWorkingCountBeforeBoot;

        /// <summary>
        /// 表示一个计时单元：记录起始时刻和结束时刻
        /// </summary>
        class TimingSlice
        {
            public TimingSlice()
            {
                StartTime = EndTime = DateTime.Now;
            }

            public TimingSlice(DateTime startTime, DateTime endTime)
            {
                StartTime = startTime;
                EndTime = endTime;
            }

            /// <summary>
            /// 时长
            /// </summary>
            public TimeSpan Duration
            {
                get { return EndTime - StartTime; }
            }

            public DateTime StartTime { get; set; }

            public DateTime EndTime { get; set; }
        }
    }
}
