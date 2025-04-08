using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
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
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Scanner.ViewModel
{
    public class TipLibSelectViewModel : ViewModelBase
    {
        public RelayCommand TipLibSelectionChangedEventCommand { get; private set; }

        public RelayCommand<SelectionChangedEventArgs> ImagesSelectionChangedEventCommand { get; private set; }
        public RelayCommand<SelectionChangedEventArgs> SelectImagesSelectionChangedEventCommand { get; private set; }
        public RelayCommand MoveToNextPageCommand { get; private set; }
        public RelayCommand MoveToPreviousCommand { get; private set; }
        public RelayCommand SelectMoveToNextPageCommand { get; private set; }
        public RelayCommand SelectMoveToPreviousCommand { get; private set; }
        public RelayCommand ClosedEventCommand { get; private set; }

        public RelayCommand SelectCommand { get; private set; }
        public RelayCommand DeleteSelectedCommand { get; private set; }

        public RelayCommand SelectAllCommand { get; set; }
        public RelayCommand AntiSelectAllCommand { get; set; }

        private bool _isSelectedForEach;
        private BindableImage _lastSelectedImage = null;
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
        /// 选择图像列表控件是否可见（当图像个数超过0时即可见）
        /// </summary>
        private Visibility _selectImagesListVisibility;
        public Visibility SelectImagesListVisibility
        {
            get { return _selectImagesListVisibility; }
            set { _selectImagesListVisibility = value; RaisePropertyChanged(); }
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
                _showingImageMaxIndex = value;
                RaisePropertyChanged();
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
                _totalImagesCount = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 当前显示选择的图像，在整个检索到的图像中的索引中的最小编号，起始编号为1
        /// </summary>
        private int _selectShowingImageMinIndex = 0;
        public int SelectShowingImageMinIndex
        {
            get { return _selectShowingImageMinIndex; }
            set { _selectShowingImageMinIndex = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 当前显示选择的图像，在整个检索到的图像中的索引中的最大编号，最大值为所有检索到的图像之和
        /// </summary>
        private int _selectShowingImageMaxIndex = 0;
        public int SelectShowingImageMaxIndex
        {
            get { return _selectShowingImageMaxIndex; }
            set
            {
                _selectShowingImageMaxIndex = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 当前选择图像检索记录总数
        /// </summary>
        private int _selectTotalImagesCount;
        public int SelectTotalImagesCount
        {
            get { return _selectTotalImagesCount; }
            set
            {
                _selectTotalImagesCount = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 操作图像库中选中图像的按钮是否可用（包括删除，选择按钮）
        /// </summary>
        private bool _isManipulateSelectedImagesButtonsEnabled;
        public bool IsManipulateSelectedImagesButtonsEnabled
        {
            get { return _isManipulateSelectedImagesButtonsEnabled; }
            set { _isManipulateSelectedImagesButtonsEnabled = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 操作已选择图像库选中图像的按钮是否可用（包括删除按钮）
        /// </summary>
        private bool _isManipulateSelectedImagesButtonEnabled;
        public bool IsManipulateSelectedImagesButtonEnabled
        {
            get { return _isManipulateSelectedImagesButtonEnabled; }
            set { _isManipulateSelectedImagesButtonEnabled = value; RaisePropertyChanged(); }
        }

        private TipImagesManagementController _managementController;

        public TipLibSelectViewModel()
        {
            TipLibSelectionChangedEventCommand = new RelayCommand(TipLibSelectionChangedEventCommandExecute);
            MoveToPreviousCommand = new RelayCommand(MoveToPreviousPageCommandExecute);
            MoveToNextPageCommand = new RelayCommand(MoveToNextPageCommmandExecute);
            SelectMoveToPreviousCommand = new RelayCommand(SelectMoveToPreviousPageCommandExecute);
            SelectMoveToNextPageCommand = new RelayCommand(SelectMoveToNextPageCommmandExecute);
            ClosedEventCommand = new RelayCommand(ClosedEventCommandExecute);
            SelectCommand = new RelayCommand(SelectCommandExecute);
            SelectAllCommand = new RelayCommand(SelectAllCommandExecute);
            AntiSelectAllCommand = new RelayCommand(AntiSelectAllCommandExecute);
            DeleteSelectedCommand = new RelayCommand(DeleteSelectedCommandExecute);
            ImagesSelectionChangedEventCommand = new RelayCommand<SelectionChangedEventArgs>(ImagesSelectionChangedEventCommandExecute);
            SelectImagesSelectionChangedEventCommand = new RelayCommand<SelectionChangedEventArgs>(SelectImagesSelectionChangedEventCommandExecute);
            TipLibList = new List<ValueStringItem<TipLibrary>>()
            {
                new ValueStringItem<TipLibrary>(TipLibrary.Explosives, TranslationService.FindTranslation("Explosives")),
                new ValueStringItem<TipLibrary>(TipLibrary.Knives, TranslationService.FindTranslation("Knives")),
                new ValueStringItem<TipLibrary>(TipLibrary.Guns, TranslationService.FindTranslation("Guns")),
                new ValueStringItem<TipLibrary>(TipLibrary.Others, TranslationService.FindTranslation("Others")),
            };

            CurrentImages = new ObservableCollection<BindableImage>();
            _controller = new ImageRetrievalControllerExt(100);

            //LoadPlan();
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
        private bool SelectHasSelectedImage
        {
            get
            {
                if (SelectImages != null)
                {
                    if (SelectImages.Any(image => image.IsSelected))
                    {
                        return true;
                    }
                }

                return false;
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

        private void SelectImagesSelectionChangedEventCommandExecute(SelectionChangedEventArgs args)
        {
            IsManipulateSelectedImagesButtonEnabled = SelectHasSelectedImage;
        }

        private void SelectCommandExecute()
        {
            if (_managementController != null)
            {
                SelectImages = new ObservableCollection<BindableImage>();
                List<string> imageFilePaths = new List<string>();
                foreach (var image in CurrentImages)
                {
                    if (image.IsSelected)
                    {
                        SelectImages.Add(image);
                        image.IsSelected = false;
                        imageFilePaths.Add(image.ImagePath);
                    }
                }

                if (SelectImages != null && SelectImages.Count > 0)
                {
                    _managementController.ImportSelectImagesToLib(imageFilePaths);
                    SelectImagesListVisibility = Visibility.Visible;
                }
                LoadImagesOfLib(SelectedLibrary);
            }
        }

        private void DeleteSelectedCommandExecute()
        {
            if (_managementController != null)
            {
                var selectedRecords = new List<string>(10);
                var selectedImages = new List<BindableImage>(10);
                var selectedFileName = new List<string>(10);
                foreach (var image in SelectImages)
                {
                    if (image.IsSelected)
                    {
                        selectedRecords.Add(image.ImagePath);
                        selectedImages.Add(image);
                        selectedFileName.Add(Path.GetFileName(image.ImagePath));
                    }
                }
                _managementController.UpDateUnSelectImages(selectedFileName);
                // 从数据库及磁盘中删除
                _managementController.DeleteSelectImages(selectedRecords);

                // 从缓存及视图中删除
                foreach (var image in selectedImages)
                {
                    SelectImages.Remove(image);
                }

                // 更新当前页信息
                UpdateIndices();
                CurrentImages = _managementController.GetCurrentUnSelectPageImages();
                SelectImages = _managementController.GetCurrentSelectPageImages();
                if (TotalImagesCount > 0)
                {
                    ImagesListVisibility = Visibility.Visible;
                }
                if (SelectTotalImagesCount <= 0)
                {
                    SelectImagesListVisibility = Visibility.Collapsed;
                }
            }
        }

        private void MoveToNextPageCommmandExecute()
        {
            var images = _managementController.MoveToUnSelectNextPage();
            if (images != null && images.Count > 0)
            {
                CurrentImages = images;
                UpdateIndices();
            }
        }

        private void MoveToPreviousPageCommandExecute()
        {
            var images = _managementController.MoveToUnSelectPreviousPage();
            if (images != null && images.Count > 0)
            {
                CurrentImages = images;
                UpdateIndices();
            }
        }

        private void SelectMoveToNextPageCommmandExecute()
        {
            var images = _managementController.MoveToSelectNextPage();
            if (images != null && images.Count > 0)
            {
                SelectImages = images;
                UpdateIndices();
            }
        }

        private void SelectMoveToPreviousPageCommandExecute()
        {
            var images = _managementController.MoveToSelectPreviousPage();
            if (images != null && images.Count > 0)
            {
                SelectImages = images;
                UpdateIndices();
            }
        }

        private void UpdateIndices()
        {
            if (_managementController != null)
            {
                TotalImagesCount = _managementController.UnSelectImagesCount;
                ShowingImageMinIndex = _managementController.ShowingUnSelectMinIndex + 1;
                ShowingImageMaxIndex = _managementController.ShowingUnSelectMaxIndex + 1;
                SelectTotalImagesCount = _managementController.SelectImagesCount;
                SelectShowingImageMinIndex = _managementController.ShowingSelectMinIndex + 1;
                SelectShowingImageMaxIndex = _managementController.ShowingSelectMaxIndex + 1;
            }
        }

        private void TipLibSelectionChangedEventCommandExecute()
        {
            LoadImagesOfLib(SelectedLibrary);
        }

        private void ClosedEventCommandExecute()
        {
            this.MessengerInstance.Send(new PlanandImageToDbMessage());
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
                CurrentImages = _managementController.MoveToUnSelectFirstPage();
                SelectImages = _managementController.MoveToSelectFirstPage();
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
            if (SelectImages != null && SelectImages.Count > 0)
            {
                SelectImagesListVisibility = Visibility.Visible;
            }
            else
            {
                SelectImagesListVisibility = Visibility.Collapsed;
            }
        }
    }
}
