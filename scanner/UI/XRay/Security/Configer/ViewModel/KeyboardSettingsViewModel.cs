using GalaSoft.MvvmLight;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Services;
using UI.XRay.ImagePlant;

namespace UI.XRay.Security.Configer.ViewModel
{

    /// <summary>
    /// 键盘设置视图的ViewModel
    /// </summary>
    public class KeyboardSettingsViewModel : ViewModelBase, IViewModel
    {
        private readonly string _keyboardComNameDefault = "USB Serial Port";

        private string _comName = "USB Serial Port";

        /// <summary>
        /// 键盘使用的串口号
        /// </summary>
        public string ComName
        {
            get { return _comName; }
            set
            {
                _comName = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 是否自动检测键盘
        /// </summary>
        private bool _autoRestart;

        public List<string> ComNames { get; set; }

        /// <summary>
        /// 是否自动检测键盘
        /// </summary>
        public bool AutoRestart
        {
            get { return _autoRestart; }
            set { _autoRestart = value; RaisePropertyChanged(); }
        }

        public KeyboardSettingsViewModel()
        {
            Tracer.TraceEnterFunc("KeyboardSettingsViewModel Constructor.");

            try
            {
                GetComList();
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            try
            {
                LoadSettings();
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            Tracer.TraceExitFunc("KeyboardSettingsViewModel Constructor.");
        }
        
        /// <summary>
        /// 获取本机所有的串口
        /// </summary>
        private void GetComList()
        {
            ComNames = new List<string>();
            ComNames.Add("USB Serial Port");
            ComNames.Add("USB Common Keyboard");

            var keyCom = Registry.LocalMachine.OpenSubKey(@"Hardware\DeviceMap\SerialComm");
            if (keyCom != null)
            {
                var sSubKeys = keyCom.GetValueNames();
                foreach (string sName in sSubKeys)
                {
                    var sValue = (string)keyCom.GetValue(sName);
                    ComNames.Add(sValue);
                }
            }
        }

        public void SaveSettings()
        {
            if (ComName != null)
            {
                ScannerConfig.Write(ConfigPath.KeyboardComName, ComName);
            }

            ScannerConfig.Write(ConfigPath.KeyboardAutoDetect, _autoRestart);
        }

        public void LoadSettings()
        {
            string comName;
            if (ScannerConfig.Read(ConfigPath.KeyboardComName, out comName))
            {
                ComName = comName;
            }
            else
            {
                ComName = _keyboardComNameDefault;
            }

            if (!ScannerConfig.Read(ConfigPath.KeyboardAutoDetect, out _autoRestart))
            {
                AutoRestart = true;
            }
        }
    }
}
