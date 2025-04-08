using System;
using System.Configuration;
using System.Linq;
using System.Net.Configuration;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.Algo;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Common.Utilities;

namespace UI.XRay.Flows.Services.DataProcess
{
    public enum CalibrationResultCode
    {
        /// <summary>
        /// 标定失败：数据量不足
        /// </summary>
        NotStarted,

        /// <summary>
        /// 标定成功
        /// </summary>
        Success,

        /// <summary>
        /// 本底标定失败：本底值超过设定的阈值
        /// </summary>
        GroundGreatThanThreshold,

        /// <summary>
        /// 满度标定失败：满度值低于设定的阈值
        /// </summary>
        AirLowThanThreshold,
    }

    /// <summary>
    /// 本底或满度的标定结果
    /// </summary>
    public class CalibrationResult
    {
        public CalibrationResultCode ResultCode { get; private set; }

        /// <summary>
        /// 标定后的本底或满度值
        /// </summary>
        public ScanlineDataBundle Value { get; private set; }

        public CalibrationResult(CalibrationResultCode code, ScanlineDataBundle line)
        {
            ResultCode = code;
            Value = line;
        }
    }

    /// <summary>
    /// 本底满度手动标定服务
    /// 此服务依赖于图像数据采集服务
    /// </summary>
    public class ManualCalibrationService
    {
        /// <summary>
        /// 弱事件，满度校正结束：如果参数为null则表示校正失败
        /// </summary>
        public event EventHandler<ScanlineDataBundle> AirCalibratedWeakEvent
        {
            add { _aircalibratedWeakEvent.Add(value); }
            remove { _aircalibratedWeakEvent.Remove(value); }
        }

        private readonly SmartWeakEvent<EventHandler<ScanlineDataBundle>> _aircalibratedWeakEvent =
            new SmartWeakEvent<EventHandler<ScanlineDataBundle>>();

        /// <summary>
        /// 弱事件，本底校正结束：如果参数为null则表示校正失败
        /// </summary>
        public event EventHandler<ScanlineDataBundle> GroundCalibratedWeakEvent
        {
            add { _groundcalibratedWeakEvent.Add(value); }
            remove { _groundcalibratedWeakEvent.Remove(value); }
        }

        private readonly SmartWeakEvent<EventHandler<ScanlineDataBundle>> _groundcalibratedWeakEvent =
            new SmartWeakEvent<EventHandler<ScanlineDataBundle>>();

        private CalibratingMode _mode = CalibratingMode.None;

        /// <summary>
        /// 本底标定程序
        /// </summary>
        //private readonly GroundAirManualCalibrationRoutine _groundAirCalibration = new GroundAirManualCalibrationRoutine();

        private GroundAirManualCalibrationRoutine _view1GroundUpdateRoutine = new GroundAirManualCalibrationRoutine(DetectViewIndex.View1);
        private GroundAirManualCalibrationRoutine _view1AirUpdateRoutine = new GroundAirManualCalibrationRoutine(DetectViewIndex.View1);

        private GroundAirManualCalibrationRoutine _view2GroundUpdateRoutine = new GroundAirManualCalibrationRoutine(DetectViewIndex.View2);
        private GroundAirManualCalibrationRoutine _view2AirUpdateRoutine = new GroundAirManualCalibrationRoutine(DetectViewIndex.View2);

        /// <summary>
        /// 满度标定程序
        /// </summary>
        //private readonly AirManualCalibrationRoutine _airCalibration = new AirManualCalibrationRoutine();

        /// <summary>
        /// 单实例
        /// </summary>
        public static ManualCalibrationService Service { get; private set; }

        //private ScanlineDataBundle _ground;

        private ScanlineData _view1Ground = null;

        private ScanlineData _view2Ground = null;

        private ScanlineData _view1Air = null;

        private ScanlineData _view2Air = null;

        private int _viewsCount;

        private CalibrationResultCode _groundCalibrationResult;

        //private ScanlineDataBundle _air;

        private CalibrationResultCode _airCalibrationResult;

        /// <summary>
        /// 手动更新服务是否已经启动
        /// </summary>
        public bool IsServiceStarted { get; private set; }

        /// <summary>
        /// 当前是否正在忙碌：正在更新本底或满度。
        /// </summary>
        public bool IsBusy
        {
            get { return _mode != CalibratingMode.None; }
        }

        private CalibrationValidationService _validationService = new CalibrationValidationService();

        static ManualCalibrationService()
        {
            Service = new ManualCalibrationService();
        }

        /// <summary>
        /// 构造函数定义为受保护的，不允许在外部定义新的实例
        /// </summary>
        protected ManualCalibrationService()
        {
            try
            {
                if (!ScannerConfig.Read(ConfigPath.MachineViewsCount, out _viewsCount))
                {
                    _viewsCount = 1;
                }
            }
            catch (Exception)
            {
                
            }

            _mode = CalibratingMode.None;
        }

