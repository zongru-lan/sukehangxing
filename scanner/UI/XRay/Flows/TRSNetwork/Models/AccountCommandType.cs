using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Flows.TRSNetwork.Models
{
    /// <summary>
    /// 账户相关指令枚举
    /// </summary>
    public enum AccountCommandType
    {
        /// <summary>
        /// 远程客户端获取账户信息
        /// </summary>
        GetAccounts,
        /// <summary>
        /// 远程客户端获取当前登录账户
        /// </summary>
        GetCurrentAccount,
        /// <summary>
        /// 远程客户端登录到本地安检机
        /// </summary>
        RemoteLoginAccount,
        /// <summary>
        /// 远程客户端修改账户信息
        /// </summary>
        UpdateAccounts,
        /// <summary>
        /// 导出账户到对端
        /// </summary>
        SendAccounts,
        /// <summary>
        /// 向远程客户端发送当前账户
        /// </summary>
        SendCurrentAccount,
        /// <summary>
        /// 验证安检机本地用户登录
        /// </summary>
        VerifyUserLogin,
        /// <summary>
        /// 验证安检机本地用户登出
        /// </summary>
        VerifyUserExit,
        /// <summary>
        /// 导出账户返回
        /// </summary>
        SendAccountsOver,
        /// <summary>
        /// 导入账户返回
        /// </summary>
        ImportAccountsOver,
    }
}
