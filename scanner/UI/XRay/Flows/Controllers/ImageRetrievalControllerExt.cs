using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.Entities;
using UI.XRay.Common.Utilities;
using UI.XRay.ImagePlant.Cpu;

namespace UI.XRay.Flows.Controllers
{
    public class ImageRetrievalControllerExt : ImageRetrievalController
    {
        private readonly XRayImageProcessor _imageProcessor = new XRayImageProcessor();

        public ImageRetrievalControllerExt(int pageSize = 10, ImageRetrievalConditions conditions = null)
            : base(pageSize, conditions)
        {

        }

        public Task<int> ReLoadRecordsAsync()
        {
            return Task.Run(() => base.ReLoadRecords());
        }

        public ObservableCollection<BindableImage> MoveToFirstPageWithoutLoadXrayImage()
        {
            if (_resultRecords == null || _resultRecords.Count == 0)
            {
                return null;
            }

            ShowingMinIndex = 0;
            ShowingMaxIndex = Math.Min(ShowingMinIndex + PageSize - 1, ResultRecordsCount - 1);

            return GetCurrentPageImagesWithoutLoadXrayImage();
        }

        public Task<ObservableCollection<BindableImage>> MoveToFirstPageWithoutLoadXrayImageAsync()
        {
            return Task.Run(() => this.MoveToFirstPageWithoutLoadXrayImage());
        }

        /// <summary>
        /// 生成当前页图像缩略图等列表，用于显示
        /// </summary>
        /// <returns></returns>
        private ObservableCollection<BindableImage> GetCurrentPageImagesWithoutLoadXrayImage()
        {
            var pageRecords = GetCurrentPageRecords();
            if (pageRecords != null)
            {
                return ConvertRecordsToBindableImagesWithoutLoadXRayImage(pageRecords);
            }

            return null;
        }

        public List<ImageRecord> GetNotLoadPageImagesRecords()
        {
            return _resultRecords != null ? _resultRecords.Skip(ShowingMaxIndex + 1).ToList() : null;
        }

        public ObservableCollection<BindableImage> MoveToNextPageWithoutLoadXRayImage()
        {
            if (ShowingMaxIndex == ResultRecordsCount - 1)
            {
                return null;
            }

            ShowingMinIndex += PageSize;
            ShowingMinIndex = Math.Min(ShowingMinIndex, ResultRecordsCount - 1);
            ShowingMaxIndex = Math.Min(ShowingMinIndex + PageSize - 1, ResultRecordsCount - 1);

            return GetCurrentPageImagesWithoutLoadXrayImage();
        }

        public Task<ObservableCollection<BindableImage>> MoveToNextPageWithoutLoadXRayImageAsync()
        {
            return Task.Run(() => this.MoveToNextPageWithoutLoadXRayImage());
        }

        public ObservableCollection<BindableImage> UpdateCurrentPageWithoutLoadXRayImage()
        {
            if (ShowingMinIndex == ResultRecordsCount - 1)
            {
                ShowingMinIndex -= PageSize;
            }

            ShowingMinIndex = Math.Max(ShowingMinIndex, 0);
            ShowingMaxIndex = Math.Min(ShowingMinIndex + PageSize - 1, ResultRecordsCount - 1);

            return GetCurrentPageImagesWithoutLoadXrayImage();
        }


        public Task<ObservableCollection<BindableImage>> UpdateCurrentPageWithoutLoadXRayImageAsync()
        {
            return Task.Run(() => this.UpdateCurrentPageWithoutLoadXRayImage());
        }

        private ObservableCollection<BindableImage> ConvertRecordsToBindableImagesWithoutLoadXRayImage(
            IEnumerable<ImageRecord> records)
        {
            var collections = new ObservableCollection<BindableImage>();
            if (records != null)
            {
                foreach (var record in records)
                {
                    var bindImage = new BindableImage(record);
                    try
                    {
                        bindImage.FileName = Path.GetFileName(record.StorePath);
                    }
                    catch (Exception exception)
                    {
                        Tracer.TraceException(exception);
                    }

                    collections.Add(bindImage);
                }
            }

            return collections;
        }

        public BindableImage InitBindableImageViewBmp(BindableImage bindableImage)
        {
            if (bindableImage == null)
                return null;

            try
            {
                var image = XRayScanlinesImage.LoadFromDiskFile(bindableImage.ImagePath);
                bindableImage.DualView = image.ViewsCount == 2;


                _imageProcessor.AttachImageData(image.View1Data);
                var bmp = _imageProcessor.GetBitmap();
                bindableImage.View1Image = BitmapHelper.ConvertToBitmapImage(bmp);

                if (image.ViewsCount == 2)
                {
                    _imageProcessor.AttachImageData(image.View2Data);
                    bmp = _imageProcessor.GetBitmap();
                    bindableImage.View2Image = BitmapHelper.ConvertToBitmapImage(bmp);
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
            return bindableImage;
        }

        public Task<BindableImage> InitBindableImageViewBmpAsync(BindableImage bindableImage)
        {
            return Task.Run(() => this.InitBindableImageViewBmp(bindableImage));
        }
    }
}
