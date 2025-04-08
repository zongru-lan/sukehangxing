using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UI.XRay.Flows.Controllers;
using UI.XRay.Security.Scanner.ViewModel.Setting.Image;
using UI.XRay.Security.Scanner.ViewModel;
using System.Collections.Generic;
using UI.XRay.Business.Entities;
using System.Linq;
using UI.XRay.Flows.Services;
using UI.Common.Tracers;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using UI.XRay.RenderEngine;
using System.Linq.Expressions;
using System;
using System.IO;
using Path = System.IO.Path;
using System.Collections.Concurrent;
using UI.XRay.Business.Algo;
using UI.XRay.Business.DataAccess.Config;

namespace UI.XRay.Security.Scanner.SettingViews.Image.Pages
{
    /// <summary>
    /// ImgPage.xaml 的交互逻辑
    /// </summary>
    public partial class ImsPage : Page
    {

        private bool AllowDoubleClick;
        public ImsPage()
        {
            InitializeComponent();
            LoadSettings();

        }
        private void LoadSettings()
        {
            try 
            {
                if (!ScannerConfig.Read(ConfigPath.AllowDC, out AllowDoubleClick))
                {
                    AllowDoubleClick = false;
                }
            }
            catch (Exception e)
            { 
                Tracer.TraceException(e);
            }
        }
        /// <summary>
        /// 用户单击了一个图像的 打开 按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            // 按钮的DataContext，对应的是此ListBoxItem的对应的VisualBindingImageItem对象
            var imageItem = (sender as FrameworkElement).DataContext as BindableImage;

            // todo: 考虑使用分裂按钮，在分裂按钮中增加功能：View、Replay、Delete
            // 通过Mvvm信使机制，发送消息给ViewModel，由ViewModel读取并驱动显示图像
        }

        private void ImsPage_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            
        }

        private void DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(AllowDoubleClick==false)
            {
                return;
            }
            switch (e.ClickCount)
            {
                case 2: //双击
                    {
                     
                        if (ViewModelLocator.Locator.ImsPage.CurrentImages != null)
                        {

                            List<BindableImage> selItems = new List<BindableImage>();
                            if (ImageListBox.SelectedItems != null)
                            {
                                foreach (var item in ImageListBox.SelectedItems)
                                {
                                    selItems.Add((BindableImage)item);
                                }
                            }
                            else
                            {
                                e.Handled = true;
                                break;
                            }
                        if(selItems.Count>0)
                            {
                                RenderEngine1(selItems[selItems.Count-1].ImagePath);    //第一次双击的图片
                            }

                        selItems.Clear();  //每次弹出窗口后清空List

                        }
                        e.Handled = true;
                        break;
                    }
            }
        }
       
        private void RenderEngine1(string imagepath)
        {
            Transmission.ImagePath1 = imagepath;
            ViewModelLocator.Locator.RenderEngine22Window.Show();
            
            WindowFocusHelper.Pause();
                    
        }

    }
}
