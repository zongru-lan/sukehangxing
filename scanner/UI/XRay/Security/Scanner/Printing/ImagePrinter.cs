using System;
using System.Drawing;
using System.Printing;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UI.XRay.Common.Utilities;
using Image = System.Windows.Controls.Image;
using Point = System.Windows.Point;

namespace UI.XRay.Security.Scanner.Printing
{
    /// <summary>
    /// 图像打印机：提供用于打印Bitmap图像的功能
    /// </summary>
    public class ImagePrinter
    {
        public static void PrintBitmapImageAsync(BitmapImage bi, string machineNumber, DateTime? scanTime, string accountId)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                //读出用于图像打印的固定页面
                //var stream = Assembly.GetExecutingAssembly()
                //    .GetManifestResourceStream("UI.XRay.Common.Utilities.Printing.ImagePrintPage.xaml");

                FixedPage fp = Application.LoadComponent(new Uri("/Printing/ImagePrintPage.xaml", UriKind.Relative)) as FixedPage;
                // FixedPage fp = XamlReader.Load(stream) as FixedPage;

                var numberLabel = fp.FindName("MachineNumberLabel") as Label;
                if (numberLabel != null)
                {
                    numberLabel.Content = machineNumber;
                }

                var scanTimeLabel = fp.FindName("ScanTimeLabel") as Label;
                if (scanTimeLabel != null && scanTime != null)
                {
                    scanTimeLabel.Content = scanTime.Value.ToString("G");
                }

                var accountLabel = fp.FindName("AccountLabel") as Label;
                if (accountLabel != null && scanTime != null)
                {
                    accountLabel.Content = accountId;
                }

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
                    var a4Size = new System.Windows.Size(96 * 8.5, 96 * 11);

                    // Update the layout of our FixedPage
                    fp.Measure(a4Size);
                    fp.Arrange(new Rect(new Point(), a4Size));
                    fp.UpdateLayout();

                    // 获取默认的打印机
                    var pq = LocalPrintServer.GetDefaultPrintQueue();

                    var writer = PrintQueue.CreateXpsDocumentWriter(pq);

                    // 同步打印输出
                    writer.WriteAsync(fp);
                }
            });
        }


        /// <summary>
        /// 打印一张图像
        /// </summary>
        /// <param name="bmp"></param>
        public static void PrintBitmapAsync(Bitmap bmp)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                PrintBitmapImageAsync(BitmapHelper.ConvertToBitmapImage(bmp), null, null, null);
            });
        }

        /// <summary>
        /// 打印一张图像及其相关信息
        /// </summary>
        /// <param name="bmp">要打印的图像</param>
        /// <param name="machineNumber">设备编号</param>
        /// <param name="scanTime">图像扫描的时间</param>
        public static void PrintBitmapAsync(Bitmap bmp, string machineNumber, DateTime? scanTime, string accountId)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                PrintBitmapImageAsync(BitmapHelper.ConvertToBitmapImage(bmp), machineNumber, scanTime, accountId);
            });
        }
    }
}
