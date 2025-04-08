using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;

namespace UI.XRay.Common.Utilities
{
    /// <summary>
    /// 操作系统功能辅助
    /// </summary>
    public static class WindowsHelper
    {
        /// <summary>
        /// 立即关闭计算机
        /// </summary>
        public static void Shutdown()
        {
            Process.Start("shutdown", "-s -t 0");
        }

        /// <summary>
        /// 显示系统文件管理器
        /// </summary>
        public static void ShowExplorer()
        {
            //Process.Start("explorer.exe");
            Process.Start(Path.Combine(Environment.GetEnvironmentVariable("windir"), "explorer.exe"));
        }

        [DllImport("user32.dll")]
        private static extern bool LockWorkStation();

        //[Conditional("Release")]
        public static void AutoLockWorkStation()
        {
            LockWorkStation();
        }

        [DllImport("kernel32.dll")]
        public static extern int SetProcessWorkingSetSize(IntPtr process, int minSize, int maxSize);
        public static void ClearMemory()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
            }
        }
    }
}
