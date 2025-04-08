using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace UI.XRay.Gui.Framework.Converters
{
    /// <summary>
    /// 将字符串转换为SecurityString
    /// </summary>
    public class SecureStringConverter : IValueConverter
    {
        public object Convert(object o, Type type, object parameter, CultureInfo culture)
        {
            string objectToConvert = o.ToString();
            var secure = new SecureString();

            foreach (char c in objectToConvert)
            {
                secure.AppendChar(c);
            }

            return secure.ToString();
        }

        public object ConvertBack(object o, Type type, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
