using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Business.Entities
{
    [Serializable]
    public class TipPlan : PropertyNotifiableObject
    {
        #region Properties
        /// <summary>
        /// 此Tip注入计划的别名
        /// </summary>
        private string _alias = "abc";
        public string Alias
        {
            get { return _alias; }
            set { _alias = value; 
                RaisePropertyChanged(); }
        }

        /// <summary>
        /// 此计划的创建时间
        /// </summary>
        private DateTime _creationTime = DateTime.Now;
        public DateTime CreationTime
        {
            get { return _creationTime; }
            set { _creationTime = value; RaisePropertyChanged(); }
        }

        private long _tipPlanId;
        [Key]
        public long TipPlanId
        {
            get { return _tipPlanId; }
            set { _tipPlanId = value; RaisePropertyChanged(); }
        }

        ///// <summary>
        ///// 优先级，1-5
        ///// </summary>
        //private int _priority = 1;
        //public int Priority
        //{
        //    get { return _priority; }
        //    set { _priority = value; RaisePropertyChanged(); }
        //}

        /// <summary>
        /// 插入概率百分比，如90表示对90%的图像都插入Tip
        /// </summary>
        private int _probability = 50;
        public int Probability
        {
            get { return _probability; }
            set { _probability = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 此计划是否生效
        /// </summary>
        private bool _isEnabled = false;
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { _isEnabled = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 此计划的有效起始时间
        /// </summary>
        private DateTime _startTime;
        public DateTime StartTime
        {
            get { return _startTime; }
            set { _startTime = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 此计划的有效结束时间
        /// </summary>
        private DateTime _endTime;
        public DateTime EndTime
        {
            get { return _endTime; }
            set
            {
                if (value > StartTime)
                {
                    _endTime = value;
                    RaisePropertyChanged();
                }
                else
                {
                    _endTime = _endTime > _startTime ? _endTime : _startTime;
                    RaisePropertyChanged();
                }
            }
        }       

        /// <summary>
        /// Tip插入完成后，用户线下（停输送机后）判读的最长秒数
        /// </summary>
        private int _offlineRecMaxSeconds = 8;
        public int OfflineRecMaxSeconds
        {
            get { return _offlineRecMaxSeconds; }
            set { _offlineRecMaxSeconds = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 默认权重
        /// </summary>
        private static int _defaultWeight = 0;

        /// <summary>
        /// 刀具插入的权重，0-10
        /// </summary>
        private int _knivesWeight = _defaultWeight;
        public int KnivesWeight
        {
            get { return _knivesWeight; }
            set { _knivesWeight = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 枪支插入的权重，0-10
        /// </summary>
        private int _gunsWeight = _defaultWeight;
        public int GunsWeight
        {
            get { return _gunsWeight; }
            set { _gunsWeight = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 炸药插入的权重，0-10
        /// </summary>
        private int _explosivesWeight = _defaultWeight;
        public int ExplosivesWeight
        {
            get { return _explosivesWeight; }
            set { _explosivesWeight = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 其它物品的插入权重，0-10
        /// </summary>
        private int _otherObjectsWeight = _defaultWeight;
        public int OtherObjectsWeight
        {
            get { return _otherObjectsWeight; }
            set { _otherObjectsWeight = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 获取四种Tip图像的总权重
        /// </summary>
        public int TotalWeights
        {
            get { return ExplosivesWeight + KnivesWeight + GunsWeight + OtherObjectsWeight; }
        }
        #endregion

        #region Constructors
        public TipPlan()
        {
            StartTime = DateTime.Now;
            EndTime = DateTime.Now + TimeSpan.FromDays(30);
        }

        public TipPlan(string alias, DateTime creationTime, long tipPlanId, int probability, bool isEnabled, DateTime startTime, DateTime endTime, int offlineRecMaxSeconds,
            int knivesWeight, int gunsWeight, int explosivesWeight, int otherObjectsWeight)
        {
            this.Alias = alias;
            this.CreationTime = creationTime;
            this.TipPlanId = tipPlanId;
            this.Probability = probability;
            this.IsEnabled = isEnabled;
            this.StartTime = startTime;
            this.EndTime = endTime;
            this.OfflineRecMaxSeconds = offlineRecMaxSeconds;
            this.KnivesWeight = knivesWeight;
            this.GunsWeight = gunsWeight;
            this.ExplosivesWeight = explosivesWeight;
            this.OtherObjectsWeight = otherObjectsWeight;
        }
        #endregion
    }
}
