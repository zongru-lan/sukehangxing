using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UI.XRay.Security.Scanner
{
    /// <summary>
    /// SystemBarControlTouch.xaml 的交互逻辑
    /// </summary>
    public partial class SystemBarControlTouch : System.Windows.Controls.UserControl
    {
        public SystemBarControlTouch()
        {
            InitializeComponent();
            ContentGrid.Width = MainScreenWidth;

            Loaded += SystemBarControl_Loaded;
        }
        private int MainScreenWidth
        {
            get { return Screen.PrimaryScreen.Bounds.Width; }
        }


        void SystemBarControl_Loaded(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).Closing += OnClosing;
        }

        private void OnClosing(object sender, CancelEventArgs cancelEventArgs)
        {
            var vm = DataContext as ViewModel.SystemBarTouchViewModel;
            if (vm == null) return;
            vm.emergencyWindow.Close();
        }
    }
}
