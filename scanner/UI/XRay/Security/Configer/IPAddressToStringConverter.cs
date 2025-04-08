using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace UI.XRay.Security.Configer
{

    /// <summary>
    /// IPAddress和String的转换器，实现IPAddress类型和String类型的互转，主要用于绑定
    /// </summary>
    [ValueConversion(typeof(IPAddress), typeof(String))]
    public class IPAddressToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 将IPAddress转换成String
            if (value != null)
            {
                return value.ToString();
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 将String转换成IPAddress，如果失败，wpf会出现一定的提示
            IPAddress ip;
            if (value != null && IPAddress.TryParse(value.ToString(), out ip))
            {
                return ip;
            }

            // 转换失败，则发出提示，todo 暂时不太清楚该语句的原理
            return DependencyProperty.UnsetValue;
        }
    }
}
