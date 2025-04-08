using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UI.XRay.Gui.Widgets;

namespace UI.XRay.Security.Scanner.SettingViews.Device.Pages
{
    /// <summary>
    /// DetectorsPage.xaml 的交互逻辑
    /// </summary>
    public partial class DetectorsPage : Page
    {
        private CurveControl _curveControl;
        public DetectorsPage()
        {
            InitializeComponent();
            _curveControl = new CurveControl();
        }
    }
}
