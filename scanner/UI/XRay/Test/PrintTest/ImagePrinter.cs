using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UI.XRay.Common.Utilities;
using Image = System.Windows.Controls.Image;
using Point = System.Windows.Point;

namespace UI.XRay.Test.PrintTest
{
    /// <summary>
    /// 图像打印机：提供用于打印Bitmap图像的功能
    /// </summary>
    public class ImagePrinter
    {
        public static void PrintBitmapImage(BitmapImage bi)
        {
            FixedPage fp = (FixedPage)Application.LoadComponent(new Uri("/ImagePrintPage.xaml", UriKind.Relative));

            var image = fp.FindName("Image") as Image;
            if (image != null)
            {
                var w = image.Width;
                var h = image.Height;

                if (bi.Width <= w && bi.Height <= h)
                {
                    image.Stretch = Stretch.None;
                }

                image.Source = bi;
                var a4Size = new System.Windows.Size(96*8.5, 96*11);

                // Update the layout of our FixedPage
                fp.Measure(a4Size);
                fp.Arrange(new Rect(new Point(), a4Size));
                fp.UpdateLayout();

                // 获取默认的打印机
                var pq = LocalPrintServer.GetDefaultPrintQueue();

                var writer = PrintQueue.CreateXpsDocumentWriter(pq);

                // 同步打印输出
                writer.Write(fp);
            }
        }

        public static void PrintBitmap(Bitmap bmp)
        {
            PrintBitmapImage(BitmapHelper.ConvertToBitmapImage(bmp));
        }
    }
}
