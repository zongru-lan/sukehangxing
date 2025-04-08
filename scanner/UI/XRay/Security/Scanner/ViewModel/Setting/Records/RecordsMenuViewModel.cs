using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Records
{
    public class RecordsMenuViewModel : ViewModelBase
    {
        public RelayCommand NaviToBootLogPageCommand { get; set; }

        public RelayCommand NaviToLoginLogPageCommand { get; set; }

        public RelayCommand NaviToOperationPageCommand { get; set; }

        public RelayCommand NaviToConveyorWorkLogPageCommand { get; set; }

        public RelayCommand NaviToXRayGenWorkLogPageCommand { get; set; }

        public RelayCommand NaviToTipExamLogPageCommand { get; private set; }

        private IFramePageNavigationService _navigationService;

        public RecordsMenuViewModel(IFramePageNavigationService service)
        {
            _navigationService = service;
            NaviToBootLogPageCommand = new RelayCommand(NaviToStartupRecordsPageCommandExecute);
            NaviToLoginLogPageCommand = new RelayCommand(NaviToSessionRecordsPageCommandExecute);
            NaviToXRayGenWorkLogPageCommand = new RelayCommand(NaviToRadiationRecordsPageCommandExecute);
            NaviToTipExamLogPageCommand = new RelayCommand(NaviToTipExamRecordsPageCommandExecute);
            NaviToOperationPageCommand = new RelayCommand(NaviToOperationPageCommandExecute);
            NaviToConveyorWorkLogPageCommand = new RelayCommand(NaviToConveyorWorkLogPageCommandExecute);
        }

        private void NaviToConveyorWorkLogPageCommandExecute()
        {
            _navigationService.ShowPage("ConveyorWorkLogPage");
        }

        private void NaviToOperationPageCommandExecute()
        {
            _navigationService.ShowPage("OperationLogPage");
        }

        private void NaviToTipExamRecordsPageCommandExecute()
        {
            _navigationService.ShowPage("TipExamLogPage");
        }

        private void NaviToStartupRecordsPageCommandExecute()
        {
            _navigationService.ShowPage("BootLogPage");
        }

        private void NaviToSessionRecordsPageCommandExecute()
        {
            _navigationService.ShowPage("LoginLogPage");
        }

        private void NaviToRadiationRecordsPageCommandExecute()
        {
            _navigationService.ShowPage("XRayGenWorkLogPage");
        }
    }
}
