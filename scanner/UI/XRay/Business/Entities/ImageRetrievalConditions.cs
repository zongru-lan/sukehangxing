using System;
using System.Security.AccessControl;
using UI.XRay.Business.Entities.Enums;

namespace UI.XRay.Business.Entities
{
    /// <summary>
    /// 图像检索具体条件：在检索图像时，将根据此类的实例进行检索
    /// </summary>
    public class ImageRetrievalConditions : PropertyNotifiableObject
    {
        public ImageRetrievalConditions(DateTime startTime, DateTime endTime, 
            TimeRange timeRange = TimeRange.LastHour, string accountId = null, bool onlyLocked = false,bool onlyMarked = false)
        {
            StartTime = startTime;
            EndTime = endTime;
            AccountId = accountId;
            OnlyLocked = onlyLocked;
            OnlyMarked = onlyMarked;
            TimeRange = timeRange;
        }

        private TimeRange _timeRange;

        public TimeRange TimeRange
        {
            get { return _timeRange; }
            set { _timeRange = value; RaisePropertyChanged(); }
        }

        private string _accountId;

        /// <summary>
        /// 检索指定用户扫描的图像，如果为空，则表示不局限于某一用户
        /// </summary>
        public string AccountId
        {
            get { return _accountId; }
            set
            {
                _accountId = value;
                RaisePropertyChanged();
            }
        }

        private DateTime _startTime;

        /// <summary>
        /// 检索的起始时间
        /// </summary>
        public DateTime StartTime
        {
            get { return _startTime; }
            set
            {
                _startTime = value;
                RaisePropertyChanged();
            }
        }

        private DateTime _endTime;

        /// <summary>
        /// 检索的结束时间
        /// </summary>
        public DateTime EndTime
        {
            get { return _endTime; }
            set
            {
                _endTime = value;
                RaisePropertyChanged();
            }
        }

        private bool _onlyLocked;

        /// <summary>
        /// true表示仅检索被锁定的图像，false表示全部
        /// </summary>
        public bool OnlyLocked
        {
            get { return _onlyLocked; }
            set
            {
                _onlyLocked = value;
                RaisePropertyChanged();
            }
        }

        private bool _onlyMarked;
        /// <summary>
        /// true表示仅检索被标记的图像，false表示全部
        /// </summary>
        public bool OnlyMarked
        {
            get { return _onlyMarked; }
            set { _onlyMarked = value; RaisePropertyChanged();}
        }
        
    }
}
