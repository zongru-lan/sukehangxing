using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using UI.Common.Tracers;

namespace UI.XRay.Security.Scanner.Converters
{

    [ValueConversion(typeof(int), typeof(bool))]
    class PositiveIntBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                var val = (int)value;
                if (val > 0)
                {
                    return true;
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
            // 不存在反向转换
            return null;
        }
    }
}
