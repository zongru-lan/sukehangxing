using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using UI.XRay.Gui.Framework;
using System.Windows;
using UI.XRay.Flows.Services;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Device
{
    public class DeviceMenuViewModel : ViewModelBase
    {
        public RelayCommand ShowDetectorsPageCommand { get; set; }
        public RelayCommand ShowXRayGenPageCommand { get; set; }
        public RelayCommand ShowKeyboardPageCommand { get; set; }
        public RelayCommand ShowConveyorPageCommand { get; set; }
        public RelayCommand ShowControlSystemPageCommand { get; set; }
        public RelayCommand ShowSelfDiagnosisPageCommand { get; set; }
        public RelayCommand ShowDeviceMaintenancePageCommand { get; set; }

        private Visibility _keyboardVisibility;

        public Visibility KeyboardVisibility
        {
            get { return _keyboardVisibility; }
            set { _keyboardVisibility = value; RaisePropertyChanged();}
        }
        

        private IFramePageNavigationService _navigation;

        public DeviceMenuViewModel(IFramePageNavigationService service)
        {
            _navigation = service;

            ShowDetectorsPageCommand = new RelayCommand(ShowDetectorsPageCommandExecute);
            ShowXRayGenPageCommand = new RelayCommand(ShowXRayGenPageCommandExecute);
            ShowKeyboardPageCommand = new RelayCommand(ShowKeyboardPageCommandExecute);
            ShowConveyorPageCommand = new RelayCommand(ShowConveyorPageCommandExecute);
            ShowControlSystemPageCommand = new RelayCommand(ShowControlSystemPageCommandExecute);
            ShowSelfDiagnosisPageCommand = new RelayCommand(ShowSelfDiagnosisPageCommandExecute);
            ShowDeviceMaintenancePageCommand = new RelayCommand(ShowDeviceMaintenancePageCommandExecute);

            KeyboardVisibility = ReadConfigService.Service.IsUseUSBCommandKeyboard ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ShowXRayGenPageCommandExecute()
        {
            _navigation.ShowPage("XRayGenPage");
        }

        private void ShowDetectorsPageCommandExecute()
        {
            _navigation.ShowPage("DetectorsPage");
        }

        private void ShowKeyboardPageCommandExecute()
        {
            _navigation.ShowPage("KeyboardPage");
        }

        private void ShowConveyorPageCommandExecute()
        {
            _navigation.ShowPage("ConveyorPESensorPage");
        }

        private void ShowControlSystemPageCommandExecute()
        {
            _navigation.ShowPage("ControlSystemPage");
        }

        private void ShowSelfDiagnosisPageCommandExecute()
        {
            _navigation.ShowPage("DiagnosisPage");
        }

        private void ShowDeviceMaintenancePageCommandExecute()
        {
            _navigation.ShowPage("DeviceMaintenancePage");
        }
    }
}
