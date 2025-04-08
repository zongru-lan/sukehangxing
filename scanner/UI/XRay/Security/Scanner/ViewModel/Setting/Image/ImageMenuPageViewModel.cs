using System;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using UI.Common.Tracers;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Controllers;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Image
{
    public class ImageMenuPageViewModel : ViewModelBase
    {
        private IFramePageNavigationService _naviService;

        public RelayCommand NaviToImsPageCommand { get; set; }

        public RelayCommand NaviToImageSettingCommand { get; set; }

        /// <summary>
        /// 图像设置页按钮是否可见，仅限于系统人员操作
        /// </summary>
        public Visibility ImageSettingPageVisibility
        {
            get { return _imageSettingPageVisibility; }
            set { _imageSettingPageVisibility = value; RaisePropertyChanged();}
        }

        private Visibility _imageSettingPageVisibility = Visibility.Collapsed;


        public ImageMenuPageViewModel(IFramePageNavigationService service)
        {
            try
            {
                NaviToImsPageCommand = new RelayCommand(NaviToImsPageCommandExecute);
                NaviToImageSettingCommand = new RelayCommand(NaviToImageSettingCommandExecute);
                _naviService = service;

                if (LoginAccountManager.Service.HasLogin)
                {
                    if (LoginAccountManager.Service.IsCurrentRoleEqualOrGreaterThan(AccountRole.Maintainer))
                    {
                        ImageSettingPageVisibility = Visibility.Visible;
                    }
                    else if (LoginAccountManager.Service.CurrentAccount.Role == AccountRole.Admin && LoginAccountManager.Service.CurrentAccountPermission.CanChangeImageSettings)
                    {
                        ImageSettingPageVisibility = Visibility.Visible;
                    }
                    else
                    {
                        ImageSettingPageVisibility = Visibility.Collapsed;
                    }                
                }

                MessengerInstance.Register<NotificationMessage>(this, OnChangeImageManagerViewMode);
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        private void OnChangeImageManagerViewMode(NotificationMessage changeImageManagerViewModeMsg)
        {
            _naviService.ShowPage(changeImageManagerViewModeMsg.Notification);
        }

        private void NaviToImsPageCommandExecute()
        {
            _naviService.ShowPage("ImsPage");
        }

        private void NaviToImageSettingCommandExecute()
        {
            _naviService.ShowPage("ImageSettingPage");
        }

        public override void Cleanup()
        {
            MessengerInstance.Unregister(this);
            base.Cleanup();
        }
    }
}
