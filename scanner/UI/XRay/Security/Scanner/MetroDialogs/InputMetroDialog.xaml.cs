using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Scanner.MetroDialogs
{
    /// <summary>
    /// 用于输入信息的Metro风格的对话框。
    /// 用法：
    ///     // parentMetroWindow 是用于显示对话框窗体的宿主窗口
    ///     var dlg = new InputMetroDialog(parentMetroWindow);
    /// 
    ///     // 先显示对话框
    ///     this.ShowMetroDialogAsync(dlg);
    /// 
    ///     // 异步等待对话框关闭并获取用户输入结果。如果用户点击取消，则返回结果为空
    ///     string input = await dlg.WaitForButtonPressAsync();
    ///     
    /// </summary>
    public partial class InputMetroDialog 
    {
        readonly TaskCompletionSource<InputMetroDialogResult> _taskCompletionSource = new TaskCompletionSource<InputMetroDialogResult>();
        private MetroDialogButtons Buttons { get; set; }

        public bool CancelButtonVisible
        {
            get { return Buttons == MetroDialogButtons.OkCancel || Buttons == MetroDialogButtons.OkNoCancel; }
        }

        public InputMetroDialog(MetroWindow owningWindow, MetroDialogButtons buttons, string message)
            : base(owningWindow, null)
        {
            InitializeComponent();

            Buttons = buttons;

            if (!CancelButtonVisible)
            {
                CancelButton.Visibility = Visibility.Collapsed;
            }

            MessageBlock.Text = message;

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
                // 屏蔽F4键，防止返回至前一页
                default:
                    args.Handled = true;
                    break;
            }

            // 屏蔽针对宿主窗口的所有按键消息
        }

        /// <summary>
        /// 异步等待用户按下按键或者点击按钮后关闭对话框，并返回结果
        /// </summary>
        /// <returns></returns>
        public Task<InputMetroDialogResult> WaitForButtonPressAsync()
        {
            InputTextBox.Focus();
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
            _taskCompletionSource.TrySetResult(new InputMetroDialogResult(MetroDialogResult.Ok, InputTextBox.Text));
        }

        /// <summary>
        /// 事件响应：用户点击 取消，取消此任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            await OwningWindow.HideMetroDialogAsync(this);
            _taskCompletionSource.TrySetResult(new InputMetroDialogResult(MetroDialogResult.Cancel, null));
        }
    }
}
