using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Gui.Framework;
using UI.XRay.Security.Scanner.Converters;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Device
{
    internal enum RemindInterval
    {
        Never = -1,
        EverySeason = 120,
        EveryHalfYear = 180,
        EveryYear = 365,
    }

    public class DeviceMaintenancePageViewModel : PageViewModelBase
    {
        #region Fields
        private List<int> intervals = new List<int> { 120, 180, 365 };
        #endregion

        #region Binding
        private bool _isEnableDeviceMaintenance;
        public bool IsEnableDeviceMaintenance
        {
            get => _isEnableDeviceMaintenance;
            set { _isEnableDeviceMaintenance = value; RaisePropertyChanged(); }
        }

        private bool _isLoopRemind;
        public bool IsLoopRemind
        {
            get => _isLoopRemind;
            set { _isLoopRemind = value; RaisePropertyChanged(); }
        }

        public List<string> MaintenanceRemindInterval { get; private set; }
        private int _selectedIntervalIndex;
        public int SelectedIntervalIndex
        {
            get => _selectedIntervalIndex;
            set { _selectedIntervalIndex = value; RaisePropertyChanged(); }
        }

        private string _lastMaintenanceTimeStr;
        public string LastMaintenanceTimeStr
        {
            get => _lastMaintenanceTimeStr;
            set { _lastMaintenanceTimeStr = value; RaisePropertyChanged(); }
        }

        public RelayCommand ChangeEnableDeviceMantenanceCommand { get; private set; }
        public RelayCommand ChangeLoopRemindCommand { get; private set; }
        public RelayCommand SaveMaintenanceIntervalCommand { get; private set; }
        public RelayCommand UpdateLastMaintenanceTimeCommand { get; private set; }
        
        #endregion

        #region Constructor
        public DeviceMaintenancePageViewModel()
        {
            ChangeEnableDeviceMantenanceCommand = new RelayCommand(ChangeEnableDeviceMantenanceCommandExecute);
            ChangeLoopRemindCommand = new RelayCommand(ChangeLoopRemindCommandExecute);
            SaveMaintenanceIntervalCommand = new RelayCommand(SaveMaintenanceIntervalCommandExecute);
            UpdateLastMaintenanceTimeCommand = new RelayCommand(UpdateLastMaintenanceTimeCommandExecute);
            MaintenanceRemindInterval = new List<string>
            {
                TranslationService.FindTranslation("Device Maintenance", "EverySeason"),
                TranslationService.FindTranslation("Device Maintenance", "EveryHalfYears"),
                TranslationService.FindTranslation("Device Maintenance", "EveryYear"),
            };

            #region Read Config
            int interval = -1;
            ScannerConfig.Read(ConfigPath.SystemRemindTimeInterval, out interval);
            if (interval == -1)
            {
                IsEnableDeviceMaintenance = false;
                SelectedIntervalIndex = 0;
                ScannerConfig.Write(ConfigPath.SystemRemindTimeInterval, -1);
            }
            else
            {
                IsEnableDeviceMaintenance = true;
                SelectedIntervalIndex = intervals.FindIndex(x => x == interval);
            }

            if (!ScannerConfig.Read(ConfigPath.IsLoopRemind, out _isLoopRemind))
            {
                IsLoopRemind = false;
                ScannerConfig.Write(ConfigPath.IsLoopRemind, false);
            }

            long lastMaintenanceTimeTicks;
            if (!ScannerConfig.Read(ConfigPath.LastMaintenanceTime, out lastMaintenanceTimeTicks))
            {
                lastMaintenanceTimeTicks = DateTime.Now.Ticks;
                ScannerConfig.Write(ConfigPath.LastMaintenanceTime, lastMaintenanceTimeTicks);
            }
            
            LastMaintenanceTimeStr = (new DateTime(lastMaintenanceTimeTicks)).ToString(DateFormatHelper.GetDateFormatHelper() + " HH:mm:ss");
            #endregion

            if (IsInDesignMode)
            {
                IsEnableDeviceMaintenance = true;
            }
        }
        #endregion

        #region Methods
        #region CommandExecute
        private void ChangeEnableDeviceMantenanceCommandExecute()
        {
            if (!IsEnableDeviceMaintenance)
            {
                ScannerConfig.Write(ConfigPath.SystemRemindTimeInterval, -1);
            }
            else
            {
                SelectedIntervalIndex = 0;
                Properties.Settings.Default.HasMaintenanceReminded = false;
                Properties.Settings.Default.Save();
                ScannerConfig.Write(ConfigPath.SystemRemindTimeInterval, intervals[SelectedIntervalIndex]);
            }
        }

        private void ChangeLoopRemindCommandExecute()
        {
            Properties.Settings.Default.HasMaintenanceReminded = false;
            Properties.Settings.Default.Save();
            ScannerConfig.Write(ConfigPath.IsLoopRemind, IsLoopRemind);
        }

        private void SaveMaintenanceIntervalCommandExecute()
        {
            Properties.Settings.Default.HasMaintenanceReminded = false;
            Properties.Settings.Default.Save();
            ScannerConfig.Write(ConfigPath.SystemRemindTimeInterval, intervals[SelectedIntervalIndex]);
        }

        private void UpdateLastMaintenanceTimeCommandExecute()
        {
            var now = DateTime.Now;
            LastMaintenanceTimeStr = now.ToString(DateFormatHelper.GetDateFormatHelper() + " HH:mm:ss");
            Properties.Settings.Default.HasMaintenanceReminded = false;
            Properties.Settings.Default.Save();
            ScannerConfig.Write(ConfigPath.LastMaintenanceTime, now.Ticks);
        }

        
        #endregion

        #region Super Class - abstract
        public override void OnKeyDown(KeyEventArgs args)
        {
            return;
        }
        #endregion
        #endregion
    }
}
