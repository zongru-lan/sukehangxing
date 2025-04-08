using System;
using System.Windows.Data;

namespace UI.XRay.Security.Scanner.Converters
{
    /// <summary>
    /// 非空字符串-Bool转换。如果字符串非空，且不为null，则为true，否则为false。
    /// 注意：不存在反向的转换
    /// </summary>
    [ValueConversion(typeof(string), typeof(bool))]
    class NotNullEmptyStringBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var str = value as string;
            if (str != null)
            {
                return (!string.IsNullOrEmpty(str.Trim()));
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 非空字符串-Bool转换。如果字符串非空，且不为null，则为true，否则为false。
    /// 注意：不存在反向的转换
    /// </summary>
    [ValueConversion(typeof(string), typeof(bool))]
    class NotNullEmptyObjectBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value == null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return string.Empty;
        }
    }
}
