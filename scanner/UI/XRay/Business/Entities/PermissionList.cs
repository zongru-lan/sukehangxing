using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Business.Entities
{
    [Serializable]
    public class PermissionList
    {
        public PermissionList()
        {
            CanTraining = false;
            CanChangeImageSettings = false;
            CanManageDisk = false;
            CanManageLog = false;
        }
        
        /// <summary>
        /// 启用培训的权限 0x01
        /// </summary>
        public bool CanTraining
        {
            get { return _canTraining; }
            set { _canTraining = value; }
        }
        
        /// <summary>
        ///更改图像设置的权限 0x02
        /// </summary>
        public bool CanChangeImageSettings
        {
            get { return _canImageSetting; }
            set { _canImageSetting = value; }
        }
       
        /// <summary>
        /// 磁盘空间管理的权限 0x04
        /// </summary>
        public bool CanManageDisk
        {
            get { return _canManageDisk; }
            set { _canManageDisk = value; }
        }
        
        /// <summary>
        /// 重置计数的权限 0x08
        /// </summary>
        public bool CanManageLog 
        {
            get { return _canManageLog; }
            set { _canManageLog = value; }
        }


        private bool _canTraining;
        private bool _canImageSetting;
        private bool _canManageDisk;
        private bool _canManageLog;
    }
}