        /// <summary>
        /// 启动服务：订阅数据采集事件
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void StartService()
        {
            if (IsServiceStarted)
            {
                return;
            }

            IsServiceStarted = true;
            _mode = CalibratingMode.None;
            CaptureService.ServicePart.ScanlineCaptured += ServicePartOnScanlineCaptured;
        }

        /// <summary>
        /// 停止服务，清理资源
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void StopService()
        {
            if (IsServiceStarted)
            {
                IsServiceStarted = false;
                _mode = CalibratingMode.None;
                CaptureService.ServicePart.ScanlineCaptured -= ServicePartOnScanlineCaptured;
            }
        }

        /// <summary>
        /// 异步校正本底
        /// </summary>
        /// <param name="maxWaitMs">最长等待时间，一般为1500，即1.5秒</param>
        /// <returns>如果在等待时间内，本底更新成功，则返回更新成功的本底；否则返回null</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public Task<CalibrationResult> CalibrateGroundAsync(double maxWaitMs = 1500)
        {
            if (IsBusy)
            {
                return null;
            }

            // 先设置更新本底的标志位，然后异步等待更新完成
            _mode = CalibratingMode.Ground;

            _view1GroundUpdateRoutine = new GroundAirManualCalibrationRoutine(DetectViewIndex.View1);
            _view2GroundUpdateRoutine = new GroundAirManualCalibrationRoutine(DetectViewIndex.View2);

            _view1Ground = null;
            _view2Ground = null;

            _groundCalibrationResult = CalibrationResultCode.NotStarted;

            return Task.Run(() =>
            {
                var passedTime = 0;
                while (passedTime <= maxWaitMs)
                {
                    lock (this)
                    {
                        if (_viewsCount == 1)
                        {
                            if (_view1Ground != null)
                            {
                                _mode = CalibratingMode.None;
                                _groundCalibrationResult = CalibrationResultCode.Success;
                                return new CalibrationResult(_groundCalibrationResult, new ScanlineDataBundle(_view1Ground, null));
                            }
                        }
                        else
                        {
                            // 对于双视角，必须两个视角都更新成功，才计算为成功
                            if (_view1Ground != null && _view2Ground != null)
                            {
                                _mode = CalibratingMode.None;
                                _groundCalibrationResult = CalibrationResultCode.Success;
                                return new CalibrationResult(_groundCalibrationResult, new ScanlineDataBundle(_view1Ground, _view2Ground));
                            }
                        }
                    }
                    
                    Thread.Sleep(20);
                    passedTime += 20;
                }

                lock (this)
                {
                    _mode = CalibratingMode.None;
                }

                return null;
            });
        }

        /// <summary>
        /// 异步校正满度
        /// </summary>
        /// <param name="maxWaitMs">最长等待时间，如果在此时间内仍未成功，则放弃</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public Task<CalibrationResult> CalibrateAirAsync(double maxWaitMs = 1500)
        {
            if (IsBusy)
            {
                return null;
            }

            _mode = CalibratingMode.Air;
            _view1Air = null;
            _view2Air = null;
            _view1AirUpdateRoutine = new GroundAirManualCalibrationRoutine(DetectViewIndex.View1);
            _view2AirUpdateRoutine = new GroundAirManualCalibrationRoutine(DetectViewIndex.View2);

            //_air = null;
            _airCalibrationResult = CalibrationResultCode.NotStarted;

            return Task.Run(() =>
            {
                var passedTime = 0;
                while (passedTime <= maxWaitMs)
                {
                    lock (this)
                    {
                        if (_viewsCount == 1)
                        {
                            if (_view1Air != null)
                            {
                                _mode = CalibratingMode.None;
                                _airCalibrationResult = CalibrationResultCode.Success;
                                return new CalibrationResult(_airCalibrationResult, new ScanlineDataBundle(_view1Air, null));
                            }
                        }
                        else
                        {
                            // 对于双视角，必须两个视角都成功，才算成功
                            if (_view1Air != null && _view2Air != null)
                            {
                                _mode = CalibratingMode.None;
                                _airCalibrationResult = CalibrationResultCode.Success;
                                return new CalibrationResult(_airCalibrationResult, new ScanlineDataBundle(_view1Air, _view2Air));
                            }
                        }
                    }

                    Thread.Sleep(20);
                    passedTime += 20;
                }

                lock (this)
                {
                    _mode = CalibratingMode.None;
                }
                return null;
            });
        }
        
        /// <summary>
        /// 接收实时扫描的图像数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="bundle">原始数据</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void ServicePartOnScanlineCaptured(object sender, RawScanlineDataBundle bundle)
        {
            if (_mode == CalibratingMode.Ground)
            {
                CalibrateGround(bundle);
            }
            else if (_mode == CalibratingMode.Air)
            {
                CalibrateAir(bundle);
            }
        }

