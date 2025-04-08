//using TRS_NetWorkController;
using HiWing.Net.Sockets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using UI.XRay.Business.Entities;
using UI.XRay.ControlWorkflows;
using UI.XRay.Flows.TRSNetwork.Models;
using XRayNetEntities;
using XRayNetEntities.CommandType;
using XRayNetEntities.Extensions;
using XRayNetEntities.Models.ClientRequest;
using XRayNetEntities.Models.ClientResponse;
using XRayNetEntities.Tools;
using Thread = System.Threading.Thread;

namespace UI.XRay.Flows.TRSNetwork
{
    /// <summary>
    /// 网络服务类
    /// 机器的一些状态会同步更新到这里,
    /// </summary>
    public class TRSNetWorkService
    {
        /// <summary>
        /// 实例
        /// </summary>
        public static TRSNetWorkService Service
        {
            get
            {
                if (service == null)
                    service = new TRSNetWorkService();
                return service;
            }
        }


        private static TRSNetWorkService service;

        public TRSNetWorkService()
        {
            Status = new ScannerStatus();
        }

        private string LoggerName = "TRSNetWorkService";

        #region Properties & Fields
        /// <summary>
        /// 通讯服务
        /// </summary>
        public NetClient TRSClient { private set; get; }

        /// <summary>
        /// 图像开始扫描时记录数据
        /// </summary>
        public Dictionary<string, XrayStartInfo> XrayStartInfo { get; set; }

        /// <summary>
        /// 待发送线数据队列
        /// </summary>
        private ConcurrentQueue<List<DisplayScanlineDataBundle>> queueScanlineToSend;

        /// <summary>
        /// 安检机状态
        /// </summary>
        public ScannerStatus Status { get; set; }

        private object statusLock = new object();

        public bool SingleMode
        {
            get
            {
                return Global.Instance.Sys.IsSingleMode;
            }
        }

        public bool IsRunning { get; private set; }

        public bool Connected
        {
            get
            {
                return TRSClient?.State == SocketState.Connected;
            }
        }
        #region Timers
        /// <summary>
        /// 发送心跳包定时器
        /// </summary>
        private Timer timHeartBeat;

        /// <summary>
        /// 断线重连定时器
        /// </summary>
        private Timer timReconnect;

        #endregion
        private int intervalHeartBeat = 500;
        private int intervalReconnect = 500;
        private int intervalMaintain = 500;


        private string AccountId { get; set; }

        public bool IsSingleMode
        {
            get
            {
                return Global.Instance.Sys.IsSingleMode;
            }
        }
        private Timer timMaintainStartInfo;

        private Thread scanlineSendThread;
        /// <summary>
        /// IPEndPoint of the NetService.
        /// </summary>
        private IPEndPoint IPE
        {
            get
            {
                return new IPEndPoint(IPAddress.Parse("127.0.0.1"), Global.Instance.Sys.NetServerPort);
            }
        }
        private DateTime lastHeartBeat;
        public int SendLinesInterval { get; set; } = 100;
        #endregion

        #region Init & Close
        public void StartService()
        {
            XrayStartInfo = new Dictionary<string, TRSNetwork.XrayStartInfo>();
            //通信服务
            queueScanlineToSend = new ConcurrentQueue<List<DisplayScanlineDataBundle>>();
            TRSClient = new NetClient();

            TRSClient.Connected += new EventHandler<NetSocketConnectedEventArgs>(Client_Connected);
            TRSClient.DataArrived += new EventHandler<NetSockDataArrivalEventArgs>(Client_DataArrived);
            TRSClient.Disconnected += new EventHandler<NetSocketDisconnectedEventArgs>(Client_Disconnected);
            TRSClient.ErrorReceived += new EventHandler<NetSockErrorReceivedEventArgs>(Client_ErrorReceived);
            TRSClient.StateChanged += new EventHandler<NetSockStateChangedEventArgs>(Client_StateChanged);

            IsRunning = true;
            StartTimers();
            ScannerStatusMonitor.Instance.Start();

            if (!IsSingleMode && Global.Instance.Sys.JudgeMode != 0)
            {
                scanlineSendThread = new Thread(ScanlineSendThreadRoutine)
                {
                    IsBackground = true
                };
                scanlineSendThread.Start();
            }
        }

