using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Tip
{
    public class TipMenuViewModel : ViewModelBase
    {
        private IFramePageNavigationService _navigationService;

        public RelayCommand NaviToTipPlansPageCommand { get; set; }
        public RelayCommand NaviToTipImagesPageCommand { get; set; }
        public RelayCommand NaviToTipExamRecordsPageCommand{ get; set; }

        public TipMenuViewModel(IFramePageNavigationService service)
        {
            _navigationService = service;

            NaviToTipPlansPageCommand = new RelayCommand(() => _navigationService.ShowPage("TipPlansPage"));
            NaviToTipImagesPageCommand = new RelayCommand(() => _navigationService.ShowPage("TipImagesPage"));
            NaviToTipExamRecordsPageCommand = new RelayCommand(() => _navigationService.ShowPage("TipExamLogPage"));
        }
    }
}
