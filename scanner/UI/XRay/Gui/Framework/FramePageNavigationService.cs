using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using GalaSoft.MvvmLight;
using UI.Common.Tracers;

namespace UI.XRay.Gui.Framework
{
    /// <summary>
    /// 基于窗口的页导航服务：需要在主窗口初始化时，设置其用于导航的Frame
    /// </summary>
    public class FramePageNavigationService : IFramePageNavigationService
    {
        #region Fields
        private readonly Dictionary<string, Uri> _pagesByKey;
        private readonly List<PageNavigation> _navigations = new List<PageNavigation>(2);
        private string _currentPageKey;  
        #endregion

        public event EventHandler<PageNavigationEventArgs> PageNavigated;

        public PageNavigation CurrentPageNavigation { get; private set; }

        #region Properties                                              
        public string CurrentPageKey
        {
            get
            {
                return _currentPageKey;
            }

            private  set
            {
                if (_currentPageKey == value)
                {
                    return;
                }

                _currentPageKey = value;
                //OnPropertyChanged("CurrentPageKey");
            }
        }
        public object Parameter { get; private set; }
        #endregion

        #region Ctors and Methods
        public FramePageNavigationService()
        {
            _pagesByKey = new Dictionary<string, Uri>();
        }

        public void GoBack()
        {
            if (_navigations.Count > 1)
            {
                _navigations.RemoveAt(_navigations.Count - 1);
                ShowPage(_navigations.Last());
            }
        }

        public void ShowPage(string pageKey, object param = null)
        {
            lock (_pagesByKey)
            {
                if (!string.IsNullOrEmpty(pageKey))
                {
                    if (!_pagesByKey.ContainsKey(pageKey))
                        throw new ArgumentException(string.Format("No such page: {0} ", pageKey), "pageKey");

                    var frame = PageFrame;
                    var uri = _pagesByKey[pageKey];
                    if (frame != null && frame.Source != uri)
                    {
                        var page = PageFrame.Content as Page;
                        if (page != null)
                        {
                            var vmCleanup = page.DataContext as ICleanup;
                            if (vmCleanup != null)
                            {
                                try
                                {
                                    vmCleanup.Cleanup();
                                }
                                catch (Exception exception)
                                {
                                    Tracer.TraceException(exception, "Exception occured when call viewmodel' Cleanup. " + vmCleanup.ToString());
                                }
                            }
                        }
                        frame.Source = _pagesByKey[pageKey];

                        Parameter = param;

                        CurrentPageKey = pageKey;
                        if (CurrentPageNavigation == null)
                        {
                            CurrentPageNavigation = new PageNavigation(string.Empty, pageKey, pageKey);
                        }
                        else
                        {
                            CurrentPageNavigation.PageKey = pageKey;
                        }

                        // 将当前页加入到页列表中
                        _navigations.Add(new PageNavigation(CurrentPageNavigation.MenuKey, 
                            CurrentPageNavigation.PageKey, CurrentPageNavigation.Title));

                        if (PageNavigated != null)
                        {
                            PageNavigated(this, new PageNavigationEventArgs(CurrentPageNavigation));
                        }
                    }
                }
            }
        }

        public void ShowPage(PageNavigation navigation, object param = null)
        {
            lock (_pagesByKey)
            {
                if (!string.IsNullOrEmpty(navigation.MenuKey))
                {
                    if (!_pagesByKey.ContainsKey(navigation.MenuKey))
                    {
                        throw new ArgumentException(string.Format("No such menu: {0} ", navigation.MenuKey), "navigation");
                    }

                    var frame = MenuFrame;
                    var uri = _pagesByKey[navigation.MenuKey];

                    // 如果两次的Uri相同，则不会更新
                    if (frame != null && frame.Source != uri)
                    {
                        frame.Source = _pagesByKey[navigation.MenuKey];
                    }
                }
                else
                {
                    var frame = MenuFrame;
                    if (frame != null)
                    {
                        frame.Source = null;
                    }
                }

                if (!string.IsNullOrEmpty(navigation.PageKey))
                {
                    if (!_pagesByKey.ContainsKey(navigation.PageKey))
                        throw new ArgumentException(string.Format("No such page: {0} ", navigation.PageKey), "navigation");

                    var frame = PageFrame;
                    var uri = _pagesByKey[navigation.PageKey];
                    if (frame != null && frame.Source!= uri)
                    {
                        var page = PageFrame.Content as Page;
                        if (page != null)
                        {
                            var vmCleanup = page.DataContext as ICleanup;
                            if (vmCleanup != null)
                            {
                                try
                                {
                                    vmCleanup.Cleanup();
                                }
                                catch (Exception exception)
                                {
                                    Tracer.TraceException(exception, "Exception occured when call viewmodel' Cleanup. " + vmCleanup.ToString());
                                }
                            }
                        }
                        frame.Source = _pagesByKey[navigation.PageKey];

                        Parameter = param;

                        
                        CurrentPageKey = navigation.PageKey;

                        if (PageNavigated != null)
                        {
                            PageNavigated(this, new PageNavigationEventArgs(navigation));
                        }
                        CurrentPageNavigation = navigation;
                        _navigations.Add(new PageNavigation(CurrentPageNavigation.MenuKey,
                            CurrentPageNavigation.PageKey, CurrentPageNavigation.Title));

                    }
                }
            }
        }

