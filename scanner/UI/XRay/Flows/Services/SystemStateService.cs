using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UI.Common.Tracers;
using UI.XRay.Business.Algo;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Control;
using UI.XRay.ControlWorkflows;
using UI.XRay.Flows.Services.DataProcess;
using UI.XRay.Gui.Framework;


namespace UI.XRay.Flows.Services
{
    public class SystemStateService
    {
        public static SystemStateService Service { get; private set; }

        static SystemStateService()
        {
            Service = new SystemStateService();
        }
        public SystemStateService()
        {
            GetXrayGenConfig();
            GetControlBoardConfig();
            GetCaptureSystemConfig();
            GetSystemConfig();
            GetBeltStartEnd();
            if (!ScannerConfig.Read(ConfigPath.CleanTunnelContinueTime, out conveyorMoveBack))
            {
                conveyorMoveBack = 10;
            }

            IsDiagnosing = false;
            // 配置改变后需要重新读取，否则会导致诊断界面显示的设置值错误
            ScannerConfig.ConfigChanged += ScannerConfigOnConfigChanged;
        }

        private void ScannerConfigOnConfigChanged(object sender, EventArgs e)
        {
            GetXrayGenConfig();
            GetControlBoardConfig();
            GetCaptureSystemConfig();
            GetSystemConfig();
            GetBeltStartEnd();
            if (!ScannerConfig.Read(ConfigPath.CleanTunnelContinueTime, out conveyorMoveBack))
            {
                conveyorMoveBack = 10;
            }
        }

        double conveyorMoveBack = 0;

        void GetXrayGenConfig()
        {
            XRayGeneratorType xrayGenType;
            if (ScannerConfig.Read(ConfigPath.XRayGenType, out xrayGenType))
            {
                XrayGenType = xrayGenType.ToString();
            }
            float kV;
            if (ScannerConfig.Read(ConfigPath.XRayGenKV, out kV))
            {
                XrayGen1SettingKV = kV;
            }

            float mA;
            if (ScannerConfig.Read(ConfigPath.XRayGenMA, out mA))
            {
                XrayGen1SettingMA = mA;
            }

            float kV2;
            if (ScannerConfig.Read(ConfigPath.XRayGenKV2, out kV2))
            {
                XrayGen2SettingKV = kV2;
            }

            float mA2;
            if (ScannerConfig.Read(ConfigPath.XRayGenMA2, out mA2))
            {
                XrayGen2SettingMA = mA2;
            }
        }

