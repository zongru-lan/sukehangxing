using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using GalaSoft.MvvmLight.Messaging;
using MahApps.Metro.Controls;
using UI.XRay.Business.Entities;
using UI.XRay.Common.Utilities;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Scanner
{
    /// <summary>
    /// ImageRetrievalWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ImageRetrievalWindow 
    {
        readonly TaskCompletionSource<ImageRetrievalConditions> _taskCompletionSource = new TaskCompletionSource<ImageRetrievalConditions>();

        public ImageRetrievalWindow()
        {
            InitializeComponent();

            Messenger.Default.Register<ImageRetrievalConditionsMessage>(this, SetConditionsMessageAction);

            // 定时将窗口置顶，捕获按键消息
            WindowFocusHelper.MakeFocus(this);

            // 窗口关闭后，尝试再次设置任务为完成状态，防止用户使用Alt F4等方式强行关闭窗口，保证任务必须结束
            this.Closed += (sender, args) => _taskCompletionSource.TrySetResult(null);
        }

        private void SetConditionsMessageAction(ImageRetrievalConditionsMessage message)
        {
            _taskCompletionSource.TrySetResult(message.Conditions);
            this.Close();
        }

        /// <summary>
        /// 异步等待用户按下按键或者点击按钮后关闭对话框，并返回结果
        /// </summary>
        /// <returns></returns>
        public Task<ImageRetrievalConditions> WaitForButtonPressAsync()
        {
            return _taskCompletionSource.Task;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)  //EndHour
        {
            if(StartTimePicker == null || EndTimePicker == null || StartHour == null || EndHour == null)
            {
                return;
            }
            if (StartTimePicker.SelectedDate>=EndTimePicker.SelectedDate&&StartHour.SelectedIndex>=EndHour.SelectedIndex)
            {
               EndHour.SelectedValue = StartHour.SelectedIndex;
            }
          
        }

        private void ComboBox_SelectionChanged2(object sender, SelectionChangedEventArgs e) //EndMinute
        {
            if(StartTimePicker==null|| EndTimePicker==null||StartHour==null||StartMinute==null|| EndHour==null|| EndMinute==null)
            {
                return;
            }
            if(StartTimePicker.SelectedDate >= EndTimePicker.SelectedDate && StartHour.SelectedIndex>=EndHour.SelectedIndex&&StartMinute.SelectedIndex>=EndMinute.SelectedIndex)
            {
                EndMinute.SelectedValue= StartMinute.SelectedIndex;    
            }
        }

        private void StartHour_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StartTimePicker == null || EndTimePicker == null || StartHour == null || EndHour == null)
            {
                return;
            }
            if (StartTimePicker.SelectedDate >= EndTimePicker.SelectedDate && StartHour.SelectedIndex >= EndHour.SelectedIndex)
            {                
                StartHour.SelectedValue = EndHour.SelectedIndex;
            }
        }

        private void StartMinute_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StartTimePicker == null || EndTimePicker == null || StartHour == null || StartMinute == null || EndHour == null || EndMinute == null)
            {
                return;
            }
            if (StartTimePicker.SelectedDate >= EndTimePicker.SelectedDate && StartHour.SelectedIndex >=EndHour.SelectedIndex && StartMinute.SelectedIndex >= EndMinute.SelectedIndex)
            {
                StartMinute.SelectedValue = EndMinute.SelectedIndex;
            }
        }

        private void StartTimePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if(StartTimePicker==null||EndTimePicker==null)
            {
                return;
            }
            if(StartTimePicker.SelectedDate>=EndTimePicker.SelectedDate)
            {
               StartTimePicker.SelectedDate= EndTimePicker.SelectedDate;
            }
        }
    }
}
