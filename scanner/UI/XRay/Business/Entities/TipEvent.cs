using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Business.Entities
{
    /// <summary>
    /// 表示一次Tip插入的记录
    /// </summary>
    public class TipEventRecord : PropertyNotifiableObject
    {
        private long _tipEventId;

        /// <summary>
        /// 对应的图像在数据库中的编号
        /// </summary>
        //private long _imageRecordId;

        /// <summary>
        /// 被考核用户的Id
        /// </summary>
        private string _evaluatedAccountId;

        /// <summary>
        /// Tip插入完成的时刻。如果没有插入，则为空
        /// </summary>
        private DateTime _injectionTime;

        /// <summary>
        /// 被考核用户识别出tip的时刻；如果识别失败，则为空
        /// </summary>
        private DateTime? _recognizedTime;

        /// <summary>
        /// 注入时使用的Tip库类型
        /// </summary>
        private TipLibrary? _library;

        /// <summary>
        /// 注入时使用的Tip文件名称
        /// </summary>
        private string _fileName;

        /// <summary>
        /// 被考核用户在识别Tip期间，是否停止了输送机
        /// </summary>
        private bool _isConveyorStopped;

        [Key]
        public long TipEventId
        {
            get { return _tipEventId; }
            set { _tipEventId = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 对应的图像在数据库中的编号
        /// </summary>
        //public long ImageRecordId
        //{
        //    get { return _imageRecordId; }
        //    set { _imageRecordId = value; RaisePropertyChanged(); }
        //}

        /// <summary>
        /// 被考核用户的Id
        /// </summary>
        public string EvaluatedAccountId
        {
            get { return _evaluatedAccountId; }
            set { _evaluatedAccountId = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// Tip插入完成的时刻。
        /// </summary>
        public DateTime InjectionTime
        {
            get { return _injectionTime; }
            set { _injectionTime = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 被考核用户识别出tip的时刻；如果识别失败，则为空
        /// </summary>
        public DateTime? RecognizedTime
        {
            get { return _recognizedTime; }
            set { _recognizedTime = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 被考核用户在识别Tip期间，是否停止了输送机
        /// </summary>
        public bool IsConveyorStopped
        {
            get { return _isConveyorStopped; }
            set { _isConveyorStopped = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 注入时使用的Tip库类型
        /// </summary>
        public TipLibrary? Library
        {
            get { return _library; }
            set { _library = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 注入时使用的Tip文件名称
        /// </summary>
        public string FileName
        {
            get { return _fileName; }
            set { _fileName = value; RaisePropertyChanged(); }
        }
    }
}
