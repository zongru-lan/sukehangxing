using System;
using System.Windows.Controls;

namespace UI.XRay.Gui.Framework
{
    public class PageNavigationEventArgs : EventArgs
    {
        public PageNavigation Navigation { get; private set; }

        public PageNavigationEventArgs(PageNavigation pageKey)
        {
            Navigation = pageKey;
        }
    }

    /// <summary>
    /// 页导航消息
    /// </summary>
    public class PageNavigation 
    {
        public string Title { get; private set; }

        public string MenuKey { get; private set; }

        public string PageKey { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="menuKey">要显示的菜单页的Key</param>
        /// <param name="pageKey">要显示的配置页的Key</param>
        /// <param name="title">窗口将要显示的标题</param>
        public PageNavigation(string menuKey, string pageKey, string title)
        {
            MenuKey = menuKey;
            PageKey = pageKey;
            Title = title;
        }
    }

    /// <summary>
    /// 接口：用于一个窗口中的Page导航
    /// </summary>
    public interface IFramePageNavigationService
    {
        event EventHandler<PageNavigationEventArgs> PageNavigated;

        PageNavigation CurrentPageNavigation { get; }

        string CurrentPageKey { get; }

        /// <summary>
        /// 设置窗口中的Frame对象，通过此对象实现导航
        /// </summary>
        /// <param name="menuFrame"></param>
        /// <param name="pageFrame">默认显示的Page</param>
        void SetFrame(Frame menuFrame, Frame pageFrame);

        /// <summary>
        /// 显示指定的菜单和页：包括菜单页和设置页
        /// </summary>
        /// <param name="navigation"></param>
        /// <param name="param"></param>
        void ShowPage(PageNavigation navigation, object param = null);

        /// <summary>
        /// 显示指定的页：包括设置页，不更新菜单，也不更新标题
        /// </summary>
        /// <param name="pageKey"></param>
        /// <param name="param"></param>
        void ShowPage(string pageKey, object param = null);

        void GoBack();
    }
}
