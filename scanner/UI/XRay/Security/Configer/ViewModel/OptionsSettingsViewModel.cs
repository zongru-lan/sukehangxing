using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UI.Common.Tracers;
using UI.XRay.Business.Entities;
using UI.XRay.Business.Logic;
using UI.XRay.Security.Configer.Interfaces;

namespace UI.XRay.Security.Configer.ViewModel
{
    /// <summary>
    /// 用户选项设置视图的ViewModel
    /// </summary>
    public class OptionsSettingsViewModel : ViewModelBase, IViewModel
    {
        private float _imageInterval;
        /// <summary>
        /// 培训图像产生的时间间隔，单位为秒，0表示连续过图
        /// </summary>
        public float ImageInterval
        {
            get { return _imageInterval; }
            set
            {
                _imageInterval = value;
                RaisePropertyChanged("ImageInterval");

                DataChanged = true;
            }
        }

        /// <summary>
        /// 培训图像产生时间间隔列表，用于UI绑定
        /// </summary>
        public List<float> ImageIntervals { get; set; }

        private bool _trainingIsEnabled;
        /// <summary>
        /// 是否启用培训模式
        /// </summary>
        public bool TrainingIsEnabled
        {
            get { return _trainingIsEnabled; }
            set
            {
                _trainingIsEnabled = value;
                RaisePropertyChanged("TrainingIsEnabled");

                DataChanged = true;
            }
        }

        private TrainingImageSimuMode _trainingMode = TrainingImageSimuMode.SingleTime;
        /// <summary>
        /// 培训模式类型
        /// </summary>
        public TrainingImageSimuMode TrainingMode
        {
            get { return _trainingMode; }
            set
            {
                _trainingMode = value;
                RaisePropertyChanged("TrainingMode");

                DataChanged = true;
            }
        }

        /// <summary>
        /// 培训模式类型列表，用于UI绑定
        /// </summary>
        public List<TrainingImageSimuMode> TrainingImageSimuModes { get; set; }

        private bool _autoLoginIsEnabled;
        /// <summary>
        /// 是否启用制动登录
        /// </summary>
        public bool AutoLoginIsEnabled
        {
            get { return _autoLoginIsEnabled; }
            set
            {
                _autoLoginIsEnabled = value;
                RaisePropertyChanged("AutoLoginIsEnabled");

                DataChanged = true;
            }
        }

        private string _userId;
        /// <summary>
        /// 自动登录默认的登录用户ID
        /// </summary>
        public string UserId
        {
            get { return _userId; }
            set
            {
                _userId = value;
                RaisePropertyChanged("UserId");

                DataChanged = true;
            }
        }

        /// <summary>
        /// 可以设置为自动登录用户的用户列表， todo 应该是从数据库或者其它保存方式获取
        /// </summary>
        public List<string> UserIds { get; set; }

        private PackageCounterType _pkgCounterType = PackageCounterType.Total;
        /// <summary>
        /// 包裹计数模式，可以是包裹总数或者单次登录
        /// </summary>
        public PackageCounterType PkgCounterType
        {
            get { return _pkgCounterType; }
            set
            {
                _pkgCounterType = value;
                RaisePropertyChanged("PkgCounterType");

                DataChanged = true;
            }
        }

        /// <summary>
        /// 包裹计数模式列表，用于UI绑定
        /// </summary>
        public List<PackageCounterType> PkgCounterTypes { get; set; }

