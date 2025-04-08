using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace UI.XRay.Common.Utilities
{
    /// <summary>
    /// 软件开机自启动工具
    /// </summary>
    public static class AppStartupUtil
    {
        /// <summary>
        /// 将指定的exe添加到开机启动项
        /// </summary>
        /// <param name="exeName">exe名称，将在开机启动项中记录</param>
        /// <param name="exePath">exe文件的完整路径</param>
        public static void AddToStartupp(string exeName, string exePath)
        {
            RegistryKey currentUser = Registry.LocalMachine;
            RegistryKey run =
                currentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true) ??
                currentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");

            if (run != null)
            {
                run.SetValue(exeName, exePath, RegistryValueKind.String);
            }
        }

        /// <summary>
        /// 从开机启动项中移除指定的启动项名称
        /// </summary>
        /// <param name="exeName"></param>
        public static void RemoveFromStartup(string exeName)
        {
            RegistryKey currentUser = Registry.LocalMachine;
            RegistryKey run = currentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

            try
            {
                if (run != null)
                {
                    run.DeleteValue(exeName);
                }
            }
            catch (Exception e)
            {
                
            }
        }

        /// <summary>
        /// 判断是否存在某个开机启动项
        /// </summary>
        /// <param name="exeName"></param>
        /// <returns></returns>
        public static bool HasStartupItem(string exeName)
        {
            RegistryKey currentUser = Registry.LocalMachine;
            RegistryKey run = currentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

            if (run != null)
            {
                var val = run.GetValue(exeName);
                if (val != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
