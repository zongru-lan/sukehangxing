using System;
using System.ComponentModel.DataAnnotations;

namespace UI.XRay.Business.Entities
{
    [Serializable]
    public class TipPlanandImage : PropertyNotifiableObject
    {
        [Key]
        private long _id;
        public long Id
        {
            get { return _id; }
            set { _id = value; RaisePropertyChanged(); }
        }
        private long _tipPlanId;
        public long TipPlanId
        {
            get { return _tipPlanId; }
            set { _tipPlanId = value; RaisePropertyChanged(); }
        }

        private string _library;
        public string Library
        {
            get { return _library; }
            set { _library = value; RaisePropertyChanged(); }
        }

        private string _imageName;
        public string ImageName
        {
            get { return _imageName; }
            set { _imageName = value; RaisePropertyChanged(); }
        }

        public TipPlanandImage()
        {

        }
    }
}
