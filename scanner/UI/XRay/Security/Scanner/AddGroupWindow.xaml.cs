using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Globalization;
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
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using UI.XRay.Business.Entities;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Scanner
{
    /// <summary>
    /// AddGroupWindow.xaml 的交互逻辑
    /// </summary>
    public partial class AddGroupWindow
    {
        readonly TaskCompletionSource<AccountGroup> _taskCompletionSource = new TaskCompletionSource<AccountGroup>();

        public AddGroupWindow()
        {
            InitializeComponent();
            GroupIdTextBox.Focus();
            Messenger.Default.Register<AccountGroupMessage>(this, AddGroupMessageAction);

            // 窗口关闭后，尝试再次设置任务为完成状态，防止用户使用Alt F4等方式强行关闭窗口，保证任务必须结束
            this.Closed += (sender, args) => _taskCompletionSource.TrySetResult(null);

            // 定时将窗口置顶，捕获按键消息
            WindowFocusHelper.MakeFocus(this);
        }

        private void AddGroupMessageAction(AccountGroupMessage message)
        {
            _taskCompletionSource.TrySetResult(message.Group);
            this.Close();
        }

        /// <summary>
        /// 异步等待用户按下按键或者点击按钮后关闭对话框，并返回结果
        /// </summary>
        public Task<AccountGroup> WaitForButtonPressAsync()
        {
            return _taskCompletionSource.Task;
        }
    }
}
