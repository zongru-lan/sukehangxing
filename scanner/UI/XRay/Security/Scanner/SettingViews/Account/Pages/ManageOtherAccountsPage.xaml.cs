using System;
using System.Collections.Generic;
using System.IO;
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
using GalaSoft.MvvmLight.Messaging;
using UI.XRay.Common.Utilities;
using UI.XRay.Flows.Services;
using UI.XRay.Flows.Services.Db;
using UI.XRay.Flows.TRSNetwork;
using UI.XRay.Gui.Framework;
using UI.XRay.Security.Scanner.ViewModel.Setting.Account;

namespace UI.XRay.Security.Scanner.SettingViews.Account.Pages
{
    /// <summary>
    /// ManageOtherAccountsPage.xaml 的交互逻辑
    /// </summary>
    public partial class ManageOtherAccountsPage : Page
    {
        public ManageOtherAccountsPage()
        {
            InitializeComponent();
            Messenger.Default.Register<CommitChangesMessage>(this, "AccountEdit", CommitChangesMessageAction);
        }

        /// <summary>
        /// 当用户通过快捷键提交或撤销更改时，在执行之前，先通知Datagrid提交当前行的变更。
        /// </summary>
        /// <param name="msg"></param>
        private void CommitChangesMessageAction(CommitChangesMessage msg)
        {
            AccountsDataGrid.CommitEdit(DataGridEditingUnit.Row, true);
        }

        /// <summary>
        /// 用户点击了重置密码按钮，将会重置当前选中行的密码
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PasswordColumnEditButton_Click(object sender, RoutedEventArgs e)
        {
            // 用户点击重置密码后，结束对当前密码cell的编辑
            AccountsDataGrid.CommitEdit(DataGridEditingUnit.Row, true);
        }

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

    
    }
}
