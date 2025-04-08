using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using UI.XRay.Parts.Keyboard;

namespace UI.XRay.Security.Scanner
{
    /// <summary>
    /// ScreenImagesOperationWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ScreenImagesOperationWindow 
    {
        public ScreenImagesOperationWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ScreenImagesOperationWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == ScannerKeyboardPart.Keyboard.F3 || e.Key == ScannerKeyboardPart.Keyboard.Esc ||
                e.Key == Key.Escape)
            {
                this.Close();
            }
        }
    }
}
