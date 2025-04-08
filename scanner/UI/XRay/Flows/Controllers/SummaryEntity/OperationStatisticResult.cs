using GalaSoft.MvvmLight;

namespace UI.XRay.Flows.Controllers.SummaryEntity
{
    /// <summary>
    /// �û�����ͳ�ƽ��
    /// </summary>
    public class OperationStatisticResult : StatisticResultBase
    {
        /// <summary>
        /// �˻���
        /// </summary>
        private string _accountId;

        private int _logInTimes;

        /// <summary>
        /// ������
        /// </summary>
        private int _bagCount;
         
        /// <summary>
        /// Σ��Ʒ�������
        /// </summary>
        private int _tipInjectionCount;

        /// <summary>
        /// �û��ܵı�Ǵ����������û������ǣ�����tip������µı�ǣ�
        /// </summary>
        private int _totolMarkCount;

        /// <summary>
        /// ���TIP����
        /// </summary>
        private int _missTipCount;

        /// <summary>
        /// Tip©����
        /// </summary>
        private double _missRate;

        /// <summary>
        /// �˻���
        /// </summary>
        public string AccountId
        {
            get { return _accountId; }
            set { _accountId = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// ������
        /// </summary>
        public int BagCount
        {
            get { return _bagCount; }
            set { _bagCount = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// Σ��Ʒ�������
        /// </summary>
        public int TipInjectionCount
        {
            get { return _tipInjectionCount; }
            set { _tipInjectionCount = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// �û��ܵı�Ǵ����������û������ǣ�����tip������µı�ǣ�
        /// </summary>
        public int TotolMarkCount
        {
            get { return _totolMarkCount; }
            set { _totolMarkCount = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// ���TIP����
        /// </summary>
        public int MissTipCount
        {
            get { return _missTipCount; }
            set { _missTipCount = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// Tip©����
        /// </summary>
        public double MissRate
        {
            get { return _missRate; }
            set { _missRate = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// �û��ڴ��ڼ��ڵ�½�ܴ���
        /// </summary>
        public int LogInTimes
        {
            get { return _logInTimes; }
            set { _logInTimes = value; RaisePropertyChanged(); }
        }
    }
}