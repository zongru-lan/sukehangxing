using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;

namespace UI.XRay.Security.Configer.ViewModel
{
    /// <summary>
    /// 图象预处理设置试图的ViewModel
    /// </summary>
    public class PreProcSettingsViewModel : ViewModelBase, IViewModel
    {
        private ushort _groundHighAvgUpper;

        /// <summary>
        /// 高能本地的平均值上限，超过该值则不能作为本底
        /// </summary>
        public ushort GroundHighAvgUpper
        {
            get { return _groundHighAvgUpper; }
            set
            {
                _groundHighAvgUpper = value;
                RaisePropertyChanged();
            }
        }

        private ushort _groundHighSingleUpper;

        /// <summary>
        /// 高能本地的单个像素的上限，超过该值不能作为本底，并可自动标记为坏点
        /// </summary>
        public ushort GroundHighSingleUpper
        {
            get { return _groundHighSingleUpper; }
            set
            {
                _groundHighSingleUpper = value;
                RaisePropertyChanged();
            }
        }

        private ushort _groundLowAvgUpper;

        /// <summary>
        /// 高低能本地的平均值上限，超过该值则不能作为本底
        /// </summary>
        public ushort GroundLowAvgUpper
        {
            get { return _groundLowAvgUpper; }
            set
            {
                _groundLowAvgUpper = value;
                RaisePropertyChanged();
            }
        }

        private ushort _groundLowSingleUpper;

        /// <summary>
        /// 低能本地的单个像素的上限，超过该值不能作为本底，并可自动标记为坏点
        /// </summary>
        public ushort GroundLowSingleUpper
        {
            get { return _groundLowSingleUpper; }
            set
            {
                _groundLowSingleUpper = value;
                RaisePropertyChanged();
            }
        }

        private ushort _airHighAvgLower;

        /// <summary>
        /// 高能满度的平均值下限，低于该值则不能作为满度
        /// </summary>
        public ushort AirHighAvgLower
        {
            get { return _airHighAvgLower; }
            set
            {
                _airHighAvgLower = value;
                RaisePropertyChanged();
            }
        }

        private ushort _airHighSingleLower;

        /// <summary>
        /// 高能满度的单个像素的下限，低于该值不能作为满度，并可自动标记为坏点
        /// </summary>
        public ushort AirHighSingleLower
        {
            get { return _airHighSingleLower; }
            set
            {
                _airHighSingleLower = value;
                RaisePropertyChanged();
            }
        }

        private ushort _airLowAvgLower;

        /// <summary>
        /// 低能满度的平均值下限，低于该值则不能作为满度
        /// </summary>
        public ushort AirLowAvgLower
        {
            get { return _airLowAvgLower; }
            set
            {
                _airLowAvgLower = value;
                RaisePropertyChanged();
            }
        }

        private ushort _airLowSingleLower;

        /// <summary>
        /// 低能满度的单个像素的下限，低于该值不能作为满度，并可自动标记为坏点
        /// </summary>
        public ushort AirLowSingleLower
        {
            get { return _airLowSingleLower; }
            set
            {
                _airLowSingleLower = value;
                RaisePropertyChanged();
            }
        }

        private bool _autoDetectBadChannels;

        /// <summary>
        /// 是否启用自动探测坏点，true代表启用，false代表不启用
        /// </summary>
        public bool AutoDetectBadChannels
        {
            get { return _autoDetectBadChannels; }
            set
            {
                _autoDetectBadChannels = value;
                RaisePropertyChanged();
            }
        }

        private bool _openNewPenetrationAlgorithm;

        /// <summary>
        /// 是否启用自动探测坏点，true代表启用，false代表不启用
        /// </summary>
        public bool OpenNewPenetrationAlgorithm
        {
            get { return _openNewPenetrationAlgorithm; }
            set
            {
                _openNewPenetrationAlgorithm = value;
                RaisePropertyChanged();
            }
        }

        private ushort _unpenetratableThreshold;

        /// <summary>
        /// 穿不透阈值（下限），低于该值表示穿不透区域，否则表示正常值
        /// </summary>
        public ushort UnpenetratableThreshold
        {
            get { return _unpenetratableThreshold; }
            set
            {
                _unpenetratableThreshold = value;
                RaisePropertyChanged();
            }
        }

        private ushort _bkgThreshold;

        /// <summary>
        /// 背景阈值（上限），超过该值表示背景，不存在包裹，否则表示正常包裹区域
        /// </summary>
        public ushort BkgThreshold
        {
            get { return _bkgThreshold; }
            set
            {
                _bkgThreshold = value;
                RaisePropertyChanged();
            }
        }

        private ushort _bgbThreshold;
        public ushort BgbThreshold
        {
            get { return _bgbThreshold; }
            set
            {
                _bgbThreshold = value;
                RaisePropertyChanged();
            }
        }

        public float DrugLowZ
        {
            get { return _drugLowZ; }
            set { _drugLowZ = value; RaisePropertyChanged(); }
        }

        public float DrugHighZ
        {
            get { return _drugHighZ; }
            set { _drugHighZ = value; RaisePropertyChanged(); }
        }

        public float ExplosivesLowZ
        {
            get { return _explosivesLowZ; }
            set { _explosivesLowZ = value; RaisePropertyChanged(); }
        }

        public float ExplosivesHighZ
        {
            get { return _explosivesHighZ; }
            set { _explosivesHighZ = value; RaisePropertyChanged(); }
        }

        private float _drugLowZ;

        private float _drugHighZ;

        private float _explosivesLowZ;

        private float _explosivesHighZ;

