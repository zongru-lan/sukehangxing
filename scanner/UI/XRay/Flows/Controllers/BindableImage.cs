using System;
using System.IO;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using UI.XRay.Business.Entities;

namespace UI.XRay.Flows.Controllers
{
    /// <summary>
    /// �ɰ󶨵�ͼ��ʹ����INotifyPropertyChanged�ӿڡ������ڰ󶨵�WPF�ؼ���ʵ��֪ͨ����
    /// </summary>
    public class BindableImage : ObservableObject
    {
        public BindableImage(ImageRecord record)
        {
            Record = record;
            ImagePath = record.StorePath;
            ScanTime = record.ScanTime;
            IsManualMark = record.IsManualSaved;
        }

        public BindableImage(string path)
        {
            ImagePath = path;
            FileName = Path.GetFileNameWithoutExtension(path);
        }

        public BindableImage()
        {
        }

        /// <summary>
        /// ��ͼ�������ݿ��������
        /// </summary>
        public string ImageGuid { get; set; }

        /// <summary>
        /// ��ͼ���ڴ����еĴ洢ȫ·��
        /// </summary>
        public string ImagePath { get; set; }

        private DateTime _scanTime;

        /// <summary>
        /// ͼ��ɨ��ʱ��
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

        private BitmapSource _thumbnail;

        /// <summary>
        /// ͼ�������ͼ
        /// </summary>
        public BitmapSource Thumbnail
        {
            get { return _thumbnail; }
            set
            {
                _thumbnail = value;
                RaisePropertyChanged();
            }
        }

        private BitmapSource _view1Image;

        public BitmapSource View1Image
        {
            get { return _view1Image; }
            set
            {
                _view1Image = value;
                RaisePropertyChanged();
            }
        }

        private BitmapSource _view2Image;

        public BitmapSource View2Image
        {
            get { return _view2Image; }
            set
            {
                _view2Image = value;
                RaisePropertyChanged();
            }
        }

        private bool _dualView = false;

        public bool DualView
        {
            get { return _dualView; }
            set
            {
                _dualView = value;
                RaisePropertyChanged();
            }
        }

        private string _channalid;

        public string ChannalId
        {
            get { return _channalid; }
            set
            {
                _channalid = value;
                RaisePropertyChanged();
            }
        }

        public bool IsManualMark
        {
            get
            {
                if (Record == null)
                    return false;
                return Record.IsManualSaved;
            }
            set
            {
                if (Record != null)
                {
                    Record.IsManualSaved = value;
                }

                RaisePropertyChanged();
            }
        }
        

        /// <summary>
        /// ͼ���Ƿ��Ѿ�������
        /// </summary>
        public bool IsLocked
        {
            get
            {
                if (Record == null)
                    return false;
                return Record.IsLocked;
            }
            set
            {
                if (Record != null)
                {
                    Record.IsLocked = value;
                }

                RaisePropertyChanged();
            }
        }

        private bool _isSelected;

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                RaisePropertyChanged();
            }
        }

        private string _fileName;

        public ImageRecord Record { get; set; }

        /// <summary>
        /// �ļ�������������չ����·��
        /// </summary>
        public string FileName
        {
            get { return _fileName; }
            set { _fileName = value; RaisePropertyChanged(); }
        }
    }
}