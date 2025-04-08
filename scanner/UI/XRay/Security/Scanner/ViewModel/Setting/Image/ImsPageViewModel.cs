using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Services;
using UI.XRay.Flows.Services.Db;
using UI.XRay.Flows.TRSNetwork.Models;
using UI.XRay.Gui.Framework;
using UI.XRay.Security.Scanner.MetroDialogs;
using UI.XRay.Security.Scanner.Printing;
using UI.XRay.Security.Scanner.SettingViews.Image.Pages;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Image
{
    public class ImsPageViewModel : PageViewModelBase
    {
        private ObservableCollection<BindableImage> _currentImages;

        /// <summary>
        /// 当前显示的所有图像
        /// </summary>
        public ObservableCollection<BindableImage> CurrentImages
        {
            get { return _currentImages; }
            set
            {
                _currentImages = value;
                RaisePropertyChanged();
            }
        }

        private int _showingImageMinIndex = 0;

        /// <summary>
        /// 当前显示的图像，在整个检索到的图像中的索引中的最小编号，起始编号为1
        /// </summary>
        public int ShowingImageMinIndex
        {
            get { return _showingImageMinIndex; }
            set { _showingImageMinIndex = value; RaisePropertyChanged(); }
        }

        private int _showingImageMaxIndex = 0;

        /// <summary>
        /// 当前显示的图像，在整个检索到的图像中的索引中的最大编号，最大值为所有检索到的图像之和
        /// </summary>
        public int ShowingImageMaxIndex
        {
            get { return _showingImageMaxIndex; }
            set
            {
                _showingImageMaxIndex = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 当前图像检索记录总数
        /// </summary>
        private int _totalImagesCount;

        public int TotalImagesCount
        {
            get { return _totalImagesCount; }
            set
            {
                _totalImagesCount = value;
                RaisePropertyChanged();
            }
        }

        private bool _isDeleteButtonEnabled;

        private bool _isOpenButtonEnabled;


        public bool IsDeleteButtonEnabled
        {
            get { return _isDeleteButtonEnabled && IsButtonsEnabled; }
            set { _isDeleteButtonEnabled = value; RaisePropertyChanged(); }
        }

        public bool IsOpenButtonEnabled
        {
            get { return _isOpenButtonEnabled && IsButtonsEnabled; }
            set
            {
                _isOpenButtonEnabled = value;
                RaisePropertyChanged();
            }
        }


        private bool _isPlaybackButtonEnabled;

        public bool IsPlaybackButtonEnabled
        {
            get { return _isPlaybackButtonEnabled && IsButtonsEnabled; }
            set
            {
                _isPlaybackButtonEnabled = value;
                RaisePropertyChanged();
            }
        }

        private bool _isDumpButtonEnabled;

        public bool IsDumpButtonEnabled
        {
            get { return _isDumpButtonEnabled && IsButtonsEnabled; }
            set
            {
                _isDumpButtonEnabled = value;
                RaisePropertyChanged();
            }
        }

        private bool _isButtonsEnabled = true;
        public bool IsButtonsEnabled
        {
            get { return _isButtonsEnabled; }
            set
            {
                _isButtonsEnabled = value;
                RaisePropertyChanged("IsButtonsEnabled");
                RaisePropertyChanged("IsDeleteButtonEnabled");
                RaisePropertyChanged("IsOpenButtonEnabled");
                RaisePropertyChanged("IsPlaybackButtonEnabled");
            }
        }

        private Visibility _canDumpToNetVisibility = Visibility.Visible;

        public Visibility CanDumpToNetVisibility
        {
            get { return _canDumpToNetVisibility; }
            set { _canDumpToNetVisibility = value; RaisePropertyChanged(); }
        }

        private Visibility _canRemoveBadChannelVisibility;

        public Visibility CanRemoveBadChannelVisibility
        {
            get
            {
                if (LoginAccountManager.Service.IsCurrentRoleEqualOrGreaterThan(AccountRole.Maintainer))
                    _canRemoveBadChannelVisibility = Visibility.Visible;
                else
                    _canRemoveBadChannelVisibility = Visibility.Collapsed;
                return _canRemoveBadChannelVisibility;
            }
            set
            {
                _canRemoveBadChannelVisibility = value;
                RaisePropertyChanged();
            }
        }

        public DateTime _lastChangeImagePage;

        public bool CanChangeImagePage
        {
            get
            {
                return DateTime.Now - _lastChangeImagePage > TimeSpan.FromSeconds(0.5);
            }
        }

        public ObservableCollection<DetectViewSelection> ViewIndexs { get; set; }
        public int SelectViewIndex { get; set; }
        #region ProcessBar
        private Visibility _processBarVisibility = Visibility.Collapsed;
        public Visibility ProcessBarVisibility
        {
            get { return _processBarVisibility; }
            set { _processBarVisibility = value; RaisePropertyChanged(); }
        }

        private int _currentProcessNum;
        public int CurrentProcessNum
        {
            get { return _currentProcessNum; }
            set { _currentProcessNum = value; RaisePropertyChanged(); }
        }

        private int _totalProcessNum;
        public int TotalProcessNum
        {
            get { return _totalProcessNum; }
            set { _totalProcessNum = value; RaisePropertyChanged(); }
        }

        private int _percentDone;

        public int PercentDone
        {
            get { return _percentDone; }
            set { _percentDone = value; RaisePropertyChanged(); }
        }

        private bool _processBarCanceled;

        public RelayCommand CancelProcessBarCommand { get; set; }
        #endregion

        public RelayCommand RetrieveImagesCommand { get; set; }

        public RelayCommand SelectAllCommand { get; set; }

        public RelayCommand AntiSelectAllCommand { get; set; }

        public RelayCommand ReplayCommand { get; set; }

        public RelayCommand OpenCommand { get; set; }

        public RelayCommand OpenRemoveBadChannelCommand { get; set; }

        public RelayCommand DeleteCommand { get; set; }

        public RelayCommand PrintCommand { get; set; }

        public RelayCommand DumpCommand { get; set; }

        public RelayCommand DumpToNetCommand { set; get; }

        public RelayCommand LockCommand { get; set; }

        public RelayCommand UnlockCommand { get; set; }

        public RelayCommand MoveToNextPageCommand { get; set; }

        public RelayCommand MoveToPreviousCommand { get; set; }

        public RelayCommand ToListViewCommand { get; set; }

        /// <summary>探测器错茬调整命令</summary>
        public RelayCommand DetectorModuleAdjustCommand { get; set; }

        //   public RelayCommand<MouseButtonEventArgs> ImageListBox_DoubleOnClickCommand { get; set; }

        public RelayCommand<SelectionChangedEventArgs> SelectionChangedEventCommand { get; private set; }

        /// <summary>
        /// 当前的图像检索条件
        /// </summary>
        private ImageRetrievalConditions _conditions;

        /// <summary>
        /// 当前的图像检索条件
        /// </summary>
        public ImageRetrievalConditions Conditions
        {
            get { return _conditions; }
            set
            {
                _conditions = value;
                RaisePropertyChanged();
            }
        }

        private ImageRecordDbSet _recordDbSet;
        protected List<ImageRecord> _resultRecords1;

        /// <summary>
        /// 选中的图像导出格式索引：0为bmp，1为原始数据
        /// </summary>
        private int _selectedDumpFormatIndex = 0;

        /// <summary>
        /// 选中的图像导出格式
        /// </summary>
        public int SelectedDumpFormat
        {
            get { return _selectedDumpFormatIndex; }
            set { _selectedDumpFormatIndex = value; RaisePropertyChanged(); }
        }

        private Visibility _detectorModuleAdjustVisibility;
        public Visibility DetectorModuleAdjustVisibility { get { return _detectorModuleAdjustVisibility; } set { _detectorModuleAdjustVisibility = value; RaisePropertyChanged(); } }

        private ImageRetrievalController _controller;

        #region 原构造函数
        /// <summary>
        /// 原构造函数
        /// </summary>
        //public ImsPageViewModel(int a = 0)
        //{
        //    RetrieveImagesCommand = new RelayCommand(RetrieveImagesCommandExecute);
        //    SelectAllCommand = new RelayCommand(SelectAllCommandExecute);
        //    AntiSelectAllCommand = new RelayCommand(AntiSelectAllCommandExecute);
        //    ReplayCommand = new RelayCommand(ReplayCommandExecute);
        //    OpenCommand = new RelayCommand(OpenCommandExecute);
        //    OpenRemoveBadChannelCommand = new RelayCommand(OpenRemoveBadChannelCommandExecute);
        //    DeleteCommand = new RelayCommand(DeleteCommandExecute);
        //    DumpCommand = new RelayCommand(DumpCommandExecute);
        //    PrintCommand = new RelayCommand(PrintCommandExecute);
        //    MoveToPreviousCommand = new RelayCommand(MoveToPreviousPageCommandExecute);
        //    MoveToNextPageCommand = new RelayCommand(MoveToNextPageCommmandExecute);
        //    SelectionChangedEventCommand = new RelayCommand<SelectionChangedEventArgs>(SelectionChangedEventCommandExecute);
        //    LockCommand = new RelayCommand(LockCommandExecute);
        //    UnlockCommand = new RelayCommand(UnlockCommandExecute);
        //    DumpToNetCommand = new RelayCommand(DumpToNetCommandExcute);
        //    //    ImageListBox_DoubleOnClickCommand = new RelayCommand<MouseButtonEventArgs>(ImageListBox_DoubleOnClick);
        //    ToListViewCommand = new RelayCommand(ToListViewCommandExe);
        //    CancelProcessBarCommand = new RelayCommand(CancelProcessBarCommandExe);

        //    CurrentImages = new ObservableCollection<BindableImage>();

        //    ShowingImageMinIndex = 1;
        //    ShowingImageMaxIndex = CurrentImages.Count;



        //    UpdateDeleteButtonState();

        //    if (IsInDesignMode)
        //    {
        //        var imgUri = new Uri("../../../Icons/Image.png", UriKind.Relative);
        //        CurrentImages.Add(new BindableImage() { ImageGuid = "12", Thumbnail = new BitmapImage(imgUri), ScanTime = DateTime.Now, IsLocked = true });
        //        CurrentImages.Add(new BindableImage() { ImageGuid = "34", Thumbnail = new BitmapImage(imgUri) });
        //        CurrentImages.Add(new BindableImage() { ImageGuid = "12", Thumbnail = new BitmapImage(imgUri), ScanTime = DateTime.Now });
        //        CurrentImages.Add(new BindableImage() { ImageGuid = "34", Thumbnail = new BitmapImage(imgUri) });
        //        CurrentImages.Add(new BindableImage() { ImageGuid = "12", Thumbnail = new BitmapImage(imgUri), ScanTime = DateTime.Now });
        //        CurrentImages.Add(new BindableImage() { ImageGuid = "34", Thumbnail = new BitmapImage(imgUri) });
        //        CurrentImages.Add(new BindableImage() { ImageGuid = "12", Thumbnail = new BitmapImage(imgUri), ScanTime = DateTime.Now });
        //        CurrentImages.Add(new BindableImage() { ImageGuid = "34", Thumbnail = new BitmapImage(imgUri), IsLocked = true });
        //    }
        //    else
        //    {
        //        try
        //        {
        //            _controller = new ImageRetrievalController(20);
        //            TotalImagesCount = _controller.ReLoadRecords();
        //            //TotalImagesCount = _controller.GetTableTotalCount();
        //            CurrentImages = _controller.MoveToFirstPage();
        //            UpdateIndices();
        //        }
        //        catch (Exception exception)
        //        {
        //            Tracer.TraceException(exception);
        //        }
        //    }
        //    _lastChangeImagePage = DateTime.Now;

        //    _resultRecords1 = null;

        //    _recordDbSet=new ImageRecordDbSet();

        //    Task.Run(() =>
        //    {
        //        _resultRecords1 = _recordDbSet.TakeByConditions(new DateTime(2016, 1, 1), DateTime.Now, LoginAccountManager.Service.AllAccountId, false, false);

        //        TotalImagesCount = _resultRecords1.Count;
        //        _controller._resultRecords = _resultRecords1;

        //    });
        //}
        #endregion

        /// <summary>
        /// 新构造函数
        /// </summary>
        /// <update>姜毅改</update>
        public ImsPageViewModel()
        {
            RetrieveImagesCommand = new RelayCommand(RetrieveImagesCommandExecute1);
            SelectAllCommand = new RelayCommand(SelectAllCommandExecute);
            AntiSelectAllCommand = new RelayCommand(AntiSelectAllCommandExecute);
            ReplayCommand = new RelayCommand(ReplayCommandExecute);
            OpenCommand = new RelayCommand(OpenCommandExecute);
            OpenRemoveBadChannelCommand = new RelayCommand(OpenRemoveBadChannelCommandExecute);
            DeleteCommand = new RelayCommand(DeleteCommandExecute);
            DumpCommand = new RelayCommand(DumpCommandExecute);
            PrintCommand = new RelayCommand(PrintCommandExecute);
            MoveToPreviousCommand = new RelayCommand(MoveToPreviousPageCommandExecute1);
            MoveToNextPageCommand = new RelayCommand(MoveToNextPageCommmandExecute1);
            SelectionChangedEventCommand = new RelayCommand<SelectionChangedEventArgs>(SelectionChangedEventCommandExecute);
            LockCommand = new RelayCommand(LockCommandExecute);
            UnlockCommand = new RelayCommand(UnlockCommandExecute);
            DumpToNetCommand = new RelayCommand(DumpToNetCommandExcute);
            //    ImageListBox_DoubleOnClickCommand = new RelayCommand<MouseButtonEventArgs>(ImageListBox_DoubleOnClick);
            ToListViewCommand = new RelayCommand(ToListViewCommandExe);
            CancelProcessBarCommand = new RelayCommand(CancelProcessBarCommandExe);
            DetectorModuleAdjustCommand = new RelayCommand(DetectorModuleAdjustExcute);
            CurrentImages = new ObservableCollection<BindableImage>();
            int viewCount = 1;
            if (!ScannerConfig.Read(ConfigPath.MachineViewsCount, out viewCount))
            {
                viewCount = 1;
            }
            ViewIndexs = new ObservableCollection<DetectViewSelection>();
            for (int i = 0; i < viewCount; i++)
            {
                ViewIndexs.Add(new DetectViewSelection(i, $"视角{i + 1}"));
            }
            SelectViewIndex = 0;
            ShowingImageMinIndex = 1;
            ShowingImageMaxIndex = CurrentImages.Count;

            bool isEnabled;
            ScannerConfig.Read(ConfigPath.IsNetDumpToOperator, out isEnabled);
            if (LoginAccountManager.Service.CurrentAccount.Role == AccountRole.Operator)
            {
                if (isEnabled)
                {
                    CanDumpToNetVisibility = Visibility.Visible;
                    DetectorModuleAdjustVisibility = Visibility.Visible;
                }
                else
                {
                    CanDumpToNetVisibility = Visibility.Collapsed;
                    DetectorModuleAdjustVisibility = Visibility.Collapsed;
                }
            }


            UpdateDeleteButtonState();

            if (IsInDesignMode)
            {
                var imgUri = new Uri("../../../Icons/Image.png", UriKind.Relative);
                CurrentImages.Add(new BindableImage() { ImageGuid = "12", Thumbnail = new BitmapImage(imgUri), ScanTime = DateTime.Now, IsLocked = true });
                CurrentImages.Add(new BindableImage() { ImageGuid = "34", Thumbnail = new BitmapImage(imgUri) });
                CurrentImages.Add(new BindableImage() { ImageGuid = "12", Thumbnail = new BitmapImage(imgUri), ScanTime = DateTime.Now });
                CurrentImages.Add(new BindableImage() { ImageGuid = "34", Thumbnail = new BitmapImage(imgUri) });
                CurrentImages.Add(new BindableImage() { ImageGuid = "12", Thumbnail = new BitmapImage(imgUri), ScanTime = DateTime.Now });
                CurrentImages.Add(new BindableImage() { ImageGuid = "34", Thumbnail = new BitmapImage(imgUri) });
                CurrentImages.Add(new BindableImage() { ImageGuid = "12", Thumbnail = new BitmapImage(imgUri), ScanTime = DateTime.Now });
                CurrentImages.Add(new BindableImage() { ImageGuid = "34", Thumbnail = new BitmapImage(imgUri), IsLocked = true });
            }
            else
            {
                try
                {
                    _controller = new ImageRetrievalController(20);
                    _controller.ReLoadRecords1();
                    _totalImagesCount = _controller.GetTableTotalCount1();
                    //TotalImagesCount = _controller.GetTableTotalCount();
                    CurrentImages = _controller.MoveToFirstPage();
                    UpdateIndices();
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception);
                }
            }
            _lastChangeImagePage = DateTime.Now;
            processor = new XRayImageProcessor();
            //_resultRecords1 = null;

            //_recordDbSet = new ImageRecordDbSet();

            //Task.Run(() =>
            //{
            //    _resultRecords1 = _recordDbSet.TakeByConditions(new DateTime(2016, 1, 1), DateTime.Now, LoginAccountManager.Service.AllAccountId, false, false);

            //    _controller._resultRecords = _resultRecords1;

            //});
        }

        private void DetectorModuleAdjustExcute()
        {
            if (CurrentImages == null || CurrentImages.Count <= 0)
                return;
            ImageRecord imageRecord = CurrentImages.Where(it => it.IsSelected).Select(it => it.Record).FirstOrDefault();
            if (imageRecord == null)
                return;
            var window = new DetectorModuleAdjustWindow();
            var viewModel = new DetectorModuleAdjustViewModel(imageRecord.StorePath);
            viewModel.UpdateViewOffsetRegion();
            window.DataContext = viewModel;
            window.ShowDialog();
        }

        XRayImageProcessor processor;
        private void ToListViewCommandExe()
        {
            MessengerInstance.Send(new NotificationMessage(this, "ImsListPage"));
        }


        /// <summary>
        /// 当前是否有选中某个图像
        /// </summary>
        private bool HasSelectedImage
        {
            get
            {
                if (CurrentImages != null)
                {
                    if (CurrentImages.Any(image => image.IsSelected))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// 锁定当前选中的图像
        /// </summary>
        private void LockCommandExecute()
        {
            if (CurrentImages != null)
            {
                foreach (var image in CurrentImages.Where(image => image.IsSelected == true))
                {
                    image.IsLocked = true;
                    new OperationRecordService().RecordOperation(OperationUI.ImsPage, Path.GetFileName(image.ImagePath), OperationCommand.Lock, string.Empty);
                }

                List<ImageRecord> selected = CurrentImages.Where(image => image.IsSelected == true).Select(image => image.Record).ToList();
                if (selected.Count > 0)
                {
                    // 锁定所有选中图像
                    _controller.Update(selected);
                }
            }
        }

        /// <summary>
        /// 对当前选中图像解除锁定
        /// </summary>
        private void UnlockCommandExecute()
        {
            if (CurrentImages != null)
            {
                foreach (var image in CurrentImages.Where(image => image.IsSelected == true))
                {
                    image.IsLocked = false;
                    new OperationRecordService().RecordOperation(OperationUI.ImsPage, Path.GetFileName(image.ImagePath), OperationCommand.Unlock, string.Empty);
                }

                List<ImageRecord> selected = CurrentImages.Where(image => image.IsSelected == true).Select(image => image.Record).ToList();
                if (selected.Count > 0)
                {
                    // 锁定所有选中图像
                    _controller.Update(selected);
                }
            }
        }

        /// <summary>
        /// 当图像列表的选择发生变化时，更新几个按钮的使能状态
        /// </summary>
        /// <param name="args"></param>
        private void SelectionChangedEventCommandExecute(SelectionChangedEventArgs args)
        {
            IsOpenButtonEnabled = HasSelectedImage;
            IsPlaybackButtonEnabled = HasSelectedImage;
            IsDumpButtonEnabled = HasSelectedImage;
            UpdateDeleteButtonState();
        }

        private void UpdateDeleteButtonState()
        {
            if (LoginAccountManager.Service.CurrentAccount != null)
            {
                //if (LoginAccountManager.CurrentAccount.Role == AccountRole.Operator)
                //{
                //    IsDeleteButtonEnabled = false;
                //}
                //else
                {
                    IsDeleteButtonEnabled = HasSelectedImage;
                }
            }
            else
            {
                IsDeleteButtonEnabled = false;
            }
        }

        private void RetrieveImagesCommandExecute()
        {
            var msg = new ShowImageRetrievalWindowAction("SettingWindow", conditions =>
            {
                try
                {
                    Conditions = conditions;
                    if (conditions != null)
                    {
                        _controller.ResetConditions(conditions);
                        TotalImagesCount = _controller.ReLoadRecords();
                        CurrentImages = _controller.MoveToFirstPage();
                    }
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception);
                }
            });

            // 在显示图像检索窗口之前，先缓存当前的检索条件，这样在图像检索窗口显示时，将初始化为当前的检索条件
            ViewModelLocator.Locator.Cache["ImageRetrievalConditions"] = Conditions;
            Messenger.Default.Send(msg);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <update>姜毅改</update>
        private void RetrieveImagesCommandExecute1()
        {
            var msg = new ShowImageRetrievalWindowAction("SettingWindow", conditions =>
            {
                try
                {
                    Conditions = conditions;
                    if (conditions != null)
                    {
                        bool isChanged = _controller.ResetConditions1(conditions);
                        if (!isChanged)
                        {
                            ShowingImageMinIndex = 1;
                            _controller.ShowingMinIndex = 0;
                        }
                        _controller.ReLoadRecords1();
                        TotalImagesCount = _controller.GetTableTotalCount1();
                        CurrentImages = _controller.MoveToFirstPage1();
                        ShowingImageMaxIndex = _controller.ShowingMaxIndex + 1;
                    }
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception);
                }
            });

            // 在显示图像检索窗口之前，先缓存当前的检索条件，这样在图像检索窗口显示时，将初始化为当前的检索条件
            ViewModelLocator.Locator.Cache["ImageRetrievalConditions"] = Conditions;
            Messenger.Default.Send(msg);
        }

        private void PrintCommandExecute()
        {
            if (CurrentImages != null)
            {
                var images = CurrentImages.Where(image => image.IsSelected);

                foreach (var image in images)
                {
                    try
                    {
                        var fileName = Path.GetFileName(image.ImagePath);
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            var ip = new XRayImageProcessor();
                            var img = XRayScanlinesImage.LoadFromDiskFile(image.ImagePath);
                            ip.AttachImageData(SelectViewIndex == 0 ? img.View1Data : img.View2Data);
                            var bmp = ip.GetBitmap();
                            if (bmp != null)
                            {
                                ImagePrinter.PrintBitmapAsync(bmp, image.Record.MachineNumber, image.ScanTime, image.Record.AccountId);
                                new OperationRecordService().RecordOperation(OperationUI.ImsPage, Path.GetFileName(image.ImagePath), OperationCommand.Print, string.Empty);
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        Tracer.TraceException(exception, "Failed to print image file: " + image.ImagePath);
                    }
                }
            }
        }

        private void DumpCommandExecute()
        {
            WindowFocusHelper.Pause();
            IsButtonsEnabled = false;
            var msg = new ShowFolderBrowserDialogMessageAction("SettingWindow", s =>
            {
                if (!string.IsNullOrWhiteSpace(s))
                {
                    ExportSelectedImagesToFolder(s);
                }
                else
                {
                    IsButtonsEnabled = true;
                }
                WindowFocusHelper.Continue();
            });

            MessengerInstance.Send(msg);
        }

        private void CancelProcessBarCommandExe()
        {
            _processBarCanceled = true;
        }

        private async void DumpToNetCommandExcute()
        {
            await Task.Run(() =>
            {
                IsButtonsEnabled = false;
                if (CurrentImages != null)
                {
                    var srcImageFilePath = CurrentImages.Where(image => image.IsSelected).Select(image => image.ImagePath);
                    int totalCount = srcImageFilePath.Count();
                    int currentNum = 1;

                    if (SystemStatus.Instance.IsFTPServerConnected)
                    {
                        foreach (var path in srcImageFilePath)
                        {
                            if (!UpdateProcessBar(totalCount, currentNum++))
                            {
                                break;
                            }

                            try
                            {
                                var fileName = Path.GetFileName(path);
                                string destPath = System.Environment.CurrentDirectory + "\\Recv\\";
                                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
                                if (!string.IsNullOrEmpty(fileName))
                                {
                                    if (SelectedDumpFormat == 4)
                                    {
                                        var rst = HttpNetworkController.Controller.SendFileTo(fileName, path, FileType.Image);
                                        if (rst)
                                        {
                                            new OperationRecordService().RecordOperation(OperationUI.ImsPage, fileName, OperationCommand.Saveto, "DumpToNet");
                                        }
                                    }
                                    else
                                    {
                                        // 导出图像
                                        Save4ImagesService.Service.SaveAllImageToNetwork(path, destPath, SelectedDumpFormat);
                                    }
                                }
                            }
                            catch (Exception exception)
                            {
                                Tracer.TraceError($"[Image] Error occured in DumpToNetCommandExcute, path: {path}");
                                Tracer.TraceException(exception);
                            }
                        }

                        Tracer.TraceInfo("[Image] Dump to network completely");
                        Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            MessengerInstance.Send(new ShowDialogMessageAction("SettingWindow",
                                "", TranslationService.FindTranslation("Dump to network completely"), MetroDialogButtons.Ok,
                                result =>
                                {

                                }));
                        });
                    }
                    else
                    {
                        Tracer.TraceInfo("[ImageExportToNet] FTP server not connected");
                        Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            MessengerInstance.Send(new ShowDialogMessageAction("SettingWindow",
                                "", TranslationService.FindTranslation("Send Failed"), MetroDialogButtons.Ok,
                                result =>
                                {
                                }));
                        });
                    }
                }
                else
                {
                    Tracer.TraceInfo("[ImageExportToNet] CurrentImages == null");
                }
                IsButtonsEnabled = true;
            });
        }

        /// <summary>
        /// 将当前选中的图像导出到指定的文件夹中
        /// </summary>
        /// <param name="folderPath">文件夹路径</param>
        private async void ExportSelectedImagesToFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                return;

            if (CurrentImages != null)
            {
                var srcImageFilePath = CurrentImages.Where(image => image.IsSelected).Select(image => image.ImagePath);

                await Task.Run(() =>
                {
                    var destPath = folderPath;
                    try
                    {
                        destPath = Path.Combine(folderPath, DateTime.Now.ToString(@"yyyyMMdd-HHmmss") + "_Export");
                        Directory.CreateDirectory(destPath);
                    }
                    catch (Exception exception)
                    {
                        Tracer.TraceException(exception, "Failed to create directory for dumping images.");
                    }

                    int totalNum = srcImageFilePath.Count();
                    int currentNum = 1;

                    foreach (var path in srcImageFilePath)
                    {
                        if (!UpdateProcessBar(totalNum, currentNum++))
                        {
                            break;
                        }

                        try
                        {
                            var fileName = Path.GetFileName(path);
                            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
                            if (!string.IsNullOrEmpty(fileName))
                            {
                                var ext = ".xray";
                                if (SelectedDumpFormat == 4)
                                {
                                    // 导出原始数据
                                    File.Copy(path, Path.Combine(destPath, fileName), true);
                                    new OperationRecordService().RecordOperation(OperationUI.ImsPage, fileNameWithoutExtension + ".xray",
                                        OperationCommand.Saveto, ConfigHelper.AddQuotationForPath(Path.Combine(destPath, fileName)));
                                }
                                else
                                {
                                    // 导出图像
                                    Save4ImagesService.Service.SaveAllImageToDisk(null, path, destPath, SelectedDumpFormat);
                                }
                            }
                        }
                        catch (Exception exception)
                        {
                            Tracer.TraceException(exception, "Failed to copy file to dest folder: " + path);
                        }
                    }

                    IsButtonsEnabled = true;

                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessengerInstance.Send(new ShowDialogMessageAction("SettingWindow",
                            "", TranslationService.FindTranslation("Images dump completed"), MetroDialogButtons.Ok,
                            result =>
                            {

                            }));
                    });
                });
            }
        }

        private bool UpdateProcessBar(int totalNum, int currentNum)
        {
            if (totalNum > 0 && !_processBarCanceled)
            {
                TotalProcessNum = totalNum;
                CurrentProcessNum = currentNum;
                if (currentNum <= 1)
                {
                    ProcessBarVisibility = Visibility.Visible;
                }

                if (currentNum == totalNum)
                {
                    ProcessBarVisibility = Visibility.Collapsed;
                }

                PercentDone = currentNum * 100 / totalNum;
                return true;
            }

            ProcessBarVisibility = Visibility.Collapsed;
            _processBarCanceled = false;
            return false;
        }

        private void DeleteCommandExecute()
        {
            if (CurrentImages != null && IsDeleteButtonEnabled)
            {
                var message = new ShowDialogMessageAction("SettingWindow", TranslationService.FindTranslation("Warning"),
                    TranslationService.FindTranslation("Delete the selected images permanently?"), MetroDialogButtons.OkCancel,
                    result =>
                    {
                        if (result == MetroDialogResult.Ok)
                        {
                            DeleteSelectedImages();
                        }
                    });

                this.MessengerInstance.Send(message);
            }
        }

        private void DeleteSelectedImages()
        {
            bool hasLockedImage = false;
            var selectedRecords = new List<ImageRecord>(10);
            var selectedImages = new List<BindableImage>(10);

            foreach (var image in CurrentImages)
            {
                if (image.IsSelected)
                {
                    if (!image.IsLocked)
                    {
                        selectedRecords.Add(image.Record);
                        selectedImages.Add(image);

                        new OperationRecordService().RecordOperation(OperationUI.ImsPage, Path.GetFileName(image.Record.StorePath), OperationCommand.Delete, string.Empty);
                    }
                    else
                    {
                        hasLockedImage = true;
                    }
                }
            }

            // 从数据库及磁盘中删除
            _controller.RemoveRecords(selectedRecords);

            // 从缓存及视图中删除
            foreach (var image in selectedImages)
            {
                CurrentImages.Remove(image);
            }

            // 更新当前页信息
            TotalImagesCount = _controller.ResultRecordsCount;
            CurrentImages = _controller.UpdateCurrentPage();
            UpdateIndices();

            if (hasLockedImage)
            {
                var message = new ShowDialogMessageAction("SettingWindow", TranslationService.FindTranslation("Warning"),
                    TranslationService.FindTranslation("Locked image can't be deleted"), MetroDialogButtons.Ok, result =>
                    {

                    });
                this.MessengerInstance.Send(message);
            }
        }

        /// <summary>
        /// 在当前窗口中打开查看图像
        /// </summary>
        private void OpenCommandExecute()
        {
            if (CurrentImages != null)
            {
                foreach (var image in CurrentImages)
                {
                    // 打开第一个选中的图像
                    if (image.IsSelected)
                    {
                        new OperationRecordService().RecordOperation(OperationUI.ImsPage, Path.GetFileName(image.ImagePath), OperationCommand.Open, string.Empty);
                        var bmpSource = _controller.GenerateImage(image.Record);
                        if (bmpSource != null)
                            Messenger.Default.Send(new ShowXRayImageMessage(image.Record, bmpSource));
                    }
                }
            }
        }

        /// <summary>
        /// 在当前窗口中打开坏点手动剔除界面
        /// </summary>
        private void OpenRemoveBadChannelCommandExecute()
        {

            var img = CurrentImages.FirstOrDefault(a => a.IsSelected);
            if (img != null)
            {
                ImageBadChannelManual win = new ImageBadChannelManual();
                ImageBadChannelManualViewModel vm = new ImageBadChannelManualViewModel(img.Record);
                win.DataContext = vm;
                win.ShowDialog();
            }

            //if (CurrentImages != null)
            //{
            //    foreach (var image in CurrentImages)
            //    {
            //        // 打开第一个选中的图像
            //        if (image.IsSelected)
            //        {
            //            //var bmpSource = _controller.GenerateImage(image.Record,processor);
            //            //if (bmpSource != null)
            //            Messenger.Default.Send(new ShowBadChannelImageMessage(image.Record));
            //        }
            //    }
            //}
        }

        private void ReplayCommandExecute()
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (CurrentImages != null)
                {
                    List<ImageRecord> selected = CurrentImages.Where(image => image.IsSelected == true).Select(image => image.Record).ToList();
                    if (selected.Count > 0)
                    {
                        Task.Run(() =>
                        {
                            foreach (var image in selected)
                            {
                                new OperationRecordService().RecordOperation(OperationUI.ImsPage, Path.GetFileName(image.StorePath), OperationCommand.Relay, string.Empty);
                            }
                        });


                        // 先关闭设置窗口
                        Messenger.Default.Send(new CloseWindowMessage("SettingWindow"));

                        // 发送消息，启动图像回放
                        Messenger.Default.Send(new PlaybackImageRecordsMessage(selected));
                    }
                }
            });

        }

        private void SelectAllCommandExecute()
        {
            if (CurrentImages != null)
            {
                foreach (var image in CurrentImages)
                {
                    image.IsSelected = true;
                }
            }
        }

        private void AntiSelectAllCommandExecute()
        {
            if (CurrentImages != null)
            {
                foreach (var image in CurrentImages)
                {
                    image.IsSelected = !image.IsSelected;
                }
            }
        }

        /// <summary>
        /// 原下一页方法
        /// </summary>
        private void MoveToNextPageCommmandExecute()
        {
            if (!CanChangeImagePage)
            {
                return;
            }
            try
            {
                var images = _controller.MoveToNextPage();
                if (images != null && images.Count > 0)
                {
                    CurrentImages = images;
                    UpdateIndices();
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
            _lastChangeImagePage = DateTime.Now;
        }

        /// <summary>
        /// 新下一页方法
        /// </summary>
        /// <update>姜毅改</update>
        private void MoveToNextPageCommmandExecute1()
        {

            if (!CanChangeImagePage)
            {
                return;
            }
            try
            {
                var images = _controller.MoveToNextPage1();
                if (images != null && images.Count > 0)
                {
                    CurrentImages = images;
                    UpdateIndices();
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
            _lastChangeImagePage = DateTime.Now;
        }

        /// <summary>
        /// 原上一页方法
        /// </summary>
        private void MoveToPreviousPageCommandExecute()
        {
            if (!CanChangeImagePage)
            {
                return;
            }
            try
            {
                var images = _controller.MoveToPreviousPage();
                if (images != null && images.Count > 0)
                {
                    CurrentImages = images;
                    UpdateIndices();
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
            _lastChangeImagePage = DateTime.Now;
        }

        /// <summary>
        /// 新上一页方法
        /// </summary>
        private void MoveToPreviousPageCommandExecute1()
        {
            if (!CanChangeImagePage)
            {
                return;
            }
            try
            {
                var images = _controller.MoveToPreviousPage1();
                if (images != null && images.Count > 0)
                {
                    CurrentImages = images;
                    UpdateIndices();
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
            _lastChangeImagePage = DateTime.Now;
        }

        private void UpdateIndices()
        {
            ShowingImageMinIndex = _controller.ShowingMinIndex + 1;
            ShowingImageMaxIndex = _controller.ShowingMaxIndex + 1;
        }

        public override void OnKeyDown(System.Windows.Input.KeyEventArgs args)
        {
            switch (args.Key)
            {
                case Key.D1:
                    SelectAllCommandExecute();

                    args.Handled = true;
                    break;

                case Key.D2:
                    AntiSelectAllCommandExecute();
                    args.Handled = true;
                    break;

                case Key.D3:
                    if (IsPlaybackButtonEnabled)
                    {
                        ReplayCommandExecute();
                    }
                    args.Handled = true;
                    break;

                case Key.D4:
                    if (IsDumpButtonEnabled && CanDumpToNetVisibility == Visibility.Visible)
                    {
                        DumpCommandExecute();
                    }
                    args.Handled = true;
                    break;

                case Key.D5:
                    if (IsDumpButtonEnabled && CanDumpToNetVisibility == Visibility.Visible)
                    {
                        DumpToNetCommandExcute();
                    }
                    args.Handled = true;
                    break;
                case Key.D6:
                    // 若该帐户下按钮不可用，应屏蔽对应快捷按键
                    if (IsOpenButtonEnabled && CanRemoveBadChannelVisibility == Visibility.Visible && CanDumpToNetVisibility == Visibility.Visible)
                    {
                        OpenRemoveBadChannelCommandExecute();
                    }

                    args.Handled = true;
                    break;

                case Key.D7:
                    PrintCommandExecute();
                    args.Handled = true;
                    break;

                case Key.F1:
                    RetrieveImagesCommandExecute();
                    args.Handled = true;
                    break;

                case Key.OemPlus:
                    MoveToNextPageCommmandExecute();
                    args.Handled = true;
                    break;

                case Key.OemMinus:
                    MoveToPreviousPageCommandExecute();
                    args.Handled = true;
                    break;
            }
        }

        //private void ImageListBox_DoubleOnClick(MouseButtonEventArgs e)
        //{
        //    switch (e.ClickCount)
        //    {

        //        case 2://双击
        //            {
        //                MessageBox.Show("hahah");
        //                break;
        //            }
        //    }
        //}

        public override void Cleanup()
        {
            base.Cleanup();

            // ViewModel清理时，从cache中移除
            ViewModelLocator.Locator.Cache.Remove("ImageRetrievalConditions");
        }
    }
}
