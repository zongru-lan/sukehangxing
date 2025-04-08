using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Windows;
using UI.Common.Tracers;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Services;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Menu
{
    /// <summary>
    /// 设置菜单页的视图模型
    /// </summary>
    public class SystemMenuPageViewModel : ViewModelBase
    {
        private IFramePageNavigationService _naviService;

        

        #region Commands
        public RelayCommand NavigateToAccountPageCommand { get; set; }

        public RelayCommand NavigateToImagePageCommand { get; set; }

        public RelayCommand NavigateToDevicePageCommand { get; set; }

        public RelayCommand NavigateToSettingPageCommand { get; set; }

        public RelayCommand NavigateToAboutPageCommand { get; set; }

        public RelayCommand NavigateToRecordsPageCommand { get; set; }

        public RelayCommand NavigateToTrainingPageCommand { get; set; }

        public RelayCommand NavigateToTipPageCommand { get; set; }

        public RelayCommand ShutdownCommand { get; set; }

        public RelayCommand ExitToDesktopCommand { get; set; }

        public RelayCommand LogoutCommand { get; set; }

       
        /// <summary>
        /// 手动校正图像
        /// </summary>
        public RelayCommand ManualCalibrateCommand { get; set; }

        public Visibility DeviceVisibility
        {
            get { return _deviceVisibility; }
            set { _deviceVisibility = value; RaisePropertyChanged(); }
        }

        public Visibility RecordsVisibility
        {
            get { return _recordsVisibility; }
            set { _recordsVisibility = value; RaisePropertyChanged(); }
        }

        public Visibility TrainingVisibility
        {
            get { return _trainingVisibility; }
            set { _trainingVisibility = value; RaisePropertyChanged(); }
        }

        public Visibility TipVisibility
        {
            get { return _tipVisibility; }
            set { _tipVisibility = value; RaisePropertyChanged(); }
        }

        public Visibility SettingVisibility
        {
            get { return _settingVisibility; }
            set { _settingVisibility = value; RaisePropertyChanged(); }
        }

        public Visibility AccountVisibility
        {
            get { return _accountVisibility; }
            set { _accountVisibility = value; }
        }

        public Visibility ImageVisibility
        {
            get { return _imageVisibility; }
            set { _imageVisibility = value; }
        }


        /// <summary>
        /// 退出至桌面的菜单项是否可见
        /// </summary>
        public Visibility ExitVisibility
        {
            get { return _exitVisibility; }
            set { _exitVisibility = value; RaisePropertyChanged(); }
        }

        #endregion commands

        private Visibility _deviceVisibility = Visibility.Collapsed;

        private Visibility _recordsVisibility = Visibility.Collapsed;

        private Visibility _trainingVisibility = Visibility.Collapsed;

        private Visibility _tipVisibility = Visibility.Collapsed;

        private Visibility _settingVisibility = Visibility.Collapsed;

        private Visibility _exitVisibility = Visibility.Collapsed;

        private Visibility _accountVisibility = Visibility.Collapsed;

        private Visibility _imageVisibility = Visibility.Collapsed;

        public SystemMenuPageViewModel(IFramePageNavigationService service)
        {
            Tracer.TraceEnterFunc("UI.XRay.Gui.ViewModels.SettingMenuPageViewModel");

            if (service == null)
            {
                throw new ArgumentNullException("service");
            }

            _naviService = service;

            CreateCommands();
            InitButtonsVisibility();

            //ControlService.ServicePart.RadiateXRay(false);
            //ControlService.ServicePart.DriveConveyor(Control.ConveyorDirection.Stop);

            Tracer.TraceExitFunc("UI.XRay.Gui.ViewModels.SettingMenuPageViewModel");
        }

        private void CreateCommands()
        {
            NavigateToAccountPageCommand = new RelayCommand(NavigateToAccountPageCommandExecute);
            NavigateToImagePageCommand = new RelayCommand(NavigateToImagePageCommandExecute);
            NavigateToDevicePageCommand = new RelayCommand(NavigateToDevicePageCommandExecute);
            NavigateToSettingPageCommand = new RelayCommand(NavigateToSettingPageCommandExecute);
            NavigateToAboutPageCommand = new RelayCommand(NavigateToAboutPageCommandExecute);
            NavigateToRecordsPageCommand = new RelayCommand(NavigateToRecordsPageCommandExecute);
            NavigateToTrainingPageCommand = new RelayCommand(NavigateToTrainingPageCommandExecute);
            NavigateToTipPageCommand = new RelayCommand(NavigateToTipPageCommandExecute);
            ShutdownCommand = new RelayCommand(ShutdownCommandExecute);
            ExitToDesktopCommand = new RelayCommand(ExitToDesktopCommandExecute);
            LogoutCommand = new RelayCommand(LogoutCommandExecute);
            ManualCalibrateCommand = new RelayCommand(ManualCalibrateCommandExecute);
           
        }

        /// <summary>
        /// 根据当前用户的状态，初始化所有菜单的可见性
        /// </summary>
        private void InitButtonsVisibility()
        {
            try
            {
                if (LoginAccountManager.Service.HasLogin)
                {
                    if (LoginAccountManager.Service.CurrentAccount.Role == AccountRole.System)
                    {
                        ExitVisibility = Visibility.Visible;
                        DeviceVisibility = Visibility.Visible;
                        SettingVisibility = Visibility.Visible;
                        TipVisibility = Visibility.Visible;

                        TrainingVisibility = Visibility.Visible;
                        RecordsVisibility = Visibility.Visible;

                        AccountVisibility = Visibility.Visible;

                        ImageVisibility = Visibility.Visible;
                    }
                    else if (LoginAccountManager.Service.CurrentAccount.Role == AccountRole.Maintainer)
                    {
                        ExitVisibility = Visibility.Visible;
                        DeviceVisibility = Visibility.Visible;
                        SettingVisibility = Visibility.Visible;
                        TipVisibility = Visibility.Visible;

                        TrainingVisibility = Visibility.Visible;
                        RecordsVisibility = Visibility.Visible;

                        AccountVisibility = Visibility.Visible;
                        ImageVisibility = Visibility.Visible;
                    }
                    else if (LoginAccountManager.Service.CurrentAccount.Role == AccountRole.Admin)
                    {
                        SettingVisibility = Visibility.Visible;
                        TipVisibility = Visibility.Visible;

                        TrainingVisibility = Visibility.Visible;
                        RecordsVisibility = Visibility.Collapsed;

                        AccountVisibility = Visibility.Visible;
                        ImageVisibility = Visibility.Visible;

                        if (LoginAccountManager.Service.CurrentAccountPermission.CanManageLog)
                        {
                            RecordsVisibility = Visibility.Visible;
                        }
                    }
                    else
                    {

                        ImageVisibility = Visibility.Visible;
                        AccountVisibility = Visibility.Visible;
                        SettingVisibility = Visibility.Visible;
                        if (LoginAccountManager.Service.CurrentAccountPermission.CanTraining)
                        {
                            TrainingVisibility = Visibility.Visible;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        /// <summary>
        /// 关闭设置窗口
        /// </summary>
        private void CloseSettingWindow()
        {
            this.MessengerInstance.Send(new CloseWindowMessage("SettingWindow"));
        }


        private void NavigateToTipPageCommandExecute()
        {
            if (_naviService != null)
            {
                _naviService.ShowPage(new PageNavigation("TipMenu", "TipPlansPage", "Tip"));
            }
        }

        private void NavigateToTrainingPageCommandExecute()
        {
            if (_naviService != null)
            {
                _naviService.ShowPage(new PageNavigation("TrainingMenu", "TrainingSettingPage", "Training"));
            }
        }

        private void NavigateToRecordsPageCommandExecute()
        {
            if (_naviService != null)
            {
                _naviService.ShowPage(new PageNavigation("RecordsMenu", "BootLogPage", "Work Records"));
            }
        }

        /// <summary>
        /// 导航至用户管理页
        /// </summary>
        private void NavigateToAccountPageCommandExecute()
        {
            if (_naviService != null)
            {
                _naviService.ShowPage(new PageNavigation("AccountMenu", "AccountPage", "Account"));
            }
        }

        private void NavigateToImagePageCommandExecute()
        {
            if (_naviService != null)
            {
                _naviService.ShowPage(new PageNavigation("ImageMenu", "ImsPage", "Image Management"));
            }
        }

        private void NavigateToDevicePageCommandExecute()
        {
            if (_naviService != null)
            {
                _naviService.ShowPage(new PageNavigation("DeviceMenu", "DetectorsPage", "Device"));
            }
        }

        private void NavigateToSettingPageCommandExecute()
        {
            if (_naviService != null)
            {
                _naviService.ShowPage(new PageNavigation("SettingMenu", "FunctionKeysPage", "Setting"));
            }
        }

        private void NavigateToAboutPageCommandExecute()
        {
            if (_naviService != null)
            {
                _naviService.ShowPage(new PageNavigation("AboutMenu", "AboutPage", "About"));
            }
        }

        /// <summary>
        /// 关机命令执行
        /// </summary>
        private void ShutdownCommandExecute()
        {
            Tracer.TraceInfo("[Exit] User Shutdown!");
            ShutdownAsync();
        }

        /// <summary>
        /// 退出至桌面
        /// </summary>
        private void ExitToDesktopCommandExecute()
        {
            //SystemStartShutdownService.ShowExplorer();
            Application.Current.Shutdown();
        }

        private void ShutdownAsync()
        {
            var message = new ShowDialogMessageAction("SettingWindow", TranslationService.FindTranslation("Shutdown"),
                TranslationService.FindTranslation("Do you want to shutdown scanner now?"), MetroDialogButtons.OkCancel,
                result =>
                {
                    if (result == MetroDialogResult.Ok)
                    {
                        SystemStartShutdownService.Shutdown();
                    }
                });

            this.MessengerInstance.Send(message);
        }

        /// <summary>
        /// 注销命令执行：先关闭设置窗口， 然后显示登陆窗口
        /// </summary>
        private async void LogoutCommandExecute()
        {
            
            CloseSettingWindow();

            MessengerInstance.Send(new LogoutMessage());
            LoginAccountManager.Service.Logout();

            MessengerInstance.Send(new OpenWindowMessage("MainWindow", "LoginWindow"));
         
        }


        /// <summary>
        /// 标定命令执行：关闭设置窗口，并显示标定窗口
        /// </summary>
        private void ManualCalibrateCommandExecute()
        {
            CloseSettingWindow();
            MessengerInstance.Send(new OpenWindowMessage("MainWindow", "CalibrateWindow"));
        }
    }
}
