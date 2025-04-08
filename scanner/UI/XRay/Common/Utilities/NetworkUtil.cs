using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Common.Utilities
{
    public static class NetworkUtil
    {
        /// <summary>
        /// 根据网络适配器名称，获取其IPV4的地址
        /// </summary>
        /// <param name="nicName">网络适配器名称</param>
        /// <returns>网络适配器的IP地址，如果未找到，则为空</returns>
        public static IPAddress GetIAddressByNicName(string nicName)
        {
            var nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var nic in nics)
            {
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet && string.Compare(nicName, nic.Name, true) == 0)
                {
                    foreach (var ip in nic.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return ip.Address;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 获取本机的所有IP地址
        /// </summary>
        /// <returns></returns>
        public static List<IPAddress> GetAllIpV4Addresses()
        {
            var addrList = new List<IPAddress>();

            var nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var nic in nics)
            {
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    foreach (var ip in nic.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            addrList.Add(ip.Address);
                        }
                    }
                }
            }

            return addrList;
        }
    }
}
