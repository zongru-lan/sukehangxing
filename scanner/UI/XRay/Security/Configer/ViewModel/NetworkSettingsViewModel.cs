using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Threading.Tasks;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;

namespace UI.XRay.Security.Configer.ViewModel
{
    public class NetworkSettingsViewModel : ViewModelBase, IViewModel
    {
        private string _localIp;

        public string LocalIp
        {
            get { return _localIp; }
            set { _localIp = value; RaisePropertyChanged(); }
        }
        private string _serverIp;

        public string ServerIp
        {
            get { return _serverIp; }
            set { _serverIp = value; RaisePropertyChanged(); }
        }

        private bool _enable;

        public bool HttpEnable
        {
            get { return _enable; }
            set
            {
                _enable = value; RaisePropertyChanged();
                if (value)
                {
                    ParamVisibility = Visibility.Visible;
                }
                else
                {
                    ParamVisibility = Visibility.Collapsed;
                }
            }
        }

        private Visibility _paramVisibility;

        public Visibility ParamVisibility
        {
            get { return _paramVisibility; }
            set { _paramVisibility = value; RaisePropertyChanged(); }
        }
        

        public NetworkSettingsViewModel()
        {
            LoadSettings();
        }
        public void LoadSettings()
        {
            if (!ScannerConfig.Read(ConfigPath.SystemHttpServiceIp, out _serverIp))
            {
            }
            if (!ScannerConfig.Read(ConfigPath.SystemHttpLocalIp, out _localIp))
            {
                _localIp = "http://127.0.0.1:9090/form/";
            }
            if (!ScannerConfig.Read(ConfigPath.SystemHttpEnable,out _enable))
            {
                _enable = false;
            }
            if (_enable)
            {
                ParamVisibility = Visibility.Visible;
            }
            else
            {
                ParamVisibility = Visibility.Collapsed;
            }
        }
        public void SaveSettings()
        {
            ScannerConfig.Write(ConfigPath.SystemHttpServiceIp, _serverIp);
            ScannerConfig.Write(ConfigPath.SystemHttpLocalIp, _localIp);
            ScannerConfig.Write(ConfigPath.SystemHttpEnable, _enable);
        }
    }
}
