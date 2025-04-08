using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using UI.Common.Tracers;

namespace UI.XRay.Security.Scanner.Converters
{
    /// <summary>
    /// 把Bitmap图片转换为BitmapImage格式，用于显示控件绑定
    /// </summary>
    [ValueConversion(typeof(System.Drawing.Bitmap), typeof(BitmapSource))]
    class Bitmap2BitmapImageConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            BitmapSource returnSource = null; ; 
            try
            {
                var bmp = (System.Drawing.Bitmap)value;

                if (bmp != null)
                {
                    returnSource = Imaging.CreateBitmapSourceFromHBitmap(bmp.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
                returnSource = null;
            }
            return returnSource;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // 不存在反向转换
            return null;
        }
    }
}
