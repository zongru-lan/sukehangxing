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
using System.Windows.Shapes;
using GalaSoft.MvvmLight.Messaging;
using UI.XRay.Gui.Framework;
using UI.XRay.Parts.Keyboard;

namespace UI.XRay.Security.Scanner
{
    /// <summary>
    /// XRayGenWarmupWindow.xaml 的交互逻辑
    /// </summary>
    public partial class XRayGenWarmupWindow
    {
        public XRayGenWarmupWindow()
        {
            InitializeComponent();

            // 定时将窗口置顶，捕获按键消息
            WindowFocusHelper.MakeFocus(this);

            Messenger.Default.Register<CloseWindowMessage>(this, OnCloseWindowMessage);
            this.Closed += (sender, args) =>
            {
                Messenger.Default.Unregister(this);
            };
        }

        private void OnCloseWindowMessage(CloseWindowMessage message)
        {
            if (message.WindowKey == "XRayGenWarmupWindow")
            {
                this.Close();
            }
        }
    }
}