        public void Stop()
        {
            IsRunning = false;
            if (timHeartBeat != null)
            {
                timHeartBeat.Close();
                timHeartBeat = null;
            }
            if (timReconnect != null)
            {
                timReconnect.Close();
                timReconnect = null;
            }
            if (timMaintainStartInfo != null)
            {
                timMaintainStartInfo.Close();
                timMaintainStartInfo = null;
            }
            ScannerStatusMonitor.Instance.Stop();
            TRSClient.Close("App Exit");
        }
        private void StartTimers()
        {
            if (timHeartBeat != null)
            {
                timHeartBeat.Close();
                timHeartBeat = null;
            }
            if (timReconnect != null)
            {
                timReconnect.Close();
                timReconnect = null;
            }
            if (timMaintainStartInfo != null)
            {
                timMaintainStartInfo.Close();
                timMaintainStartInfo = null;
            }

            timHeartBeat = new Timer(intervalHeartBeat);
            timHeartBeat.Elapsed += timHeartBeat_Elapsed;

            timReconnect = new Timer(intervalReconnect);
            timReconnect.Elapsed += timReconnect_Elapsed;

            timMaintainStartInfo = new Timer(intervalMaintain);
            timMaintainStartInfo.Elapsed += timMaintainStartInfo_Elapsed;
            timHeartBeat.Start();
            timReconnect.Start();
            timMaintainStartInfo.Start();
        }

