using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using UI.Common.Tracers;

namespace UI.XRay.Common.Utilities
{
    /// <summary>
    /// 表示一个使用PInvoke方法获取设备属性的工具类
    /// </summary>
    public static class DeviceCaps
    {
        /// <summary>
        /// 获取屏幕刷新速率：60hz或75hz
        /// </summary>
        public static int RefreshRate
        {
            get
            {
                IntPtr desktopDC = NativeMethods.GetDC(NativeMethods.GetDesktopWindow());
                return NativeMethods.GetDeviceCaps(desktopDC, 116);
            }
        }

        public static string GetCpuId()
        {
            try
            {
                string cpuInfo = string.Empty;

                ManagementClass mc = new ManagementClass("Win32_Processor");
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (var mo in moc)
                {
                    cpuInfo = mo.Properties["ProcessorId"].Value.ToString();
                    break;
                }
                moc = null;
                mc = null;
                return cpuInfo.Trim();
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
            return string.Empty;
        }

        public static string GetPCID(int iCPU = 5, int iHardDisk = 5, int iMac = 5, int iGPU = 0, int iMainB = 5)
        {
            string strPCID = "";
            try
            {
                string strCPU = GetCPUID();
                string strHardDisk = GetDiskSn();
                string strMAC = GetMacAddress();
                strMAC = strMAC.Replace(":", string.Empty);
                string strGPU = GetVideoCtrlId();
                string strMb = GetMainBoardSn();
                strPCID = strCPU.Remove(0, strCPU.Length - iCPU)
                          + strHardDisk.Remove(0, strHardDisk.Length - iHardDisk)
                          + strMAC.Remove(0, strMAC.Length - iMac)
                          + strGPU.Remove(0, strGPU.Length - iGPU)
                          + strMb.Remove(0, strMb.Length - iMainB);
            }
            catch
            {
                return "";
            }

            return strPCID;
        }

        /// 获得CPU编号        
        ///         
        ///         
        public static string GetCPUID()
        {
            string cpuid = "";
            try
            {
                ManagementClass mc = new ManagementClass("Win32_Processor");
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (ManagementObject mo in moc)
                {
                    cpuid = mo.Properties["ProcessorId"].Value.ToString();
                }
            }
            catch
            {
                return "";
            }
            return cpuid.TrimEnd();
        }

        /// 获取硬盘序列号 
        /// 
        /// 
        public static string GetDiskSn()
        {
            //这种模式在插入一个U盘后可能会有不同的结果，如插入我的手机时
            String HDid = "";
            try
            {
                ManagementClass mc = new ManagementClass("Win32_DiskDrive");
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (ManagementObject mo in moc)
                {
                    HDid = (string) mo.Properties["SerialNumber"].Value; //SerialNumber在Win7以上系统有效
                    break;
                    //这名话解决有多个物理盘时产生的问题，只取第一个物理硬盘，也可查wmi_HD["MediaType"].ToString() == "Fixed hard disk media")//固定硬盘，在Win7以上系统上，XP上"Fixed hard disk ” 
                }
            }
            catch
            {
                return "";
            }
            return HDid.TrimEnd();
        }

        ///     

        /// 获取网卡硬件地址 
        /// 
        ///         
        public static string GetMacAddress()
        {
            string mac = "";
            try
            {
                ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (ManagementObject mo in moc)
                {
                    if ((bool) mo["IPEnabled"] == true)
                    {
                        mac = mo["MacAddress"].ToString();
                        break;
                    }
                }
            }
            catch
            {
                return "";
            }

            return mac.TrimEnd();
        }

        /// 获取 显卡ID
        /// 
        ///         
        public static string GetVideoCtrlId()
        {
            string VideoCtrl = "";
            try
            {
                ManagementClass mc = new ManagementClass("Win32_VideoController");
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (ManagementObject mo in moc)
                {
                    VideoCtrl = (string) mo.Properties["PNPDeviceID"].Value;
                    break;
                }
            }
            catch
            {
                return "";
            }
            return VideoCtrl.TrimEnd();
        }

        /// 获取 主板序列号 
        /// 
        ///         
        public static string GetMainBoardSn()
        {
            string MbSn = "";
            try
            {
                ManagementClass mc = new ManagementClass("Win32_BaseBoard");
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (ManagementObject mo in moc)
                {
                    MbSn = (string) mo.Properties["SerialNumber"].Value; //SerialNumber在Win7以上系统有效
                    break;
                }
            }
            catch
            {
                return "";
            }
            return MbSn.TrimEnd();
        }

        /// 获取IP地址  
        ///         
        ///         
        public static string GetIPAddress()
        {
            string st = "";
            ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection moc = mc.GetInstances();
            foreach (ManagementObject mo in moc)
            {

            }
            return st.TrimEnd();
        }
    }
}
