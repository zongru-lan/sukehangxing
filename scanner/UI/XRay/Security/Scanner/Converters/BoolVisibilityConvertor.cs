using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using UI.Common.Tracers;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Scanner.Converters
{
    /// <summary>
    /// 布尔值到可见性的值转换器：当bool值为true时，转换为Visible，否则转换为Collapse
    /// 主要用于在界面设计中，将一个
    /// 注意：不存在反向的转换
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    class BoolVisibilityConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                var val = (bool)value;
                if (val)
                {
                    return Visibility.Visible;
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // 不存在反向转换
            return null;
        }
    }

    class BoolUnVisibilityConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value != null)
                {
                    var val = (Visibility)value;
                    if (val != Visibility.Visible)
                    {
                        return true;
                    }
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

     [ValueConversion(typeof(bool), typeof(string))]
    class BoolTranslationConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value != null)
                {
                    var val = value.ToString();
                    return TranslationService.FindTranslation("HardwareStation", val);
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

}
