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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Messaging;
using UI.XRay.Common.Utilities;
using UI.XRay.Flows.Controllers;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Scanner
{
    /// <summary>
    /// 手动校正本底、满度的窗口
    /// </summary>
    public partial class CalibrateWindow 
    {
        public CalibrateWindow()
        {
            InitializeComponent();
            Messenger.Default.Register<CloseWindowMessage>(this, CloseWindowMessageAction);

            // 定时将窗口置顶，捕获按键消息
            WindowFocusHelper.MakeFocus(this);
        }

        private void CloseWindowMessageAction(CloseWindowMessage message)
        {
            if (message.WindowKey == "CalibrateWindow")
            {
                this.Close();
                Messenger.Default.Unregister(this);
            }
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CalibrateWindow_OnClosed(object sender, EventArgs e)
        {
            SystemStatesMonitor.Monitor.CheckedCaptureSysAfterAppStarted = true;
        }
    }
}
