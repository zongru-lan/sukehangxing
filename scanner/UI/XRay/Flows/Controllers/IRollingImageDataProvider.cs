using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Business.Entities;

namespace UI.XRay.Flows.Controllers
{
    internal interface IRollingImageDataProvider
    {
        /// <summary>
        /// 数据准备完毕
        /// </summary>
        event Action<DisplayScanlineDataBundle> DataReady;

        /// <summary>
        /// 提供图像回拉数据
        /// </summary>
        /// <returns></returns>
        IEnumerable<DisplayScanlineDataBundle> PullBack();

        /// <summary>
        /// 提供图像前拉的数据
        /// </summary>
        /// <returns></returns>
        IEnumerable<DisplayScanlineDataBundle> PullForward();

        /// <summary>
        /// 获取所有当前正在显示中的图像
        /// </summary>
        /// <returns></returns>
        IEnumerable<ImageRecord> GetShowingImages();
    }
}
