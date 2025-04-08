using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Common.Utilities;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Services;
using UI.XRay.Flows.Services.Db;
using UI.XRay.Gui.Framework;
using UI.XRay.Security.Scanner.Printing;

namespace UI.XRay.Security.Scanner.ViewModel
{
    /// <summary>
    /// 屏幕图像操作窗口的视图模型
    /// </summary>
    public class ScreenImagesOperationWindowViewModel : ViewModelBase
    {
        /// <summary>
        /// 命令：选择所有屏幕图像
        /// </summary>
        public RelayCommand SelectAllCommand { get; set; }

        /// <summary>
        /// 命令：反选命令
        /// </summary>
        public RelayCommand AntiSelectAllCommand { get; set; }

        /// <summary>
        /// 命令：锁定当前选中的图像
        /// </summary>
        public RelayCommand LockCommand { get; set; }

        /// <summary>
        /// 打印命令
        /// </summary>
        public RelayCommand PrintCommand { get; set; }

        public RelayCommand SaveSelectedCommand { get; set; }

        public RelayCommand<KeyEventArgs> PreviewKeyDownCommand { get; set; }

        private IEnumerable<ImageRecord> _imageRecords;

        private ObservableCollection<BindableImage> _currentImages;

        private string _manualStoreXrayPath;

        /// <summary>
        /// 当前显示的所有图像
        /// </summary>
        public ObservableCollection<BindableImage> CurrentImages
        {
            get { return _currentImages; }
            set
            {
                _currentImages = value;
                SaveSuccessVisibility = Visibility.Hidden;
                RaisePropertyChanged();
            }
        }

        private Visibility _saveSuccessVisibility = Visibility.Collapsed;

        public Visibility SaveSuccessVisibility
        {
            get { return _saveSuccessVisibility; }
            set
            {
                _saveSuccessVisibility = value;
                RaisePropertyChanged();
            }
        }


        public ScreenImagesOperationWindowViewModel()
        {
            if (IsInDesignMode)
            {
                CurrentImages = new ObservableCollection<BindableImage>();
                var imgUri = new Uri("../../../Icons/Image.png", UriKind.Relative);
                CurrentImages.Add(new BindableImage() { ImageGuid = "12", Thumbnail = new BitmapImage(imgUri), ScanTime = DateTime.Now, IsLocked = true });
                CurrentImages.Add(new BindableImage() { ImageGuid = "34", Thumbnail = new BitmapImage(imgUri) });
                CurrentImages.Add(new BindableImage() { ImageGuid = "12", Thumbnail = new BitmapImage(imgUri), ScanTime = DateTime.Now });
                CurrentImages.Add(new BindableImage() { ImageGuid = "34", Thumbnail = new BitmapImage(imgUri) });
                //CurrentImages.Add(new BindableImage() { ImageGuid = "12", Thumbnail = new BitmapImage(imgUri), ScanTime = DateTime.Now });
                //CurrentImages.Add(new BindableImage() { ImageGuid = "34", Thumbnail = new BitmapImage(imgUri) });
                //CurrentImages.Add(new BindableImage() { ImageGuid = "12", Thumbnail = new BitmapImage(imgUri), ScanTime = DateTime.Now });
                //CurrentImages.Add(new BindableImage() { ImageGuid = "34", Thumbnail = new BitmapImage(imgUri), IsLocked = true });
            }

            _manualStoreXrayPath = ConfigPath.ManualStorePath;

            if (!Directory.Exists(_manualStoreXrayPath)) Directory.CreateDirectory(_manualStoreXrayPath);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="records">待操作处理的图像记录列表，可能为空</param>
        public ScreenImagesOperationWindowViewModel(IEnumerable<ImageRecord> records)
        {
            SelectAllCommand = new RelayCommand(SelectAllCommandExecute);
            AntiSelectAllCommand = new RelayCommand(AntiSelectAllCommandExecute);
            PrintCommand = new RelayCommand(PrintCommandExecute);
            LockCommand = new RelayCommand(LockCommandExecute);

            SaveSelectedCommand = new RelayCommand(SaveSelectedExecute);

            PreviewKeyDownCommand = new RelayCommand<KeyEventArgs>(PreviewKeyDownCommandExecute);

            _imageRecords = records;
            if (_imageRecords != null)
            {
                CurrentImages = ImageRetrievalController.ConvertRecordsToBindableImages(_imageRecords);
            }
        }

        private void PreviewKeyDownCommandExecute(KeyEventArgs args)
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
                    LockCommandExecute();
                    args.Handled = true;
                    break;

                case Key.D4:
                    PrintCommandExecute();
                    args.Handled = true;
                    break;
                case Key.D5:
                    SaveSelectedExecute();
                    break;
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
                    image.IsLocked = !image.IsLocked;
                }

                List<ImageRecord> selected = CurrentImages.Where(image => image.IsSelected == true).Select(image => image.Record).ToList();
                if (selected.Count > 0)
                {
                    // 锁定所有选中图像
                    ImageRecordDbSet.LockImages(selected);
                }
                List<ImageRecord> unselected = CurrentImages.Where(image => image.IsSelected == false).Select(image => image.Record).ToList();
                if (unselected.Count > 0)
                {
                    // 锁定所有选中图像
                    ImageRecordDbSet.UnLockImages(selected);
                }

