
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.Algo;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Control;
using UI.XRay.ControlWorkflows;
using UI.XRay.Flows.Services.DataProcess;

namespace UI.XRay.Flows.Services
{
    /// <summary>
    /// 中断拼图状态
    /// </summary>
    public enum InterruptMode
    {
        /// <summary>
        /// 不在中断拼图
        /// </summary>
        NotInInterrrupt,
        /// <summary>
        /// 回退
        /// </summary>
        Backward,
        /// <summary>
        /// 在中断拼图
        /// </summary>
        InInterrrupt,
        /// <summary>
        /// 中断拼图恢复中
        /// </summary>
        Recovering
    }

    /// <summary>
    /// 中断拼图服务类。匹配的方法是查找中断前和中断后的数据匹配的位置，方法是计算相关系数最大的位置
    /// 
    /// 自动倒带的距离是0.35m，计算时间0.35/0.2=1.75s = 1750ms. 每一线数据时间为4ms，大概是438线。
    /// 
    /// 上升沿时间大概为0.4s即400ms，大概是100线（todo：此处值需要根据实际参数进行计算），则实际处理数据为中断恢复过程开始后338线后的数据。
    /// 
    /// 为减少计算，拼图阶段前200线不缓存，即缓存匹配位置的前138线，同时缓存匹配位置后100多线（共300线）从中找到和模板数据相关系数最大的数据
    /// 
    /// </summary>
    public class InterruptAndMatchService
    {
        /// <summary>
        /// 模板数据缓存数
        /// </summary>
        private int _templateDataCacheCount = 6;

        /// <summary>
        /// 缓存的模板数据，用于缓存部分数据，（大概是4线），进入中断模式后此数据是拼图的原始数据，todo：这里的数据是射线关闭前的数据，因为这部分数据都已经显示出来了
        /// </summary>
        private readonly Queue<RawScanlineDataBundle> _templateBundles = new Queue<RawScanlineDataBundle>();

        /// <summary>
        /// 中断后开始拼图功能时缓存的。todo：这个参数是否要做成配置参数
        /// </summary>
        private readonly int _registerDataMaxCacheCount;

        /// <summary>
        /// 缓存中断后的数据，根据_serBundles找到匹配的位置，然后将
        /// </summary>
        private readonly List<RawScanlineDataBundle> _registerBundles;

        /// <summary>
        /// 开始进行中断配准的列数，中断恢复后从经过_startCalCols行数据开始缓存，提高配准效率，但不同型号设备此参数可能不一样，
        /// 可能要根据回退时间及采样时间等计算。
        /// </summary>
        private readonly int _startCalCols;

        /// <summary>
        /// 记录最大相关系数
        /// </summary>
        private double _maxRelatedCoefficient;

        /// <summary>
        /// 记录在配准数据链表中与模板数据具有最大相关性的数据的下一线数据的位置，即在配准数据链表中的index
        /// </summary>
        private int _matchPosition;

        /// <summary>
        /// 记录模板数据的平方和
        /// </summary>
        private double _templateDataSigma;

        /// <summary>
        /// 记录中断拼图前电机的方向，如果中断恢复时电机方向相反，这不进行数据的匹配，否则执行中断拼图。同时，根据电机方向选择视角的数据。
        /// 
        /// moveforward--> view1;  movebackward --> view2
        /// </summary>
        public ConveyorDirection LastMoveDirection = ConveyorDirection.Stop;

        /// <summary>
        /// 记录配准所用到的视角
        /// </summary>
        private DetectViewIndex _matchViewIndex = DetectViewIndex.View1;

        /// <summary>
        /// 是否处于中断拼图模式
        /// </summary>
        public bool InInterruptMode { get; private set; }

        public InterruptMode InterruptState { get; set; }

        /// <summary>
        ///
        /// </summary>
        private readonly object _interruptLock = new object();

        private bool _isDualView;

        public event Action<List<RawScanlineDataBundle>> LeaveEventHandler;

        private ConveyorDirection _currentDirection = ConveyorDirection.Stop;
        public InterruptAndMatchService()
        {
            if (!ScannerConfig.Read(ConfigPath.MachineInterruptModeMatchLineCount,out _templateDataCacheCount))
            {
                _templateDataCacheCount = 6;
            }
            if (!ScannerConfig.Read(ConfigPath.MachineInterruptModeStartCalCols,out _startCalCols))
            {
                _startCalCols = 1;
            }

            if (!ScannerConfig.Read(ConfigPath.MachineInterruptModeRegisterDataCacheCount,out _registerDataMaxCacheCount))
            {
                _registerDataMaxCacheCount = 400;
            }
            int viewsCount = 1;
            if (!ScannerConfig.Read(ConfigPath.MachineViewsCount, out viewsCount))
            {
                viewsCount = 1;
            }
            _isDualView = viewsCount > 1 ? true : false;
            _registerBundles = new List<RawScanlineDataBundle>(_registerDataMaxCacheCount);
        }

