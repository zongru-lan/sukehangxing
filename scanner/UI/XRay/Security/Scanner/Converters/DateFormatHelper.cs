using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;

namespace UI.XRay.Security.Scanner.Converters
{
    public static class DateFormatHelper
    {
        /// <summary>
        /// 从配置中获取时间格式，说明如下：
        /// 0：默认格式，MM/dd/yyyy，现在软件的格式
        /// 1:dd/MM/yyyy 法语格式
        /// 2:yyyy/MM/dd 另一种通用格式
        /// </summary>
        /// <returns></returns>
        public static string GetDateFormatHelper()
        {
            int systemDateformat;

            if (!ScannerConfig.Read(ConfigPath.SystemDateFormat,out systemDateformat))
            {
                systemDateformat = 0;
            }
            switch (systemDateformat)
            {
                case 0:
                    return "MM.dd.yyyy";
                case 1:
                    return "dd.MM.yyyy";
                case 2:
                    return "yyyy.MM.dd";
                default:
                    return "MM.dd.yyyy";
            }
        }
        public static string DateTime2String(DateTime dt)
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
    }

    [ValueConversion(typeof(DateTime), typeof(string))]
    class DateTime2StringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if (value == null) return "NULL";
                int systemDateformat;

                if (!ScannerConfig.Read(ConfigPath.SystemDateFormat, out systemDateformat))
                {
                    systemDateformat = 0;
                }
                switch (systemDateformat)
                {
                    case 0:
                        return ((DateTime)value).ToString("MM.dd.yyyy  HH:mm:ss");
                    case 1:
                        return ((DateTime)value).ToString("dd.MM.yyyy  HH:mm:ss");
                    case 2:
                        return ((DateTime)value).ToString("yyyy.MM.dd  HH:mm:ss");
                    default:
                        return string.Format("MM.dd.yyyy  HH:mm:ss", value);
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return DateTime.Parse((string)value);
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
            return null;
        }
    }

    class Date2StringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                int systemDateformat;

                if (!ScannerConfig.Read(ConfigPath.SystemDateFormat, out systemDateformat))
                {
                    systemDateformat = 0;
                }
                var charCount = ((string)value).Count();
                var isWeakStyle = ((string)value).Contains('-');
                if (charCount == 10 && !isWeakStyle)
                {
                    switch (systemDateformat)
                    {
                        case 0:
                            return DateTime.Parse(value.ToString()).ToString("MM.dd.yyyy");
                        case 1:
                            return DateTime.Parse(value.ToString()).ToString("dd.MM.yyyy");
                        case 2:
                            return DateTime.Parse(value.ToString()).ToString("yyyy.MM.dd");
                        default:
                            return DateTime.Parse(value.ToString()).ToString("MM.dd.yyyy");
                    }
                }
                
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            return (string)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return DateTime.Parse((string)value);
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
            return null;
        }
    }

    class Date3StringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null) return "NULL";
                int systemDateformat;

                if (!ScannerConfig.Read(ConfigPath.SystemDateFormat, out systemDateformat))
                {
                    systemDateformat = 0;
                }
                switch (systemDateformat)
                {
                    case 0:
                        return ((DateTime)value).ToString("MM.dd.yyyy HH:mm");
                    case 1:
                        return ((DateTime)value).ToString("dd.MM.yyyy HH:mm");
                    case 2:
                        return ((DateTime)value).ToString("yyyy.MM.dd HH:mm");
                    default:
                        return string.Format("MM.dd.yyyy HH:mm:ss", value);
                }

            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            return (string)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return DateTime.Parse((string)value);
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
            return null;
        }
    }
    class Hours2StringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                double input = (double)value;
                int totalSeconds = (int)(input * 60 * 60);
                int hours = totalSeconds / 60 / 60;
                int minutes = totalSeconds / 60 % 60;
                int seconds = totalSeconds % 60;
                string str =string.Format("{0:00}:{1:00}:{2:00}", hours ,minutes ,seconds);
                return str;
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            return string.Format("{0:00}:{1:00}:{2:00}", 0, 0, 0); ;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    class TimeSpan2HoursStringConverter:IValueConverter
    {
        public object Convert(object value,Type targetType,object parameter,System.Globalization.CultureInfo culture)
        {
            try
            {
                if (value == null) return 0;
                var input = ((TimeSpan)value).TotalHours;
                int totalSeconds = (int)(input * 60 * 60);
                int hours = totalSeconds / 60 / 60;
                int minutes = totalSeconds / 60 % 60;
                int seconds = totalSeconds % 60;
                string str = string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
                return str;
            }
            catch (Exception e)
            {
                
            }
            return string.Format("{0:00}:{1:00}:{2:00}", 0, 0, 0); ;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
