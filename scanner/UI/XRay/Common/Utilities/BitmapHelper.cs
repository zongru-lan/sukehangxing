using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace UI.XRay.Common.Utilities
{
    public static class BitmapHelper
    {
        /// <summary>
        /// 将bitmap图像转换成BitmapImage，用于界面控件的显示，
        /// </summary>
        /// <param name="sourceImage">要转换为BitmapImage的Bitmap源图像</param>
        /// <returns>转换成的BitmapImage图像</returns>
        public static BitmapImage ConvertToBitmapImage(Bitmap sourceImage)
        {
            if (sourceImage != null)
            {
                var ms = new MemoryStream();
                sourceImage.Save(ms, ImageFormat.Png);
                var imageSource = new BitmapImage();
                imageSource.BeginInit();
                imageSource.StreamSource = new MemoryStream(ms.ToArray());
                imageSource.EndInit();
                ms.Dispose();
                return imageSource;
            }
            return null;
        }

        /// <summary>
        /// 合并两幅图片
        /// </summary>
        /// <param name="b1"></param>
        /// <param name="b2"></param>
        /// <returns></returns>
        public static Bitmap CombineBmp(Bitmap b1, Bitmap b2)
        {
            if (b1 == null)
            {
                return b2;
            }
            if (b2 == null)
            {
                return b1;
            }

            //默认两张图片横向拼接到一起
            Bitmap newBitmap = new Bitmap(b1.Width + b2.Width, Math.Max(b1.Height, b2.Height));
            Graphics g = Graphics.FromImage(newBitmap);
            g.FillRectangle(new SolidBrush(Color.White), new Rectangle(0, 0, newBitmap.Width, newBitmap.Height));
            g.DrawImage(b1, 0, 0);
            g.DrawImage(b2, b1.Width, 0);
            g.Dispose();
            return newBitmap;
        }

        public static void ImageCompress(Bitmap bitmap, string dstPath, ImageFormat format, int flag = 75)
        {
            ImageFormat tFormat = bitmap.RawFormat;
            EncoderParameters eps = new EncoderParameters();
            long[] quality = new long[1];
            quality[0] = flag;
            EncoderParameter ep = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            eps.Param[0] = ep;
            try
            {
                ImageCodecInfo[] arrayICI = ImageCodecInfo.GetImageEncoders();
                ImageCodecInfo jpegICIInfo = null;
                for (int x = 0; x < arrayICI.Length; x++)
                {
                    if (arrayICI[x].FormatDescription.Equals("JPEG"))
                    {
                        jpegICIInfo = arrayICI[x];
                        break;
                    }
                }
                if (jpegICIInfo != null)
                {
                    bitmap.Save(dstPath, jpegICIInfo, eps);
                }
                else
                {
                    bitmap.Save(dstPath, format);
                }
            }
            catch (Exception e)
            {
                
            }
        }
    }
}
