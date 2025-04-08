using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight;

namespace UI.XRay.Security.Scanner.ViewModel
{
    /// <summary>
    /// 设置窗口中的导航页视图模型的基类。
    /// 增加了从主窗口中接收并处理按键的功能。
    /// </summary>
    public abstract class PageViewModelBase : ViewModelBase
    {
        public abstract void OnKeyDown(KeyEventArgs args);

        public virtual void OnPreviewKeyDown(KeyEventArgs args)
        {
            
        }

        public virtual void OnPreviewKeyUp(KeyEventArgs args)
        {

        }
    }
}
