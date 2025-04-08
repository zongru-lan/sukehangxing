using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Services;
using UI.XRay.Flows.Services.Db;
using UI.XRay.Gui.Framework;
using UI.XRay.Parts.Keyboard;
using UI.XRay.Security.Scanner.ViewModel.Setting.Account;

namespace UI.XRay.Security.Scanner.ViewModel
{
    public class AddAccountViewModel : ViewModelBase
    {
        public RelayCommand AddCommand { get; set; }

        public RelayCommand CancelCommand { get; set; }
        public RelayCommand KeyboardCommand { get; set; }
        public RelayCommand<KeyEventArgs> PreviewKeyDownEventCommand { get; set; }

        private bool _isAddButtonEnabled;

        /// <summary>
        /// 添加按钮是否可用
        /// </summary>
        public bool IsAddButtonEnabled
        {
            get { return _isAddButtonEnabled; }
            set { _isAddButtonEnabled = value; RaisePropertyChanged(); }
        }

        private string _accountId;

        public string AccountId
        {
            get { return _accountId; }
            set
            {
                _accountId = value;
                IsAddButtonEnabled = !string.IsNullOrEmpty(_accountId) && !string.IsNullOrWhiteSpace(_password);

                if (!string.IsNullOrWhiteSpace(_accountId))
                {
                    _accountId = _accountId.Replace("_", "");
                }

                RaisePropertyChanged();
            }
        }

        private string _name;

        public string Name
        {
            get { return _name; }
            set { _name = value; RaisePropertyChanged(); }
        }


        private string _password;

        public string Password
        {
            get { return _password; }
            set
            {
                _password = value;
                IsAddButtonEnabled = !string.IsNullOrEmpty(_accountId) && !string.IsNullOrWhiteSpace(_password);
                RaisePropertyChanged();
            }
        }

        private AccountRole _role;

        public AccountRole Role
        {
            get { return _role; }
            set
            {
                _role = value;
                switch (_role)
                {
                    case AccountRole.System:
                        break;
                    case AccountRole.Admin:
                    case AccountRole.Maintainer:
                    case AccountRole.Operator:
                        _permissionValue = 0;
                        break;
                    default:
                        break;
                }
                RaisePropertyChanged();
            }
        }

        private string _groupName;
        public string GroupName
        {
            get { return _groupName; }
            set
            {
                _groupName = value;
                RaisePropertyChanged();
            }
        }

        public List<string> GroupList
        {
            get => GroupInfoProvider.GetGroupNames();
        }

        private int _permissionValue;

        public int PermissionValue
        {
            get { return _permissionValue; }
            set
            {
                _permissionValue = value;
                RaisePropertyChanged();
            }
        }


        private Visibility _idDuplicatedTextVisibility;

        /// <summary>
        /// 用户Id重复的提示性文字是否可见
        /// </summary>
        public Visibility IdDuplicatedTextVisibility
        {
            get { return _idDuplicatedTextVisibility; }
            set { _idDuplicatedTextVisibility = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<AccountRoleEnumString> AvailableAccountTypes { get; private set; }

        private AccountDbSet _manager;

        public AddAccountViewModel()
        {
            AddCommand = new RelayCommand(AddCommandExecute);
            CancelCommand = new RelayCommand(CancelCommandExecute);
            PreviewKeyDownEventCommand = new RelayCommand<KeyEventArgs>(PreviewKeyDownEventCommandExecute);
            KeyboardCommand = new RelayCommand(KeyboardCommandExecute);
            _manager = new AccountDbSet();

            // 当前登陆的是管理员，则可以添加操作员
            AvailableAccountTypes = AccountRoleStringsProvider.GetManageableRoleStringPairList();

            //默认用户级别为操作员
            Role = AccountRole.Operator;

            Password = Business.Entities.Account.DefaultPassword;

            IdDuplicatedTextVisibility = Visibility.Hidden;

            UIScannerKeyboard1.SetWitchInputStates(true);
        }

        bool _isNumLocked = true;
        private void PreviewKeyDownEventCommandExecute(KeyEventArgs args)
        {
            switch (args.Key)
            {
                case Key.F1:
                case Key.Enter:
                    if (IsAddButtonEnabled)
                    {
                        AddCommandExecute();
                    }
                    args.Handled = true;
                    break;

                case Key.F3:
                case Key.Escape:
                    CancelCommandExecute();
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
            //bool isCtrlA = args.Key == Key.A && args.KeyboardDevice.Modifiers == ModifierKeys.Control;
            //bool isCtrlV = args.Key == Key.V && args.KeyboardDevice.Modifiers == ModifierKeys.Control;
            //bool isBack = args.Key == Key.Back;
            //bool isTab = args.Key == Key.Tab;
            //bool isLeftOrRight = args.Key == Key.Left || args.Key == Key.Right;
            //bool isUpOrDown = args.Key == Key.Up || args.Key == Key.Down;

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

            //if (isLetter || isNumber || isCtrlA || isCtrlV || isBack || isTab || isLeftOrRight || isUpOrDown)
            //{
            //    args.Handled = false;
            //}
            //else
            //{
            //    args.Handled = true;
            //}
        }

        private void AddCommandExecute()
        {
            try
            {
                if (!IsAddButtonEnabled)
                {
                    return;
                }

                //获取所有用户，并进行对比，判断要添加的用户是否已经存在于数据库中
                var all = _manager.SelectAll();
                Account account1 = new Account(AccountId, Name, Password, Role, PermissionValue, true, true, groupName: GroupName);
                if (all != null)
                {

                    if (all.Any(a => (a.AccountId == AccountId)))
                    {
                        foreach (var item in all)
                        {
                            if (item.AccountId == AccountId && (item.IsEnable == false))
                            {
                                _manager.AddOrUpdate(account1);
                                break;
                            }
                            if (item.AccountId == AccountId && (item.IsEnable == true))
                            {
                                IdDuplicatedTextVisibility = Visibility.Visible;
                                return;
                            }
                        }
                    }

                }

                // 将新用户发送至用户管理视图
                this.MessengerInstance.Send(new AccountMessage(account1));
            }
            catch (Exception ex)
            {

            }

        }

        private void KeyboardCommandExecute()
        {
            TouchKeyboardService.Service.OpenKeyboardWindow("HXkeyboard");
        }

        private void CancelCommandExecute()
        {
            this.MessengerInstance.Send(new AccountMessage(null));

        }

        public override void Cleanup()
        {
            UIScannerKeyboard1.SetWitchInputStates(false);
            base.Cleanup();
        }
    }
}
