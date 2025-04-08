using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Security.Configer.Interfaces;

namespace UI.XRay.Security.Configer.ViewModel
{
    /// <summary>
    /// 表示软件显示设置视图的ViewModel，暂时没有相关设置
    /// </summary>
    public class ApperanceSettingsViewModel : ViewModelBase, IViewModel
    {
        public void SaveSettings()
        {
            
        }

        public void LoadSettings()
        {
            
        }

        public bool DataChanged { get; private set; }
    }
}
