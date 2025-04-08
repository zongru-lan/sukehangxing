using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using UI.XRay.Gui.Framework;
using System.Drawing;
using System.Windows;
using UI.XRay.Flows.Controllers;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Training
{
    public class TrainingMenuViewModel : ViewModelBase
    {
        private IFramePageNavigationService _navigationService;

        public RelayCommand NaviToSettingCommand { get; set; }

        public RelayCommand NaviToImagesCommand { get; set; }

        private Visibility _trainingImageManage = Visibility.Collapsed;

        public Visibility TrainingImageManageVisibility
        {
            get { return _trainingImageManage; }
            set { _trainingImageManage = value; RaisePropertyChanged(); }
        }
        

        public TrainingMenuViewModel(IFramePageNavigationService service)
        {
            _navigationService = service;

            NaviToSettingCommand = new RelayCommand(() => _navigationService.ShowPage("TrainingSettingPage"));
            NaviToImagesCommand = new RelayCommand(() => _navigationService.ShowPage("TrainingImagesPage"));

            if (LoginAccountManager.Service.HasLogin)
            {
                if (LoginAccountManager.Service.IsCurrentRoleEqualOrGreaterThan(Business.Entities.AccountRole.Admin))
	            {
                    TrainingImageManageVisibility = Visibility.Visible;
	            }
            }
        }
    }
}
