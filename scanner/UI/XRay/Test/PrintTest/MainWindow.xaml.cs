using System;
using System.Drawing;
using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace UI.XRay.Test.PrintTest
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {

        }

        private void PrintButton_OnClick(object sender, RoutedEventArgs e)
        {
            //var bi = new BitmapImage();
            //bi.BeginInit();
            //bi.CacheOption = BitmapCacheOption.OnLoad;
            //bi.UriSource = new Uri("pack://application:,,,/desktop.png", UriKind.Absolute);
            //bi.EndInit();

            var dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == true)
            {
                var bmp = System.Drawing.Image.FromFile(dlg.FileName) as Bitmap;
                if (bmp != null)
                {
                    ImagePrinter.PrintBitmap(bmp);
                }
            }
        }
    }
}
