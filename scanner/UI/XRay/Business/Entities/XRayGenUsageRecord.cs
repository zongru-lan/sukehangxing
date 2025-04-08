using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Business.Entities
{
    [Serializable]
    public class XRayGenUsageRecord : PropertyNotifiableObject
    {
        public XRayGenUsageRecord()
        {

        }
        public XRayGenUsageRecord(string id,float gen1Timeoffset,int gen1CountOffset,float gen2TimeOffset,int gen2CountOffset)
        {
            AccountId = id;
            ChangeTime = DateTime.Now;
            Gen1UsageTimeOffset = gen1Timeoffset;
            Gen1UsageCountOffset = gen1CountOffset;
            Gen2UsageTimeOffset = gen2TimeOffset;
            Gen2UsageCountOffset = gen2CountOffset;
        }

        private int _xrayGenUsageRecordId;
        [Key]
        public int XRayGenUsageRecordId
        {
            get { return _xrayGenUsageRecordId; }
            set { _xrayGenUsageRecordId = value; RaisePropertyChanged(); }
        }

        private string _account;

        public string AccountId
        {
            get { return _account; }
            set { _account = value; RaisePropertyChanged(); }
        }

        private DateTime _changeTime;

        public DateTime ChangeTime
        {
            get { return _changeTime; }
            set { _changeTime = value; RaisePropertyChanged(); }
        }
        

        private double _Gen1UsageTimeOffset;

        public double Gen1UsageTimeOffset
        {
            get { return _Gen1UsageTimeOffset; }
            set { _Gen1UsageTimeOffset = value; RaisePropertyChanged(); }
        }

        private int _gen1UsageCountOffset;

        public int Gen1UsageCountOffset
        {
            get { return _gen1UsageCountOffset; }
            set { _gen1UsageCountOffset = value; RaisePropertyChanged(); }
        }


        private double _Gen2UsageTimeOffset;

        public double Gen2UsageTimeOffset
        {
            get { return _Gen2UsageTimeOffset; }
            set { _Gen2UsageTimeOffset = value; RaisePropertyChanged(); }
        }

        private int _gen2UsageCountOffset;

        public int Gen2UsageCountOffset
        {
            get { return _gen2UsageCountOffset; }
            set { _gen2UsageCountOffset = value; RaisePropertyChanged(); }
        }
    }
}