                foreach (var image in selected)
                {
                    new OperationRecordService().AddRecord(new OperationRecord()
                    {
                        AccountId = LoginAccountManager.Service.CurrentAccount != null ? LoginAccountManager.Service.CurrentAccount.AccountId : "",
                        OperateUI = OperationUI.ScreenImagesOperation,
                        OperateTime = DateTime.Now,
                        OperateObject = Path.GetFileName(image.StorePath),
                        OperateCommand = OperationCommand.Lock,
                        OperateContent = string.Empty,
                    });
                }
                foreach (var image in unselected)
                {
                    new OperationRecordService().AddRecord(new OperationRecord()
                    {
                        AccountId = LoginAccountManager.Service.CurrentAccount != null ? LoginAccountManager.Service.CurrentAccount.AccountId : "",
                        OperateUI = OperationUI.ScreenImagesOperation,
                        OperateTime = DateTime.Now,
                        OperateObject = Path.GetFileName(image.StorePath),
                        OperateCommand = OperationCommand.Unlock,
                        OperateContent = string.Empty,
                    });
                }
            }
        }

        /// <summary>
        /// 打印当前选中的所有图像
        /// </summary>
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

                            ip.AttachImageData(img.View1Data);
                            var bmp = ip.GetBitmap();
                            if (bmp != null)
                            {
                                ImagePrinter.PrintBitmapAsync(bmp, image.Record.MachineNumber, image.ScanTime, image.Record.AccountId);
                            }
                            new OperationRecordService().AddRecord(new OperationRecord()
                            {
                                AccountId = LoginAccountManager.Service.CurrentAccount != null ? LoginAccountManager.Service.CurrentAccount.AccountId : "",
                                OperateUI = OperationUI.ScreenImagesOperation,
                                OperateTime = DateTime.Now,
                                OperateObject = fileName,
                                OperateCommand = OperationCommand.Print,
                                OperateContent = string.Empty,
                            });
                        }
                    }
                    catch (Exception exception)
                    {
                        Tracer.TraceException(exception, "Failed to print image file: " + image.ImagePath);
                    }
                }
            }
        }

        private void SaveSelectedExecute()
        {
            MessengerInstance.Send(new CloseWindowMessage("ScreenImagesOperationWindow"));
            _manualStoreXrayPath = ConfigPath.ManualStorePath;

            if (!Directory.Exists(_manualStoreXrayPath)) Directory.CreateDirectory(_manualStoreXrayPath);

            var images = CurrentImages.Where(image => image.IsSelected);
            if (images.Count() < 1) return;

            foreach (var image in images)
            {
                try
                {
                    var ip = new XRayImageProcessor();
                    var fileName = Path.GetFileName(image.ImagePath);
                    var dstFileName = Path.Combine(_manualStoreXrayPath, fileName);
                    if (File.Exists(dstFileName))
                    {
                        File.Delete(dstFileName);
                    }
                    if (File.Exists(image.ImagePath))
                    {
                        File.Copy(image.ImagePath, dstFileName);

                        var img = XRayScanlinesImage.LoadFromDiskFile(image.ImagePath);
                        ip.AttachImageData(img.View1Data);
                        var bmp = ip.GetBitmap();

                        Bitmap bmp2 = null;
                        if (img.View2Data != null)
                        {
                            ip.AttachImageData(img.View2Data);
                            bmp2 = ip.GetBitmap();
                        }

                        if (bmp != null && Save4ImagesService.Service.View1Vertical)
                        {
                            bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                        }
                        if (bmp2 != null && Save4ImagesService.Service.View2Vertical)
                        {
                            bmp2.RotateFlip(RotateFlipType.RotateNoneFlipY);
                        }
                        if (Save4ImagesService.Service.Left2Right)
                        {
                            bmp.RotateFlip(RotateFlipType.RotateNoneFlipX);
                            if (bmp2 != null)
                            {
                                bmp2.RotateFlip(RotateFlipType.RotateNoneFlipX);
                            }
                        }

                        var combinebmp = BitmapHelper.CombineBmp(bmp, bmp2);
                        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                        // 民航要求，在保存的图片上显示设备编号、扫描时间和操机员
                        PrintInfomationOnImage.PrintOnImage(combinebmp, fileNameWithoutExtension, dateTime2String(img.ScanningTime));
                        combinebmp.Save(Path.Combine(_manualStoreXrayPath, fileNameWithoutExtension + ".jpg"), ImageFormat.Jpeg);
                        new OperationRecordService().AddRecord(new OperationRecord()
                        {
                            AccountId = LoginAccountManager.Service.CurrentAccount != null ? LoginAccountManager.Service.CurrentAccount.AccountId : "",
                            OperateUI = OperationUI.ScreenImagesOperation,
                            OperateTime = DateTime.Now,
                            OperateObject = fileName,
                            OperateCommand = OperationCommand.Export,
                            OperateContent = ConfigHelper.AddQuotationForPath(dstFileName),
                        });

                    }

                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception, "Failed to save image file: " + image.ImagePath);
                }
            }
            SaveSuccessVisibility = Visibility.Visible;
        }

        private string dateTime2String(DateTime dt)
        {
            int systemDateformat;
            if (!ScannerConfig.Read(ConfigPath.SystemDateFormat, out systemDateformat))
            {
                systemDateformat = 0;
            }
            switch (systemDateformat)
            {
                case 0:
                    return dt.ToString("MM.dd.yyyy  HH:mm:ss");
                case 1:
                    return dt.ToString("dd.MM.yyyy  HH:mm:ss");
                case 2:
                    return dt.ToString("yyyy.MM.dd  HH:mm:ss");
                default:
                    return string.Format("MM.dd.yyyy  HH:mm:ss", dt);
            }
        }

        /// <summary>
        /// 选择所有图像
        /// </summary>
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

        /// <summary>
        /// 反响选择
        /// </summary>
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

    }
}
