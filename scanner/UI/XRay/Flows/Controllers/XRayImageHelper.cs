using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.Entities;
using UI.XRay.Common.Utilities;

namespace UI.XRay.Flows.Controllers
{
    public class XRayImageHelper
    {
        /// <summary>
        /// 生成一幅XRayImage对应的Bitmap的缩略图，最大尺寸为160*160
        /// </summary>
        /// <param name="bmp">原始图像</param>
        /// <param name="verticalScale">图像延纵向拉伸的比例，应该大于0。1表示不拉伸，大于1表示纵向放大</param>
        /// <returns>生成的缩略图</returns>
        public static Bitmap GenerateThumbnail(Bitmap bmp, float verticalScale = 1)
        {
            if (bmp == null)
            {
                throw new ArgumentNullException("bmp");
            }

            // 避免负数
            verticalScale = verticalScale < 0 ? 1 : verticalScale;

            // 如果图像的长、宽满足缩略图要求，则直接使用源图像作为缩略图
            if (bmp.Height <= 160 && bmp.Width <= 160)
            {
                return bmp;
            }

            // 源图像的宽度
            var sourceWidth = bmp.Width;

            // 源图像缩放调整后的高度：在原来的基础上，根据需要进行放大
            var sourceHeight = bmp.Height * verticalScale;

            var scaleFactor = sourceWidth > sourceHeight ? 160.0f / sourceWidth : 160.0f / sourceHeight;
            var thumbWidth = (int)(sourceWidth * scaleFactor);
            var thumbHeight = (int)(sourceHeight * scaleFactor);
            Image image = bmp.GetThumbnailImage(thumbWidth, thumbHeight,
                new Image.GetThumbnailImageAbort(ThumbnailCallback),
                IntPtr.Zero);

            return image as Bitmap;
        }

        /// <summary>
        /// 仅仅用于GenerateMatColorThumbnail中调用Image.GetThumbnailImage使用，无实际意义
        /// </summary>
        /// <returns></returns>
        private static bool ThumbnailCallback()
        {
            return false;
        }

        /// <summary>
        /// 基于图像记录，生成用于显示的图像
        /// </summary>
        /// <param name="records"></param>
        /// <returns></returns>
        public static ObservableCollection<BindableImage> ToBindableImages(IEnumerable<ImageRecord> records)
        {
            var collections = new ObservableCollection<BindableImage>();
            if (records != null)
            {
                foreach (var record in records)
                {
                    var bindImage = new BindableImage(record);
                    try
                    {
                        var image = XRayScanlinesImage.LoadFromDiskFile(record.StorePath);
                        bindImage.Thumbnail = BitmapHelper.ConvertToBitmapImage(image.Thumbnail);
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

        /// <summary>
        /// 基于图像文件，生成用于显示的图像
        /// </summary>
        /// <param name="imageFilePathList"></param>
        /// <returns></returns>
        public static ObservableCollection<BindableImage> ToBindableImages(IEnumerable<string> imageFilePathList)
        {
            var collections = new ObservableCollection<BindableImage>();
            if (imageFilePathList != null)
            {
                foreach (var record in imageFilePathList)
                {
                    var bindImage = new BindableImage(record);
                    try
                    {
                        var image = XRayScanlinesImage.LoadFromDiskFile(record);
                        bindImage.Thumbnail = BitmapHelper.ConvertToBitmapImage(image.Thumbnail);
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

        /// <summary>
        /// 基于图像文件，生成用于显示的图像
        /// </summary>
        /// <returns></returns>
        public static BindableImage ToBindableImage(string imagePath)
        {
            if (!string.IsNullOrWhiteSpace(imagePath))
            {
                var bindImage = new BindableImage(imagePath);
                try
                {
                    var image = XRayScanlinesImage.LoadFromDiskFile(imagePath);
                    if (image.Thumbnail != null)
                    {
                        bindImage.Thumbnail = BitmapHelper.ConvertToBitmapImage(image.Thumbnail);
                    }
                    return bindImage;
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception);
                }
            }

            return null;
        }
    }
}
