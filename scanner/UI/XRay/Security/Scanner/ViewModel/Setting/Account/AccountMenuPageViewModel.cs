using System;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Controllers;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Account
{
    public class AccountMenuPageViewModel : ViewModelBase
    {
        private IFramePageNavigationService _naviService;

        public RelayCommand NaviToAccountInfoCommand { get; set; }

        public RelayCommand NaviToAccountManageCommand { get; set; }

        public RelayCommand NaviToGroupManageCommand { get; set; }

        public RelayCommand NaviToAutoLoginCommand { get; set; }

        private bool _isAutoLoginChecked;

        public bool IsAutoLoginChecked
        {
            get { return _isAutoLoginChecked; }
            set
            {
                _isAutoLoginChecked = value;
                RaisePropertyChanged();
            }
        }

        private bool _isAccountInfoChecked;

        public bool IsAccountInfoChecked
        {
            get { return _isAccountInfoChecked; }
            set
            {
                _isAccountInfoChecked = value;
                RaisePropertyChanged();
            }
        }

        private Visibility _autoLoginMenuVisibility;

        public Visibility AutoLoginMenuVisibility
        {
            get { return _autoLoginMenuVisibility; }
            set { _autoLoginMenuVisibility = value; RaisePropertyChanged(); }
        }

        private Visibility _manageOtherMenuVisibility;

        public Visibility ManageOtherMenuVisibility
        {
            get { return _manageOtherMenuVisibility; }
            set { _manageOtherMenuVisibility = value; RaisePropertyChanged(); }
        }

        private Visibility _manageGroupsMenuVisibility;

        public Visibility ManageGroupsMenuVisibility
        {
            get { return _manageGroupsMenuVisibility; }
            set { _manageGroupsMenuVisibility = value; RaisePropertyChanged(); }
        }

        private bool _isAccountManageChecked;

        public bool IsAccountManageChecked
        {
            get
            {
                return _isAccountManageChecked;
            }
            set
            {
                _isAccountManageChecked = value;
                RaisePropertyChanged();
            }
        }

        private bool _isGroupManageChecked;
        public bool IsGroupManageChecked
        {
            get
            {
                return _isGroupManageChecked;
            }
            set
            {
                _isGroupManageChecked = value;
                RaisePropertyChanged();
            }
        }

        private Uri _currentPageUri;

        public Uri CurrentPageUri
        {
            get { return _currentPageUri; }
            set
            {
                _currentPageUri = value;
                RaisePropertyChanged();
            }
        }

        public AccountMenuPageViewModel(IFramePageNavigationService service)
        {
            if (service == null)
            {
                throw new ArgumentNullException("service");
            }

            _naviService = service;

            CheckButton(service.CurrentPageNavigation.PageKey);

            NaviToAccountInfoCommand = new RelayCommand(NaviToAccountInfoCommandExecute);
            NaviToAccountManageCommand = new RelayCommand(NaviToAccountManageCommandExecute);
            NaviToGroupManageCommand = new RelayCommand(NaviToGroupManageCommandExecute);
            NaviToAutoLoginCommand = new RelayCommand(NaviToAutoLoginCommandExecute);

            if (LoginAccountManager.Service.HasLogin)
            {
                if (LoginAccountManager.Service.CurrentAccount.Role == AccountRole.Operator)
                {
                    AutoLoginMenuVisibility = Visibility.Collapsed;
                    ManageOtherMenuVisibility = Visibility.Collapsed;
                    ManageGroupsMenuVisibility = Visibility.Collapsed;
                }
                else
                {
                    AutoLoginMenuVisibility = Visibility.Visible;
                    ManageOtherMenuVisibility = Visibility.Visible;
                    ManageGroupsMenuVisibility = Visibility.Visible;
                }
            }
            else
            {
                AutoLoginMenuVisibility = Visibility.Collapsed;
                ManageOtherMenuVisibility = Visibility.Collapsed;
                ManageGroupsMenuVisibility = Visibility.Collapsed;
            }
        }

        private void CheckButton(string pageKey)
        {
            if (pageKey == "AutoLoginSettingPage")
            {
                IsAutoLoginChecked = true;
            }
            else if (pageKey == "AccountPage")
            {
                IsAccountInfoChecked = true;
            }
            else if (pageKey == "AccountManagePage")
            {
                IsAccountManageChecked = true;
            }
            else if (pageKey == "ManageGroupsPage")
            {
                IsGroupManageChecked = true;
            }
        }

        private void NaviToAccountInfoCommandExecute()
        {
            _naviService.ShowPage("AccountPage");
        }

        private void NaviToAccountManageCommandExecute()
        {
            _naviService.ShowPage("ManageOtherAccountsPage");
        }

        private void NaviToGroupManageCommandExecute()
        {
            _naviService.ShowPage("ManageGroupsPage");
        }

        private void NaviToAutoLoginCommandExecute()
        {
            _naviService.ShowPage("AutoLoginSettingPage");
        }
    }
}
