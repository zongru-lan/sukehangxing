using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight.Command;
using UI.Common.Tracers;
using UI.XRay.Business.Entities;
using UI.XRay.Business.Entities.Enums;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Controllers.SummaryEntity;
using UI.XRay.Gui.Framework;
using System.Windows.Controls;
using UI.XRay.Security.Scanner.Converters;
using UI.XRay.ImagePlant;
using UI.XRay.Flows.Services;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Records
{
    public class OperationLogPageViewModel : PageViewModelBase
    {
        /// <summary>
        /// 用户角色选择变化更改事件
        /// </summary>
        public RelayCommand RoleSelectionChangedEventCommand { get; private set; }

        public RelayCommand<SelectionChangedEventArgs> SelectionChangedEventCommand { get; set; } 

        public RelayCommand FindCommand { get; private set; }

        public RelayCommand ExportCommand { get; private set; }

        /// <summary>
        /// 统计结果
        /// </summary>
        public List<OperationRecordShow> StatisticResults
        {
            get { return _statisticResults; }
            set
            {
                _statisticResults = value;
                RaisePropertyChanged();
            }
        }

        private List<OperationRecordShow> _statisticResults;

        /// <summary>
        /// 记录列表视图是否可见
        /// </summary>
        private Visibility _recordsListVisibility;

        private List<string> _accounts;


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

        protected OperationLogsRetrievalController _controller;

        public List<TimeRangeEnumString> TimeRangeEnums { get; set; }

        /// <summary>
        /// 当前选中的时间范围枚举
        /// </summary>
        public TimeRange SelectedTimeRange { get; set; }

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
        
        public OperationLogPageViewModel()
        {
            try
            {
                RoleSelectionChangedEventCommand = new RelayCommand(RoleSelectionChangedEventCommandExecute);
                SelectionChangedEventCommand = new RelayCommand<SelectionChangedEventArgs>(SelectionChangedEventCommandExecute);
                FindCommand = new RelayCommand(FindCommandExecute);
                ExportCommand = new RelayCommand(ExportCommandExecute);
                _controller = new OperationLogsRetrievalController();

                RecordsListVisibility = Visibility.Collapsed;

                RolesList = _controller.GetRoleStringList();

                SelectedRole = AccountRole.Operator;
                ChangeRolesList();
                TimeRangeEnums = TimeRangeEnumString.EnumAll();

                SelectedTimeRange = TimeRange.LastHour;
                StartDate = DateTime.Now - new TimeSpan(0, 1, 0, 0);
                EndDate = DateTime.Now;
                EndHour = DateTime.Now.Hour;
                EndMinute = DateTime.Now.Minute;
                StartHour = EndHour > 0 ? EndHour - 1 : 23;
                StartMinute = EndMinute;
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

            StatisticResults = GetStatisticResults(startTime, endTime).ToList();
        }

        private IEnumerable<OperationRecordShow> GetStatisticResults(DateTime startTime, DateTime endTime)
        {            
            var results = _controller.GetOperationRecord(startTime, endTime);
            if (results != null && results.Count > 0)
            {                
                RecordsListVisibility = Visibility.Visible;
                foreach (var result in results)
                {                 
                    OperationRecordShow or = new OperationRecordShow()
                    {
                        OperationRecordId = result.OperationRecordId,
                        AccountId = result.AccountId,
                        OperateTime = DateFormatHelper.DateTime2String(result.OperateTime),
                        OperateUI = TranslationService.FindTranslation("OperationRecord", result.OperateUI.ToString()),
                        OperateObject = TranslationService.FindTranslation("OperationRecord", result.OperateObject),
                        OperateCommand = TranslationService.FindTranslation("OperationRecord", result.OperateCommand.ToString()),
                        OperateContent = TranslationService.FindTranslation("OperationRecord", result.OperateContent.ToString()),
                    };
                    if (result.OperateUI == OperationUI.MainUI)
                    {
                        var effectsList = result.OperateContent.Split(',');
                        var effectTranslation = new StringBuilder();
                        foreach (var eff in effectsList)
                        {
                            DisplayColorMode dc;
                            if (Enum.TryParse(eff.Trim(), out dc))
                            {
                                effectTranslation.Append(TranslationService.FindTranslation(dc)).Append(' ');
                                continue;
                            }
                            PenetrationMode pm;
                            if (Enum.TryParse(eff.Trim(), out pm))
                            {
                                effectTranslation.Append(TranslationService.FindTranslation(pm)).Append(' ');
                                continue;
                            }
                            effectTranslation.Append(TranslationService.FindTranslation(eff.Trim())).Append(' ');
                        }
                        or.OperateContent = effectTranslation.ToString();
                    }
                    else if (result.OperateUI == OperationUI.FunctionKeys)
                    {                     
                        int index = 0;
                        var partsList = result.OperateContent.Split(',');
                        var settingStr = new StringBuilder();
                        foreach (var eff in partsList)
                        {
                            if(eff!=""&&index==0)
                            {
                                DisplayColorMode dc;
                                if (Enum.TryParse(eff.Trim(), out dc))
                                {
                                    settingStr.Append(TranslationService.FindTranslation("Color Mode") + TranslationService.FindTranslation("OperationRecord", ":") + TranslationService.FindTranslation(dc)).Append(' ').Append(' ');
                                }
                            }
                            else if (eff == "" && index == 0 )
                            {
                                settingStr.Append(TranslationService.FindTranslation("Color Mode") + TranslationService.FindTranslation("OperationRecord", ":") + TranslationService.FindTranslation("OperationRecord", "null")).Append(' ').Append(' ');
                            }
                            else if(eff!=""&&index==1)
                            {
                                PenetrationMode pm;
                                if (Enum.TryParse(eff.Trim(), out pm))
                                {
                                    settingStr.Append(TranslationService.FindTranslation("Penetration") + TranslationService.FindTranslation("OperationRecord", ":") + TranslationService.FindTranslation(pm)).Append(' ').Append(' ');
                                }
                            }  
                            else if (eff == "" && index == 1 )
                            {
                                settingStr.Append(TranslationService.FindTranslation("Penetration") + TranslationService.FindTranslation("OperationRecord", ":") + TranslationService.FindTranslation("OperationRecord", "null")).Append(' ').Append(' ');
                            }
                            else if (index == 2)
                                settingStr.Append(TranslationService.FindTranslation("Inversed") + TranslationService.FindTranslation("OperationRecord", ":") + TranslationService.FindTranslation("OperationRecord", eff)).Append(' ').Append(' ');
                            else if (index == 3)
                                settingStr.Append(TranslationService.FindTranslation("SuperEnhance") + TranslationService.FindTranslation("OperationRecord", ":") + TranslationService.FindTranslation("OperationRecord", eff)).Append(' ').Append(' ');
                            index++;                           
                        }
                        or.OperateContent = settingStr.ToString();
                    }
                    else
                    {
                        var parts = SplitAndKeep(result.OperateContent);
                        var settingStr = new StringBuilder();
                        foreach (var s in parts)
                        {
                            settingStr.Append(TranslationService.FindTranslation("OperationRecord", s));
                        }
                        or.OperateContent = settingStr.ToString();  
                    }
                    yield return or;
                }
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
            new OperationRecordService().RecordOperation(OperationUI.OperationLog, GetAccountsString(), OperationCommand.Find, content);
        }

        private string GetAccountsString()
        {
            if (SelectedAccountId != null)
            {
                return SelectedAccountId;
            }
            else
            {
                var str = string.Join(",", Accounts.Where(a => !string.IsNullOrWhiteSpace(a)).ToArray());
                if (string.IsNullOrWhiteSpace(str)) return string.Empty;
                return str.Substring(str.Length - 1, 1).Equals(',') ? str.Remove(0, 1) : string.Empty;
            }
        }

        virtual protected void ExportCommandExecute()
        {
            var msg = new ShowFolderBrowserDialogMessageAction("SettingWindow", s =>
            {
                if (_controller != null && !string.IsNullOrWhiteSpace(s))
                {
                    _controller.Export(StatisticResults, new List<KeyValuePair<string, string>>()
                    {
                        new KeyValuePair<string, string>("AccountId", TranslationService.FindTranslation("Account Id")),
                        new KeyValuePair<string, string>("OperateTime", TranslationService.FindTranslation("Operation Time")),
                        new KeyValuePair<string, string>("OperateUI", TranslationService.FindTranslation("Operation UI")),
                        new KeyValuePair<string, string>("OperateObject", TranslationService.FindTranslation("Operation Object")),
                        new KeyValuePair<string, string>("OperateCommand",TranslationService.FindTranslation("Operation Command")),
                        new KeyValuePair<string, string>("OperateContent",TranslationService.FindTranslation("Operation Content")),
                    }, ConfigHelper.GetExportFileName(OperationUI.OperationLog, s, TranslationService.FindTranslation("OperationLog")));

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
        virtual protected void ChangeRolesList()
        {

        }

        private void RoleSelectionChangedEventCommandExecute()
        {
            RaisePropertyChanged("Accounts");
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

        IEnumerable<string> SplitAndKeep(string s)
        {
            var delims = new char[] { ',', ':' };
            int start = 0, index;

            while ((index = s.IndexOfAny(delims, start)) != -1)
            {
                if (index - start > 0)
                    yield return s.Substring(start, index - start);
                yield return s.Substring(index, 1);
                start = index + 1;
            }

            if (start < s.Length)
            {
                yield return s.Substring(start);
            }
        }

        public override void OnKeyDown(System.Windows.Input.KeyEventArgs args)
        {
        }
    }
}
