using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using UI.XRay.Business.Entities;
using UI.XRay.Business.Entities.Enums;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Controllers.SummaryEntity;
using UI.XRay.Gui.Framework;
using UI.XRay.Flows.Services;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Records
{
    public class BootLogsPageViewModel : PageViewModelBase
    {
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
        /// 记录列表视图是否可见
        /// </summary>
        private Visibility _recordsListVisibility;

        /// <summary>
        /// 统计周期选择列表
        /// </summary>
        public List<ValueStringItem<StatisticalPeriod>> PeriodList
        {
            get { return _controller.GetPeriodList1(); }
        }

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
        /// 选中的统计周期
        /// </summary>
        public StatisticalPeriod SelectedPeriod
        {
            get { return _controller.SelectedPeriod; }
            set { _controller.SelectedPeriod = value; RaisePropertyChanged(); }
        }

        private BootLogsRetrievalController _controller;

        /// <summary>
        /// 统计结果
        /// </summary>
        public List<BootWorkingHours> StatisticResults
        {
            get { return _statisticResults; }
            set
            {
                _statisticResults = value;
                RaisePropertyChanged();
            }
        }

        private List<BootWorkingHours> _statisticResults;


        public BootLogsPageViewModel()
        {
            _controller = new BootLogsRetrievalController();
            PeriodSelectionChangedEventCommand = new RelayCommand(PeriodSelectionChangedEventCommandExecute);

            FindCommand = new RelayCommand(FindCommandExecute);
            ExportCommand = new RelayCommand(ExportCommandExecute);

            SelectedPeriod = StatisticalPeriod.Dayly;
            SelectedYear = DateTime.Now.Year;
            SelectedMonth = DateTime.Now.Month;

            RecordsListVisibility = Visibility.Collapsed;

            if (IsInDesignMode)
            {
                StatisticResults = new List<BootWorkingHours>() { new BootWorkingHours() { Date = "2016-1-1", Hours = 10.2 ,BagCount = 12,H="10:12:00"}};
                RecordsListVisibility = Visibility.Visible;
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
                    new OperationRecordService().RecordOperation(OperationUI.BootLog, string.Format("{0}-{1}", SelectedYear, SelectedMonth), OperationCommand.Find, "Dayly");
                    break;
                case StatisticalPeriod.Weekly:
                    new OperationRecordService().RecordOperation(OperationUI.BootLog, string.Format("{0}", SelectedYear), OperationCommand.Find, "Weekly");
                    break;
                case StatisticalPeriod.Monthly:
                    new OperationRecordService().RecordOperation(OperationUI.BootLog, string.Format("{0}", SelectedYear), OperationCommand.Find, "Monthly");
                    break;
                case StatisticalPeriod.Quarterly:
                    new OperationRecordService().RecordOperation(OperationUI.BootLog, string.Format("{0}", SelectedYear), OperationCommand.Find, "Quarterly");
                    break;
                default:
                    break;
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
                        new KeyValuePair<string, string>("Date", TranslationService.FindTranslation("Date")),
                        new KeyValuePair<string, string>("H", TranslationService.FindTranslation("Working Hours")),
                        new KeyValuePair<string, string>("BagCount", TranslationService.FindTranslation("Bag Count")),  //yxc
                    }, ConfigHelper.GetExportFileName(OperationUI.BootLog, s, TranslationService.FindTranslation("BootLog")));
                    
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

        public override void OnKeyDown(System.Windows.Input.KeyEventArgs args)
        {
        }
    }
}
