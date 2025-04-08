using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Parts.Keyboard;
using UI.XRay.Security.Scanner.SettingViews.Device.Pages;
using UI.XRay.Flows.Services;
using System.Threading;
using static System.Windows.Forms.AxHost;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Device
{
    public class KeyboardPageViewModel : PageViewModelBase
    {
        public RelayCommand IsDirectionKeyChangedEventCommand { get; private set; }

        public RelayCommand IsBiDirectionScanEventCommand { get; private set; }

        public RelayCommand IsEnableContinuousScanEventCommand { get; private set; }

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
            set
            {
                _saveKeyStates = value;
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

        private bool _isConveyorKeyReversed;

        /// <summary>
        /// 是否进行电机建翻转
        /// </summary>
        public bool IsConveyorKeyReversed
        {
            get { return _isConveyorKeyReversed; }
            set
            {
                _isConveyorKeyReversed = value;
                RaisePropertyChanged();
            }
        }

        private bool _isKeyboardReversed;
        public bool IsKeyboardReversed
        {
            get { return _isKeyboardReversed; }
            set { _isKeyboardReversed = value; RaisePropertyChanged(); }
        }
        


        private bool _enableBidirectionScan;

        public bool EnableBidirectionScan
        {
            get { return _enableBidirectionScan; }
            set { _enableBidirectionScan = value;RaisePropertyChanged(); }
        }

        private bool _isEnableContinuousScan;
        public bool IsEnableContinuousScan
        {
            get { return _isEnableContinuousScan; }
            set { _isEnableContinuousScan = value; RaisePropertyChanged(); }
        }

        public KeyboardPageViewModel()
        {
            try
            {
                IsDirectionKeyChangedEventCommand = new RelayCommand(IsDirectionKeyChangedEventCommandExecute);
                IsBiDirectionScanEventCommand = new RelayCommand(IsBiDirectionScanEventCommandExecute);
                IsEnableContinuousScanEventCommand = new RelayCommand(IsEnableContinuousScanEventCommandExecute);
                bool isConveyorKeyReversed;
                if (ScannerConfig.Read(ConfigPath.KeyboardIsConveyorKeyReversed, out isConveyorKeyReversed))
                {
                    IsConveyorKeyReversed = isConveyorKeyReversed;
                }

                bool isKeyboardReversed;
                if (ScannerConfig.Read(ConfigPath.KeyboardIsKeyboardReversed, out isKeyboardReversed))
                {
                    IsKeyboardReversed = isKeyboardReversed;
                }

                bool enableBidirectionScan;
                if (ScannerConfig.Read(ConfigPath.MachineBiDirectionScan, out enableBidirectionScan))
                {
                    EnableBidirectionScan = enableBidirectionScan;
                }

                bool enableContinuousScan;
                if (ScannerConfig.Read(ConfigPath.MachineContinuousScan, out enableContinuousScan))
                {
                    IsEnableContinuousScan = enableContinuousScan;
                }

                BeepCommand = new RelayCommand(BeepCommandExecute);
                UIScannerKeyboard1.SetWitchInputStates(false);
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        public override void OnPreviewKeyDown(KeyEventArgs args)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
                UpdateKeyStates(args, KeyStates.Down));

            base.OnPreviewKeyDown(args);
        }

        public override void OnPreviewKeyUp(KeyEventArgs args)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
                UpdateKeyStates(args, KeyStates.None));

            base.OnPreviewKeyUp(args);
        }

        private void BeepCommandExecute()
        {
            // if (UIScannerKeyboard1._keyboard1 != null)



            //for (int i = 0; i < 3; i++)
            //{
            //    try
            //    {
            //        ControlService.ServicePart.Beep(true);
            //        Thread.Sleep(TimeSpan.FromMilliseconds(100));
            //        ControlService.ServicePart.Beep(false);
            //        Thread.Sleep(TimeSpan.FromMilliseconds(100));
            //    }
            //    catch (Exception e)
            //    {
            //    Tracer.TraceException(e, " Occured when beep for emergengy button down");
            //    }
            //}
            ScannerKeyboardPart.Keyboard.StartBeep(TimeSpan.FromMilliseconds(300), TimeSpan.FromMilliseconds(300), 3);
            Tracer.TraceInfo("键盘蜂鸣器报警 ");
            Task.Run(() =>
            {
                var times = 3;

                while (times-- > 0)
                {
                    try
                    {
                        ControlService.ServicePart.Beep(true);

                        Thread.Sleep(TimeSpan.FromMilliseconds(300));

                        ControlService.ServicePart.Beep(false);

                        Thread.Sleep(TimeSpan.FromMilliseconds(300));
                    }
                    catch (Exception e)
                    {
                        Tracer.TraceException(e, " Occured when beep ");
                    }
                }
            }

           );
            //if (ScannerKeyboardPart.Keyboard.KeyboardType == "USB Serial Port")
            //{
            //    // 当用户按下了按键后，发出三声间隔300毫秒的蜂鸣
            //    ScannerKeyboardPart.Keyboard.StartBeep(TimeSpan.FromMilliseconds(300), TimeSpan.FromMilliseconds(300), 3);
            //    Tracer.TraceInfo("键盘蜂鸣器报警 ");
            //}
            //else
            //{
            //    Task.Run(() =>
            //{
            //    var times = 3;

            //    while (times-- > 0)
            //    {
            //        try
            //        {
            //            ControlService.ServicePart.Beep(true);

            //            Thread.Sleep(TimeSpan.FromMilliseconds(300));

            //            ControlService.ServicePart.Beep(false);

            //            Thread.Sleep(TimeSpan.FromMilliseconds(300));
            //        }
            //        catch (Exception e)
            //        {
            //            Tracer.TraceException(e, " Occured when beep ");
            //        }
            //    }
            //}

            //);
            //    Tracer.TraceInfo("内部蜂鸣器报警 ");

            //}
        }

        /// <summary>
        /// 更新按键的状态
        /// </summary>
        private void UpdateKeyStates(KeyEventArgs args, KeyStates state)
        {
            var key = args.Key;
            switch (key)
            {
                case UIScannerKeyboard1.InverseImageKey:
                    InverseKeyStates = state;
                    //args.Handled = true;
                    break;
                case UIScannerKeyboard1.IncreaseAbsorbKey:
                    IncreaseAbsorbKeyStates = state;
                    args.Handled = true;
                    break;
                case UIScannerKeyboard1.DecreaseAbsorbKey:
                    DecreaseAbsorbKeyStates = state;
                    args.Handled = true;
                    break;
                case UIScannerKeyboard1.SaveKey:
                    SaveKeyStates = state;
                    args.Handled = true;
                    break;
                case UIScannerKeyboard1.ShiftKey:
                    ShiftKeyStates = state;
                    args.Handled = true;
                    break;
                case UIScannerKeyboard1.HighPenetrateKey:
                    HpKeyStates = state;
                    args.Handled = true;
                    break;
                case UIScannerKeyboard1.ContinuousScanKey:
                    ContinuousKeyStates = state;
                    args.Handled = true;
                    break;
                case UIScannerKeyboard1.AutoKey:
                    AutoKeyStates = state;
                    args.Handled = true;
                    break;
                case UIScannerKeyboard1.MarkKey:
                    MarkKeyStates = state;
                    args.Handled = true;
                    break;
                case UIScannerKeyboard1.ImsKey:
                    ImsKeyStates = state;
                    args.Handled = true;
                    break;
                case UIScannerKeyboard1.MenuKey:
                    MenuKeyStates = state;
                    break;
                case UIScannerKeyboard1.BlackWhiteKey:
                    BWKeyStates = state;
                    args.Handled = true;
                    break;
                case UIScannerKeyboard1.OsKey:
                    OsKeyStates = state;
                    args.Handled = true;
                    break;
                case UIScannerKeyboard1.MsKey:
                    MsKeyStates = state;
                    break;
                case UIScannerKeyboard1.SenKey:
                    SenKeyStates = state;
                    args.Handled = true;
                    break;
                case UIScannerKeyboard1.EscKey:
                    EscKeyStates = state;
                    args.Handled = true;
                    break;
                case UIScannerKeyboard1.Z789Key:
                    Z789KeyStates = state;
                    args.Handled = true;
                    break;
                case UIScannerKeyboard1.VFlipKey:
                    FlipKeyStates = state;
                    args.Handled = true;
                    break;
                case UIScannerKeyboard1.DynamicGSTKey:
                    GrayScaleTransformKeyStates = state;
                    args.Handled = true;
                    break;
                case UIScannerKeyboard1.Zoom1X:
                    Zoom1XKeyStates = state;
                    break;
                case UIScannerKeyboard1.ZoomOutKey:
                    ZoomOutKeyStates = state;
                    args.Handled = true;
                    break;
                case UIScannerKeyboard1.ZoomInKey:
                    ZoomInKeyStates = state;
                    args.Handled = true;
                    break;

                case UIScannerKeyboard1.F1Key:
                    F1KeyStates = state;
                    args.Handled = true;
                    break;
                case UIScannerKeyboard1.F2Key:
                    F2KeyStates = state;
                    args.Handled = true;
                    break;
                case UIScannerKeyboard1.F3Key:
                    F3KeyStates = state;
                    args.Handled = true;
                    break;

                case UIScannerKeyboard1.ConveyorBackwardKey:
                    if ((args.KeyboardDevice.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        ConveyorBackKeyState = state;
                        args.Handled = true;
                    }
                    break;
                case UIScannerKeyboard1.ConveyorForwardKey:
                    if ((args.KeyboardDevice.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        ConveyorForwKeyStates = state;
                        args.Handled = true; 
                    }
                    break;
                case UIScannerKeyboard1.ConveyorStopKey:
                    if ((args.KeyboardDevice.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    {
                        ConveyorStopKeyStates = state;
                        args.Handled = true;
                    }
                    break;

                case UIScannerKeyboard1.UpKey:
                    UpKeyStates = state;
                    args.Handled = true;
                    break;
                case UIScannerKeyboard1.DownKey:
                    DownKeyStates = state;
                    args.Handled = true;
                    break;
                case UIScannerKeyboard1.LeftKey:
                    LeftKeyStates = state;
                    args.Handled = true;
                    break;
                case UIScannerKeyboard1.RightKey:
                    RightKeyStates = state;
                    args.Handled = true;
                    break;

                // 以下仅用于测试钥匙开关和几个LED灯
                case Key.F7:
                    IsKeySwitchOn = state == KeyStates.Down;
                    args.Handled = true;
                    break;
                case Key.F8:
                    IsKeySwitchLedOn = state == KeyStates.Down;
                    UIScannerKeyboard1.IsKeySwitchIndicatorOn = IsKeySwitchLedOn;
                    args.Handled = true;
                    break;
                case Key.F9:
                    IsPowerLedOn = state == KeyStates.Down;
                    UIScannerKeyboard1.IsPowerIndicatorOn = IsPowerLedOn;
                    args.Handled = true;
                    break;
                case Key.F11:
                    IsXRay1LedOn = state == KeyStates.Down;
                    UIScannerKeyboard1.IsXRayIndicator1On = IsXRay1LedOn;
                    args.Handled = true;
                    break;
                case Key.F12:
                    IsXRay2LedOn = state == KeyStates.Down;
                    UIScannerKeyboard1.IsXRayIndicator2On = IsXRay2LedOn;
                    args.Handled = true;
                    break;

                // 使用计算机键盘中的逗号键，用于测试串口键盘的蜂鸣器
                case Key.OemComma:
                    if (state == KeyStates.Down)
                    {
                        BeepCommandExecute();
                    }
                    args.Handled = true;
                    break;
            }
        }

        private void IsDirectionKeyChangedEventCommandExecute()
        {
            ScannerConfig.Write(ConfigPath.KeyboardIsConveyorKeyReversed, _isConveyorKeyReversed);
            ScannerConfig.Write(ConfigPath.KeyboardIsKeyboardReversed, _isKeyboardReversed);
        }

        private void IsBiDirectionScanEventCommandExecute()
        {
            ScannerConfig.Write(ConfigPath.MachineBiDirectionScan, _enableBidirectionScan);
            ControlService.ServicePart.EnableBidirectionalScan(_enableBidirectionScan);
        }

        private void IsEnableContinuousScanEventCommandExecute()
        {
            ScannerConfig.Write(ConfigPath.MachineContinuousScan, _isEnableContinuousScan);
        }

        public override void OnKeyDown(System.Windows.Input.KeyEventArgs args)
        {
            
        }

        public override void Cleanup()
        {
            base.Cleanup();
            UIScannerKeyboard1.SetWitchInputStates(true);
        }
    }
}
