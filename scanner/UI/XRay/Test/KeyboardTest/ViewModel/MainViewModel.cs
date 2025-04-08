using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using UI.XRay.Parts.Keyboard;

namespace UI.XRay.Input.KeyboardTest.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        public RelayCommand<KeyEventArgs> PreviewKeyDownCommand { get; set; }

        public RelayCommand<KeyEventArgs> PreviewKeyUpCommand { get; set; }

        public RelayCommand BeepCommand { get; set; }

        #region 按键状态绑定属性

        public KeyStates InverseKeyStates
        {
            get { return _inverseKeyStates; }
            set
            {
                _inverseKeyStates = value;
                RaisePropertyChanged();
            }
        }


        public KeyStates BWKeyStates
        {
            get { return _bwKeyStates; }
            set
            {
                _bwKeyStates = value;
                RaisePropertyChanged();
            }
        }


        public KeyStates IncreaseAbsorbKeyStates
        {
            get { return _increaseAbsorbKeyStates; }
            set
            {
                _increaseAbsorbKeyStates = value;
                RaisePropertyChanged();
            }
        }

        public KeyStates DecreaseAbsorbKeyStates
        {
            get { return _decreaseAbsorbKeyStates; }
            set
            {
                _decreaseAbsorbKeyStates = value; 
                RaisePropertyChanged();
            }
        }

        public KeyStates HpKeyStates
        {
            get { return _hpKeyStates; }
            set
            {
                _hpKeyStates = value; 
                RaisePropertyChanged();
            }
        }

        public KeyStates SenKeyStates
        {
            get { return _senKeyStates; }
            set
            {
                _senKeyStates = value; 
                RaisePropertyChanged();
            }
        }

        public KeyStates OsKeyStates
        {
            get { return _osKeyStates; }
            set
            {
                _osKeyStates = value;
                RaisePropertyChanged();
            }
        }

        public KeyStates MsKeyStates
        {
            get { return _msKeyStates; }
            set
            {
                _msKeyStates = value; 
                RaisePropertyChanged();
            }
        }

        public KeyStates EscKeyStates
        {
            get { return _escKeyStates; }
            set
            {
                _escKeyStates = value; 
                RaisePropertyChanged();
            }
        }

        public KeyStates Z789KeyStates
        {
            get { return _z789KeyStates; }
            set
            {
                _z789KeyStates = value;
                RaisePropertyChanged();
            }
        }

        public KeyStates EdgeEnhKeyStates
        {
            get { return _edgeEnhKeyStates; }
            set
            {
                _edgeEnhKeyStates = value; 
                RaisePropertyChanged();
            }
        }

        public KeyStates ShiftKeyStates
        {
            get { return _shiftKeyStates; }
            set
            {
                _shiftKeyStates = value; 
                RaisePropertyChanged();
            }
        }

        public KeyStates ContinuousKeyStates
        {
            get { return _continuousKeyStates; }
            set { _continuousKeyStates = value; RaisePropertyChanged(); }
        }

        public KeyStates AutoKeyStates
        {
            get { return _autoKeyStates; }
            set { _autoKeyStates = value; RaisePropertyChanged(); }
        }

        public KeyStates MarkKeyStates
        {
            get { return _markKeyStates; }
            set { _markKeyStates = value; RaisePropertyChanged(); }
        }

        public KeyStates ImsKeyStates
        {
            get { return _imsKeyStates; }
            set { _imsKeyStates = value; RaisePropertyChanged(); }
        }

        public KeyStates MenuKeyStates
        {
            get { return _menuKeyStates; }
            set { _menuKeyStates = value; RaisePropertyChanged(); }
        }

        public KeyStates FlipKeyStates
        {
            get { return _flipKeyStates; }
            set { _flipKeyStates = value; RaisePropertyChanged(); }
        }

        public KeyStates GrayScaleTransformKeyStates
        {
            get { return _grayScaleTransformKeyStates; }
            set { _grayScaleTransformKeyStates = value; RaisePropertyChanged(); }
        }

        public KeyStates Zoom1XKeyStates
        {
            get { return _zoom1XKeyStates; }
            set { _zoom1XKeyStates = value; RaisePropertyChanged(); }
        }

        public KeyStates ZoomInKeyStates
        {
            get { return _zoomInKeyStates; }
            set { _zoomInKeyStates = value; RaisePropertyChanged(); }
        }

        public KeyStates ZoomOutKeyStates
        {
            get { return _zoomOutKeyStates; }
            set { _zoomOutKeyStates = value; RaisePropertyChanged(); }
        }

        public KeyStates F1KeyStates
        {
            get { return _f1KeyStates; }
            set { _f1KeyStates = value; RaisePropertyChanged(); }
        }

        public KeyStates F2KeyStates
        {
            get { return _f2KeyStates; }
            set { _f2KeyStates = value; RaisePropertyChanged(); }
        }

        public KeyStates F3KeyStates
        {
            get { return _f3KeyStates; }
            set { _f3KeyStates = value; RaisePropertyChanged(); }
        }

        public KeyStates ConveyorBackKeyState
        {
            get { return _conveyorBackKeyState; }
            set { _conveyorBackKeyState = value; RaisePropertyChanged(); }
        }

        public KeyStates ConveyorForwKeyStates
        {
            get { return _conveyorForwKeyStates; }
            set { _conveyorForwKeyStates = value; RaisePropertyChanged(); }
        }

        public KeyStates ConveyorStopKeyStates
        {
            get { return _conveyorStopKeyStates; }
            set { _conveyorStopKeyStates = value; RaisePropertyChanged(); }
        }

        public KeyStates UpKeyStates
        {
            get { return _upKeyStates; }
            set { _upKeyStates = value; RaisePropertyChanged(); }
        }

        public KeyStates DownKeyStates
        {
            get { return _downKeyStates; }
            set { _downKeyStates = value; RaisePropertyChanged(); }
        }

        public KeyStates LeftKeyStates
        {
            get { return _leftKeyStates; }
            set { _leftKeyStates = value; RaisePropertyChanged(); }
        }

        public KeyStates RightKeyStates
        {
            get { return _rightKeyStates; }
            set { _rightKeyStates = value; RaisePropertyChanged(); }
        }

        public KeyStates ShutdownKeyStates
        {
            get { return _shutdownKeyStates; }
            set { _shutdownKeyStates = value; RaisePropertyChanged(); }
        }

        public bool IsKeySwitchOn
        {
            get { return _isKeySwitchOn; }
            set { _isKeySwitchOn = value; RaisePropertyChanged(); }
        }

        public bool IsKeySwitchLedOn
        {
            get { return _isKeySwitchLedOn; }
            set { _isKeySwitchLedOn = value; RaisePropertyChanged(); }
        }

        public bool IsXRay1LedOn
        {
            get { return _isXRay1LedOn; }
            set { _isXRay1LedOn = value; RaisePropertyChanged(); }
        }

        public bool IsXRay2LedOn
        {
            get { return _isXRay2LedOn; }
            set { _isXRay2LedOn = value; RaisePropertyChanged(); }
        }

        public bool IsPowerLedOn
        {
            get { return _isPowerLedOn; }
            set { _isPowerLedOn = value; RaisePropertyChanged(); }
        }

        public KeyStates SaveKeyStates
        {
            get { return _saveKeyStates; }
            set { _saveKeyStates = value;
                RaisePropertyChanged();
            }
        }

        private KeyStates _saveKeyStates;

        private KeyStates _shutdownKeyStates;

        private KeyStates _inverseKeyStates = KeyStates.None;

        private KeyStates _bwKeyStates;

        private KeyStates _increaseAbsorbKeyStates;

        private KeyStates _shiftKeyStates;

        private KeyStates _edgeEnhKeyStates;

        private KeyStates _z789KeyStates;

        private KeyStates _escKeyStates;

        private KeyStates _msKeyStates;

        private KeyStates _osKeyStates;

        private KeyStates _senKeyStates;

        private KeyStates _decreaseAbsorbKeyStates;

        private KeyStates _hpKeyStates;

        private KeyStates _continuousKeyStates;

        private KeyStates _autoKeyStates;

        private KeyStates _markKeyStates;

        private KeyStates _imsKeyStates;

        private KeyStates _menuKeyStates;

        private KeyStates _flipKeyStates;

        private KeyStates _grayScaleTransformKeyStates;

        private KeyStates _zoom1XKeyStates;

        private KeyStates _zoomInKeyStates;

        private KeyStates _zoomOutKeyStates;

        private KeyStates _f1KeyStates;

        private KeyStates _f2KeyStates;

        private KeyStates _f3KeyStates;

        private KeyStates _conveyorBackKeyState;

        private KeyStates _conveyorForwKeyStates;

        private KeyStates _conveyorStopKeyStates;

        private KeyStates _upKeyStates;

        private KeyStates _downKeyStates;

        private KeyStates _leftKeyStates;

        private KeyStates _rightKeyStates;

        private bool _isKeySwitchOn;

        private bool _isKeySwitchLedOn;

        private bool _isXRay1LedOn;

        private bool _isXRay2LedOn;

        private bool _isPowerLedOn;

        #endregion

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            ////if (IsInDesignMode)
            ////{
            ////    // Code runs in Blend --> create design time data.
            ////}
            ////else
            ////{
            ////    // Code runs "for real"
            ////}
            UIScannerKeyboard1.Open("");

            PreviewKeyDownCommand = new RelayCommand<KeyEventArgs>(PreviewKeyDownCommandExecute);
            PreviewKeyUpCommand = new RelayCommand<KeyEventArgs>(PreviewKeyUpCommandExecute);
            BeepCommand = new RelayCommand(BeepCommandExecute);
        }

        private void PreviewKeyDownCommandExecute(KeyEventArgs args)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
                UpdateKeyStates(args.Key, KeyStates.Down));
            
        }

        private void PreviewKeyUpCommandExecute(KeyEventArgs args)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
                UpdateKeyStates(args.Key, KeyStates.None));
        }

        private void BeepCommandExecute()
        {
            // 当用户按下了按键后，发出三声间隔300毫秒的蜂鸣
            UIScannerKeyboard1.StartBeep(TimeSpan.FromMilliseconds(300), TimeSpan.FromMilliseconds(300), 3);
        }

        /// <summary>
        /// 更新按键的状态
        /// </summary>
        /// <param name="key"></param>
        /// <param name="state"></param>
        private void UpdateKeyStates(Key key, KeyStates state)
        {
            switch (key)
            {
                case UIScannerKeyboard1.InverseImageKey:
                    InverseKeyStates = state;
                    break;
                case UIScannerKeyboard1.IncreaseAbsorbKey:
                    IncreaseAbsorbKeyStates = state;
                    break;
                case UIScannerKeyboard1.DecreaseAbsorbKey:
                    DecreaseAbsorbKeyStates = state;
                    break;
                case UIScannerKeyboard1.SaveKey:
                     SaveKeyStates= state;
                    break;
                case UIScannerKeyboard1.ShiftKey:
                    ShiftKeyStates = state;
                    break;
                case UIScannerKeyboard1.HighPenetrateKey:
                    HpKeyStates = state;
                    break;
                case UIScannerKeyboard1.ContinuousScanKey:
                    ContinuousKeyStates = state;
                    break;
                case UIScannerKeyboard1.AutoKey:
                    AutoKeyStates = state;
                    break;
                case UIScannerKeyboard1.MarkKey:
                    MarkKeyStates = state;
                    break;
                case UIScannerKeyboard1.ImsKey:
                    ImsKeyStates = state;
                    break;
                case UIScannerKeyboard1.MenuKey:
                    MenuKeyStates = state;
                    break;
                case UIScannerKeyboard1.BlackWhiteKey:
                    BWKeyStates = state;
                    break;
                case UIScannerKeyboard1.OsKey:
                    OsKeyStates = state;
                    break;
                case UIScannerKeyboard1.MsKey:
                    MsKeyStates = state;
                    break;
                case UIScannerKeyboard1.SenKey:
                    SenKeyStates = state;
                    break;
                case UIScannerKeyboard1.EscKey:
                    EscKeyStates = state;
                    break;
                case UIScannerKeyboard1.Z789Key:
                    Z789KeyStates = state;
                    break;
                case UIScannerKeyboard1.VFlipKey:
                    FlipKeyStates = state;
                    break;
                case UIScannerKeyboard1.DynamicGSTKey:
                    GrayScaleTransformKeyStates = state;
                    break;
                case UIScannerKeyboard1.Zoom1X:
                    Zoom1XKeyStates = state;
                    break;
                case UIScannerKeyboard1.ZoomOutKey:
                    ZoomOutKeyStates = state;
                    break;
                case UIScannerKeyboard1.ZoomInKey:
                    ZoomInKeyStates = state;
                    break;

                case UIScannerKeyboard1.F1Key:
                    F1KeyStates = state;
                    break;
                case UIScannerKeyboard1.F2Key:
                    F2KeyStates = state;
                    break;
                case UIScannerKeyboard1.F3Key:
                    F3KeyStates = state;
                    break;

                case UIScannerKeyboard1.ConveyorBackwardKey:
                    ConveyorBackKeyState = state;
                    break;
                case UIScannerKeyboard1.ConveyorForwardKey:
                    ConveyorForwKeyStates = state;
                    break;
                case UIScannerKeyboard1.ConveyorStopKey:
                    ConveyorStopKeyStates = state;
                    break;

                case UIScannerKeyboard1.UpKey:
                    UpKeyStates = state;
                    break;
                case UIScannerKeyboard1.DownKey:
                    DownKeyStates = state;
                    break;
                case UIScannerKeyboard1.LeftKey:
                    LeftKeyStates = state;
                    break;
                case UIScannerKeyboard1.RightKey:
                    RightKeyStates = state;
                    break;

                    // 以下仅用于测试钥匙开关和几个LED灯
                case Key.F7:
                    IsKeySwitchOn = state == KeyStates.Down;
                    break;
                case Key.F8:
                    IsKeySwitchLedOn = state == KeyStates.Down;
                    UIScannerKeyboard1.IsKeySwitchIndicatorOn = IsKeySwitchLedOn;
                    break;
                case Key.F9:
                    IsPowerLedOn = state == KeyStates.Down;
                    UIScannerKeyboard1.IsPowerIndicatorOn = IsPowerLedOn;
                    break;
                case Key.F11:
                    IsXRay1LedOn = state == KeyStates.Down;
                    UIScannerKeyboard1.IsXRayIndicator1On = IsXRay1LedOn;
                    break;
                case Key.F12:
                    IsXRay2LedOn = state == KeyStates.Down;
                    UIScannerKeyboard1.IsXRayIndicator2On = IsXRay2LedOn;
                    break;

                    // 使用计算机键盘中的逗号键，用于测试串口键盘的蜂鸣器
                case Key.OemComma:
                    if (state == KeyStates.Down)
                    {
                        BeepCommandExecute();
                    }
                    break;
            }
        }
    }
}