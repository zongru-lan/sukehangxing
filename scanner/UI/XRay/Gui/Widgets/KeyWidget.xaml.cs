using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace UI.XRay.Gui.Widgets
{
    /// <summary>
    /// 表示一个按键
    /// </summary>
    public partial class KeyWidget
    {
        public KeyWidget()
        {
            InitializeComponent();
            KeyBkgRectangle.Fill = Brushes.SeaGreen;
        }

        private static readonly DependencyProperty KeyStateProperty = DependencyProperty.Register(
            "KeyStates",
            typeof(KeyStates),
            typeof(KeyWidget),
            new FrameworkPropertyMetadata(KeyStates.None,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                new PropertyChangedCallback(OnPropertyChanged))
            );

        private static readonly DependencyProperty KeyTextProperty = DependencyProperty.Register(
            "KeyText",
            typeof(string),
            typeof(KeyWidget),
            new FrameworkPropertyMetadata("",
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                new PropertyChangedCallback(OnPropertyChanged))
            );

        private static void OnPropertyChanged(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs args)
        {
            var widget = dependencyObject as KeyWidget;
            if (widget != null)
            {
                if (args.Property == KeyStateProperty)
                {
                    widget.OnKeyStatePropertyChanged(args);
                }
                else if (args.Property == KeyTextProperty)
                {
                    widget.OnKeyTextPropertyChanged(args);
                }
            }
        }

        private void OnKeyTextPropertyChanged(DependencyPropertyChangedEventArgs args)
        {
            var str = args.NewValue as string;
            KeyTextBlock.Text = str;
        }

        private void OnKeyStatePropertyChanged(DependencyPropertyChangedEventArgs args)
        {
            var state = (KeyStates)args.NewValue;
            if (state == KeyStates.Down || state == KeyStates.Toggled)
            {
                KeyBkgRectangle.Fill = Brushes.Crimson;
            }
            else if (state == KeyStates.None)
            {
                KeyBkgRectangle.Fill = Brushes.SeaGreen;
            }
        }

        /// <summary>
        /// Get or set light sensor state.
        /// </summary>
        public KeyStates KeyStates
        {
            get { return (KeyStates)GetValue(KeyStateProperty); }
            set { SetValue(KeyStateProperty, value); }
        }

        /// <summary>
        /// get or set key text showing.
        /// </summary>
        public string KeyText
        {
            get { return (string)GetValue(KeyTextProperty); }
            set { SetValue(KeyTextProperty, value); }
        }
    }
}
