namespace UI.XRay.Security.Configer
{
    /// <summary>
    /// 定义视图模型ViewModel应该实现的接口，该接口定义个视图模型的通用接口，如加载、保存、数据变化标志
    /// </summary>
    public interface IViewModel
    {
        /// <summary>
        /// 将更改保存到本地， 
        /// </summary>
        void SaveSettings();

        /// <summary>
        /// 是否Refresh更合适，从本地加载配置
        /// </summary>
        void LoadSettings();
    }
}
