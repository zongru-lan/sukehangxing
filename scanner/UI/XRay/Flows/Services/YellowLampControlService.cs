using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Control;

namespace UI.XRay.Flows.Services
{
    public class YellowLampControlService
    {
        /// <summary>
        /// 周期性明灯的专用线程是否已经存在：同一时刻，仅允许一个线程明灯
        /// </summary>
        private bool _flareAlarmThreadAlive;

        /// <summary>
        /// 当前正在明灯的时长、间隔、次数等信息。
        /// 如果为null，表示当前没有在闪烁报警
        /// </summary>
        private FlareDesc _flareDesc;

        /// <summary>
        /// 当前射线是否正在发射
        /// </summary>
        private bool _isYellowOn;

        /// <summary>
        /// 单实例服务
        /// </summary>
        public static YellowLampControlService Service { get; private set; }

        static YellowLampControlService()
        {
            Service = new YellowLampControlService();
        }

        protected YellowLampControlService()
        {
        }

        /// <summary>
        /// 启动射线灯控制服务
        /// </summary>
        //public void StartService()
        //{
        //    //ControlService.ServicePart.ScannerWorkingStatesUpdated += ServicePartOnScannerWorkingStatesUpdated;
        //}

        /// <summary>
        /// 停止射线灯控制服务
        /// </summary>
        //public void StopService()
        //{
        //    //ControlService.ServicePart.ScannerWorkingStatesUpdated += ServicePartOnScannerWorkingStatesUpdated;
        //}

        /// <summary>
        /// 实时监测X射线发射状态，并更改设备的X射线指示灯
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="scannerWorkingStates"></param>
        private void ServicePartOnScannerWorkingStatesUpdated(object sender, ScannerWorkingStates scannerWorkingStates)
        {
            lock (this)
            {
                //var xrayOn = scannerWorkingStates.IsXRayGen1Radiating || scannerWorkingStates.IsXRayGen2Radiating;
                var yellowOn = scannerWorkingStates.LEDStates.IsYellowLEDOn;
                if (_isYellowOn != yellowOn && _flareDesc == null)
                {
                    // 射线状态发生变化，并且当前未闪烁报警，则更新射线状态
                    ControlService.ServicePart.LightYellowLamp(yellowOn);
                }
                _isYellowOn = yellowOn;
            }
        }

        /// <summary>
        /// 按指定的时间间隔和次数进行明灯告警
        /// </summary>
        /// <param name="pulseWidth">每次明灯的时长</param>
        /// <param name="interval">每两次明灯之间的间隔时间</param>
        /// <param name="times">明灯的次数</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Flare(TimeSpan pulseWidth, TimeSpan interval, int times)
        {
            // 更新明灯参数
            _flareDesc = new FlareDesc(pulseWidth, interval, times);

            // 如果当前已经有线程在明灯，则不需要再次分配专属线程
            // 如果当前没有正在运行的明灯线程，则在线程池中启动一个新的线程来工作
            if (!_flareAlarmThreadAlive)
            {
                ThreadPool.QueueUserWorkItem(FlaringLampThreadCallback);

                // 新线程已经创建。
                _flareAlarmThreadAlive = true;
            }
        }

        /// <summary>
        /// 线程池回调：通过周期性开关射线指示灯，实现周期性的闪烁报警
        /// </summary>
        /// <param name="state"></param>
        private void FlaringLampThreadCallback(object state)
        {
            try
            {
                var pulse = new TimeSpan(0);
                var interval = new TimeSpan(0);
                int times = 1;

                while (times > 0)
                {
                    lock (this)
                    {
                        if (_flareDesc != null)
                        {
                            // 取出最新的明灯参数（在明灯的过程中，外部由可能更新了明灯参数，因此需要再次取出最新的明灯）
                            pulse = _flareDesc.PulseWidth;
                            interval = _flareDesc.Interval;
                            times = _flareDesc.Times;

                            _flareDesc.Times--;
                        }
                        else
                        {
                            // 如果 _flareDesc == null 了，可能是外部突然停止了明灯，因此将不会再继续
                            times = 0;
                        }

                        // 当剩余的明灯次数为0时，将结束明灯线程
                        if (times == 0)
                        {
                            _flareDesc = null;
                            _flareAlarmThreadAlive = false;

                            // 还原X射线指示灯状态至最新
                            ControlService.ServicePart.LightYellowLamp(_isYellowOn);

                            break;
                        }
                    }

                    ControlService.ServicePart.LightYellowLamp(true);
                    Thread.Sleep(pulse);

                    ControlService.ServicePart.LightYellowLamp(false);
                    Thread.Sleep(interval);
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception, "Unhandled exception in LightAlarmService thread.");
            }
        }
    }
}
