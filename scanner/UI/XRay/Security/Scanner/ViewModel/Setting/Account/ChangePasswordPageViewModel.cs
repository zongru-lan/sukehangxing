using GalaSoft.MvvmLight.Command;
using System;
using System.Windows.Input;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Services;
using UI.XRay.Flows.Services.Db;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Account
{
    public class ChangePasswordPageViewModel : PageViewModelBase
    {
        public RelayCommand NaviBackCommand { get; set; }
        public RelayCommand OkCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }

        private string _currentAccountId;

        public string CurrentAccountId
        {
            get { return _currentAccountId; }
            set
            {
                _currentAccountId = value; RaisePropertyChanged();
            }
        }

        private string _newPassword;

        public string NewPassword
        {
            get { return _newPassword; }
            set { _newPassword = value; RaisePropertyChanged(); }
        }

        private string _confirmPassword;

        public string ConfirmPassword
        {
            get { return _confirmPassword; }
            set { _confirmPassword = value; RaisePropertyChanged(); }
        }

        private IFramePageNavigationService _navigationService;

        public ChangePasswordPageViewModel(IFramePageNavigationService service)
        {
            _navigationService = service;

            CurrentAccountId = LoginAccountManager.Service.AccountId;

            NaviBackCommand = new RelayCommand(NaviBackCommandExecute);
            OkCommand = new RelayCommand(OkCommandExecute);
            CancelCommand = new RelayCommand(CancelCommandExecute);
        }

        private void NaviBackCommandExecute()
        {
            _navigationService.ShowPage(new PageNavigation("AccountMenu", "AccountPage", "Account"));
        }

        private void CancelCommandExecute()
        {
            NaviBackCommandExecute();
        }

        private void OkCommandExecute()
        {
            // 密码和确认密码不同
            if (NewPassword != ConfirmPassword)
            {
                var msg = new ShowDialogMessageAction("SettingWindow", TranslationService.FindTranslation("Error"),
                            TranslationService.FindTranslation("The passwords you entered do not match!"), MetroDialogButtons.Ok, dialogResult => { });
                this.MessengerInstance.Send(msg);
            }
            // 密码为空或空格
            else if (string.IsNullOrWhiteSpace(NewPassword))
            {
                var msg = new ShowDialogMessageAction("SettingWindow", TranslationService.FindTranslation("Error"),
                            TranslationService.FindTranslation("Password can not be empty!"), MetroDialogButtons.Ok, dialogResult => { });
                this.MessengerInstance.Send(msg);
            }
            // 密码长度必须小于16位
            else if (NewPassword.Length > 16)
            {
                var msg = new ShowDialogMessageAction("SettingWindow", TranslationService.FindTranslation("Error"),
                            TranslationService.FindTranslation("Password must contain less than 16 characters!"), MetroDialogButtons.Ok, dialogResult => { });
                this.MessengerInstance.Send(msg);
            }
            else if (!isPasswordValid(NewPassword))
            {
                var msg = new ShowDialogMessageAction("SettingWindow", TranslationService.FindTranslation("Error"),
                            TranslationService.FindTranslation("Password can only contain numbers and letters!"), MetroDialogButtons.Ok, dialogResult => { });
                this.MessengerInstance.Send(msg);
            }
            else
            {
                LoginAccountManager.Service.CurrentAccount.Password = NewPassword;

                var controller = new AccountDbSet();
                controller.AddOrUpdate(LoginAccountManager.Service.CurrentAccount);

                NaviBackCommandExecute();

                new OperationRecordService().AddRecord(new OperationRecord()
                {
                    AccountId = LoginAccountManager.Service.CurrentAccount.AccountId,
                    OperateUI = OperationUI.ChangePassword,
                    OperateTime = DateTime.Now,
                    OperateObject = "Password",
                    OperateCommand = OperationCommand.Setting,
                    OperateContent = LoginAccountManager.Service.CurrentAccount.Password,
                });
            }
        }

        private bool isPasswordValid(string password)
        {
            bool ret = true;
            foreach (var ch in password)
            {
                if (!((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9')))
                {
                    ret = false;
                    break;
                }
            }

            return ret;
        }

        public override void OnKeyDown(System.Windows.Input.KeyEventArgs args)
        {
            switch (args.Key)
            {
                case Key.F1:
                case Key.Enter:
                    OkCommandExecute();
                    args.Handled = true;
                    break;

                case Key.F2:
                case Key.F3:
                case Key.Escape:
                    CancelCommandExecute();
                    args.Handled = true;
                    break;
            }
        }
    }
}
