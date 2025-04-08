/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocator xmlns:vm="clr-namespace:UI.XRay.Scanner"
                           x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"

  You can also use Blend to do all this with the tool's support.
  See http://www.galasoft.ch/mvvm
*/

using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using UI.XRay.Business.Entities;
using UI.XRay.Gui.Framework;
using UI.XRay.Security.Scanner.ViewModel;
using UI.XRay.Security.Scanner.ViewModel.Setting.About;
using UI.XRay.Security.Scanner.ViewModel.Setting.Account;
using UI.XRay.Security.Scanner.ViewModel.Setting.Device;
using UI.XRay.Security.Scanner.ViewModel.Setting.Image;
using UI.XRay.Security.Scanner.ViewModel.Setting.Menu;
using UI.XRay.Security.Scanner.ViewModel.Setting.Records;
using UI.XRay.Security.Scanner.ViewModel.Setting.Setting;
using UI.XRay.Security.Scanner.ViewModel.Setting.Tip;
using UI.XRay.Security.Scanner.ViewModel.Setting.Training;

namespace UI.XRay.Security.Scanner
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

            //if (ViewModelBase.IsInDesignModeStatic)
            //{
            //    // Create design time view services and models
            //    SimpleIoc.Default.Register<IDataService, DesignDataService>();
            //}
            //else
            //{
            //    // Create run time view services and models
            //    SimpleIoc.Default.Register<IDataService, DataService>();
            //}

            Cache = new Hashtable();

            SetupNavigation();

            SimpleIoc.Default.Register<MainViewModel>();
            //SimpleIoc.Default.Register<SettingMenuPageViewModel>();
            //SimpleIoc.Default.Register<SettingWindowViewModel>();
        }

        /// <summary>
        /// ���ڻ�����ViewModel֮�����н����Ķ���
        /// </summary>
        public Hashtable Cache { get; private set; }

        public static ViewModelLocator Locator
        {
            get { return Application.Current.FindResource("Locator") as ViewModelLocator; }
        }

        public static IFramePageNavigationService SettingPageNavigationService
        {
            get { return ServiceLocator.Current.GetInstance<IFramePageNavigationService>("SettingPageNavigationService"); }
        }

        private void SetupNavigation()
        {
            var navigationService = new FramePageNavigationService();
            navigationService.Configure("AccountMenu", new Uri("pack://application:,,,/SettingViews/Account/AccountMenuPage.xaml", UriKind.Absolute));
            navigationService.Configure("ChangePasswordEmptyMenuPage", new Uri("pack://application:,,,/SettingViews/Account/ChangePasswordEmptyMenuPage.xaml", UriKind.Absolute));
            navigationService.Configure("ManageOtherAccountsPage", new Uri("pack://application:,,,/SettingViews/Account/Pages/ManageOtherAccountsPage.xaml", UriKind.Absolute));
            navigationService.Configure("ManageGroupsPage", new Uri("pack://application:,,,/SettingViews/Account/Pages/ManageGroupsPage.xaml", UriKind.Absolute));
            navigationService.Configure("ChangePasswordPage", new Uri("pack://application:,,,/SettingViews/Account/Pages/ChangePasswordSettingPage.xaml", UriKind.Absolute));
            navigationService.Configure("AccountPage", new Uri("pack://application:,,,/SettingViews/Account/Pages/AccountPage.xaml", UriKind.Absolute));
            navigationService.Configure("AutoLoginSettingPage", new Uri("pack://application:,,,/SettingViews/Account/Pages/AutoLoginSettingPage.xaml", UriKind.Absolute));

            navigationService.Configure("ImageMenu", new Uri("pack://application:,,,/SettingViews/Image/ImageMenu.xaml", UriKind.Absolute));
            navigationService.Configure("ImsPage", new Uri("pack://application:,,,/SettingViews/Image/Pages/ImsPage.xaml", UriKind.Absolute));
            navigationService.Configure("ImsListPage", new Uri("pack://application:,,,/SettingViews/Image/Pages/ImsListPage.xaml", UriKind.Absolute));
            navigationService.Configure("ImageSettingPage", new Uri("pack://application:,,,/SettingViews/Image/Pages/ImageSettingPage.xaml", UriKind.Absolute));

            navigationService.Configure("SystemMenu", new Uri("pack://application:,,,/SettingViews/Menu/SystemMenu.xaml", UriKind.Absolute));
            navigationService.Configure("MenuPage", new Uri("pack://application:,,,/SettingViews/Menu/Pages/MenuPage.xaml", UriKind.Absolute));

            navigationService.Configure("DeviceMenu", new Uri("pack://application:,,,/SettingViews/Device/DeviceMenu.xaml", UriKind.Absolute));
            navigationService.Configure("DetectorsPage", new Uri("pack://application:,,,/SettingViews/Device/Pages/DetectorsPage.xaml", UriKind.Absolute));
            navigationService.Configure("XRayGenPage", new Uri("pack://application:,,,/SettingViews/Device/Pages/XRayGenPage.xaml", UriKind.Absolute));
            navigationService.Configure("ConveyorPESensorPage", new Uri("pack://application:,,,/SettingViews/Device/Pages/ConveyorPESensorPage.xaml", UriKind.Absolute));
            navigationService.Configure("KeyboardPage", new Uri("pack://application:,,,/SettingViews/Device/Pages/KeyboardPage.xaml", UriKind.Absolute));
            navigationService.Configure("ControlSystemPage", new Uri("pack://application:,,,/SettingViews/Device/Pages/ControlSystemPage.xaml", UriKind.Absolute));
            navigationService.Configure("DiagnosisPage", new Uri("pack://application:,,,/SettingViews/Device/Pages/DiagnosisPage.xaml", UriKind.Absolute));
            navigationService.Configure("DeviceMaintenancePage", new Uri("pack://application:,,,/SettingViews/Device/Pages/DeviceMaintenancePage.xaml", UriKind.Absolute));

            navigationService.Configure("SettingMenu", new Uri("pack://application:,,,/SettingViews/Setting/SettingMenu.xaml", UriKind.Absolute));
            navigationService.Configure("ObjectCounterPage", new Uri("pack://application:,,,/SettingViews/Setting/Pages/ObjectCounterPage.xaml", UriKind.Absolute));
            navigationService.Configure("IntelliSensePage", new Uri("pack://application:,,,/SettingViews/Setting/Pages/IntelliSensePage.xaml", UriKind.Absolute));
            navigationService.Configure("FunctionKeysPage", new Uri("pack://application:,,,/SettingViews/Setting/Pages/FunctionKeysPage.xaml", UriKind.Absolute));
            navigationService.Configure("DiskSpaceManagePage", new Uri("pack://application:,,,/SettingViews/Setting/Pages/DiskSpaceManagePage.xaml", UriKind.Absolute));

            navigationService.Configure("AboutMenu", new Uri("pack://application:,,,/SettingViews/About/AboutMenu.xaml", UriKind.Absolute));
            navigationService.Configure("AboutPage", new Uri("pack://application:,,,/SettingViews/About/Pages/AboutPage.xaml", UriKind.Absolute));

            navigationService.Configure("RecordsMenu", new Uri("pack://application:,,,/SettingViews/Records/RecordsMenu.xaml", UriKind.Absolute));
            navigationService.Configure("BootLogPage", new Uri("pack://application:,,,/SettingViews/Records/Pages/BootLogPage.xaml", UriKind.Absolute));
            navigationService.Configure("LoginLogPage", new Uri("pack://application:,,,/SettingViews/Records/Pages/LoginLogPage.xaml", UriKind.Absolute));
            navigationService.Configure("XRayGenWorkLogPage", new Uri("pack://application:,,,/SettingViews/Records/Pages/XRayGenWorkLogPage.xaml", UriKind.Absolute));
            navigationService.Configure("TipExamLogPage", new Uri("pack://application:,,,/SettingViews/Records/Pages/TipExamLogPage.xaml", UriKind.Absolute));
            navigationService.Configure("OperationLogPage", new Uri("pack://application:,,,/SettingViews/Records/Pages/OperationLogPage.xaml", UriKind.Absolute));
            navigationService.Configure("ConveyorWorkLogPage", new Uri("pack://application:,,,/SettingViews/Records/Pages/ConveyorWorkLogPage.xaml", UriKind.Absolute));

            navigationService.Configure("TipMenu", new Uri("pack://application:,,,/SettingViews/Tip/TipMenu.xaml", UriKind.Absolute));
            navigationService.Configure("TipImagesPage", new Uri("pack://application:,,,/SettingViews/Tip/Pages/TipImagesPage.xaml", UriKind.Absolute));
            navigationService.Configure("TipPlansPage", new Uri("pack://application:,,,/SettingViews/Tip/Pages/TipPlansPage.xaml", UriKind.Absolute));

            navigationService.Configure("TrainingMenu", new Uri("pack://application:,,,/SettingViews/Training/TrainingMenu.xaml", UriKind.Absolute));
            navigationService.Configure("TrainingImagesPage", new Uri("pack://application:,,,/SettingViews/Training/Pages/TrainingImagesManagementPage.xaml", UriKind.Absolute));
            navigationService.Configure("TrainingSettingPage", new Uri("pack://application:,,,/SettingViews/Training/Pages/TrainingSettingPage.xaml", UriKind.Absolute));

            navigationService.Configure("ImageBadChannelManual", new Uri("pack://application:,,,/MetroDialogs/ImageBadChannelManual.xaml", UriKind.Absolute));


            navigationService.Configure("RenderEngine22", new Uri("pack://application:,,,/MetroDialogs/RenderEngine22.xaml", UriKind.Absolute));
            SimpleIoc.Default.Register<IFramePageNavigationService>(() => navigationService, "SettingPageNavigationService");
        }

        public MainViewModel Main
        {
            get
            {
                return ServiceLocator.Current.GetInstance<MainViewModel>();
            }
        }

        public ScreenImagesOperationWindowViewModel ScreenImagesOperation
        {
            get
            {
                // ��cache��ȡ����ǰ��Ļͼ���б�
                var list = Cache["ScreenImagesRecordsList"] as IEnumerable<ImageRecord>;
                return new ScreenImagesOperationWindowViewModel(list);
            }
        }

        public SystemBarViewModel SystemBar
        {
            get
            {
                return new SystemBarViewModel();
            }
        }

        public SystemBarTouchViewModel SystemBarTouch
        {
            get
            {
                return new SystemBarTouchViewModel();
            }
        }

        public SystemMenuPageViewModel MainMenuPage
        {
            get
            {
                return new SystemMenuPageViewModel(SettingPageNavigationService);
            }
        }

        public SettingWindowViewModel SettingWindow
        {
            get
            {
                return new SettingWindowViewModel(SettingPageNavigationService);
            }
        }

        public XRayGenWarmupWindowViewModel XRayGenWarmupWindow
        {
            get
            {
                return new XRayGenWarmupWindowViewModel();
            }
        }

        public CleanTunnelWindowViewModel CleanTunnelWindow
        {
            get
            {
                return new CleanTunnelWindowViewModel();
            }
        }

        public DiagnosisWarningWindowViewModel DiagnosisWarningWindow
        {
            get
            {
                return new DiagnosisWarningWindowViewModel();
            }
        }

        public CalibrateWindowViewModel CalibrateWindow
        {
            get
            {
                return new CalibrateWindowViewModel();
            }
        }

        public RegularRemindViewModel RegularRemindWindow
        {
            get
            {
                return new RegularRemindViewModel();
            }
        }

        private RenderEngine22 _renderEngine22Window = null;
        public RenderEngine22 RenderEngine22Window
        {
            get
            {
                if (_renderEngine22Window != null)
                {
                    _renderEngine22Window.Close();
                }
                _renderEngine22Window = new RenderEngine22();
                return _renderEngine22Window;

            }
        }

       
        public RemoteDiagnose R1
        {
            get
            {
                return new RemoteDiagnose();
            }
        }

        public LoginViewModel Login
        {
            get
            {
                return new LoginViewModel();
            }
        }

        public AccountMenuPageViewModel AccountMenuPage
        {
            get
            {
                return new AccountMenuPageViewModel(SettingPageNavigationService);
            }
        }

        public AddAccountViewModel AddAccount
        {
            get
            {
                return new AddAccountViewModel();
            }
        }

        public AddGroupViewModel AddGroup
        {
            get
            {
                return new AddGroupViewModel();
            }
        }

        public TipLibSelectViewModel TipLibSelect
        {
            get
            {
                return new TipLibSelectViewModel();
            }
        }

        public ImageRetrievalViewModel ImageRetrieval
        {
            get
            {
                return new ImageRetrievalViewModel();
            }
        }

        public ObjectCounterPageViewModel ObjectCounterPage
        {
            get
            {
                return new ObjectCounterPageViewModel();
            }
        }

        public IntelliSensePageViewModel IntelliSensePage
        {
            get
            {
                return new IntelliSensePageViewModel();
            }
        }

        public TipMenuViewModel TipMenu
        {
            get
            {
                return new TipMenuViewModel(SettingPageNavigationService);
            }
        }

        public TipPlansPageViewModel TipPlansPage
        {
            get
            {
                return new TipPlansPageViewModel();
            }
        }

        public ImsPageViewModel ImsPage
        {
            get
            {
                return new ImsPageViewModel();
            }
        }

        public ImsListPageViewModel ImsListPage
        {
            get
            {
                return new ImsListPageViewModel();
            }
        }

        public TipImagesPageViewModel TipImagesPage
        {
            get
            {
                return new TipImagesPageViewModel();
            }
        }

        public ImageSettingPageViewModel ImageSettingPage
        {
            get
            {
                return new ImageSettingPageViewModel();
            }
        }

        public TrainingMenuViewModel TrainingMenu
        {
            get
            {
                return new TrainingMenuViewModel(SettingPageNavigationService);
            }
        }

        public TrainingSettingPageViewModel TrainingSettingPage
        {
            get
            {
                return new TrainingSettingPageViewModel();
            }
        }

        public TrainingImagesManagementPageViewModel TrainingImagesesManagementPage
        {
            get
            {
                return new TrainingImagesManagementPageViewModel();
            }
        }

        public AutoLoginPageViewModel AutoLoginPage
        {
            get
            {
                return new AutoLoginPageViewModel();
            }
        }

        public SettingMenuPageViewModel SettingMenu
        {
            get
            {
                return new SettingMenuPageViewModel(SettingPageNavigationService);
            }
        }

        public ImageMenuPageViewModel ImageMenu
        {
            get
            {
                return new ImageMenuPageViewModel(SettingPageNavigationService);
            }
        }

        public DeviceMenuViewModel DeviceMenu
        {
            get
            {
                return new DeviceMenuViewModel(SettingPageNavigationService);
            }
        }

        public RecordsMenuViewModel RecordsMenu
        {
            get
            {
                return new RecordsMenuViewModel(SettingPageNavigationService);
            }
        }

        public TipExamLogPageViewModel TipExamLogPage
        {
            get { return new TipExamLogPageViewModel(); }
        }

        public OperationLogPageViewModel OperationLogPage
        {
            get { return new OperationLogPageViewModel(); }
        }

        public DetectorsPageViewModel DetectorsPage
        {
            get
            {
                return new DetectorsPageViewModel();
            }
        }

        public ControlSystemPageViewModel ControlSystemPage
        {
            get
            {
                return new ControlSystemPageViewModel();
            }
        }

        public DiagnosisPageViewModel DiagnosisPage
        {
            get
            {
                return new DiagnosisPageViewModel();
            }
        }

        public DeviceMaintenancePageViewModel DeviceMaintenancePage
        {
            get
            {
                return new DeviceMaintenancePageViewModel();
            }
        }

        public KeyboardPageViewModel KeyboardPage
        {
            get
            {
                return new KeyboardPageViewModel();
            }
        }

        public FunctionKeysPageViewModel FunctionKeysPage
        {
            get
            {
                return new FunctionKeysPageViewModel();
            }
        }

        public ConveyorPESensorPageViewModel ConveyorPeSensorPage
        {
            get { return new ConveyorPESensorPageViewModel(); }
        }

        public XRayGenPageViewModel XRayGenPage
        {
            get { return new XRayGenPageViewModel(); }
        }

        public XRayGenWorkLogPageViewModel XRayGenWorkLogPage
        {
            get { return new XRayGenWorkLogPageViewModel(); }
        }

        public ConveyorWorkLogPageViewModel ConveyorWorkLogPage
        {
            get { return new ConveyorWorkLogPageViewModel(); }
        }


        public LoginLogsPageViewModel LoginLogsPage
        {
            get { return new LoginLogsPageViewModel(); }
        }

        public BootLogsPageViewModel BootLogsPage
        {
            get { return new BootLogsPageViewModel(); }
        }

        public AccountPageViewModel AccountPage
        {
            get { return new AccountPageViewModel(SettingPageNavigationService); }
        }

        public ChangePasswordPageViewModel ChangePasswordPage
        {
            get { return new ChangePasswordPageViewModel(SettingPageNavigationService); }
        }

        public ManageOtherAccountsViewModel ManageOtherAccounts
        {
            get
            {
                { return new ManageOtherAccountsViewModel(); }
            }
        }

        public ManageGroupsPageViewModel ManageGroupsPage
        {
            get { return new ManageGroupsPageViewModel(); }
        }

        public DiskspaceManagePageViewModel DiskspaceManagePage
        {
            get
            {
                return new DiskspaceManagePageViewModel();
            }
        }

        public AboutPageViewModel AboutPage
        {
            get
            {
                return new AboutPageViewModel();
            }
        }

        public ChangeSysDateTimeWindowViewModel ChangeSysDateTimeWindowViewModel
        {
            get
            {
                return new ChangeSysDateTimeWindowViewModel();
            }
        }

        //public ImageBadChannelManualViewModel ImageBadManualViewModel
        //{
        //    get
        //    {
        //        return new ImageBadChannelManualViewModel();
        //    }
        //}

       
        public static void Cleanup()
        {
            // TODO Clear the ViewModels
        }
    }
}