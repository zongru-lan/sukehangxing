using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.ControlWorkflows;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Services;
using UI.XRay.Flows.Services.Db;
using UI.XRay.Flows.TRSNetwork;
using UI.XRay.Flows.TRSNetwork.Models;
using UI.XRay.Gui.Framework;
using UI.XRay.Parts.Keyboard;
using UI.XRay.Security.Scanner.ViewModel.Setting.Account;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace UI.XRay.Security.Scanner.ViewModel
{
    /// <summary>
    /// 登录窗口的视图模型
    /// </summary>
    public class LoginViewModel : ViewModelBase
    {
        public RelayCommand<KeyEventArgs> KeyDownEventCommand { get; set; }
        public RelayCommand<KeyEventArgs> PreviewKeyDownEventCommand { get; set; }
        public RelayCommand<KeyEventArgs> PreviewKeyUpEventCommand { get; set; }
        public RelayCommand<KeyEventArgs> KeyUpEventCommand { get; set; }

        public RelayCommand ResetCommand { get; set; }

        public RelayCommand LoginCommand { get; set; }

        public RelayCommand KeyboardCommand { get; set; }

        private bool _isLoginEnabled;

        public bool IsLoginEnabled
        {
            get { return _isLoginEnabled; }
            set
            {
                _isLoginEnabled = value;
                RaisePropertyChanged();
            }
        }

        private string _accountId;

        public string AccountId
        {
            get { return _accountId; }
            set
            {
                _accountId = value; RaisePropertyChanged();
                IsLoginEnabled = !string.IsNullOrEmpty(_accountId.Trim());
            }
        }

        private string _password;

        public string Password
        {
            get { return _password; }
            set { _password = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 维护用户 
        /// </summary>
        Account maintainAccount = null;

        private bool _loginMode = false;
        public bool LoginMode
        {
            get { return _loginMode; }
            set
            {
                _loginMode = value;
                if (_loginMode == true)
                    //updateNetAccounts();
                    SendLocalAccount();
                RaisePropertyChanged();
            }
        }

        private System.Windows.Visibility loginModeVisibility;

        public System.Windows.Visibility LoginModeVisibility
        {
            get { return loginModeVisibility; }
            set { loginModeVisibility = value; RaisePropertyChanged("LoginModeVisibility"); }
        }

        public LoginViewModel()
        {
            PreviewKeyDownEventCommand = new RelayCommand<KeyEventArgs>(PreviewKeyDownEventCommandExecute);
            PreviewKeyUpEventCommand = new RelayCommand<KeyEventArgs>(PreviewKeyUpEventCommandExecute);
            KeyUpEventCommand = new RelayCommand<KeyEventArgs>(KeyUpEventCommandExecute);
            KeyDownEventCommand = new RelayCommand<KeyEventArgs>(KeyDownEventCommandExecute);
            LoginCommand = new RelayCommand(LoginCommandExecute);
            ResetCommand = new RelayCommand(ResetCommandExecute);
            KeyboardCommand = new RelayCommand(KeyboardCommandExceute);
            //maintainAccount = new Account("1234239", "88888", AccountRole.Maintainer,true);
            UIScannerKeyboard1.SetWitchInputStates(true);
            LoginModeVisibility = Global.Instance.Sys.IsSingleMode ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        }

        private void SendLocalAccount()
        {
            NetCommandService.Instance.SendImportAccountsRequest();
            //var _fileName = "LowAuthorityAccounts.xml";
            //string _xmlFile = "D:\\SecurityScanner\\Database\\" + _fileName;
            //if (File.Exists(_xmlFile)) File.Delete(_xmlFile);

            //var controller = new AccountDbSet();
            //List<Account> allAccount;

            //try
            //{
            //    allAccount = controller.SelectAll();
            //    var LowerAuthorityAccounts = new List<Account>();
            //    foreach (var account in allAccount)
            //    {
            //        if (account.Role > 0)
            //        {
            //            LowerAuthorityAccounts.Add(account);
            //        }
            //    }
            //    ReadWriteXml.WriteXmlToFile<List<Account>>(LowerAuthorityAccounts, _xmlFile, Encoding.UTF8);
            //}
            //catch (Exception e)
            //{

            //}
        }
        private void updateNetAccounts()   //yxc 
        {
            string _accountxmlFile = "D:\\SecurityScanner\\Database\\" + "Accounts.xml";
            if (!File.Exists(_accountxmlFile))
                return;
            var controller = new AccountDbSet();
            try
            {
                List<Account> netAccountList = ReadWriteXml.ReadXmlFromFile<List<Account>>(_accountxmlFile, Encoding.UTF8);
                var idAccountParis = netAccountList.ToDictionary(account => account.AccountId, account => account);
                List<Account> oriNetAccountList = controller.SelectNetAccounts();
                if (netAccountList == null || netAccountList.Count == 0)
                {
                    // 本地保存的网络账号要和工作站一致
                    controller.RemoveRange(oriNetAccountList);
                    return;
                }
                foreach (var oriNetAccount in oriNetAccountList)
                {
                    if (idAccountParis.ContainsKey(oriNetAccount.AccountId))
                    {
                        // 更新时保留原有的分组
                        idAccountParis[oriNetAccount.AccountId].GroupName = oriNetAccount.GroupName;
                    }
                    else
                    {
                        controller.Remove(oriNetAccount);
                    }
                }
                var groupList = GroupInfoProvider.GetGroupNames();
                foreach (var item in netAccountList)
                {
                    item.IsNetAccount = true;
                    // 网络下发的账户全部权限置为操机员
                    item.Role = AccountRole.Operator;
                    item.IsEnable = true;
                    item.ActionTypes = "";
                    item.EffectsCompositions = "";
                    if (!groupList.Contains(item.GroupName))
                    {
                        item.GroupName = "";
                    }
                    controller.AddOrUpdate(item);
                }
                File.Delete(_accountxmlFile);
            }
            catch (Exception e)
            {
                Tracer.TraceError("[Account] Error occured in UpdateNetAccounts");
                Tracer.TraceException(e);
            }
        }
        private string EncryptByMD5(string cleartext)  //yxc
        {
            cleartext = cleartext.Trim();
            using (MD5CryptoServiceProvider md5Provider = new MD5CryptoServiceProvider())
            {
                byte[] hashBytes = md5Provider.ComputeHash(Encoding.UTF8.GetBytes("HIWINGDB_" + cleartext + "_SWSIMS"));
                var builder = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    builder.Append(hashBytes[i].ToString("x2"));
                }
                return builder.ToString().ToUpper();
            }
        }
        private void LoginCommandExecute()
        {
            Account loginAccount = null;
            LoginAccountManager.Service.LoginModeState = LoginMode;
            string password = Password;
            if (LoginMode)
                password = EncryptByMD5(password);
            try
            {
                var controller = new AccountDbSet();
                loginAccount = controller.Find(AccountId);
                this.MessengerInstance.Send(new ClearUpTipSelectedMessage());
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception, "Exception caught when try to login.");

                var builder = new StringBuilder(200);
                builder.Append(TranslationService.FindTranslation("Failed to login because of exception"))
                    .AppendLine()
                    .Append("Exception: ")
                    .Append(exception.Message);

                MessengerInstance.Send(new ShowDialogMessageAction("LoginWindow", TranslationService.FindTranslation("Exception"),
                        builder.ToString(), MetroDialogButtons.Ok, result => { }));

                return;
            }

            if (loginAccount != null && loginAccount.IsEnable)
            {
                if (string.Equals(loginAccount.Password, password, StringComparison.OrdinalIgnoreCase))
                {

                    if (loginAccount.IsActive)
                    {
                        bool isWorkReminder;
                        if (!ScannerConfig.Read(ConfigPath.IsWorkIntervalReminder, out isWorkReminder))
                        {
                            isWorkReminder = false;
                        }
                        TimeIntervalEnum timeIntervalEnum;
                        if (!ScannerConfig.Read(ConfigPath.WorkReminderTime, out timeIntervalEnum))
                        {
                            timeIntervalEnum = TimeIntervalEnum.HalfHour;
                        }
                        GalaSoft.MvvmLight.Messaging.Messenger.Default.Send<WorkReminderChangedMessage>(new WorkReminderChangedMessage(timeIntervalEnum, isWorkReminder));
                        CloseLoginWindow();

                        LoginAccountManager.Service.Login(loginAccount);

                        // 在本地网络登录成功后，需要将该信号发送至网络服务，以更新网络数据库中的人员登录状态
                        // 白云机场项目
                        if (LoginMode)
                        {
                            TRSNetWorkService.Service.UserLogin(AccountId, Password);
                        }
                        MessengerInstance.Send(new LoadKeyFunctionMessage());
                    }
                    else
                    {
                        MessengerInstance.Send(new ShowDialogMessageAction("LoginWindow", TranslationService.FindTranslation("Login Failed"),
                        TranslationService.FindTranslation("The account has been disabled. Please ask Administrator to activate it."), MetroDialogButtons.Ok, result => { }));

                    }


                }
                else
                {
                    MessengerInstance.Send(new ShowDialogMessageAction("LoginWindow", TranslationService.FindTranslation("Login Failed"),
                        TranslationService.FindTranslation("The password you typed is incorrect."), MetroDialogButtons.Ok, result => { }));
                }
            }
            else
            {
                MessengerInstance.Send(new ShowDialogMessageAction("LoginWindow", TranslationService.FindTranslation("Login Failed"),
                        TranslationService.FindTranslation("The account you typed does not exist."), MetroDialogButtons.Ok, result => { }));
            }

            //else
            //{
            //    int res;
            //    try
            //    {
            //        res = TRSNetWorkService.Service.UserLogin(AccountId, Password, out loginAccount);

            //        this.MessengerInstance.Send(new ClearUpTipSelectedMessage());
            //    }
            //    catch (Exception exception)
            //    {
            //        Tracer.TraceException(exception, "Exception caught when try to login.");

            //        var builder = new StringBuilder(200);
            //        builder.Append(TranslationService.FindTranslation("Failed to login because of exception"))
            //            .AppendLine()
            //            .Append("Exception: ")
            //            .Append(exception.Message);

            //        MessengerInstance.Send(new ShowDialogMessageAction("LoginWindow", TranslationService.FindTranslation("Exception"),
            //                builder.ToString(), MetroDialogButtons.Ok, result => { }));

            //        return;
            //    }

            //    if (res == 0)
            //    {
            //        if (loginAccount.IsActive)
            //        {
            //            CloseLoginWindow();
            //            LoginAccountManager.Service.Login(loginAccount);
            //        }
            //    }
            //    else if (res == -1)
            //    {
            //        MessengerInstance.Send(new ShowDialogMessageAction("LoginWindow", TranslationService.FindTranslation("Login Failed"),
            //            TranslationService.FindTranslation("The password you typed is incorrect."), MetroDialogButtons.Ok, result => { }));

            //    }
            //    else if (res == -2)
            //    {
            //        MessengerInstance.Send(new ShowDialogMessageAction("LoginWindow", TranslationService.FindTranslation("Login Failed"),
            //            TranslationService.FindTranslation("The account you typed does not exist."), MetroDialogButtons.Ok, result => { }));

            //    }
            //    else if (res == -3)
            //    {
            //        MessengerInstance.Send(new ShowDialogMessageAction("LoginWindow", TranslationService.FindTranslation("Login Failed"),
            //            TranslationService.FindTranslation("The account does not have permission to login to this position."), MetroDialogButtons.Ok, result => { }));

            //    }
            //    else if (res == -4)
            //    {
            //        MessengerInstance.Send(new ShowDialogMessageAction("LoginWindow", TranslationService.FindTranslation("Login Failed"),
            //            TranslationService.FindTranslation("Failed to login because of exception."), MetroDialogButtons.Ok, result => { }));

            //    }
            //}
        }

        private void KeyboardCommandExceute()
        {
            TouchKeyboardService.Service.OpenKeyboardWindow("HXkeyboard");
        }

        private void ResetCommandExecute()
        {
            AccountId = null;
            Password = null;
        }

        /// <summary>
        /// 关闭登陆窗口
        /// </summary>
        private void CloseLoginWindow()
        {
            UIScannerKeyboard1.SetWitchInputStates(false);
            this.MessengerInstance.Send(new CloseWindowMessage("LoginWindow"));
        }

        bool _isNumLocked = true;
        private void KeyDownEventCommandExecute(KeyEventArgs args)
        {
            switch (args.Key)
            {
                // 用户按下F3功能键或者计算机键盘中的Esc键时，关闭登录窗口
                case Key.F3:
                case Key.Escape:
                    CloseWindowInDebugMode();
                    args.Handled = true;
                    break;
                case Key.Enter:
                    LoginMode = !LoginMode;
                    break;
                // 用户按下F1功能键时，执行登录
                case Key.F1:
                    if (IsLoginEnabled)
                    {
                        LoginCommandExecute();
                    }
                    args.Handled = true;
                    break;
                case Key.O:
                    if (_isNumLocked)
                    {
                        SendKeyEvents.Press(Key.Back);
                        System.Threading.Thread.Sleep(100);
                        SendKeyEvents.Release(Key.Back);
                    }
                    break;
                case Key.I:
                    if (_isNumLocked)
                    {
                        SendKeyEvents.Press(Key.Tab);
                        System.Threading.Thread.Sleep(100);
                        SendKeyEvents.Release(Key.Tab);
                    }
                    break;
                case Key.System:
                    _isNumLocked = true;
                    break;
                case Key.F9:
                    _isNumLocked = false;
                    break;
            }
            bool isNumber = args.Key >= Key.D0 && args.Key <= Key.D9 || args.Key >= Key.NumPad0 && args.Key <= Key.NumPad9;
            bool isLetter = args.Key >= Key.A && args.Key <= Key.Z && args.KeyboardDevice.Modifiers != ModifierKeys.Shift;
            bool isCtrlA = args.Key == Key.A && args.KeyboardDevice.Modifiers == ModifierKeys.Control;
            bool isCtrlV = args.Key == Key.V && args.KeyboardDevice.Modifiers == ModifierKeys.Control;
            bool isBack = args.Key == Key.Back;
            bool isTab = args.Key == Key.Tab;
            bool isLeftOrRight = args.Key == Key.Left || args.Key == Key.Right;
            bool isUpOrDown = args.Key == Key.Up || args.Key == Key.Down;

            if (ScannerKeyboardPart.Keyboard.IsUSBCommonKeyboard)
            {
                if (_isNumLocked)
                {
                    if (isLetter)
                    {
                        args.Handled = true;
                        return;
                    }
                }

                if (isNumber && !_isNumLocked)
                {
                    ScannerKeyboardPart.Keyboard.AddKey((byte)args.Key);
                    args.Handled = true;
                    return;
                }
            }

            if (isLetter || isNumber || isCtrlA || isCtrlV || isBack || isTab || isLeftOrRight || isUpOrDown)
            {
                args.Handled = false;
            }
            else
            {
                args.Handled = true;
            }
        }

        [Conditional("DEBUG")]
        private void CloseWindowInDebugMode()
        {
            CloseLoginWindow();
        }

        private void KeyUpEventCommandExecute(KeyEventArgs args)
        {

        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        private void PreviewKeyUpEventCommandExecute(KeyEventArgs args)
        {
            ConveyorKeyEventsConvertor.HandlePreviewKeyUp(args);
        }

        /// <summary>
        /// </summary>
        /// <param name="args"></param>
        private void PreviewKeyDownEventCommandExecute(KeyEventArgs args)
        {
            ConveyorKeyEventsConvertor.HandlePreviewKeyDown(args);
        }
    }
}
