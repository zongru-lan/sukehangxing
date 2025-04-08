using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UI.Common.Tracers;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Services;
using UI.XRay.Flows.TRSNetwork.Models;
using UI.XRay.Gui.Framework;
using UI.XRay.Security.Scanner.MetroDialogs;
using UI.XRay.Security.Scanner.Printing;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Image
{
    public class ImsListPageViewModel : PageViewModelBase
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

        private BindableImage _currentClickImage;

        public BindableImage CurrentClickImage
        {
            get { return _currentClickImage; }
            set
            {
                _currentClickImage = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 上次选中的图像，用于shift多选
        /// </summary>
        private BindableImage _lastSelectedImage = null;

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

        private int _imageCountInListView;

        public int ImageCountInListView
        {
            get { return _imageCountInListView; }
            set
            {
                _imageCountInListView = value;
                RaisePropertyChanged();
            }
        }


        private bool _isDeleteButtonEnabled;

        private bool _isOpenButtonEnabled;

        private bool _isPlaybackButtonEnabled;

        public bool IsDeleteButtonEnabled
        {
            get { return _isDeleteButtonEnabled; }
            set { _isDeleteButtonEnabled = value; RaisePropertyChanged(); }
        }

        public bool IsOpenButtonEnabled
        {
            get { return _isOpenButtonEnabled; }
            set
            {
                _isOpenButtonEnabled = value;
                RaisePropertyChanged();
            }
        }

        public bool IsPlaybackButtonEnabled
        {
            get { return _isPlaybackButtonEnabled; }
            set
            {
                _isPlaybackButtonEnabled = value;
                RaisePropertyChanged();
            }
        }

        private bool _busy;
        public bool Busy
        {
            get { return _busy; }

            set
            {
                if (_busy != value)
                {
                    _busy = value;

                    RaisePropertyChanged();
                }
            }
        }

        #region ProcessBar

        private int _currentProcessNum;

        public int CurrentProcessNum
        {
            get { return _currentProcessNum; }
            set
            {
                _currentProcessNum = value;
                RaisePropertyChanged();
            }
        }

        private int _totalProcessNum;

        public int TotalProcessNum
        {
            get { return _totalProcessNum; }
            set
            {
                _totalProcessNum = value;
                RaisePropertyChanged();
            }
        }

        private int _percentDone;

        public int PercentDone
        {
            get { return _percentDone; }
            set
            {
                _percentDone = value;
                RaisePropertyChanged();
            }
        }

        private Visibility _processBarVisibility = Visibility.Collapsed;

        public Visibility ProcessBarVisibility
        {
            get { return _processBarVisibility; }
            set
            {
                _processBarVisibility = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        /// <summary>
        /// listbox滚动条下滑到最后自动加载下一页明星
        /// </summary>
        public RelayCommand FetchMoreDataCommand { get; private set; }

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

        public RelayCommand CancelProcessBarCommand { get; set; }

        public RelayCommand<SelectionChangedEventArgs> SelectionChangedEventCommand { get; private set; }

        public RelayCommand ToThumbnailViewCommand { get; set; }

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

        private readonly ImageRetrievalControllerExt _controller;

        private readonly BindableImage _emptyBindableImage = new BindableImage(@"D:\SecurityScanner\Images\0000\01\01\00000000_000000000000_111111.xray");

        /// <summary>
        /// 未显示的图像是否被选中
        /// </summary>
        private bool _isUnVisibleImageSelected;

        public bool IsUnvisibleImageSelected
        {
            get { return _isUnVisibleImageSelected; }
            set
            {
                if (CurrentImages != null && CurrentImages.Count < TotalImagesCount)
                {
                    _isUnVisibleImageSelected = value;
                }
            }
        }

        private bool _processBarCanceled;

        /// <summary>
        /// 是不是正在设置选中的foreach中，因为此时一直在selectionchanged
        /// </summary>
        private bool _isSelectedForEach;

        private bool _isLeftShiftPressed;

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

        public ImsListPageViewModel()
        {
            RetrieveImagesCommand = new RelayCommand(RetrieveImagesCommandExecute);
            SelectAllCommand = new RelayCommand(SelectAllCommandExecute);
            AntiSelectAllCommand = new RelayCommand(AntiSelectAllCommandExecute);
            ReplayCommand = new RelayCommand(ReplayCommandExecute);
            OpenCommand = new RelayCommand(OpenCommandExecute);
            OpenRemoveBadChannelCommand = new RelayCommand(OpenRemoveBadChannelCommandExecute);
            DeleteCommand = new RelayCommand(DeleteCommandExecute);
            DumpCommand = new RelayCommand(DumpCommandExecute);
            DumpToNetCommand = new RelayCommand(DumpToNetCommandExcute);
            PrintCommand = new RelayCommand(PrintCommandExecute);
            SelectionChangedEventCommand =
                new RelayCommand<SelectionChangedEventArgs>(SelectionChangedEventCommandExecute);

            LockCommand = new RelayCommand(LockCommandExecute);
            UnlockCommand = new RelayCommand(UnlockCommandExecute);

            FetchMoreDataCommand = new RelayCommand(MoveToNextPageCommmandExecute);

            CancelProcessBarCommand = new RelayCommand(CancelProcessBarCommandExe);
            ToThumbnailViewCommand = new RelayCommand(ToThumbnailViewCommandExe);

            CurrentImages = new ObservableCollection<BindableImage>();

            UpdateDeleteButtonState();

            _controller = new ImageRetrievalControllerExt(100);

            CurrentClickImage = _emptyBindableImage;

            LoadFirstPageOnCondition();
        }

        private void ToThumbnailViewCommandExe()
        {
            MessengerInstance.Send(new NotificationMessage(this, "ImsPage"));
        }

        private void CancelProcessBarCommandExe()
        {
            _processBarCanceled = true;
        }

        private async void LoadFirstPageOnCondition()
        {
            Busy = true;
            try
            {
                IsUnvisibleImageSelected = false;

                TotalImagesCount = await _controller.ReLoadRecordsAsync();
                CurrentImages = await _controller.MoveToFirstPageWithoutLoadXrayImageAsync();

                ImageCountInListView = CurrentImages != null ? CurrentImages.Count : 0;

                if (ImageCountInListView > 0 && CurrentImages != null)
                {
                    //选中第一个
                    CurrentImages[0].IsSelected = true;
                    CurrentClickImage = _controller.InitBindableImageViewBmp(CurrentImages.First());
                }
                else
                {
                    CurrentClickImage = _emptyBindableImage;
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            Busy = false;
        }

        private void RetrieveImagesCommandExecute()
        {
            var msg = new ShowImageRetrievalWindowAction("SettingWindow", ResetConditions);

            // 在显示图像检索窗口之前，先缓存当前的检索条件，这样在图像检索窗口显示时，将初始化为当前的检索条件
            ViewModelLocator.Locator.Cache["ImageRetrievalConditions"] = Conditions;
            Messenger.Default.Send(msg);
        }

        private void ResetConditions(ImageRetrievalConditions conditions)
        {
            if (conditions != null)
            {
                _controller.ResetConditions(conditions);

                LoadFirstPageOnCondition();
            }
        }

        private void SelectAllCommandExecute()
        {
            if (CurrentImages != null && CurrentImages.Count > 0)
            {
                CurrentClickImage = _controller.InitBindableImageViewBmp(CurrentImages.First());

                IsUnvisibleImageSelected = true;
                ForeachChangeCurrentImagesSelection(false);
            }
        }

        private void AntiSelectAllCommandExecute()
        {
            //反选的时候，未显示图像的选择属性反转
            IsUnvisibleImageSelected = !IsUnvisibleImageSelected;

            //反选情况复杂，不在考虑shift多选情况
            _lastSelectedImage = null;
            ForeachChangeCurrentImagesSelection(true);
        }

        private void ForeachChangeCurrentImagesSelection(bool isAntiSelectAll)
        {
            if (CurrentImages != null && CurrentImages.Count > 0)
            {
                _isSelectedForEach = true;

                if (isAntiSelectAll)
                {
                    foreach (var image in CurrentImages)
                    {
                        image.IsSelected = !image.IsSelected;
                    }
                }
                else
                {
                    foreach (var image in CurrentImages)
                    {
                        if (!image.IsSelected)
                        {
                            image.IsSelected = true;
                        }
                    }
                }

                _isSelectedForEach = false;
            }
        }
        XRayImageProcessor processor;
        /// <summary>
        /// 当图像列表的选择发生变化时，更新几个按钮的使能状态
        /// </summary>
        /// <param name="args"></param>
        private void SelectionChangedEventCommandExecute(SelectionChangedEventArgs args)
        {
            IsOpenButtonEnabled = HasSelectedImage;
            IsPlaybackButtonEnabled = HasSelectedImage;
            UpdateDeleteButtonState();
            processor = new XRayImageProcessor();
            BindableImage changedValue = null;

            if (args.AddedItems.Count > 0 && !_isSelectedForEach)
            {
                changedValue = args.AddedItems[0] as BindableImage;

                //增加选中项的时候，判断是否有按着shift
                if (_isLeftShiftPressed && _lastSelectedImage != null)
                {
                    _isLeftShiftPressed = false;

                    var lastIndex = CurrentImages.IndexOf(_lastSelectedImage);
                    var currentIndex = CurrentImages.IndexOf(changedValue);

                    var end = lastIndex >= currentIndex ? lastIndex : currentIndex;
                    var start = lastIndex >= currentIndex ? currentIndex : lastIndex;

                    if (end - start > 1)
                    {
                        _isSelectedForEach = true;
                        for (int i = start + 1; i < end; i++)
                        {
                            if (!CurrentImages[i].IsSelected)
                            {
                                CurrentImages[i].IsSelected = true;
                            }
                        }

                        _isSelectedForEach = false;
                    }
                }

                _lastSelectedImage = changedValue;
            }
            if (args.RemovedItems.Count > 0)
            {
                if (!_isSelectedForEach)
                {
                    changedValue = args.RemovedItems[0] as BindableImage;
                }
            }

            if (changedValue != null && !_isSelectedForEach)
            {
                CurrentClickImage = _controller.InitBindableImageViewBmp(changedValue);
            }
        }

        /// <summary>
        /// 锁定当前选中的图像
        /// </summary>
        private async void LockCommandExecute()
        {
            Busy = true;
            await Task.Run(() =>
            {
                LockOrUnlockSelectedRecord(true);
            });
            Busy = false;
        }

        /// <summary>
        /// 对当前选中图像解除锁定
        /// </summary>
        private async void UnlockCommandExecute()
        {
            Busy = true;
            await Task.Run(() =>
            {
                LockOrUnlockSelectedRecord(false);
            });
            Busy = false;
        }

        private void LockOrUnlockSelectedRecord(bool isLock)
        {
            if (CurrentImages != null)
            {
                //更新界面
                foreach (var image in CurrentImages.Where(image => image.IsSelected == true))
                {
                    image.IsLocked = isLock;
                    new OperationRecordService().RecordOperation(OperationUI.ImsPage, Path.GetFileName(image.ImagePath), isLock ? OperationCommand.Lock : OperationCommand.Unlock, string.Empty);
                }

                List<ImageRecord> selected = GetCurrentSelectedImageRecord();

                if (selected.Count > 0)
                {
                    foreach (var imageRecord in selected)
                    {
                        imageRecord.IsLocked = isLock;
                    }

                    // 锁定所有选中图像
                    _controller.Update(selected);
                }
            }
        }

        private List<ImageRecord> GetCurrentSelectedImageRecord()
        {
            if (CurrentImages == null || CurrentImages.Count == 0)
                return null;

            List<ImageRecord> selected =
                CurrentImages.Where(image => image.IsSelected).Select(image => image.Record).ToList();

            //未显示的图像被选中，说明还有没显示的图像
            if (IsUnvisibleImageSelected)
            {
                var unvisibleImageSelected = _controller.GetNotLoadPageImagesRecords();
                if (unvisibleImageSelected != null && unvisibleImageSelected.Count > 0)
                {
                    selected.AddRange(unvisibleImageSelected);
                }
            }
            return selected.Where(s => !s.IsLocked).ToList();
        }

        private void UpdateDeleteButtonState()
        {
            if (LoginAccountManager.Service.CurrentAccount != null)
            {
                //IsDeleteButtonEnabled = LoginAccountManager.CurrentAccount.Role != AccountRole.Operator && HasSelectedImage;
                IsDeleteButtonEnabled = HasSelectedImage;
            }
            else
            {
                IsDeleteButtonEnabled = false;
            }
        }

        private async void PrintCommandExecute()
        {
            await Task.Run(() =>
            {
                if (CurrentImages != null && CurrentImages.Count > 0)
                {
                    var images = GetCurrentSelectedImageRecord();

                    int totalCount = images.Count();

                    int currentNum = 1;

                    foreach (var image in images)
                    {
                        if (!UpdateProcessBar(totalCount, currentNum++))
                            break;

                        try
                        {
                            var fileName = Path.GetFileName(image.StorePath);
                            if (!string.IsNullOrEmpty(fileName))
                            {
                                var ip = new XRayImageProcessor();
                                var img = XRayScanlinesImage.LoadFromDiskFile(image.StorePath);

                                if (img != null)
                                {
                                    ip.AttachImageData(img.View1Data);
                                    var bmp = ip.GetBitmap();
                                    if (bmp != null)
                                    {
                                        ImagePrinter.PrintBitmapAsync(bmp, image.MachineNumber, image.ScanTime,
                                            image.AccountId);
                                    }
                                }
                            }
                        }
                        catch (Exception exception)
                        {
                            Tracer.TraceException(exception, "Failed to print image file: " + image.StorePath);
                        }
                    }
                }
            });
        }

        private void DumpCommandExecute()
        {
            WindowFocusHelper.Pause();
            var msg = new ShowFolderBrowserDialogMessageAction("SettingWindow", s =>
            {
                if (!string.IsNullOrWhiteSpace(s))
                {
                    ExportSelectedImagesToFolder(s);
                }
                WindowFocusHelper.Continue();
            });

            MessengerInstance.Send(msg);
        }

        private async void DumpToNetCommandExcute()
        {
            await Task.Run(() =>
            {
                if (CurrentImages != null)
                {
                    var srcImageFilePath = CurrentImages.Where(image => image.IsSelected).Select(image => image.ImagePath);
                    int totalCount = srcImageFilePath.Count();
                    int currentNum = 0;

                    if (Flows.TRSNetwork.TRSNetWorkService.Service.Connected)
                    {
                        foreach (var path in srcImageFilePath)
                        {
                            if (!UpdateProcessBar(totalCount, currentNum))
                                break;

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
                                            new OperationRecordService().RecordOperation(OperationUI.ImsListPage, fileName, OperationCommand.Saveto, "DumpToNet");
                                        }
                                    }
                                    else
                                    {
                                        // 导出图像
                                        Save4ImagesService.Service.SaveAllImageToNetwork(path, destPath, SelectedDumpFormat);
                                    }
                                }
                                if (!UpdateProcessBar(totalCount, ++currentNum))
                                    break;
                            }
                            catch (Exception exception)
                            {
                                Tracer.TraceException(exception, "Failed to copy file to dest folder: " + path);
                            }
                        }
                        Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            MessengerInstance.Send(new ShowDialogMessageAction("SettingWindow",
                                "", TranslationService.FindTranslation("Images dump completed"), MetroDialogButtons.Ok,
                                result =>
                                {

                                }));
                        });
                    }
                    else
                    {
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

            await Task.Run(() =>
            {
                if (CurrentImages != null)
                {
                    var selectImageRecords = GetCurrentSelectedImageRecord();
                    var srcImageFilePath = selectImageRecords.Select(image => image.StorePath);

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
                            break;

                        try
                        {
                            var fileName = Path.GetFileName(path);
                            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
                            if (!string.IsNullOrEmpty(fileName))
                            {
                                if (SelectedDumpFormat == 4)
                                {
                                    // 导出原始数据
                                    File.Copy(path, Path.Combine(destPath, fileName), true);
                                    new OperationRecordService().RecordOperation(OperationUI.ImsListPage, fileName, OperationCommand.Saveto, ConfigHelper.AddQuotationForPath(Path.Combine(destPath, Path.Combine(destPath, fileName))));
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

                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessengerInstance.Send(new ShowDialogMessageAction("SettingWindow",
                            "", TranslationService.FindTranslation("Images dump completed"), MetroDialogButtons.Ok,
                            result =>
                            {

                            }));
                    });
                }
            });

            WindowFocusHelper.Continue();
        }

        private void SaveViewImage(XRayImageProcessor ip, ImageViewData viewData, string destPath, string fileNameWithoutExtension,
            string extension)
        {
            ip.AttachImageData(viewData);
            var bmp = ip.GetBitmap();
            if (bmp != null)
            {
                var format = ImageFormat.Bmp;
                var ext = ".bmp";
                switch (SelectedDumpFormat)
                {
                    case 0:
                        format = ImageFormat.Bmp;
                        ext = ".bmp";
                        break;
                    case 1:
                        format = ImageFormat.Jpeg;
                        ext = ".jpg";
                        break;
                    case 2:
                        format = ImageFormat.Png;
                        ext = ".png";
                        break;
                    case 3:
                        format = ImageFormat.Tiff;
                        ext = ".tiff";
                        break;
                }

                bmp.Save(Path.Combine(destPath, fileNameWithoutExtension + extension + ext), format);
            }
        }

        private void DeleteCommandExecute()
        {
            if (CurrentImages != null && IsDeleteButtonEnabled)
            {
                _lastSelectedImage = null;

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

        private async void DeleteSelectedImages()
        {
            bool hasLockedImage = false;
            int deletedImageCount = 0;

            await Task.Run(() =>
            {
                int deletedImageCountTemp = 0;

                List<ImageRecord> selectedRecords = GetCurrentSelectedImageRecord();

                // 从数据库及磁盘中删除
                if (selectedRecords != null && selectedRecords.Count > 0)
                {
                    //每10条记录作为一份，删除
                    if (selectedRecords.Count > 10)
                    {
                        var totalCount = selectedRecords.Count;

                        int partCount = (int)Math.Ceiling((double)totalCount / 10);

                        UpdateProcessBar(totalCount, 1);

                        for (int i = 0; i < partCount; i++)
                        {
                            var records = selectedRecords.Skip(i * 10).Take(10);
                            _controller.RemoveRecords(records);
                            deletedImageCountTemp += records.Count();
                            foreach (var item in records)
                            {
                                new OperationRecordService().RecordOperation(OperationUI.ImsListPage, Path.GetFileName(item.StorePath), OperationCommand.Delete, string.Empty);
                            }

                            if (!UpdateProcessBar(totalCount, deletedImageCountTemp))
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        deletedImageCountTemp += selectedRecords.Count;
                        _controller.RemoveRecords(selectedRecords);
                    }

                    deletedImageCount = deletedImageCountTemp;
                }
            });

            List<BindableImage> selectedShowingImages = new List<BindableImage>(30);

            if (CurrentImages != null && CurrentImages.Count > 0)
            {
                selectedShowingImages = CurrentImages.Where(image => image.IsSelected).ToList();
            }

            if (selectedShowingImages.Count > 0 && CurrentImages != null)
            {
                // 从缓存及视图中删除
                foreach (var image in selectedShowingImages)
                {
                    if (!image.IsLocked)
                    {
                        CurrentImages.Remove(image);
                        if (--deletedImageCount <= 0)
                        {
                            break;
                        }
                    }
                    else
                    {
                        hasLockedImage = true;
                    }
                }
            }

            // 更新当前页信息
            TotalImagesCount = _controller.ResultRecordsCount;
            CurrentImages = _controller.UpdateCurrentPageWithoutLoadXRayImage();

            if (CurrentImages != null && CurrentImages.Count > 0)
            {
                if (!CurrentImages.Contains(CurrentClickImage))
                {
                    CurrentImages[0].IsSelected = true;
                    //CurrentClickImage = _controller.InitBindableImageViewBmp(CurrentImages[0]);
                }
                ImageCountInListView = CurrentImages.Count;
            }
            else
            {
                CurrentClickImage = _emptyBindableImage;
                ImageCountInListView = 0;
            }
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
            if (CurrentImages != null)
            {
                var main = Application.Current.MainWindow;

                var img = CurrentImages.FirstOrDefault(a => a.IsSelected);
                if (img != null)
                {
                    ImageBadChannelManual win = new ImageBadChannelManual();
                    ImageBadChannelManualViewModel vm = new ImageBadChannelManualViewModel(img.Record);
                    win.Owner = main;
                    win.DataContext = vm;
                    win.ShowDialog();
                }

                //var img = CurrentImages.FirstOrDefault(a => a.IsSelected);
                //if (img != null)
                //{
                //    ImageBadChannelManual win = new ImageBadChannelManual();
                //    ImageBadChannelManualViewModel vm = new ImageBadChannelManualViewModel();
                //    win.DataContext = vm;
                //    win.ShowDialog();
                //}
                //foreach (var image in CurrentImages)
                //{
                //    // 打开第一个选中的图像
                //    if (image.IsSelected)
                //    {

                //        //var bmpSource = _controller.GenerateImage(image.Record, processor);
                //        //if (bmpSource != null)
                //        //    Messenger.Default.Send(new ShowBadChannelImageMessage(image.Record));
                //    }
                //}
            }
        }

        private void ReplayCommandExecute()
        {
            if (CurrentImages != null)
            {
                List<ImageRecord> selected = CurrentImages.Where(image => image.IsSelected == true).Select(image => image.Record).ToList();
                if (selected.Count > 0)
                {
                    // 先关闭设置窗口
                    Messenger.Default.Send(new CloseWindowMessage("SettingWindow"));

                    // 发送消息，启动图像回放
                    Messenger.Default.Send(new PlaybackImageRecordsMessage(selected));
                }
            }
        }

        private async void MoveToNextPageCommmandExecute()
        {
            if (Busy) return;
            Busy = true;

            ObservableCollection<BindableImage> nextPageImages = null;

            await Task.Run(() =>
            {
                nextPageImages = _controller.MoveToNextPageWithoutLoadXRayImage();
            });

            if (nextPageImages != null && nextPageImages.Count > 0)
            {
                foreach (var image in nextPageImages)
                {
                    if (IsUnvisibleImageSelected)
                    {
                        image.IsSelected = true;
                    }
                    CurrentImages.Add(image);
                }

                ImageCountInListView = CurrentImages.Count;
            }
            System.Threading.Thread.Sleep(100);
            Busy = false;
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

        public override void OnKeyDown(KeyEventArgs args)
        {
            switch (args.Key)
            {
                case Key.D0:
                    CancelProcessBarCommandExe();
                    args.Handled = true;
                    break;

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

                //case Key.D4:
                //    if (IsDeleteButtonEnabled)
                //    {
                //        DeleteCommandExecute();
                //    }
                //    args.Handled = true;
                //    break;

                case Key.D4:
                    LockCommandExecute();
                    args.Handled = true;
                    break;

                case Key.D5:
                    UnlockCommandExecute();
                    args.Handled = true;
                    break;

                case Key.D6:
                    DumpCommandExecute();
                    args.Handled = true;
                    break;

                case Key.D7:
                    DumpToNetCommandExcute();
                    args.Handled = true;
                    break;

                case Key.D8:
                    OpenRemoveBadChannelCommandExecute();
                    args.Handled = true;
                    break;

                case Key.F1:
                    RetrieveImagesCommandExecute();
                    args.Handled = true;
                    break;

                case Key.F2:
                    ToThumbnailViewCommandExe();
                    args.Handled = true;
                    break;

                case Key.LeftShift:
                    LeftShiftPressed();
                    args.Handled = true;
                    break;
            }
        }

        public override void OnPreviewKeyUp(KeyEventArgs args)
        {
            if (args.Key == Key.LeftShift)
            {
                _isLeftShiftPressed = false;
            }
        }

        private void LeftShiftPressed()
        {
            _isLeftShiftPressed = true;
        }

        public override void Cleanup()
        {
            base.Cleanup();

            // ViewModel清理时，从cache中移除
            ViewModelLocator.Locator.Cache.Remove("ImageRetrievalConditions");
        }
    }
}
