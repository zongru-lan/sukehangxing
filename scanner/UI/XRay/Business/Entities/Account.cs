using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Serialization;

namespace UI.XRay.Business.Entities
{
    /// <summary>
    /// 表示一个用户账户
    /// </summary>
    [Serializable]
    public class Account : PropertyNotifiableObject
    {
        #region Default Fields
        private const string defaultEffectsCompositions = "MaterialColor,SlicePenetrate,False,False;MaterialColor,SlicePenetrate,False,False;MaterialColor,SlicePenetrate,False,False";
        private const string defaultActionTypes = "StartShapeCorrection;StartShapeCorrection;StartShapeCorrection";
        #endregion

        public Account()
        {

        }

        public Account(string id, string name, string password, AccountRole role, int permisssion, bool isActive = true, bool isEnable = true,
            bool isNetAccount = false, string groupName = "Default Group", string effectsCompositions = defaultEffectsCompositions,
            string actionTypes = defaultActionTypes)
        {
            AccountId = id;
            Name = name;
            Password = password;
            Role = role;
            IsActive = isActive;
            IsEnable = isEnable;
            PermissionValue = permisssion;
            IsNetAccount = isNetAccount;
            GroupName = groupName;
            EffectsCompositions = effectsCompositions;
            ActionTypes = actionTypes;
        }

        
        private bool _isEnable = true;
        public bool IsEnable
        {
            get { return _isEnable; }
            set
            {
                _isEnable = value;
                RaisePropertyChanged();
            }
        }
        private string _accountId;
        //[Key]
        public string AccountId
        {
            get { return _accountId; }
            set
            {
                _accountId = value;
                RaisePropertyChanged();
            }
        }

        private string _password;
        public string Password
        {
            get { return _password; }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _password = value;
                    RaisePropertyChanged();
                }

            }
        }

        private AccountRole _role;

        public AccountRole Role
        {
            get { return _role; }
            set
            {
                _role = value;
                RaisePropertyChanged();
            }
        }

        private string _name;

        public string Name
        {
            get { return _name; }
            set { _name = value; RaisePropertyChanged(); }
        }


        private bool _isActive;
        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                _isActive = value;
                RaisePropertyChanged();
            }
        }

        private bool _isNetAccount;
        public bool IsNetAccount
        {
            get { return _isNetAccount; }
            set
            {
                _isNetAccount = value;
                RaisePropertyChanged();
            }
        }

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

        private string _employeeId;

        public string EmployeeId
        {
            get { return _employeeId; }
            set { _employeeId = value; RaisePropertyChanged(); }
        }

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

        private int _permissionValue;
        public int PermissionValue
        {
            get { return _permissionValue; }
            set
            {
                _permissionValue = value;
                RaisePropertyChanged();
            }
        }
        private string _effectsCompositions= defaultEffectsCompositions;

        public string EffectsCompositions
        {
            get { return _effectsCompositions; }
            set { _effectsCompositions = value; RaisePropertyChanged(); }
        }

        private string _actionTypes= defaultActionTypes;

        //[XmlElement(ElementName = "ActionTypes", DataType = "string", IsNullable = false), DefaultValue(defaultActionTypes)]
        public string ActionTypes
        {
            get { return _actionTypes; }
            set { _actionTypes = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 用户显示的密码：在被重置前，为******，被重置后，将显示为默认密码
        /// 此属性不存储于数据库中
        /// </summary>
        private string _displayPassword = "******";
        [NotMapped, XmlIgnore]
        public string DisplayPassword
        {
            get { return _displayPassword; }
            set
            {
                _displayPassword = value;
                RaisePropertyChanged();
            }
        }

        private bool _isExportToNet = false;
        [NotMapped, XmlIgnore]
        public bool IsExportToNet
        {
            get { return _isExportToNet; }
            set
            {
                _isExportToNet = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 当重置时，默认使用的密码
        /// </summary>
        public static string DefaultPassword
        {
            get { return "000000"; }
        }
        public override string ToString()
        {
            return this.AccountId;
        }
    }
}
