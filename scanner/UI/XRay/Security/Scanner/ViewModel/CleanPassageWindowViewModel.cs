using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Services;
using UI.XRay.Gui.Framework;
using UI.XRay.Parts.Keyboard;

namespace UI.XRay.Security.Scanner.ViewModel
{
    public class CleanTunnelWindowViewModel : ViewModelBase
    {
        #region commands
        public RelayCommand<KeyEventArgs> PreviewKeyDownEventCommand { get; set; }

        public RelayCommand ClosedEventCommand { get; private set; }

        public RelayCommand ConfirmEventCommand { get; private set; }

        #endregion commands

        /// <summary>
        /// 定时器，周期为1秒，定时检测
        /// </summary>
        private DispatcherTimer _timer;

        /// <summary>
        /// 预热总时长
        /// </summary>
        private readonly TimeSpan _duration;

        /// <summary>
        /// 已经预热的时长
        /// </summary>
        private TimeSpan _timeUsed;

        private double _percentDone;

        private double _remainingSeconds;

        public double PercentDone
        {
            get { return _percentDone; }
            set { _percentDone = value; RaisePropertyChanged(); }
        }


        /// <summary>
        /// 剩余时间
        /// </summary>
        public double RemainingSeconds
        {
            get { return _remainingSeconds; }
            set { _remainingSeconds = value; RaisePropertyChanged(); }
        }

        private float _MoveBackTime;
        private bool _isMoveBackward;

        public CleanTunnelWindowViewModel()
        {
            PreviewKeyDownEventCommand = new RelayCommand<KeyEventArgs>(PreviewKeyDownEventCommandExecute);
            ClosedEventCommand = new RelayCommand(ClosedEventCommandExecute);
            ConfirmEventCommand = new RelayCommand(ConfirmEventCommandExecute);

            LoadSettings();

            if (IsInDesignMode)
            {
                PercentDone = 30;
                RemainingSeconds = 100;
            }
            else
            {
                try
                {
                    _duration = TimeSpan.FromSeconds(_MoveBackTime);

                    RemainingSeconds = _duration.TotalSeconds;
                }
                catch (Exception exception)
                {
                }
            }
        }

        private void LoadSettings()
        {
            if (!ScannerConfig.Read(ConfigPath.CleanTunnelContinueTime, out _MoveBackTime))
            {
                _MoveBackTime = 15;
            }
            if (!ScannerConfig.Read(ConfigPath.CleanTunnelMoveBackward,out _isMoveBackward))
            {
                _isMoveBackward = true;
            }
        }

        private void PreviewKeyDownEventCommandExecute(KeyEventArgs args)
        {
            // 当用户按下Esc或F3按键时，终止预热过程
            if (args.Key == ScannerKeyboardPart.Keyboard.Esc || args.Key == ScannerKeyboardPart.Keyboard.F3)
            {
                ClosedEventCommandExecute();
            }
            else if(args.Key == ScannerKeyboardPart.Keyboard.F1)
            {
                ConfirmEventCommandExecute();
            }
        }

        private void ClosedEventCommandExecute()
        {
            if (_timer!=null)
            {
                _timer.Stop();
                _timer.Tick -= TimerOnTick;
                _timer = null;
            }

            // 窗口关闭时，必须注销此事件
            ControlService.ServicePart.DriveConveyor(Control.ConveyorDirection.Stop);
            MessengerInstance.Send(new CloseWindowMessage("CleanTunnelWindow"));
        }

        private void ConfirmEventCommandExecute()
        {
            if (_timer != null) return;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += TimerOnTick;
            _timer.Start();

            if(_isMoveBackward)
                ControlService.ServicePart.DriveConveyor(Control.ConveyorDirection.MoveBackward);
            else
                ControlService.ServicePart.DriveConveyor(Control.ConveyorDirection.MoveForward);
        }
        private void TimerOnTick(object sender, EventArgs eventArgs)
        {
            if (_timer!=null)
            {
                  _timeUsed += _timer.Interval;
            }          

            PercentDone = _timeUsed.TotalMilliseconds / _duration.TotalMilliseconds * 100;
            RemainingSeconds = _duration.TotalSeconds - _timeUsed.TotalSeconds;

            if (_timeUsed >= _duration)
            {
                ClosedEventCommandExecute();
            }
            else
            {
                if (_isMoveBackward)
                    ControlService.ServicePart.DriveConveyor(Control.ConveyorDirection.MoveBackward);
                else
                    ControlService.ServicePart.DriveConveyor(Control.ConveyorDirection.MoveForward);
            }
        }
    }
}
