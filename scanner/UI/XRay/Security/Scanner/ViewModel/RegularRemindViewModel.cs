using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Scanner.ViewModel
{
    public class RegularRemindViewModel : ViewModelBase
    {
        public RelayCommand<KeyEventArgs> PreviewKeyDownEventCommand { get; set; }

        public RegularRemindViewModel()
        {
            PreviewKeyDownEventCommand = new RelayCommand<KeyEventArgs>(PreviewKeyDownEventCommandExecute);
        }

        /// <summary>
        /// 处理按键消息，用户按下F3时关闭窗口
        /// </summary>
        /// <param name="args"></param>
        private void PreviewKeyDownEventCommandExecute(KeyEventArgs args)
        {
            if (args.Key == Key.F3)
            {
                args.Handled = true;
                CloseWindow();
            }
        }

        private void CloseWindow()
        {
            this.MessengerInstance.Send(new CloseWindowMessage("RegularRemindWindow"));
        }
    }
}
