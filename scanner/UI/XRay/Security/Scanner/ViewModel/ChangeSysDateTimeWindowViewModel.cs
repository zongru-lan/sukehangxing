using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using UI.XRay.Common.Utilities;
using UI.XRay.Gui.Framework;
using UI.XRay.Flows.Services;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Controllers;
using UI.XRay.Security.Scanner.Converters;

namespace UI.XRay.Security.Scanner.ViewModel
{
    public class ChangeSysDateTimeWindowViewModel:ViewModelBase
    {
        public RelayCommand OkCommand { get; private set; }
        public RelayCommand CancelCommand { get; private set; }

        public RelayCommand<KeyEventArgs> PreviewKeyDownEventCommand { get; set; }

        public RelayCommand TimePickerGotFocusCommand { get; private set; }
        public RelayCommand TimePickerLostFocusCommand { get; private set; }

        private DateTime _systemTime;

        public DateTime SystemTime
        {
            get { return _systemTime; }
            set
            {
                _systemTime = value;
                RaisePropertyChanged();
            }
        }

        private DateTime _systemDate;

        public DateTime SystemDate
        {
            get { return _systemDate; }
            set
            {
                _systemDate = value;
                RaisePropertyChanged();
            }
        }

        private readonly Timer _updateTimer;

        private readonly object _timeLock = new object();

        private bool _isTimePickerGetFocus;

        public ChangeSysDateTimeWindowViewModel()
        {
            OkCommand = new RelayCommand(OkCommandExe);
            CancelCommand = new RelayCommand(CancelCommandExe);
            PreviewKeyDownEventCommand = new RelayCommand<KeyEventArgs>(PreviewKeyDownEventCommandExecute);
            TimePickerGotFocusCommand = new RelayCommand(TimePickerGotFocusCommandExe);
            TimePickerLostFocusCommand = new RelayCommand(TimePickerLostFocusCommandExe);
            _updateTimer = new Timer
            {
                Interval = 500,
                AutoReset = true
            };
            _updateTimer.Elapsed+=UpdateTimer_Elapsed;
            _updateTimer.Start();

            SystemDate = DateTime.Now;
        }

        private void TimePickerLostFocusCommandExe()
        {
            _isTimePickerGetFocus = false;
        }

        private void TimePickerGotFocusCommandExe()
        {
            _isTimePickerGetFocus = true;
        }

        private void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //时间空间获取焦点后是修改时间
            if (_isTimePickerGetFocus)
            {
                return;
            }

            lock (_timeLock)
            {
                SystemTime = DateTime.Now;
            }   
        }

        private void CancelCommandExe()
        {
            if (_updateTimer.Enabled)
            {
                _updateTimer.Stop();
                _updateTimer.Close();
            }

            MessengerInstance.Send(new CloseWindowMessage("ChangeSysDateTimeWindow"));
        }

        private void OkCommandExe()
        {
            lock (_timeLock)
            {
                DateTimeHelper.SetLocalTime(SystemDate.Year, SystemDate.Month, SystemDate.Day, SystemTime.Hour,
                    SystemTime.Minute, SystemTime.Second);

                new OperationRecordService().AddRecord(new OperationRecord()
                {
                    AccountId = LoginAccountManager.Service.CurrentAccount != null ? LoginAccountManager.Service.CurrentAccount.AccountId : "",
                    OperateUI = OperationUI.ChangeSysDateTime,
                    OperateTime = DateTime.Now,
                    OperateObject = "ChangeSysDateTime",
                    OperateCommand = OperationCommand.Setting,
                    OperateContent = DateFormatHelper.DateTime2String(new DateTime(SystemDate.Year, SystemDate.Month, SystemDate.Day, SystemTime.Hour,
                    SystemTime.Minute, SystemTime.Second)),
                });
            }
        }

        private void PreviewKeyDownEventCommandExecute(KeyEventArgs args)
        {
            switch (args.Key)
            {
                case Key.F1:
                case Key.Enter:
                    OkCommandExe();
                    args.Handled = true;
                    break;

                case Key.F3:
                case Key.Escape:
                    CancelCommandExe();
                    args.Handled = true;
                    break;
            }
        }
    }
}
