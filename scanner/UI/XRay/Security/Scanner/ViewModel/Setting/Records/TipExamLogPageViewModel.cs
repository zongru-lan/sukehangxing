using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using UI.XRay.Business.Entities;
using UI.XRay.Business.Entities.Enums;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Controllers.SummaryEntity;
using UI.XRay.Flows.Services;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Records
{    
    public class TipExamLogPageViewModel : PageViewModelBase
    {
        public RelayCommand RoleSelectionChangedEventCommand { get; private set; }

        public RelayCommand PeriodSelectionChangedEventCommand { get; set; }

        public RelayCommand FindCommand { get; private set; }

        public RelayCommand ExportCommand { get; private set; }

        public List<OperationStatisticResult> StatisticResults
        {
            get { return _statisticResults; }
            set
            {
                _statisticResults = value;
                RaisePropertyChanged();
            }
        }
        private List<OperationStatisticResult> _statisticResults;

        /// <summary>
        /// 记录列表视图是否可见
        /// </summary>
        private Visibility _recordsListVisibility;

        private List<string> _accounts;

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

        public string SelectedAccountId
        {
            get { return _controller.SelectedAccountId; }
            set { _controller.SelectedAccountId = value; RaisePropertyChanged(); }
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
        /// 选中的统计周期
        /// </summary>
        public StatisticalPeriod SelectedPeriod
        {
            get { return _controller.SelectedPeriod; }
            set { _controller.SelectedPeriod = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 选中的用户角色下的所有用户
        /// </summary>
        public List<string> Accounts
        {
            get { return _controller.AccountIds; }
        }

        protected OperationLogsRetrievalController _controller;

        /// <summary>
        /// 统计周期选择列表：按天或自定义时间范围
        /// </summary>
        public List<TimeRangeEnumString> TimeRangeEnums { get; set; }

        /// <summary>
        /// 当前选中的时间范围枚举
        /// </summary>
        public TimeRange SelectedTimeRange { get; set; }

        /// <summary>
        /// 统计周期选择列表
        /// </summary>
        public List<ValueStringItem<StatisticalPeriod>> PeriodList
        {
            get { return _controller.GetPeriodList1(); }
        }

        public List<int> YearsList
        {
            get { return _controller.GetYearsList(); }
        }

        public List<ValueStringItem<int>> MonthsList
        {
            get { return _controller.GetMonthsList(); }
        }

        /// <summary>
        /// 当前选择的用户角色
        /// </summary>
        public AccountRole? SelectedRole
        {
            get { return _controller.SelectedRole; }
            set { _controller.SelectedRole = value; RaisePropertyChanged(); }
        }

        public TipExamLogPageViewModel()
        {
            try
            {
                RoleSelectionChangedEventCommand = new RelayCommand(RoleSelectionChangedEventCommandExecute);
                PeriodSelectionChangedEventCommand = new RelayCommand(PeriodSelectionChangedEventCommandExecute);
                FindCommand = new RelayCommand(FindCommandExecute);
                ExportCommand = new RelayCommand(ExportCommandExecute);
                _controller = new OperationLogsRetrievalController();

                SelectedPeriod = StatisticalPeriod.Dayly;
                SelectedYear = DateTime.Now.Year;
                SelectedMonth = DateTime.Now.Month;

                RecordsListVisibility = Visibility.Collapsed;

                SelectedRole = AccountRole.Operator;
                RaisePropertyChanged("Accounts");

                TimeRangeEnums = TimeRangeEnumString.EnumAll();
                SelectedTimeRange = TimeRange.LastHour;
                PeriodSelectionChangedEventCommandExecute();

            }
            catch (Exception)
            {
                
                throw;
            }
        }

        //protected override void ChangeRolesList()
        //{
        //    base._controller.SelectedRole = AccountRole.Operator;
        //}
        protected void ExportCommandExecute()
        {
            var msg = new ShowFolderBrowserDialogMessageAction("SettingWindow", s =>
            {
                if (_controller != null && !string.IsNullOrWhiteSpace(s))
                {
                    _controller.Export(StatisticResults, new List<KeyValuePair<string, string>>()
                    {
                        new KeyValuePair<string, string>("AccountId", TranslationService.FindTranslation("Account Id")),
                        new KeyValuePair<string, string>("Date", TranslationService.FindTranslation("Date")),
                        new KeyValuePair<string, string>("BagCount", TranslationService.FindTranslation("Bags Count")),
                        new KeyValuePair<string, string>("TipInjectionCount", TranslationService.FindTranslation("TIP Injection Count")),
                        new KeyValuePair<string, string>("TotolMarkCount",
                            TranslationService.FindTranslation("Mark Operation Count")),
                        new KeyValuePair<string, string>("MissTipCount", TranslationService.FindTranslation("Missed TIP Count")),
                        new KeyValuePair<string, string>("MissRate", TranslationService.FindTranslation("TIP Miss Rate")),
                    }, ConfigHelper.GetExportFileName(OperationUI.TipExamLog, s, TranslationService.FindTranslation("TipExamLog")));

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
            if (SelectedPeriod == StatisticalPeriod.Dayly)
            {
                SelectedMonth = DateTime.Now.Month;
            }
        }

        private void FindCommandExecute()
        {
            StatisticResults = _controller.GetStatisticalResults();

            if (StatisticResults != null && StatisticResults.Count > 0)
            {
                RecordsListVisibility = Visibility.Visible;
            }
            else
            {
                RecordsListVisibility = Visibility.Collapsed;
            }
            switch (SelectedPeriod)
            {
                case StatisticalPeriod.Dayly:
                    new OperationRecordService().RecordOperation(OperationUI.TipExamLog, string.Format("{0}-{1}", SelectedYear, SelectedMonth), OperationCommand.Find, "Dayly");
                    break;
                case StatisticalPeriod.Weekly:
                    new OperationRecordService().RecordOperation(OperationUI.TipExamLog, string.Format("{0}", SelectedYear), OperationCommand.Find, "Weekly");
                    break;
                case StatisticalPeriod.Monthly:
                    new OperationRecordService().RecordOperation(OperationUI.TipExamLog, string.Format("{0}", SelectedYear), OperationCommand.Find, "Monthly");
                    break;
                case StatisticalPeriod.Quarterly:
                    new OperationRecordService().RecordOperation(OperationUI.TipExamLog, string.Format("{0}", SelectedYear), OperationCommand.Find, "Quarterly");
                    break;
                default:
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
