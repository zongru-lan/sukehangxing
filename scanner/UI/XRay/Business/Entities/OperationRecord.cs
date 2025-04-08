using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Business.Entities
{
    /// <summary>
    /// 记录主要操作，如设置参数，打开关闭硬件，删除转存图像，重置计数，
    /// 登录退出，图像处理效果，P功能键，回拉，专用键盘功能键，界面，新建删除帐户，日志查看及导出等
    /// </summary>
    [Serializable]
    public class OperationRecord : PropertyNotifiableObject
    {
        private long _operationRecordId;
        [Key]
        public long OperationRecordId
        {
            get { return _operationRecordId; }
            set { _operationRecordId = value; RaisePropertyChanged();}
        }
        

        private string _accountId;
        public string AccountId
        {
            get { return _accountId; }
            set { _accountId = value; RaisePropertyChanged(); }
        }

        private DateTime _time;
        public DateTime OperateTime
        {
            get { return _time; }
            set { _time = value; RaisePropertyChanged(); }
        }

        private OperationUI _operateUI;
        public OperationUI OperateUI
        {
            get { return _operateUI; }
            set { _operateUI = value; RaisePropertyChanged(); }
        }

        private string _operateObject;
        public string OperateObject
        {
            get { return _operateObject; }
            set { _operateObject = value; RaisePropertyChanged(); }
        }

        private OperationCommand _operateCommand;

        public OperationCommand OperateCommand
        {
            get { return _operateCommand; }
            set { _operateCommand = value; RaisePropertyChanged(); }
        }
        

        private string _operateContent;
        public string OperateContent
        {
            get { return _operateContent; }
            set { _operateContent = value; RaisePropertyChanged(); }
        }
    }

    public class OperationRecordShow : PropertyNotifiableObject
    {
        private long _operationRecordId;
       // [Key]             //yxc 注释掉了主键
        public long OperationRecordId
        {
            get { return _operationRecordId; }
            set { _operationRecordId = value; RaisePropertyChanged(); }
        }


        private string _accountId;
        public string AccountId
        {
            get { return _accountId; }
            set { _accountId = value; RaisePropertyChanged(); }
        }

        private string _time;
        public string OperateTime
        {
            get { return _time; }
            set { _time = value; RaisePropertyChanged(); }
        }

        private string _operateUI;
        public string OperateUI
        {
            get { return _operateUI; }
            set { _operateUI = value; RaisePropertyChanged(); }
        }

        private string _operateObject;
        public string OperateObject
        {
            get { return _operateObject; }
            set { _operateObject = value; RaisePropertyChanged(); }
        }

        private string _operateCommand;

        public string OperateCommand
        {
            get { return _operateCommand; }
            set { _operateCommand = value; RaisePropertyChanged(); }
        }


        private string _operateContent;
        public string OperateContent
        {
            get { return _operateContent; }
            set { _operateContent = value; RaisePropertyChanged(); }
        }
    }

    public enum OperationUI
    {
        MainUI = 0,
        SystemMenu = 1,

        //about
        About,

        //account
        ChangePassword,
        ManageOtherAccounts,
        ManageGroups,

        //device
        Detectors,
        XRayGen,
        ConveyorPESensor,
        Keyboard,
        ControlSystem,
        Diagnosis,

        //image
        ImageSetting,
        ImsPage,
        ImsListPage,
        ImageRetrieval,

        //record
        BootLog,
        ConveyorWorkLog,
        LoginLog,
        OperationLog,
        TipExamLog,
        XRayGenWorkLog,

        //setting
        DiskSpaceManage,
        FunctionKeys,
        IntelliSense,
        ObjectCounter,

        //tip
        TipImages,
        TipPlans,

        //training
        TrainingSetting,
        TrainingImagesManagement,

        //other
        Calibrate,
        XRayGenWarmup,
        ChangeSysDateTime,
        Login,
        ScreenImagesOperation,
        ImageBadChannel,
    }

    public enum OperationCommand
    {
        Saveto = 0,
        Delete = 1,
        Create = 2,
        Setting = 3,
        ShowUI = 4,
        Open = 5,
        Close = 6,
        KeyPress = 7,
        Import = 8,
        Export = 9,
        Relay = 10,
        Lock,
        Unlock,
        Print,
        Find,
        Login,
    }
}
