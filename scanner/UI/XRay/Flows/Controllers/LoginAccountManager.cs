using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using UI.Common.Tracers;
using UI.XRay.Business.Entities;
using UI.XRay.Common.Utilities;
using UI.XRay.Flows.Services;
using UI.XRay.Flows.Services.Db;
using UI.XRay.Flows.TRSNetwork;

namespace UI.XRay.Flows.Controllers
{
    //public class LoginEventArgs : EventArgs

    /// <summary>
    /// 管理当前已经登录的用户
    /// </summary>
    public class LoginAccountManager
    {
        public static LoginAccountManager Service { get; private set; }

        static LoginAccountManager()
        {
            Service = new LoginAccountManager();
        }

        public  Account CurrentAccount { get; private set; }

        public  PermissionList CurrentAccountPermission { get; private set; }

        public DateTime LoginTime { get; private set; }

        /// <summary>
        /// 当前用户标记总次数
        /// </summary>
        public int TotalMarkCount { get; private set; }

        /// <summary>
        /// Tip注入总次数
        /// </summary>
        public int TipInjectionCount { get; private set; }

        /// <summary>
        /// 错过Tip总次数
        /// </summary>
        public int MissTipCount { get; private set; }

        /// <summary>
        /// 当前账户的Id。如果尚未登录，则返回为null
        /// </summary>
        public string AccountId
        {
            get { return HasLogin ? CurrentAccount.AccountId : null; }
        }

        /// <summary>
        /// 用户是否已经登录
        /// </summary>
        /// <returns></returns>
        public bool HasLogin
        {
            get { return CurrentAccount != null; }
        }

        private bool _loginMode;
        public bool LoginModeState
        {
            get { return _loginMode; }
            set { _loginMode = value; }
        }
        public List<string> AllAccountId { get; private set; }

        public List<Account> AllAccount { get; private set; }
        #region events

        /// <summary>
        /// 登录弱事件
        /// </summary>
        private static SmartWeakEvent<EventHandler<Account>> _loginEvent = new SmartWeakEvent<EventHandler<Account>>();

        /// <summary>
        /// 弱事件：用户登录成功
        /// </summary>
        public event EventHandler<Account> AccountLogin
        {
            add
            {
                _loginEvent.Add(value); 
            }
            remove { _loginEvent.Remove(value); }
        }

        /// <summary>
        /// 注销弱事件
        /// </summary>
        private static SmartWeakEvent<EventHandler<Account>> _logoutEvent = new SmartWeakEvent<EventHandler<Account>>();

        /// <summary>
        /// 弱事件：用户登录成功
        /// </summary>
        public event EventHandler<Account> AccountLogout
        {
            add
            {
                _logoutEvent.Add(value);
            }
            remove { _logoutEvent.Remove(value); }
        }

        #endregion

