using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using UI.XRay.Common.Utilities;

namespace UI.XRay.Security.Scanner
{
    public static class WindowFocusHelper
    {
        private static bool _bePaused = false;

        [Conditional("RELEASE")]
        public static void MakeFocus(Window window)
        {
            // 定时将窗口置顶，捕获按键消息
            var timer = new DispatcherTimer(TimeSpan.FromSeconds(10), DispatcherPriority.Normal, (sender, args) =>
            {
                if (!_bePaused)
                {
                    if (window.OwnedWindows.Count == 0)
                    {
                        var wndHelper = new WindowInteropHelper(window);
                        NativeMethods.SetForegroundWindow(wndHelper.Handle);
                    }
                }
            }, window.Dispatcher);

            timer.Start();

            window.Closed += (sender, args) => timer.Stop();
        }

        public static void Pause()
        {
            _bePaused = true;
        }

        public static void Continue()
        {
            _bePaused = false;
        }
    }
}