        public PreProcSettingsViewModel()
        {
            try
            {
                LoadSettings();
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void SaveSettings()
        {
            Tracer.TraceEnterFunc("PreProcSettingsViewModel.SaveSettings.");

            ScannerConfig.Write(ConfigPath.PreProcGroundHighAvgUpper, _groundHighAvgUpper);
            ScannerConfig.Write(ConfigPath.PreProcGroundHighSingleUpper, _groundHighSingleUpper);
            ScannerConfig.Write(ConfigPath.PreProcGroundLowAvgUpper, _groundLowAvgUpper);
            ScannerConfig.Write(ConfigPath.PreProcGroundLowSingleUpper, _groundLowSingleUpper);

            //ScannerConfig.Write(ConfigPath.PreProcGroundUpdateRateUpper, _groundUpdateRateUpper);

            ScannerConfig.Write(ConfigPath.PreProcAirHighAvgLower, _airHighAvgLower);
            ScannerConfig.Write(ConfigPath.PreProcAirHighSingleLower, _airHighSingleLower);
            ScannerConfig.Write(ConfigPath.PreProcAirLowAvgLower, _airLowAvgLower);
            ScannerConfig.Write(ConfigPath.PreProcAirLowSingleLower, _airLowSingleLower);

            //ScannerConfig.Write(ConfigPath.PreProcAirUpdateRateUpper, _airUpdateRateUpper);

            ScannerConfig.Write(ConfigPath.PreProcAutoDetectBadChannels, _autoDetectBadChannels);
            ScannerConfig.Write(ConfigPath.PreProcOpenNewPenetrationAlgorithm, _openNewPenetrationAlgorithm);
            ScannerConfig.Write(ConfigPath.PreProcUnpenetratableUpper, _unpenetratableThreshold);
            ScannerConfig.Write(ConfigPath.PreProcBkgThreshold, _bkgThreshold);
            ScannerConfig.Write(ConfigPath.PreProcBgbThreshold, _bgbThreshold);

            ScannerConfig.Write(ConfigPath.IntellisenseDrugHighZ, _drugHighZ);
            ScannerConfig.Write(ConfigPath.IntellisenseDrugLowZ, _drugLowZ);
            ScannerConfig.Write(ConfigPath.IntellisenseExplosivesHighZ, _explosivesHighZ);
            ScannerConfig.Write(ConfigPath.IntellisenseExplosivesLowZ, _explosivesLowZ);
        }

        /// <summary>
        /// 
        /// </summary>
        public void LoadSettings()
        {
            ushort result;

            if (ScannerConfig.Read(ConfigPath.PreProcGroundHighAvgUpper, out result))
            {
                GroundHighAvgUpper = result;
            }

            if (ScannerConfig.Read(ConfigPath.PreProcGroundHighSingleUpper, out result))
            {
                GroundHighSingleUpper = result;
            }

            if (ScannerConfig.Read(ConfigPath.PreProcGroundLowAvgUpper, out result))
            {
                GroundLowAvgUpper = result;
            }

            if (ScannerConfig.Read(ConfigPath.PreProcGroundLowSingleUpper, out result))
            {
                GroundLowSingleUpper = result;
            }

            if (ScannerConfig.Read(ConfigPath.PreProcAirHighAvgLower, out result))
            {
                AirHighAvgLower = result;
            }

            if (ScannerConfig.Read(ConfigPath.PreProcAirHighSingleLower, out result))
            {
                AirHighSingleLower = result;
            }

            if (ScannerConfig.Read(ConfigPath.PreProcAirLowAvgLower, out result))
            {
                AirLowAvgLower = result;
            }

            if (ScannerConfig.Read(ConfigPath.PreProcAirLowSingleLower, out result))
            {
                AirLowSingleLower = result;
            }

            bool autoDetectBadChannels;
            if (ScannerConfig.Read(ConfigPath.PreProcAutoDetectBadChannels, out autoDetectBadChannels))
            {
                AutoDetectBadChannels = autoDetectBadChannels;
            }
            bool openNewPenetrationAlgorithm;
            if (ScannerConfig.Read(ConfigPath.PreProcOpenNewPenetrationAlgorithm, out openNewPenetrationAlgorithm))
            {
                OpenNewPenetrationAlgorithm = openNewPenetrationAlgorithm;
            }

            if (ScannerConfig.Read(ConfigPath.PreProcUnpenetratableUpper, out result))
            {
                UnpenetratableThreshold = (ushort) result;
            }

            if (ScannerConfig.Read(ConfigPath.PreProcBkgThreshold, out result))
            {
                BkgThreshold = (ushort) result;
            }

            if (ScannerConfig.Read(ConfigPath.PreProcBgbThreshold, out result))
            {
                BgbThreshold = (ushort)result;
            }

            if (!ScannerConfig.Read(ConfigPath.IntellisenseDrugHighZ, out _drugHighZ))
            {
                _drugHighZ = 0;
            }
            DrugHighZ = _drugHighZ;

            if (!ScannerConfig.Read(ConfigPath.IntellisenseDrugLowZ, out _drugLowZ))
            {
                _drugLowZ = 0;
            }
            DrugLowZ = _drugLowZ;

            if (!ScannerConfig.Read(ConfigPath.IntellisenseExplosivesHighZ, out _explosivesHighZ))
            {
                _explosivesHighZ = 0;
            }
            ExplosivesHighZ = _explosivesHighZ;

            if (!ScannerConfig.Read(ConfigPath.IntellisenseExplosivesLowZ, out _explosivesLowZ))
            {
                _explosivesLowZ = 0;
            }
            ExplosivesLowZ = _explosivesLowZ;
        }
    }
}
