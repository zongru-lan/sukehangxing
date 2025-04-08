using System;

namespace UI.XRay.Business.Entities
{
    /// <summary>
    /// 表示一次Tip注入操作，其中定义注入时使用的Tip图像，注入的起始位置以及Tip图像的Flip、Rotate等属性
    /// </summary>
    [Serializable]
    public class TipInjection
    {
        /// <summary>
        /// 构造一次Tip注入的实例
        /// </summary>
        /// <param name="tipImage">Tip注入时所使用的图像对象，即Tip图像</param>
        /// <param name="regionInImage">Tip注入位置：在宿主图像中的位置信息</param>
        public TipInjection(XRayScanlinesImage tipImage, MarkerRegion regionInImage)
        {
            TipImage = tipImage;
            RegionInImage = regionInImage;
        }

        /// <summary>
        /// 注入时所使用的Tip图像
        /// </summary>
        public XRayScanlinesImage TipImage { get; set; }

        /// <summary>
        /// Tip注入的起始探测通道编号
        /// </summary>
        public int StartChannel
        {
            get { return RegionInImage.FromChannel; }
        }

        /// <summary>
        /// Tip注入的起始扫描线编号，即在被注入图像中的扫描线的编号
        /// </summary>
        public int StartScanLine
        {
            get { return RegionInImage.FromLine; }
        }

        /// <summary>
        /// Tip图像在宿主图像中的具体位置信息
        /// </summary>
        public MarkerRegion RegionInImage { get; private set; }
    }

    /// <summary>
    /// 表示一次Tip注入完成事件，包含被注入的Tip图像，以及注入的起始位置
    /// </summary>
    public class TipInjectionEventArgs : EventArgs
    {
        public TipInjectionEventArgs(XRayScanlinesImage tipImage, MarkerRegion injectRegion, MarkerRegion injectRegion2)
        {
            TipImage = tipImage;
            InjectRegion = injectRegion;
            InjectRegion2 = injectRegion2;
        }

        /// <summary>
        /// 注入时所使用的Tip图像
        /// </summary>
        public XRayScanlinesImage TipImage { get; private set; }

        /// <summary>
        /// 此次注入在全局数据中的位置
        /// </summary>
        public MarkerRegion InjectRegion { get; private set; }

        /// <summary>
        /// 视角2
        /// </summary>
        public MarkerRegion InjectRegion2 { get; private set; }
    }
}