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
    /// KeySwitchWidget.xaml 的交互逻辑
    /// </summary>
    public partial class KeySwitchWidget 
    {
        public KeySwitchWidget()
        {
            InitializeComponent();
        }

        /// <summary>
        /// get or set light on state.
        /// </summary>
        public bool IsSwitchOn
        {
            get { return (bool)GetValue(IsSwitchOnProperty); }
            set { SetValue(IsSwitchOnProperty, value); }
        }

        private static readonly DependencyProperty IsSwitchOnProperty = DependencyProperty.Register(
            "IsSwitchOn",
            typeof(bool),
            typeof(KeySwitchWidget),
            new FrameworkPropertyMetadata(false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                new PropertyChangedCallback(OnPropertyChanged))
            );

        private static void OnPropertyChanged(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs args)
        {
            var widget = dependencyObject as KeySwitchWidget;
            if (widget != null)
            {
                if (args.Property == IsSwitchOnProperty)
                {
                    widget.OnIsSwitchOnPropertyChanged(args);
                }
            }
        }

        private void OnIsSwitchOnPropertyChanged(DependencyPropertyChangedEventArgs args)
        {
            var isOn = (bool)args.NewValue;
            if (isOn)
            {
                base.RaiseEvent(new RoutedEventArgs(SwitchOnEvent, this));
            }
            else
            {
                base.RaiseEvent(new RoutedEventArgs(SwitchOffEvent, this));
            }
        }

        public static readonly RoutedEvent SwitchOnEvent = EventManager.RegisterRoutedEvent("SwitchOn",
            RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(KeySwitchWidget));

        public event RoutedEventHandler SwitchOn
        {
            add
            {
                base.AddHandler(KeySwitchWidget.SwitchOnEvent, value);
            }
            remove
            {
                base.RemoveHandler(KeySwitchWidget.SwitchOnEvent, value);
            }
        }

        public static readonly RoutedEvent SwitchOffEvent = EventManager.RegisterRoutedEvent("SwitchOff",
            RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(KeySwitchWidget));

        public event RoutedEventHandler SwitchOff
        {
            add
            {
                base.AddHandler(KeySwitchWidget.SwitchOffEvent, value);
            }
            remove
            {
                base.RemoveHandler(KeySwitchWidget.SwitchOffEvent, value);
            }
        }
    }
}
