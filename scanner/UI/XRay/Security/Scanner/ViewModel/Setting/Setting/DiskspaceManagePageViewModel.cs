using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight.Command;
using UI.XRay.Common.Utilities;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Services;
using UI.XRay.Business.Entities;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Setting
{
    /// <summary>
    /// 磁盘空间管理页面视图模型
    /// </summary>
    public class DiskspaceManagePageViewModel : PageViewModelBase
    {
        private DiskSpaceManageController _controller = new DiskSpaceManageController();

        public RelayCommand SaveCommand { get; set; }

        public RelayCommand StartCleanCommand { get; set; }

        public RelayCommand StopCleanCommand { get; set; }

        public int EmergencyDiskSpaceRatio
        {
            get { return _controller.EmergencyFreeDiskSpaceRatio; }
            set
            {
                _controller.EmergencyFreeDiskSpaceRatio = _controller.StartCleanupFreeDiskSpaceRatioThreshold < value
                    ? _controller.StartCleanupFreeDiskSpaceRatioThreshold
                    : value;
            }
        }

        public int FreeDiskSpaceRatioThreshold
        {
            get { return _controller.StartCleanupFreeDiskSpaceRatioThreshold; }
            set
            {
                _controller.StartCleanupFreeDiskSpaceRatioThreshold = _controller.StopDiskSpaceCleanupRatio < value
                    ? _controller.StopDiskSpaceCleanupRatio
                    : value;
            }
        }

        public int CountOfImagesToDelete
        {
            get { return _controller.CountOfImagesToDelete; }
            set { _controller.CountOfImagesToDelete = value; }
        }

        public int StopDiskSpaceCleanupRatio
        {
            get { return _controller.StopDiskSpaceCleanupRatio; }
            set
            {
                _controller.StopDiskSpaceCleanupRatio = _controller.StartCleanupFreeDiskSpaceRatioThreshold > value
                    ? _controller.StartCleanupFreeDiskSpaceRatioThreshold
                    : value;
            }
        }

        public DateTime StartDiskSpaceCleanupTime
        {
            get { return _controller.StartDiskSpaceCleanupTime; }
            set
            {
                _controller.StartDiskSpaceCleanupTime = DateTimeHelper.TimeHourMinuteBigger(value,
                    _controller.StopDiskSpaceCleanupTime)
                    ? _controller.StopDiskSpaceCleanupTime
                    : value;
                //RaisePropertyChanged("StartDiskSpaceCleanupTime");
            }
        }

        public DateTime StopDiskSpaceCleanupTime
        {
            get { return _controller.StopDiskSpaceCleanupTime; }
            set
            {
                _controller.StopDiskSpaceCleanupTime = DateTimeHelper.TimeHourMinuteBigger(_controller.StartDiskSpaceCleanupTime,
                    value)
                    ? _controller.StartDiskSpaceCleanupTime
                    : value;
            }
        }

        private string _saveImageDisk = @"D:\";
        public string SaveImageDisk
        {
            get { return _saveImageDisk; }
            set { _saveImageDisk = value; RaisePropertyChanged();}
        }

        private double _saveImageDiskSpaceSize;
        public double SaveImageDiskSpaceSize
        {
            get { return _saveImageDiskSpaceSize; }
            set { _saveImageDiskSpaceSize = value; RaisePropertyChanged(); }
        }

        private double _saveImageDiskSpaceAlreadyUsedSize;
        public double SaveImageDiskSpaceAlreadyUsedSize
        {
            get { return _saveImageDiskSpaceAlreadyUsedSize; }
            set { _saveImageDiskSpaceAlreadyUsedSize = value; RaisePropertyChanged(); }
        }

        private Visibility _showStartCleanDiskButton = Visibility.Visible;
        public Visibility ShowStartCleanDiskButton
        {
            get { return _showStartCleanDiskButton;}
            set { _showStartCleanDiskButton = value;RaisePropertyChanged(); }
        }

        private Visibility _showStopCleanDiskButton = Visibility.Collapsed;
        public Visibility ShowStopCleanDiskButton
        {
            get { return _showStopCleanDiskButton; }
            set { _showStopCleanDiskButton = value; RaisePropertyChanged(); }
        }

        public DiskspaceManagePageViewModel()
        {
            SaveCommand = new RelayCommand(SaveCommandExecute);
            StartCleanCommand = new RelayCommand(StartCleanCommandExe);
            StopCleanCommand = new RelayCommand(StopCleanCommandExe);
            SaveImageDisk = ImageStoreDiskHelper.DiskName;
            SaveImageDiskSpaceSize = Math.Round(ImageStoreDiskHelper.TotalSizeGB,2);
            SaveImageDiskSpaceAlreadyUsedSize = Math.Round(ImageStoreDiskHelper.TotalUsedSpaceGB,2);

            OldImagesCleanupService.Service.AlreadyUsedDiskSpaceChangedWeakEvent += OnAlreadyUsedDiskSpaceChangedWeakEvent;
        }

        private void OnAlreadyUsedDiskSpaceChangedWeakEvent(object sender, double e)
        {
            var newUsedDiskSpace = Math.Round(e, 2);

            if (Math.Abs(SaveImageDiskSpaceAlreadyUsedSize - newUsedDiskSpace) < 1e-6)
            {
                //如果磁盘剩余空间不再变化，自动停止清理
                StopCleanCommandExe();
                return;
            }

            SaveImageDiskSpaceAlreadyUsedSize = newUsedDiskSpace;
        }

        private void StopCleanCommandExe()
        {
            ShowStopCleanDiskButton = Visibility.Collapsed;
            ShowStartCleanDiskButton = Visibility.Visible;
            OldImagesCleanupService.Service.ManualCleanupDisk = false;
            new OperationRecordService().AddRecord(new OperationRecord()
            {
                AccountId = LoginAccountManager.Service.CurrentAccount != null ? LoginAccountManager.Service.CurrentAccount.AccountId : "",
                OperateUI = OperationUI.DiskSpaceManage,
                OperateTime = DateTime.Now,
                OperateObject = "DiskCleanService",
                OperateCommand = OperationCommand.Close,
                OperateContent = string.Empty,
            });

        }

        private void StartCleanCommandExe()
        {
            ShowStartCleanDiskButton = Visibility.Collapsed;
            ShowStopCleanDiskButton = Visibility.Visible;
            OldImagesCleanupService.Service.ManualCleanupDisk = true;
            new OperationRecordService().AddRecord(new OperationRecord()
            {
                AccountId = LoginAccountManager.Service.CurrentAccount != null ? LoginAccountManager.Service.CurrentAccount.AccountId : "",
                OperateUI = OperationUI.DiskSpaceManage,
                OperateTime = DateTime.Now,
                OperateObject = "DiskCleanService",
                OperateCommand = OperationCommand.Open,
                OperateContent = string.Empty,
            });
        
        }

        /// <summary>
        /// 将最新的阈值，保存至存储中
        /// </summary>
        private void SaveCommandExecute()
        {
            _controller.SaveSettings();
            RaisePropertyChanged("EmergencyFreeDiskSpaceRatio");
            RaisePropertyChanged("FreeDiskSpaceRatioThreshold");
            RaisePropertyChanged("CountOfImagesToDelete");
            RaisePropertyChanged("StopDiskSpaceCleanupRatio");
            RaisePropertyChanged("StartDiskSpaceCleanupTime");
            RaisePropertyChanged("StopDiskSpaceCleanupTime");

            var sb = new StringBuilder();
            sb.Append("EmergencyFreeDiskSpaceRatio:").Append(EmergencyDiskSpaceRatio).Append(",");
            sb.Append("FreeDiskSpaceRatioThreshold:").Append(FreeDiskSpaceRatioThreshold).Append(",");
            sb.Append("CountOfImagesToDelete:").Append(CountOfImagesToDelete).Append(",");
            sb.Append("StopDiskSpaceCleanupRatio:").Append(StopDiskSpaceCleanupRatio).Append(",");
            sb.Append("StartDiskSpaceCleanupTime:").Append(StartDiskSpaceCleanupTime).Append(",");
            sb.Append("StopDiskSpaceCleanupTime:").Append(StopDiskSpaceCleanupTime);

            new OperationRecordService().AddRecord(new OperationRecord()
            {
                AccountId = LoginAccountManager.Service.CurrentAccount != null ? LoginAccountManager.Service.CurrentAccount.AccountId : "",
                OperateUI = OperationUI.DiskSpaceManage,
                OperateTime = DateTime.Now,
                OperateObject = "DiskCleanService",
                OperateCommand = OperationCommand.Setting,
                OperateContent = sb.ToString(),
            });
        }

        public override void OnKeyDown(System.Windows.Input.KeyEventArgs args)
        {
            
        }

        public override void Cleanup()
        {
            //停止可能的手动磁盘清理
            StopCleanCommandExe();
            base.Cleanup();
        }
    }
}
