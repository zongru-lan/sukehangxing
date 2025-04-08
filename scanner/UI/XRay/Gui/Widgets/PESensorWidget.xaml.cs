using System.Windows;
using System.Windows.Media;

namespace UI.XRay.Gui.Widgets
{
    /// <summary>
    /// 光障的状态，共分为三种：未使用（发射端未发射光线），已发射并且光路正常，光路被阻隔
    /// </summary>
    public enum PESensorState
    {
        /// <summary>
        /// Tx is powered off. The PESensor is not used.
        /// </summary>
        PowerOff = 0,

        /// <summary>
        /// Tx is powered on, and Rx can get Tx signal. No object is blocking.
        /// </summary>
        Ready = 1,

        /// <summary>
        /// Tx is powered on, and Rx detect an object.
        /// </summary>
        DetectObject = 2,

        Broken = 3
    }

    /// <summary>
    /// 表示一个用于显示光电开关状态的窗体小部件，将来将作为通用的窗体部件。
    /// </summary>
    public partial class PESensorWidget 
    {
        /// <summary>
        /// 注册依赖项事件：SensorsStateProperty
        /// </summary>
        private static readonly DependencyProperty SensorsStateProperty = DependencyProperty.Register(
            "SensorState",
            typeof (PESensorState),
            typeof (PESensorWidget),
            new FrameworkPropertyMetadata(PESensorState.PowerOff,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                new PropertyChangedCallback(OnSensorsStatePropertyChanged))
            );

        /// <summary>
        /// 光电开关打开时的画刷
        /// </summary>
        private static readonly Brush ActiveBrush = Brushes.DarkGoldenrod;

        /// <summary>
        /// 光电开关被关闭时的画刷
        /// </summary>
        private static readonly Brush InActiveBrush = Brushes.Transparent;

        /// <summary>
        /// Get or set light sensor state.
        /// </summary>
        public PESensorState SensorState
        {
            get { return (PESensorState) GetValue(SensorsStateProperty); }
            set { SetValue(SensorsStateProperty, value); }
        }

        private static void OnSensorsStatePropertyChanged(DependencyObject dependencyObject, 
            DependencyPropertyChangedEventArgs args)
        {
            var widget = dependencyObject as PESensorWidget;
            if (widget != null)
            {
                widget.OnSensorsStateChanged((PESensorState)args.OldValue, (PESensorState)args.NewValue);
            }
        }

        /// <summary>
        /// 事件处理：光电开关的状态发生变化
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        private void OnSensorsStateChanged(PESensorState oldValue, PESensorState newValue)
        {
            if (newValue == PESensorState.PowerOff)
            {
                // 光障未发射
                SetOffState();
            }
            else if (newValue == PESensorState.Ready)
            {
                // 光障发射，无物体遮挡
                RayBeam.Visibility = Visibility.Visible;
                HalfBeam.Visibility = Visibility.Hidden;
                BlockingObject.Visibility = Visibility.Hidden;
                Emitter.Fill = ActiveBrush;
                Receiver.Fill = ActiveBrush;
            }
            else
            {
                // 光障发射，且有物体遮挡
                RayBeam.Visibility = Visibility.Hidden;
                HalfBeam.Visibility = Visibility.Visible;
                BlockingObject.Visibility = Visibility.Visible;
                Emitter.Fill = ActiveBrush;
                Receiver.Fill = ActiveBrush;
            }
        }

        /// <summary>
        /// 设置光障关闭时的状态
        /// </summary>
        private void SetOffState()
        {
            //  By Default Light sensor is off, so hide all indicaiting elements.
            RayBeam.Visibility = Visibility.Hidden;
            HalfBeam.Visibility = Visibility.Hidden;
            BlockingObject.Visibility = Visibility.Hidden;
            Emitter.Fill = InActiveBrush;
            Receiver.Fill = InActiveBrush;
        }

        public PESensorWidget()
        {
            InitializeComponent();

            SetOffState();
        }
    }
}
