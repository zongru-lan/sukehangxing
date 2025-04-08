using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Scanner.ViewModel
{
    public class DiagnosisWarningWindowViewModel : ViewModelBase
    {
        #region commands
        public RelayCommand<KeyEventArgs> PreviewKeyDownEventCommand { get; set; }

        public RelayCommand ClosedEventCommand { get; private set; }
        #endregion

        public DiagnosisWarningWindowViewModel()
        {
            PreviewKeyDownEventCommand = new RelayCommand<KeyEventArgs>(PreviewKeyDownEventCommandExecute);
            ClosedEventCommand = new RelayCommand(ClosedEventCommandExecute);
        }

        private void PreviewKeyDownEventCommandExecute(KeyEventArgs args)
        {

        }
        private void ClosedEventCommandExecute()
        {
            // 窗口关闭时，必须注销此事件
            MessengerInstance.Send(new CloseWindowMessage("DiagnosisWarningWindow"));
        }
    }
}
