using GalaSoft.MvvmLight.Messaging;
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
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Scanner
{
    /// <summary>
    /// RemoteDiagnose.xaml 的交互逻辑
    /// </summary>
    public partial class RemoteDiagnose 
    {
        public RemoteDiagnose()
        {
            InitializeComponent();
            Messenger.Default.Register<CloseWindowMessage>(this, CloseWindowMessageAction);
            WindowFocusHelper.MakeFocus(this);
        }

        private void CloseWindowMessageAction(CloseWindowMessage message)
        {
            if (message.WindowKey == "RemoteDiagnose")
            {
                this.Close();
                Messenger.Default.Unregister(this);
            }
        }

       
    }
}
