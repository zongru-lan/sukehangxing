using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Flows.Services;
using UI.XRay.Gui.Framework;
using UI.XRay.Security.Configer;

namespace UI.XRay.Security.Configer.ViewModel
{
    /// <summary>
    /// 图像采集系统设置界面的ViewModel
    /// </summary>
    public class CaptureSystemSettingsViewModel : ViewModelBase, IViewModel
    {
        private CaptureSysTypeEnum _captureSysType = CaptureSysTypeEnum.DtGCUSTD;

        public List<DetSysTypeEnum> DtDetSysTypes { get; set; }

        private DetSysTypeEnum _dtDetSysType = DetSysTypeEnum.USB;

        public DetSysTypeEnum DtDetSysType
        {
            get { return _dtDetSysType; }
            set
            {
                _dtDetSysType = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 图像采集系统的类型，可以是UI或者DT，用于UI绑定
        /// </summary>
        public CaptureSysTypeEnum CaptureSysType
        {
            get { return _captureSysType; }
            set
            {
                _captureSysType = value;
                if (value == CaptureSysTypeEnum.DtGCUSTD)
                {
                    RemoteIPVisibility = Visibility.Collapsed;
                    PbControlParamVisibility = Visibility.Collapsed;
                    DeviceInterfaceVisibility = Visibility.Visible;
                }
                else if (value == CaptureSysTypeEnum.TYM)
                {
                    RemoteIPVisibility = Visibility.Collapsed;
                    PbControlParamVisibility = Visibility.Visible;
                    DeviceInterfaceVisibility = Visibility.Visible;
                }
                else if(value == CaptureSysTypeEnum.Dt)
                {
                    RemoteIPVisibility = Visibility.Collapsed;
                    PbControlParamVisibility = Visibility.Collapsed;
                    DeviceInterfaceVisibility = Visibility.Collapsed;
                }
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 图像采集系统类型列表，用于UI绑定
        /// </summary>
        public List<CaptureSysTypeEnum> CaptureSysTypes { get; set; }

        /// <summary>
        /// 数字采集板数量列表，用于UI绑定
        /// </summary>
        public List<int> BoardCountSource { get; set; }

        private int _detBoardPixelsCount = 128;
        /// <summary>
        /// 探测板的像素个数，是高低能之和，用于UI绑定
        /// </summary>
        public int DetBoardPixelsCount
        {
            get { return _detBoardPixelsCount; }
            set
            {
                _detBoardPixelsCount = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 探测板的像素个数列表，用于UI绑定
        /// </summary>
        public List<int> DetBoardPixelsCountSource { get; set; }

        private int _detBoardsCountPerView;
        /// <summary>
        /// 每个视角的探测板数量，用于UI绑定
        /// </summary>
        public int DetBoardsCountPerView
        {
            get { return _detBoardsCountPerView; }
            set
            {
                _detBoardsCountPerView = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 每个视角的探测板数量列表，用于UI绑定
        /// </summary>
        public List<int> DetBoardsCountPerViewSource { get; set; }

        private bool _isDualEnergy;
        /// <summary>
        /// 是否为双能，true代表双能，false代表单能，用于UI绑定
        /// </summary>
        public bool IsDualEnergy
        {
            get { return _isDualEnergy; }
            set
            {
                _isDualEnergy = value;
                RaisePropertyChanged();
            }
        }

        private float _lineIntegrationTime;
        /// <summary>
        /// 积分时间，单位ms，用于UI绑定
        /// </summary>
        public float LineIntegrationTime
        {
            get { return _lineIntegrationTime; }
            set
            {
                _lineIntegrationTime = value;
                RaisePropertyChanged();
            }
        }

        private string _captureRemoteIP1;

        public string CaptureRemoteIP1
        {
            get { return _captureRemoteIP1; }
            set { _captureRemoteIP1 = value; RaisePropertyChanged(); }
        }

        private int _captureRemoteCmdPort1;

        public int CaptureRemoteCmdPort1
        {
            get { return _captureRemoteCmdPort1; }
            set { _captureRemoteCmdPort1 = value; RaisePropertyChanged(); }
        }
        private int _captureRemoteImagePort1;

        public int CaptureRemoteImagePort1
        {
            get { return _captureRemoteImagePort1; }
            set { _captureRemoteImagePort1 = value; RaisePropertyChanged(); }
        }


        private string _captureRemoteIP2;

        public string CaptureRemoteIP2
        {
            get { return _captureRemoteIP2; }
            set { _captureRemoteIP2 = value; RaisePropertyChanged(); }
        }

        private int _captureRemoteCmdPort2;

        public int CaptureRemoteCmdPort2
        {
            get { return _captureRemoteCmdPort2; }
            set { _captureRemoteCmdPort2 = value; RaisePropertyChanged(); }
        }

        private int _captureRemoteImagePort2;

        public int CaptureRemoteImagePort2
        {
            get { return _captureRemoteImagePort2; }
            set { _captureRemoteImagePort2 = value; RaisePropertyChanged(); }
        }


        private IPAddress _captureHostIP;
        public IPAddress CaptureHostIP
        {
            get { return _captureHostIP; }
            set { _captureHostIP = value; RaisePropertyChanged(); }
        }

        private Visibility _deviceInterfaceVisibility = Visibility.Collapsed;

        public Visibility DeviceInterfaceVisibility
        {
            get { return _deviceInterfaceVisibility; }
            set
            {
                _deviceInterfaceVisibility = value;
                RaisePropertyChanged();
            }
        }

        private Visibility _pbControlParamVisibility = Visibility.Collapsed;

        public Visibility PbControlParamVisibility
        {
            get { return _pbControlParamVisibility; }
            set
            {
                _pbControlParamVisibility = value;
                RaisePropertyChanged();
            }
        }


        private Visibility _pingTimeVisibility = Visibility.Collapsed;

        public Visibility PingTimeVisibility
        {
            get { return _pingTimeVisibility; }
            set { _pingTimeVisibility = value; RaisePropertyChanged(); }
        }


        private bool _capturePingEnable;

        public bool CapturePingEnable
        {
            get { return _capturePingEnable; }
            set
            {
                _capturePingEnable = value;
                PingTimeVisibility = _capturePingEnable ? Visibility.Visible : Visibility.Collapsed;
                RaisePropertyChanged();
            }
        }


        private int _capturePingTime;

        public int CapturePingTime
        {
            get { return _capturePingTime; }
            set { _capturePingTime = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 积分时间列表，用于UI绑定
        /// </summary>
        public List<float> LineIntegrationTimeSource { get; set; }

        private string _view1CardsDist;
        /// <summary>
        /// 视角一探测板分布字符串，针对于DT探测方案，用于UI绑定
        /// </summary>
        public string View1CardsDist
        {
            get { return _view1CardsDist; }
            set
            {
                _view1CardsDist = value;
                RaisePropertyChanged();
            }
        }

        private string _view2CardsDist;
        /// <summary>
        /// 视角2探测板分布字符串，针对于DT探测方案，用于UI绑定
        /// </summary>
        public string View2CardsDist
        {
            get { return _view2CardsDist; }
            set
            {
                _view2CardsDist = value;
                RaisePropertyChanged();
            }
        }

        private Visibility _remoteIPVisibility;

        public Visibility RemoteIPVisibility
        {
            get { return _remoteIPVisibility; }
            set { _remoteIPVisibility = value; RaisePropertyChanged(); }
        }


        private int _viewsCount = 1;

        /// <summary>
        /// 视角数量，用于UI绑定
        /// </summary>
        public int ViewsCount
        {
            get { return _viewsCount; }
            set
            {
                _viewsCount = value;
                if (_viewsCount == 1)
                {
                    ShowView2Settings = Visibility.Collapsed;
                }
                else
                {
                    ShowView2Settings = Visibility.Visible;
                }
                RaisePropertyChanged();
            }
        }

        private Visibility _showView2Settings = Visibility.Collapsed;

        /// <summary>
        /// 是否显示视角2的设置，如果选中双视角，则显示，否者不显示，用于UI绑定
        /// </summary>
        public Visibility ShowView2Settings
        {
            get { return _showView2Settings; }
            set
            {
                _showView2Settings = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// PC机所有可用的IP地址
        /// </summary>
        public List<IPAddress> ComputerIpList { get; set; }

        private int _boardNum;

        public int BoardCount
        {
            get { return _boardNum; }
            set
            {
                _boardNum = value;
                if (_boardNum == 1)
                {
                    ShowView2Settings = Visibility.Collapsed;
                }
                else
                {
                    ShowView2Settings = Visibility.Visible;
                }
                RaisePropertyChanged();
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 构造函数，主要是构造各种选项列表
        /// </summary>
        public CaptureSystemSettingsViewModel()
        {
            Tracer.TraceEnterFunc("CaptureSystemSettingsViewModel Constructor.");

            CaptureSysTypes = new List<CaptureSysTypeEnum>();
            CaptureSysTypes.Add(CaptureSysTypeEnum.DtGCUSTD);
            CaptureSysTypes.Add(CaptureSysTypeEnum.TYM);
            CaptureSysTypes.Add(CaptureSysTypeEnum.Dt);


            //DetBoardPixelsCountSource = new List<int> { 32, 64, 96, 128 };

            //DetBoardsCountPerViewSource = new List<int> { 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            BoardCountSource = new List<int> { 1, 2 };
            DtDetSysTypes = new List<DetSysTypeEnum>()
            {
                DetSysTypeEnum.USB,
                DetSysTypeEnum.TCP,
                DetSysTypeEnum.UDP
            };
            ComputerIpList = GetIP();
            
            try
            {
                LoadSettings();
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception, "Failed to load settings for Capture System.");
            }

            Tracer.TraceExitFunc("CaptureSystemSettingsViewModel Constructor.");
        }

        /// <summary>
        /// 保存数据采集设置
        /// </summary>
        public void SaveSettings()
        {
            // 先保存至注册表，然后保存至Dt硬件
            if (SaveSettingsToRegistry())
            {
                if (CaptureSysType == CaptureSysTypeEnum.DtGCUSTD)
                {
                    if (!DtGCUSTDCaptureUpdater.Update())
                    {
                        MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer", "Failed to save settings to Capture Card."),
                            LanguageResourceExtension.FindTranslation("Configer", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer", "New settings have been saved into Capture System."),
                            "", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else if (CaptureSysType == CaptureSysTypeEnum.TYM)
                {
                    if (!TYMCaptureUpdater.Update())
                    {
                        MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer", "Failed to save settings to Capture Card."),
                            LanguageResourceExtension.FindTranslation("Configer", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer", "New settings have been saved into Capture System."),
                            "", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else if (CaptureSysType == CaptureSysTypeEnum.Dt)
                {
                    if (!DtCaptureUpdater.Update())
                    {
                        MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer", "Failed to save settings to Capture Card."),
                            LanguageResourceExtension.FindTranslation("Configer", "Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer", "New settings have been saved into Capture System."),
                            "", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

            }
            else
            {
                MessageBox.Show(LanguageResourceExtension.FindTranslation("Configer","Failed to save settings."), 
                    LanguageResourceExtension.FindTranslation("Configer","Error"), MessageBoxButton.OK,MessageBoxImage.Error);
            }
        }

        public bool SaveSettingsToRegistry()
        {
            try
            {
                ScannerConfig.Write<CaptureSysTypeEnum>(ConfigPath.CaptureSysType, CaptureSysType);
                ScannerConfig.Write(ConfigPath.CaptureSysDTDetSysType, DtDetSysType);
                ScannerConfig.Write(ConfigPath.CaptureSysBoardCount, _boardNum);

                ScannerConfig.Write(ConfigPath.CaptureSysRemoteIP1, CaptureRemoteIP1);
                ScannerConfig.Write(ConfigPath.CaptureSysRemoteCmdPort1, CaptureRemoteCmdPort1);
                ScannerConfig.Write(ConfigPath.CaptureSysRemoteImagePort1, CaptureRemoteImagePort1);
                ScannerConfig.Write(ConfigPath.CaptureSysRemoteIP2, CaptureRemoteIP2);
                ScannerConfig.Write(ConfigPath.CaptureSysRemoteCmdPort2, CaptureRemoteCmdPort2);
                ScannerConfig.Write(ConfigPath.CaptureSysRemoteImagePort2, CaptureRemoteImagePort2);

                if (CaptureHostIP != null)
                {
                    ScannerConfig.Write(ConfigPath.CaptureSysHostIP, CaptureHostIP.ToString());
                }

                ScannerConfig.Write(ConfigPath.CaptureSysPingEnable, CapturePingEnable);
                ScannerConfig.Write(ConfigPath.CaptureSysPingTime, CapturePingTime);

                ScannerConfig.Write(ConfigPath.CaptureSysLineIntegrationTime, LineIntegrationTime);
                ScannerConfig.Write(ConfigPath.CaptureSysDTView1CardsDist, View1CardsDist);
                ScannerConfig.Write(ConfigPath.CaptureSysDTView2CardsDist, View2CardsDist);

                return true;
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            return false;
        }

        /// <summary>
        /// 从本地（注册表）加载配置
        /// </summary>
        public void LoadSettings()
        {
            Tracer.TraceEnterFunc("CaptureSystemSettingsViewModel.LoadSettings.");

            CaptureSysTypeEnum captureSysType;
            if (ScannerConfig.Read<CaptureSysTypeEnum>(ConfigPath.CaptureSysType, out captureSysType))
            {
                CaptureSysType = captureSysType;
            }
            CaptureSysType = captureSysType;

            if (!ScannerConfig.Read(ConfigPath.CaptureSysDTDetSysType, out _dtDetSysType))
            {
                _dtDetSysType = DetSysTypeEnum.USB;
            }

            if (!ScannerConfig.Read(ConfigPath.MachineViewsCount, out _viewsCount))
            {
                _viewsCount = 1;
            }

            int result;

            if (ScannerConfig.Read(ConfigPath.MachineViewsCount, out result))
            {
                ViewsCount = result;
            }

            if (ScannerConfig.Read(ConfigPath.CaptureSysBoardCount, out _boardNum))
            {
                BoardCount = _boardNum;
            }

            bool isDualEnergy;
            if (ScannerConfig.Read(ConfigPath.CaptureSysIsDualEnergy, out isDualEnergy))
            {
                IsDualEnergy = isDualEnergy;
            }

            float lineIntegrationTime;
            if (ScannerConfig.Read(ConfigPath.CaptureSysLineIntegrationTime, out lineIntegrationTime))
            {
                LineIntegrationTime = lineIntegrationTime;
            }

            if (ScannerConfig.Read(ConfigPath.CaptureSysRemoteIP1, out _captureRemoteIP1))
            {
                CaptureRemoteIP1 = _captureRemoteIP1;
            }
            if (ScannerConfig.Read(ConfigPath.CaptureSysRemoteCmdPort1, out _captureRemoteCmdPort1))
            {
                CaptureRemoteCmdPort1 = _captureRemoteCmdPort1;
            }
            if (ScannerConfig.Read(ConfigPath.CaptureSysRemoteImagePort1, out _captureRemoteImagePort1))
            {
                CaptureRemoteImagePort1 = _captureRemoteImagePort1;
            }

            if (ScannerConfig.Read(ConfigPath.CaptureSysRemoteIP2, out _captureRemoteIP2))
            {
                CaptureRemoteIP2 = _captureRemoteIP2;
            }
            if (ScannerConfig.Read(ConfigPath.CaptureSysRemoteCmdPort2, out _captureRemoteCmdPort2))
            {
                CaptureRemoteCmdPort2 = _captureRemoteCmdPort2;
            }
            if (ScannerConfig.Read(ConfigPath.CaptureSysRemoteImagePort2, out _captureRemoteImagePort2))
            {
                CaptureRemoteImagePort2 = _captureRemoteImagePort2;
            }

            string captureSysHostIP;
            if (ScannerConfig.Read(ConfigPath.CaptureSysHostIP, out captureSysHostIP))
            {
                IPAddress sysIP;
                if (IPAddress.TryParse(captureSysHostIP, out sysIP))
                {
                    CaptureHostIP = sysIP;
                }
                else
                {
                    if (ComputerIpList.Count > 0)
                    {
                        CaptureHostIP = ComputerIpList[0];
                    }
                }
            }

            bool captureSysPingEnable;
            if (ScannerConfig.Read(ConfigPath.CaptureSysPingEnable, out captureSysPingEnable))
            {
                CapturePingEnable = captureSysPingEnable;
            }

            int captureSysPingTime;
            if (ScannerConfig.Read(ConfigPath.CaptureSysPingTime, out captureSysPingTime))
            {
                CapturePingTime = captureSysPingTime;
            }

            string cardsDist;
            if (ScannerConfig.Read(ConfigPath.CaptureSysDTView1CardsDist, out cardsDist))
            {
                View1CardsDist = cardsDist;
            }

            if (_viewsCount == 2)
            {
                if (ScannerConfig.Read(ConfigPath.CaptureSysDTView2CardsDist, out cardsDist))
                {
                    View2CardsDist = cardsDist;
                }
            }

            Tracer.TraceExitFunc("CaptureSystemSettingsViewModel.LoadSettings.");
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
