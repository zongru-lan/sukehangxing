using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using UI.Common.Tracers;
using UI.XRay.Business.Entities;
using UI.XRay.Business.Entities.Enums;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Services.Db;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Scanner.ViewModel
{
    /// <summary>
    /// 图像检索窗口的视图模型
    /// </summary>
    public class ImageRetrievalViewModel : ViewModelBase
    {
        public RelayCommand OkCommand { get; set; }

        public RelayCommand CancelCommand { get; set; }

        public RelayCommand<KeyEventArgs> PreviewKeyDownEventCommand { get; set; }

        public RelayCommand<KeyEventArgs> PreviewKeyUpEventCommand { get; set; }

        public RelayCommand<SelectionChangedEventArgs> SelectionChangedEventCommand { get; set; }

        public List<Account> Accounts { get; set; }

        private string _selectedAccountId;

        public string SelectedAccountId
        {
            get { return _selectedAccountId; }
            set
            {
                _selectedAccountId = value;
                RaisePropertyChanged();
            }
        }

        private bool _isAccountsSwitchEnabled;

        /// <summary>
        /// 是否启用根据用户账户进行检索的选项
        /// </summary>
        public bool IsAccountsSwitchEnabled
        {
            get { return _isAccountsSwitchEnabled; }
            set { _isAccountsSwitchEnabled = value; RaisePropertyChanged(); }
        }

        private bool _isOnlyLockedImage;

        /// <summary>
        /// 是否仅检索被锁定的图像
        /// </summary>
        public bool IsOnlyLockedImage
        {
            get { return _isOnlyLockedImage; }
            set
            {
                _isOnlyLockedImage = value;
                RaisePropertyChanged();
            }
        }

        private bool _iOnlyMarkedImage;

        public bool IsOnlyMarkedImage
        {
            get { return _iOnlyMarkedImage; }
            set { _iOnlyMarkedImage = value; RaisePropertyChanged(); }
        }


        public List<TimeRangeEnumString> TimeRangeEnums { get; set; }

        /// <summary>
        /// 当前选中的时间范围枚举
        /// </summary>
        public TimeRange SelectedTimeRange { get; set; }

        private DateTime _startDate;

        public DateTime StartDate
        {
            get { return _startDate; }
            set
            {
                _startDate = value;
                RaisePropertyChanged();
            }
        }

        private int _startHour;

        public int StartHour
        {
            get { return _startHour; }
            set
            {
                if (StartDate == EndDate && value > EndHour)
                {
                    _startHour = EndHour;
                }
                else
                {
                    _startHour = value;
                }
                RaisePropertyChanged();
            }
        }

        private int _endHour;

        public int EndHour
        {
            get { return _endHour; }
            set
            {
                if (StartDate == EndDate && value < StartHour)
                {
                    _endHour = StartHour;
                }
                else
                {
                    _endHour = value;
                }
                RaisePropertyChanged();
            }
        }

        private int _startMinute;

        public int StartMinute
        {
            get { return _startMinute; }
            set
            {
                if (StartDate == EndDate && StartHour == EndHour && value > EndMinute)
                {
                    _startMinute = EndMinute;
                }
                else
                {
                    _startMinute = value;
                }
                RaisePropertyChanged();
            }
        }

        private int _endMinute;

        public int EndMinute
        {
            get { return _endMinute; }
            set
            {
                if (EndDate == StartDate && EndHour == StartHour && value < StartMinute)
                {
                    _endMinute = StartMinute;
                }
                else
                {
                    _endMinute = value;
                }
                RaisePropertyChanged();
            }
        }

        private DateTime _endDate = DateTime.Now;

        public DateTime EndDate
        {
            get { return _endDate; }
            set
            {
                _endDate = value < StartDate ? StartDate : value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 手动编辑查询起止时间的grid是否可用
        /// </summary>
        public bool IsTimeRangeEditGridEnabled
        {
            get { return _isTimeRangeEditGridEnabled; }
            set { _isTimeRangeEditGridEnabled = value; RaisePropertyChanged(); }
        }


        private bool _isTimeRangeEditGridEnabled;

        public ImageRetrievalViewModel()
        {
            Tracer.TraceEnterFunc("UI.XRay.Security.Scanner.ViewModel.ImageRetrievalViewModel constructor");

            try
            {
                OkCommand = new RelayCommand(OkCommandExecute);
                CancelCommand = new RelayCommand(CancelCommandExecute);
                SelectionChangedEventCommand = new RelayCommand<SelectionChangedEventArgs>(SelectionChangedEventCommandExecute);
                PreviewKeyDownEventCommand = new RelayCommand<KeyEventArgs>(PreviewKeyDownEventCommandExecute);
                PreviewKeyUpEventCommand = new RelayCommand<KeyEventArgs>(PreviewKeyUpEventCommandExecute);

                var accountController = new AccountDbSet();
                Accounts = LoginAccountManager.Service.AllAccount;
                TimeRangeEnums = TimeRangeEnumString.EnumAll();

                // 初始化时，尝试从Locator的cache中获取缓存的图像检索条件
                var conditions = ViewModelLocator.Locator.Cache["ImageRetrievalConditions"] as ImageRetrievalConditions;
                if (conditions != null)
                {
                    SelectedAccountId = conditions.AccountId;
                    SelectedTimeRange = conditions.TimeRange;
                    IsAccountsSwitchEnabled = !string.IsNullOrEmpty(SelectedAccountId);
                    IsOnlyLockedImage = conditions.OnlyLocked;
                    IsOnlyMarkedImage = conditions.OnlyMarked;
                    StartDate = conditions.StartTime.Date;    //yxc
                    StartHour = conditions.StartTime.Hour;
                    StartMinute = conditions.StartTime.Minute;
                    EndDate = conditions.EndTime.Date;  // yxc
                    EndHour = conditions.EndTime.Hour;
                    EndMinute = conditions.EndTime.Minute;
                }
                else
                {
                    if (LoginAccountManager.Service.CurrentAccount != null)
                    {
                        SelectedAccountId = LoginAccountManager.Service.CurrentAccount.AccountId;
                    }

                    SelectedTimeRange = TimeRange.LastHour;
                    IsAccountsSwitchEnabled = false;
                    IsOnlyLockedImage = false;
                    IsOnlyMarkedImage = false;
                }

                SelectionChangedEventCommandExecute(null);
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception, "Unexpected exception caught in ImageRetrievalViewModel constructor");
            }

            Tracer.TraceExitFunc("UI.XRay.Security.Scanner.ViewModel.ImageRetrievalViewModel constructor");
        }

        private void PreviewKeyUpEventCommandExecute(KeyEventArgs args)
        {
            ConveyorKeyEventsConvertor.HandlePreviewKeyUp(args);
        }

        private void PreviewKeyDownEventCommandExecute(KeyEventArgs args)
        {
            if (ConveyorKeyEventsConvertor.HandlePreviewKeyDown(args))
            {
                return;
            }

            switch (args.Key)
            {
                case Key.F1:
                case Key.Enter:
                    OkCommandExecute();
                    args.Handled = true;
                    break;

                case Key.F3:
                case Key.Escape:
                    CancelCommandExecute();
                    args.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// 根据所选择的查询时间范围，动态修改日期和时间
        /// </summary>
        /// <param name="args"></param>
        private void SelectionChangedEventCommandExecute(SelectionChangedEventArgs args)
        {
            var now = DateTime.Now;
            switch (SelectedTimeRange)
            {
                case TimeRange.RecentMonth:
                    StartDate = now - new TimeSpan(30, 0, 0, 0);
                    EndDate = now;
                    StartHour = EndHour = now.Hour;
                    StartMinute = EndMinute = now.Minute;
                    IsTimeRangeEditGridEnabled = false;
                    break;

                case TimeRange.RecentWeek:
                    StartDate = now - new TimeSpan(7, 0, 0, 0);
                    EndDate = now;
                    StartHour = EndHour = now.Hour;
                    StartMinute = EndMinute = now.Minute;
                    IsTimeRangeEditGridEnabled = false;
                    break;

                case TimeRange.LastHour:
                    StartDate = now - new TimeSpan(0, 1, 0, 0);
                    EndDate = now;
                    EndHour = now.Hour;
                    EndMinute = now.Minute;
                    StartHour = EndHour > 0 ? EndHour - 1 : 23;
                    StartMinute = EndMinute;
                    IsTimeRangeEditGridEnabled = false;
                    break;

                case TimeRange.Today:
                    StartDate = DateTime.Today;
                    EndDate = DateTime.Today;
                    StartHour = 0;
                    StartMinute = 0;
                    EndHour = 23;
                    EndMinute = 59;
                    IsTimeRangeEditGridEnabled = false;
                    break;

                case TimeRange.Yestoday:
                    StartDate = DateTime.Today - new TimeSpan(1, 0, 0, 0);
                    EndDate = DateTime.Today - new TimeSpan(1, 0, 0, 0);
                    StartHour = 0;
                    StartMinute = 0;
                    EndHour = 23;
                    EndMinute = 59;
                    IsTimeRangeEditGridEnabled = false;
                    break;

                case TimeRange.SpecifiedTimeRange:
                    IsTimeRangeEditGridEnabled = true;
                    break;
            }
        }

        private void OkCommandExecute()
        {
            var startTime = new DateTime(StartDate.Year, StartDate.Month, StartDate.Day, StartHour, StartMinute, 0);
            var endTime = new DateTime(EndDate.Year, EndDate.Month, EndDate.Day, EndHour, EndMinute, 59);

            var accountId = SelectedAccountId;
            if (!IsAccountsSwitchEnabled)
            {
                accountId = null;
            }

            var conditions = new ImageRetrievalConditions(startTime, endTime, SelectedTimeRange, accountId, IsOnlyLockedImage, IsOnlyMarkedImage);

            // 用户点击确定按钮后，将用户设定的图像查询条件，发送给视图，由视图结束异步任务
            Messenger.Default.Send(new ImageRetrievalConditionsMessage(conditions));
        }

        private void CancelCommandExecute()
        {
            // 用户点击取消按钮后，不发送查询条件，直接由视图结束异步任务
            Messenger.Default.Send(new ImageRetrievalConditionsMessage(null));
        }
    }
}