        private bool _resetSessionCounterWhenLogin;
        /// <summary>
        /// 当用户登录时，是否清空包裹计数，只用于单次登录模式，当为true时，每次用户登录都清空包裹计数，否则，软件启动一次无论存在多少次用户登录，显示的都是开机以来的包裹数量
        /// </summary>
        public bool ResetSessionCounterWhenLogin
        {
            get { return _resetSessionCounterWhenLogin; }
            set
            {
                _resetSessionCounterWhenLogin = value;
                RaisePropertyChanged("ResetSessionCounterWhenLogin");

                DataChanged = true;
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public OptionsSettingsViewModel()
        {
            Tracer.TraceEnterFunc("OptionsSettingsViewModel Constructor.");

            ImageIntervals = new List<float> { 0, 5, 10, 15, 30, 60 };

            TrainingImageSimuModes = new List<TrainingImageSimuMode> 
            { 
                TrainingImageSimuMode.SingleTime, 
                TrainingImageSimuMode.NormalLoop,
                TrainingImageSimuMode.RandomLoop
            };

            PkgCounterTypes = new List<PackageCounterType> { PackageCounterType.Session, PackageCounterType.Total };

            Tracer.TraceExitFunc("OptionsSettingsViewModel Constructor.");
        }

        /// <summary>
        /// 
        /// </summary>
        public void SaveSettings()
        {
            Tracer.TraceEnterFunc("OptionsSettingsViewModel.SaveSettings.");

            try
            {
                ScannerConfig.Write(ConfigPath.TrainingImageInterval, _imageInterval);

                ScannerConfig.Write(ConfigPath.TrainingIsEnabled, _trainingIsEnabled);

                ScannerConfig.Write<TrainingImageSimuMode>(ConfigPath.TrainingMode, _trainingMode);

                ScannerConfig.Write(ConfigPath.AutoLoginIsEnabled, _autoLoginIsEnabled);

                ScannerConfig.Write(ConfigPath.AutoLoginUserId, _userId);

                ScannerConfig.Write<PackageCounterType>(ConfigPath.PkgCounterType, _pkgCounterType);

                ScannerConfig.Write(ConfigPath.PkgCounterResetSessionCounterWhenLogin, _resetSessionCounterWhenLogin);
            }
            catch (Exception e)
            {
                Tracer.TraceException(e, "Can't Save Options Settings.");

                MessageBox.Show("Can't Save Options Settings." + e.Message);
            }

            DataChanged = false;

            Tracer.TraceExitFunc("OptionsSettingsViewModel.SaveSettings.");
        }

        /// <summary>
        /// 
        /// </summary>
        public void LoadSettings()
        {
            Tracer.TraceEnterFunc("OptionsSettingsViewModel.LoadSettings.");

            try
            {
                float imageInterval;
                if (ScannerConfig.Read(ConfigPath.TrainingImageInterval, out imageInterval))
                {
                    ImageInterval = imageInterval;
                }
                //RaisePropertyChanged("ImageInterval");

                bool boolResult;

                if (ScannerConfig.Read(ConfigPath.TrainingIsEnabled, out boolResult))
                {
                    TrainingIsEnabled = boolResult;
                }
                //RaisePropertyChanged("TrainingIsEnabled");

                TrainingImageSimuMode trainingImageSimuMode;
                if (ScannerConfig.Read<TrainingImageSimuMode>(ConfigPath.TrainingMode, out trainingImageSimuMode))
                {
                    TrainingMode = trainingImageSimuMode;
                }
                //RaisePropertyChanged("TrainingMode");

                if (ScannerConfig.Read(ConfigPath.AutoLoginIsEnabled, out boolResult))
                {
                    AutoLoginIsEnabled = boolResult;
                }
                //RaisePropertyChanged("AutoLoginIsEnabled");

                string userId;
                if (ScannerConfig.Read(ConfigPath.AutoLoginUserId, out userId))
                {
                    UserId = userId;
                }
                //RaisePropertyChanged("UserId");

                PackageCounterType packageCounterType;
                if (ScannerConfig.Read<PackageCounterType>(ConfigPath.PkgCounterType, out packageCounterType))
                {
                    PkgCounterType = packageCounterType;
                }
                //RaisePropertyChanged("PkgCounterType");

                if (ScannerConfig.Read(ConfigPath.PkgCounterResetSessionCounterWhenLogin, out boolResult))
                {
                    ResetSessionCounterWhenLogin = boolResult;
                }
                //RaisePropertyChanged("ResetSessionCounterWhenLogin");
            }
            catch(Exception e)
            {
                Tracer.TraceException(e, "Can't Load Options Settings.");

                MessageBox.Show("Can't Load Options Settings." + e.Message);
            }

            DataChanged = false;

            Tracer.TraceExitFunc("OptionsSettingsViewModel.LoadSettings.");
        }

        /// <summary>
        /// 是否存在未保存的更改，true代表存在，false代表不存在
        /// </summary>
        public bool DataChanged { get; private set; }
    }
}
