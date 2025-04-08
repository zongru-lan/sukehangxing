using System;
using System.ComponentModel.DataAnnotations;

namespace UI.XRay.Business.Entities
{
    [Serializable]
    public class AccountGroup : PropertyNotifiableObject
    {
        #region Constructors
        public AccountGroup() { }

        public AccountGroup(string groupID, string groupName, string description = "")
        {
            GroupID = groupID;
            GroupName = groupName;
            Description = description;
        }
        #endregion

        #region Properties
        /// <summary>组ID，是AccountGroup的主键</summary>
        private string _groupID;
        [Key]
        public string GroupID
        {
            get { return _groupID; }
            set
            {
                _groupID = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>组名</summary>
        private string _groupName;
        public string GroupName
        {
            get { return _groupName; }
            set
            {
                _groupName = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>组描述</summary>
        private string _description;
        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region Methods
        public bool Equals(AccountGroup other)
        {
            return this.GroupID == other.GroupID && this.GroupName == other.GroupName && this.Description == other.Description;
        }
        #endregion
    }
}
