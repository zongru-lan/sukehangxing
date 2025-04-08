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
using UI.XRay.Security.Scanner.ViewModel.Setting.Account;

namespace UI.XRay.Security.Scanner.SettingViews.Account.Pages
{
    /// <summary>
    /// AccountInformation.xaml 的交互逻辑
    /// </summary>
    public partial class AccountPage 
    {
        public AccountPage()
        {
            InitializeComponent();
        }

        ///// <summary>
        ///// 用户点击了重置密码按钮，将会重置当前选中行的密码
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private void PasswordColumnEditButton_Click(object sender, RoutedEventArgs e)
        //{
        //    var account = ((FrameworkElement)sender).DataContext as Business.Entities.Account;

        //    if (account != null)
        //    {
        //        // 将密码重置为默认密码，同时
        //        account.Password = Business.Entities.Account.DefaultPassword;
        //        account.DisplayPassword = account.Password;
        //    }

        //    // 用户点击重置密码后，结束对当前密码cell的编辑
        //    //AccountsDataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        //}

        /// <summary>
        /// 用户在DataGrid获取焦点时，按下空格键，切换进入编辑模式。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AccountsDataGrid_OnKeyDown(object sender, KeyEventArgs e)
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

        private void AccountPage_OnKeyDown(object sender, KeyEventArgs e)
        {
        }
    }
}
