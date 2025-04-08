using System;
using System.Runtime.CompilerServices;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Common.Utilities;
using UI.XRay.Flows.Services.Db;

namespace UI.XRay.Flows.Services
{
    public class BagCountEventArgs : EventArgs
    {
        public int SessionCount { get; private set; }

        public int TotalCount { get; private set; }

        public BagCountEventArgs(int sessionCount, int totalCount)
        {
            SessionCount = sessionCount;
            TotalCount = totalCount;
        }
    }

    /// <summary>
    /// 行李计数服务
    /// </summary>
    public class BagCounterService
    {
        public static BagCounterService Service { get; private set; }

        static BagCounterService()
        {
            Service = new BagCounterService();
        }

        public event EventHandler<BagCountEventArgs> BagCountChangedWeakEvent
        {
            add { _bagCountChangedWeakEvent.Add(value); }
            remove { _bagCountChangedWeakEvent.Remove(value); }
        }

        private SmartWeakEvent<EventHandler<BagCountEventArgs>> _bagCountChangedWeakEvent =
            new SmartWeakEvent<EventHandler<BagCountEventArgs>>();

        /// <summary>
        /// 启动软件后共扫描的图像数
        /// </summary>
        public int CountSinceOpen { get; private set; }

        /// <summary>
        /// 此次会话期间的bag计数,切换用户后置零
        /// </summary>
        public int SessionCount { get; private set; }

        /// <summary>
        /// bag总计数
        /// </summary>
        public int TotalCountSinceInstall { get; private set; }

        private bool _resetCounterWhenLogin;

        private ImageRecordDbSet _recordDbSet;

        /// <summary>
        /// 设置或获取 标志位：用户登录后自动复位临时计数器
        /// </summary>
        public bool ResetCounterWhenLogin
        {
            get { return _resetCounterWhenLogin; }
            set
            {
                lock (this)
                {
                    _resetCounterWhenLogin = value;
                    ScannerConfig.Write(ConfigPath.PkgCounterResetSessionCounterWhenLogin, value);
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        protected BagCounterService()
        {
            // 从注册表中读取物体总数
            try
            {
                int totalCount = 0;
                //// 优先从主控板读取包裹计数
                //if (!ControlService.ServicePart.GetTotalBagCount(ref totalCount))
                //{
                //    Tracer.TraceWarning("[BagCount] Failed to read bag count from control board.");
                //    ScannerConfig.Read(ConfigPath.SystemTotalBagCount, out totalCount);
                //}
                _recordDbSet = new ImageRecordDbSet();
                totalCount = _recordDbSet.Count(); ;
                TotalCountSinceInstall = totalCount;
                Tracer.TraceInfo($"[BagCount] Total bag count: {TotalCountSinceInstall}");
                CountSinceOpen = 0;
                //InitAsync();

                ScannerConfig.Read(ConfigPath.PkgCounterResetSessionCounterWhenLogin, out _resetCounterWhenLogin);
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }
        private async void InitAsync()
        {
            var imageRecordsManager = new ImageRecordDbSet();
            TotalCountSinceInstall = await imageRecordsManager.CountAsync();
        }

        /// <summary>
        /// 清空临时计数器
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void ResetSessionCounter()
        {
            SessionCount = 0;
            _bagCountChangedWeakEvent.Raise(this, new BagCountEventArgs(SessionCount, TotalCountSinceInstall));
        }

        /// <summary>
        /// 包裹计数加一
        /// </summary>
        /// <param name="isScanning">是否是扫描模式</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Increase(bool isScanning = true)
        {
            lock (this)
            {
                SessionCount++;
                //扫描模式下，总数和开机后扫描数加一；培训模式下，计数不变
                if (isScanning)
                {
                    TotalCountSinceInstall++;
                    CountSinceOpen++;
                    Tracer.TraceInfo("[BacCount] Bag count increased");
                }

                // 尽量不要周期性的存储行李总数，避免造成注册表频繁更新
                //if (TotalCountSinceInstall % 50 == 0)
                //{
                //    SaveBagCount();
                //}
            }
            if (isScanning)
            {
                ControlService.ServicePart.SetBagCount(1, 0);
                //Tracer.TraceInfo("[BacCount] Sent set bag count command to control board");
                //Tracer.TraceInfo("测试包裹数量_" + "总数：" + TotalCountSinceInstall + "_当前数量：" + CountSinceOpen);
            }

            _bagCountChangedWeakEvent.Raise(this, new BagCountEventArgs(SessionCount, TotalCountSinceInstall));
        }

        /// <summary>
        /// 保存扫描物体计数
        /// 在软件退出时，保存计数
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SaveBagCount()
        {
            try
            {
                ScannerConfig.Write(ConfigPath.SystemTotalBagCount, TotalCountSinceInstall);
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }
    }
}
