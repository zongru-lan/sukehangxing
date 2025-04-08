using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Flows.TRSNetwork.Models
{
    [Serializable]
    public class TipLogStatisticResult
    {
        /// <summary>
        /// 账户名
        /// </summary>
        private string _accountId;

        private int _logInTimes;

        /// <summary>
        /// 行李数
        /// </summary>
        private int _bagCount;

        /// <summary>
        /// 危险品插入次数
        /// </summary>
        private int _tipInjectionCount;

        /// <summary>
        /// 用户总的标记次数（包括用户随意标记，即无tip的情况下的标记）
        /// </summary>
        private int _totolMarkCount;

        /// <summary>
        /// 错过TIP次数
        /// </summary>
        private int _missTipCount;

        /// <summary>
        /// Tip漏检率
        /// </summary>
        private double _missRate;

        /// <summary>
        /// 账户名
        /// </summary>
        public string AccountId
        {
            get { return _accountId; }
            set { _accountId = value; }
        }

        /// <summary>
        /// 行李数
        /// </summary>
        public int BagCount
        {
            get { return _bagCount; }
            set { _bagCount = value; }
        }

        /// <summary>
        /// 危险品插入次数
        /// </summary>
        public int TipInjectionCount
        {
            get { return _tipInjectionCount; }
            set { _tipInjectionCount = value; }
        }

        /// <summary>
        /// 用户总的标记次数（包括用户随意标记，即无tip的情况下的标记）
        /// </summary>
        public int TotalMarkCount
        {
            get { return _totolMarkCount; }
            set { _totolMarkCount = value; }
        }

        /// <summary>
        /// 错过TIP次数
        /// </summary>
        public int MissTipCount
        {
            get { return _missTipCount; }
            set { _missTipCount = value; }
        }

        /// <summary>
        /// Tip漏检率
        /// </summary>
        public double MissRate
        {
            get { return _missRate; }
            set { _missRate = value; }
        }

        /// <summary>
        /// 用户在此期间内登陆总次数
        /// </summary>
        public int LogInTimes
        {
            get { return _logInTimes; }
            set { _logInTimes = value; }
        }

        private DateTime loginDateTime;

        /// <summary>
        /// 该账户登陆时的时间，用于区分TIP日志所属的账户登陆周期
        /// </summary>
        public DateTime LoginDateTime
        {
            get { return loginDateTime; }
            set { loginDateTime = value; }
        }
    }
}
