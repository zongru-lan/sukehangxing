using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Control;
using UI.XRay.Flows.TRSNetwork.Models;
using XRayNetEntities.Tools;

namespace UI.XRay.Flows.TRSNetwork
{
    public class NetCommandService
    {
        #region Instance & Constructor

        private static NetCommandService _instance;

        public static NetCommandService Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new NetCommandService();
                return _instance;
            }
        }

        private bool _isRunning;
        private string LoggerName = "ControlCommand";
        public NetCommandService()
        {

        }

        public void Start()
        {
            _isRunning = true;
            TRSNetWorkService.Service.ControlCommandReceived += Service_ControlCommandReceived;
        }


        public void Stop()
        {
            _isRunning = false;
        }
        #endregion


        #region Public Commands


        #region Events
        public event EventHandler<ConveyorDirection> OnConveyorCommand;

        /// <summary>
        /// 接收到来自远程的TIP指令
        /// </summary>
        public event EventHandler<TIPCommand> OnTipCommand;
        /// <summary>
        /// 接收到来自远程的账户指令
        /// </summary>
        public event EventHandler<AccountCommand> OnAccountCommand; // Net  南航SDK适配1.0版本，根据
                                                                    // TODO:NET 账户导入导出返回结果，由该事件获取
        /// <summary>
        /// 接收到来自远程的诊断指令
        /// </summary>
        public event EventHandler OnDiagnosticsCommand;
        /// <summary>
        /// 接收到来自远程的工作模式切换指令
        /// </summary>
        public event EventHandler<int> OnWorkModeCommand;

        /// <summary>
        /// 接收网络状态指令
        /// </summary>
        public event EventHandler<object> OnNetworkCommand;

        /// <summary>
        /// 接收视角截图指令
        /// 参数:int 0:水平视角 1:垂直视角 2:全视角
        /// </summary>
        public event Action<int> OnScreenshotCommand;         // Net  南航SDK适配1.0版本
        /// <summary>
        /// 接收键鼠锁定模式指令
        /// 参数:int 0:锁定 1:解锁
        /// </summary>
        public event Action<int> OnKeyboardModeCommand;       // Net  南航SDK适配1.0版本
        /// <summary>
        /// 导出文件到远程的响应结果
        /// <list type="table">
        /// <item>arg1(string) - 文件名</item>
        /// <item>arg2(string) - 远程路径, 为null或空字符表示导出失败</item> 
        /// <item>arg3(string) - 消息字符</item>
        /// </list>
        /// </summary>
        public event Action<string, string, string> OnDumpFileOverCommand;      // TODO: NET 民航认证图像远程导出返回值

        /// <summary>
        /// 条形码元数据命令
        /// </summary>
        public event EventHandler<DcsBagInfoDto> OnBarcodeMetaDataCommand;
        /// <summary>
        /// 重置命令(德江大通道本地判图复位命令)
        /// </summary>
        public event EventHandler<bool> OnResetCommand;
        #endregion

        #region Commands Response

        public void SendGetNetworkState()
        {
            TRSNetWorkService.Service.SendNetworkCommand();
        }

        public void SendTipLog(TipLogStatisticResult tipLog)
        {
            byte[] data = null;
            try
            {
                var tipLogXmlString = XmlUtil.Serialize(tipLog);
                data = Encoding.UTF8.GetBytes(tipLogXmlString);
            }
            catch (Exception ex)
            {
                LogUtil.Exception(ex);
            }
            if (data == null || data.Length == 0)
            {
                return;
            }
            SendResultFile("TipLog", data, FileType.TipLogs);
        }
        /// <summary>
        /// 发送诊断结果xml文件
        /// </summary>
        /// <param name="fileData"></param>
        public void SendResultFile(string fileName, byte[] fileData, FileType type)
        {
            if (!_isRunning)
            {
                return;
            }
            TRSNetWorkService.Service.SendFile(fileName, fileData, type);
        }

        /// <summary>
        /// 将图像转存至网络，若fileData为空，则需传递文件本地全路径
        /// </summary>
        /// <param name="fileData">文件数据</param>
        /// <param name="localFileFullPath">本地图像的全路径</param>
        /// <param name="fileName">文件名</param>
        /// <param name="remoteFilePath">远程存储路径</param>
        public void SendDumpFileToRemote(byte[] fileData, string fileName, string localFileFullPath = "", string remoteFilePath = "")
        {
            if (!_isRunning)
                return;
            TRSNetWorkService.Service.SendDumpFile(fileData, localFileFullPath, remoteFilePath, fileName);
        }

        // TODO:NET 账户远程导入时，调用该方法获取远程路径

        /// <summary>
        /// 向对端发送账户导入请求
        /// </summary>
        public void SendImportAccountsRequest()
        {
            TRSNetWorkService.Service.SendAccountsImportRequest();
        }

        void Service_ControlCommandReceived(object sender, XRayNetEntities.ControlCommand e)
        {
            if (!_isRunning)
            {
                return;
            }
            if (e == null)
            {
                LogUtil.Error("接收到空的ControlCommand数据包", LoggerName);
                return;
            }
            LogUtil.Info("接收到控制指令, commandType = " + e.CommandType, LoggerName);
            switch (e.CommandType)
            {
                case XRayNetEntities.ControlCommandType.AccountCommand:
                    if (OnAccountCommand != null )
                    {
                        if(e.AccountType == XRayNetEntities.AccountCommandType.UpdateAccounts)
                        {
                           var ResultFile=XmlUtil.Deserialize<List<UI.XRay.Business.Entities.Account>>(Encoding.UTF8.GetString(e.FileData)); //yxc

                        }
                        // CommandInfo里，如果是验证本地账户能否登录登出，则为"{userID}:{password}"形式的字符串
                        OnAccountCommand(e.CommandInfo?.ToString(), new AccountCommand((AccountCommandType)((int)e.AccountType), e.FileData));
                    }
                    break;
                case XRayNetEntities.ControlCommandType.ConveyorCommand:
                    LogUtil.Info("接收到电机控制指令, conveyor is set to be " + e.RunDirection);
                    if (OnConveyorCommand != null)
                    {
                        OnConveyorCommand(this, (ConveyorDirection)((int)e.RunDirection));
                    }
                    break;
                case XRayNetEntities.ControlCommandType.DiagnosticsCommand:
                    if (OnDiagnosticsCommand != null)
                    {
                        OnDiagnosticsCommand(this, EventArgs.Empty);
                    }
                    break;
                case XRayNetEntities.ControlCommandType.TIPCommand:
                    if (OnTipCommand != null)
                    {
                        OnTipCommand(this, new TIPCommand((TIPCommandType)((int)e.TIPType), e.FileData, e.FileName));
                    }
                    break;
                case XRayNetEntities.ControlCommandType.WorkModeCommand:
                    LogUtil.Info("接收到工作模式控制指令, scanner work mode is set to be " + e.ScannerWorkMode);
                    if (OnWorkModeCommand != null)
                    {
                        OnWorkModeCommand(this, e.ScannerWorkMode);
                    }
                    break;
                case XRayNetEntities.ControlCommandType.NetWorkCommand:
                    LogUtil.Info("接收到网站状态指令 " );
                    if (OnNetworkCommand != null)
                    {
                        OnNetworkCommand(this, e.CommandInfo);
                    }
                    break;
                case XRayNetEntities.ControlCommandType.KeyboardModeCommand:
                    OnKeyboardModeCommand?.Invoke((int)e.KeyboardType);
                    break;
                case XRayNetEntities.ControlCommandType.ScreenShotCommand:
                    OnScreenshotCommand?.Invoke((int)e.ScreenshotType);
                    break;
                case XRayNetEntities.ControlCommandType.DumpFileOverCommand:
                    OnDumpFileOverCommand?.Invoke(e.FileName, e.CommandInfo?.ToString(), Encoding.UTF8.GetString(e.FileData));
                    break;
                case XRayNetEntities.ControlCommandType.BarcodeMetaDataCommand: // 接收离港信息
                    if (e.CommandInfo is string infoJson)
                    {
                        var infoDto = Deserialize<DcsBagInfoDto>(infoJson);
                        if (OnBarcodeMetaDataCommand != null)
                            OnBarcodeMetaDataCommand(this, infoDto);
                    }
                    break;
                case XRayNetEntities.ControlCommandType.ResetCommand:
                    if (OnResetCommand != null)
                        OnResetCommand(this, true);
                    break;
                default:
                    break;
            }
        }

        #endregion

        #endregion
        public static T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static string Serialize<T>(T data)
        {
            return JsonConvert.SerializeObject(data, Formatting.None);
        }
    }
}
