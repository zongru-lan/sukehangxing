using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Services;
using GalaSoft.MvvmLight.Messaging;
using UI.XRay.Security.Scanner.Converters;
using System.IO;
using System.Xml.Serialization;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Flows.Services.DataProcess;
using System.Windows.Media.Media3D;
using UI.XRay.Business.Algo;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;

namespace UI.XRay.Security.Scanner.ViewModel
{
    public class SendCanvasParam
    {
        public double ActHeight { get; set; }
        public double ActWidth { get; set; }
        public SendCanvasParam(double height, double width)
        {
            ActHeight = height;
            ActWidth = width;
        }
    }
    public class DetectViewSelection
    {
        public DetectViewSelection(int viewNum, string display)
        {
            ViewNum = viewNum;
            DisplayViewNum = display;
        }

        public string DisplayViewNum { get; set; }

        /// <summary>
        /// 1或2表示视角编号
        /// </summary>
        public int ViewNum { get; set; }
    }

    public class ImageBadChannelManualViewModel : ViewModelBase
    {
        /// <summary>
        /// 视角切换Combo控件ChangeEvent
        /// </summary>
        public RelayCommand ViewSelectionChangedEventCommand { get; set; }

        /// <summary>
        /// 双击改变坏点选中状态
        /// </summary>
        public RelayCommand<ChannelBadFlag> SelectionChangedCommand { get; set; }

        public RelayCommand ShowImageCommand { get; set; }


        /// <summary>
        /// 响应保存按钮事件
        /// </summary>
        public RelayCommand SaveBadChannelCommand { get; set; }


        /// <summary>
        /// 切换显示模式 1：直线标识  2：插值图像
        /// </summary>
        public RelayCommand ImageShowSelectionChangedEventCommand { get; set; }


