using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using UI.Common.Tracers;
using UI.XRay.Flows.Controllers;
using UI.XRay.Gui.Framework;
using UI.XRay.Security.Scanner.MetroDialogs;
using UI.XRay.Security.Scanner.ViewModel;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace UI.XRay.Security.Scanner
{
    public class OwnerWindowWrapper : System.Windows.Forms.IWin32Window
    {
        IntPtr _handle;

        public OwnerWindowWrapper(IntPtr handle)
        {
            _handle = handle;
        }

        #region IWin32Window Members
        IntPtr System.Windows.Forms.IWin32Window.Handle
        {
            get { return _handle; }
        }
        #endregion
    }

    /// <summary>
    /// SettingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingWindow
    {
        /// <summary>
        /// 当前是否正在显示一个Metro对话框
        /// </summary>
        private bool _hasMetroDialog = false;

        /// <summary>
        /// 显示配置窗口。
        /// </summary>
        public SettingWindow(PageNavigation navigation)
        {
            InitializeComponent();

            ViewModelLocator.SettingPageNavigationService.SetFrame(MenuFrame, PageFrame);
            ViewModelLocator.SettingPageNavigationService.ShowPage(navigation);

            // 屏蔽Page的Back键，该键用于导航的goback功能。
            NavigationCommands.BrowseBack.InputGestures.Clear();
            //PageFrame.InputBindings.Add(new InputBinding(ApplicationCommands.NotACommand, new KeyGesture(Key.Back)));
            //MenuFrame.InputBindings.Add(new InputBinding(ApplicationCommands.NotACommand, new KeyGesture(Key.Back)));

            PageFrame.Focus();

            // 注册信使消息，以便于从vm中关闭此窗口
            Messenger.Default.Register<CloseWindowMessage>(this, CloseWindowMessageAction);
            Messenger.Default.Register<ShowDialogMessageAction>(this, ShowMsgDialogMessageAction);
            Messenger.Default.Register<ShowPasswordDialogAsyncMessage>(this, ShowPasswordDialogAsyncMessageAction);
            Messenger.Default.Register<ShowAddAccountWindowAction>(this, OpenWindowMessageAction);
            Messenger.Default.Register<ShowAddGroupWindowAction>(this, OpenAddGroupWindowMessageAction);
            Messenger.Default.Register<ShowTipImageSelectWindowAction>(this, ShowTipImageSelectWindowActionAction);
            Messenger.Default.Register<ShowImageRetrievalWindowAction>(this, OnShowImageRetrievalWindowAction);
            Messenger.Default.Register<ShowXRayImageMessage>(this, ShowXRayImageMessageAction);
            Messenger.Default.Register<ShowBadChannelImageMessage>(this, ShowImageBadChannelMessageAction);
            Messenger.Default.Register<ShowOpenFilesDialogMessageAction>(this, ShowImageFilesSelectionDialogAction);
            Messenger.Default.Register<ShowSaveFileDialogMessageAction>(this, ShowSaveFileDialogMessageAction);
            Messenger.Default.Register<ShowFolderBrowserDialogMessageAction>(this, ShowFolderSelectionDlgAction);

            this.Closed += OnClosed;
            this.Closing += (sender, args) =>
            {
                if (_hasMetroDialog)
                {
                    args.Cancel = true;
                }
            };

            // 定时将窗口置顶，捕获按键消息
            WindowFocusHelper.MakeFocus(this);
            HttpNetworkController.Controller.SendingMsgAction += Controller_SendingMsgAction;
            HttpNetworkController.Controller.SendCompleteAction += Controller_SendCompleteAction;
            HttpNetworkController.Controller.ReceiveAction += Controller_ReceiveAction;
        }
        ShowDialogMessageAction showUpdateMsg;
        void Controller_ReceiveAction(string obj)
        {
            if (obj.Equals("autologin"))
            {
                Messenger.Default.Unregister(this);
                this.Dispatcher.Invoke(new Action(() =>
                {
                    this.Close();
                }));
            }
            else if (obj.Equals("diagnosis warning"))
            {
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (showUpdateMsg != null)
                    {
                        showUpdateMsg = new ShowDialogMessageAction("SettingWindow", TranslationService.FindTranslation("Warning"),
                            TranslationService.FindTranslation("In the process of diagnosis, the ray will turn on and the conveyor will move back, please ensure the safety of personnel and luggage."), MetroDialogButtons.Ok,
                            result => { });
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
            else
            {
                var item = TranslationService.FindTranslation(obj);
                System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (showUpdateMsg == null)
                    {
                        showUpdateMsg = new ShowDialogMessageAction("SettingWindow",
                        "", item, MetroDialogButtons.Ok, result => { });
                        ShowMsgDialogMessageAction(showUpdateMsg);
                    }
                });
            }
        }

        void Controller_SendCompleteAction(UI.AI.Detect.HttpService.RespMsg obj)
        {
            //System.Threading.Thread.Sleep(1000);
            //SendKeyEvents.Press(Key.F3);
            //System.Threading.Thread.Sleep(1000);
            System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var show = new ShowDialogMessageAction("SettingWindow",
                    "", TranslationService.FindTranslation("Dump to network completely"), MetroDialogButtons.Ok, result => { });
                ShowMsgDialogMessageAction(show);
            });
        }

        void Controller_SendingMsgAction(UI.AI.Detect.HttpService.RespMsg obj)
        {
            System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var show = new ShowDialogMessageAction("SettingWindow",
                    "", TranslationService.FindTranslation("Sending"), MetroDialogButtons.Ok, result => { });
                ShowMsgDialogMessageAction(show);
            });
        }

        private void ShowSaveFileDialogMessageAction(ShowSaveFileDialogMessageAction msgAction)
        {
            var dlg = new SaveFileDialog();
            dlg.Filter = msgAction.Filter;

            try
            {
                if (dlg.ShowDialog(this) == true)
                {
                    msgAction.Execute(dlg.FileName);
                }
                else
                {
                    msgAction.Execute(null);
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }

        private void ShowImageFilesSelectionDialogAction(ShowOpenFilesDialogMessageAction messageAction)
        {
            var dlg = new OpenFileDialog();
            dlg.Multiselect = messageAction.MultiSelected;
            dlg.Filter = messageAction.Filter;

            try
            {
                if ((bool)dlg.ShowDialog(this))
                {
                    messageAction.Execute(dlg.FileNames);
                }
                else
                {
                    messageAction.Execute(null);
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }

        private void ShowFolderSelectionDlgAction(ShowFolderBrowserDialogMessageAction msgAction)
        {
            var dlg = new FolderBrowserDialog();
            dlg.ShowNewFolderButton = true;

            var source = PresentationSource.FromVisual(this) as HwndSource;
            if (source != null)
            {
                System.Windows.Forms.IWin32Window win = new OwnerWindowWrapper(source.Handle);
                var result = dlg.ShowDialog(win);
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    msgAction.Execute(dlg.SelectedPath);
                }
                else
                {
                    msgAction.Execute(null);
                }
            }
        }

        /// <summary>
        /// 窗口关闭事件：在窗口关闭，清理最后一个页面占用的资源
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void OnClosed(object sender, EventArgs eventArgs)
        {
            var page = PageFrame.Content as Page;
            if (page != null)
            {
                var vmCleanup = page.DataContext as ICleanup;
                if (vmCleanup != null)
                {
                    try
                    {
                        vmCleanup.Cleanup();
                    }
                    catch (Exception exception)
                    {
                        Tracer.TraceException(exception, "Exception occured when call viewmodel' Cleanup. " + vmCleanup.ToString());
                    }
                }
            }
        }

        private async void OnShowImageRetrievalWindowAction(ShowImageRetrievalWindowAction msg)
        {
            if (msg.ParentWindowKey == "SettingWindow")
            {
                try
                {
                    // 先实现Overlay，再显示对话框，关闭对话框后，隐藏Overlay，最后再将添加结果返回给消息发送者
                    ShowOverlayAsync();

                    ImageRetrievalWindow dlg = new ImageRetrievalWindow() { Owner = this };
                    dlg.ShowDialog();

                    HideOverlayAsync();

                    msg.Execute(await dlg.WaitForButtonPressAsync());
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception);
                }

            }
        }

        private async void OpenWindowMessageAction(ShowAddAccountWindowAction msg)
        {
            if (msg.ParentWindowKey == "SettingWindow")
            {
                // 先实现Overlay，再显示对话框，关闭对话框后，隐藏Overlay，最后再将添加结果返回给消息发送者
                await ShowOverlayAsync();

                //var dlg = new AddAccountWindow { Owner = this };

                try
                {
                    var dlg = new AddAccountWindow { Owner = this };
                    dlg.ShowDialog();

                    await HideOverlayAsync();

                    msg.Execute(await dlg.WaitForButtonPressAsync());
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception);
                }

                //await HideOverlayAsync();

                //msg.Execute(await dlg.WaitForButtonPressAsync());
            }
        }

        private async void OpenAddGroupWindowMessageAction(ShowAddGroupWindowAction msg)
        {
            if (msg.ParentWindowKey == "SettingWindow")
            {
                // 先实现Overlay，再显示对话框，关闭对话框后，隐藏Overlay，最后再将添加结果返回给消息发送者
                await ShowOverlayAsync();

                try
                {
                    var dlg = new AddGroupWindow { Owner = this };
                    dlg.ShowDialog();

                    await HideOverlayAsync();

                    msg.Execute(await dlg.WaitForButtonPressAsync());
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception);
                }
            }
        }

        private async void ShowTipImageSelectWindowActionAction(ShowTipImageSelectWindowAction msg)
        {
            if (msg.ParentWindowKey == "SettingWindow")
            {
                try
                {
                    // 先实现Overlay，再显示对话框，关闭对话框后，隐藏Overlay，最后再将添加结果返回给消息发送者
                    ShowOverlayAsync();

                    TipLibSelectWindow dlg = new TipLibSelectWindow() { Owner = this };
                    dlg.ShowDialog();

                    HideOverlayAsync();

                    msg.Execute(await dlg.WaitForButtonPressAsync());
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception);
                }
            }
        }

        private void CloseWindowMessageAction(CloseWindowMessage msg)
        {
            if (msg.WindowKey == "SettingWindow")
            {
                Messenger.Default.Unregister(this);
                this.Close();
            }
        }

        private async void ShowPasswordDialogAsyncMessageAction(ShowPasswordDialogAsyncMessage msg)
        {
            // 为防止用户在极短时间内，两次点击同一按钮，导致同时显示两个对话框，在这里加以限制，以防止这种现象
            // 只有在上一个对话框结束后，才会允许显示新的Metro对话框
            if (_hasMetroDialog)
            {
                return;
            }

            _hasMetroDialog = true;

            var result = await this.ShowPasswordDialogAsync(msg.AccountId, msg.Buttons);

            _hasMetroDialog = false;

            msg.Execute(result);

            // 对话框显示完毕后，将角点再设置回显示对话框之前的元素上
            MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
        }

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

        private async void ShowXRayImageMessageAction(ShowXRayImageMessage msg)
        {
            // 为防止用户在极短时间内，两次点击同一按钮，导致同时显示两个对话框，在这里加以限制，以防止这种现象
            // 只有在上一个对话框结束后，才会允许显示新的Metro对话框
            if (_hasMetroDialog)
            {
                return;
            }

            _hasMetroDialog = true;

            await this.ShowImageDialogAsync(msg.Record, msg.BmpImage);

            // 对话框显示完毕后，将角点再设置回显示对话框之前的元素上
            MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));

            _hasMetroDialog = false;
        }

        private async void ShowImageBadChannelMessageAction(ShowBadChannelImageMessage msg)
        {
            // 为防止用户在极短时间内，两次点击同一按钮，导致同时显示两个对话框，在这里加以限制，以防止这种现象
            // 只有在上一个对话框结束后，才会允许显示新的Metro对话框
            if (_hasMetroDialog)
            {
                return;
            }

            _hasMetroDialog = true;

            await this.ShowBadChannelDialogAsync(msg.Record);

            // 对话框显示完毕后，将角点再设置回显示对话框之前的元素上
            MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));

            _hasMetroDialog = false;
        }
        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SettingWindow_OnKeyDown(object sender, KeyEventArgs e)
        {
            var content = PageFrame.Content as Page;
            if (content != null)
            {
                var vm = content.DataContext as PageViewModelBase;
                if (vm != null)
                {
                    vm.OnKeyDown(e);
                }
            }
        }

        private void SettingWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var content = PageFrame.Content as Page;
            if (content != null)
            {
                var vm = content.DataContext as PageViewModelBase;
                if (vm != null)
                {
                    vm.OnPreviewKeyDown(e);
                }
            }
        }

        private void SettingWindow_OnPreviewKeyUp(object sender, KeyEventArgs e)
        {
            var content = PageFrame.Content as Page;
            if (content != null)
            {
                var vm = content.DataContext as PageViewModelBase;
                if (vm != null)
                {
                    vm.OnPreviewKeyUp(e);
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            try
            {
                HttpNetworkController.Controller.SendingMsgAction -= Controller_SendingMsgAction;
                HttpNetworkController.Controller.SendCompleteAction -= Controller_SendCompleteAction;
                HttpNetworkController.Controller.ReceiveAction -= Controller_ReceiveAction;
            }
            catch (Exception ex)
            {
                Tracer.TraceException(ex);
            }
        }
    }
}
