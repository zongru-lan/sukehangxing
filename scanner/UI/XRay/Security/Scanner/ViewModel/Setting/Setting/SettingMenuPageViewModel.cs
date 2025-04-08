using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using UI.XRay.Gui.Framework;
using UI.XRay.Flows.Controllers;
using UI.XRay.Business.Entities;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Setting
{
    public class SettingMenuPageViewModel : ViewModelBase
    {
        public RelayCommand NaviToIntelliSensePageCommand { get; set; }

        public RelayCommand NaviToObjectCounterPageCommand { get; set; }

        public RelayCommand NaviToFunctionKyesPageCommand { get; set; }

        public RelayCommand NaviToDiskSpaceManagePageCommand { get; set; }

        private IFramePageNavigationService _navigationService;

        private Visibility _diskSettingVisibility = Visibility.Collapsed;

        public Visibility DiskSettingVisibility
        {
            get { return _diskSettingVisibility; }
            set { _diskSettingVisibility = value; RaisePropertyChanged(); }
        }

        private Visibility _adminSettingVisibility = Visibility.Collapsed;

        public Visibility AdminSettingVisibility
        {
            get { return _adminSettingVisibility; }
            set { _adminSettingVisibility = value; RaisePropertyChanged(); }
        }
                

        public SettingMenuPageViewModel(IFramePageNavigationService service)
        {
            _navigationService = service;
            NaviToIntelliSensePageCommand = new RelayCommand(NaviToIntelliSensePageCommandExecute);
            NaviToObjectCounterPageCommand = new RelayCommand(NaviToObjectCounterPageCommandExecute);
            NaviToFunctionKyesPageCommand = new RelayCommand(NaviToFunctionKyesPageCommandExecute);
            NaviToDiskSpaceManagePageCommand = new RelayCommand(NaviToDiskSpaceManagePageCommandExecute);

            if (LoginAccountManager.Service.HasLogin)
            {
                if (LoginAccountManager.Service.IsCurrentRoleEqualOrGreaterThan(AccountRole.Maintainer))
                {
                    DiskSettingVisibility = Visibility.Visible;
                }
                else if (LoginAccountManager.Service.CurrentAccount.Role == AccountRole.Admin && LoginAccountManager.Service.CurrentAccountPermission.CanManageDisk)
                {
                    DiskSettingVisibility = Visibility.Visible;
                }
                else
                {
                    DiskSettingVisibility = Visibility.Collapsed;
                }

                if (LoginAccountManager.Service.IsCurrentRoleEqualOrGreaterThan(AccountRole.Admin))
                {
                    AdminSettingVisibility = Visibility.Visible;
                }
                else
                {
                    AdminSettingVisibility = Visibility.Collapsed;
                }
            }
        }

        private void NaviToFunctionKyesPageCommandExecute()
        {
            _navigationService.ShowPage("FunctionKeysPage");
        }

        private void NaviToIntelliSensePageCommandExecute()
        {
            _navigationService.ShowPage("IntelliSensePage");
        }

        private void NaviToObjectCounterPageCommandExecute()
        {
            _navigationService.ShowPage("ObjectCounterPage");
        }

        private void NaviToDiskSpaceManagePageCommandExecute()
        {
            _navigationService.ShowPage("DiskSpaceManagePage");
        }
    }
}
