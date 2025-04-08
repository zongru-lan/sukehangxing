using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using UI.XRay.Gui.Framework;
using UI.XRay.Flows.Services;

namespace UI.XRay.Security.Scanner.MetroDialogs
{
    /// <summary>
    /// InputPasswordDialog.xaml 的交互逻辑
    /// </summary>
    public partial class PasswordMetroDialog 
    {
        readonly TaskCompletionSource<PasswordMetroDialogResult> _taskCompletionSource = new TaskCompletionSource<PasswordMetroDialogResult>();
        private MetroDialogButtons Buttons { get; set; }

        public bool CancelButtonVisible
        {
            get { return Buttons == MetroDialogButtons.OkCancel || Buttons == MetroDialogButtons.OkNoCancel; }
        }

        public PasswordMetroDialog(MetroWindow owningWindow, MetroDialogButtons buttons, string userId)
            : base(owningWindow, null)
        {
            InitializeComponent();

            Buttons = buttons;

            if (!CancelButtonVisible)
            {
                CancelButton.Visibility = Visibility.Collapsed;
            }

            AccountIdBlock.Text = userId;
            this.PreviewKeyDown += OnPreviewKeyDown;
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            this.Focus();
        }

        /// <summary>
        /// 按键事件处理：F1为 确定，F2为 取消
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnPreviewKeyDown(object sender, KeyEventArgs args)
        {
            switch (args.Key)
            {
                case Key.F1:
                case Key.Enter:
                    OkButton_OnClick(this, null);
                    args.Handled = true;
                    break;

                case Key.F2:
                case Key.Escape:
                    if (CancelButtonVisible)
                    {
                        CancelButton_OnClick(this, null);
                    }
                    args.Handled = true;
                    break;

                    // 屏蔽F3键，防止宿主窗口被关闭
                case Key.F3:
                    args.Handled = true;
                    break;

                // 屏蔽F4键，防止返回至前一页
                case Key.F4:
                    args.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// 异步等待用户按下按键或者点击按钮后关闭对话框，并返回结果
        /// </summary>
        /// <returns></returns>
        public Task<PasswordMetroDialogResult> WaitForButtonPressAsync()
        {
            PasswordBox.Focus();
            return _taskCompletionSource.Task;
        }

        /// <summary>
        /// 事件响应：用户点击 确定，设定任务结果，并关闭对话框，将结果返回给异步等待者
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            await OwningWindow.HideMetroDialogAsync(this);
            _taskCompletionSource.TrySetResult(new PasswordMetroDialogResult(MetroDialogResult.Ok, PasswordBox.Password));
        }

        /// <summary>
        /// 事件响应：用户点击 取消，设定任务结果为空，并关闭对话框
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            await OwningWindow.HideMetroDialogAsync(this);
            _taskCompletionSource.TrySetResult(new PasswordMetroDialogResult(MetroDialogResult.Cancel, null));
        }

        private void KeyboardButton_Click(object sender, RoutedEventArgs e)
        {
            TouchKeyboardService.Service.OpenKeyboardWindow("HXkeyboard");
        }
    }
}
