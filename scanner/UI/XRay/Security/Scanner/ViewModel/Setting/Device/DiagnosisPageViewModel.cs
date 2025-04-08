using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Services;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Device
{
    public class DiagnosisPageViewModel : PageViewModelBase
    {
        public RelayCommand DiagnosisCommand { get; set; }
        public RelayCommand SaveReportCommand { get; set; }
        public RelayCommand PrintReportCommand { get; set; }

        private string _diagnosisInformation;

        public string DiagnosisInformation
        {
            get { return _diagnosisInformation; }
            set { _diagnosisInformation = value; RaisePropertyChanged(); }
        }

        private bool _isDiagnosisEnable = true;

        public bool IsDiagnosisEnable
        {
            get { return _isDiagnosisEnable; }
            set { _isDiagnosisEnable = value; RaisePropertyChanged(); }
        }


        private Visibility _hasDiagnosisInformation;
        public Visibility HasDiagnosisInformation
        {
            get
            {
                _hasDiagnosisInformation = string.IsNullOrWhiteSpace(DiagnosisInformation) ? Visibility.Collapsed : Visibility.Visible;
                return _hasDiagnosisInformation;
            }
            set { _hasDiagnosisInformation = value; RaisePropertyChanged(); }
        }

        private string _buttonText;

        public string ButtonText
        {
            get { return _buttonText; }
            set { _buttonText = value; RaisePropertyChanged(); }
        }


        public DiagnosisPageViewModel()
        {
            DiagnosisCommand = new RelayCommand(TestDiagnosisCommandExecute);
            SaveReportCommand = new RelayCommand(SaveReportCommandExecute);
            PrintReportCommand = new RelayCommand(PrintReportCommandExecute);

            ButtonText = TranslationService.FindTranslation("Diagnose");
        }
        bool _isWarning = false;
        private async void TestDiagnosisCommandExecute()
        {
            if (!SystemStatus.Instance.EmergencyStop)
            {
                var message = new ShowDialogMessageAction("SettingWindow", TranslationService.FindTranslation("Warning"),
                    TranslationService.FindTranslation("Can't diagnose because of emergency stop!"), MetroDialogButtons.Ok, result => { });

                this.MessengerInstance.Send(message);
            }
            else if (!_isWarning)
            {
                var message = new ShowDialogMessageAction("SettingWindow", TranslationService.FindTranslation("Warning"),
                    TranslationService.FindTranslation("In the process of diagnosis, the ray will turn on and the conveyor will move back, please ensure the safety of personnel and luggage."), MetroDialogButtons.Ok,
                    result =>
                    {
                        _isWarning = true;
                    });

                this.MessengerInstance.Send(message);
            }
            else
            {
                SystemStateService.Service.IsDiagnosing = true;
                ButtonText = TranslationService.FindTranslation("Diagnosing");
                IsDiagnosisEnable = false;
                ControlService.ServicePart.PowerOnPESensors(true);
                var task = SystemStateService.Service.GetDiagnosisReport();
                if (task != null)
                {
                    DiagnosisInformation = await task;
                }

                RaisePropertyChanged("HasDiagnosisInformation");
                new OperationRecordService().AddRecord(new OperationRecord()
                {
                    AccountId = LoginAccountManager.Service.CurrentAccount != null ? LoginAccountManager.Service.CurrentAccount.AccountId : "",
                    OperateUI = OperationUI.Diagnosis,
                    OperateTime = DateTime.Now,
                    OperateObject = "Diagnosis",
                    OperateCommand = OperationCommand.Find,
                    OperateContent = string.Empty,
                });
                ControlService.ServicePart.PowerOnPESensors(false);
                ButtonText = TranslationService.FindTranslation("Diagnose");
                IsDiagnosisEnable = true;
                SystemStateService.Service.IsDiagnosing = false;
                //如在出图界面，则关闭诊断警告窗口
                MessengerInstance.Send(new CloseWindowMessage("DiagnosisWarningWindow"));
                _isWarning = false;
            }
        }
        private void SaveReportCommandExecute()
        {
            var msg = new ShowFolderBrowserDialogMessageAction("SettingWindow", s =>
            {
                if (s != null)
                {
                    var filepath = ConfigHelper.GetDiagnosisExportFileName(OperationUI.BootLog, s, "Diagnosis");
                    using (FileStream fsWrite = new FileStream(filepath, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        byte[] buffer = Encoding.Default.GetBytes(DiagnosisInformation);
                        fsWrite.Write(buffer, 0, buffer.Length);
                    }
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessengerInstance.Send(new ShowDialogMessageAction("SettingWindow",
                            "", TranslationService.FindTranslation("Dump completely"), MetroDialogButtons.Ok,
                            result =>
                            {

                            }));
                    });
                    new OperationRecordService().AddRecord(new OperationRecord()
                    {
                        AccountId = LoginAccountManager.Service.CurrentAccount != null ? LoginAccountManager.Service.CurrentAccount.AccountId : "",
                        OperateUI = OperationUI.Diagnosis,
                        OperateTime = DateTime.Now,
                        OperateObject = "Diagnosis",
                        OperateCommand = OperationCommand.Saveto,
                        OperateContent = ConfigHelper.AddQuotationForPath(filepath),
                    });
                }
            });
            MessengerInstance.Send(msg);

        }

        private void PrintReportCommandExecute()
        {

        }

        public override void OnKeyDown(System.Windows.Input.KeyEventArgs args)
        {

        }
    }
}