        public void OnConveyorDirectionChanged(ConveyorDirection direction)
        {
            lock (_interruptLock)
            {
                _currentDirection = direction;
            }
        }

        public void OnUserControlMotor(ConveyorDirection direction)
        {
            lock (_interruptLock)
            {
                if (direction != ConveyorDirection.Stop)
                {
                    if (InterruptState == InterruptMode.InInterrrupt)
                    {

                        if (LastMoveDirection != direction)
                        {
                            //中断拼图模式下，电机反向
                            ProcessExitInterruptMode();
                            Tracer.TraceWarning("Interrupt Mode: Exit interrup mode, because of motor move direction changed!");
                        }
                        else
                        {
                            Tracer.TraceInfo($"[InterruptTailAfter] OnUserControlMotor - Enter into interrrupt recovering status!");
                            //同方向，中断恢复
                            InterruptState = InterruptMode.Recovering;
                        }
                    }
                    LastMoveDirection = direction;
                }
            }
        }

        public void ServicePart_EnterInterruptMode()
        {
            lock (_interruptLock)
            {
                Tracer.TraceInfo($"[InterruptTailAfter] ServicePart_EnterInterruptMode - Enter into interrrupt backward status!");
                InterruptState = InterruptMode.Backward;
                //计算配准数据的平方和
                _templateDataSigma = CalQuadraticSum(_templateBundles);

                Tracer.TraceInfo("Interrupt Mode: System enter interrupt mode, record the motor direction: " + LastMoveDirection.ToString());
            }
        }

        public void ServicePart_WorkModeChanged(ScannerWorkMode mode)
        {
            lock (_interruptLock)
            {
                Tracer.TraceInfo($"[InterruptTailAfter] ServicePart_WorkModeChanged - exit interrrupt status!");
                InterruptState = InterruptMode.NotInInterrrupt;
            }
            Tracer.TraceInfo($"[Interrupt] Reset InterruptState, Current Mode: {mode}");
        }

        public void Match(RawScanlineDataBundle bundle, out RawScanlineDataBundle singleBundle,
            out List<RawScanlineDataBundle> multipleBundles)
        {
            singleBundle = null;
            multipleBundles = null;

            //记录电机方向，用于获取配准是使用那个视角的数据
            lock (_interruptLock)
            {
                //处理重新进入中断拼图模式
                //ProcessReIntoInterruptMode();

                if (InterruptState == InterruptMode.Recovering && _currentDirection == LastMoveDirection)
                {
                    //进入中断拼图后缓存数据，用于查找配准位置
                    _registerBundles.Add(bundle);

                    //计算最大相关系数及相应位置
                    CalMaxRelatedCoefAndPosition();

                    //如果数据缓存够了
                    if (_registerBundles.Count >= _registerDataMaxCacheCount)
                    {
                        var registerData = _registerBundles.Skip(_matchPosition).ToList();

                        Tracer.TraceInfo("Interrupt Mode: Interrupt mode finished! The match position is " +
                                         _matchPosition);

                        ProcessExitInterruptMode(registerData);

                        multipleBundles = registerData;
                        return;
                    }

                    //中断模式缓存期间不向外发送数据
                    return;
                }
                //未处于中断拼图模式的时候，缓存配准数据
                CacheRegistData(bundle);
                singleBundle = bundle;
            }
        }

        public void FireEventHandler()
        {
            var registerData = _registerBundles.Skip(_matchPosition).ToList();

            Tracer.TraceInfo("Interrupt Mode: Interrupt mode break forced! The match position is " +
                             _matchPosition);
            ProcessExitInterruptMode(registerData);
            if (registerData.Count > 0)
            {
                if (LeaveEventHandler != null)
                {
                    LeaveEventHandler(registerData);
                }
            }
        }

        private void CalMaxRelatedCoefAndPosition()
        {
            //如果缓存的数据跟模板的数据量一致
            if (_registerBundles.Count > _startCalCols + _templateDataCacheCount)
            {
                //每增加一线数据，取配准数据末尾TemplateLineDataBundlesCount(4线)数据，用于和模板数据计算相关性，
                //相关性越大的配准数据越与模板数据匹配
                var registrationLineDataBundles =
                    _registerBundles.GetRange(
                        _registerBundles.Count - _templateDataCacheCount, _templateDataCacheCount);

                //计算相关系数
                double relatedCoefficient = CalRelatedCoefficient(_templateBundles.ToList(), registrationLineDataBundles,
                    _templateDataSigma);

                if (relatedCoefficient > _maxRelatedCoefficient)
                {
                    //记录最大相关系数及位置
                    _maxRelatedCoefficient = relatedCoefficient;

                    //配准后要输出的数据是从最匹配的四线数据的下一线开始的，因此配准的位置为配准数据最后一线数据的下一线
                    _matchPosition = _registerBundles.Count;
                }
            }
        }

