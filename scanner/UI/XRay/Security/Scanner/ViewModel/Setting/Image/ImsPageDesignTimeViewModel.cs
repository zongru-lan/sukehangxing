using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using UI.XRay.Flows.Controllers;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Image
{
    class ImsPageDesignTimeViewModel : ViewModelBase
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

        private ObservableCollection<BindableImage> _selectedImages;

        /// <summary>
        /// 当前显示的所有图像
        /// </summary>
        public ObservableCollection<BindableImage> SelectedImages
        {
            get { return _selectedImages; }
            set
            {
                _selectedImages = value;
                RaisePropertyChanged();
            }
        }


        public ImsPageDesignTimeViewModel()
        {
            CurrentImages = new ObservableCollection<BindableImage>();

            var imgUri = new Uri("../../../Icons/Image.png", UriKind.Relative);
            CurrentImages.Add(new BindableImage() { ImageGuid = "12", Thumbnail = new BitmapImage(imgUri) });
            CurrentImages.Add(new BindableImage() { ImageGuid = "34", Thumbnail = new BitmapImage(imgUri) });
        }
    }
}
