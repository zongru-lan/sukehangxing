using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Services.Db;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Account
{
    public class AutoLoginPageViewModel : PageViewModelBase
    {
        public RelayCommand SelectionChangedEventCommand { get; set; }

        public RelayCommand IsAutoLoginCheckedChangedEventCommand { get; set; }

        private ObservableCollection<Business.Entities.Account> _accounts;

        public ObservableCollection<Business.Entities.Account> Accounts
        {
            get { return _accounts; }
            set { _accounts = value;RaisePropertyChanged(); }
        }

        private string _selectedAccountId;

        public string SelectedAccountId
        {
            get { return _selectedAccountId; }
            set { _selectedAccountId = value; RaisePropertyChanged(); }
        }

        private bool _isAutoLoginChecked;

        public bool IsAutoLoginChecked
        {
            get { return _isAutoLoginChecked; }
            set
            {
                _isAutoLoginChecked = value;
                RaisePropertyChanged();
            }
        }

        private AccountDbSet _manager;

        public AutoLoginPageViewModel()
        {
            try
            {
                SelectionChangedEventCommand = new RelayCommand(SelectionChangedEventCommandExecute);
                IsAutoLoginCheckedChangedEventCommand = new RelayCommand(IsAutoLoginCheckedChangedEventCommandExecute);

                _manager = new AccountDbSet();
                var list = _manager.SelectOperators();
                Accounts = new ObservableCollection<Business.Entities.Account>(list);

                MakeAllUserCanAutoLoginInDebugMode();

                IsAutoLoginChecked = AutoLoginController.IsAutoLoginEnabled();
                SelectedAccountId = AutoLoginController.GetAccountId();
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception, "Unexpected exception in AutoLoginPageViewModel Constructor");
            }
        }

        /// <summary>
        /// 在debug调试模式下，允许使用所有用户进行自动登录，方便调试
        /// </summary>
        [Conditional("DEBUG")]
        private void MakeAllUserCanAutoLoginInDebugMode()
        {
            _manager = new AccountDbSet();
            var list = _manager.SelectAll();
            Accounts = new ObservableCollection<Business.Entities.Account>(list);
        }

        private void IsAutoLoginCheckedChangedEventCommandExecute()
        {
            AutoLoginController.EnableAutoLogin(IsAutoLoginChecked);
        }

        private void SelectionChangedEventCommandExecute()
        {
            AutoLoginController.ChangeAccountId(SelectedAccountId);
        }

        public override void OnKeyDown(System.Windows.Input.KeyEventArgs args)
        {
        }
    }
}
