using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using UI.XRay.Business.Entities;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Scanner.MetroDialogs
{
    /// <summary>
    /// 图像预览Metro对话框，用于在图像管理页面中，打开预览图像。
    /// </summary>
    public partial class ImageMetroDialog 
    {
        readonly TaskCompletionSource<object> _taskCompletionSource = new TaskCompletionSource<object>();

        public ImageMetroDialog(MetroWindow owningWindow, ImageRecord record, BitmapSource image)
            : base(owningWindow, null)
        {
            InitializeComponent();

            XRayImage.Source = image;
            ScanTimeTextBlock.Text = record.ScanTime.ToString("G");

            this.PreviewKeyDown += OnPreviewKeyDown;
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            this.Focus();
        }

        /// <summary>
        /// 按键事件处理：F1为 确定，F2为 取消
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnPreviewKeyDown(object sender, KeyEventArgs args)
        {
            switch (args.Key)
            {
                case Key.F1:
                case Key.Enter:
                    RotateButton_OnClick(null, null);
                    args.Handled = true;
                    break;

                case Key.Escape:
                case Key.F3:
                    CancelButton_OnClick(this, null);
                    args.Handled = true;
                    break;

                    // 屏蔽F4键，防止返回至前一页
                case Key.F4:
                    args.Handled = true;
                    break;

                default:
                    args.Handled = true;
                    break;
            }

            // 屏蔽针对宿主窗口的所有按键消息
        }

        /// <summary>
        /// 异步等待用户按下按键或者点击按钮后关闭对话框，并返回结果
        /// </summary>
        /// <returns></returns>
        public Task WaitForButtonPressAsync()
        {
            return _taskCompletionSource.Task;
        }

        /// <summary>
        /// 事件响应：用户点击 取消，取消此任务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private async void CancelButton_OnClick(object sender, RoutedEventArgs e)
        //{
        //    await OwningWindow.HideMetroDialogAsync(this);
        //    _taskCompletionSource.TrySetResult(null);
        //}

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            OwningWindow.Hide();
            _taskCompletionSource.TrySetResult(null);
        }

        private int _currentAngle = 0;

        private void RotateButton_OnClick(object sender, RoutedEventArgs e)
        {
            _currentAngle += 90;
            XRayImage.LayoutTransform = new RotateTransform(_currentAngle);
        }
    }
}