        /// <summary>
        /// 标定两个视角的满度数据
        /// </summary>
        /// <param name="bundle"></param>
        public void CalibrateAir(RawScanlineDataBundle bundle)
        {
            if (bundle.View1RawData != null)
            {
                if (_view1AirUpdateRoutine != null && _view1AirUpdateRoutine.TryUpdate(bundle.View1RawData, out _view1Air))
                {
                    if (!_validationService.IsAirValid(_view1Air))
                    {
                        _view1Air = null;
                        _view1AirUpdateRoutine = null;
                        _airCalibrationResult = CalibrationResultCode.AirLowThanThreshold;
                        Tracer.TraceError("Failed to calibrate view1 air value manually.");
                    }
                }
            }

            if (bundle.View2RawData != null)
            {
                if (_view2AirUpdateRoutine != null && _view2AirUpdateRoutine.TryUpdate(bundle.View2RawData, out _view2Air))
                {
                    if (!_validationService.IsAirValid(_view2Air))
                    {
                        _view2Air = null;
                        _view2AirUpdateRoutine = null;
                        _airCalibrationResult = CalibrationResultCode.AirLowThanThreshold;
                        Tracer.TraceError("Failed to calibrate view2 air value manually.");
                    }
                }
            }

            if (_view1Air != null && _view2Air != null)
            {
                _view1AirUpdateRoutine = null;
                _view2AirUpdateRoutine = null;
                _aircalibratedWeakEvent.Raise(this, new ScanlineDataBundle(_view1Air, _view2Air));

                Tracer.TraceInfo("View1 Air value has been calibrated manually.");
            }
            else if(_view1Air != null)
            {
                _view1AirUpdateRoutine = null;
                _aircalibratedWeakEvent.Raise(this, new ScanlineDataBundle(_view1Air, null));
            }

            // 标定满度
            //if (_airCalibration.TryUpdate(bundle, out _air))
            //{
            //    _mode = CalibratingMode.None;

            //    if (_validationService.IsAirValid(_air.ChannelAXRayLineDataBundle))
            //    {
            //        _airCalibrationResult = CalibrationResultCode.Success;
            //        _aircalibratedWeakEvent.Raise(this, _air);
            //        Tracer.TraceInfo("Air value has been calibrated manually.");
            //    }
            //    else
            //    {
            //        _airCalibrationResult = CalibrationResultCode.AirLowThanThreshold;
            //        Tracer.TraceError("Failed to calibrate air value manually.");
            //    }
            //}
        }

        public void CalibrateGround(RawScanlineDataBundle bundle)
        {
            if (bundle.View1RawData != null)
            {
                if (_view1GroundUpdateRoutine != null && _view1GroundUpdateRoutine.TryUpdate(bundle.View1RawData, out _view1Ground))
                {
                    if (!_validationService.IsGroundValid(_view1Ground))
                    {
                        _view1Ground = null;
                        _view1GroundUpdateRoutine = null;
                        _groundCalibrationResult = CalibrationResultCode.GroundGreatThanThreshold;
                        Tracer.TraceError("Failed to calibrate view1 ground value manually.");
                    }
                }
            }

            if (bundle.View2RawData != null)
            {
                if (_view2GroundUpdateRoutine != null && _view2GroundUpdateRoutine.TryUpdate(bundle.View2RawData, out _view2Ground))
                {
                    if (!_validationService.IsGroundValid(_view2Ground))
                    {
                        _view2Ground = null;
                        _view2GroundUpdateRoutine = null;
                        _groundCalibrationResult = CalibrationResultCode.GroundGreatThanThreshold;
                        Tracer.TraceError("Failed to calibrate view2 ground value manually.");
                    }
                }
            }

            if (_view1Ground != null && _view1GroundUpdateRoutine != null)
            {
                // 一旦更新完毕，将其设置为空（保证每个视角的本底只向外输出一次）
                _view1GroundUpdateRoutine = null;

                _groundcalibratedWeakEvent.Raise(this, new ScanlineDataBundle(_view1Ground, null));
                Tracer.TraceInfo("View1 Ground value has been calibrated manually.");
            }

            if (_view2Ground != null && _view2GroundUpdateRoutine != null)
            {
                _view2GroundUpdateRoutine = null;

                _groundcalibratedWeakEvent.Raise(this, new ScanlineDataBundle(null, _view2Ground));
                Tracer.TraceInfo("View2 Ground value has been calibrated manually.");
            }
        }

        /// <summary>
        /// 标定工作流程枚举
        /// </summary>
        enum CalibratingMode
        {
            None,
            Ground,
            Air
        }
    }
}
