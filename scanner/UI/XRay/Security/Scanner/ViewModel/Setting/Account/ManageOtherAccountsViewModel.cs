using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Input;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Common.Utilities;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Services;
using UI.XRay.Flows.Services.Db;
using UI.XRay.Flows.TRSNetwork;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Account
{
    public class ManageOtherAccountsViewModel : PageViewModelBase
    {


        #region commands and event commands
        public RelayCommand SaveCommand { get; set; }

        public RelayCommand CancelCommand { get; set; }

        public RelayCommand AddNewAccountCommand { get; set; }

        public RelayCommand DeleteAccountCommand { get; set; }

        public RelayCommand ResetCurrentAccountPasswordCommand { get; set; }

        public RelayCommand CellEditEndingEventCommand { get; set; }

        public RelayCommand DataGridSelectionChanged { get; set; }

        public RelayCommand SavePermissionCmd { get; set; }

        public RelayCommand ExportCommand { get; set; }

        public RelayCommand ImportCommand { get; set; }

        public RelayCommand ExportToNetWorkCommand { get; set; }
        public RelayCommand WorkIntervalReminderChangedCommand { get; set; }
        #endregion

        #region  properties

        private bool _hasChanges;
        /// <summary>
        /// 当前是否添加、删除或修改了用户
        /// </summary>
        public bool HasChanges
        {
            get { return _hasChanges; }
            set { _hasChanges = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<Business.Entities.Account> _manageableAccounts;

        public ObservableCollection<Business.Entities.Account> ManageableAccounts
        {
            get { return _manageableAccounts; }
            set
            {
                _manageableAccounts = value;
                RaisePropertyChanged();
            }
        }

        private List<Business.Entities.Account> _beforeChangedAccounts;

        /// <summary>
        /// 即将被删除的账户。即存储用户在界面中点击删除后，记录被删除的用户
        /// </summary>
        private List<Business.Entities.Account> AccountsTobeRemoved { get; set; }

        /// <summary>
        /// 重置时使用的默认密码
        /// </summary>
        public string DefaultPassword { get; set; }

        /// <summary>
        /// 数据库中存在的所有账号信息
        /// </summary>
        private List<Business.Entities.Account> _allAccountsInDb;

        private Business.Entities.Account _currentAccount;

        public Business.Entities.Account CurrentAccount
        {
            get { return _currentAccount; }
            set { _currentAccount = value; RaisePropertyChanged(); }
        }

        private Visibility _operatorPermissionVisibility = Visibility.Collapsed;

        public Visibility OperatorPermissionVisibility
        {
            get { return _operatorPermissionVisibility; }
            set { _operatorPermissionVisibility = value; RaisePropertyChanged(); }
        }

        private Visibility _adminPermissionVisibility = Visibility.Collapsed;

        public Visibility AdminPerssionVisibility
        {
            get { return _adminPermissionVisibility; }
            set { _adminPermissionVisibility = value; RaisePropertyChanged(); }
        }


        private PermissionList _permission;

        public PermissionList Permission
        {
            get { return _permission; }
            set { _permission = value; RaisePropertyChanged(); }
        }

        private Visibility _exportVisibility;

        public Visibility ExportVisibility
        {
            get { return _exportVisibility; }
            set { _exportVisibility = value; RaisePropertyChanged(); }
        }

        #region 换班提醒
        public List<float> WorkIntervalList { get; private set; }
        /// <summary>
        /// 是否开启工作时长间隔提醒
        /// </summary>
        private bool _isWorkIntervalRemind;
        public bool IsWorkIntervalRemind
        {
            get => _isWorkIntervalRemind;
            set { _isWorkIntervalRemind = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 工作时长提醒间隔
        /// </summary>
        private float _selectedWorkDurationInterval;
        public float SelectedWorkDurationInterval
        {
            get => _selectedWorkDurationInterval;
            set { _selectedWorkDurationInterval = value; RaisePropertyChanged(); }
        }
        #endregion
        #endregion properties

        private AccountDbSet _manager;
        public ManageOtherAccountsViewModel()
        {
            SaveCommand = new RelayCommand(SaveCommandExecute);
            CancelCommand = new RelayCommand(CancelCommandExecute);
            AddNewAccountCommand = new RelayCommand(AddNewAccountCommandExecute);
            DeleteAccountCommand = new RelayCommand(DeleteAccountCommandExecute);
            ResetCurrentAccountPasswordCommand = new RelayCommand(ResetCurrentAccountPasswordCommandExecute);
            CellEditEndingEventCommand = new RelayCommand(CellEditEndingEventCommandExecute);
            DataGridSelectionChanged = new RelayCommand(DataGridSelectionChangedExecute);
            SavePermissionCmd = new RelayCommand(SavePermissionCmdExecute);
            ExportCommand = new RelayCommand(ExportCommandExecute);
            ImportCommand = new RelayCommand(ImportCommandExecute);
            ExportToNetWorkCommand = new RelayCommand(ExportToNetWorkCommandExecute);  //yxc
            _manager = new AccountDbSet();
            //AvailableAccountTypes = AccountTypeEnumString.GetAdminOperatorPairs();

            ReLoad();
            DefaultPassword = Business.Entities.Account.DefaultPassword;
            _operatorPermissionVisibility = Visibility.Collapsed;
            if (LoginAccountManager.Service.CurrentAccount.Role == AccountRole.System || LoginAccountManager.Service.CurrentAccount.Role == AccountRole.Admin
                || LoginAccountManager.Service.CurrentAccount.Role == AccountRole.Maintainer)
            {
                _exportVisibility = Visibility.Visible;
            }
            else
            {
                _exportVisibility = Visibility.Collapsed;
            }
            OperatorPermissionVisibility = Visibility.Collapsed;
            AdminPerssionVisibility = Visibility.Collapsed;
        }

        #region 换班提醒
        
        #endregion

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

        private void ReLoad(bool isReloadView = true)
        {
            _manager = new AccountDbSet();

            // 角色低于当前登陆角色的用户
            List<Business.Entities.Account> lessRoleAccounts = null;

            try
            {
                _allAccountsInDb = _manager.SelectAll();

                if (LoginAccountManager.Service.CurrentAccount != null)
                {
                    if (LoginAccountManager.Service.CurrentAccount.Role == AccountRole.System)
                    {

                    }
                }

                lessRoleAccounts =
                    LoginAccountManager.Service.CurrentAccount != null
                    ? _manager.SelectAccountsWithRoleLessThan(LoginAccountManager.Service.CurrentAccount.Role).Where(a => (a.IsEnable == true)).ToList()
                    : _allAccountsInDb;
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            ManageableAccounts = isReloadView ? new ObservableCollection<Business.Entities.Account>(lessRoleAccounts): ManageableAccounts;
            
            _beforeChangedAccounts = GetCopyOfAccounts(ManageableAccounts);

            AccountsTobeRemoved = new List<Business.Entities.Account>(5);

            HasChanges = false;
        }

        private void ResetCurrentAccountPasswordCommandExecute()
        {
            CurrentAccount.Password = Business.Entities.Account.DefaultPassword;
            CurrentAccount.DisplayPassword = CurrentAccount.Password;
        }

        /// <summary>
        /// 事件命令：当用户编辑完某个单元格，并且结束编辑后触发
        /// </summary>
        private void CellEditEndingEventCommandExecute()
        {
            // 一旦结束了对某个单元的编辑，即认定为发生了变更，使能部分操作
            HasChanges = true;
        }
        private void DataGridSelectionChangedExecute()
        {
            if (CurrentAccount != null)
            {
                if (CurrentAccount.Role == AccountRole.Operator)
                {
                    OperatorPermissionVisibility = Visibility.Visible;
                    AdminPerssionVisibility = Visibility.Collapsed;
                }
                else if (CurrentAccount.Role == AccountRole.Admin)
                {
                    OperatorPermissionVisibility = Visibility.Collapsed;
                    AdminPerssionVisibility = Visibility.Visible;
                }
                else
                {
                    OperatorPermissionVisibility = Visibility.Collapsed;
                    AdminPerssionVisibility = Visibility.Collapsed;
                }
            }
            else
            {
                OperatorPermissionVisibility = Visibility.Collapsed;
                AdminPerssionVisibility = Visibility.Collapsed;
            }
            Permission = PermissionService.Service.GetPermissionList(CurrentAccount);
        }
        private void SavePermissionCmdExecute()
        {
            int value = PermissionService.Service.GetPermissionValue(Permission);
            CurrentAccount.PermissionValue = value;
            HasChanges = true;
        }

        

        private void ExportCommandExecute()
        {
            //ReLoad();
            var msg = new ShowFolderBrowserDialogMessageAction("SettingWindow", s =>
            {
                if (s != null)
                {
                    var filename = ConfigHelper.GetExportFileName(s, "Accounts");
                    PermissionService.Service.ExportAccountList(ManageableAccounts, filename);
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessengerInstance.Send(new ShowDialogMessageAction("SettingWindow",
                            "", TranslationService.FindTranslation("Dump completely"), MetroDialogButtons.Ok,
                            result =>
                            {

                            }));
                    });
                    new OperationRecordService().AddRecord(new OperationRecord()
                    {
                        AccountId = LoginAccountManager.Service.CurrentAccount.AccountId,
                        OperateUI = OperationUI.ManageOtherAccounts,
                        OperateTime = DateTime.Now,
                        OperateObject = "Account",
                        OperateCommand = OperationCommand.Export,
                        OperateContent = ConfigHelper.AddQuotationForPath(filename),
                    });
                }
            });
            MessengerInstance.Send(msg);
        }

        private void ExportToNetWorkCommandExecute()   //yxc
        {
            //ReLoad();
            // 命名是历史遗留问题
            var _fileName = "LowAuthorityAccounts.xml";
            string _xmlFile = "D:\\SecurityScanner\\Database\\" + _fileName;
            if (File.Exists(_xmlFile)) File.Delete(_xmlFile);
            try
            {
                if (SystemStatus.Instance.IsDatabaseConnected && SystemStatus.Instance.IsDatabaseServerConnected)
                {
                    var accountsToBeExported = new List<UI.XRay.Business.Entities.Account>(5);
                    foreach (var account in ManageableAccounts)
                    {
                        if (account.IsExportToNet)
                        {
                            var encryptedAccount = new Business.Entities.Account
                            {
                                AccountId = account.AccountId,
                                Name = account.Name,
                                Password = account.IsNetAccount ? account.Password : EncryptByMD5(account.Password),
                                Role = account.Role,
                                IsActive = account.IsActive,
                                PermissionValue = account.PermissionValue,
                                Description = $"Net_{account.GroupName}",
                            };
                            accountsToBeExported.Add(encryptedAccount);
                        }
                    }
                    ReadWriteXml.WriteXmlToFile<List<UI.XRay.Business.Entities.Account>>(accountsToBeExported, _xmlFile, Encoding.UTF8);
                    byte[] Data = ReadFileToBytes.ReadDataFromFile(_xmlFile);
                    NetCommandService.Instance.SendResultFile(_fileName, Data, Flows.TRSNetwork.Models.FileType.Accounts);

                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                                    {
                                        MessengerInstance.Send(new ShowDialogMessageAction("SettingWindow",
                                            "", TranslationService.FindTranslation("Dump to network completely"), MetroDialogButtons.Ok,
                                            result =>
                                            {

                                            }));
                                    });
                    ReLoad();

                }
                else
                {
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessengerInstance.Send(new ShowDialogMessageAction("SettingWindow",
                            "", TranslationService.FindTranslation("Send Failed"), MetroDialogButtons.Ok,
                            result =>
                            {

                            }));
                    });
                }
            }
            catch (Exception e)
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessengerInstance.Send(new ShowDialogMessageAction("SettingWindow",
                        "", TranslationService.FindTranslation("Send Failed"), MetroDialogButtons.Ok,
                        result =>
                        {

                        }));
                });
            }
        }
        private void ImportCommandExecute()
        {
            var msg = new ShowOpenFilesDialogMessageAction("SettingWindow", "Xml file | *.xml", false, s =>
            {
                if (s != null && s.Length > 0)
                {
                    ImportCommandExecute(s[0]);
                }
            });
            MessengerInstance.Send(msg);
        }
        public void ImportCommandExecute(string path)
        {
            AccountsTobeRemoved.Clear();
            ReLoad();

            PermissionService.Service.ImportAccountList(ManageableAccounts, path);
            HasChanges = true;
            System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MessengerInstance.Send(new ShowDialogMessageAction("SettingWindow",
                    "", TranslationService.FindTranslation("Import completely"), MetroDialogButtons.Ok,
                    result =>
                    {

                    }));
            });

            new OperationRecordService().AddRecord(new OperationRecord()
            {
                AccountId = LoginAccountManager.Service.CurrentAccount.AccountId,
                OperateUI = OperationUI.ManageOtherAccounts,
                OperateTime = DateTime.Now,
                OperateObject = "Account",
                OperateCommand = OperationCommand.Import,
                OperateContent = ConfigHelper.AddQuotationForPath(path),
            });
        }

        private void CancelCommandExecute()
        {
            ReLoad();
        }

        /// <summary>
        /// 保存所有变更
        /// </summary>
        private void SaveCommandExecute()
        {
            try
            {
                _manager.AddOrUpdate(ManageableAccounts);

                _manager.RemoveRange(AccountsTobeRemoved);

                foreach (var account in AccountsTobeRemoved)
                {
                    var sb = new StringBuilder();
                    sb.Append("AccountId:").Append(account.AccountId).Append(",");
                    sb.Append("Password:").Append(account.Password).Append(",");
                    sb.Append("Role:").Append(account.Role.ToString()).Append(",");
                    sb.Append("IsActive:").Append(account.IsActive).Append(",");
                    sb.Append("IsEnable:").Append(account.IsEnable).Append(",");
                    sb.Append("Description:").Append(account.Description);

                    new OperationRecordService().AddRecord(new OperationRecord()
                    {
                        AccountId = LoginAccountManager.Service.CurrentAccount.AccountId,
                        OperateUI = OperationUI.ManageOtherAccounts,
                        OperateTime = DateTime.Now,
                        OperateObject = "Account",
                        OperateCommand = OperationCommand.Delete,
                        OperateContent = sb.ToString(),
                    });
                }

                foreach (var account in ManageableAccounts)
                {
                    var origin = _beforeChangedAccounts.Where(a => a.AccountId == account.AccountId).ToList();
                    if (origin == null || origin.Count < 1)
                    {
                        var sb = new StringBuilder();
                        sb.Append("AccountId:").Append(account.AccountId).Append(",");
                        sb.Append("Password:").Append(account.Password).Append(",");
                        sb.Append("Role:").Append(account.Role.ToString()).Append(",");
                        sb.Append("IsActive:").Append(account.IsActive).Append(",");
                        sb.Append("IsEnable:").Append(account.IsEnable).Append(",");
                        sb.Append("PermissionValue:").Append(account.PermissionValue).Append(",");
                        sb.Append("Description:").Append(account.Description);

                        new OperationRecordService().AddRecord(new OperationRecord()
                        {
                            AccountId = LoginAccountManager.Service.CurrentAccount.AccountId,
                            OperateUI = OperationUI.ManageOtherAccounts,
                            OperateTime = DateTime.Now,
                            OperateObject = "Account",
                            OperateCommand = OperationCommand.Create,
                            OperateContent = sb.ToString(),
                        });
                        new OperationRecordService().AddRecord(new OperationRecord()
                        {
                            AccountId = LoginAccountManager.Service.CurrentAccount.AccountId,
                            OperateUI = OperationUI.ManageOtherAccounts,
                            OperateTime = DateTime.Now,
                            OperateObject = account.AccountId,
                            OperateCommand = OperationCommand.Setting,
                            OperateContent = PermissionService.Service.GetPermissionString(account),
                        });
                        continue;
                    }
                    else
                    {
                        var sb = new StringBuilder();
                        if (origin[0].Password != account.Password)
                        {
                            new OperationRecordService().AddRecord(new OperationRecord()
                            {
                                AccountId = LoginAccountManager.Service.CurrentAccount.AccountId,
                                OperateUI = OperationUI.ManageOtherAccounts,
                                OperateTime = DateTime.Now,
                                OperateObject = account.AccountId,
                                OperateCommand = OperationCommand.Setting,
                                OperateContent = "Password:" + origin[0].Password + "-" + account.Password,
                            });
                        }

                        if (origin[0].Role != account.Role)
                        {
                            new OperationRecordService().AddRecord(new OperationRecord()
                            {
                                AccountId = LoginAccountManager.Service.CurrentAccount.AccountId,
                                OperateUI = OperationUI.ManageOtherAccounts,
                                OperateTime = DateTime.Now,
                                OperateObject = account.AccountId,
                                OperateCommand = OperationCommand.Setting,
                                OperateContent = "Role:" + origin[0].Role + "-" + account.Role,
                            });
                        }
                        if (origin[0].PermissionValue != account.PermissionValue)
                        {
                            new OperationRecordService().AddRecord(new OperationRecord()
                            {
                                AccountId = LoginAccountManager.Service.CurrentAccount.AccountId,
                                OperateUI = OperationUI.ManageOtherAccounts,
                                OperateTime = DateTime.Now,
                                OperateObject = account.AccountId,
                                OperateCommand = OperationCommand.Setting,
                                OperateContent = PermissionService.Service.GetPermissionString(origin[0]) + "-" + PermissionService.Service.GetPermissionString(account)
                            });
                        }
                        if (origin[0].IsActive != account.IsActive)
                        {
                            new OperationRecordService().AddRecord(new OperationRecord()
                            {
                                AccountId = LoginAccountManager.Service.CurrentAccount.AccountId,
                                OperateUI = OperationUI.ManageOtherAccounts,
                                OperateTime = DateTime.Now,
                                OperateObject = account.AccountId,
                                OperateCommand = OperationCommand.Setting,
                                OperateContent = "IsActive:" + origin[0].IsActive + "-" + account.IsActive,
                            });
                        }
                        if (origin[0].IsEnable != account.IsEnable)
                        {
                            new OperationRecordService().AddRecord(new OperationRecord()
                            {
                                AccountId = LoginAccountManager.Service.CurrentAccount.AccountId,
                                OperateUI = OperationUI.ManageOtherAccounts,
                                OperateTime = DateTime.Now,
                                OperateObject = account.AccountId,
                                OperateCommand = OperationCommand.Setting,
                                OperateContent = "IsEnable:" + origin[0].IsEnable + "-" + account.IsEnable,
                            });
                        }
                        if (origin[0].Description != account.Description)
                        {

                            new OperationRecordService().AddRecord(new OperationRecord()
                            {
                                AccountId = LoginAccountManager.Service.CurrentAccount.AccountId,
                                OperateUI = OperationUI.ManageOtherAccounts,
                                OperateTime = DateTime.Now,
                                OperateObject = account.AccountId,
                                OperateCommand = OperationCommand.Setting,
                                OperateContent = "Description:" + origin[0].Description + "-" + account.Description,
                            });
                        }
                    }


                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
            finally
            {
                // 用户点击保存后，重新加载数据库中的最新用户
                ReLoad(false);

            }


        }

        private bool _isAddingAccount = false;
        /// <summary>
        /// 添加一个新用户
        /// </summary>
        private void AddNewAccountCommandExecute()
        {
            if (_isAddingAccount)
            {
                return;
            }

            _isAddingAccount = true;

            var msg = new ShowAddAccountWindowAction("SettingWindow", account1 =>
            {
                if (account1 != null)
                {
                    // 如果当前正在编辑的用户列表或者数据库中已经存在与此要添加的新账户的账户名一致的账户，则不允许添加
                    //if (ManageableAccounts.FirstOrDefault(account => string.Equals(account.AccountId, account1.AccountId, StringComparison.OrdinalIgnoreCase)) != null
                    //    || _manager.Find(account1.AccountId) != null)
                    //{
                    //    Messenger.Default.Send(new ShowDialogMessageAction("SettingWindow", TranslationService.FindTranslation("Error"),
                    //        TranslationService.FindTranslation("The account id already exists!"), MetroDialogButtons.Ok, result => { }));
                    //}
                    // 账户名不能为空
                    if (string.IsNullOrWhiteSpace(account1.AccountId)) //yxc
                    {
                        Messenger.Default.Send(new ShowDialogMessageAction("SettingWindow", TranslationService.FindTranslation("Error"),
                            TranslationService.FindTranslation("Account id can not be empty!"), MetroDialogButtons.Ok, result => { }));
                    }
                    // 账户名和密码长度限制在16位以内
                    else if (account1.AccountId.Length > 16 || account1.Password.Length > 16)
                    {
                        Messenger.Default.Send(new ShowDialogMessageAction("SettingWindow", TranslationService.FindTranslation("Error"),
                            TranslationService.FindTranslation("Account id and password must contain less than 16 characters!"), MetroDialogButtons.Ok, result => { }));
                    }
                    // 密码只能包含数字和字母
                    else if (!isPasswordOrAccountIDValid(account1.Password) || !isPasswordOrAccountIDValid(account1.AccountId))
                    {
                        Messenger.Default.Send(new ShowDialogMessageAction("SettingWindow", TranslationService.FindTranslation("Error"),
                            TranslationService.FindTranslation("Account id and password can only contain numbers and letters!"), MetroDialogButtons.Ok, result => { }));
                    }
                    else
                    {
                        ManageableAccounts.Add(account1);

                        // 如果刚刚添加的新账户，也存在于删除列表中，则将其从删除列表中移除（场景：先添加一个X，删除，然后再次添加X）
                        AccountsTobeRemoved.RemoveAll(account => account.AccountId == account1.AccountId);

                        HasChanges = true;
                    }
                    SaveCommandExecute();
                }

                _isAddingAccount = false;

            });

            Messenger.Default.Send(msg);

        }

        private bool isPasswordOrAccountIDValid(string password)
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

        /// <summary>
        /// 删除选中账户的命令执行
        /// </summary>
        private void DeleteAccountCommandExecute()
        {
            if (CurrentAccount != null)
            {
                // 将要被删除的用户记录下来，然后从显示列表中移除
                // 如果是删除了本次刚刚新添加但尚未保存的账户，则不会标记为删除
                //if (_allAccountsInDb.Exists(account => account.AccountId == CurrentAccount.AccountId))
                //{
                //    AccountsTobeRemoved.Add(CurrentAccount);
                //}
                var account1 = _manager.Find(CurrentAccount.AccountId);
                _manager.Remove(account1);
                CurrentAccount.IsEnable = false;
                _manager.Add(CurrentAccount);

                ManageableAccounts.Remove(CurrentAccount);
                HasChanges = true;
            }
        }


        private List<Business.Entities.Account> GetCopyOfAccounts(ObservableCollection<Business.Entities.Account> accounts)
        {
            var accountList = new List<Business.Entities.Account>();
            foreach (var account in accounts)
            {
                var tmp = new Business.Entities.Account(account.AccountId, account.Name, account.Password, account.Role, account.PermissionValue, account.IsActive, account.IsEnable);
                tmp.Description = account.Description;
                accountList.Add(tmp);
            }
            return accountList;
        }

        public override void OnKeyDown(KeyEventArgs args)
        {
            // 注意：在使用快捷键完成操作时，需要先通知视图中的datagrid提交更改，否则正在通过键盘导航更改的项会自动被撤销
            //switch (args.Key)
            //{
            //    case Key.D1:
            //        Messenger.Default.Send(new CommitChangesMessage(), "AccountEdit");
            //        AddNewAccountCommandExecute();
            //        args.Handled = true;
            //        break;

            //    case Key.D2:
            //        if (HasChanges)
            //        {
            //            Messenger.Default.Send(new CommitChangesMessage(), "AccountEdit");
            //            SaveCommandExecute();
            //            args.Handled = true;
            //        }
            //        break;

            //    case Key.D3:
            //        if (HasChanges)
            //        {
            //            Messenger.Default.Send(new CommitChangesMessage(), "AccountEdit");
            //            CancelCommandExecute();
            //            args.Handled = true;
            //        }
            //        break;
            //}
        }
    }
}
