using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace UI.XRay.LHDataCollector.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        public RelayCommand StartCapture { get; set; }
        public RelayCommand StopCapture { get; set; }

        public Visibility StartCaptureButtonVisibility = Visibility.Visible;
        public Visibility StopCaptureButtonVisibility = Visibility.Collapsed;

        public RelayCommand MoveLeft { get; set; }
        public RelayCommand Stop { get; set; }
        public RelayCommand MoveRight { get; set; }

        private int _viewIndex = 0;

        public int ViewIndex
        {
            get { return _viewIndex; }
            set
            {
                if (_viewIndex != value)
                {
                    _viewIndex = value;
                    RaisePropertyChanged("ViewIndex");
                }
            }
        }

        public RelayCommand ViewChanged { get; set; }


        private int _hlIndex = 0;

        public int HlIndex
        {
            get { return _hlIndex; }
            set
            {
                if (_hlIndex != value)
                {
                    _hlIndex = value;
                    RaisePropertyChanged("HlIndex");
                }
            }
        }

        public RelayCommand HlChanged { get; set; }

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            ////if (IsInDesignMode)
            ////{
            ////    // Code runs in Blend --> create design time data.
            ////}
            ////else
            ////{
            ////    // Code runs "for real"
            ////}
            
            StartCapture = new RelayCommand(StartCaptureExe);
            StopCapture = new RelayCommand(StopCaptureExe);
        }

        private void StopCaptureExe()
        {
            
        }

        private void StartCaptureExe()
        {
            
        }
    }
}