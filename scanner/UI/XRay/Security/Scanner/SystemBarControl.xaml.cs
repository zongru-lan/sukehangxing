using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UserControl = System.Windows.Controls.UserControl;

namespace UI.XRay.Security.Scanner
{
    /// <summary>
    /// SystemBarControl.xaml 的交互逻辑
    /// </summary>
    public partial class SystemBarControl : UserControl
    {
        private double MainScreenWidth
        {
            //get { return SystemParameters.VirtualScreenWidth; }
            get { return Screen.PrimaryScreen.Bounds.Width; }
        }

        public SystemBarControl()
        {
            InitializeComponent();
            ContentGrid.Width = MainScreenWidth;
            //ContentGrid.Width = MainScreenWidth;

            Loaded += SystemBarControl_Loaded;
        }

        void SystemBarControl_Loaded(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).Closing += OnClosing;
        }

        private void OnClosing(object sender, CancelEventArgs cancelEventArgs)
        {
            var vm = DataContext as ViewModel.SystemBarViewModel;
            if (vm != null)
                vm.Cleanup();
        }

    }
}
