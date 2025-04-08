using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Messaging;
using UI.Common.Tracers;
using UI.XRay.Business.Entities;
using UI.XRay.Common.Utilities;
using UI.XRay.Flows.Controllers;
using UI.XRay.Gui.Framework;
using UI.XRay.ImagePlant.Gpu;
using UI.XRay.RenderEngine;
using UI.XRay.Security.Scanner.SettingViews;
using UI.XRay.Security.Scanner.ViewModel;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Security.Scanner.MetroDialogs;
using UI.XRay.Flows.Services;
using UI.XRay.Security.Scanner;
using UI.XRay.ControlWorkflows;
using UI.XRay.Security.Scanner.Converters;
using Xceed.Wpf.AvalonDock.Converters;

namespace UI.XRay.Security.Scanner
{
    /// <summary>
    /// 安检软件的主窗口
    /// </summary>
    public partial class MainWindow
    {
        private int MainScreenWidth
        {
            get { return Screen.PrimaryScreen.Bounds.Width; }
        }

        /// <summary>
        /// Flyout 消息消失定时器
        /// </summary>
        private DispatcherTimer _flyOutMessageTimer;

        /// <summary>
        /// 缓存的当前正在显示的flyout消息
        /// </summary>
        private List<ShowFlyoutMessage> _flyoutMessageList = new List<ShowFlyoutMessage>();

        public MainWindow()
        {
            InitializeComponent();
            
            // 必须禁止主窗口的显示时的动画，否则会导致gl控件的位置有偏差
            this.WindowTransitionsEnabled = false;

            this.Loaded += OnLoaded;
            Messenger.Default.Register<OpenWindowMessage>(this, OnOpenWindowMessageAction);
            Messenger.Default.Register<ShowFlyoutMessage>(this, ShowFlyoutMessageAction);

            // 窗口全屏时的宽度和高度
            var windowWidth = SystemParameters.VirtualScreenWidth;
            var windowHeight = SystemParameters.PrimaryScreenHeight;

            // 初始化图像显示控件，同时获取图像显示控制对象
            IRollingImageProcessController imagesController;

            bool _touchControlVisibility = false;
            if (!ScannerConfig.Read(ConfigPath.SystemTouchUI, out _touchControlVisibility))
            {
                _touchControlVisibility = false;
            }
            int _imageMargin = 0;
            if (!ScannerConfig.Read(ConfigPath.ImageMargin, out _imageMargin))
            {
                _imageMargin = 0;
            }
            if (!_touchControlVisibility)
            {
                SystemBar.Visibility = Visibility.Visible;
                SystemBarTouch.Visibility = Visibility.Collapsed;
                imagesController = ImagingControl.Initialize((int)windowWidth, (int)(windowHeight - SystemBar.Height + _imageMargin));
            }
            else
            {
                SystemBar.Visibility = Visibility.Collapsed;
                SystemBarTouch.Visibility = Visibility.Visible; ;
                imagesController = ImagingControl.Initialize((int)windowWidth, (int)(windowHeight - SystemBarTouch.Height + _imageMargin));
            }

            imagesController.CanManualDraw = true;

            this.DataContext = new MainViewModel(imagesController);

            HttpNetworkController.Controller.ReceiveAction += Controller_ReceiveAction;

            BottomFlyout.Width = MainScreenWidth;

            // 定时将窗口置顶，捕获按键消息
            //WindowFocusHelper.MakeFocus(this);

            
          
        }

        ShowDialogMessageAction showUpdateMsg;

