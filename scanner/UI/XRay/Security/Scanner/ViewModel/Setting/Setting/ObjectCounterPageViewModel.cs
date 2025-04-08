using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Services;
using UI.XRay.Flows.Controllers;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Setting
{
    public class ObjectCounterPageViewModel : ViewModelBase
    {
        private int _totalCount;

        /// <summary>
        /// 物体总计数
        /// </summary>
        public int TotalCount
        {
            get { return _totalCount; }
            set { _totalCount = value; RaisePropertyChanged();}
        }

        /// <summary>
        /// 当前会话期间的物体计数
        /// </summary>
        public int CurrentCount
        {
            get { return _currentCount; }
            set { _currentCount = value; RaisePropertyChanged(); }
        }

        private int _currentCount;

        private bool _isAutoResetChecked;
        public bool IsAutoResetChecked
        {
            get { return _isAutoResetChecked; }
            set { _isAutoResetChecked = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 用户登录后是否清零包裹计数器的开关事件命令
        /// </summary>
        public RelayCommand IsAutoResetToggleSwitchCheckedChangedEventCommand { get; private set; }

        /// <summary>
        /// 清除临时计数
        /// </summary>
        public RelayCommand ClearSessionCounterCommand { get; private set; }

        public ObjectCounterPageViewModel()
        {
            IsAutoResetToggleSwitchCheckedChangedEventCommand = new RelayCommand(IsCheckedChangedEventCommandExecute);
            ClearSessionCounterCommand = new RelayCommand(ClearSessionCounterCommandExecute);
            TotalCount = BagCounterService.Service.TotalCountSinceInstall;
            CurrentCount = BagCounterService.Service.SessionCount;
            IsAutoResetChecked = BagCounterService.Service.ResetCounterWhenLogin;
        }

        private void IsCheckedChangedEventCommandExecute()
        {
            BagCounterService.Service.ResetCounterWhenLogin = IsAutoResetChecked;
            new OperationRecordService().RecordOperation(OperationUI.ObjectCounter,"AutoResetWhenLogin", OperationCommand.Setting, (IsAutoResetChecked).ToString());
        }

        /// <summary>
        /// 命令：清除临时计数
        /// </summary>
        private void ClearSessionCounterCommandExecute()
        {

            BagCounterService.Service.ResetSessionCounter();
            CurrentCount = BagCounterService.Service.SessionCount;
            new OperationRecordService().RecordOperation(OperationUI.ObjectCounter,"ResetSessionCount", OperationCommand.Setting, string.Empty);
        }
    }
}
