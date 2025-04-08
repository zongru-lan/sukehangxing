using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UI.Common.Tracers;
using System.Windows.Forms;

namespace UI.XRay.Flows.Services
{
    public class TouchKeyboardService
    {
        public static TouchKeyboardService Service { get; private set; }

        static TouchKeyboardService()
        {
            Service = new TouchKeyboardService();
        }

        public void OpenKeyboardWindow(string exeName)
        {
            CheckAndOpenExe(exeName);
        }

        private bool OpenKeyboard(string exeName)
        {
            //打开软键盘
            try
            {
                if (!System.IO.File.Exists(System.AppDomain.CurrentDomain.BaseDirectory + "\\" + exeName + ".exe"))
                {
                    return false;
                }

                System.Diagnostics.Process.Start(System.AppDomain.CurrentDomain.BaseDirectory + "\\" + exeName + ".exe");
                return true;
            }
            catch (System.Exception ex)
            {
                Tracer.TraceError(ex.ToString());
                return false;
            }
        }

        private bool CheckAndOpenExe(string exeName)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(exeName);
                if (processes.Length > 0)
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                Tracer.TraceInfo(e.ToString());
            }

            return OpenKeyboard(exeName);
        }

        public void CloseKeyboard(string exeName)
        {
            Process[] processes = Process.GetProcessesByName(exeName);
            if (processes.Length > 0)
            {
                for (int i = 0; i < processes.Length; i++)
                {
                    processes[i].Kill();
                }
            }
        }
    }
}