        ///// <summary>
        ///// 导航至pageKey指定的Page
        ///// </summary>
        ///// <param name="pageKey"></param>
        ///// <param name="parameter"></param>
        //public virtual void NavigateTo(string pageKey, object parameter)
        //{
        //    lock (_pagesByKey)
        //    {
        //        if (!_pagesByKey.ContainsKey(pageKey))
        //        {
        //            throw new ArgumentException(string.Format("No such page: {0} ", pageKey), "pageKey");
        //        }

        //        // todo: 需要解决窗口的问题
        //        //var frame = GetDescendantFromName(Application.Current.MainWindow, "MainFrame") as Frame;

        //        //if (frame != null)
        //        //{
        //        //    frame.Source = _pagesByKey[pageKey];
        //        //}

        //        // 获取一个对当前用于导航的Frame的强引用
        //        var frame = MenuFrame;

        //        if (frame != null)
        //        {
        //            // 在导航至新页面之前，先获取当前页，调用当前页的ViewModel的ICleanup接口来完成ViewModel的清理工作。
        //            var page = MenuFrame.Content as Page;
        //            if (page != null)
        //            {
        //                var vmCleanup = page.DataContext as ICleanup;
        //                if (vmCleanup != null)
        //                {
        //                    try
        //                    {
        //                        vmCleanup.Cleanup();
        //                    }
        //                    catch (Exception exception)
        //                    {
        //                        Tracer.TraceException(exception, "Exception occured when call viewmodel' Cleanup. " + vmCleanup.ToString());
        //                    }
        //                }
        //            }

        //            Parameter = parameter;
        //            frame.Source = _pagesByKey[pageKey];

        //            _historic.Add(pageKey);
        //            CurrentPageKey = pageKey;

        //            if (PageNavigated != null)
        //            {
        //                PageNavigated(this, new PageNavigationEventArgs(CurrentPageKey));
        //            }
        //        }
        //    }
        //}

        /// <summary>
        /// 添加一个Page
        /// </summary>
        /// <param name="key">Page 名称，作为标志</param>
        /// <param name="pageType">Page的路径</param>
        public void Configure(string key, Uri pageType)
        {
            lock (_pagesByKey)
            {
                if (_pagesByKey.ContainsKey(key))
                {
                    _pagesByKey[key] = pageType;
                }
                else
                {
                    _pagesByKey.Add(key, pageType);
                }
            }
        }

        //private static FrameworkElement GetDescendantFromName(DependencyObject parent, string name)
        //{
        //    var count = VisualTreeHelper.GetChildrenCount(parent);

        //    if (count < 1)
        //    {
        //        return null;
        //    }

        //    for (var i = 0; i < count; i++)
        //    {
        //        var frameworkElement = VisualTreeHelper.GetChild(parent, i) as FrameworkElement;
        //        if (frameworkElement != null)
        //        {
        //            if (frameworkElement.Name == name)
        //            {
        //                return frameworkElement;
        //            }

        //            frameworkElement = GetDescendantFromName(frameworkElement, name);
        //            if (frameworkElement != null)
        //            {
        //                return frameworkElement;
        //            }
        //        }
        //    }
        //    return null;
        //}

        //public event PropertyChangedEventHandler PropertyChanged;

        //[NotifyPropertyChangedInvocator]
        //protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        //{
        //    PropertyChangedEventHandler handler = PropertyChanged;
        //    if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        //}
        #endregion

        /// <summary>
        /// 指向用于导航的Frame对象的弱引用
        /// </summary>
        private WeakReference<Frame> _menuFrame;

        private WeakReference<Frame> _pageFrame;

        /// <summary>
        /// 获取用于导航的Frame对象，可能为空
        /// </summary>
        private Frame MenuFrame
        {
            get
            {
                Frame frame;
                if (_menuFrame != null && _menuFrame.TryGetTarget(out frame))
                {
                    return frame;
                }
                return null;
            }
        }

        private Frame PageFrame
        {
            get
            {
                Frame frame;
                if (_pageFrame != null && _pageFrame.TryGetTarget(out frame))
                {
                    return frame;
                }
                return null;
            }
        }

        public void SetFrame(Frame menuFrame, Frame pageFrame)
        {
            _menuFrame = new WeakReference<Frame>(menuFrame);
            _pageFrame = new WeakReference<Frame>(pageFrame);
            //if (!string.IsNullOrEmpty(defaultPage))
            //{
            //    NavigateTo(defaultPage);
            //}
        }
    }
}
