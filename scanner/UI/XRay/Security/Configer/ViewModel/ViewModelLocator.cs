/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocator xmlns:vm="clr-namespace:UI.XRay.Gui.Configer"
                           x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"

  You can also use Blend to do all this with the tool's support.
  See http://www.galasoft.ch/mvvm
*/

using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using UI.XRay.Gui.Configer.ViewModel;

namespace UI.XRay.Security.Configer.ViewModel
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    public class ViewModelLocator
    {
        /// <summary>
        /// Initializes a new instance of the ViewModelLocator class.
        /// </summary>
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            ////if (ViewModelBase.IsInDesignModeStatic)
            ////{
            ////    // Create design time view services and models
            ////    SimpleIoc.Default.Register<IDataService, DesignDataService>();
            ////}
            ////else
            ////{
            ////    // Create run time view services and models
            ////    SimpleIoc.Default.Register<IDataService, DataService>();
            ////}

            SimpleIoc.Default.Register<MainViewModel>();
        }

        public MainViewModel Main
        {
            get
            {
                return ServiceLocator.Current.GetInstance<MainViewModel>();
            }
        }

        public SystemSettingsViewModel SystemSettings
        {
            get
            {
                return new SystemSettingsViewModel();
            }
        }

        public CaptureSystemSettingsViewModel CaptureSystemSettings
        {
            get
            {
                return new CaptureSystemSettingsViewModel();
            }
        }

        public ControlSystemSettingsViewModel ControlSystemSettings
        {
            get
            {
                return new ControlSystemSettingsViewModel();
            }
        }

        public KeyboardSettingsViewModel KeyboardSettings
        {
            get
            {
                return new KeyboardSettingsViewModel();
            }
        }

        public PreProcSettingsViewModel PreProcSettings
        {
            get { return new PreProcSettingsViewModel(); }
        }

        public ImagesSettingsViewModel ImagesSettings
        {
            get { return new ImagesSettingsViewModel();}
        }

        public MachineSettingsViewModel MachineSettings
        {
            get { return new MachineSettingsViewModel();}
        }

        public XRayGenSettingsViewModel XRayGenSettings
        {
            get
            {
                return new XRayGenSettingsViewModel();
            }
        }

        public ChangeModelWindowViewModel ChangeModel
        {
            get
            {
                return new ChangeModelWindowViewModel();
            }
        }

        public NetworkSettingsViewModel NetworkSettings
        {
            get
            {
                return new NetworkSettingsViewModel();
            }
        }

        public static void Cleanup()
        {
            // TODO Clear the ViewModels
        }
    }
}