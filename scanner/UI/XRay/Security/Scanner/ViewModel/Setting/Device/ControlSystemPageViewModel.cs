using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UI.Common.Tracers;
using UI.XRay.Control;
using UI.XRay.Flows.Services;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Device
{
    public class ControlSystemPageViewModel : PageViewModelBase
    {      
        public string Firmware
        {
            get { return _firmware; }
            set { _firmware = value; RaisePropertyChanged(); }
        }        

        public string Protocol
        {
            get { return _protocol; }
            set { _protocol = value; RaisePropertyChanged(); }
        }
        
        public Visibility SuccessfulVisibility
        {
            get { return _successfulVisibility; }
            set { _successfulVisibility = value; RaisePropertyChanged(); }
        }
        
        public Visibility FailedVisibility
        {
            get { return _failedVisibility; }
            set { _failedVisibility = value; RaisePropertyChanged(); }
        }        

        private string _firmware;
        private string _protocol;
        private Visibility _successfulVisibility;
        private Visibility _failedVisibility;

        public RelayCommand TestConnectionCommand { get; set; }

        public ControlSystemPageViewModel()
        {
            TestConnectionCommand = new RelayCommand(TestConnectionCommandExecute);
            SuccessfulVisibility = Visibility.Collapsed;
            FailedVisibility = Visibility.Collapsed;
        }

        private void TestConnectionCommandExecute()
        {
            CtrlSysVersion firmware = new CtrlSysVersion(0, 0);
            CtrlSysVersion protocol = new CtrlSysVersion(0, 0);
            try
            {
                if (ControlService.ServicePart.GetSystemDesc(ref firmware,ref protocol))
                {
                    Firmware = firmware.ToString();
                    Protocol = protocol.ToString();
                    SuccessfulVisibility = Visibility.Visible;
                    FailedVisibility = Visibility.Collapsed;
                }
                else
                {
                    SuccessfulVisibility = Visibility.Collapsed;
                    FailedVisibility = Visibility.Visible;
                }
            }
            catch (Exception exception)
            {
                SuccessfulVisibility = Visibility.Collapsed;
                FailedVisibility = Visibility.Visible;
                Tracer.TraceException(exception);
            }
        }

        public override void OnKeyDown(System.Windows.Input.KeyEventArgs args)
        {

        }

    }
}