        void timMaintainStartInfo_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                var overTimeInfo = XrayStartInfo
                    .Where(p => (DateTime.Now - p.Value.BeginScanTime) > TimeSpan.FromSeconds(60))
                    .Select(p => p.Key).ToArray();
                if (overTimeInfo == null || overTimeInfo.Length < 1)
                {
                    return;
                }
                for (int i = overTimeInfo.Length - 1; i >= 0; i--)
                {
                    XrayStartInfo.Remove(overTimeInfo[i]);
                }
            }
            catch (Exception ex)
            {
                LogUtil.Exception(ex, "", LoggerName);
            }

        }

        void timReconnect_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (TRSClient == null) return;
            if (!IsRunning)
                return;
            if (TRSClient.State == SocketState.Connected || TRSClient.State == SocketState.Connecting)
                return;

            try
            {
                TRSClient.Connect(IPE);
            }
            catch (Exception ex)
            {
                LogUtil.Exception(ex, "", LoggerName);
            }
        }

        void timHeartBeat_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (TRSClient == null)
                return;
            if (TRSClient.State != SocketState.Connected)
                return;
            if (!IsRunning)
                return;
            try
            {
                lock (ScannerStatusMonitor.Instance.StatusLocker)
                {
                    NetDataPacket packet = new NetDataPacket();
                    packet.DataType = NetType.HeartBeat;
                    packet.Data = ScannerStatusMonitor.Instance.Status.ToByteArray<ScannerStatus>();

                    var dataBuffer = packet.ToByteArray();
                    TRSClient.Send(dataBuffer);
                    lastHeartBeat = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                LogUtil.Exception(ex, "HeartBeat", LoggerName);
            }
        }


        #endregion


        #region ClientNet
        private void Client_Connected(object sender, NetSocketConnectedEventArgs e)
        {
            if (TRSClient.State == SocketState.Connected)
            {
                LogUtil.Info("[TRS]...Connected: " + e.SourceIP + DateTime.Now.ToString(), LoggerName);
            }
            //if (timHeartBeat != null && !timHeartBeat.Enabled)
            //{
            //    timHeartBeat.Start();
            //}
            //if (!(IsSingleMode || Global.Instance.Sys.JudgeMode == 0 || !IsRunning || TRSClient.State != SocketState.Connected))
            //{
            //    Task.Run(ScanlineSendThreadRoutine);
            //}
        }
        private void Client_DataArrived(object sender, NetSockDataArrivalEventArgs e)
        {
            NetDataPacket packet = null;
            try
            {
                packet = e.Data.ToDataPacket();
            }
            catch (Exception ex)
            {
                LogUtil.Exception(ex, LoggerName);
                packet = null;
            }

            if (packet == null)
            {
                LogUtil.Error("收到来自安检机的空数据包", LoggerName);
                return;
            }
            HandleCommonData(packet);

        }

        private void Client_Disconnected(object sender, NetSocketDisconnectedEventArgs e)
        {
            if (timReconnect != null && !timReconnect.Enabled)
            {
                timReconnect.Start();
            }
            if (timHeartBeat != null && timHeartBeat.Enabled)
            {
                timHeartBeat.Stop();
            }
        }

        private void Client_ErrorReceived(object sender, NetSockErrorReceivedEventArgs e)
        {
            LogUtil.Exception(e.Exception, e.Function, LoggerName);
        }

        private void Client_StateChanged(object sender, NetSockStateChangedEventArgs e)
        {
            LogUtil.Info(string.Format("client state changed. [{0}] -> [{1}]", e.PrevState, e.NewState));
        }

        #endregion


        #region Methods

        #endregion
        #region Public
        #region login & logout
        /// <summary>
        /// 用户登录
        /// </summary>
        /// <param name="UserAccount">用户登录账户</param>
        /// <param name="Password">明文密码</param>        
        public void UserLogin(string UserAccount, string Password)
        {
            if (!IsRunning)
                return;

            try
            {
                var packet = new NetDataPacket()
                {
                    DataType = NetType.User,
                    Data = new UserLoginRequest()
                    {
                        ScanType = ClientDataType.UserRemoteLogin,
                        UserAccount = UserAccount,
                        Password = Password,
                    }
                .ToByteArray<UserLoginRequest>()
                };
                TRSClient.Send(packet.ToByteArray());
            }
            catch (Exception ex)
            {
                LogUtil.Exception(ex, "UserLogin", LoggerName);
            }
        }

        /// <summary>
        /// 用户退出
        /// </summary>
        public void UserExit()
        {
            if (!IsRunning)
                return;
            try
            {
                var packet = new NetDataPacket()
                {
                    DataType = NetType.User,
                    Data = new UserLoginRequest()
                    {
                        ScanType = ClientDataType.UserRemoteExit
                    }
                .ToByteArray<UserLoginRequest>()
                };
                TRSClient.Send(packet.ToByteArray());
            }
            catch (Exception ex)
            {
                LogUtil.Exception(ex, "UserExit", LoggerName);
            }
        }

        /// <summary>
        /// 验证账户是否能够成功登录安检机的返回
        /// </summary>
        /// <param name="verifyResult">
        /// 登录返回结果
        /// 0-	登录成功
        /// 1-	用名错误
        /// 2-	密码错误
        /// 3-	权限不足
        /// 4-	不是有效账户</param>
        /// <param name="accountJson">账户json格式</param>
        public void VerifyUserLoginOver(int verifyResult, string accountJson = "")
        {
            if (!IsRunning)
            {
                return;
            }
            if (TRSClient?.State != SocketState.Connected)
            {
                return;
            }
            NetDataPacket packet = new NetDataPacket();
            packet.DataType = NetType.User;
            var userResponse = new UserResponse()
            {
                ScanType = ClientDataType.VerifyUserLoginOver,
                VerifyResult = verifyResult,
                Account = accountJson
            };
            packet.Data = userResponse.ToByteArray<UserResponse>();
            try
            {
                TRSClient.Send(packet.ToByteArray());
            }
            catch (Exception)
            {
            }
        }
        /// <summary>
        /// 验证账户是否能够成功登出安检机的返回
        /// </summary>
        /// <param name="verifyResult">
        /// 登出返回结果
        /// 0-	登出成功
        /// 1-	无法登出
        /// </param>
        public void VerifyUserExitOver(int verifyResult)
        {
            if (!IsRunning)
            {
                return;
            }
            if (TRSClient?.State != SocketState.Connected)
            {
                return;
            }
            NetDataPacket packet = new NetDataPacket();
            packet.DataType = NetType.User;
            var userResponse = new UserResponse()
            {
                ScanType = ClientDataType.VerifyUserExitOver,
                VerifyResult = verifyResult,
            };
            packet.Data = userResponse.ToByteArray<UserResponse>();
            try
            {
                TRSClient.Send(packet.ToByteArray());
            }
            catch (Exception)
            {
            }
        }
        #endregion

        #region Scanning



        /// <summary>
        /// 向NetService发送线数据包
        /// </summary>
        /// <param name="line"></param>
        public void AddScanlineData(List<DisplayScanlineDataBundle> lines)
        {
            if (IsSingleMode || Global.Instance.Sys.JudgeMode == 0 || !IsRunning || TRSClient?.State != SocketState.Connected)
                return;
            queueScanlineToSend.Enqueue(lines);
        }

        private void ScanlineSendThreadRoutine()
        {
            // 每过sendInterval时间后把所有数据包发送

            while (IsRunning)
            {
                try
                {
                    if (SendLinesInterval > 0)
                    {
                        Thread.Sleep(SendLinesInterval);
                    }
                
                    if (queueScanlineToSend.Count < 1)
                    {
                        continue;
                    }
                    while (queueScanlineToSend.TryDequeue(out var temp))
                    {
                        // 判断是否满足发送条件
                        if (temp != null && temp.Count > 0)
                        {
                            SendToNetService(temp);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogUtil.Exception(ex, "", LoggerName);
                }
            }
        }



        //private void SendToNetService(DisplayScanlineDataBundle line)
        //{
        //    try
        //    {
        //        var packet = new NetDataPacket()
        //        {
        //            DataType = NetType.ScanlineData,
        //            Data = (new List<DisplayScanlineDataBundle>() { line })
        //            .ToByteArray<List<DisplayScanlineDataBundle>>()
        //        };
        //        TRSClient.Send(packet.ToByteArray());
        //        LogUtil.Info(string.Format("send scanline, lineNum:[{0}], data bytes:[{1}]", line.LineNumber, packet.Data.Length), LoggerName);
        //    }
        //    catch (Exception ex)
        //    {
        //        LogUtil.Exception(ex, "", LoggerName);
        //    }
        //}

        private void SendToNetService(List<DisplayScanlineDataBundle> lines)
        {
            if (TRSClient == null || TRSClient.State != SocketState.Connected)
            {
                return;
            }
            try
            {
                var packet = new NetDataPacket()
                {
                    DataType = NetType.ScanlineData,
                    Data = lines.ToByteArray<List<DisplayScanlineDataBundle>>(),
                };
                TRSClient.Send(packet.ToByteArray());
                LogUtil.Info(string.Format("send scanlines, lineBeginNum:[{0}], lineEndNum:[{1}], data bytes:[{2}]", lines.First().LineNumber, lines.Last().LineNumber, packet.Data.Length), LoggerName);
            }
            catch (Exception ex)
            {
                LogUtil.Exception(ex, "", LoggerName);
            }
        }


        /// <summary>
        /// 更新输送机方向
        /// </summary>
        /// <param name="direction"></param>
        //public void UpdateConvoyerDirection(UI.XRay.Control.ConveyorDirection direction)
        //{
        //}
        public void UpdateXrayGenState(XRayStateChangedEventArgs state)
        {
            //Tracer.TraceInfo(string.Format("[TRS]...xray index:{0},state:{1}", state.XRayGen, state.State));
        }

        public void SendNetworkCommand()
        {
            try
            {
                NetDataPacket packet = new NetDataPacket();
                ControlCommand cmdData = new ControlCommand()
                {
                    CommandType = ControlCommandType.NetWorkCommand,
                };
                packet.DataType = NetType.ControlCommand;
                packet.Data = cmdData.ToByteArray<ControlCommand>();
                TRSClient.Send(packet.ToByteArray());
                LogUtil.Info("Check whether the network is connected");
            }
            catch (Exception ex)
            {
                LogUtil.Exception(ex, "", LoggerName);
            }
        }
        /// <summary>
        /// 包裹开始扫描
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="startLineNo">开始线编号</param>
        /// <param name="xrayPath">xray文件路径</param>
        public void UpdateBagStartInfo(DateTime startTime, int startLineNo, string xrayPath)
        {
            if (IsSingleMode)
                return;
            if (!IsRunning)
                return;
            if (TRSClient.State != SocketState.Connected)
            {
                LogUtil.Error(string.Format("BagStartInfo but socket is not in connection. beginLineNum : [{0}], xrayPath : [{1}]", startLineNo, xrayPath), LoggerName);
                return;
            }
            LogUtil.Info(string.Format("----Bag start info, beginLineNum : [{0}], xrayPath : [{1}]", startLineNo, xrayPath), LoggerName);

            var packet = new NetDataPacket()
            {
                DataType = NetType.ScanningImage,
                Data = new ScanningImageInfo()
                {
                    ScanType = ScanDataType.Begin,
                    BeginLineNumber = startLineNo,
                    LocalXRayFilePath = xrayPath,
                    BeginScanTime = startTime,
                }
                .ToByteArray<ScanningImageInfo>()
            };
            try
            {
                TRSClient.Send(packet.ToByteArray());
                LogUtil.Info(string.Format("----Successfully send bag start info, beginLineNum : [{0}], xrayPath : [{1}], dataBytes:[{2}]", startLineNo, xrayPath, packet.Data.Length), LoggerName);
            }
            catch (Exception ex)
            {
                LogUtil.Exception(ex, "", LoggerName);
            }

            XrayStartInfo startInfo = new XrayStartInfo()
            {
                BeginScanTime = startTime,
                BeginScanLineNumber = startLineNo,
                XrayFilePath = xrayPath
            };
            XrayStartInfo.Add(xrayPath, startInfo);
        }

        public void UpdateBagEndInfo(DateTime endTime, int endLineNo, string xrayPath, string accountId)
        {
            if (IsSingleMode)
                return;
            if (!IsRunning)
                return;
            if (TRSClient.State != SocketState.Connected)
            {
                LogUtil.Error(string.Format("BagEndInfo but socket is not in connection. xrayPath : [{0}]", xrayPath), LoggerName);
                return;
            }
            try
            {

                LogUtil.Info(string.Format("----Bag end info, endLineNum : [{0}], xrayPath : [{1}]", endLineNo, xrayPath), LoggerName);
                if (!XrayStartInfo.ContainsKey(xrayPath))
                {
                    LogUtil.Error("None startInfo found while updating BagEndInfo");
                    return;
                }
                else
                {
                    var info = XrayStartInfo[xrayPath];
                    info.AccountId = accountId;
                    info.EndScanLineNumber = endLineNo;
                    info.EndScanTime = endTime;

                    var packet = new NetDataPacket()
                    {
                        DataType = NetType.ScanningImage,
                        Data = new ScanningImageInfo()
                        {
                            ScanType = ScanDataType.End,
                            BeginLineNumber = info.BeginScanLineNumber,
                            BeginScanTime = info.BeginScanTime,
                            EndLineNumber = info.EndScanLineNumber,
                            EndScanTime = info.EndScanTime,
                            LocalXRayFilePath = info.XrayFilePath,
                        }
                        .ToByteArray<ScanningImageInfo>()
                    };
                    TRSClient.Send(packet.ToByteArray());
                    LogUtil.Info(string.Format("----Successfully send bag end info, endLineNum : [{0}], xrayPath : [{1}], dataBytes:[{2}]", endLineNo, xrayPath, packet.Data.Length), LoggerName);
                }
            }
            catch (Exception e)
            {
                LogUtil.Exception(e, "", LoggerName);
            }
        }

        /// <summary>
        /// 保存完毕XRay图像及附加图像后，向网络服务传递数据
        /// </summary>
        /// <param name="xrayPath">XRay图像存储路径</param>
        /// <param name="savedImages">随XRay图像一同保存的5种可见光图像地址</param>
        public void SendBagSavedInfo(string xrayPath, List<ScannerSavedImage> savedImages)
        {
            if (IsSingleMode)
                return;
            if (!IsRunning)
                return;
            if (TRSClient.State != SocketState.Connected)
            {
                LogUtil.Error(string.Format("BagSavedInfo but socket is not in connection. xrayPath : [{0}]", xrayPath), LoggerName);
                return;
            }

            try
            {
                var packet = new NetDataPacket()
                {
                    DataType = NetType.ScanningImage,
                    Data = new ScanningImageInfo()
                    {
                        ScanType = ScanDataType.Saved,
                        AttachedImages = savedImages,
                        //BeginLineNumber = info.BeginScanLineNumber,
                        //BeginScanTime = info.BeginScanTime,
                        //EndLineNumber = info.EndScanLineNumber,
                        //EndScanTime = info.EndScanTime,
                        LocalXRayFilePath = xrayPath,
                    }
                    .ToByteArray<ScanningImageInfo>()
                };

                TRSClient.Send(packet.ToByteArray());
            }
            catch (Exception ex)
            {
                LogUtil.Exception(ex, "", LoggerName);
            }
        }

        /// <summary>
        /// 更新手动判图信息
        /// </summary>
        /// <param name="time">时间</param>
        /// <param name="rects">框信息</param>
        public void UpdateManualJudgeResult(DateTime time, JudgeResult judgeResult, List<PaintingRectangle> rects, string xrayFilePath = null)
        {
            if (IsSingleMode)
                return;
            if (!IsRunning)
                return;
            if (TRSClient.State != SocketState.Connected)
            {
                LogUtil.Error(string.Format("ManualJudge but socket is not in connection.", LoggerName));
                return;
            }

            try
            {
                Dictionary<string, List<Marker>> imageAndMarkers = new Dictionary<string, List<Marker>>();
                if(rects != null)
                {
                    foreach (var item in rects)
                    {
                        var marker = new Marker()
                        {
                            FromLine = item.FromLine,
                            ToLine = item.ToLine,
                            FromChannel = item.FromChannel,
                            ToChannel = item.ToChannel,
                            Right2Left = item.Right2Left,
                            VerticalFlip = item.Vertical,
                            DetectType = 0,
                            MarkTime = time,
                        };
                        if (ScannerSystemConfig.Instance.ImagesCount > 1)
                        {
                            marker.View = item.View == 2 ? 0 : 1;
                        }
                        else
                        {
                            marker.View = 0;
                        }

                        if (imageAndMarkers.ContainsKey(item.StorePath) == false)
                        {
                            imageAndMarkers.Add(item.StorePath, new List<Marker>() { marker });
                        }
                        else
                        {
                            imageAndMarkers[item.StorePath].Add(marker);
                        }
                    }
                }
                if (imageAndMarkers != null && imageAndMarkers.Count > 0)
                {
                    foreach (var item in imageAndMarkers)
                    {
                        var packet = new NetDataPacket()
                        {
                            DataType = NetType.ScanningImage,
                            Data = new ScanningImageInfo()
                            {
                                ScanType = ScanDataType.ManualJudge,
                                OptJudgeTime = time,
                                Markers = item.Value,
                                LocalXRayFilePath = item.Key,
                                JudgeResult = judgeResult
                            }
                            .ToByteArray<ScanningImageInfo>()
                        };
                        TRSClient.Send(packet.ToByteArray());
                    }
                    return;
                }
                var dataPacket = new NetDataPacket()
                {
                    DataType = NetType.ScanningImage,
                    Data = new ScanningImageInfo()
                    {
                        ScanType = ScanDataType.ManualJudge,
                        OptJudgeTime = time,
                        Markers = null,
                        LocalXRayFilePath = xrayFilePath,
                        JudgeResult = judgeResult
                    }.ToByteArray<ScanningImageInfo>()
                };
                TRSClient.Send(dataPacket.ToByteArray());
                //foreach (var item in imageAndMarkers)
                //{
                //    var packet = new NetDataPacket()
                //    {
                //        DataType = NetType.ScanningImage,
                //        Data = new ScanningImageInfo()
                //        {
                //            ScanType = ScanDataType.ManualJudge,
                //            OptJudgeTime = time,
                //            Markers = item.Value,
                //            LocalXRayFilePath = item.Key,
                //        }
                //        .ToByteArray<ScanningImageInfo>()
                //    };
                //    TRSClient.Send(packet.ToByteArray());
                //}
            }
            catch (Exception ex)
            {
                LogUtil.Exception(ex, "", LoggerName);
            }
        }

        /// <summary>
        /// 更新物质自动识别判图结果
        /// </summary>
        /// <param name="time"></param>
        /// <param name="rects">物质识别标记框</param>
        public void UpdateAutoJudgeResult(DateTime time, MarkerRegion markerRegion, DetectViewIndex view)
        {
            if (IsSingleMode)
                return;
            if (!IsRunning)
                return;
            if (TRSClient.State != SocketState.Connected)
            {
                LogUtil.Error(string.Format("AutoJudge but socket is not in connection.", LoggerName));
                return;
            }

            try
            {
                var marker = new Marker()
                {
                    FromLine = markerRegion.FromLine,
                    ToLine = markerRegion.ToLine,
                    FromChannel = markerRegion.FromChannel,
                    ToChannel = markerRegion.ToChannel,
                    Right2Left = ScannerSystemConfig.Instance.Image1RightToLeft,
                    VerticalFlip = view == DetectViewIndex.View2 ? ScannerSystemConfig.Instance.Image2VerticalFlip : ScannerSystemConfig.Instance.Image1VerticalFlip,
                    MarkTime = time,
                    DetectType = 1,
                    AutoJudgeDangerType = (AutoJudgeDangerType)markerRegion.RegionType
                };
                if (ScannerSystemConfig.Instance.ImagesCount > 1)
                {
                    marker.View = view == DetectViewIndex.View2 ? 0 : 1;
                }
                else
                {
                    marker.View = 0;
                }

                var packet = new NetDataPacket()
                {
                    DataType = NetType.ScanningImage,
                    Data = new ScanningImageInfo()
                    {
                        ScanType = ScanDataType.AutoJudge,
                        AutoJudgeTime = time,
                        Markers = new List<Marker>() { marker }
                    }
                    .ToByteArray<ScanningImageInfo>()
                };
                TRSClient.Send(packet.ToByteArray());
            }
            catch (Exception ex)
            {
                LogUtil.Exception(ex, "", LoggerName);
            }
        }

        #endregion

        #region ControlCommand

        #region Events
        public event EventHandler<ControlCommand> ControlCommandReceived;
        #endregion
        private void HandleCommonData(NetDataPacket data)
        {
            try
            {
                switch (data.DataType)
                {
                    case NetType.HeartBeat:
                    case NetType.ScanningImage:
                    case NetType.ScanlineData:
                        break;
                    case NetType.ControlCommand:
                        var ctrlCmd = data.Data.ToNetEntities<ControlCommand>();
                        if (ControlCommandReceived != null)
                        {
                            ControlCommandReceived(this, ctrlCmd);
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                LogUtil.Exception(ex, "Exceptions in HandleCommonData, DataType:" + data.DataType, LoggerName);
            }
        }


        /// <summary>
        /// 将远程端请求的文件发送至网络服务
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fileData"></param>
        /// <param name="type"></param>
        public void SendFile(string fileName, byte[] fileData, FileType type)
        {
            if (!IsRunning)
            {
                return;
            }
            NetDataPacket packet = new NetDataPacket();
            ControlCommand cmdData = null;
            try
            {
                switch (type)
                {
                    case FileType.Accounts:
                    case FileType.CurrentAccount:
                        cmdData = new ControlCommand()
                        {
                            CommandType = ControlCommandType.AccountCommand,
                        };
                        if (type == FileType.Accounts)
                            cmdData.AccountType = XRayNetEntities.AccountCommandType.SendAccounts;
                        else
                            cmdData.AccountType = XRayNetEntities.AccountCommandType.SendCurrentAccount;
                        break;
                    case FileType.TipPlans:
                    case FileType.TipImageLib:
                    case FileType.TipImages:
                    case FileType.TipLogs:
                        cmdData = new ControlCommand()
                        {
                            CommandType = ControlCommandType.TIPCommand,
                        };
                        if (type == FileType.TipPlans)
                            cmdData.TIPType = XRayNetEntities.TIPCommandType.GetTIPPlan;
                        if (type == FileType.TipImageLib)
                            cmdData.TIPType = XRayNetEntities.TIPCommandType.GetTIPImageLibraries;
                        if (type == FileType.TipImages)
                            cmdData.TIPType = XRayNetEntities.TIPCommandType.GetTIPImages;
                        if (type == FileType.TipLogs)
                            cmdData.TIPType = XRayNetEntities.TIPCommandType.GetTIPLogs;
                        break;
                    case FileType.Diagnosis:
                    case FileType.DiagnosisCanceled:
                    case FileType.DiagnosisFailed:
                        cmdData = new ControlCommand()
                        {
                            CommandType = ControlCommandType.DiagnosticsCommand,
                        };
                        break;
                    case FileType.Image:
                        // 不知道这个图像文件是干嘛的，暂不实现
                        break;
                    default:
                        break;
                }
                if (cmdData == null)
                    return;
                cmdData.FileName = fileName;
                cmdData.FileData = fileData;
                packet.DataType = NetType.ControlCommand;
                packet.Data = cmdData.ToByteArray<ControlCommand>();
            }
            catch (Exception ex)
            {
                LogUtil.Exception(ex, "", LoggerName);
                return;
            }

            try
            {
                TRSClient.Send(packet.ToByteArray());
                LogUtil.Info(string.Format("send controlCommand, commandType:[{0}], FileType:[{1}]", cmdData.CommandType, type), LoggerName);
            }
            catch (Exception ex)
            {
                LogUtil.Exception(ex, "", LoggerName);
                return;
            }
        }

        public void SendDumpFile(byte[] fileData, string localFilePath, string remoteFileDir, string fileName)
        {
            if (!IsRunning)
            {
                return;
            }
            var dumpCmd = new NetworkDumpCommand()
            {
                FileData = fileData,
                LocalFileFullPath = localFilePath,
                RemoteFileDir = remoteFileDir,
                FileName = fileName
            };

            NetDataPacket packet = new NetDataPacket();
            try
            {
                packet.DataType = NetType.NetworkDump;
                packet.Data = dumpCmd.ToByteArray<NetworkDumpCommand>();
            }
            catch (Exception ex)
            {
                LogUtil.Exception(ex, "", LoggerName);
                return;
            }
            try
            {
                TRSClient.Send(packet.ToByteArray());
                LogUtil.Info(string.Format("send dumpToNetwork, fileName:[{0}], remoteFileDir:[{1}]", fileName, remoteFileDir), LoggerName);
            }
            catch (Exception ex)
            {
                LogUtil.Exception(ex, "", LoggerName);
                return;
            }
        }
        /// <summary>
        /// 发送视角截图结果
        /// </summary>
        /// <param name="fileData">图像byte数组</param>
        /// <param name="type">
        /// 0 - 水平视角
        /// 1 - 垂直视角
        /// 2 - 所有视角
        /// </param>
        public void SendScreenshotFile(byte[] fileData, int type)
        {
            if (!IsRunning)
            {
                return;
            }
            if (TRSClient?.State != SocketState.Connected)
            {
                return;

            }
            try
            {

                ImmediateFileCommand fileCommand = new ImmediateFileCommand()
                {
                    ScanType = ClientDataType.ScreenShot,
                    FileData = fileData,
                    ParaData = type
                };
                NetDataPacket packet = new NetDataPacket()
                {
                    DataType = NetType.ImmediateFile,
                    Data = fileCommand.ToByteArray<ImmediateFileCommand>()
                };
                TRSClient.Send(packet.ToByteArray());
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 向远程发起账户导入请求
        /// </summary>
        internal void SendAccountsImportRequest()
        {
            if (!IsRunning)
            {
                return;
            }

            NetDataPacket packet = new NetDataPacket();
            try
            {
                packet.DataType = NetType.AccountsImport;
                packet.Data = null;
            }
            catch (Exception ex)
            {
                LogUtil.Exception(ex, "", LoggerName);
                return;
            }
            try
            {
                TRSClient.Send(packet.ToByteArray());
            }
            catch (Exception ex)
            {
                LogUtil.Exception(ex, "", LoggerName);
                return;
            }
        }
        #endregion
        #endregion
    }
}
