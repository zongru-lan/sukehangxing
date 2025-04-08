using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Business.Entities
{
    /// <summary>
    /// 工作会话：记录一个账户登录、注销的过程，及其在此期间扫描物体的计数
    /// 此实体对应数据库中的同名数据表 WorkSession，因此不要随意改变属性名称。
    /// </summary>
    public class WorkSession : PropertyNotifiableObject
    {
        private long _workSessionId;

        private string _accountId;

        private string _machineNumber;

        private int _objectCount;

        /// <summary>
        /// 会话期间，用户标记总数（包括误标记）
        /// </summary>
        private int _totalMarkCount;

        /// <summary>
        /// 会话期间，注入的Tip总数
        /// </summary>
        private int _tipInjectionCount;

        /// <summary>
        /// 会话期间，错过的Tip总数
        /// </summary>
        private int _missTipCount;

        /// <summary>
        /// 会话期间，射线发射计时
        /// </summary>
        private int _radiatingSeconds;

        private DateTime _loginTime;

        private DateTime? _logoutTime;

        /// <summary>
        /// 主键，在插入数据库时，将由数据库自动分配一个值，因此在创建时不需要指定
        /// </summary>
        [Key]
        public long WorkSessionId
        {
            get { return _workSessionId; }
            set { _workSessionId = value; RaisePropertyChanged();}
        }

        public string AccountId
        {
            get { return _accountId; }
            set { _accountId = value; RaisePropertyChanged();}
        }

        public string MachineNumber
        {
            get { return _machineNumber; }
            set { _machineNumber = value; RaisePropertyChanged();}
        }

        public int ObjectCount
        {
            get { return _objectCount; }
            set { _objectCount = value; RaisePropertyChanged();}
        }

        public DateTime LoginTime
        {
            get { return _loginTime; }
            set { _loginTime = value; RaisePropertyChanged();}
        }

        public DateTime? LogoutTime
        {
            get { return _logoutTime; }
            set { _logoutTime = value; RaisePropertyChanged();}
        }

        /// <summary>
        /// 获取总的工作时间：单位为小时
        /// </summary>
        public double WorkingHours
        {
            get
            {
                if (LogoutTime.HasValue)
                {
                    return ((DateTime)LogoutTime - LoginTime).TotalHours;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// 会话期间，用户标记总数（包括误标记）
        /// </summary>
        public int TotalMarkCount
        {
            get { return _totalMarkCount; }
            set { _totalMarkCount = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 会话期间，注入的Tip总数
        /// </summary>
        public int TipInjectionCount
        {
            get { return _tipInjectionCount; }
            set { _tipInjectionCount = value; RaisePropertyChanged();}
        }

        /// <summary>
        /// 会话期间，错过的Tip总数
        /// </summary>
        public int MissTipCount
        {
            get { return _missTipCount; }
            set { _missTipCount = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 会话期间，射线发射计时
        /// </summary>
        public int RadiatingSeconds
        {
            get { return _radiatingSeconds; }
            set { _radiatingSeconds = value; RaisePropertyChanged();}
        }

        public WorkSession()
        {
            
        }

        public WorkSession(string accountId, DateTime loginTime, DateTime? logOutTime, int objCount)
        {
            AccountId = accountId;
            LoginTime = loginTime;
            LogoutTime = logOutTime;
            ObjectCount = objCount;
        }
    }
}
