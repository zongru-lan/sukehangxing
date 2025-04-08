using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Flows.TRSNetwork.Models
{
    /// <summary>
    /// TIP相关指令枚举
    /// </summary>
    public enum TIPCommandType
    {
        /// <summary>
        /// 清空图像
        /// </summary>
        ClearTIPImage,
        /// <summary>
        /// 远程客户端获取TIP计划
        /// </summary>
        GetTIPPlan,
        /// <summary>
        /// 远程客户端获取TIP图像库
        /// </summary>
        GetTIPImageLibraries,
        /// <summary>
        /// 远程客户端获取TIP图像信息
        /// </summary>
        GetTIPImages,
        /// <summary>
        /// 远程客户端获取TIP日志
        /// </summary>
        GetTIPLogs,
        /// <summary>
        /// 远程客户端更新TIP计划
        /// </summary>
        UpdateTIPPlan,
        /// <summary>
        /// 远程客户端修改TIP图像库
        /// </summary>
        UpdateTIPImageLibraries,
        /// <summary>
        /// 远程客户端修改TIP图像信息
        /// </summary>
        UpdateTIPImages,
        /// <summary>
        /// 远程客户端增加TIP图像(XRay)
        /// </summary>
        AddTIPImage,

    }
}
