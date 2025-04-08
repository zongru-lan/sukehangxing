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

namespace UI.XRay.Security.Scanner.SettingViews.Tip.Pages
{
    /// <summary>
    /// TipPlansPage.xaml 的交互逻辑
    /// </summary>
    public partial class TipPlansPage : Page
    {
        public TipPlansPage()
        {
            InitializeComponent();
        }

        private void OnPlanChecked(object sender, RoutedEventArgs e)
        {
            CheckBox selectCheckBox = (CheckBox)sender;
            for (int i = 0; i < gdPlan.Items.Count; i++)
            {
                //获取行
                DataGridRow selectedRow = (DataGridRow)gdPlan.ItemContainerGenerator.ContainerFromIndex(i);
                //获取改行的是否启用所在的列
                var cb = gdPlan.Columns[3].GetCellContent(selectedRow);
                //获取到需要的列之后再取获取需要的控件
                GetVisualChild(gdPlan, selectCheckBox);
            }
        }

        //获取并选中
        public void GetVisualChild(DependencyObject parent, CheckBox ckBox)
        {
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                DependencyObject v = (DependencyObject)VisualTreeHelper.GetChild(parent, i);
                CheckBox child = v as CheckBox;

                if (child == null)
                {
                    GetVisualChild(v, ckBox);
                }
                else
                {
                    if (child != ckBox)
                    {
                        child.IsChecked = false;
                    }
                    return;
                }
            }
        }
    }
}
