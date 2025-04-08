using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using UI.Common.Tracers;
using UI.XRay.Security.Configer;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Services;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Gui.Configer.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        public RelayCommand ShowSystemPageCommand { get; private set; }
        
        public RelayCommand ShowMachinePageCommand { get; private set; }

        public RelayCommand ShowXRayGenPageCommand { get; private set; }

        public RelayCommand ShowCapturePageCommand { get; private set; }

        public RelayCommand ShowControlPageCommand { get; private set; }

        public RelayCommand ShowKeyboardPageCommand { get; private set; }

        public RelayCommand ShowPreProcPageCommand { get; private set; }

        public RelayCommand ShowImagePageCommand { get; private set; }

        public RelayCommand ShowNetworkPageCommand { get; private set; }

        /// <summary>
        /// ���浱ǰҳ�ı���
        /// </summary>
        public RelayCommand SavePageChangesCommand { get; private set; }

        /// <summary>
        /// ������ǰҳ�ı���
        /// </summary>
        public RelayCommand DiscardPageChangesCommand { get; private set; }

        /// <summary>
        /// ��������
        /// </summary>
        public RelayCommand ImportCommand { get; private set; }

        /// <summary>
        /// ��������
        /// </summary>
        public RelayCommand ExportCommand { get; private set; }

        public RelayCommand ChangeModelCommand { get; private set; }
        /// <summary>
        /// ����
        /// </summary>
        public RelayCommand KeyPressCommand { get; private set; }

        public MainWindow Window
        {
            get { return Application.Current.MainWindow as MainWindow; }
        }

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            ShowSystemPageCommand = new RelayCommand(ShowSystemPageCommandExecute);
            ShowMachinePageCommand = new RelayCommand(ShowMachinePageCommandExecute);
            ShowXRayGenPageCommand = new RelayCommand(ShowXRayGenPageCommandExecute);
            ShowCapturePageCommand = new RelayCommand(ShowCapturePageCommandExecute);
            ShowControlPageCommand = new RelayCommand(ShowControlPageCommandExecute);
            ShowKeyboardPageCommand = new RelayCommand(ShowKeyboardPageCommandExecute);
            ShowImagePageCommand = new RelayCommand(ShowImagePageCommandExecute);
            ShowNetworkPageCommand = new RelayCommand(ShowNetworkPageCommandExecute);
            ShowPreProcPageCommand = new RelayCommand(ShowPreProcPageCommandExecute);
            SavePageChangesCommand = new RelayCommand(SavePageChangesCommandExecute);
            DiscardPageChangesCommand = new RelayCommand(DiscardPageChangesCommandExecute);
            ImportCommand = new RelayCommand(ImportCommandExecute);
            ExportCommand = new RelayCommand(ExportCommandExecute);
            ChangeModelCommand = new RelayCommand(ChangeModelCommandExecute);
            KeyPressCommand = new RelayCommand(KeyPressCommandExecute);
            MessengerInstance.Register<ChangeModelMessageAction>(this, ChangeModelMessageAction);
        }

        /// <summary>
        /// �����ı��豸�ͺŵ���Ϣ
        /// </summary>
        /// <param name="message"></param>
        private void ChangeModelMessageAction(ChangeModelMessageAction message)
        {
            try
            {
                if (ConfigImportExportHelper.Import(message.ModelFilePath))
                {
                    Window.RefreshCurrentPage();
                    MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer", "Model has been changed successfully."),
                        LanguageResourceExtension.FindTranslation("Configer", "Change Model"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer", "Failed to change model."),
                        LanguageResourceExtension.FindTranslation("Configer", "Change Model"), MessageBoxButton.OK, MessageBoxImage.Error);
                }

                // ���¿���ϵͳ�Ͳɼ�ϵͳ��Ӳ��
                if (!ControlSystemUpdater.Update())
                {
                    MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer", "Failed to save settings into Control Board System."),
                        LanguageResourceExtension.FindTranslation("Configer", "Exception"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer", "New settings have been saved into Control Board System successfully."), "",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception e)
            {
                
            }

            try
            {
                CaptureSysTypeEnum captureSysType;
                if (!ScannerConfig.Read<CaptureSysTypeEnum>(ConfigPath.CaptureSysType, out captureSysType))
                {
                    captureSysType = CaptureSysTypeEnum.DtGCUSTD;
                }

                if (captureSysType == CaptureSysTypeEnum.DtGCUSTD)
                {
                    if (!DtGCUSTDCaptureUpdater.Update())
                    {
                        MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer", "Failed to save settings to Capture Card."), "Exception", MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer", "New settings have been saved into Capture System."), "", MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
                else if (captureSysType == CaptureSysTypeEnum.TYM)
                {
                    if (!TYMCaptureUpdater.Update())
                    {
                        MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer", "Failed to save settings to Capture Card."), "Exception", MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer", "New settings have been saved into Capture System."), "", MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception e)
            {
                
            }
            
        }

        /// <summary>
        /// ��xml�ļ��е�����������
        /// </summary>
        private void ImportCommandExecute()
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Xml Files|*.xml|All Files|*.*";

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    if (ConfigImportExportHelper.Import(dlg.FileName))
                    {
                        Window.RefreshCurrentPage();
                        MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer","Configurations has been imported successfully."), 
                            LanguageResourceExtension.FindTranslation("Configer","Import Configuration"), MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer","Failed to import configurations."), 
                            LanguageResourceExtension.FindTranslation("Configer","Import Configuration"), MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception, "Failed to Import Config into registry.");
                    MessageBox.Show(exception.ToString(), "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// ���������õ�����xml�ļ���
        /// </summary>
        private void ExportCommandExecute()
        {
            var saveConfigDialog = new SaveFileDialog();
            saveConfigDialog.Filter = "Xml Files|*.xml|All Files|*.*";

            if (saveConfigDialog.ShowDialog() == true)
            {
                try
                {
                    if (ConfigImportExportHelper.Export(saveConfigDialog.FileName))
                    {
                        MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer","Export Completed."), 
                            LanguageResourceExtension.FindTranslation("Configer","Export"), MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer","Export Failed."), 
                            LanguageResourceExtension.FindTranslation("Configer","Export"), MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception, "Failed to Export Config from registry.");
                    MessageBox.Show(exception.ToString(), LanguageResourceExtension.FindTranslation("Configer","Exception"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DiscardPageChangesCommandExecute()
        {
            Window.DiscardPageChanges();
        }

        private void SavePageChangesCommandExecute()
        {
            Window.SavePageChanges();
        }

        private void ShowPreProcPageCommandExecute()
        {
            Window.ShowPage(SettingPageType.PreProcPage);
        }

        private void ShowImagePageCommandExecute()
        {
            Window.ShowPage(SettingPageType.ImagesPage);
        }

        private void ShowNetworkPageCommandExecute()
        {
            Window.ShowPage(SettingPageType.NetworkPage);
        }

        private void ShowKeyboardPageCommandExecute()
        {
            Window.ShowPage(SettingPageType.KeyboardPage);
        }

        private void ShowControlPageCommandExecute()
        {
            Window.ShowPage(SettingPageType.ControlPage);
        }

        private void ShowCapturePageCommandExecute()
        {
            Window.ShowPage(SettingPageType.CapturePage);
        }

        private void ShowSystemPageCommandExecute()
        {
            Window.ShowPage(SettingPageType.SystemPage);
        }

        private void ShowMachinePageCommandExecute()
        {
            Window.ShowPage(SettingPageType.MachinePage);
        }

        private void ShowXRayGenPageCommandExecute()
        {
            Window.ShowPage(SettingPageType.XRayGenPage);
        }

        private void ChangeModelCommandExecute()
        {
            Window.ShowChangeModelWindow();
        }

        private void KeyPressCommandExecute()
        {
            TouchKeyboardService.Service.OpenKeyboardWindow("HXkeyboard");
        }
    }
}