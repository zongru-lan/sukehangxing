using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using UI.AI.Detect.HttpService;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Common.Utilities;
using UI.XRay.Control;
using UI.XRay.Flows.Controllers.SummaryEntity;
using UI.XRay.Flows.Services;
using UI.XRay.Flows.Services.Db;
using UI.XRay.Flows.TRSNetwork;
using UI.XRay.Flows.TRSNetwork.Models;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Flows.Controllers
{
    public class HttpNetworkController
    {
        public static HttpNetworkController Controller { get; private set; }

        static HttpNetworkController()
        {
            Controller = new HttpNetworkController();
        }

        /// <summary>
        /// 电机启停指令
        /// </summary>
        public event Action<ConveyorDirection> ConveyorDirectionAction;
        /// <summary>
        /// 刷新tip注入
        /// </summary>
        public event Action TipInjectUpdateAction;
        public event Action RemoteTipPlanUpdateAction; //yxc
        public event Action<RespMsg> SendingMsgAction;
        public event Action<RespMsg> SendCompleteAction;
        public event Action<string> ReceiveAction;

        public event Action ShowWindowAction;//yxc
        public event Action CloseWindowAction;//yxc

        public event Action ExitSettngWindowAction;//yxc
        private bool _enable = true;
        private string _remoteIp;

        /// <summary>
        /// 条形码元数据委托
        /// </summary>
        public event EventHandler<DcsBagInfoDto> BarcodeMetaDataCommand;

        /// <summary>
        /// 德江大通道本地判图复位委托
        /// </summary>
        public event EventHandler<bool> ResetCommand;

        public bool IsConnected { get; set; }
        private int DisconnectCount = 6;

        /// <summary>服务器回复的网络状态字符串长度</summary>
        private const int netStateStrLen = 5;
        private string netStateStrPattern = @"^[01]{" + netStateStrLen.ToString() + "}";

        public event Action<int> OnScreenshotAction;
        public HttpNetworkController()
        {
            try
            {

                string recvpath = System.Environment.CurrentDirectory + "\\Recv\\";
                if (!Directory.Exists(recvpath))
                {
                    Directory.CreateDirectory(recvpath);
                }
                string temppath = System.Environment.CurrentDirectory + "\\Temp\\";
                if (!Directory.Exists(temppath))
                {
                    Directory.CreateDirectory(temppath);
                }

                ScannerConfig.Read(ConfigPath.SystemHttpServiceIp, out _remoteIp);

                if (!ScannerConfig.Read(ConfigPath.SystemHttpEnable, out _enable))
                {
                    _enable = true;
                }
                if (!_enable) return;

                try
                {
                    NetCommandService.Instance.OnAccountCommand += Instance_OnAccountCommand;
                    NetCommandService.Instance.OnConveyorCommand += Instance_OnConveyorCommand;
                    NetCommandService.Instance.OnDiagnosticsCommand += Instance_OnDiagnosticsCommand;
                    NetCommandService.Instance.OnTipCommand += Instance_OnTipCommand;
                    NetCommandService.Instance.OnWorkModeCommand += Instance_OnWorkModeCommand;
                    NetCommandService.Instance.OnNetworkCommand += Instance_OnNetworkCommand;
                    NetCommandService.Instance.OnKeyboardModeCommand += Instance_OnKeyboardModeCommand;
                    NetCommandService.Instance.OnScreenshotCommand += Instance_OnScreenshotCommand;
                    NetCommandService.Instance.OnBarcodeMetaDataCommand += Instance_OnBarcodeMetaDataCommand;
                    NetCommandService.Instance.OnResetCommand += Instance_OnResetCommand;
                }
                catch (Exception e)
                {
                    Tracer.TraceException(e);
                }
            }
            catch (Exception e)
            {
                Tracer.TraceError(e.ToString());
            }
        }

        private void Instance_OnResetCommand(object sender, bool e)
        {
            if (ResetCommand != null)
                ResetCommand(this, e);
        }

        private void Instance_OnBarcodeMetaDataCommand(object sender, DcsBagInfoDto e)
        {
            if (BarcodeMetaDataCommand != null)
                BarcodeMetaDataCommand(sender, e);
        }

        /// <summary>
        /// 条形码元数据信息处理
        /// 将元数据传递到上层SystemBar中显示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>



        private void Instance_OnScreenshotCommand(int view)
        {
            if (OnScreenshotAction != null)
            {
                OnScreenshotAction(view);
            }
        }

        /// <summary>
        /// 0锁定，1解锁
        /// </summary>
        /// <param name="obj"></param>
        private void Instance_OnKeyboardModeCommand(int isLock)
        {
            LockInputService.Service.BlockInputFunc(isLock < 1);
        }

        private bool firstReceivedNetStates = true;
        /// <summary>
        /// 查询网络状态的回复，目前用byte存储状态
        /// </summary>
        private void Instance_OnNetworkCommand(object sender, object e)
        {
            string netStateStr = e as string;
            if (netStateStr == null || !System.Text.RegularExpressions.Regex.IsMatch(netStateStr, netStateStrPattern))
            {
                Tracer.TraceError($"[Network] Server replied invalid msg for net state: \"{netStateStr}\"");
                return;
            }

            byte netState = 0;
            for (int i = 0; i < netStateStr.Length; i++)
            {
                netState += (byte)((netStateStr[i] - '0') << i);
            }

            SystemStatus.Instance.NetworkState = netState;
            if (firstReceivedNetStates)
            {
                Tracer.TraceInfo($"[Network] First received net states, states: {netStateStr}");
                firstReceivedNetStates = false;
            }
        }

        //public string GetServerIP()
        //{
        //    return _remoteIp;
        //}


        /// <summary>
        /// workmode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Instance_OnWorkModeCommand(object sender, int e)
        {
            ControlService.ServicePart.SetWorkMode((ControlWorkflows.ScannerWorkMode)e);
        }

        /// <summary>
        /// tip
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Instance_OnTipCommand(object sender, TRSNetwork.Models.TIPCommand e)
        {
            if (RemoteTipPlanUpdateAction != null)
            {
                Tracer.TraceInfo("HttpNetworkController on tip command.");
                Transmission.IsRemoteTipProcessing = true;
                RemoteTipPlanUpdateAction();
            }


        }

        /// <summary>
        /// 诊断
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Instance_OnDiagnosticsCommand(object sender, EventArgs e)
        {
            if (SystemStatus.Instance.ConveyorState != 0)
            {
                Diagnosis(true);
            }
            // 有人值守的情况
            else
            {
                //if (ReceiveAction != null)
                //{
                //    ReceiveAction("remote diagnosis request");
                //}
                //else
                // TODO: 弹窗实在整不了，先凑合用吧
                {
                    Diagnosis(false);
                }
            }
        }

        /// <summary>
        /// 输送机
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Instance_OnConveyorCommand(object sender, ConveyorDirection e)
        {
            switch (e)
            {
                case ConveyorDirection.MoveBackward:
                    if (ConveyorDirectionAction != null)
                    {
                        ConveyorDirectionAction(ConveyorDirection.MoveBackward);
                    }
                    break;
                case ConveyorDirection.MoveForward:
                    if (ConveyorDirectionAction != null)
                    {
                        ConveyorDirectionAction(ConveyorDirection.MoveForward);
                    }
                    break;
                case ConveyorDirection.Stop:
                    if (ConveyorDirectionAction != null)
                    {
                        ConveyorDirectionAction(ConveyorDirection.Stop);
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 账户信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Instance_OnAccountCommand(object sender, TRSNetwork.Models.AccountCommand e)
        {
            string filepath;
            switch (e.CommandType)
            {
                case UI.XRay.Flows.TRSNetwork.Models.AccountCommandType.GetAccounts:
                    GetAccounts();
                    break;
                case UI.XRay.Flows.TRSNetwork.Models.AccountCommandType.GetCurrentAccount:
                    GetCurrentAccounts();
                    break;
                case AccountCommandType.RemoteLoginAccount:
                    filepath = SaveFile(e.FileData, "CurrentAccount.xml");
                    var currentAccount = ReadWriteXml.ReadXmlFromFile<Account>(filepath);
                    LoginAccountManager.Service.Login(currentAccount);
                    break;
                //TODO: 导出帐户的操作结果
                case UI.XRay.Flows.TRSNetwork.Models.AccountCommandType.UpdateAccounts:
                    filepath = SaveFile(e.FileData, "Accounts.xml");
                    ManageAccounts(filepath);
                    break;
                case UI.XRay.Flows.TRSNetwork.Models.AccountCommandType.SendAccounts:
                    break;
                case UI.XRay.Flows.TRSNetwork.Models.AccountCommandType.SendCurrentAccount:
                    break;
                case AccountCommandType.VerifyUserLogin:
                    {
                        if (sender is null)
                        {
                            // 传入参数不合法，不用处理
                            break;
                        }

                        var userAndPwdStr = sender.ToString();
                        int seperatorIndex = userAndPwdStr.IndexOf(':');
                        if (seperatorIndex <= 0)
                        {
                            // 传入参数不合法，不用处理
                            break;
                        }
                        string userID = userAndPwdStr.Substring(0, userAndPwdStr.IndexOf(':'));
                        string password = userAndPwdStr.Substring(seperatorIndex + 1);

                        TRSNetWorkService.Service.VerifyUserLoginOver(VerifyAccount(userID, password));
                    }
                    break;
                case AccountCommandType.VerifyUserExit:
                    {
                        if (sender is null)
                        {
                            // 传入参数不合法，不用处理
                            break;
                        }
                        var userAndPwdStr = sender.ToString();
                        int seperatorIndex = userAndPwdStr.IndexOf(':');
                        if (seperatorIndex <= 0)
                        {
                            // 传入参数不合法，不用处理
                            break;
                        }
                        string userID = userAndPwdStr.Substring(0, userAndPwdStr.IndexOf(':'));
                        string password = userAndPwdStr.Substring(seperatorIndex + 1);

                        // 根据用户名以及密码校验能否登录/登出到本机，仅作校验，不用登录/登出
                        // 将校验结果通过XRayNetService返回
                        // 当前用户退出没有验证
                        TRSNetWorkService.Service.VerifyUserExitOver(0);
                    }
                    break;

                    // TODO:NET 账户远程导出导入
                case AccountCommandType.SendAccountsOver:
                    // 导出成功，则为远程文件全路径，导出失败，为null或空字符。
                    string exportFilePath = sender?.ToString();
                    // 导出结果字符信息
                    string message = Encoding.UTF8.GetString(e.FileData);
                    break;
                case AccountCommandType.ImportAccountsOver:
                    // 请求远程导入账户的路径, 根据此路径打开远程文件夹, 一般不会为空，建议将该值进行内存缓存，只在程序首次调用时进行请求。
                    string importFileDirectory = sender?.ToString();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 返回结果
        /// 0-	登录成功
        /// 1-	用名错误
        /// 2-	密码错误
        /// 3-	权限不足
        /// 4-	不是有效账户</param>
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        private int VerifyAccount(string userID,string password)
        {
            Account loginAccount = null;
            try
            {
                var controller = new AccountDbSet();
                loginAccount = controller.Find(userID);
            }
            catch (Exception exception)
            {
                return 4;
            }

            if (loginAccount != null && loginAccount.IsEnable)
            {
                if (string.Equals(loginAccount.Password, password, StringComparison.OrdinalIgnoreCase))
                {

                    if (loginAccount.IsActive)
                    {
                        return 0;
                    }
                    else
                    {
                        return 4;
                    }
                }
                else
                {
                    return 2;
                }
            }
            else
            {
                return 1;
            }
        }


        private string SaveFile(byte[] data, string name)
        {
            string fullpathname = System.Environment.CurrentDirectory + "\\Recv\\" + name;
            if (File.Exists(fullpathname)) File.Delete(fullpathname);
            using (FileStream fs = new FileStream(fullpathname, FileMode.OpenOrCreate, FileAccess.Write))
            {
                fs.Write(data, 0, data.Length);
            }
            return fullpathname;
        }


        public void Close()
        {
            if (_enable)
            {
                if (NetCommandService.Instance != null)
                {
                    NetCommandService.Instance.OnAccountCommand -= Instance_OnAccountCommand;
                    NetCommandService.Instance.OnConveyorCommand -= Instance_OnConveyorCommand;
                    NetCommandService.Instance.OnDiagnosticsCommand -= Instance_OnDiagnosticsCommand;
                    NetCommandService.Instance.OnTipCommand -= Instance_OnTipCommand;
                }
                LockInputService.Service.Close();
            }
        }

        public void Diagnosis(bool isDiagnosisCanceled)
        {
            if (isDiagnosisCanceled)
            {
                NetCommandService.Instance.SendResultFile("Diagnosis Canceled", new byte[0], FileType.DiagnosisCanceled);
            }
            else
            {
                GetDiagnosisAction();
            }
        }

        private void GetDiagnosisAction()
        {
            ControlService.ServicePart.RadiateXRay(false);
            ControlService.ServicePart.DriveConveyor(ConveyorDirection.Stop);
            //if (ReceiveAction != null)
            //{
            //    ReceiveAction("diagnosis warning");
            //}
            if (ExitSettngWindowAction != null)
                ExitSettngWindowAction();
            if (ShowWindowAction != null)
                ShowWindowAction();   //yxc

            GetDiagnosis();

        }

        /// <summary>
        /// 网络登录
        /// </summary>
        /// <param name="str"></param>
        //public void LoginInNetwork(string id, AccountRole role, int permission)
        //{
        //    Account account = new Account(id, "", "", role, permission, isNetAccount: true);
        //    LoginAccountManager.Service.Login(account);
        //    if (ReceiveAction != null)
        //    {
        //        ReceiveAction("autologin");
        //    }
        //}
        /// <summary>
        /// 清空Tip图像目录
        /// </summary>
        //private void ClearTipImageDir()
        //{
        //    var tipPlanManager = new TipPlanDbSet();
        //    var plans = tipPlanManager.SelectAll();
        //    var planIsEnabled = plans.Where(p => p.IsEnabled == true).ToList();
        //    TipImagesManagementController control = new TipImagesManagementController(TipLibrary.Guns);
        //    if (planIsEnabled.Count > 0)
        //    {
        //        if (planIsEnabled[0].GunsWeight == 0)
        //            control.DeleteAllImages();
        //        if (planIsEnabled[0].ExplosivesWeight == 0)
        //        {
        //            control = new TipImagesManagementController(TipLibrary.Explosives);
        //            control.DeleteAllImages();
        //        }
        //        if (planIsEnabled[0].KnivesWeight == 0)
        //        {
        //            control = new TipImagesManagementController(TipLibrary.Knives);
        //            control.DeleteAllImages();
        //        }
        //        if (planIsEnabled[0].OtherObjectsWeight == 0)
        //        {
        //            control = new TipImagesManagementController(TipLibrary.Others);
        //            control.DeleteAllImages();
        //        }
        //    }
        //    else
        //    {
        //        control.DeleteAllImages();
        //        control = new TipImagesManagementController(TipLibrary.Explosives);
        //        control.DeleteAllImages();
        //        control = new TipImagesManagementController(TipLibrary.Knives);
        //        control.DeleteAllImages();
        //        control = new TipImagesManagementController(TipLibrary.Others);
        //        control.DeleteAllImages();
        //    }
        //    TipPlanandImageController tipcontrol = new TipPlanandImageController();
        //    List<TipPlanandImage> _originlist = tipcontrol.GetAllPlans().ToList();
        //    tipcontrol.RemoveRange(_originlist);
        //    _originlist = new List<TipPlanandImage>();

        //}


        //private void ManageTipImages(List<string> ImageList)
        //{
        //    List<string> _gunslist = new List<string>();
        //    List<string> _kniveslist = new List<string>();
        //    List<string> _explosiveslist = new List<string>();
        //    List<string> _otherslist = new List<string>();

        //    foreach (string file in ImageList)
        //    {
        //        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
        //        var fileNameWithExt = Path.GetFileName(file);
        //        string type = fileNameWithoutExt.Split('_')[0].ToLower();
        //        string fullpathname = System.Environment.CurrentDirectory + "\\Recv\\" + fileNameWithExt;
        //        switch (type)
        //        {
        //            case "gun":
        //                _gunslist.Add(fullpathname);
        //                break;
        //            case "knife":
        //                _kniveslist.Add(fullpathname);
        //                break;
        //            case "explosive":
        //                _explosiveslist.Add(fullpathname);
        //                break;
        //            default:
        //                _otherslist.Add(fullpathname);
        //                break;
        //        }
        //    }

        //    TipImagesManagementController control = new TipImagesManagementController(TipLibrary.Guns);
        //    control.ImportImagesToLib(_gunslist);
        //    control = new TipImagesManagementController(TipLibrary.Explosives);
        //    control.ImportImagesToLib(_explosiveslist);
        //    control = new TipImagesManagementController(TipLibrary.Knives);
        //    control.ImportImagesToLib(_kniveslist);
        //    control = new TipImagesManagementController(TipLibrary.Others);
        //    control.ImportImagesToLib(_otherslist);
        //}

        /// <summary>
        /// 更新收到的Tip计划并重置TipFlow
        /// </summary>
        /// <param name="PlanList"></param>
        //private void ManageTipPlan(List<string> PlanList)
        //{
        //    if (PlanList.Count < 1) return;
        //    List<TipPlan> _list = new List<TipPlan>();
        //    TipPlansController control = new TipPlansController();
        //    List<TipPlan> _originlist = control.GetAllPlans().ToList();

        //    foreach (string file in PlanList)
        //    {
        //        _list = ReadWriteXml.ReadXmlFromFile<List<TipPlan>>(file);
        //        if (_list == null) continue;
        //        if (_list.Count < 1)
        //        {
        //            foreach (TipPlan plan in _originlist)
        //            {

        //                control.Remove(plan);
        //                control.SaveChanges();
        //            }
        //        }
        //        else
        //        {
        //            if (_originlist.Count <= _list.Count)
        //            {
        //                foreach (var tip in _list)
        //                {
        //                    control.AddOrUpdate(tip);
        //                    control.SaveChanges();
        //                }
        //            }
        //            else
        //            {
        //                foreach (var tip in _originlist)
        //                {
        //                    if (!_list.Any(p => p.TipPlanId == tip.TipPlanId))
        //                    {
        //                        control.Remove(tip);
        //                        control.SaveChanges();
        //                    }
        //                }
        //                foreach (var tip in _list)
        //                {
        //                    control.AddOrUpdate(tip);
        //                    control.SaveChanges();
        //                }
        //            }
        //        }
        //    }
        //    if (TipInjectUpdateAction != null)
        //    {
        //        Thread.Sleep(500);
        //        TipInjectUpdateAction();
        //    }
        //    if (ReceiveAction != null)
        //    {
        //        ReceiveAction("tipplans updated");
        //    }
        //}

        //private void ManageTipandImage(List<string> PlanandImageList)
        //{
        //    if (PlanandImageList.Count < 1) return;
        //    List<TipPlanandImage> _list = new List<TipPlanandImage>();
        //    TipPlanandImageController control = new TipPlanandImageController();
        //    List<TipPlanandImage> _originlist = control.GetAllPlans().ToList();

        //    foreach (string file in PlanandImageList)
        //    {
        //        _list = ReadWriteXml.ReadXmlFromFile<List<TipPlanandImage>>(file);
        //        if (_list == null || _list.Count < 1) continue;
        //        control.RemoveRange(_originlist);
        //        foreach (var tip in _list)
        //        {
        //            control.AddOrUpdate(tip);
        //            control.SaveChanges();
        //        }
        //    }
        //}

        private void ManageAccounts(string accoutsPath)
        {
            //if (LoginAccountManager.Service.CurrentAccount == null) return;
            if (!File.Exists(accoutsPath))
                return;

            AccountDbSet _manager = new AccountDbSet();
            List<Business.Entities.Account> _allAccountsInDb;


            try
            {
                var newAccounts = new ObservableCollection<Business.Entities.Account>();

                PermissionService.Service.ImportAccountList(newAccounts, accoutsPath);

                _allAccountsInDb = _manager.SelectAll();

                var manageableAccounts = new ObservableCollection<Business.Entities.Account>(_allAccountsInDb);
                var deletedAccounts = new List<Account>();
            
                if (newAccounts.Count > 0)
                {
                    foreach (var item in newAccounts)
                    {
                        item.Role=AccountRole.Operator;
                        item.IsNetAccount = true;

                        _manager.AddOrUpdate(item);

                    }
              
                    _manager.SaveChanges();

                    if (ReceiveAction != null)
                    {
                        ReceiveAction("accounts updated");
                    }
                }
                

             
            }
            catch (Exception e)
            {

            }
        }

        private void GetAccounts()
        {
            var _fileName = "Accounts.xml";
            string _xmlFile = System.Environment.CurrentDirectory + "\\Recv\\" + _fileName;
            if (File.Exists(_xmlFile)) File.Delete(_xmlFile);

            var _manager = new AccountDbSet();
            var accountlist = _manager.SelectAll();
            var ob = new ObservableCollection<Account>(accountlist);
            PermissionService.Service.ExportAccountList(ob, _xmlFile);

            byte[] Data = ReadFileToBytes.ReadDataFromFile(_xmlFile);

            NetCommandService.Instance.SendResultFile(_fileName, Data, TRSNetwork.Models.FileType.Accounts);
        }

        /// <summary>
        /// 当前用户
        /// </summary>
        private void GetCurrentAccounts()
        {
            var _fileName = "CurrentAccounts.xml";
            string _xmlFile = System.Environment.CurrentDirectory + "\\Recv\\" + _fileName;
            if (File.Exists(_xmlFile)) File.Delete(_xmlFile);

            var result = PermissionService.Service.ExportCurrentAccount(_xmlFile);
            if (result)
            {
                byte[] Data = ReadFileToBytes.ReadDataFromFile(_xmlFile);
                NetCommandService.Instance.SendResultFile(_fileName, Data, TRSNetwork.Models.FileType.Accounts);
            }
            else
            {
                NetCommandService.Instance.SendResultFile(_fileName, null, TRSNetwork.Models.FileType.Accounts);
            }
        }

        //private void GetTipPlans()
        //{
        //    var _fileName = "TipPlans.xml";
        //    string _xmlFile = System.Environment.CurrentDirectory + "\\Recv\\" + _fileName;
        //    if (File.Exists(_xmlFile)) File.Delete(_xmlFile);

        //    ObservableCollection<TipPlan> Plans;
        //    TipPlansController _controller = new TipPlansController();
        //    Plans = _controller.GetAllPlans();

        //    using (StringWriter sw = new StringWriter())
        //    {
        //        XmlSerializer serializer = new XmlSerializer(typeof(List<TipPlan>));
        //        serializer.Serialize(sw, Plans.ToList());

        //        using (FileStream fsWrite = new FileStream(_xmlFile, FileMode.OpenOrCreate, FileAccess.Write))
        //        {
        //            byte[] buffer = Encoding.Default.GetBytes(sw.ToString());
        //            fsWrite.Write(buffer, 0, buffer.Length);
        //        }
        //    }


        //    byte[] Data = ReadFileToBytes.ReadDataFromFile(_xmlFile);
        //    NetCommandService.Instance.SendResultFile(_fileName, Data, TRSNetwork.Models.FileType.TipPlans);
        //}

        /// <summary>
        /// 将安检机图像转存到网络路径上
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="imagePath"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool SendFileTo(string filename, string imagePath, FileType type, string remoteFileDir = "", byte[] fileData = null)
        {
            try
            {
                if (_enable)
                {
                    if (type == FileType.Image)
                    {
                        NetCommandService.Instance.SendDumpFileToRemote(fileData, filename, imagePath, remoteFileDir);
                    }
                    else
                    {
                        byte[] Data = PostData.ReadDataFromFile(imagePath);
                        NetCommandService.Instance.SendResultFile(filename, Data, type);
                    }
                }
            }
            catch (Exception e)
            {
                return false;
            }

            return _enable;
        }

        //private void GetTipRecord(string[] info)
        //{
        //    var id = info[1];
        //    var period = info[2];
        //    var year = int.Parse(info[3]);
        //    var month = int.Parse(info[4]);
        //    var _controller = new OperationLogsRetrievalController();
        //    _controller.SelectedAccountId = id;
        //    _controller.SelectedMonth = month;
        //    _controller.SelectedYear = year;
        //    _controller.SelectedPeriod = (Business.Entities.Enums.StatisticalPeriod)Enum.Parse(typeof(Business.Entities.Enums.StatisticalPeriod), period, true);

        //    var _statisticResults = _controller.GetStatisticalResults();
        //    var _fileName = "TipLog_" + id + "_" + period + "_" + year + "_" + month + ".xml";

        //    string _xmlFile = System.Environment.CurrentDirectory + "\\Recv\\" + _fileName;
        //    if (File.Exists(_xmlFile)) File.Delete(_xmlFile);
        //    ReadWriteXml.WriteXmlToFile<List<OperationStatisticResult>>(_statisticResults, _xmlFile);

        //    byte[] Data = ReadFileToBytes.ReadDataFromFile(_xmlFile);
        //    NetCommandService.Instance.SendResultFile(_fileName, Data, TRSNetwork.Models.FileType.TipLogs);
        //}


        private async void GetDiagnosis()
        {
            var txt = "";
            try
            {

                var task = SystemStateService.Service.GetDiagnosisReport();
                if (task != null)
                {
                    txt = await task;
                }

                var _fileName = "Diagnosis_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
                string _xmlFile = System.Environment.CurrentDirectory + "\\Recv\\" + _fileName;
                if (File.Exists(_xmlFile)) File.Delete(_xmlFile);


                byte[] data = System.Text.Encoding.Default.GetBytes(txt);

                using (FileStream fs = new FileStream(_xmlFile, FileMode.Create))
                {
                    fs.Write(data, 0, data.Length);
                    fs.Flush();
                }

                NetCommandService.Instance.SendResultFile(_fileName, data, TRSNetwork.Models.FileType.Diagnosis);



            }
            catch (Exception e)
            {
                //byte[] exceptionMessage = System.Text.Encoding.Default.GetBytes(e.Message);
                NetCommandService.Instance.SendResultFile("Diagnosis Failed", new byte[0], TRSNetwork.Models.FileType.DiagnosisFailed);

            }
            finally
            {
                if (CloseWindowAction != null)
                    CloseWindowAction();   //yxc
            }
        }

        //private void GetTipLibs()
        //{
        //    List<int> tipLibs = new List<int>();
        //    TipImagesManagementController _managementController = new TipImagesManagementController(TipLibrary.Knives);
        //    int TotalImagesCountKnives = _managementController.ImagesCount;
        //    tipLibs.Add(TotalImagesCountKnives);
        //    _managementController = new TipImagesManagementController(TipLibrary.Guns);
        //    int TotalImagesCountGuns = _managementController.ImagesCount;
        //    tipLibs.Add(TotalImagesCountGuns);
        //    _managementController = new TipImagesManagementController(TipLibrary.Explosives);
        //    int TotalImagesCountExplosives = _managementController.ImagesCount;
        //    tipLibs.Add(TotalImagesCountExplosives);
        //    _managementController = new TipImagesManagementController(TipLibrary.Others);
        //    int TotalImagesCountOthers = _managementController.ImagesCount;
        //    tipLibs.Add(TotalImagesCountOthers);

        //    var _fileName = "TipLibs.xml";
        //    string _xmlFile = System.Environment.CurrentDirectory + "\\Recv\\" + _fileName;
        //    if (File.Exists(_xmlFile)) File.Delete(_xmlFile);

        //    using (StringWriter sw = new StringWriter())
        //    {
        //        XmlSerializer serializer = new XmlSerializer(typeof(List<int>));
        //        serializer.Serialize(sw, tipLibs);
        //        using (FileStream fsWrite = new FileStream(_xmlFile, FileMode.OpenOrCreate, FileAccess.Write))
        //        {
        //            byte[] buffer = Encoding.Default.GetBytes(sw.ToString());
        //            fsWrite.Write(buffer, 0, buffer.Length);
        //        }
        //    }

        //    byte[] Data = ReadFileToBytes.ReadDataFromFile(_xmlFile);
        //    NetCommandService.Instance.SendResultFile(_fileName, Data, TRSNetwork.Models.FileType.TipImageLib);
        //}

        //private void GetTipImages()
        //{
        //    var _fileName = "TipImages.xml";
        //    string _xmlFile = System.Environment.CurrentDirectory + "\\Recv\\" + _fileName;
        //    if (File.Exists(_xmlFile)) File.Delete(_xmlFile);

        //    ObservableCollection<TipPlanandImage> PlanandImages;
        //    TipPlanandImageController _controller = new TipPlanandImageController();
        //    PlanandImages = _controller.GetAllPlans();

        //    using (StringWriter sw = new StringWriter())
        //    {
        //        XmlSerializer serializer = new XmlSerializer(typeof(List<TipPlanandImage>));
        //        serializer.Serialize(sw, PlanandImages.ToList());

        //        using (FileStream fsWrite = new FileStream(_xmlFile, FileMode.OpenOrCreate, FileAccess.Write))
        //        {
        //            byte[] buffer = Encoding.Default.GetBytes(sw.ToString());
        //            fsWrite.Write(buffer, 0, buffer.Length);
        //        }
        //    }

        //    byte[] Data = ReadFileToBytes.ReadDataFromFile(_xmlFile);
        //    NetCommandService.Instance.SendResultFile(_fileName, Data, TRSNetwork.Models.FileType.TipImages);
        //}
    }
}
