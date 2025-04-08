using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UI.Common.Tracers;
using UI.XRay.Common.Utilities;
using UI.XRay.Control;

namespace UI.XRay.Flows.Services
{
    /// <summary>
    /// 系统启动、关机服务
    /// </summary>
    public class SystemStartShutdownService
    {
        private const string AppStartupName = "XRayScanner";

        /// <summary>
        /// 启动系统自动开机、关机服务
        /// </summary>
        public static void StartService()
        {
            ControlService.ServicePart.SwitchStateChanged += ServicePartOnSwitchStateChanged;
        }

        /// <summary>
        /// 事件响应：安检机的某开关状态发生变化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private static void ServicePartOnSwitchStateChanged(object sender, SwitchStateChangedEventArgs args)
        {
            if (args.Switch == CtrlSysSwitch.PowerSwitch)
            {
                if (args.New == false)
                {
                    ControlService.ServicePart.SwitchStateChanged -= ServicePartOnSwitchStateChanged;
                    Tracer.TraceInfo("[Exit] Power Switch Off!");
                    // 钥匙开关被切换至Off：关机
                    Task.Run(() =>
                    {
                        Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            Application.Current.Shutdown();
                            WindowsHelper.Shutdown();
                        });
                    });
                }
            }
        }

        /// <summary>
        /// 关闭软件、计算机及安检机
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void Shutdown()
        {
            if (ControlService.ServicePart.ShutdownScanner())
            {
                Application.Current.Shutdown();
                WindowsHelper.Shutdown();
            }
        }

        /// <summary>
        /// 显示系统的文件管理器
        /// </summary>
         [MethodImpl(MethodImplOptions.Synchronized)]
        public static void ShowExplorer()
        {
            WindowsHelper.ShowExplorer();
        }

        /// <summary>
        /// 启动或禁用开机自动启动
        /// </summary>
        /// <param name="on"></param>
        public static void EnabledAutoStartup(bool on)
        {
            if (on)
            {
                var appPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UI.XRay.Security.Scanner.exe");
                AppStartupUtil.AddToStartupp(AppStartupName, appPath);
            }
            else
            {
                AppStartupUtil.RemoveFromStartup(AppStartupName);
            }
        }

        /// <summary>
        /// 是否开机自动启动安检软件
        /// </summary>
        /// <returns></returns>
        public static bool IsAutoStartupEnabled()
        {
            return AppStartupUtil.HasStartupItem(AppStartupName);
        }
    }
}
