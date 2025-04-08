using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using UI.Common.Tracers;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Services;
using UI.XRay.Flows.Services.Db;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Tip
{
    public class TipImagesPageViewModel : PageViewModelBase
    {
        public RelayCommand TipLibSelectionChangedEventCommand { get; private set; }
        public RelayCommand<SelectionChangedEventArgs> ImagesSelectionChangedEventCommand { get; private set; }
        public RelayCommand MoveToNextPageCommand { get; private set; }
        public RelayCommand MoveToPreviousCommand { get; private set; }
        /// <summary>
        /// 导入新图像到当前的Tip库中
        /// </summary>
        public RelayCommand ImportImagesCommand { get; private set; }
        public RelayCommand DeleteCommand { get; private set; }

        //wjj
        public RelayCommand SelectAllCommand { get; set; }
        public RelayCommand AntiSelectAllCommand { get; set; }


        /// <summary>
        /// 当前显示类别库中的所有图像
        /// </summary>
        private ObservableCollection<BindableImage> _currentImages;
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
        /// 当前选择的所有图像
        /// </summary>
        private ObservableCollection<BindableImage> _selectImages;
        public ObservableCollection<BindableImage> SelectImages
        {
            get { return _selectImages; }
            set
            {
                _selectImages = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Tip库类型列表
        /// </summary>
        private List<ValueStringItem<TipLibrary>> _tipLibList;
        public List<ValueStringItem<TipLibrary>> TipLibList
        {
            get { return _tipLibList; }
            set { _tipLibList = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 当前选中的Tip库
        /// </summary>
        private TipLibrary _selectedLibrary = TipLibrary.Explosives;
        public TipLibrary SelectedLibrary
        {
            get { return _selectedLibrary; }
            set { _selectedLibrary = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 库图像列表控件是否可见（当图像个数超过0时即可见）
        /// </summary>
        private Visibility _imagesListVisibility;
        public Visibility ImagesListVisibility
        {
            get { return _imagesListVisibility; }
            set { _imagesListVisibility = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 当前显示的库图像，在整个检索到的图像中的索引中的最小编号，起始编号为1
        /// </summary>
        private int _showingImageMinIndex = 0;
        public int ShowingImageMinIndex
        {
            get { return _showingImageMinIndex; }
            set { _showingImageMinIndex = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 当前显示的库图像，在整个检索到的图像中的索引中的最大编号，最大值为所有检索到的图像之和
        /// </summary>
        private int _showingImageMaxIndex = 0;
        public int ShowingImageMaxIndex
        {
            get { return _showingImageMaxIndex; }
            set
            {
                _showingImageMaxIndex = value; RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 当前库图像检索记录总数
        /// </summary>
        private int _totalImagesCount;
        public int TotalImagesCount
        {
            get { return _totalImagesCount; }
            set
            {
                _totalImagesCount = value; RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 操作图像库中选中图像的按钮是否可用
        /// </summary>
        private bool _isManipulateSelectedImagesButtonsEnabled;
        public bool IsManipulateSelectedImagesButtonsEnabled
        {
            get { return _isManipulateSelectedImagesButtonsEnabled; }
            set { _isManipulateSelectedImagesButtonsEnabled = value; RaisePropertyChanged(); }
        }

        private TipImagesManagementController _managementController;
        private bool _isdeleteable = true;


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
        private readonly ImageRetrievalControllerExt _controller;

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
        /// <summary>
        /// 是不是正在设置选中的foreach中，因为此时一直在selectionchanged
        /// </summary>
        private bool _isSelectedForEach;
        /// <summary>
        /// 上次选中的图像，用于shift多选
        /// </summary>
        private BindableImage _lastSelectedImage = null;

        public TipImagesPageViewModel()
        {
            TipLibSelectionChangedEventCommand = new RelayCommand(TipLibSelectionChangedEventCommandExecute);
            MoveToPreviousCommand = new RelayCommand(MoveToPreviousPageCommandExecute);
            MoveToNextPageCommand = new RelayCommand(MoveToNextPageCommmandExecute);
            ImportImagesCommand = new RelayCommand(ImportImagesCommandExecute);
            DeleteCommand = new RelayCommand(DeleteCommandExecute);
            ImagesSelectionChangedEventCommand = new RelayCommand<SelectionChangedEventArgs>(ImagesSelectionChangedEventCommandExecute);

            SelectAllCommand = new RelayCommand(SelectAllCommandExecute);
            AntiSelectAllCommand = new RelayCommand(AntiSelectAllCommandExecute);

            TipLibList = new List<ValueStringItem<TipLibrary>>()
            {
                new ValueStringItem<TipLibrary>(TipLibrary.Explosives, TranslationService.FindTranslation("Explosives")),
                new ValueStringItem<TipLibrary>(TipLibrary.Knives, TranslationService.FindTranslation("Knives")),
                new ValueStringItem<TipLibrary>(TipLibrary.Guns, TranslationService.FindTranslation("Guns")),
                new ValueStringItem<TipLibrary>(TipLibrary.Others, TranslationService.FindTranslation("Others")),
            };

            _controller = new ImageRetrievalControllerExt(100);


            CurrentImages = new ObservableCollection<BindableImage>();
            if (IsInDesignMode)
            {
                var imgUri = new Uri("../../../Icons/Image.png", UriKind.Relative);
                CurrentImages.Add(new BindableImage() { ImageGuid = "12", Thumbnail = new BitmapImage(imgUri), ScanTime = DateTime.Now });
                CurrentImages.Add(new BindableImage() { ImageGuid = "34", Thumbnail = new BitmapImage(imgUri) });
            }
            else
            {
                LoadImagesOfLib(SelectedLibrary);
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
        /// <summary>
        /// 当图像列表的选择发生变化时，更新几个按钮的使能状态
        /// </summary>
        /// <param name="args"></param>
        private void ImagesSelectionChangedEventCommandExecute(SelectionChangedEventArgs args)
        {
            IsManipulateSelectedImagesButtonsEnabled = HasSelectedImage;
        }

        /// <summary>
        /// 删除当前选中的图像
        /// </summary>
        private void DeleteCommandExecute()
        {
            var tipPlanManager = new TipPlanDbSet();
            var plans = tipPlanManager.SelectAll();
            var planIsEnabled = plans.Where(p => p.IsEnabled == true).ToList();
            if (planIsEnabled.Count > 0)
            {
                switch (SelectedLibrary)
                {
                    case TipLibrary.Explosives:
                        if (planIsEnabled[0].ExplosivesWeight > 0)
                            _isdeleteable = false;
                        break;
                    case TipLibrary.Guns:
                        if (planIsEnabled[0].GunsWeight > 0)
                            _isdeleteable = false;
                        break;
                    case TipLibrary.Knives:
                        if (planIsEnabled[0].KnivesWeight > 0)
                            _isdeleteable = false;
                        break;
                    case TipLibrary.Others:
                        if (planIsEnabled[0].OtherObjectsWeight > 0)
                            _isdeleteable = false;
                        break;
                    default:
                        break;
                }
            }
            if (_managementController != null && _isdeleteable)
            {
                var selectedRecords = new List<string>(10);
                var selectedImages = new List<BindableImage>(10);

                foreach (var image in CurrentImages)
                {
                    if (image.IsSelected)
                    {
                        selectedRecords.Add(image.ImagePath);
                        selectedImages.Add(image);
                    }
                }
                _managementController.DeleteImages(selectedRecords);
                foreach (var image in selectedImages)
                {
                    CurrentImages.Remove(image);
                }

                TotalImagesCount = _managementController.ImagesCount;
                CurrentImages = _managementController.GetCurrentPageImages();
                UpdateIndices();

                if (TotalImagesCount <= 0)
                {
                    ImagesListVisibility = Visibility.Collapsed;
                }

                foreach (var image in selectedRecords)
                {
                    new OperationRecordService().AddRecord(new OperationRecord()
                    {
                        AccountId = LoginAccountManager.Service.CurrentAccount != null ? LoginAccountManager.Service.CurrentAccount.AccountId : "",
                        OperateUI = OperationUI.TipImages,
                        OperateTime = DateTime.Now,
                        OperateObject = Path.GetFileName(image),
                        OperateCommand = OperationCommand.Delete,
                        OperateContent = SelectedLibrary.ToString(),
                    });
                }
            }
        }

        /// <summary>
        /// 命令：Tip库导入图像
        /// </summary>
        private void ImportImagesCommandExecute()
        {

            var msg = new ShowOpenFilesDialogMessageAction("SettingWindow", "Xray file | *.xray", true, list =>
            {
                if (list != null && list.Length > 0)
                {
                    if (_managementController == null)
                    {
                        _managementController = new TipImagesManagementController(SelectedLibrary);
                    }

                    _managementController.ImportImagesToLib(list);

                    // 导入完成后，重新加载图像
                    LoadImagesOfLib(SelectedLibrary);

                    //记录
                    foreach (var tipimage in list)
                    {
                        new OperationRecordService().AddRecord(new OperationRecord()
                        {
                            AccountId = LoginAccountManager.Service.CurrentAccount != null ? LoginAccountManager.Service.CurrentAccount.AccountId : "",
                            OperateUI = OperationUI.TipImages,
                            OperateTime = DateTime.Now,
                            OperateObject = Path.GetFileName(tipimage),
                            OperateCommand = OperationCommand.Import,
                            OperateContent = ConfigHelper.AddQuotationForPath(SelectedLibrary.ToString()),
                        });
                    }
                }
            });
            MessengerInstance.Send(msg);
        }

        private void MoveToNextPageCommmandExecute()
        {
            var images = _managementController.MoveToNextPage();
            if (images != null && images.Count > 0)
            {
                CurrentImages = images;
                UpdateIndices();
            }
        }

        private void MoveToPreviousPageCommandExecute()
        {
            var images = _managementController.MoveToPreviousPage();
            if (images != null && images.Count > 0)
            {
                CurrentImages = images;
                UpdateIndices();
            }
        }

        private void UpdateIndices()
        {
            if (_managementController != null)
            {
                TotalImagesCount = _managementController.ImagesCount;
                ShowingImageMinIndex = _managementController.ShowingMinIndex + 1;
                ShowingImageMaxIndex = _managementController.ShowingMaxIndex + 1;
            }
        }

        private void TipLibSelectionChangedEventCommandExecute()
        {
            LoadImagesOfLib(SelectedLibrary);
        }

        /// <summary>
        /// 根据当前选定的Tip库类型，从文件系统中加载Tip图像库
        /// </summary>
        /// <param name="library"></param>
        private void LoadImagesOfLib(TipLibrary library)
        {
            try
            {
                _managementController = new TipImagesManagementController(library);
                CurrentImages = _managementController.MoveToFirstPage();
                UpdateIndices();
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            if (CurrentImages != null && CurrentImages.Count > 0)
            {
                ImagesListVisibility = Visibility.Visible;
            }
            else
            {
                ImagesListVisibility = Visibility.Collapsed;
            }
        }

        public override void OnKeyDown(System.Windows.Input.KeyEventArgs args)
        {
        }
    }
}
