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

namespace UI.XRay.Security.Scanner.SettingViews.Account.Pages
{
    /// <summary>
    /// ManageGroupsPage.xaml 的交互逻辑
    /// </summary>
    public partial class ManageGroupsPage : Page
    {
        public ManageGroupsPage()
        {
            InitializeComponent();
        }

        private void GroupsDataGrid_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                var grid = sender as DataGrid;
                if (grid != null)
                {
                    grid.BeginEdit();
                    e.Handled = true;
                }
            }
        }
    }
}
