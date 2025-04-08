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

namespace UI.XRay.Security.Configer
{
    /// <summary>
    /// ChangeModelWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ChangeModelWindow 
    {
        public ChangeModelWindow()
        {
            InitializeComponent();

            Messenger.Default.Register<CloseWindowMessage>(this, OnCloseWindowMessageAction);
        }

        private void OnCloseWindowMessageAction(CloseWindowMessage message)
        {
            if (message.WindowKey == "ChangeModelWindow")
            {
                Close();
            }
        }
    }
}
