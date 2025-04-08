using Emgu.CV.Flann;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.Algo;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;

namespace UI.XRay.Flows.Services.DataProcess
{
    public enum AutoCalibrationMode
    {
        /// <summary>
        /// 不更新
        /// </summary>
        None = 0,
        /// <summary>
        /// 扫描前更新一次，默认设置
        /// </summary>
        BeforeScanning = 1,
        /// <summary>
        /// 动态更新
        /// </summary>
        DynamicUpdate = 2
    }

    public abstract class AutoCalibrationServiceBase
    {
        #region 各配置项参数

        /// <summary>
        /// 视角个数
        /// </summary>
        protected int ViewCount = 1;

        /// <summary>
        /// 射线源长时间关闭会使曲线下降。因此在开射线的时候需要强制更新一下满度，即只进行限制值的判断，不进行前后两次满度差异的计算
        /// </summary>
        private int _forceUpdateRefrenceAirTime = 40;

        private DateTime? _lastXRayOffTime = null;

        private bool _alwaysUseNewAirUpdate = false;

        private bool XrayOffTooLong
        {
            get
            {
                if (_lastXRayOffTime.HasValue)
                {
                    return (DateTime.Now - _lastXRayOffTime.Value).TotalSeconds > _forceUpdateRefrenceAirTime;
                }
                return true;
            }
        }

        private bool ForceUpdateRerenceAir
        {
            get { return InDynamicUpdateMode && InInterruptMode == InterruptMode.NotInInterrrupt && !HasUpdateReferenceAir && XrayOffTooLong; }
        }

        public InterruptMode InInterruptMode { get; set; }

        /// <summary>
        /// TODO：暂时在动态扫描模式下长时间不开射线，射线开时强制更新满度，防止意外
        /// </summary>
        protected bool InDynamicUpdateMode = false;

        private double _forceUpdateAirRate = 0.04;


        #endregion 各配置项参数

        #region 本底、满度更新子程序

        /// <summary>
        /// 视角1的本底更新服务
        /// </summary>
        protected GroundDataAutoUpdateRoutine View1GroundRoutine = new GroundDataAutoUpdateRoutine(DetectViewIndex.View1);

        /// <summary>
        /// 视角2的本底更新
        /// </summary>
        protected GroundDataAutoUpdateRoutine View2GroundRoutine = new GroundDataAutoUpdateRoutine(DetectViewIndex.View2);

        /// <summary>
        /// 视角1的满度更新
        /// </summary>
        protected AirDataAutoUpdateRoutineBase View1AirRoutine;

        /// <summary>
        /// 视角2的满度更新
        /// </summary>
        protected AirDataAutoUpdateRoutineBase View2AirRoutine;

        /// <summary>
        /// 视角1的本底变化率验证
        /// </summary>
        protected GroundAirVerification View1GroundVerification;

        /// <summary>
        /// 视角2的本底变化率验证
        /// </summary>
        protected GroundAirVerification View2GroundVerification;

        /// <summary>
        /// 视角1的满度变化率验证
        /// </summary>
        protected GroundAirVerification View1AirVerification;

        /// <summary>
        /// 视角2的满度变化率验证
        /// </summary>
        protected GroundAirVerification View2AirVerification;

        /// <summary>
        /// 视角1基准满度校验，主要用于动态满度更新中.动态满度更新中，基准满度比较重要，每次基准满度按照
        /// </summary>
        protected GroundAirVerification View1StandardAirVerification;
        /// <summary>
        /// 视角2基准满度校验，主要用于动态满度更新中
        /// </summary>
        protected GroundAirVerification View2StandardAirVerification;

        /// <summary>
        /// 当前的满度更新接口
        /// </summary>
        protected CalibrationValidationService ValidationService = new CalibrationValidationService();

        #endregion 本底、满度更新子程序

        public bool IsXRayOn { get; protected set; }

        /// <summary>
        /// 是否更新过基准满度。动态更新中，对于满度的更新均是在基准满度基础上的，基准满度是射线开的时间内采集的
        /// 满度数据生成的满度值，
        /// </summary>
        protected bool HasUpdateReferenceAir;

