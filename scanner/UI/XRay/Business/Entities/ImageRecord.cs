using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Business.Entities
{
    /// <summary>
    /// 表示一条图像扫描记录
    /// </summary>
    public class ImageRecord : PropertyNotifiableObject
    {
        public ImageRecord()
        {
            ScanTime = DateTime.Now;
        }

        private long _imageRecordId;

        private long _objectId;

        private string _storePath;

        private string _accountId;

        private string _machineNumber;

        private bool _isLocked;

        private bool _isManualSaved;

        private DateTime _scanTime;

        /// <summary>
        /// 图像记录在数据库中的唯一编号。在插入数据库之前，不需要指定一个值；在执行插入之后，数据库会为此属性自动分配一个最新的值
        /// </summary>
        [Key]
        public long ImageRecordId
        {
            get { return _imageRecordId; }
            set { _imageRecordId = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 此图像对应的物体编号，是另一个物体表的主键
        /// </summary>
        public long ObjectId
        {
            get { return _objectId; }
            set { _objectId = value; RaisePropertyChanged();}
        }

        public string StorePath
        {
            get { return _storePath; }
            set { _storePath = value; RaisePropertyChanged();}
        }

        public string AccountId
        {
            get { return _accountId; }
            set { _accountId = value; RaisePropertyChanged();}
        }

        public string MachineNumber
        {
            get { return _machineNumber; }
            set { _machineNumber = value; RaisePropertyChanged();}
        }

        public bool IsLocked
        {
            get { return _isLocked; }
            set { _isLocked = value; RaisePropertyChanged();}
        }

        public bool IsManualSaved
        {
            get { return _isManualSaved; }
            set { _isManualSaved = value; RaisePropertyChanged();}
        }

        /// <summary>
        /// 图像扫描时间
        /// </summary>
        public DateTime ScanTime
        {
            get { return _scanTime; }
            set
            {
                _scanTime = value;
                RaisePropertyChanged();
            }
        }
    }
}