        /// <summary>
        /// 处理退出中断模式
        /// </summary>
        private void ProcessExitInterruptMode(IEnumerable<RawScanlineDataBundle> bundles)
        {
            lock (_interruptLock)
            {
                //清空数据的缓存
                ClearCaches();

                foreach (var bundle in bundles)
                {
                    CacheRegistData(bundle);
                }
                Tracer.TraceInfo($"[InterruptTailAfter] ProcessExitInterruptMode - exit interrrupt status!");
                //退出中断模式
                InterruptState = InterruptMode.NotInInterrrupt;
            }
        }


        private void ProcessExitInterruptMode()
        {
            lock (_interruptLock)
            {
                //清空数据的缓存
                ClearCaches();
                Tracer.TraceInfo($"[InterruptTailAfter] ProcessExitInterruptMode - exit interrrupt status!");
                //退出中断模式
                InterruptState = InterruptMode.NotInInterrrupt;
            }
        }

        public void ClearCaches()
        {
            //清空数据的缓存
            _registerBundles.Clear();

            //清空记录的最大相关系数
            _maxRelatedCoefficient = 0;

            //重置记录匹配位置的字段
            _matchPosition = 0;
        }


        /// <summary>
        /// 处理正常模式下的数据，用于缓存模板数据
        /// </summary>
        /// <param name="bundle"></param>
        private void CacheRegistData(RawScanlineDataBundle bundle)
        {
            // 先缓存数据。new一个新实例，因为下面的双视角配准中涉及到某个视角数据的修改。todo：这里会不会影响效率
            var lineDataBundle = new RawScanlineDataBundle(bundle.View1RawData, bundle.View2RawData);

            //将数据缓存到模板数据队列中
            _templateBundles.Enqueue(lineDataBundle);

            //只缓存4线数据，超过四线数据将队列的第一个数据删除
            if (_templateBundles.Count > _templateDataCacheCount)
            {
                _templateBundles.Dequeue();
            }
        }

        #region 计算自相关系数

        /// <summary>
        /// 计算数据平方和
        /// </summary>
        /// <param name="lineDataBundles"></param>
        /// <param name="toCalViewIndex"></param>
        /// <returns></returns>
        private double CalQuadraticSum(IEnumerable<RawScanlineDataBundle> lineDataBundles)
        {
            if (_isDualView)
            {
                return lineDataBundles.Select(
                lineDataBundle =>
                    lineDataBundle.View1RawData)
                .Sum(viewData => viewData.Low.Sum(t => (double)t * t)) +
                lineDataBundles.Select(
                lineDataBundle =>
                    lineDataBundle.View2RawData)
                .Sum(viewData => viewData.Low.Sum(t => (double)t * t));
            }
            return lineDataBundles.Select(
                lineDataBundle =>
                    lineDataBundle.View1RawData)
                .Sum(viewData => viewData.Low.Sum(t => (double)t * t));
        }

        /// <summary>
        /// 计算模板数据与配准数据的协方差
        /// </summary>
        /// <returns></returns>
        private double CalCovariation(List<RawScanlineDataBundle> templateLineDataBundles, List<RawScanlineDataBundle> registrationLineDataBundles)
        {
            double covariation = 0;

            for (var i = 0; i < templateLineDataBundles.Count; i++)
            {
                //计算对应位置的乘积之和
                covariation +=
                    templateLineDataBundles[i].View1RawData.Low.Select((t, j) => (double)t * registrationLineDataBundles[i].View1RawData.Low[j]).Sum();
                if (_isDualView)
                {
                    covariation +=
                    templateLineDataBundles[i].View2RawData.Low.Select((t, j) => (double)t * registrationLineDataBundles[i].View2RawData.Low[j]).Sum();
                }
            }

            return covariation;
        }

        /// <summary>
        /// 计算相关系数
        /// </summary>
        /// <param name="templateBundles"></param>
        /// <param name="registrationBundles"></param>
        /// <param name="sigmaTemplateData"></param>
        /// <param name="toCalViewIndex"></param>
        /// <returns></returns>
        private double CalRelatedCoefficient(List<RawScanlineDataBundle> templateBundles,
            List<RawScanlineDataBundle> registrationBundles, double sigmaTemplateData)
        {
            double registrationDataSigma = 0.0, covariation = 0.0;

            try
            {
                Parallel.Invoke(
                    () =>
                    {
                        //计算配准用的TemplateLineDataBundlesCount(4线)数据平方和
                        registrationDataSigma = CalQuadraticSum(registrationBundles);
                    },
                    () =>
                    {
                        //计算配准数据与模板数据对应位置交差相乘的和
                        covariation = CalCovariation(templateBundles, registrationBundles);
                    });
            }
            catch (Exception e)
            {
                Tracer.TraceException(e, "occured when calRelatedConefficient in InterruptAndMatch Service!");
            }

            //如果异常发生可能出现0的情况
            if (Math.Abs(sigmaTemplateData) < 1e-6 || Math.Abs(registrationDataSigma) < 1e-6)
            {
                return 0.0;
            }

            //计算相关系数
            return covariation/Math.Sqrt(registrationDataSigma*sigmaTemplateData);
        }

        #endregion
    }
}