        private List<ChannelBadFlag> _badChannelFlags;
        /// <summary>
        /// 当前视角的每个通道是否为坏点的标志的列表
        /// </summary>
        public List<ChannelBadFlag> BadChannelFlags
        {
            get { return _badChannelFlags; }
            set { _badChannelFlags = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 多线程同步锁
        /// </summary>
        private readonly object _sync = new object();

        private List<ChannelBadFlag> _view1HorizonalPoints = new List<ChannelBadFlag>();
        private List<ChannelBadFlag> _view2HorizonalPoints = new List<ChannelBadFlag>();
        readonly XRayImageProcessor _ip;

        /// <summary>
        ///记录坏点探测通道及其左右对应的用来插值的探测通道的编号的结构体
        /// </summary>
        struct BadChannelInterpolationPosition
        {
            //坏点探测通道
            public int BadChannelIndex;
            //坏点探测通道左侧用来插值的探测通道编号
            public int LeftInterpolationChannelIndex;
            //坏点探测通道右侧用来插值的探测通道编号
            public int RightInterpolationChannelIndex;
        }

        /// <summary>
        /// 双视角数据设置是否可见
        /// </summary>
        private Visibility _dualViewSettingVisibility;
        /// <summary>
        /// 双视角数据设置是否可见
        /// </summary>
        public Visibility DualViewSettingVisibility
        {
            get { return _dualViewSettingVisibility; }
            set { _dualViewSettingVisibility = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 探测视角个数
        /// </summary>
        public List<DetectViewSelection> DetectViews { get; private set; }

        //public List<DetectViewSelection> ImageShowStyle { get; private set; }

        RemoveInstableChannelsOperator removeInstableInstance;
        RemoveBadChannelsOperator removeBadInstance;

        private DetectViewSelection _selectedView;//默认视角
        /// <summary>
        /// 当前选中的视角索引，1表示视角1，2表示视角2
        /// </summary>
        public DetectViewSelection SelectedView
        {
            get { return _selectedView; }
            set
            {
                _selectedView = value;
                ViewSelectionChangedEventCommandExecute();
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 当前选中的图像显示索引，1表示直线标识，2表示插值图像
        /// </summary>
        private int _selectImageShow;
        public int SelectedShowStyle
        {
            get { return _selectImageShow; }
            set { _selectImageShow = value; RaisePropertyChanged(); }
        }

        private string _imageDateTime;

        public string ImageDateTime
        {
            get { return _imageDateTime; }
            set { _imageDateTime = value; RaisePropertyChanged(); }
        }


        private ImageRecord _imagerecord;

        public ImageRecord ImageRecord
        {
            get { return _imagerecord; }
            set
            {
                _imagerecord = value;
                GetImageData();
                RaisePropertyChanged();
            }
        }


        bool _isRemoveEdge = false;
        private string _view1FilePath = ConfigPath.View1InstableChennelsSettingFilePath;
        private string _view2FilePath = ConfigPath.View2InstableChennelsSettingFilePath;

        private int _image1Height;
        private int _image2Height;

        private int _channel1Count;
        private int _channel2Count;

        private DateTime _lastSaveTime = DateTime.Now;

        public ImageBadChannelManualViewModel(ImageRecord record)
        {
            DetectViews = new List<DetectViewSelection> { new DetectViewSelection(1, "1") };
            DualViewSettingVisibility = Visibility.Collapsed;

            DetectViews.Add(new DetectViewSelection(2, "2"));
            this.SelectedView = DetectViews.FirstOrDefault();

            // 在双视角模式，且当前用户为系统用户时，可以配置双视角的顺序
            //if (LoginAccountManager.HasLogin && LoginAccountManager.CurrentAccount.Role == AccountRole.System)
            //{
            //    DualViewSettingVisibility = Visibility.Visible;
            //}


            //ImageShowStyle = new List<DetectViewSelection>
            //{
            //    new DetectViewSelection(0, "原图"),
            //    new DetectViewSelection(1, "直线标识"),
            //    new DetectViewSelection(2, "插值模式")
            //};

            _ip = new XRayImageProcessor();

            _isRemoveEdge = MainViewModel.IsShaped;

            if (_isRemoveEdge)
            {
                _view1FilePath = ConfigPath.View1InstableChennelsSettingFilePath;
                _view2FilePath = ConfigPath.View2InstableChennelsSettingFilePath;
                removeInstableInstance = new RemoveInstableChannelsOperator();
            }
            else
            {
                _view1FilePath = ConfigPath.View1BadChannelFlagsSettingFilePath;
                _view2FilePath = ConfigPath.View2BadChannelFlagsSettingFilePath;
                removeBadInstance = new RemoveBadChannelsOperator();
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesImage1Height, out _image1Height))
            {
                _image1Height = 64;
            }

            if (!ScannerConfig.Read(ConfigPath.ImagesImage2Height, out _image2Height))
            {
                _image2Height = 64;
            }
            ExchangeDirectionConfig.Service.GetView1ChannelsCount(out _channel1Count);
            ExchangeDirectionConfig.Service.GetView2ChannelsCount(out _channel2Count);

            LoadSetting();

            ViewSelectionChangedEventCommand = new RelayCommand(ViewSelectionChangedEventCommandExecute);
            ImageShowSelectionChangedEventCommand = new RelayCommand(ImageShowSelectionChangedEventCommandExecute);
            SelectionChangedCommand = new RelayCommand<ChannelBadFlag>(SelectedChangedEventCommandExecute);
            SaveBadChannelCommand = new RelayCommand(SaveBadChannelCommandExecute);

            ImageRecord = record;
        }



        void LoadSetting()
        {
            try
            {
                if (!CheckFile(_view1FilePath, _image1Height, ref _view1HorizonalPoints, 1))
                {
                    _view1HorizonalPoints = new List<ChannelBadFlag>();
                    //todo 和畸变挂钩后，此处新生成坏点，个数要和畸变相对应，
                    int bad1PointNum = _isRemoveEdge ? _image1Height : _channel1Count;
                    for (int i = 0; i < bad1PointNum; i++)
                    {
                        _view1HorizonalPoints.Add(new ChannelBadFlag(i, false));
                    }
                }

                if (!CheckFile(_view2FilePath, _image2Height, ref _view2HorizonalPoints, 2))
                {
                    _view2HorizonalPoints = new List<ChannelBadFlag>();
                    //todo 和畸变挂钩后，此处新生成坏点，个数要和畸变相对应，
                    int bad1PointNum = _isRemoveEdge ? _image2Height : _channel2Count;
                    for (int i = 0; i < bad1PointNum; i++)
                    {
                        _view2HorizonalPoints.Add(new ChannelBadFlag(i, false));
                    }
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }

        private bool CheckFile(string filePath, int imageHeight, ref List<ChannelBadFlag> horizonalPoints, int viewNum)
        {
            if ((DateTime.Now - _lastSaveTime) < TimeSpan.FromSeconds(2))
            {
                _lastSaveTime = DateTime.Now;
                return true;
            }
            if (!File.Exists(filePath))
            {
                return false;
            }
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 100000))
            {
                var formatter = new XmlSerializer(typeof(List<ChannelBadFlag>));
                horizonalPoints = formatter.Deserialize(stream) as List<ChannelBadFlag>;

                int bad1PointNum = _isRemoveEdge ? _image1Height : _channel1Count;
                int bad2PointNum = _isRemoveEdge ? _image2Height : _channel2Count;

                if (viewNum == 1)
                {
                    if (bad1PointNum != horizonalPoints.Count())
                    {
                        return false;
                    }
                }
                else
                {
                    if (bad2PointNum != horizonalPoints.Count())
                    {
                        return false;
                    }
                }

            }
            return true;
        }

        private void UpdateSetting()
        {
            if ((DateTime.Now - _lastSaveTime) < TimeSpan.FromSeconds(2))
            {
                _lastSaveTime = DateTime.Now;
                return;
            }
            Task.Run(() =>
            {
                try
                {
                    var dir = Path.GetDirectoryName(_view1FilePath);
                    if (!string.IsNullOrWhiteSpace(dir))
                    {
                        if (!Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }
                    }

                    try
                    {
                        if (_view1HorizonalPoints != null)
                        {
                            using (var stream = new FileStream(_view1FilePath, FileMode.Create, FileAccess.Write, FileShare.Read, 100000))
                            {
                                var formatter = new XmlSerializer(typeof(List<ChannelBadFlag>));
                                formatter.Serialize(stream, _view1HorizonalPoints);
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        Tracer.TraceException(exception);
                    }

                    try
                    {
                        if (_view2HorizonalPoints != null)
                        {
                            using (var stream = new FileStream(_view2FilePath, FileMode.Create, FileAccess.Write, FileShare.Read, 100000))
                            {
                                var formatter = new XmlSerializer(typeof(List<ChannelBadFlag>));
                                formatter.Serialize(stream, _view2HorizonalPoints);
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        Tracer.TraceException(exception);
                    }
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception);
                }
            });

        }

        private List<ChannelBadFlag> GetViewPoints(int view)
        {
            if (view == 1)
            {
                return _view1HorizonalPoints;
            }
            else if (view == 2)
            {
                return _view2HorizonalPoints;
            }
            else
            {
                return _view1HorizonalPoints;
            }
        }

        private void GetImageData()
        {
            if (ImageRecord != null)
            {
                var _xRayImageOriginal = XRayScanlinesImage.LoadFromDiskFile(ImageRecord.StorePath);

                ImageDateTime = DateFormatHelper.DateTime2String(ImageRecord.ScanTime);

                var viewCount = _xRayImageOriginal.ViewsCount;
                if (_isRemoveEdge)
                {
                    _ip.AttachImageData(_xRayImageOriginal.View1Data);
                    View1Bitmap = _ip.GetBitmap(ExportImageEffects.Inverse);
                    ShowBitmap = new Bitmap(View1Bitmap);

                    if (viewCount == 2)
                    {
                        _ip.AttachImageData(_xRayImageOriginal.View2Data);
                        View2Bitmap = _ip.GetBitmap(ExportImageEffects.Inverse);
                        DualViewSettingVisibility = Visibility.Visible;
                    }
                }
                else
                {
                    removeBadInstance.HistogramForUI(_xRayImageOriginal.View1Data);
                    _ip.AttachImageData(_xRayImageOriginal.View1Data);
                    View1Bitmap = _ip.GetBitmap(ExportImageEffects.SuperEnhance);
                    ShowBitmap = new Bitmap(View1Bitmap);

                    if (viewCount == 2)
                    {
                        removeBadInstance.HistogramForUI(_xRayImageOriginal.View2Data);
                        _ip.AttachImageData(_xRayImageOriginal.View2Data);
                        View2Bitmap = _ip.GetBitmap(ExportImageEffects.SuperEnhance);
                        DualViewSettingVisibility = Visibility.Visible;
                    }
                }


                BadChannelFlags = _view1HorizonalPoints;
                Messenger.Default.Send(BadChannelFlags);
            }
        }


        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool Delete0bject(IntPtr hObject);
        /// <summary>
        /// bitmap图像
        /// </summary>

        Bitmap view1Bitmap;
        public Bitmap View1Bitmap
        {
            get { return view1Bitmap; }
            set
            {
                if (view1Bitmap != null)
                {
                    view1Bitmap.Dispose();
                }
                view1Bitmap = value;
            }
        }
        Bitmap view2Bitmap;
        public Bitmap View2Bitmap
        {
            get { return view2Bitmap; }
            set
            {
                if (view2Bitmap != null)
                {
                    view2Bitmap.Dispose();
                }
                view2Bitmap = value;
            }
        }
        Bitmap view1OperBitmap;
        public Bitmap View1OperBitmap
        {
            get { return view1OperBitmap; }
            set
            {
                if (view1OperBitmap != null)
                {
                    view1OperBitmap.Dispose();
                }
                view1OperBitmap = value;
            }
        }
        Bitmap view2OperBitmap;
        public Bitmap View2OperBitmap
        {
            get { return view2OperBitmap; }
            set
            {
                if (view2OperBitmap != null)
                {
                    view2OperBitmap.Dispose();
                }
                view2OperBitmap = value;
            }
        }
        IntPtr ptr;
        private Bitmap _showbitmap;
        /// <summary>
        /// 用于显示的图像，和控件绑定
        /// </summary>
        public Bitmap ShowBitmap
        {
            get { return _showbitmap; }
            set
            {
                //if (_showbitmap != null)
                //{
                //    _showbitmap.Dispose();
                //}
                _showbitmap = value;
                if (_showbitmap != null)
                {
                    BitmapSource = GetBitmapSource(_showbitmap);
                    RaisePropertyChanged("BitmapSource");
                }

                base.RaisePropertyChanged();
            }
        }

        private BitmapSource GetBitmapSource(Bitmap showBitmap)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                showBitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;
                BitmapFrame frame = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                return frame;
            }
        }

        public BitmapSource BitmapSource { get; set; }


        private void Oper_View1()
        {
            var _xRayImageOriginal = XRayScanlinesImage.LoadFromDiskFile(ImageRecord.StorePath);
            if (_isRemoveEdge)
            {
                removeInstableInstance.RemoveInstableChennelForUI(_xRayImageOriginal.View1Data, _view1HorizonalPoints);

                _ip.AttachImageData(_xRayImageOriginal.View1Data);
                View1OperBitmap = _ip.GetBitmap(ExportImageEffects.Inverse);
            }
            else
            {
                removeBadInstance.RemoveBadChannelForUI(_xRayImageOriginal.View1Data, _view1HorizonalPoints);

                _ip.AttachImageData(_xRayImageOriginal.View1Data);
                View1OperBitmap = _ip.GetBitmap(ExportImageEffects.SuperEnhance);
            }
        }
        private void Oper_View2()
        {
            var _xRayImageOriginal = XRayScanlinesImage.LoadFromDiskFile(ImageRecord.StorePath);
            if (_isRemoveEdge)
            {
                removeInstableInstance.RemoveInstableChennelForUI(_xRayImageOriginal.View2Data, _view2HorizonalPoints);
                _ip.AttachImageData(_xRayImageOriginal.View2Data);
                View2OperBitmap = _ip.GetBitmap(ExportImageEffects.Inverse);
            }
            else
            {
                removeBadInstance.RemoveBadChannelForUI(_xRayImageOriginal.View2Data, _view2HorizonalPoints);
                _ip.AttachImageData(_xRayImageOriginal.View2Data);
                View2OperBitmap = _ip.GetBitmap(ExportImageEffects.SuperEnhance);
            }
        }

        /// <summary>
        /// 切换视角
        /// </summary>
        private void ViewSelectionChangedEventCommandExecute()
        {
            var tmplist = new List<ChannelBadFlag>();
            if (SelectedView.ViewNum == 1)
            {
                SelectedShowStyle = 0;
                ShowBitmap = View1Bitmap;
                BadChannelFlags = GetViewPoints(SelectedView.ViewNum);
                Messenger.Default.Send(tmplist);
            }
            else if (SelectedView.ViewNum == 2)
            {
                SelectedShowStyle = 0;
                ShowBitmap = View2Bitmap;
                BadChannelFlags = GetViewPoints(SelectedView.ViewNum);

                Messenger.Default.Send(tmplist);
            }
        }

        /// <summary>
        /// 图像模式改变
        /// </summary>
        private void ImageShowSelectionChangedEventCommandExecute()
        {
            //根据模式显示图像
            //1.显示图像和显示线 2.插值
            switch (SelectedShowStyle)
            {
                case 1:
                    if (SelectedView.ViewNum == 1)
                    {
                        ShowBitmap = View1Bitmap;
                    }
                    else if (SelectedView.ViewNum == 2)
                    {
                        ShowBitmap = View2Bitmap;
                    }
                    Messenger.Default.Send(BadChannelFlags);
                    break;
                case 2:
                    var tmplist = new List<ChannelBadFlag>();
                    Messenger.Default.Send(tmplist);
                    if (SelectedView.ViewNum == 1)
                    {
                        Oper_View1();
                        ShowBitmap = View1OperBitmap;
                    }
                    else if (SelectedView.ViewNum == 2)
                    {
                        Oper_View2();
                        ShowBitmap = View2OperBitmap;
                    }
                    break;
                case 0:
                    if (SelectedView.ViewNum == 1)
                    {
                        ShowBitmap = View1Bitmap;
                    }
                    else if (SelectedView.ViewNum == 2)
                    {
                        ShowBitmap = View2Bitmap;
                    }
                    tmplist = new List<ChannelBadFlag>();
                    Messenger.Default.Send(tmplist);
                    break;
            }
        }

        /// <summary>
        /// 双击改变坏点状态时触发
        /// </summary>
        private void SelectedChangedEventCommandExecute(ChannelBadFlag sender)
        {
            if (sender == null)
            {
                return;
            }
            var cbf = sender;
            cbf.IsBad = !cbf.IsBad;
            //如果取消选中，则把插值设为初始值
            if (!cbf.IsBad)
            {
                switch (SelectedView.ViewNum)
                {
                    case 1:
                        //todo 效果图
                        break;
                    case 2:
                        //todo 效果图
                        break;
                }
            }

            switch (SelectedShowStyle)
            {
                case 1:
                    if (SelectedView.ViewNum == 1)
                    {
                        ShowBitmap = View1Bitmap;
                    }
                    else if (SelectedView.ViewNum == 2)
                    {
                        ShowBitmap = View2Bitmap;
                    }
                    Messenger.Default.Send(BadChannelFlags);
                    break;
                case 2:
                    var tmplist = new List<ChannelBadFlag>();
                    Messenger.Default.Send(tmplist);
                    //处理拟合图像InterpolationImage
                    if (SelectedView.ViewNum == 1)
                    {
                        Oper_View1();
                        ShowBitmap = View1OperBitmap;
                    }
                    else if (SelectedView.ViewNum == 2)
                    {
                        Oper_View2();
                        ShowBitmap = View2OperBitmap;
                    }
                    break;
            }
        }

        private void SaveBadChannelCommandExecute()
        {
            var badPointStr = string.Empty;
            var badPoints = GetViewPoints(1)
                .Where(p => p.IsBad == true)
                .Select(p => p.ChannelNumber).ToArray();
            if (badPoints.Length > 0)
            {
                badPointStr = string.Join(",", badPoints).Remove(0, 1);
            }
            else
            {
                badPointStr = string.Empty;
            }

            new OperationRecordService().AddRecord(new OperationRecord()
            {
                AccountId = LoginAccountManager.Service.CurrentAccount != null ? LoginAccountManager.Service.CurrentAccount.AccountId : "",
                OperateUI = OperationUI.ImageBadChannel,
                OperateTime = DateTime.Now,
                OperateObject = "View1 Bad Points",
                OperateCommand = OperationCommand.Setting,
                OperateContent = badPointStr,
            });
            if (DualViewSettingVisibility == Visibility.Visible)
            {
                badPoints = GetViewPoints(2)
                .Where(p => p.IsBad == true)
                .Select(p => p.ChannelNumber).ToArray();
                if (badPoints.Length > 0)
                {
                    badPointStr = string.Join(",", badPoints).Remove(0, 1);
                }
                else
                {
                    badPointStr = string.Empty;
                }


                new OperationRecordService().AddRecord(new OperationRecord()
                {
                    AccountId = LoginAccountManager.Service.CurrentAccount != null ? LoginAccountManager.Service.CurrentAccount.AccountId : "",
                    OperateUI = OperationUI.ImageBadChannel,
                    OperateTime = DateTime.Now,
                    OperateObject = "View2 Bad Points",
                    OperateCommand = OperationCommand.Setting,
                    OperateContent = badPointStr,
                });
            }

            //保存数据
            UpdateSetting();
        }

        public void DisposeBitmap()
        {
            if (View1Bitmap != null)
                View1Bitmap.Dispose();
            if (View2Bitmap != null)
                View2Bitmap.Dispose();
            if (View1OperBitmap != null)
                View1OperBitmap.Dispose();
            if (View2OperBitmap != null)
                View2OperBitmap.Dispose();
            if (ShowBitmap != null)
                ShowBitmap.Dispose();
            this.Cleanup();
            GC.Collect();

        }
    }
}
