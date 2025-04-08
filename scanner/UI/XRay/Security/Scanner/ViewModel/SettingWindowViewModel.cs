using System;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using UI.Common.Tracers;
using UI.XRay.Flows.Services;
using UI.XRay.Gui.Framework;
using UI.XRay.Parts.Keyboard;
using System.Diagnostics;
using MahApps.Metro.Controls;
using UI.XRay.Flows.Controllers;
using GalaSoft.MvvmLight.Messaging;

namespace UI.XRay.Security.Scanner.ViewModel
{
    public class SettingWindowViewModel : ViewModelBase
    {
        public RelayCommand GoBackToSettingMenuCommand { get; set; }

        public RelayCommand<KeyEventArgs> KeyDownCommand { get; set; }

        public RelayCommand<KeyEventArgs> KeyUpCommand { get; set; }

        public RelayCommand<KeyEventArgs> PreviewKeyDownCommand { get; set; }

        public RelayCommand<KeyEventArgs> PreviewKeyUpCommand { get; set; }

        public RelayCommand CloseWindowCommand { get; set; }

        public RelayCommand KeyboardCommand { get; set; }

        /// <summary>
        /// 设置窗口显示时，将会加载的默认页面
        /// </summary>
        private Uri _defaultPageUri;

        public Uri DefaultPageUri
        {
            get { return _defaultPageUri; }
            set
            {
                _defaultPageUri = value;
                RaisePropertyChanged();
            }
        }

        private string _pageTitle;

        ///// <summary>
        ///// 获取或设置当前配置页的标题
        ///// </summary>
        //public string PageTitle
        //{
        //    get { return _pageTitle; }
        //    set
        //    {
        //        _pageTitle = value;
        //        RaisePropertyChanged();
        //    }
        //}

        private IFramePageNavigationService _navigationService;

        public SettingWindowViewModel(IFramePageNavigationService service)
        {
            if (service == null)
            {
                throw new ArgumentNullException("service");
            }

            _navigationService = service;

            // 以弱事件的方式，订阅页导航变化事件，用于更新窗口标题
            //WeakEventManager<IFramePageNavigationService, PageNavigationEventArgs>.AddHandler(service, "PageNavigated", NavigationServiceOnPageNavigated);



            GoBackToSettingMenuCommand = new RelayCommand(GoBackToMenuCommandExecute);
            KeyDownCommand = new RelayCommand<KeyEventArgs>(KeyDownCommandExecute);
            //KeyUpCommand = new RelayCommand<KeyEventArgs>(KeyUpCommandExecute);
            PreviewKeyDownCommand = new RelayCommand<KeyEventArgs>(PreviewKeyDownCommandExecute);
            PreviewKeyUpCommand = new RelayCommand<KeyEventArgs>(PreviewKeyUpCommandExecute);

            CloseWindowCommand = new RelayCommand(CloseWindowCommandExe);

            KeyboardCommand = new RelayCommand(KeyboardCommandExe);
            HttpNetworkController.Controller.ExitSettngWindowAction += GotoMainWindow;  //yxc 诊断的时候如果停留在settinwindow直接回退到主界面
            InitSetting();
        }

     private void GotoMainWindow()
        {
            Application.Current.Dispatcher.InvokeAsync(() => { Messenger.Default.Send(new CloseWindowMessage("SettingWindow")); });
            
        }

 

        /// <summary>
        /// 进入配置窗口时，初始化一些信息
        /// </summary>
        private void InitSetting()
        {
            try
            {
                // 进入配置窗口前，将当前行李总数保存更新至注册表
                BagCounterService.Service.SaveBagCount();
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        private void GoBackToMenuCommandExecute()
        {
            _navigationService.ShowPage(new PageNavigation("SystemMenu", "MenuPage", "Menu"));
        }

        private void PreviewKeyDownCommandExecute(KeyEventArgs args)
        {
            if ((args.KeyboardDevice.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
            {
                ConveyorKeyEventsConvertor.HandlePreviewKeyDown(args);
            }
        }

        private void PreviewKeyUpCommandExecute(KeyEventArgs args)
        {
            if ((args.KeyboardDevice.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
            {
                ConveyorKeyEventsConvertor.HandlePreviewKeyUp(args);
            }
        }

        /// <summary>
        /// 处理窗口的KeyDown事件。这些事件，可能会被窗口中的页或者Metro对话框使用Preview优先处理
        /// </summary>
        /// <param name="args"></param>
        private void KeyDownCommandExecute(KeyEventArgs args)
        {
            switch (args.Key)
            {
                // 用户按下F3功能键或者计算机键盘中的Esc键时，关闭设置窗口
                // 在KeyDown中处理，而不是在PreviewKeyDown中处理，是因为设置窗口中可能会出现Metro对话框；
                // 在显示Metro对话框的时候，不允许直接关闭窗口。
                case Key.F3:
                case Key.Escape:
                    // 必须是Ctrl键未按下时，方能直接关闭窗口。如果Ctrl键被按下，则不允许
                    // 原因：在做键盘测试时，为了测试F3功能键，先按下Ctrl键，然后再按下F3即可对F3进行测试
                    if ((args.KeyboardDevice.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
                    {
                        this.MessengerInstance.Send(new CloseWindowMessage("SettingWindow"));
                        args.Handled = true;
                    }

                    break;

                    //当角点不位于TextBox等输入框时，用户按下Backspace键，返回到主菜单。
                case Key.Back:
                    GoBackToMenuCommandExecute();
                    args.Handled = true;
                    break;
            }
        }
        private void CloseWindowCommandExe()
        {
        }

        private void KeyboardCommandExe()
        {
            TouchKeyboardService.Service.OpenKeyboardWindow("HXkeyboard");
        }
    }
}
