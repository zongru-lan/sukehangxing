using System;
using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Security.Configer.UserControl;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Configer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow 
    {
        public MainWindow()
        {
            try
            {
                InitializeComponent();
                ShowPage(SettingPageType.SystemPage);

                ScannerConfig.ConfigChanged += ScannerConfig_ConfigChanged;
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        /// <summary>
        /// 保存当前的变更
        /// </summary>
        public void SavePageChanges()
        {
            var control = PageControl.Content as System.Windows.Controls.UserControl;
            if (control != null)
            {
                var dc = control.DataContext as IViewModel;
                if (dc != null)
                {
                    dc.SaveSettings();
                }
            }
        }

        /// <summary>
        /// 取消当前页的变更
        /// </summary>
        public void DiscardPageChanges()
        {
            var control = PageControl.Content as System.Windows.Controls.UserControl;
            if (control != null)
            {
                var dc = control.DataContext as IViewModel;
                if (dc != null)
                {
                    dc.LoadSettings();
                }
            }
        }

        /// <summary>
        /// 更新当前页信息
        /// </summary>
        public void RefreshCurrentPage()
        {
            DiscardPageChanges();
        }

        /// <summary>
        /// 显示指定的配置页
        /// </summary>
        /// <param name="page"></param>
        public void ShowPage(SettingPageType page)
        {
            object control = null;

            try
            {
                switch (page)
                {
                    case SettingPageType.SystemPage:
                        control = Application.LoadComponent(new Uri(
                                "/Pages/SystemSettingsUserControl.xaml", UriKind.Relative));
                        break;
                    case SettingPageType.MachinePage:
                        control = Application.LoadComponent(new Uri(
                                "/Pages/MachineSettingsUserControl.xaml", UriKind.Relative));
                        break;
                    case SettingPageType.ControlPage:
                        control = Application.LoadComponent(new Uri(
                                "/Pages/ControlSystemSettingsUserControl.xaml", UriKind.Relative));
                        break;
                    case SettingPageType.CapturePage:
                        control = Application.LoadComponent(new Uri(
                                "/Pages/CaptureSystemSettingsUserControl.xaml", UriKind.Relative));
                        break;
                    case SettingPageType.XRayGenPage:
                        control = Application.LoadComponent(new Uri(
                                "/Pages/XRayGenSettingsUserControl.xaml", UriKind.Relative));
                        break;
                    case SettingPageType.KeyboardPage:
                        control = Application.LoadComponent(new Uri(
                                "/Pages/KeyboardSettingsUserControl.xaml", UriKind.Relative));
                        break;
                    case SettingPageType.ImagesPage:
                        control = Application.LoadComponent(new Uri(
                                "/Pages/ImagesSettingsUserControl.xaml", UriKind.Relative));
                        break;
                    case SettingPageType.PreProcPage:
                        control = Application.LoadComponent(new Uri(
                                "/Pages/PreProcSettingsUserControl.xaml", UriKind.Relative));
                        break;
                    case SettingPageType.NetworkPage:
                        control = Application.LoadComponent(new Uri(
                                "/Pages/NetworkSettingsUserControl.xaml", UriKind.Relative));
                        break;
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
                MessageBox.Show(exception.Message, LanguageResourceExtension.FindTranslation("Configer","Exception"), MessageBoxButton.OK, MessageBoxImage.Error);
            }

            var uc = control as System.Windows.Controls.UserControl;
            if (uc != null)
            {
                uc.MinWidth = 400;
            }
           
            PageControl.Content = control;
        }

        /// <summary>
        /// 显示更改设备型号窗口
        /// </summary>
        public void ShowChangeModelWindow()
        {
            this.ShowOverlay();

            try
            {
                var dlg = new ChangeModelWindow();
                dlg.Owner = this;
                dlg.ShowDialog();
            }
            finally
            {
                this.HideOverlay();
            }
        }

        void ScannerConfig_ConfigChanged(object sender, EventArgs e)
        {
        }
    }
}