        private void Controller_ReceiveAction(string obj)
        {
            if (obj.Equals("diagnosis warning"))
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (showUpdateMsg != null)
                    {
                        showUpdateMsg = new ShowDialogMessageAction("SettingWindow", TranslationService.FindTranslation("Warning"),
                            TranslationService.FindTranslation("In the process of diagnosis, the ray will turn on and the conveyor will move back, please ensure the safety of personnel and luggage."),
                            MetroDialogButtons.Ok, result => { });
                        ShowMsgDialogMessageAction(showUpdateMsg);
                    }
                });
            }
            else if (obj.Equals("remote diagnosis request"))
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (showUpdateMsg != null)
                    {
                        showUpdateMsg = new ShowDialogMessageAction("SettingWindow", TranslationService.FindTranslation("Remote Diagnosis"),
                            TranslationService.FindTranslation("Remote Wants To Diagnose"), MetroDialogButtons.OkCancel, result =>
                            {
                                if (result == MetroDialogResult.Ok)
                                {
                          
                                    HttpNetworkController.Controller.Diagnosis(isDiagnosisCanceled: false);
                                
                                }
                                else
                                {
                                    HttpNetworkController.Controller.Diagnosis(isDiagnosisCanceled: true);
                                }
                            });
                        ShowMsgDialogMessageAction(showUpdateMsg);
                    }
                });
            }
        }

        // TODO: 从SettingWindow.xaml.cs复制来的，试一下能不能正常用
        /// <summary>
        /// 当前是否正在显示一个Metro对话框
        /// </summary>
        private bool _hasMetroDialog = false;
        /// <summary>
        /// 显示Metro消息对话框
        /// </summary>
        private async void ShowMsgDialogMessageAction(ShowDialogMessageAction msg)
        {
            // 为防止用户在极短时间内，两次点击同一按钮，导致同时显示两个对话框，在这里加以限制，以防止这种现象
            // 只有在上一个对话框结束后，才会允许显示新的Metro对话框
            //if (_hasMetroDialog)
            //{
            //    return;
            //}

            _hasMetroDialog = true;

            msg.Execute(await this.ShowMessageDialogAsync(msg.Title, msg.Notification, msg.Buttons));

            // 对话框显示完毕后，将角点再设置回显示对话框之前的元素上
            MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));

            _hasMetroDialog = false;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            var windowWidth = SystemParameters.VirtualScreenWidth;
            var windowHeight = SystemParameters.PrimaryScreenHeight;

            // 先设置窗口的宽和高，再调整Left和Top: 将窗口设置为全屏
            this.MinWidth = this.MaxWidth = windowWidth;
            this.MinHeight = this.MaxHeight = windowHeight;
            this.Top = 0.0;
            this.Left = 0.0;
        }

        /// <summary>
        /// 在主窗口底部显示一个飞出消息：用于显示tip注入结果等
        /// </summary>
        /// <param name="flyoutMessage"></param>
        private void ShowFlyoutMessageAction(ShowFlyoutMessage flyoutMessage)
        {
            if (flyoutMessage.ParentWindowKey == "MainWindow")
            {
                Dispatcher.InvokeAsync(() =>
                {
                    // 如果已经存在内容相同的信息，且均无倒计时，则不再增加
                    if (!_flyoutMessageList.Exists(
                        message =>
                            message.Message == flyoutMessage.Message && flyoutMessage.CloseCountdown == null &&
                            message.CloseCountdown == null))
                    {

                        _flyoutMessageList.Add(flyoutMessage);

                        if (_flyOutMessageTimer == null)
                        {
                            _flyOutMessageTimer = new DispatcherTimer(DispatcherPriority.Normal);
                            _flyOutMessageTimer.Tick += FlyOutMessageTimerOnTick;
                            _flyOutMessageTimer.Interval = TimeSpan.FromSeconds(1);
                        }

                        if (!_flyOutMessageTimer.IsEnabled)
                        {
                            _flyOutMessageTimer.Start();
                        }

                        ShowFlyoutMessage();
                    }
                });
            }
        }

        /// <summary>
        /// Flyout消息清除定时器，每隔1秒钟检测一次flyout消息队列
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void FlyOutMessageTimerOnTick(object sender, EventArgs eventArgs)
        {
            if (_flyoutMessageList.Count > 0)
            {
                _flyoutMessageList.RemoveAll(message => message.CloseCountdown != null && message.CloseCountdown <= 0);
            }

            if (_flyoutMessageList.Count == 0)
            {
                BottomFlyout.IsOpen = false;
                _flyOutMessageTimer.Stop();
            }
            else
            {
                ShowFlyoutMessage();

                // 倒计时
                foreach (var message in _flyoutMessageList)
                {
                    if (message.CloseCountdown != null)
                    {
                        message.CloseCountdown -= 1;
                    }
                }
            }
        }

        /// <summary>
        /// 显示flyout消息
        /// </summary>
        private void ShowFlyoutMessage()
        {
            if (_flyoutMessageList.Count > 0)
            {
                BottomFlyout.IsOpen = true;

                var builder = new StringBuilder(200);
                for (int i = 0; i < _flyoutMessageList.Count; i++)
                {
                    var message = _flyoutMessageList[i];

                    if (message.CloseCountdown == null)
                    {
                        builder.Append(message.Message);
                    }
                    else if (message.CloseCountdown > 0)
                    {
                        //消息最后增加消失倒计时
                        builder.Append(message.Message).Append(" [").Append(message.CloseCountdown).Append("]");
                    }

                    if (i != _flyoutMessageList.Count - 1)
                    {
                        builder.AppendLine();
                    }
                }

                this.FlyoutTextBlock.Text = builder.ToString();
            }
        }

        private void OnOpenWindowMessageAction(OpenWindowMessage msg)
        {
            if (msg.ParentWindowKey != "MainWindow")
            {
                return;
            }

            // 打开配置窗口，并且根据参数，初始化配置窗口的默认配置页
            if (msg.ToOpenWindowKey == "SettingWindow")
            {
                var navi = msg.Parameter as PageNavigation;
                if (navi != null)
                {
                    ShowDialogInClientArea(new SettingWindow(navi));
                }
            }
            else if (msg.ToOpenWindowKey == "ScreenImagesOperationWindow")
            {
                ShowDialogInClientArea(new ScreenImagesOperationWindow());
            }
            else if (msg.ToOpenWindowKey == "LoginWindow")
            {
                ShowLoginWindow();
            }
            else if (msg.ToOpenWindowKey == "CalibrateWindow")
            {
                ShowCalibrateWindow();
            }
            else if (msg.ToOpenWindowKey == "RegularRemindWindow")
            {
                ShowRegularRemindWindow();
            }
            else if (msg.ToOpenWindowKey == "XRayGenWarmupWindow")
            {
                ShowXRayGenWarmupWindow();
            }
            else if (msg.ToOpenWindowKey == "ChangeSysDateTimeWindow")
            {
                ShowChangeSysTimeDateWindow();
            }
            else if (msg.ToOpenWindowKey == "CleanTunnelWindow")
            {
                ShowCleanTunnelWindow();
            }
            else if(msg.ToOpenWindowKey == "RemoteDiagnose")
            {
                ShowRemoteDiagnose();  //yxc
            }
            else if (msg.ToOpenWindowKey == "WorkReminderWindow")
            {
                ShowWorkReminderWindow((double)msg.Parameter);
            }
        }

     



        private void ShowRemoteDiagnose()   //yxc
        {
            
            if (!Transmission.IsRemoteDiagnosing)
            {
                Transmission.IsRemoteDiagnosing = true;
                this.Dispatcher.Invoke(() =>
                {

                    var window = new RemoteDiagnose();
                    window.Owner = this;
                    window.ShowDialog();
                   

                });
                
            }
            else
            {
                return;
            }
        }





        private void ShowChangeSysTimeDateWindow()
        {
            this.Dispatcher.Invoke(() =>
            {
                var window = new ChangeSysDateTimeWindow();
                window.WindowStartupLocation = WindowStartupLocation;

                window.Top = (ImagingControl.ActualHeight - window.ActualHeight)/2;
                window.Left = (MainScreenWidth - window.ActualWidth)/2;

                window.Owner = this;
                window.ShowDialog();
            });
        }

        /// <summary>
        /// 显示射线源预热窗口，开始预热
        /// </summary>
        private void ShowCleanTunnelWindow()
        {
            this.Dispatcher.Invoke(() =>
            {
                var window = new CleanTunnelWindow();
                window.MinWidth = window.MaxWidth = MainScreenWidth - 200;
                //window.MinHeight = window.MaxHeight = ImagingControl.ActualHeight / 2;

                window.Top = this.Top + (ImagingControl.ActualHeight - window.Height) / 2;
                window.Left = this.Left + 100;

                window.Owner = this;
                window.ShowDialog();
            });
        }

        /// <summary>
        /// 显示工作间隔提醒窗口
        /// </summary>
        private void ShowWorkReminderWindow(double workReminderCount = 0)
        {
            this.Dispatcher.Invoke(() =>
            {
                var window = new WorkReminderWindow();
                WorkReminderViewModel viewModel = window.DataContext as WorkReminderViewModel;
                if (viewModel == null)
                {
                    viewModel = new WorkReminderViewModel(window);
                    window.DataContext = viewModel;
                }
                int totalSeconds = (int)(workReminderCount * 60 * 60);
                int hours = totalSeconds / 60 / 60;
                int minutes = totalSeconds / 60 % 60;

                viewModel.WorkReminderCount = string.Format("{0}时{1}分", hours, minutes);
                window.MinWidth = window.MaxWidth = MainScreenWidth - 200;
                //window.MinHeight = window.MaxHeight = ImagingControl.ActualHeight / 2;

                window.Top = this.Top + (ImagingControl.ActualHeight - window.Height) / 2;
                window.Left = this.Left + 100;

                window.Owner = this;
                window.ShowDialog();
            });
        }

        private void ShowDiagnosisWarningWindow()
        {
            this.Dispatcher.Invoke(() =>
            {
                var window = new DiagnosisWarningWindow();
                window.MinWidth = window.MaxWidth = MainScreenWidth - 200;
                //window.MinHeight = window.MaxHeight = ImagingControl.ActualHeight / 2;

                window.Top = this.Top + (ImagingControl.ActualHeight - window.Height) / 2;
                window.Left = this.Left + 100;

                window.Owner = this;
                window.ShowDialog();
            });
        }

        /// <summary>
        /// 显示射线源预热窗口，开始预热
        /// </summary>
        private void ShowXRayGenWarmupWindow()
        {
            this.Dispatcher.Invoke(() =>
            {
                var window = new XRayGenWarmupWindow();
                window.MinWidth = window.MaxWidth = MainScreenWidth - 200;
                //window.MinHeight = window.MaxHeight = ImagingControl.ActualHeight / 2;

                window.Top = this.Top + (ImagingControl.ActualHeight - window.Height)/2;
                window.Left = this.Left + 100;

                window.Owner = this;
                window.ShowDialog();
            });
        }

        /// <summary>
        /// 显示图像校正窗口
        /// </summary>
        private void ShowCalibrateWindow()
        {
            this.Dispatcher.Invoke(() =>
            {
                var window = new CalibrateWindow();
                window.WindowStartupLocation = WindowStartupLocation;
                window.MinWidth = window.MaxWidth = MainScreenWidth*2.0/3;
                window.MinHeight = window.MaxHeight = ImagingControl.ActualHeight/2;

                window.Top = this.Top + ImagingControl.ActualHeight/4;
                window.Left = this.Left + window.Width/6;

                window.Owner = this;
                window.ShowDialog();
            });
        }

        /// <summary>
        /// 显示定期提醒窗口
        /// </summary>
        private void ShowRegularRemindWindow()
        {
            this.Dispatcher.Invoke(() =>
            {
                var window = new RegularRemindWindow();
                window.WindowStartupLocation = WindowStartupLocation;
                window.MinWidth = window.MaxWidth = MainScreenWidth * 2.0 / 3;
                window.MinHeight = window.MaxHeight = ImagingControl.ActualHeight / 2;

                window.Top = this.Top + ImagingControl.ActualHeight / 4;
                window.Left = this.Left + window.Width / 6;

                window.Owner = this;
                window.ShowDialog();
            });
        }

        private void ShowLoginWindow()
        {
            this.Dispatcher.Invoke(() => ShowDialogInClientArea(new LoginWindow()));
        }

        /// <summary>
        /// 在主窗口的客户端区域，显示对话框窗口，对话框窗口将占据整个客户区域
        /// </summary>
        /// <param name="window"></param>
        private void ShowDialogInClientArea(Window window)
        {
            window.WindowStartupLocation = WindowStartupLocation.Manual;

            // 设置窗口的宽和高，与图像显示区域一致，刚好覆盖图像显示区域
            window.MinWidth = window.MaxWidth = MainScreenWidth;
            window.MinHeight = window.MaxHeight = ImagingControl.ActualHeight;

            // 设置窗口的左上角与主窗口对齐
            window.Top = this.Top;
            window.Left = this.Left;

            window.Owner = this;
            window.ShowDialog();
        }

        private void BottomFlyout_OnIsOpenChanged(object sender, RoutedEventArgs e)
        {
            if (!BottomFlyout.IsOpen)
            {
                _flyoutMessageList.Clear();
            }
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Alt && e.SystemKey == Key.Space)
            {
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Alt && e.SystemKey == Key.F4)
            {
                e.Handled = true;
            }
            else
            {
                base.OnKeyDown(e);
            }
        }
    }
}
