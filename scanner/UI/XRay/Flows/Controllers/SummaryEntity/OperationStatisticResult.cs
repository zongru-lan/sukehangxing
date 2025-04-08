using GalaSoft.MvvmLight;

namespace UI.XRay.Flows.Controllers.SummaryEntity
{
    /// <summary>
    /// 用户操作统计结果
    /// </summary>
    public class OperationStatisticResult : StatisticResultBase
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
            set { _accountId = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 行李数
        /// </summary>
        public int BagCount
        {
            get { return _bagCount; }
            set { _bagCount = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 危险品插入次数
        /// </summary>
        public int TipInjectionCount
        {
            get { return _tipInjectionCount; }
            set { _tipInjectionCount = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 用户总的标记次数（包括用户随意标记，即无tip的情况下的标记）
        /// </summary>
        public int TotolMarkCount
        {
            get { return _totolMarkCount; }
            set { _totolMarkCount = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 错过TIP次数
        /// </summary>
        public int MissTipCount
        {
            get { return _missTipCount; }
            set { _missTipCount = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// Tip漏检率
        /// </summary>
        public double MissRate
        {
            get { return _missRate; }
            set { _missRate = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 用户在此期间内登陆总次数
        /// </summary>
        public int LogInTimes
        {
            get { return _logInTimes; }
            set { _logInTimes = value; RaisePropertyChanged(); }
        }
    }
}