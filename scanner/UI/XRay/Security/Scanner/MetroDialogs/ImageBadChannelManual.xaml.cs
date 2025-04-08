using GalaSoft.MvvmLight.Messaging;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using UI.XRay.Business.Entities;
using System.Collections.Generic;
using System.Windows.Shapes;
using System;
using System.Globalization;
using System.IO;
using System.Xml.Serialization;

namespace UI.XRay.Security.Scanner.MetroDialogs
{
    /// <summary>
    /// ImageBadChannelManual.xaml 的交互逻辑
    /// </summary>
    public partial class ImageBadChannelManual : Window
    {
        public ImageBadChannelManual()
        {
            InitializeComponent();
            this.PreviewKeyDown += OnPreviewKeyDown;
            this.Loaded += OnLoaded;

            //接受来自ViewModel的数据
            Messenger.Default.Register<List<ChannelBadFlag>>(this, m => ChannelBadFlagList = m);

            WindowFocusHelper.MakeFocus(this);
        }
        readonly TaskCompletionSource<object> _taskCompletionSource = new TaskCompletionSource<object>();
        public ImageBadChannelManual(MetroWindow owningWindow, ImageRecord record)
        {
            InitializeComponent();

            this.PreviewKeyDown += OnPreviewKeyDown;
            this.Loaded += OnLoaded;

            //向ViewModel传递图片的Record
            var vm = DataContext as ViewModel.ImageBadChannelManualViewModel;
            if (vm == null) return;
            vm.ImageRecord = record;


            //接受来自ViewModel的数据
            Messenger.Default.Register<List<ChannelBadFlag>>(this, m => ChannelBadFlagList = m);

            WindowFocusHelper.MakeFocus(this);
        }

        private int _imageHeight;

        private List<ChannelBadFlag> _channelBadFlagList;

        public List<ChannelBadFlag> ChannelBadFlagList
        {
            get { return _channelBadFlagList; }
            set
            {
                _channelBadFlagList = value;
                _imageHeight = ChannelBadFlagList.Count;
                ClearLinesOnCanvas();

                if (_channelBadFlagList.Count > 0)
                {
                    DisplayLinesOnCanvas();
                }

            }
        }

        private void DisplayLinesOnCanvas()
        {
            for (int i = 0; i < _channelBadFlagList.Count; i++)
            {
                if (_channelBadFlagList[i].IsBad)
                {
                    var tmpline = new Line();
                    tmpline.Stroke = new SolidColorBrush(Colors.Red);
                    tmpline.X1 = 0;
                    tmpline.X2 = ImgCanvas.ActualWidth;
                    tmpline.Y1 = (_channelBadFlagList[i].ChannelNumber + 0.5);
                    tmpline.Y2 = (_channelBadFlagList[i].ChannelNumber + 0.5);
                    ImgCanvas.Children.Add(tmpline);
                }

            }

        }
        private void ClearLinesOnCanvas()
        {
            for (int i = 0; i < ImgCanvas.Children.Count; i++)
            {
                if (ImgCanvas.Children[i] is Line)
                {
                    ImgCanvas.Children.RemoveAt(i);
                    i--;
                }
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Focus();
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
        private async void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as ViewModel.ImageBadChannelManualViewModel;
            if (vm == null) return;
            vm.DisposeBitmap();
            this.Close();
            //await OwningWindow.HideMetroDialogAsync(this);
            //_taskCompletionSource.TrySetResult(null);

        }

        private void AddBadChannelsToList(IEnumerable<ChannelBadFlag> viewBadChannels, List<ChannelBadFlag> summarizedViewBadChannels)
        {
            if (viewBadChannels == null)
            {
                return;
            }
            summarizedViewBadChannels.Clear();
            //将自动检测到的视角1的坏点探测通道编号添加到所有坏点索引链表
            foreach (var viewBadChannel in viewBadChannels)
            {
                summarizedViewBadChannels.Add(viewBadChannel);
            }
        }

        Point _previousMousePoint = new Point(0, 0);

        private void XRayImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //_isMouseLeftButtonDown = true;
            _previousMousePoint = e.GetPosition(XRayImage);
            PointTextBlock.Text = Math.Floor(_previousMousePoint.Y).ToString(CultureInfo.InvariantCulture);
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            this.Topmost = true;
            WindowFocusHelper.MakeFocus(this);
        }
    }

}
