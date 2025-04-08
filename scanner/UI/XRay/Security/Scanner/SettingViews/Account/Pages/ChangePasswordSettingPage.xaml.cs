using System.Windows.Controls;

namespace UI.XRay.Security.Scanner.SettingViews.Account.Pages
{
    /// <summary>
    /// ChangePasswordPage.xaml 的交互逻辑
    /// </summary>
    public partial class ChangePasswordSettingPage : Page
    {
        public ChangePasswordSettingPage()
        {
            InitializeComponent();
            NewPasswordBox.Focus();
        }
    }
}
