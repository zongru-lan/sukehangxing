using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Services;
using UI.XRay.Gui.Framework;
using UI.XRay.Security.Scanner.Converters;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Account
{
    public class AccountPageViewModel : PageViewModelBase
    {
        public RelayCommand ChangePasswordCommand { get; set; }

        private string _loginTimeStr;

        public string LoginTimeStr
        {
            get { return _loginTimeStr; }
            set { _loginTimeStr = value; RaisePropertyChanged(); }
        }

        private IFramePageNavigationService _navigationService;


        private Business.Entities.Account _current;

        public Business.Entities.Account Current
        {
            get { return _current; }
            set { _current = value; RaisePropertyChanged(); }
        }

        private bool _isChangePwBtnEnabled;

        public bool IsChangePwBtnEnabled
        {
            get { return _isChangePwBtnEnabled; }
            set { _isChangePwBtnEnabled = value;RaisePropertyChanged(); }
        }

        /// <summary>
        /// 当前登陆用户的角色
        /// </summary>
        public string RoleString { get; set; }

        public string Name { get; set; }

        public AccountPageViewModel(IFramePageNavigationService service)
        {
            Current = LoginAccountManager.Service.CurrentAccount;
            string dateFormat = DateFormatHelper.GetDateFormatHelper();

            string dateTimeFormat = dateFormat + " HH:mm:ss";
            LoginTimeStr = LoginAccountManager.Service.LoginTime.ToString(dateTimeFormat);

            if (Current != null)
            {
                InitWorkIntervalReminder();
                IsChangePwBtnEnabled = true;
                RoleString = AccountRoleStringsProvider.GetStringForRole(Current.Role);
                Name = Current.Name;
            }
            else
            {
                IsChangePwBtnEnabled = false;
            }

            _navigationService = service;
            ChangePasswordCommand = new RelayCommand(ChangePasswordCommandExecute);
        }

        /// <summary>
        /// 点击更改密码按钮后，导航至更改密码页
        /// </summary>
        private void ChangePasswordCommandExecute()
        {
            var message = new ShowPasswordDialogAsyncMessage("SettingWindow", Current.AccountId, MetroDialogButtons.OkCancel, result =>
            {
                if (result.DialogResult == MetroDialogResult.Ok)
                {
                    if (string.Equals(result.Password, Current.Password, StringComparison.OrdinalIgnoreCase))
                    {
                        _navigationService.ShowPage(new PageNavigation("ChangePasswordEmptyMenuPage", "ChangePasswordPage", "Change Password"), Current);
                    }
                    else
                    {
                        var msg = new ShowDialogMessageAction("SettingWindow", TranslationService.FindTranslation("Error"),
                            TranslationService.FindTranslation("The password you entered is incorrect!"), MetroDialogButtons.Ok, dialogResult =>{});
                        this.MessengerInstance.Send(msg);
                    }
                }
            });
            this.MessengerInstance.Send(message);
        }

        public override void OnKeyDown(System.Windows.Input.KeyEventArgs args)
        {
            switch (args.Key)
            {
                case Key.F1:
                    if (IsChangePwBtnEnabled)
                    {
                        ChangePasswordCommandExecute();
                        args.Handled = true;
                    }
                    
                    break;
            }
        }

        #region 换班提醒
        public RelayCommand WorkIntervalReminderChangedCommand { get; set; }
        public RelayCommand SaveMaintenanceIntervalCommand { get; set; }

        public List<TimeIntervalEnumString> WorkIntervalList { get; set; }

        private TimeIntervalEnum _selectedWorkDurationInterval;
        public TimeIntervalEnum SelectedWorkDurationInterval { get { return _selectedWorkDurationInterval; } set { _selectedWorkDurationInterval = value; RaisePropertyChanged(); } }

        private bool _isWorkIntervalRemind;
        public bool IsWorkIntervalRemind { get { return _isWorkIntervalRemind; } set { _isWorkIntervalRemind = value; RaisePropertyChanged(); } }

        public bool IsAdmin { get;set; }

        private void InitWorkIntervalReminder()
        {
            IsAdmin = Current.Role < AccountRole.Operator;
            WorkIntervalReminderChangedCommand = new RelayCommand(WorkIntervalReminderChangedCommandExecute);
            SaveMaintenanceIntervalCommand = new RelayCommand(SaveMaintenanceIntervalCommandExecute);
            WorkIntervalList = TimeIntervalEnumString.EnumAll();
            if (!ScannerConfig.Read(ConfigPath.IsWorkIntervalReminder, out _isWorkIntervalRemind))
            {
                _isWorkIntervalRemind = false;
            }
            if (!ScannerConfig.Read(ConfigPath.WorkReminderTime, out _selectedWorkDurationInterval))
            {
                _selectedWorkDurationInterval = TimeIntervalEnum.HalfHour;
            }
        }

        private void SaveMaintenanceIntervalCommandExecute()
        {
            ScannerConfig.Write(ConfigPath.WorkReminderTime, SelectedWorkDurationInterval);
            Messenger.Default.Send<WorkReminderChangedMessage>(new WorkReminderChangedMessage(SelectedWorkDurationInterval, IsWorkIntervalRemind));
        }

        private void WorkIntervalReminderChangedCommandExecute()
        {
            ScannerConfig.Write(ConfigPath.IsWorkIntervalReminder, IsWorkIntervalRemind);
            Messenger.Default.Send<WorkReminderChangedMessage>(new WorkReminderChangedMessage(SelectedWorkDurationInterval, IsWorkIntervalRemind));
        }
        #endregion
    }
}
