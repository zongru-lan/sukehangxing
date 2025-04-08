using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UI.XRay.Gui.Widgets
{
    /// <summary>
    /// 表示一个指示灯控件
    /// </summary>
    public partial class IndicatorLightWidget : UserControl
    {
        private Brush _lightOnBrush = Brushes.DeepPink;

        private Brush _lightOffBrush = Brushes.Black;

        public IndicatorLightWidget()
        {
            InitializeComponent();
        }

        /// <summary>
        /// get or set color
        /// </summary>
        public Brush LightOnBrush
        {
            get { return (Brush)GetValue(LightOnBrushProperty); }
            set { SetValue(LightOnBrushProperty, value); }
        }

        public static readonly DependencyProperty LightOnBrushProperty = DependencyProperty.Register(
            "LightOnBrush",
            typeof(Brush),
            typeof(IndicatorLightWidget),
            new FrameworkPropertyMetadata(Brushes.DeepPink,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                new PropertyChangedCallback(OnPropertyChanged))
            );


        /// <summary>
        /// set or get color
        /// </summary>
        public Brush LightOffBrush
        {
            get { return (Brush)GetValue(LightOffBrushProperty); }
            set { SetValue(LightOffBrushProperty, value); }
        }

        public static readonly DependencyProperty LightOffBrushProperty = DependencyProperty.Register(
            "LightOffBrush",
            typeof(Brush),
            typeof(IndicatorLightWidget),
            new FrameworkPropertyMetadata(Brushes.Black,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                new PropertyChangedCallback(OnPropertyChanged))
            );



        /// <summary>
        /// get or set light on state.
        /// </summary>
        public bool IsLightOn
        {
            get { return (bool)GetValue(IsLightOnProperty); }
            set { SetValue(IsLightOnProperty, value); }
        }

        private static readonly DependencyProperty IsLightOnProperty = DependencyProperty.Register(
            "IsLightOn",
            typeof(bool),
            typeof(IndicatorLightWidget),
            new FrameworkPropertyMetadata(false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                new PropertyChangedCallback(OnPropertyChanged))
            );

        private static void OnPropertyChanged(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs args)
        {
            var widget = dependencyObject as IndicatorLightWidget;
            if (widget != null)
            {
                if (args.Property == IsLightOnProperty)
                {
                    widget.OnIsLightOnPropertyChanged(args);
                }
                else if (args.Property == LightOnBrushProperty)
                {
                    widget.OnIsLightOnBrushPropertyChanged(args);
                }
                else if (args.Property == LightOffBrushProperty)
                {
                    widget.OnIsLightOffBrushPropertyChanged(args);
                }
            }
        }

        private void OnIsLightOnPropertyChanged(DependencyPropertyChangedEventArgs args)
        {
            var isOn = (bool)args.NewValue;
            if (isOn)
            {
                LightEllipse.Fill = _lightOnBrush;
            }
            else
            {
                LightEllipse.Fill = _lightOffBrush;
            }
        }
        private void OnIsLightOnBrushPropertyChanged(DependencyPropertyChangedEventArgs args)
        {
            var brush = (Brush)args.NewValue;
            _lightOnBrush = brush;
        }
        private void OnIsLightOffBrushPropertyChanged(DependencyPropertyChangedEventArgs args)
        {
            var brush = (Brush)args.NewValue;
            _lightOffBrush = brush;
        }
    }
}