        protected AutoCalibrationServiceBase()
        {
            Tracer.TraceEnterFunc("RawDataCorrectLogic constructor");

            try
            {
                LoadSettings();
                InternalInitialize();
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            Tracer.TraceExitFunc("RawDataCorrectLogic constructor");
        }

        /// <summary>
        /// 加载配置项
        /// </summary>
        private void LoadSettings()
        {
            //获取视角个数
            ViewCount = 1;
            if (!ScannerConfig.Read(ConfigPath.MachineViewsCount, out ViewCount))
            {
                ViewCount = 1;
            }
        }

        /// <summary>
        /// 内部初始化
        /// </summary>
        private void InternalInitialize()
        {
            var airUpdateRate = 0.01;
            if (!ScannerConfig.Read(ConfigPath.PreProcAirUpdateRateUpper, out airUpdateRate))
            {
                airUpdateRate = 0.02;
            }
            if (airUpdateRate > 0.6 || airUpdateRate < 0)
            {
                airUpdateRate = 0.02;
            }

            if (!ScannerConfig.Read(ConfigPath.PreProcForceAirUpdateRateUpper, out _forceUpdateAirRate))
            {
                _forceUpdateAirRate = 0.02;
            }

            if (_forceUpdateAirRate > 0.6 || _forceUpdateAirRate < 0)
            {
                _forceUpdateAirRate = 0.02;
            }

            if (!ScannerConfig.Read(ConfigPath.PreProcForceUpdateRefrenceAirTime, out _forceUpdateRefrenceAirTime))
            {
                _forceUpdateRefrenceAirTime = 40;
            }

            View1AirVerification = new GroundAirVerification(airUpdateRate);
            View2AirVerification = new GroundAirVerification(airUpdateRate);

            View1StandardAirVerification = new GroundAirVerification(airUpdateRate);
            View2StandardAirVerification = new GroundAirVerification(airUpdateRate);

            var groundUpdateRate = 0.01;
            if (!ScannerConfig.Read(ConfigPath.PreProcGroundUpdateRateUpper, out groundUpdateRate))
            {
                groundUpdateRate = 0.02;
            }
            if (groundUpdateRate > 0.6 || groundUpdateRate < 0)
            {
                groundUpdateRate = 0.02;
            }

            if (!ScannerConfig.Read(ConfigPath.PreProcAlwaysUseLastAirUpdate, out _alwaysUseNewAirUpdate))
            {
                _alwaysUseNewAirUpdate = false;
            }

            View1GroundVerification = new GroundAirVerification(groundUpdateRate);
            View2GroundVerification = new GroundAirVerification(groundUpdateRate);

            Tracer.TraceInfo("Ground Value Change Rate Threshold:", View1GroundVerification.ChangeRate);
            Tracer.TraceInfo("Air Value Change Rate Threshold:", View1AirVerification.ChangeRate);
        }

        /// <summary>
        /// 更新手动标定的本底做为参考值
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void ResetManualCalibratedGround(ScanlineDataBundle ground)
        {
            View1GroundVerification.SetStandard(ground.View1LineData);
            View2GroundVerification.SetStandard(ground.View2LineData);
        }

        /// <summary>
        /// 更新手动标定的满度作为参考值
        /// </summary>
        /// <param name="air"></param>      
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void ResetManualCalibratedAir(ScanlineDataBundle air)
        {
            // 分别更新视角1和视角2的满度参考值
            View1AirVerification.SetStandard(air.View1LineData);
            View2AirVerification.SetStandard(air.View2LineData);

            //手动更新后设置参考满度
            this.SetReferenceAir(air);
        }

        /// <summary>
        /// 设置参考满度
        /// </summary>
        /// <param name="air"></param>
        protected void SetReferenceAir(ScanlineDataBundle air)
        {
            if (air != null)
            {
                if (air.View1LineData != null)
                {
                    View1AirRoutine.SetReferenceAir(air.View1LineData);
                }
                if (air.View2LineData != null && View2AirRoutine != null)
                {
                    View2AirRoutine.SetReferenceAir(air.View2LineData);
                }
            }
        }

        /// <summary>
        /// 判断自动获取的视角的新满度是否合规
        /// </summary>
        /// <param name="airLine">视角2的新满度</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        protected bool IsViewNewAirValid(DetectViewIndex index, GroundAirVerification verifier, ScanlineData airLine)
        {
            // 先验证满度均值是否在合理的区间内
            if (!ValidationService.IsAirValid(airLine))
            {
                Tracer.TraceError(
                    "Fail to cali " + index.ToString() +
                    "  air.Not valid.");
                return false;
            }

            // 验证满度的变化率
            double rate;
            bool ok = verifier.Verify(airLine, _alwaysUseNewAirUpdate, out rate);


            if (ok || _alwaysUseNewAirUpdate)
            {
                //Tracer.TraceInfo("Sucessful to calibrate " + index.ToString() +
                //                  "  air value automatically.New air change rate is: " + rate.ToString());

                return true;
            }

            if (ForceUpdateRerenceAir)
            {
                if (rate < _forceUpdateAirRate)
                {
                    Tracer.TraceInfo("Suc cali  " + index.ToString() +
                                      " air. New air is: " + rate +
                                      ", greator: " + verifier.ChangeRate + ",smaller: " +
                                      _forceUpdateAirRate.ToString() + ", force upd air!");
                    verifier.UpdateAir(airLine);
                    return true;
                }
                Tracer.TraceError("Fail cali" + index.ToString() +
                                  " air. New air is: " + rate +
                                  ", greator: " +
                                  _forceUpdateAirRate.ToString());

                return false;
            }

            Tracer.TraceError(index.ToString() +
                  "air is: " + rate +
                  ", greator:" + verifier.ChangeRate);

            return false;
        }

        protected bool IsViewNewGroundValid(DetectViewIndex index, GroundAirVerification verifier, ScanlineData ground)
        {
            // 先验证自动更新的本底的均值是否在合理的区间内
            if (!ValidationService.IsGroundValid(ground))
            {
                Tracer.TraceError(
                    "Fail to cali" + index.ToString() +
                    " ground.Not valid");
                return false;
            }

            double rate;
            bool ok = verifier.Verify(ground, _alwaysUseNewAirUpdate, out rate);

            // 对自动更新的本底进行校验，如果变化率过高，将被抛弃
            if (ok || _alwaysUseNewAirUpdate)
            {
                return true;
            }

            Tracer.TraceWarning("Fail update " + index.ToString() +
                                " ground. New ground is: " + rate + ", greator:" +
                                verifier.ChangeRate);

            return false;
        }

        /// <summary>
        /// 射线关闭期间，尝试更新视角1或视角2的本底
        /// </summary>
        /// <param name="rawData">一线新采集的数据，至少有一个视角的数据不为空</param>
        /// <param name="groundResult">如果返回值为true，则存储视角1或视角2的本底（其中至少有一个不为null）</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool TryCalibrateGround(RawScanlineDataBundle rawData, out ScanlineDataBundle groundResult)
        {
            if (!IsXRayOn)
            {
                ScanlineData view1Ground = null;
                ScanlineData view2Ground = null;

                if (rawData.View1RawData != null)
                {
                    if (View1GroundRoutine.TryUpdate(rawData.View1RawData, out view1Ground))
                    {
                        if (!IsViewNewGroundValid(DetectViewIndex.View1, View1GroundVerification, view1Ground))
                        {
                            // 新采集的本底不合法，放弃
                            view1Ground = null;
                        }
                    }
                }

                if (rawData.View2RawData != null)
                {
                    if (View2GroundRoutine.TryUpdate(rawData.View2RawData, out view2Ground))
                    {
                        if (!IsViewNewGroundValid(DetectViewIndex.View2, View2GroundVerification, view2Ground))
                        {
                            // 新采集的本底不合法，放弃
                            view2Ground = null;
                        }
                    }
                }

                // 如果有一个视角的本底已经更新，则输出
                if (view1Ground != null && view2Ground != null)
                {
                    //this.HasUpdateReferenceAir = true;
                    groundResult = new ScanlineDataBundle(view1Ground, view2Ground);
                    return true;
                }
            }

            groundResult = null;
            return false;
        }

        /// <summary>
        /// 尝试更新视角1或视角2的满度
        /// </summary>
        /// <param name="rawData"></param>
        /// <param name="airResult">如果返回值为true，则存储至少视角1或视角2中之一的新满度</param>
        /// <returns></returns>
        protected bool TryCalibrateAirBeforeScanning(RawScanlineDataBundle rawData, out ScanlineDataBundle airResult)
        {
            if (IsXRayOn)
            {
                ScanlineData view1Air = null;
                ScanlineData view2Air = null;

                if (rawData.View1RawData != null)
                {
                    if (View1AirRoutine.TryUpdate(rawData.View1RawData, out view1Air))
                    {
                        Tracer.TraceError("zyx view1 try before bag cali.");
                        if (!IsViewNewAirValid(DetectViewIndex.View1, View1AirVerification, view1Air))
                        {
                            Tracer.TraceError("zyx view1 before bag cali.not valid");
                            view1Air = null;
                        }
                    }
                }

                if (rawData.View2RawData != null && View2AirRoutine != null)
                {
                    if (View2AirRoutine.TryUpdate(rawData.View2RawData, out view2Air))
                    {
                        Tracer.TraceError("zyx view2 try before bag cali.");
                        if (!IsViewNewAirValid(DetectViewIndex.View2, View2AirVerification, view2Air))
                        {
                            Tracer.TraceError("zyx view2 before bag cali.not valid");
                            view2Air = null;
                        }
                    }
                }

                // 如果有两个视角有一个已经更新，则输出
                if (view1Air != null && view2Air != null)
                {
                    this.HasUpdateReferenceAir = true;
                    Tracer.TraceError("zyx view2 before bag cali.suc");
                    airResult = new ScanlineDataBundle(view1Air, view2Air);
                    return true;
                }
            }


            //无论成功与否  均不再尝试包前更新
            //this.HasUpdateReferenceAir = true;
            airResult = null;
            return false;
        }

        /// <summary>
        /// 设置X射线的最新状态。根据射线的状态，更新内部状态
        /// </summary>
        /// <param name="on"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void OnXRayStateChanged(bool on)
        {
            if (on)
            {
                View1GroundRoutine.OnXRayOn();
                View2GroundRoutine.OnXRayOn();

                if (InInterruptMode == InterruptMode.NotInInterrrupt)
                {
                    View1AirRoutine.Enable(true);
                    if (View2AirRoutine != null)
                    {
                        View2AirRoutine.Enable(true);
                    }

                    //射线开，重置此标志
                    HasUpdateReferenceAir = false;
                }
            }
            else
            {
                View1GroundRoutine.OnXRayOff();
                View2GroundRoutine.OnXRayOff();

                View1AirRoutine.Enable(false);
                if (View2AirRoutine != null)
                {
                    View2AirRoutine.Enable(false);
                }

                _lastXRayOffTime = DateTime.Now;
            }

            IsXRayOn = on;
        }

        public abstract void Clearup();

        /// <summary>
        /// 尝试更新视角1或视角2的满度
        /// </summary>
        /// <param name="rawData"></param>
        /// <param name="airResult">如果返回值为true，则存储至少视角1或视角2中之一的新满度</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public abstract bool TryCalibrateAir(RawScanlineDataBundle rawData, out ScanlineDataBundle airResult);

        /// <summary>
        /// 检查线数据中非包裹区域
        /// </summary>
        /// <param name="rawData"></param>
        /// <param name="normaizedData"></param>
        /// <param name="airResult"></param>
        /// <returns></returns>
        public abstract bool TryUpdateCurrentAir(RawScanlineDataBundle rawData, ScanlineDataBundle normaizedData, out ScanlineDataBundle airResult);
    }
}
