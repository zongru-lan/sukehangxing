using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using UI.XRay.Business.Entities;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Scanner.MetroDialogs
{
    public static class MetroDialogExtensions
    {
        /// <summary>
        /// 在窗口中异步显示输入当前用户密码的对话框
        /// </summary>
        /// <param name="window"></param>
        /// <param name="accountId">用户名</param>
        /// <returns></returns>
        public static async Task<PasswordMetroDialogResult> ShowPasswordDialogAsync(this MetroWindow window, string accountId, 
            MetroDialogButtons buttons = MetroDialogButtons.OkCancel)
        {
            var dlg = new PasswordMetroDialog(window, buttons, accountId);
            await window.ShowMetroDialogAsync(dlg);
            return await dlg.WaitForButtonPressAsync();
        }

        public static async Task<MetroDialogResult> ShowMessageDialogAsync(this MetroWindow window, string title, string message, 
            MetroDialogButtons buttons = MetroDialogButtons.OkNo)
        {
            var dlg = new MessageMetroDialog(window,buttons, title, message);
            await window.ShowMetroDialogAsync(dlg);
            return await dlg.WaitForButtonPressAsync();
        }

        /// <summary>
        /// 在MetroWindow中以MetroDialog的形式显示一个图像
        /// </summary>
        /// <param name="window"></param>
        /// <param name="record"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        public static async Task ShowImageDialogAsync(this MetroWindow window, ImageRecord record,
            BitmapSource image)
        {
            var dlg = new ImageMetroDialog(window, record, image);
            dlg.Height = window.ActualHeight - 100;
            await window.ShowMetroDialogAsync(dlg);
            await dlg.WaitForButtonPressAsync();
        }

        /// <summary>
        /// 在MetroWindow中以MetroDialog的形式显示图像坏点手动剔除界面
        /// </summary>
        /// <param name="window"></param>
        /// <param name="record"></param>
        /// <returns></returns>
        public static async Task ShowBadChannelDialogAsync(this MetroWindow window, ImageRecord record)
        {
            //var dlg = new ImageBadChannelManual(window, record);
            //dlg.Height = window.ActualHeight - 100;
            //dlg.Width = window.ActualWidth;
            //await window.ShowMetroDialogAsync(dlg);
            //await dlg.WaitForButtonPressAsync();
        }
    }
}