        /// <summary>
        /// 登录用户
        /// </summary>
        /// <param name="account"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Login(Account account, bool isNetLogin = false)
        {
            CurrentAccount = account;
            CurrentAccountPermission = PermissionService.Service.GetPermissionList(account);
            LoginTime = DateTime.Now;
            SystemStateService.Service.UserID = account.AccountId;
            try
            {
                _loginEvent.Raise(null, account);
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            var sessionSet = new WorkSessionDbSet();
            var session = new WorkSession(CurrentAccount.AccountId, LoginTime, LoginTime, BagCounterService.Service.SessionCount);

            session.TotalMarkCount = TotalMarkCount;
            session.TipInjectionCount = TipInjectionCount;
            session.MissTipCount = MissTipCount;

            sessionSet.Add(session);

            if (BagCounterService.Service.ResetCounterWhenLogin)
            {
                BagCounterService.Service.ResetSessionCounter();
            }

            AllAccountId = GetCurrentAndLessThanId();
            AllAccount = GetCurrentAndLessThanAccount();

            PermissionService.Service.CurrentAccount = account;
            SystemStatus.Instance.HasLogin = true;
            SystemStatus.Instance.CurrentUser = account;

            new OperationRecordService().AddRecord(new OperationRecord()
            {
                AccountId = CurrentAccount != null ? CurrentAccount.AccountId : "",
                OperateUI = OperationUI.Login,
                OperateTime = DateTime.Now,
                OperateObject = CurrentAccount.Role.ToString(),
                OperateCommand = OperationCommand.Login,
                OperateContent = PermissionService.Service.GetPermissionString(CurrentAccount),
            });
        }

        /// <summary>
        /// 用户标记次数+1
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void AddMarkCount(int times = 1)
        {
            TotalMarkCount += times;
        }

        /// <summary>
        /// TIP注入总次数+1
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void AddTipInjectionCount(int times = 1)
        {
            TipInjectionCount += times;
        }

        /// <summary>
        /// 用户错过TIP次数+1
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void AddMissTipCount(int times = 1)
        {
            MissTipCount += times;
        }

        public bool IsCurrentRoleEqualOrGreaterThan(AccountRole role)
        {
            if (HasLogin)
            {
                switch (role)
                {
                    case AccountRole.System:
                        if (CurrentAccount.Role == AccountRole.System) return true;
                        else return false;
                    case AccountRole.Maintainer:
                        if (CurrentAccount.Role == AccountRole.System || CurrentAccount.Role == AccountRole.Maintainer) return true;
                        else return false;
                    case AccountRole.Admin:
                        if (CurrentAccount.Role != AccountRole.Operator) return true;
                        else return false;
                    default:
                        return false;
                }
            }
            else
            {
                return false;
            }
        }

        public bool IsEqualOrLessThanCurrentRole(AccountRole role)
        {
            if (HasLogin)
            {
                switch (CurrentAccount.Role)
                {
                    case AccountRole.System:
                        return true;
                    case AccountRole.Maintainer:
                        if (role != AccountRole.System) return true;
                        break;
                    case AccountRole.Admin:
                        if (role == AccountRole.Admin || role == AccountRole.Operator)
                            return true;
                        break;
                    case AccountRole.Operator:
                        if (role == AccountRole.Operator) return true;
                        break;
                    default:
                        break;
                }
                return false;
            }
            return false;
        }

        public bool IsLessThanCurrentRole(string id)
        {
            if (AllAccountId.Contains(id))
                return true;

            return false;
        }

        public List<string> GetCurrentAndLessThanId()
        {
            var controller = new AccountDbSet();
            var allAccount = controller.SelectAll();
            var selectedId = new List<string>();

            bool hasCurrent = false;
            foreach (var user in allAccount)
            {
                if (user.AccountId == CurrentAccount.AccountId)
                {
                    hasCurrent = true;
                    break;
                }
            }
            if (!hasCurrent)
            {
                allAccount.Add(CurrentAccount);
            }

            selectedId.Add(CurrentAccount.AccountId);
            if (CurrentAccount.Role == AccountRole.System)
            {
                foreach (var account in allAccount)
                {
                    selectedId.Add(account.AccountId);
                }
            }
            else if (CurrentAccount.Role == AccountRole.Maintainer)
            {
                foreach (var account in allAccount)
                {
                    if (account.Role == AccountRole.Admin || account.Role == AccountRole.Operator)
                    {
                        selectedId.Add(account.AccountId);
                    }
                }
            }
            else if (CurrentAccount.Role == AccountRole.Admin)
            {
                foreach (var account in allAccount)
                {
                    if (account.Role == AccountRole.Operator)
                    {
                        selectedId.Add(account.AccountId);
                    }
                }
            }
            return selectedId;
        }

        private List<Account> GetCurrentAndLessThanAccount()
        {
            var controller = new AccountDbSet();
            var allAccount = controller.SelectAll();
            var selectedAccount = new List<Account>();

           
            if (CurrentAccount.Role == AccountRole.System)
            {
                foreach (var account in allAccount)
                {
                   
                        selectedAccount.Add(account);
                    
                }
            }
            else if (CurrentAccount.Role == AccountRole.Maintainer)
            {
                selectedAccount.Add(CurrentAccount);

                foreach (var account in allAccount)
                {
                    if (account.Role == AccountRole.Admin || account.Role == AccountRole.Operator)
                    {
                       
                            selectedAccount.Add(account);
                        
                    }
                }

            }
            else if (CurrentAccount.Role == AccountRole.Admin)
            {
                selectedAccount.Add(CurrentAccount);

                foreach (var account in allAccount)
                {
                    if (account.Role == AccountRole.Operator)
                    {
                        
                            selectedAccount.Add(account);
                        
                    }
                }
            }

          
            return selectedAccount;
        }

 
        /// <summary>
        /// 注销用户
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Logout()
        {
            if (CurrentAccount != null)
            {
                _logoutEvent.Raise(null, CurrentAccount);

                var sessionSet = new WorkSessionDbSet();
                var session = sessionSet.GetLast();
                session.ObjectCount = BagCounterService.Service.SessionCount;
                session.LogoutTime = DateTime.Now;
                session.ObjectCount = BagCounterService.Service.SessionCount;
                session.TotalMarkCount = TotalMarkCount;
                session.TipInjectionCount = TipInjectionCount;
                session.MissTipCount = MissTipCount;

                sessionSet.Update(session);

                TotalMarkCount = 0;
                TipInjectionCount = 0;
                MissTipCount = 0;
                CurrentAccount = null;
                CurrentAccountPermission = null;
                PermissionService.Service.CurrentAccount = null;

                SystemStatus.Instance.CurrentUser = null;
                SystemStatus.Instance.HasLogin = false;

                if (TRSNetWorkService.Service.SingleMode)
                    return;
                if (_loginMode)
                {
                    try
                    {
                        TRSNetWorkService.Service.UserExit();
                    }
                    catch (Exception)
                    {
                    }
                }
                
            }
        }
    }
}
