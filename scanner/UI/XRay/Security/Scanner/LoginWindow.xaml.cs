using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Messaging;
using MahApps.Metro.Controls.Dialogs;
using UI.XRay.Business.Entities;
using UI.XRay.Common.Utilities;
using UI.XRay.Flows.Controllers;
using UI.XRay.Gui.Framework;
using UI.XRay.Security.Scanner.MetroDialogs;
using System.Windows.Input;

namespace UI.XRay.Security.Scanner
{
    /// <summary>
    /// LoginWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow
    {
        /// <summary>
        /// 当前是否正在显示一个Metro对话框
        /// </summary>
        private bool _hasMetroDialog = false;

        public LoginWindow()
        {
            InitializeComponent();

            IdTextBox.Focus();

            // 注册信使消息，以便于从vm中关闭此窗口
            Messenger.Default.Register<CloseWindowMessage>(this, CloseWindowMessageAction);
            Messenger.Default.Register<ShowDialogMessageAction>(this, ShowMsgDialogMessageAction);

            // 定时将窗口置顶，捕获按键消息
            WindowFocusHelper.MakeFocus(this);
        }

        private void CloseWindowMessageAction(CloseWindowMessage msg)
        {
            if (msg.WindowKey == "LoginWindow")
            {
                Messenger.Default.Unregister(this);
                this.Close();
            }
        }

        private async void ShowMsgDialogMessageAction(ShowDialogMessageAction dialog)
        {
            if (_hasMetroDialog)
            {
                return;
            }

            _hasMetroDialog = true;

            dialog.Execute(
                await this.ShowMessageDialogAsync(dialog.Title, dialog.Notification, dialog.Buttons));
            

            _hasMetroDialog = false;
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
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
