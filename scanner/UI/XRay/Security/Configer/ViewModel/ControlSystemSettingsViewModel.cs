using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Control;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Configer.ViewModel
{
    /// <summary>
    /// 控制系统类型枚举
    /// </summary>
    public enum ControlSysType
    {
        /// <summary>
        /// 
        /// </summary>
        Net = 0
    }

    /// <summary>
    /// 工作模式类型枚举，包含Regular和Continuous
    /// </summary>
    public enum WorkMode
    {
        /// <summary>
        /// 
        /// </summary>
        Regular = 0,

        /// <summary>
        /// 
        /// </summary>
        Continuous = 1
    }

    /// <summary>
    /// 控制系统设置窗口的ViewModel
    /// </summary>
    public class ControlSystemSettingsViewModel : ViewModelBase, IViewModel
    {
        /// <summary>
        /// 命令：测试控制板的连接状态
        /// </summary>
        public RelayCommand TestConnectionCommand { get; private set; }

        private ControlSysType _controlSysType = ControlSysType.Net;

        public RelayCommand QueryBagCountCommand { get; private set; }
        public RelayCommand SetBagCountCommand { get; private set; }
        public RelayCommand ReloadBagCountCommand { get; private set; }

        public RelayCommand StartTimingCommand { get; private set; }
        public RelayCommand StopTimingCommand { get; private set; }

        private int _bagSaveCount;

        /// <summary>
        /// 控制系统类型，目前只可能是UI类型，用于UI绑定
        /// </summary>
        public ControlSysType ControlSysType
        {
            get { return _controlSysType; }
            set
            {
                _controlSysType = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 控制板类型列表，用于UI绑定
        /// </summary>
        public List<ControlSysType> ControlSysTypes { get; set; }


        private IPAddress _controlBoardIp;
        /// <summary>
        /// 控制系统的IP地址，用于UI绑定
        /// </summary>
        public IPAddress ControlBoardIp
        {
            get { return _controlBoardIp; }
            set
            {
                //if (IPAddress.TryParse(value, out _ipAddress))
                {
                    _controlBoardIp = value;
                    RaisePropertyChanged();
                }
            }
        }

        private ushort _udpPort = 10050;
        /// <summary>
        /// 控制系统的端口号，用于UI绑定
        /// </summary>
        public ushort UdpPort
        {
            get { return _udpPort; }
            set
            {
                _udpPort = value;
                RaisePropertyChanged();
            }
        }

        private IPAddress _computerIp;
        /// <summary>
        /// PC机与控制板相连的网卡的IP地址
        /// </summary>
        public IPAddress ComputerIp
        {
            get { return _computerIp; }
            set
            {
                _computerIp = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// PC机所有可用的IP地址
        /// </summary>
        public List<IPAddress> ComputerIpList { get; set; }

        private string _controlBoardVersion;

        public string ControlBoardVersion
        {
            get { return _controlBoardVersion; }
            set { _controlBoardVersion = value; RaisePropertyChanged(); }
        }


        private ushort _controlBoardCmdInterval;

        public ushort ControlBoardCmdInterval
        {
            get { return _controlBoardCmdInterval; }
            set { _controlBoardCmdInterval = value; RaisePropertyChanged(); }
        }

        private Visibility _controlIntervalVisibility = Visibility.Collapsed;

        public Visibility ControlIntervalVisibility
        {
            get { return _controlIntervalVisibility; }
            set { _controlIntervalVisibility = value; RaisePropertyChanged(); }
        }

        private int _bagCountFromControlBoard;
        public int BagCountFromControlBoard
        {
            get { return _bagCountFromControlBoard; }
            set { _bagCountFromControlBoard = value; RaisePropertyChanged(); }
        }

        private int _bagDisplayCount;

        public int BagDisplayCount
        {
            get { return _bagDisplayCount; }
            set { _bagDisplayCount = value; RaisePropertyChanged(); }
        }

        private uint _bagCountFromHardware;
        public uint BagCountFromHardware
        {
            get { return _bagCountFromHardware; }
            set { _bagCountFromHardware = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public ControlSystemSettingsViewModel()
        {
            Tracer.TraceEnterFunc("ControlSystemSettingsViewModel Constructor");

            TestConnectionCommand = new RelayCommand(TestConnectionCommandExecute);

            QueryBagCountCommand = new RelayCommand(QueryBagCountCommandExecute);
            SetBagCountCommand = new RelayCommand(SetBagCountCommandExecute);
            ReloadBagCountCommand = new RelayCommand(ReloadBagCountCommandExecute);

            StartTimingCommand = new RelayCommand(StartTimingCommandExecute);
            StopTimingCommand = new RelayCommand(StopTimingCommandExecute);

            ControlSysTypes = new List<ControlSysType> { ControlSysType.Net };
            ComputerIpList = GetIP();

            try
            {
                LoadSettings();
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            Tracer.TraceExitFunc("ControlSystemSettingsViewModel Constructor");
        }

        /// <summary>
        /// 测试控制系统的连接状态
        /// </summary>
        private void TestConnectionCommandExecute()
        {
            string _version = "";
            CtrlSysVersion firmware = new CtrlSysVersion(0, 0);
            CtrlSysVersion protocol = new CtrlSysVersion(0, 0);
            try
            {
                if (ControlSystemUpdater.TestConnection(ref firmware, ref protocol))
                {
                    _version = firmware.ToString() + " - " + protocol.ToString();
                    ScannerConfig.Write(ConfigPath.ControlFirmWare, firmware.ToString());
                    ScannerConfig.Write(ConfigPath.ControlProtocol, protocol.ToString());
                    MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer", "Connection with Control Board System is OK!"),
                        LanguageResourceExtension.FindTranslation("Configer", "Test Connection"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer", "Could not connect to Control Board System!"),
                        LanguageResourceExtension.FindTranslation("Configer", "Test Connection"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
                MessageBox.Show(exception.ToString(), LanguageResourceExtension.FindTranslation("Configer", "Connection Test"), MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (firmware.Major >= 3)
            {
                ControlIntervalVisibility = Visibility.Visible;
            }
            else
            {
                ControlIntervalVisibility = Visibility.Collapsed;
            }
            ControlBoardVersion = _version;
        }

        private void QueryBagCountCommandExecute()
        {
            int count;
            try
            {
                if (ControlSystemUpdater.QueryTotalBagCount(out count))
                {
                    BagCountFromControlBoard = count;
                    _bagSaveCount = count;
                    ScannerConfig.Write(ConfigPath.SystemTotalBagCount, count);
                    MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer", "Query Bag Count from Control Board Successfully!"),
                        LanguageResourceExtension.FindTranslation("Configer", "Bag Count"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer", "Query Bag Count from Control Board Failed!"),
                        LanguageResourceExtension.FindTranslation("Configer", "Bag Count"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
                MessageBox.Show(exception.ToString(), LanguageResourceExtension.FindTranslation("Configer", "Bag Count"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetBagCountCommandExecute()
        {
            ScannerConfig.Read(ConfigPath.SystemTotalBagCount, out _bagSaveCount);
            ushort num = _bagSaveCount > _bagDisplayCount ? (ushort)(_bagSaveCount - _bagDisplayCount) : (ushort)0;
            try
            {
                if (ControlSystemUpdater.SetBagCount(num))
                {
                    MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer", "Set Bag Count to Control Board Successfully!"),
                        LanguageResourceExtension.FindTranslation("Configer", "Bag Count"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer", "Set Bag Count to Control Board Failed!"),
                        LanguageResourceExtension.FindTranslation("Configer", "Bag Count"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
                MessageBox.Show(exception.ToString(), LanguageResourceExtension.FindTranslation("Configer", "Bag Count"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReloadBagCountCommandExecute()
        {
            try
            {
                if (ControlSystemUpdater.ReloadBagCount(BagCountFromHardware))
                {
                    _bagSaveCount = (int)BagCountFromHardware;
                    ScannerConfig.Write(ConfigPath.SystemTotalBagCount, BagCountFromHardware);
                    MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer", "Reload Bag Count to Control Board Successfully!"),
                        LanguageResourceExtension.FindTranslation("Configer", "Bag Count"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer", "Reload Bag Count to Control Board Failed!"),
                        LanguageResourceExtension.FindTranslation("Configer", "Bag Count"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
                MessageBox.Show(exception.ToString(), LanguageResourceExtension.FindTranslation("Configer", "Bag Count"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartTimingCommandExecute()
        {
            try
            {
                if (ControlSystemUpdater.SetWorkTiming(true))
                {
                    MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer", "Start Hardware Timer Successfully!"),
                        LanguageResourceExtension.FindTranslation("Configer", "Hardware Timer"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer", "Start Hardware Timer Failed!"),
                        LanguageResourceExtension.FindTranslation("Configer", "Hardware Timer"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
                MessageBox.Show(exception.ToString(), LanguageResourceExtension.FindTranslation("Configer", "Hardware Timer"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopTimingCommandExecute()
        {
            try
            {
                if (ControlSystemUpdater.SetWorkTiming(false))
                {
                    MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer", "Stop Hardware Timer Successfully!"),
                        LanguageResourceExtension.FindTranslation("Configer", "Hardware Timer"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer", "Stop Hardware Timer Failed!"),
                        LanguageResourceExtension.FindTranslation("Configer", "Hardware Timer"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
                MessageBox.Show(exception.ToString(), LanguageResourceExtension.FindTranslation("Configer", "Hardware Timer"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 将配置写入注册表
        /// </summary>
        public void SaveSettings()
        {
            Tracer.TraceEnterFunc("ControlSystemSettingsViewModel.SaveSettings");

            if (ControlBoardIp != null)
            {
                ScannerConfig.Write(ConfigPath.ControlSysBoardIp, ControlBoardIp.ToString());
            }

            ScannerConfig.Write(ConfigPath.ControlSysBoardPort, UdpPort);
            ScannerConfig.Write(ConfigPath.ComputerPort, UdpPort);

            if (ComputerIp != null)
            {
                ScannerConfig.Write(ConfigPath.ComputerIp, ComputerIp.ToString());
            }

            ScannerConfig.Write(ConfigPath.ControlSysBoardCmdInterval, ControlBoardCmdInterval);

            if (ControlIntervalVisibility == Visibility.Visible)
                ControlSystemUpdater.SetCmdInterval(ControlBoardCmdInterval);

            Tracer.TraceExitFunc("ControlSystemSettingsViewModel.SaveSettings");
        }

        /// <summary>
        /// 从注册表加载配置
        /// </summary>
        public void LoadSettings()
        {
            Tracer.TraceEnterFunc("ControlSystemSettingsViewModel.LoadSettings");

            bool pcIPChanged = false;

            string ipAddress;
            if (ScannerConfig.Read(ConfigPath.ControlSysBoardIp, out ipAddress))
            {
                IPAddress sysIP;
                if (IPAddress.TryParse(ipAddress, out sysIP))
                {
                    ControlBoardIp = sysIP;
                }
            }

            int port;
            if (ScannerConfig.Read(ConfigPath.ControlSysBoardPort, out port))
            {
                UdpPort = (ushort)port;
            }

            int interval;
            if (ScannerConfig.Read(ConfigPath.ControlSysBoardCmdInterval, out interval))
            {
                ControlBoardCmdInterval = (ushort)interval;
            }

            if (ScannerConfig.Read(ConfigPath.ComputerIp, out ipAddress))
            {
                IPAddress sysIP;
                if (IPAddress.TryParse(ipAddress, out sysIP))
                {
                    ComputerIp = sysIP;
                }
                else
                {
                    if (ComputerIpList.Count > 0)
                    {
                        ComputerIp = ComputerIpList[0];
                    }
                }
            }
            else
            {
                if (ComputerIpList.Count > 0)
                {
                    ComputerIp = ComputerIpList[0];
                }
            }

            float firmware = 0f, protocol = 0f;
            if (!ScannerConfig.Read(ConfigPath.ControlFirmWare, out firmware))
            {
                firmware = 0.0f;
            }
            if (!ScannerConfig.Read(ConfigPath.ControlProtocol, out protocol))
            {
                protocol = 0.0f;
            }
            var version = firmware.ToString() + " - " + protocol.ToString();
            ControlBoardVersion = version;

            if (firmware >= 3.0)
            {
                ControlIntervalVisibility = Visibility.Visible;
            }
            else
            {
                ControlIntervalVisibility = Visibility.Collapsed;
            }
            if (!ScannerConfig.Read(ConfigPath.SystemTotalBagCount, out _bagSaveCount))
            {
                _bagSaveCount = 0;
            }

            Tracer.TraceExitFunc("ControlSystemSettingsViewModel.LoadSettings");
        }

        /// <summary>
        /// 获取本机所有可用IPV4地址
        /// </summary>
        /// <returns></returns>
        private List<IPAddress> GetIP()
        {
            string hostName = Dns.GetHostName();//本机名   
            //System.Net.IPAddress[] addressList = Dns.GetHostByName(hostName).AddressList;//会警告GetHostByName()已过期，我运行时且只返回了一个IPv4的地址   
            System.Net.IPAddress[] addressList = Dns.GetHostAddresses(hostName);//会返回所有地址，包括IPv4和IPv6   
            //foreach (IPAddress ip in addressList)
            //{
            //    listBox1.Items.Add(ip.ToString());
            //}
            //return addressList.ToList();
            List<IPAddress> ipAddresses = new List<IPAddress>();
            foreach (IPAddress ip in addressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    ipAddresses.Add(ip);
                }
            }
            return ipAddresses;
        }
    }
}
