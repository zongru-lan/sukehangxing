using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using UI.Common.Tracers;
using UI.XRay.Business.Entities;
using UI.XRay.Business.Entities.Enums;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Controllers.SummaryEntity;
using UI.XRay.Gui.Framework;
using UI.XRay.Flows.Services;
using UI.XRay.Security.Scanner.Converters;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Records
{
    /// <summary>
    /// 登录日志查询页面的视图模型
    /// </summary>
    public class LoginLogsPageViewModel : PageViewModelBase
    {
        /// <summary>
        /// 用户角色选择变化更改事件
        /// </summary>
        public RelayCommand RoleSelectionChangedEventCommand { get; private set; }

        /// <summary>
        /// 检索周期选择变化更改事件
        /// </summary>
        public RelayCommand PeriodSelectionChangedEventCommand { get; private set; }

        public RelayCommand FindCommand { get; private set; }

        public RelayCommand ExportCommand { get; private set; }

        public List<int> YearsList
        {
            get { return _controller.GetYearsList(); }
        }

        public List<ValueStringItem<int>> MonthsList
        {
            get { return _controller.GetMonthsList(); }
        }

        /// <summary>
        /// 统计结果
        /// </summary>
        public List<WorkSession> StatisticResults
        {
            get { return _statisticResults; }
            set
            {
                _statisticResults = value;
                RaisePropertyChanged();
            }
        }

        private List<WorkSession> _statisticResults;

        /// <summary>
        /// 记录列表视图是否可见
        /// </summary>
        private Visibility _recordsListVisibility;

        private List<string> _accounts;

        /// <summary>
        /// 统计周期选择列表：按天或自定义时间范围
        /// </summary>
        public List<TimeRangeEnumString> TimeRangeEnums { get; set; }

        /// <summary>
        /// 当前选中的时间范围枚举
        /// </summary>
        public TimeRange SelectedTimeRange { get; set; }

        /// <summary>
        /// 用户角色选择列表。为空时，表示选择所有角色
        /// </summary>
        public List<ValueStringItem<AccountRole?>> RolesList { get; private set; }

        /// <summary>
        /// 当前的用户列表
        /// </summary>

        /// <summary>
        /// 记录列表视图是否可见
        /// </summary>
        public Visibility RecordsListVisibility
        {
            get { return _recordsListVisibility; }
            set { _recordsListVisibility = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 当前选中的年份
        /// </summary>
        public int SelectedYear
        {
            get { return _controller.SelectedYear; }
            set { _controller.SelectedYear = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 当前选中的月
        /// </summary>
        public int SelectedMonth
        {
            get { return _controller.SelectedMonth; }
            set
            {
                _controller.SelectedMonth = value;
                RaisePropertyChanged();
            }
        }


        /// <summary>
        /// 当前选择的用户角色
        /// </summary>
        public AccountRole? SelectedRole
        {
            get { return _controller.SelectedRole; }
            set { _controller.SelectedRole = value; RaisePropertyChanged(); }
        }

        public string SelectedAccountId
        {
            get { return _controller.SelectedAccountId; }
            set { _controller.SelectedAccountId = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 选中的用户角色下的所有用户
        /// </summary>
        public List<string> Accounts
        {
            get { return _controller.AccountIds; }
        }

        private LoginLogsRetrievalController _controller;

        private DateTime _startDate = DateTime.Now.AddHours(-1);
        public DateTime StartDate
        {
            get { return _startDate; }
            set { _startDate = value; RaisePropertyChanged(); }
        }

        private int _startHour;

        public int StartHour
        {
            get { return _startHour; }
            set
            {
                _startHour = value;
                RaisePropertyChanged();
            }
        }

        private int _endHour;

        public int EndHour
        {
            get { return _endHour; }
            set
            {
                _endHour = value;
                RaisePropertyChanged();
            }
        }

        private int _startMinute;

        public int StartMinute
        {
            get { return _startMinute; }
            set { _startMinute = value; RaisePropertyChanged(); }
        }

        private int _endMinute;

        public int EndMinute
        {
            get { return _endMinute; }
            set { _endMinute = value; RaisePropertyChanged(); }
        }

        private DateTime _endDate = DateTime.Now;

        public DateTime EndDate
        {
            get { return _endDate; }
            set { _endDate = value; RaisePropertyChanged(); }
        }

        private Visibility _isTimeRangeEditGridVisibility;
        public Visibility IsTimeRangeEditGridVisibility
        {
            get { return _isTimeRangeEditGridVisibility; }
            set { _isTimeRangeEditGridVisibility = value; RaisePropertyChanged(); }
        }

        public LoginLogsPageViewModel()
        {
            try
            {
                RoleSelectionChangedEventCommand = new RelayCommand(RoleSelectionChangedEventCommandExecute);
                PeriodSelectionChangedEventCommand = new RelayCommand(PeriodSelectionChangedEventCommandExecute);
                FindCommand = new RelayCommand(FindCommandExecute);
                ExportCommand = new RelayCommand(ExportCommandExecute);
                _controller = new LoginLogsRetrievalController();

                SelectedYear = DateTime.Now.Year;
                SelectedMonth = DateTime.Now.Month;

                RecordsListVisibility = Visibility.Collapsed;

                RolesList = _controller.GetRoleStringList();
                SelectedRole = AccountRole.Operator;

                TimeRangeEnums = TimeRangeEnumString.EnumAll();
                SelectedTimeRange = TimeRange.LastHour;
                PeriodSelectionChangedEventCommandExecute();
                IsTimeRangeEditGridVisibility = Visibility.Collapsed;
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }

        private void FindCommandExecute()
        {
            var startTime = new DateTime(StartDate.Year, StartDate.Month, StartDate.Day, StartHour, StartMinute, 0);
            var endTime = new DateTime(EndDate.Year, EndDate.Month, EndDate.Day, EndHour, EndMinute, 59);
            StatisticResults = _controller.GetStatisticalResults(startTime,endTime);
            if (StatisticResults != null && StatisticResults.Count > 0)
            {
                RecordsListVisibility = Visibility.Visible;
            }
            else
            {
                RecordsListVisibility = Visibility.Collapsed;
            }

            string content = string.Empty;
            if (SelectedTimeRange == TimeRange.SpecifiedTimeRange)
            {
                content = string.Format("SpecifiedTimeRange:{{{0} - {1}}}", DateFormatHelper.DateTime2String(startTime), DateFormatHelper.DateTime2String(endTime));
            }
            else
            {
                content = SelectedTimeRange.ToString();
            }

            new OperationRecordService().RecordOperation(OperationUI.LoginLog, GetAccountsString(), OperationCommand.Find,content);
        }

        private string GetAccountsString()
        {
            if (SelectedAccountId != null)
            {
                return SelectedAccountId;
            }
            else
            {
                if (Accounts.Count > 1)
                {
                    return string.Join(",", Accounts.ToArray()).Remove(0,1);
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        private void ExportCommandExecute()
        {
            var msg = new ShowFolderBrowserDialogMessageAction("SettingWindow", s =>
            {
                if (_controller != null && !string.IsNullOrWhiteSpace(s))
                {
                    _controller.Export(StatisticResults, new List<KeyValuePair<string, string>>()
                    {
                        new KeyValuePair<string, string>("AccountId", TranslationService.FindTranslation("Account Id")),
                        new KeyValuePair<string, string>("LoginTime", TranslationService.FindTranslation("Login Time")),
                        new KeyValuePair<string, string>("LogoutTime", TranslationService.FindTranslation("Logout Time")),
                        new KeyValuePair<string, string>("WorkingHours", TranslationService.FindTranslation("Working Hours")),
                    }, ConfigHelper.GetExportFileName(OperationUI.LoginLog,s, TranslationService.FindTranslation("LoginLog")));

                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessengerInstance.Send(new ShowDialogMessageAction("SettingWindow",
                            "", TranslationService.FindTranslation("Dump completely"), MetroDialogButtons.Ok,
                            result =>
                            {
                            }));
                    });
                }
            });

            MessengerInstance.Send(msg);
        }

        private void PeriodSelectionChangedEventCommandExecute()
        {
            var now = DateTime.Now;
            switch (SelectedTimeRange)
            {
                case TimeRange.RecentMonth:
                    StartDate = now - new TimeSpan(30, 0, 0, 0);
                    EndDate = now;
                    StartHour = EndHour = now.Hour;
                    StartMinute = EndMinute = now.Minute;
                    IsTimeRangeEditGridVisibility = Visibility.Collapsed;
                    break;

                case TimeRange.RecentWeek:
                    StartDate = now - new TimeSpan(7, 0, 0, 0);
                    EndDate = now;
                    StartHour = EndHour = now.Hour;
                    StartMinute = EndMinute = now.Minute;
                    IsTimeRangeEditGridVisibility = Visibility.Collapsed;
                    break;

                case TimeRange.LastHour:
                    StartDate = now - new TimeSpan(0, 1, 0, 0);
                    EndDate = now;
                    EndHour = now.Hour;
                    EndMinute = now.Minute;
                    StartHour = EndHour > 0 ? EndHour - 1 : 23;
                    StartMinute = EndMinute;
                    IsTimeRangeEditGridVisibility = Visibility.Collapsed;
                    break;

                case TimeRange.Today:
                    StartDate = DateTime.Today;
                    EndDate = DateTime.Today;
                    StartHour = 0;
                    StartMinute = 0;
                    EndHour = 23;
                    EndMinute = 59;
                    IsTimeRangeEditGridVisibility = Visibility.Collapsed;
                    break;

                case TimeRange.Yestoday:
                    StartDate = DateTime.Today - new TimeSpan(1, 0, 0, 0);
                    EndDate = DateTime.Today - new TimeSpan(1, 0, 0, 0);
                    StartHour = 0;
                    StartMinute = 0;
                    EndHour = 23;
                    EndMinute = 59;
                    IsTimeRangeEditGridVisibility = Visibility.Collapsed;
                    break;

                case TimeRange.SpecifiedTimeRange:
                    IsTimeRangeEditGridVisibility = Visibility.Visible;
                    break;
            }
        }

        private void RoleSelectionChangedEventCommandExecute()
        {
            RaisePropertyChanged("Accounts");
        }


        public override void OnKeyDown(System.Windows.Input.KeyEventArgs args)
        {
        }
    }
}
