using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using GalaSoft.MvvmLight;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using GalaSoft.MvvmLight.Command;
using UI.Common.Tracers;
using UI.XRay.Flows.Services;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Configer.ViewModel
{
    /// <summary>
    /// 系统设置试图的ViewModel
    /// </summary>
    public class SystemSettingsViewModel : ViewModelBase, IViewModel
    {
        private string _language = "English";
        /// <summary>
        /// 系统语言，默认是英语
        /// </summary>
        public string Language
        {
            get { return _language; }
            set
            {
                _language = value;
                RaisePropertyChanged();
            }
        }

        private List<string> _availableLanguages;

        /// <summary>
        /// 可用系统语言列表，用于UI绑定
        /// </summary>
        public List<string> AvailableLanguages
        {
            get { return _availableLanguages; }
            set
            {
                _availableLanguages = value;
                RaisePropertyChanged();
            }
        }



        private string _companyName;
        /// <summary>
        /// 公司名称，默认是UI，可设置是用于OEM
        /// </summary>
        public string CompanyName
        {
            get { return _companyName; }
            set
            {
                _companyName = value;
                RaisePropertyChanged();
            }
        }

        private string _machineNumber;
        /// <summary>
        /// 设备编号
        /// </summary>
        public string MachineNumber
        {
            get { return _machineNumber; }
            set
            {
                _machineNumber = value;
                _machineNumber = _machineNumber.Replace("_", "");
                RaisePropertyChanged();
            }
        }
        
        private string _productionDate;

        public string ProductionDate
        {
            get { return _productionDate; }
            set { _productionDate = value; RaisePropertyChanged();}
        }
        
        
        private string _model;
        /// <summary>
        /// 设备类型
        /// </summary>
        public string Model
        {
            get { return _model; }
            set { _model = value; RaisePropertyChanged(); }
        }

        private string _description;
        /// <summary>
        /// 设备描述
        /// </summary>
        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 是否开启Shift功能键
        /// </summary>
        private bool _isEnableShiftKey;
        public bool IsEnableShiftKey
        {
            get { return _isEnableShiftKey; }
            set { _isEnableShiftKey = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 软件是否开机自动启动
        /// </summary>
        private bool _isAutoStartupEnabled;

        /// <summary>
        /// 软件是否开机自动启动
        /// </summary>
        public bool IsAutoStartupEnabled
        {
            get { return _isAutoStartupEnabled; }
            set { _isAutoStartupEnabled = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 操机员是否可以将图像转存到网络
        /// </summary>
        private bool _isCanNetDumpToOperator;

    
        public bool IsCanNetDumpToOperator
        {
            get { return _isCanNetDumpToOperator; }
            set { _isCanNetDumpToOperator = value; RaisePropertyChanged(); }
        }

        private bool _isUsingTouchUI;

        public bool IsUsingTouchUI
        {
            get { return _isUsingTouchUI; }
            set { _isUsingTouchUI = value; RaisePropertyChanged(); }
        }


        private string _autoLoginUserID;

        public string AutoLoginUserID
        {
            get { return _autoLoginUserID; }
            set { _autoLoginUserID = value; RaisePropertyChanged();}
        }

        private bool _isAutoLogin;

        private bool _isEnableLeaveHarbor;
        public bool IsEnableLeaveHarbor { get { return _isEnableLeaveHarbor; } set { _isEnableLeaveHarbor = value;  RaisePropertyChanged(); } }

        public bool IsAutoLogin
        {
            get { return _isAutoLogin; }
            set { 
                _isAutoLogin = value;
                if (_isAutoLogin)
                {
                    IsAutoLoginVisibility = Visibility.Visible;
                }
                else
                {
                    IsAutoLoginVisibility = Visibility.Collapsed;
                }
                RaisePropertyChanged();
            }
        }

        private Visibility _isAutoLoginVisibility;

        public Visibility IsAutoLoginVisibility
        {
            get { return _isAutoLoginVisibility; }
            set { _isAutoLoginVisibility = value; RaisePropertyChanged();}
        }
        
        

        /// <summary>
        /// 构造函数
        /// </summary>
        public SystemSettingsViewModel()
        {
            LoadAvailableLanguages();
            //AvailableLanguages = new List<string> { "English", "Chinese", "Russian"};


            try
            {
                LoadSettings();
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception, "Failed to load settings for SystemSettingsViewModel");
            }
        }

        

        /// <summary>
        /// 从bin上层目录中的language文件夹中，加载所有语言文件。
        /// </summary>
        private void LoadAvailableLanguages()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            path = Path.Combine(path, "../Language");

            var filePaths = Directory.GetFiles(path, "*.xml", SearchOption.TopDirectoryOnly);

            AvailableLanguages = new List<string>(3);

            foreach (var filePath in filePaths)
            {
                AvailableLanguages.Add(Path.GetFileNameWithoutExtension(filePath));   
            }
        }

        /// <summary>
        /// 保存系统设置到本地
        /// </summary>
        public void SaveSettings()
        {
            ScannerConfig.Write(ConfigPath.SystemLanguage, _language);
            //根据不同语言采用不同的日期格式
            int index = 0;
            switch (_language)
            {
                case "Chinese":
                    index = 2;
                    break;
                case "English":
                    index = 0;
                    break;
                case "Russian":
                    index = 1;
                    break;
                default:
                    index = 0;
                    break;
            }

            //写入日期格式标示
            ScannerConfig.Write(ConfigPath.SystemDateFormat, index);
            
            ScannerConfig.Write(ConfigPath.SystemCompanyName, _companyName);
            ScannerConfig.Write(ConfigPath.SystemMachineNum, _machineNumber);
            ScannerConfig.Write(ConfigPath.SystemMachineDate, _productionDate);
            ScannerConfig.Write(ConfigPath.SystemModel, _model);
            ScannerConfig.Write(ConfigPath.SystemDescription, _description);
            ScannerConfig.Write(ConfigPath.IsNetDumpToOperator, _isCanNetDumpToOperator);
            ScannerConfig.Write(ConfigPath.SystemTouchUI, IsUsingTouchUI);
            ScannerConfig.Write(ConfigPath.SystemEnableShiftKey, IsEnableShiftKey);
            ScannerConfig.Write(ConfigPath.IsLeaveHarborEnable, IsEnableLeaveHarbor);
            SystemStartShutdownService.EnabledAutoStartup(IsAutoStartupEnabled);
         
            SaveAutoLoginInfo();
        }

        private void SaveAutoLoginInfo()
        {
            ScannerConfig.Write(ConfigPath.AutoLoginIsEnabled, IsAutoLogin);
            if (!string.IsNullOrWhiteSpace(AutoLoginUserID))
            {
                ScannerConfig.Write(ConfigPath.AutoLoginUserId, AutoLoginUserID);
            }
            else
            {
                //用户名无效
                ScannerConfig.Write(ConfigPath.AutoLoginIsEnabled, false);
            }
        }

        /// <summary>
        /// 从本地加在系统设置
        /// </summary>
        public void LoadSettings()
        {
            IsAutoLogin = IsAutoLoginEnabled();
            AutoLoginUserID = GetAutoLoginUserID();

            string result;

            if (ScannerConfig.Read(ConfigPath.SystemLanguage, out result))
            {
                Language = result;
            }

            if (!ScannerConfig.Read(ConfigPath.SystemEnableShiftKey,out _isEnableShiftKey))
            {
                _isEnableShiftKey = true;
            }

            if (ScannerConfig.Read(ConfigPath.SystemCompanyName, out result))
            {
                CompanyName = result;
            }

            if (ScannerConfig.Read(ConfigPath.SystemMachineNum, out result))
            {
                MachineNumber = result;
            }

            if (ScannerConfig.Read(ConfigPath.SystemMachineDate, out result))
            {
                ProductionDate = result;
            }

            if (ScannerConfig.Read(ConfigPath.SystemModel, out result))
            {
                Model = result;
            }

            if (ScannerConfig.Read(ConfigPath.SystemDescription, out result))
            {
                Description = result;
            }

            IsAutoStartupEnabled = SystemStartShutdownService.IsAutoStartupEnabled();


            if (!ScannerConfig.Read(ConfigPath.SystemTouchUI, out _isUsingTouchUI))
            {
                _isUsingTouchUI = false;
            }

            if (!ScannerConfig.Read(ConfigPath.IsNetDumpToOperator, out _isCanNetDumpToOperator))
            {
                _isCanNetDumpToOperator = false;
            }

            if (!ScannerConfig.Read(ConfigPath.IsLeaveHarborEnable, out _isEnableLeaveHarbor))
            {
                _isEnableLeaveHarbor = false;
            }
        }

        private bool IsAutoLoginEnabled()
        {
            bool isEnabled;
            if (ScannerConfig.Read(ConfigPath.AutoLoginIsEnabled, out isEnabled))
            {
                return isEnabled;
            }

            return false;
        }

        private string GetAutoLoginUserID()
        {
            string user;
            if (ScannerConfig.Read(ConfigPath.AutoLoginUserId, out user))
            {
                return user;
            }

            return string.Empty ;
        }
    }
}