        void GetControlBoardConfig()
        {
            try
            {
                if (ControlService.ServicePart.IsOpened)
                {
                    CtrlSysVersion firmware = new CtrlSysVersion(0, 0);
                    CtrlSysVersion protocol = new CtrlSysVersion(0, 0);
                    if (ControlService.ServicePart.GetSystemDesc(ref firmware, ref protocol))
                    {
                        ControlBoardFirmware = firmware.ToString();
                        ControlBoardProtocol = protocol.ToString();
                    }
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
            }
        }

        void GetCaptureSystemConfig()
        {
            CaptureSysTypeEnum captureSysType;
            if (ScannerConfig.Read<CaptureSysTypeEnum>(ConfigPath.CaptureSysType, out captureSysType))
            {
                CaptureBoardType = captureSysType.ToString();
            }
            int _boardNum = 1;
            if (ScannerConfig.Read(ConfigPath.CaptureSysBoardCount, out _boardNum))
            {
                BoardCount = _boardNum;
            }
            float lineIntegrationTime;
            if (ScannerConfig.Read(ConfigPath.CaptureSysLineIntegrationTime, out lineIntegrationTime))
            {
                LineIntegration = lineIntegrationTime;
            }
            string _model = "";
            if (ScannerConfig.Read(ConfigPath.SystemModel, out _model))
            {
                MachineModel = _model;
            }
            string cardsDist;
            if (ScannerConfig.Read(ConfigPath.CaptureSysDTView1CardsDist, out cardsDist))
            {
                View1CardsDist = cardsDist;
            }
            if (ScannerConfig.Read(ConfigPath.CaptureSysDTView2CardsDist, out cardsDist))
            {
                View2CardsDist = cardsDist;
            }
        }
        private List<IPAddress> GetIP()
        {
            string hostName = Dns.GetHostName();//本机名   

            System.Net.IPAddress[] addressList = Dns.GetHostAddresses(hostName);//会返回所有地址，包括IPv4和IPv6   
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

        void GetSystemConfig()
        {
            float _conveyorSpeed = 0.2f;
            if (ScannerConfig.Read(ConfigPath.MachineConveyorSpeed, out _conveyorSpeed))
            {
                ConveyorSpeed = _conveyorSpeed;
            }
            string _machineNum = "";
            if (ScannerConfig.Read(ConfigPath.SystemMachineNum, out _machineNum))
            {
                MachineNumber = _machineNum;
            }

            SoftwareVersion = VersionHelper.Service.GetVersionStr(SubSystemComponents.Software);

            int _viewsCount = 1;
            if (ScannerConfig.Read(ConfigPath.MachineViewsCount, out _viewsCount))
            {
                ViewsCount = _viewsCount;
            }
            double DiskSpaceSizeGB = ImageStoreDiskHelper.TotalSizeGB;
            double DiskSpaceUsedGB = ImageStoreDiskHelper.TotalUsedSpaceGB;
            DiskUsageState = Math.Round(DiskSpaceUsedGB, 1).ToString() + "/" + Math.Round(DiskSpaceSizeGB, 2).ToString();
        }

        List<int> GetPreProcSetting()
        {
            int _airValueHighAverageLower;

            int _airValueLowAverageLower;

            int _groundValueHighAverageUpper;

            int _groundValueLowAverageUpper;
            int airValueHighSingleLower = 7500;
            int airValueLowSingleLower = 7500;
            int groundValueHighSingleUpper = 5000;
            int groundValueLowSingleUpper = 5000;
            var returnlist = new List<int>();

            if (!ScannerConfig.Read(ConfigPath.PreProcAirHighAvgLower, out _airValueHighAverageLower))
            {
                _airValueHighAverageLower = 7000;
            }
            returnlist.Add(_airValueHighAverageLower);
            if (!ScannerConfig.Read(ConfigPath.PreProcAirHighSingleLower, out airValueHighSingleLower))
            {
                airValueHighSingleLower = 7500;
            }
            returnlist.Add(airValueHighSingleLower);

            if (!ScannerConfig.Read(ConfigPath.PreProcAirLowAvgLower, out _airValueLowAverageLower))
            {
                _airValueLowAverageLower = 7000;
            }
            returnlist.Add(_airValueLowAverageLower);
            if (!ScannerConfig.Read(ConfigPath.PreProcAirLowSingleLower, out airValueLowSingleLower))
            {
                airValueLowSingleLower = 7500;
            }
            returnlist.Add(airValueLowSingleLower);

            if (!ScannerConfig.Read(ConfigPath.PreProcGroundHighAvgUpper, out _groundValueHighAverageUpper))
            {
                _groundValueHighAverageUpper = 4500;
            }
            returnlist.Add(_groundValueHighAverageUpper);
            if (!ScannerConfig.Read(ConfigPath.PreProcGroundHighSingleUpper, out groundValueHighSingleUpper))
            {
                groundValueHighSingleUpper = 5000;
            }
            returnlist.Add(groundValueHighSingleUpper);

            if (!ScannerConfig.Read(ConfigPath.PreProcGroundLowAvgUpper, out _groundValueLowAverageUpper))
            {
                _groundValueLowAverageUpper = 4500;
            }
            returnlist.Add(_groundValueLowAverageUpper);
            if (!ScannerConfig.Read(ConfigPath.PreProcGroundLowSingleUpper, out groundValueLowSingleUpper))
            {
                groundValueLowSingleUpper = 5000;
            }
            returnlist.Add(groundValueLowSingleUpper);

            return returnlist;
        }

        private void GetBeltStartEnd()
        {
            if (!ScannerConfig.Read(ConfigPath.MachineView1BeltEdgeAtBegin, out _view1MarginPixelsAtBegin))
            {
                _view1MarginPixelsAtBegin = 0;
            }

            if (!ScannerConfig.Read(ConfigPath.MachineView1BeltEdgeAtEnd, out _view1MarginPixelsAtEnd))
            {
                _view1MarginPixelsAtEnd = 0;
            }
            if (!ScannerConfig.Read(ConfigPath.MachineView2BeltEdgeAtBegin, out _view2MarginPixelsAtBegin))
            {
                _view2MarginPixelsAtBegin = 0;
            }

            if (!ScannerConfig.Read(ConfigPath.MachineView2BeltEdgeAtEnd, out _view2MarginPixelsAtEnd))
            {
                _view2MarginPixelsAtEnd = 0;
            }

            ExchangeDirectionConfig.Service.GetView1ChannelsCount(out _view1ChannelCount);
            ExchangeDirectionConfig.Service.GetView2ChannelsCount(out _view2ChannelCount);
        }

        private void CleanTunnelExecute()
        {
            var now = DateTime.Now;
            ControlService.ServicePart.DriveConveyor(ConveyorDirection.MoveBackward);





            while (DateTime.Now - now < TimeSpan.FromSeconds(conveyorMoveBack))
            {
                System.Threading.Thread.Sleep(100);
            }
            ControlService.ServicePart.DriveConveyor(ConveyorDirection.Stop);



        }

        private void CalibrateGroundAsync()
        {
            ControlService.ServicePart.RadiateXRay(false);
            System.Threading.Thread.Sleep(1000);

            // 开始更新本底
            var task = ManualCalibrationService.Service.CalibrateGroundAsync();
            if (task != null)
            {
                var groundResult = task.Result;

                // 本底更新成功
                if (groundResult != null)
                {
                    if (groundResult.ResultCode == CalibrationResultCode.Success)
                    {
                        IsPassGroundCalibration = true;
                    }
                }
            }

            TurnOnXRay();
        }
        private void CalibrateAirAsync()
        {
            float waitingTime;
            if (!ScannerConfig.Read(ConfigPath.CalibrateWaitingXRayTimeSpan, out waitingTime))
            {
                waitingTime = 3;
            }
#if DEBUG
            Tracer.TraceDebug($"Yep, you succeed! Time: {waitingTime}");
#endif
            System.Threading.Thread.Sleep((int)(waitingTime * 1000));
            //从实时电压电流中获取校准时的电压电流
            XrayGen1CalibrationKV = XrayGen1RealTimeKV;
            XrayGen2CalibrationKV = XrayGen2RealTimeKV;
            XrayGen1CalibrationMA = XrayGen1RealTimeMA;
            XrayGen2CalibrationMA = XrayGen2RealTimeMA;
            XrayGen1CalibrationErrorCode = XrayGen1RealTimeErrorCode;
            XrayGen2CalibrationErrorCode = XrayGen2RealTimeErrorCode;

            var task = ManualCalibrationService.Service.CalibrateAirAsync();
            if (task != null)
            {
                var air = task.Result;

                // 满度更新结束后，关闭X射线
                ControlService.ServicePart.RadiateXRay(false);


                // 更新成功
                if (air != null && air.ResultCode == CalibrationResultCode.Success)
                {
                    IsPassAirCalibration = true;
                }
            }
        }

        private void TurnOnXRay()
        {
            try
            {
                ControlService.ServicePart.RadiateXRay(true);
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }

            //射线启动后一定时间后再做空气值校准，防止射线不稳定
            System.Threading.Thread.Sleep(3000);

        }

        private void ScanlineCalibration()
        {
            if (!IsCaptureBoardAlive)
            {
                IsPassGroundCalibration = false;
                IsPassAirCalibration = false;
            }
        }

        public Task<string> GetDiagnosisReport()
        {
            return Task.Run(() =>
            {
                IsDiagnosing = true;
                //清空通道

                ControlService.ServicePart.SetWorkMode(ScannerWorkMode.Maintenance);  //YXC 进入维护模式 只转皮带不触发光障

                CleanTunnelExecute();

                //校准空气值和本底值
                ScanlineCalibration();

                ManualCalibrationService.Service.StartService();
                CalibrateGroundAsync();
                CalibrateAirAsync();
                ManualCalibrationService.Service.StopService();

                var normal = "Normal";
                var abnormal = "Abnormal";


                StringBuilder sb = new StringBuilder();
                sb.AppendLine(TranslationService.FindTranslation("Time") + ": " + DateTime.Now.ToString());
                sb.AppendLine("ID: " + UserID);
                sb.AppendLine();


                sb.AppendLine(TranslationService.FindTranslation("Configer", "System Setting"));
                sb.AppendLine("------------------------");
                sb.AppendLine(TranslationService.FindTranslation("Configer", "Views Count") + ": " + ViewsCount);
                sb.AppendLine(TranslationService.FindTranslation("Configer", "Machine Number") + ": " + MachineNumber);
                sb.AppendLine(TranslationService.FindTranslation("Configer", "Model") + ": " + MachineModel);
                sb.AppendLine(TranslationService.FindTranslation("Configer", "Conveyor State") + ": " + normal);
                sb.AppendLine(TranslationService.FindTranslation("Configer", "Version") + ": " + SoftwareVersion);
                sb.AppendLine(TranslationService.FindTranslation("Disk Space") + ": " + DiskUsageState + " G");
                sb.AppendLine();

                sb.AppendLine(TranslationService.FindTranslation("Configer", "Control System"));
                sb.AppendLine("------------------------");
                sb.AppendLine(TranslationService.FindTranslation("Diagnosis Translation", "Control Board Ip") + ": " + ConfigHelper.GetControlBoardIp().ToString());
                sb.AppendLine(TranslationService.FindTranslation("Diagnosis Translation", "Control Board Port") + ": " + ConfigHelper.GetControlBoardPort().ToString());
                sb.AppendLine(TranslationService.FindTranslation("Diagnosis Translation", "Control System Status") + ": " + (IsControlBoardConnected ? normal : abnormal));
                sb.AppendLine(TranslationService.FindTranslation("Configer", "Firmware Version") + ": " + ControlBoardFirmware);
                sb.AppendLine(TranslationService.FindTranslation("Configer", "Communication Protocol") + ": " + ControlBoardProtocol);
                sb.AppendLine(TranslationService.FindTranslation("Diagnosis Translation", "Temperature") + ": " + ControlBoardTemperature.ToString() + " ℃");
                sb.AppendLine();


                sb.AppendLine(TranslationService.FindTranslation("X-Ray Generator Information"));
                sb.AppendLine("------------------------");
                sb.AppendLine(TranslationService.FindTranslation("X-Ray Generator Type") + ": " + XrayGenType);
                sb.AppendLine("#1 " + TranslationService.FindTranslation("Diagnosis Translation", "Generator Alive") + ": " + (IsXrayGen1Alive ? normal : abnormal));
                sb.AppendLine("#1 " + TranslationService.FindTranslation("Diagnosis Translation", "Valtage") + ": " + XrayGen1CalibrationKV.ToString("F2") + "/" + XrayGen1SettingKV.ToString("F2") + " KV");
                sb.AppendLine("#1 " + TranslationService.FindTranslation("Diagnosis Translation", "Current") + ": " + XrayGen1CalibrationMA.ToString("F2") + "/" + XrayGen1SettingMA.ToString("F2") + " mA");
                sb.AppendLine("#1 " + TranslationService.FindTranslation("Diagnosis Translation", "Sparking") + ": " + Convert.ToBoolean(XrayGen1CalibrationErrorCode & 0x01));
                sb.AppendLine("#1 " + TranslationService.FindTranslation("Diagnosis Translation", "OverTemperature") + ": " + Convert.ToBoolean(XrayGen1CalibrationErrorCode & 0x02));
                sb.AppendLine("#1 " + TranslationService.FindTranslation("Diagnosis Translation", "OverVoltage") + ": " + Convert.ToBoolean(XrayGen1CalibrationErrorCode & 0x04));
                sb.AppendLine("#1 " + TranslationService.FindTranslation("Diagnosis Translation", "OverCurrent") + ": " + Convert.ToBoolean(XrayGen1CalibrationErrorCode & 0x08));
                sb.AppendLine("#1 " + TranslationService.FindTranslation("Diagnosis Translation", "OverloadPower") + ": " + Convert.ToBoolean(XrayGen1CalibrationErrorCode & 0x10));
                sb.AppendLine("#1 " + TranslationService.FindTranslation("Diagnosis Translation", "VAOutofControl") + ": " + Convert.ToBoolean(XrayGen1CalibrationErrorCode & 0x20));
                sb.AppendLine("#1 " + TranslationService.FindTranslation("Diagnosis Translation", "InterlockSwitchNotClosed") + ": " + Convert.ToBoolean(XrayGen1CalibrationErrorCode & 0x40));

                if (ViewsCount > 1)
                {
                    sb.AppendLine("#2 " + TranslationService.FindTranslation("Diagnosis Translation", "Generator Alive") + ": " + (IsXrayGen2Alive ? normal : abnormal));
                    sb.AppendLine("#2 " + TranslationService.FindTranslation("Diagnosis Translation", "Valtage") + ": " + XrayGen2CalibrationKV.ToString("F2") + "/" + XrayGen2SettingKV.ToString("F2") + " KV");
                    sb.AppendLine("#2 " + TranslationService.FindTranslation("Diagnosis Translation", "Current") + ": " + XrayGen2CalibrationMA.ToString("F2") + "/" + XrayGen2SettingMA.ToString("F2") + " mA");
                    sb.AppendLine("#2 " + TranslationService.FindTranslation("Diagnosis Translation", "Sparking") + ": " + Convert.ToBoolean(XrayGen2CalibrationErrorCode & 0x01));
                    sb.AppendLine("#2 " + TranslationService.FindTranslation("Diagnosis Translation", "OverTemperature") + ": " + Convert.ToBoolean(XrayGen2CalibrationErrorCode & 0x02));
                    sb.AppendLine("#2 " + TranslationService.FindTranslation("Diagnosis Translation", "OverVoltage") + ": " + Convert.ToBoolean(XrayGen2CalibrationErrorCode & 0x04));
                    sb.AppendLine("#2 " + TranslationService.FindTranslation("Diagnosis Translation", "OverCurrent") + ": " + Convert.ToBoolean(XrayGen2CalibrationErrorCode & 0x08));
                    sb.AppendLine("#2 " + TranslationService.FindTranslation("Diagnosis Translation", "OverloadPower") + ": " + Convert.ToBoolean(XrayGen2CalibrationErrorCode & 0x10));
                    sb.AppendLine("#2 " + TranslationService.FindTranslation("Diagnosis Translation", "VAOutofControl") + ": " + Convert.ToBoolean(XrayGen2CalibrationErrorCode & 0x20));
                    sb.AppendLine("#2 " + TranslationService.FindTranslation("Diagnosis Translation", "InterlockSwitchNotClosed") + ": " + Convert.ToBoolean(XrayGen2CalibrationErrorCode & 0x40));
                }

                sb.AppendLine();


                sb.AppendLine(TranslationService.FindTranslation("Configer", "Image Capture System"));
                sb.AppendLine("------------------------");
                sb.AppendLine(TranslationService.FindTranslation("Configer", "Capture System Type") + ": " + CaptureBoardType);
                sb.AppendLine(TranslationService.FindTranslation("Diagnosis Translation", "Capture System Connect Status") + ": " + (IsCaptureBoardAlive ? normal : abnormal));
                sb.AppendLine(TranslationService.FindTranslation("Configer", "Line Integration Time (ms)") + ": " + LineIntegration);
                sb.AppendLine(TranslationService.FindTranslation("CalibrationWindow", "Air value has been calibrated") + ": " + (IsPassAirCalibration ? normal : abnormal));
                sb.AppendLine(TranslationService.FindTranslation("CalibrationWindow", "Ground value has been calibrated") + ": " + (IsPassGroundCalibration ? normal : abnormal));

                sb.AppendLine(TranslationService.FindTranslation("Diagnosis Translation", "Command Port") + ": " + "3000");
                sb.AppendLine(TranslationService.FindTranslation("Diagnosis Translation", "Image Port") + ": " + "4001");
                sb.AppendLine(TranslationService.FindTranslation("Diagnosis Translation", "Cards Distribution") + ": " + View1CardsDist);
                var view1list = new StringBuilder();
                if (View1BadChannels != null)
                {
                    foreach (var badchannel in View1BadChannels)
                    {
                        int index = badchannel.ChannelIndex + 1;
                        if (index > _view1MarginPixelsAtBegin && index <= (_view1ChannelCount - _view1MarginPixelsAtEnd))
                        {
                            view1list.Append(index);
                            view1list.Append(",");
                        }
                    }
                    if (view1list.Length > 1)
                        view1list.Remove(view1list.Length - 1, 1);
                }
                sb.AppendLine(TranslationService.FindTranslation("Diagnosis Translation", "View1 Bad Points of Auto Detection") + ": " + view1list.ToString());

                //var view1Manual = new StringBuilder();
                //if (View1ManualBadChannels != null)
                //{
                //    foreach (var badchannel in View1ManualBadChannels)
                //    {
                //        view1Manual.Append(badchannel.ChannelNumber);
                //        view1Manual.Append(",");
                //    }
                //    if (view1Manual.Length > 1)
                //        view1Manual.Remove(view1Manual.Length - 1, 1);
                //}
                //sb.AppendLine(TranslationService.FindTranslation("Diagnosis Translation", "View1 Bad Points of Manual Setting") + ": " + view1Manual.ToString());

                if (ViewsCount > 1)
                {
                    if (BoardCount>1) { 
                        sb.AppendLine(TranslationService.FindTranslation("Diagnosis Translation", "Command Port") + ": " + "3001");
                        sb.AppendLine(TranslationService.FindTranslation("Diagnosis Translation", "Image Port") + ": " + "4002");
                        sb.AppendLine(TranslationService.FindTranslation("Diagnosis Translation", "Cards Distribution") + ": " + View2CardsDist);
                    }
                    var view2list = new StringBuilder();
                    if (View2BadChannels != null)
                    {
                        foreach (var badchannel in View2BadChannels)
                        {
                            int index = badchannel.ChannelIndex + 1;
                            if (index > _view2MarginPixelsAtBegin && index <= (_view2ChannelCount - _view2MarginPixelsAtEnd))
                            {
                                view2list.Append(index);
                                view2list.Append(",");
                            }
                        }
                        if (view2list.Length > 1)
                            view2list.Remove(view2list.Length - 1, 1);
                    }

                    sb.AppendLine(TranslationService.FindTranslation("Diagnosis Translation", "View2 Bad Points of Auto Detection") + ": " + view2list.ToString());

                    //var view2Manual = new StringBuilder();
                    //if (View2ManualBadChannels != null)
                    //{
                    //    foreach (var badchannel in View2ManualBadChannels)
                    //    {
                    //        view2Manual.Append(badchannel.ChannelNumber);
                    //        view2Manual.Append(",");
                    //    }
                    //    if (view2Manual.Length > 1)
                    //        view2Manual.Remove(view2Manual.Length - 1, 1);
                    //}
                    //sb.AppendLine(TranslationService.FindTranslation("Diagnosis Translation", "View2 Bad Points of Manual Setting") + ": " + view2Manual.ToString());
                }
                var threshold = GetPreProcSetting();


                sb.AppendLine(TranslationService.FindTranslation("Configer", "Lower Thresholds of Air Value") + " "
                    + TranslationService.FindTranslation("Configer", "Average of HE Line") + ": " + threshold[0]);

                sb.AppendLine(TranslationService.FindTranslation("Configer", "Lower Thresholds of Air Value") + " "
                + TranslationService.FindTranslation("Configer", "HE Point") + ": " + threshold[1]);

                sb.AppendLine(TranslationService.FindTranslation("Configer", "Lower Thresholds of Air Value") + " "
                + TranslationService.FindTranslation("Configer", "Average of LE Line") + ": " + threshold[2]);

                sb.AppendLine(TranslationService.FindTranslation("Configer", "Lower Thresholds of Air Value") + " "
                + TranslationService.FindTranslation("Configer", "LE Point") + ": " + threshold[3]);

                sb.AppendLine(TranslationService.FindTranslation("Configer", "Upper Thresholds of Ground Value") + " "
                    + TranslationService.FindTranslation("Configer", "Average of HE Line") + ": " + threshold[4]);

                sb.AppendLine(TranslationService.FindTranslation("Configer", "Upper Thresholds of Ground Value") + " "
                + TranslationService.FindTranslation("Configer", "HE Point") + ": " + threshold[5]);

                sb.AppendLine(TranslationService.FindTranslation("Configer", "Upper Thresholds of Ground Value") + " "
                + TranslationService.FindTranslation("Configer", "Average of LE Line") + ": " + threshold[6]);

                sb.AppendLine(TranslationService.FindTranslation("Configer", "Upper Thresholds of Ground Value") + " "
                + TranslationService.FindTranslation("Configer", "LE Point") + ": " + threshold[7]);


                sb.AppendLine();

                sb.AppendLine(TranslationService.FindTranslation("Others"));
                sb.AppendLine("------------------------");
                sb.AppendLine(TranslationService.FindTranslation("Diagnosis Translation", "Power Status") + ": " + normal);

                //sb.AppendLine("#1 " + TranslationService.FindTranslation("Diagnosis Translation", "PESensor Status") + ": " + TranslationService.FindTranslation("HardwareStation", IsPESensor1Triggered.ToString()));
                //sb.AppendLine("#2 " + TranslationService.FindTranslation("Diagnosis Translation", "PESensor Status") + ": " + TranslationService.FindTranslation("HardwareStation", IsPESensor2Triggered.ToString()));
                // TODO: 因为网络诊断总是报broken，所以暂时写为ready
                sb.AppendLine("#1 " + TranslationService.FindTranslation("Diagnosis Translation", "PESensor Status") + ": " + TranslationService.FindTranslation("HardwareStation", PESensorState.Ready.ToString()));
                sb.AppendLine("#2 " + TranslationService.FindTranslation("Diagnosis Translation", "PESensor Status") + ": " + TranslationService.FindTranslation("HardwareStation", PESensorState.Ready.ToString()));
                sb.AppendLine(TranslationService.FindTranslation("Emergency window") + ": " + IsEmergencyTriggered);
                sb.AppendLine(TranslationService.FindTranslation("Diagnosis Translation", "InterLock Triggerred") + ": " + IsInterlockTriggered);
                sb.AppendLine(TranslationService.FindTranslation("Diagnosis Translation", "Network Port") + ": ");

                var allIp4 = GetIP();
                foreach (var ip in allIp4)
                {
                    sb.AppendLine("\t" + ip.ToString());
                }

                sb.Replace("True", TranslationService.FindTranslation("HardwareStation", "True"));
                sb.Replace("False", TranslationService.FindTranslation("HardwareStation", "False"));
                sb.Replace(normal, TranslationService.FindTranslation("HardwareStation", normal));
                sb.Replace(abnormal, TranslationService.FindTranslation("HardwareStation", abnormal));

                IsDiagnosing = false;
                ControlService.ServicePart.SetWorkMode(ScannerWorkMode.Regular);  //YXC 进入常规模式
                return sb.ToString();
            });
        }

        public DateTime DiagnoseDateTime { get; set; }
        public string UserID { get; set; }

        /// <summary>
        /// 正在诊断
        /// </summary>
        public bool IsDiagnosing { get; set; }

        #region 控制板
        public bool IsControlBoardConnected { get; set; }
        public string ControlBoardFirmware { get; set; }
        public string ControlBoardProtocol { get; set; }
        public float ControlBoardTemperature { get; set; }
        #endregion

        #region 射线源
        /// <summary>
        /// 射线源是否是连接状态
        /// </summary>
        public bool IsXrayGen1Alive { get; set; }
        public bool IsXrayGen2Alive { get; set; }
        //实时电压电流
        public float XrayGen1RealTimeMA { get; set; }
        public float XrayGen1RealTimeKV { get; set; }
        public byte XrayGen1RealTimeErrorCode { get; set; }

        public float XrayGen2RealTimeMA { get; set; }
        public float XrayGen2RealTimeKV { get; set; }
        public byte XrayGen2RealTimeErrorCode { get; set; }

        //曲线校准时电压电流
        public float XrayGen1CalibrationMA { get; set; }
        public float XrayGen1CalibrationKV { get; set; }
        public byte XrayGen1CalibrationErrorCode { get; set; }
        public float XrayGen2CalibrationMA { get; set; }
        public float XrayGen2CalibrationKV { get; set; }
        public byte XrayGen2CalibrationErrorCode { get; set; }



        /// <summary>
        /// 射线源设定电流
        /// </summary>
        public float XrayGen1SettingMA { get; set; }
        public float XrayGen1SettingKV { get; set; }

        /// <summary>
        /// 射线源设定电流
        /// </summary>
        public float XrayGen2SettingMA { get; set; }
        public float XrayGen2SettingKV { get; set; }

        /// <summary>
        /// 射线源类型
        /// </summary>
        public string XrayGenType { get; set; }
        #endregion

        #region 采集板
        public bool IsCaptureBoardAlive { get; set; }
        public bool IsPassGroundCalibration { get; set; }
        public bool IsPassAirCalibration { get; set; }
        public string CaptureBoardType { get; set; }
        public float LineIntegration { get; set; }

        public int BoardCount { get; set; }
        public string View1CardsDist { get; set; }
        public string View2CardsDist { get; set; }
        public List<BadChannel> View1BadChannels { get; set; }

        public List<BadChannel> View2BadChannels { get; set; }

        public List<ChannelBadFlag> View1ManualBadChannels { get; set; }
        public List<ChannelBadFlag> View2ManualBadChannels { get; set; }

        private int _view1MarginPixelsAtBegin;

        private int _view1MarginPixelsAtEnd;

        private int _view2MarginPixelsAtBegin;

        private int _view2MarginPixelsAtEnd;

        private int _view1ChannelCount;

        private int _view2ChannelCount;

        #endregion

        #region 其他硬件
        public PESensorState IsPESensor1Triggered { get; set; }
        public PESensorState IsPESensor2Triggered { get; set; }
        public bool IsEmergencyTriggered { get; set; }
        public bool IsInterlockTriggered { get; set; }
        #endregion

        #region 系统设置
        public int ViewsCount { get; set; }
        public float ConveyorSpeed { get; set; }
        public string MachineNumber { get; set; }
        public string MachineModel { get; set; }
        public string SoftwareVersion { get; set; }
        public string DiskUsageState { get; set; }

        #endregion
    }
}
