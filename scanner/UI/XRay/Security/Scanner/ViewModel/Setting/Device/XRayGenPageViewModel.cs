using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms.VisualStyles;
using System.Windows.Threading;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Control;
using UI.XRay.Flows.Controllers;
using UI.XRay.Flows.Services;
using UI.XRay.Gui.Framework;
using UI.XRay.Security.Scanner.Properties;
using UI.XRay.Flows.Services.Db;

namespace UI.XRay.Security.Scanner.ViewModel.Setting.Device
{
    public class XRayGenPageViewModel : PageViewModelBase
    {
        private XRayGenSettingController _settingController;

        #region 绑定属性
        public RelayCommand<String> ResetButtonPressCommand { get; private set; }

        private XRayGeneratorType? _type = null;

        private double _kvSetting;

        private double _maSetting;

        private double? _kvWorking;

        private double? _maWorking;

        private double _kvNew;

        private double _maNew;

        private double _kvSetting2;

        private double _maSetting2;

        private double? _kvWorking2;

        private double? _maWorking2;

        private double _kvNew2;

        private double _maNew2;

        private int _gen1OilTemp;

        private int _gen2OilTemp;

        private int _gen1ErrorCode;

        private int _gen2ErrorCode;

        private Visibility _gen2Visibility = Visibility.Collapsed;

        private Visibility _oilTempuratureVisibility = Visibility.Collapsed;

        private Visibility _xrayGenErorVisibility = Visibility.Collapsed;

        private double _totalXRayGenWorkHours;

        private double _xRayGenWorkHoursSinceBoot;

        private int _xrayGenUsageCountSinceBoot;

        private int _xrayGenTotalUsageCount;

        private double _totalXRayGen1WorkHours;

        private int _xrayGen1TotalUsageCount;

        private double _totalXRayGen2WorkHours;

        private int _xrayGen2TotalUsageCount;

        public int Gen1OilTemp
        {
            get { return _gen1OilTemp; }
            set { _gen1OilTemp = value; RaisePropertyChanged(); }
        }

        public int Gen2OilTemp
        {
            get { return _gen2OilTemp; }
            set { _gen2OilTemp = value; RaisePropertyChanged(); }
        }

        public int Gen1ErrorCode
        {
            get { return _gen1ErrorCode; }
            set { _gen1ErrorCode = value; RaisePropertyChanged(); }
        }

        public int Gen2ErrorCode
        {
            get { return _gen2ErrorCode; }
            set { _gen2ErrorCode = value; RaisePropertyChanged(); }
        }

        public XRayGeneratorType? Type
        {
            get { return _type; }
            set { _type = value; RaisePropertyChanged(); }
        }

        public Visibility OilTempuratureVisibility
        {
            get { return _oilTempuratureVisibility; }
            set { _oilTempuratureVisibility = value; RaisePropertyChanged(); }
        }

        public Visibility Gen2Visibility
        {
            get { return _gen2Visibility; }
            set { _gen2Visibility = value; RaisePropertyChanged(); }
        }

        public Visibility GenErrorVisibility
        {
            get { return _xrayGenErorVisibility; }
            set { _xrayGenErorVisibility = value; RaisePropertyChanged(); }
        }
        public double KvSetting
        {
            get { return _kvSetting; }
            set { _kvSetting = value; RaisePropertyChanged(); }
        }

        public double MaSetting
        {
            get { return _maSetting; }
            set { _maSetting = value; RaisePropertyChanged(); }
        }

        public double? KvWorking
        {
            get { return _kvWorking; }
            set { _kvWorking = value; RaisePropertyChanged(); }
        }

        public double? MaWorking
        {
            get { return _maWorking; }
            set { _maWorking = value; RaisePropertyChanged(); }
        }

        public double KvNew
        {
            get { return _kvNew; }
            set
            {
                _kvNew = value;
                RaisePropertyChanged();
            }
        }

        public double MaNew
        {
            get { return _maNew; }
            set { _maNew = value; RaisePropertyChanged(); }
        }

        public double KvSetting2
        {
            get { return _kvSetting2; }
            set { _kvSetting2 = value; RaisePropertyChanged(); }
        }

        public double MaSetting2
        {
            get { return _maSetting2; }
            set { _maSetting2 = value; RaisePropertyChanged(); }
        }

        public double? KvWorking2
        {
            get { return _kvWorking2; }
            set { _kvWorking2 = value; RaisePropertyChanged(); }
        }

        public double? MaWorking2
        {
            get { return _maWorking2; }
            set { _maWorking2 = value; RaisePropertyChanged(); }
        }

        public double KvNew2
        {
            get { return _kvNew2; }
            set
            {
                _kvNew2 = value;
                RaisePropertyChanged();
            }
        }

        public double MaNew2
        {
            get { return _maNew2; }
            set { _maNew2 = value; RaisePropertyChanged(); }
        }

        public double TotalXRayGenWorkHours
        {
            get { return _totalXRayGenWorkHours; }
            set { _totalXRayGenWorkHours = value; RaisePropertyChanged(); }
        }

        public int XrayGenTotalUsageCount
        {
            get { return _xrayGenTotalUsageCount; }
            set { _xrayGenTotalUsageCount = value; RaisePropertyChanged(); }
        }


        public double XRayGenWorkHoursSinceBoot
        {
            get { return _xRayGenWorkHoursSinceBoot; }
            set { _xRayGenWorkHoursSinceBoot = value; RaisePropertyChanged(); }
        }

        public int XRayGenUsageCountSinceBoot
        {
            get { return _xrayGenUsageCountSinceBoot; }
            set { _xrayGenUsageCountSinceBoot = value; RaisePropertyChanged(); }
        }

        public double TotalXRayGen1WorkHours
        {
            get { return _totalXRayGen1WorkHours; }
            set { _totalXRayGen1WorkHours = value; RaisePropertyChanged(); }
        }

        public int XrayGen1TotalUsageCount
        {
            get { return _xrayGen1TotalUsageCount; }
            set { _xrayGen1TotalUsageCount = value; RaisePropertyChanged(); }
        }

        public double TotalXRayGen2WorkHours
        {
            get { return _totalXRayGen2WorkHours; }
            set { _totalXRayGen2WorkHours = value; RaisePropertyChanged(); }
        }

        public int XrayGen2TotalUsageCount
        {
            get { return _xrayGen2TotalUsageCount; }
            set { _xrayGen2TotalUsageCount = value; RaisePropertyChanged(); }
        }

        //// 故障
        private bool _sparking1;

        public bool Sparking1
        {
            get { return _sparking1; }
            set { _sparking1 = value; RaisePropertyChanged(); }
        }

        private bool _overTemperature1;

        public bool OverTemperature1
        {
            get { return _overTemperature1; }
            set { _overTemperature1 = value; RaisePropertyChanged(); }
        }

        private bool _overVoltage1;

        public bool OverVoltage1
        {
            get { return _overVoltage1; }
            set { _overVoltage1 = value; RaisePropertyChanged(); }
        }

        private bool _overCurrent1;

        public bool OverCurrent1
        {
            get { return _overCurrent1; }
            set { _overCurrent1 = value; RaisePropertyChanged(); }
        }

        private bool _overloadPower1;

        public bool OverloadPower1
        {
            get { return _overloadPower1; }
            set { _overloadPower1 = value; RaisePropertyChanged(); }
        }

        private bool _vAOutofControl1;

        public bool VAOutofControl1
        {
            get { return _vAOutofControl1; }
            set { _vAOutofControl1 = value; RaisePropertyChanged(); }
        }

        private bool _interlockSwitchNotClosed1;

        public bool InterlockSwitchNotClosed1
        {
            get { return _interlockSwitchNotClosed1; }
            set { _interlockSwitchNotClosed1 = value; RaisePropertyChanged(); }
        }

        private bool _sparking2;

        public bool Sparking2
        {
            get { return _sparking2; }
            set { _sparking2 = value; RaisePropertyChanged(); }
        }

        private bool _overTemperature2;

        public bool OverTemperature2
        {
            get { return _overTemperature2; }
            set { _overTemperature2 = value; RaisePropertyChanged(); }
        }

        private bool _overVoltage2;

        public bool OverVoltage2
        {
            get { return _overVoltage2; }
            set { _overVoltage2 = value; RaisePropertyChanged(); }
        }

        private bool _overCurrent2;

        public bool OverCurrent2
        {
            get { return _overCurrent2; }
            set { _overCurrent2 = value; RaisePropertyChanged(); }
        }

        private bool _overloadPower2;

        public bool OverloadPower2
        {
            get { return _overloadPower2; }
            set { _overloadPower2 = value; RaisePropertyChanged(); }
        }

        private bool _vAOutofControl2;

        public bool VAOutofControl2
        {
            get { return _vAOutofControl2; }
            set { _vAOutofControl2 = value; RaisePropertyChanged(); }
        }

        private bool _interlockSwitchNotClosed2;

        public bool InterlockSwitchNotClosed2
        {
            get { return _interlockSwitchNotClosed2; }
            set { _interlockSwitchNotClosed2 = value; RaisePropertyChanged(); }
        }

        private bool _isSettingEnable;

        public bool IsSettingEnable
        {
            get { return _isSettingEnable; }
            set { _isSettingEnable = value; RaisePropertyChanged(); }
        }


        #endregion 绑定属性

        public RelayCommand SetNewSettingCommand { get; set; }
        public RelayCommand SetNewSettingCommand2 { get; set; }
        public RelayCommand EmitXRayCommand { get; private set; }

        public RelayCommand CloseXRayCommand { get; private set; }

        private DispatcherTimer _dispatcherTimer;

        public XRayGenUsageRecord LastRecord { get; set; }


        public XRayGenPageViewModel()
        {
            SetNewSettingCommand = new RelayCommand(SetNewSettingCommandExecute);
            SetNewSettingCommand2 = new RelayCommand(SetNewSettingCommandExecute2);
            EmitXRayCommand = new RelayCommand(EmitXRayCommandExecute);
            CloseXRayCommand = new RelayCommand(CloseXRayCommandExecute);
            ResetButtonPressCommand = new RelayCommand<string>(ResetButtonPressCommandExe);

            if (IsInDesignMode)
            {
                Type = XRayGeneratorType.XRayGen_Spellman80;
                KvSetting = 160;
                KvWorking = 150;
                MaSetting = 0.5;
                MaWorking = 0;
                KvSetting2 = 160;
                KvWorking2 = 150;
                MaSetting2 = 0.5;
                MaWorking2 = 0;
            }
            else
            {
                try
                {
                    int xrayCount = 1;
                    if (!ScannerConfig.Read(ConfigPath.XRayGenCount, out xrayCount))
                    {
                        xrayCount = 1;
                    }

                    if (xrayCount > 1)
                    {
                        Gen2Visibility = Visibility.Visible;
                    }
                    InitKVMA();
                    
                }
                catch (Exception exception)
                {
                    Tracer.TraceException(exception);
                }


                _dispatcherTimer = new DispatcherTimer();
                _dispatcherTimer.Interval = TimeSpan.FromSeconds(1);
                _dispatcherTimer.Tick += DispatcherTimerOnTick;
                _dispatcherTimer.Start();

                _settingController = new XRayGenSettingController();
                GetSettingsAsync(3);
                InitialWorkHours();
                UpdateInitEvent.updateInit += InitialWorkHours;
            }
        }

        /// <summary>
        /// 定时检测X射线发生器的状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private async void DispatcherTimerOnTick(object sender, EventArgs eventArgs)
        {
            XRayGeneratorWorkingStates state;
            if (ControlService.ServicePart.GetXRayGenWorkingStates(out state))
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Gen1OilTemp = state.XRayGen1OilTemperature;
                    Gen2OilTemp = state.XRayGen2OilTemperature;

                    Gen1ErrorCode = state.XRayGen1ErrorCode;
                    Gen2ErrorCode = state.XRayGen2ErrorCode;

                    SetGen1ErrorCode(Gen1ErrorCode);
                    SetGen2ErrorCode(Gen2ErrorCode);

                    if (Gen1ErrorCode == 0 && Gen2ErrorCode == 0)
                        GenErrorVisibility = Visibility.Collapsed;
                    else
                        GenErrorVisibility = Visibility.Visible;

                    GetWorkingValuesAsync();
                });
            }
        }

        private void EmitXRayCommandExecute()
        {
            _settingController.EmitXRay(true);
            new OperationRecordService().RecordOperation(OperationUI.XRayGen, "XRay", OperationCommand.Open, string.Empty);
        }

        private void CloseXRayCommandExecute()
        {
            _settingController.EmitXRay(false);
            new OperationRecordService().RecordOperation(OperationUI.XRayGen, "XRay", OperationCommand.Close, string.Empty);
        }

        /// <summary>
        /// 保存新的高压束流值
        /// </summary>
        private void SetNewSettingCommandExecute()
        {
            if(MaNew<=0.2)//yxc 控制电流值不要过流或者欠流
            {
                MaNew = 0.21;
            }
            //if (MaNew>= 1.25)//yxc 控制电流值不要过流或者欠流
            //{
            //    MaNew = 1.24;
            //}
            if (_settingController.ChangeXRayGenSettings(KvNew, MaNew, XRayGeneratorIndex.XRayGenerator1,true))
            {
                // 设置成功后，更新所有值
                GetSettingsAsync(1);
                new OperationRecordService().RecordOperation(OperationUI.XRayGen, "XrayGen1", OperationCommand.Setting, String.Format("{0}Kv,{1}mA", KvNew, MaNew));
            }
            else
            {
                // failed
                var msg = new ShowDialogMessageAction("SettingWindow", TranslationService.FindTranslation("Error"),
                            TranslationService.FindTranslation("Could not change X-Ray generator settings, please check the Control System connection state!"), MetroDialogButtons.Ok, dialogResult => { });
                // TODO: 民航认证，求个稳，先不弹窗了，认证后视情况改回来
                //this.MessengerInstance.Send(msg);
            }

        }
        private void SetNewSettingCommandExecute2()
        {
            if (MaNew2 <= 0.2)//yxc 控制电流值不要过流或者欠流
            {
                MaNew2 = 0.21;
            }
            //if (MaNew2 >= 1.25)//yxc 控制电流值不要过流或者欠流
            //{
            //    MaNew2 = 1.24;
            //}
            if (_settingController.ChangeXRayGenSettings(KvNew2, MaNew2, XRayGeneratorIndex.XRayGenerator2,true))
            {
                // 设置成功后，更新所有值
                GetSettingsAsync(2);

               new OperationRecordService().RecordOperation(OperationUI.XRayGen,"XrayGen2", OperationCommand.Setting, String.Format("{0}Kv,{1}mA", KvNew2, MaNew2));
            }
            else
            {
                // failed
                var msg = new ShowDialogMessageAction("SettingWindow", TranslationService.FindTranslation("Error"),
                            TranslationService.FindTranslation("Could not change X-Ray generator settings, please check the Control System connection state!"), MetroDialogButtons.Ok, dialogResult => { });
                // TODO: 民航认证，求个稳，先不弹窗了，认证后视情况改回来
                //this.MessengerInstance.Send(msg);
            }

        }
        /// <summary>
        /// 异步获取最新的配置和实时值
        /// </summary>
        private async void GetSettingsAsync(int index)
        {
            var setting = await _settingController.GetXRayGenSettingsAsync();
            
            if ((index & 0x01) == 1)  
            {
               
                if (setting != null)
                {
                    
                    Type = setting.VendorType;
                   
                    if (Type == XRayGeneratorType.XRayGen_KWA)
                    {
                        
                        IsSettingEnable = false;
                    }
                    else
                    {
                        
                        IsSettingEnable = true;
                        KvSetting = KvNew;
                        MaSetting = MaNew;
                    }
                }
                else
                {
                  
                    Type = null;
                    //KvSetting = null;
                    //MaSetting = null;

                    //KvNew = 150;
                    //MaNew = 0.5;
                }
                ScannerConfig.Write(ConfigPath.XRayGenKV, _kvSetting);
                ScannerConfig.Write(ConfigPath.XRayGenMA, _maSetting);
            }
            if ((index & 0x02) == 2)
            {
               
                if (setting != null)
                {
                   
                    Type = setting.VendorType;
                    if (Type == XRayGeneratorType.XRayGen_KWA)
                    {
                       
                        IsSettingEnable = false;
                    }
                    else
                    {
                       
                        IsSettingEnable = true;
                        //KvSetting = setting.KV;
                        //MaSetting = setting.MA;
                        KvSetting2 = KvNew2;
                        MaSetting2 = MaNew2;
                    }
                    
                }
                else
                {
                    
                    Type = null;
                    //KvSetting = null;
                    //MaSetting = null;
                    //KvNew2 = 150;
                    //MaNew2 = 0.5;
                }
                ScannerConfig.Write(ConfigPath.XRayGenKV2, _kvSetting2);
                ScannerConfig.Write(ConfigPath.XRayGenMA2, _maSetting2);
            }


            var states = await _settingController.GetXRayGenWorkingStatesAsync();
            if ((index & 0x01) == 1)   
            {
                
                if (states != null)
                {
                    
                    KvWorking = states.XRayGen1kV;
                    MaWorking = states.XRayGen1mA;
               
                }
                else
                {
                    
                    KvWorking = null;
                    MaWorking = null;
                }



                if (Type == XRayGeneratorType.XRayGen_KWA)
                {
                    
                    KvWorking = _kvSetting;
                    MaWorking = _maSetting;
                }
            }
            if ((index & 0x02) == 2)
            {
                
                if (states != null)
                {
                    
                    KvWorking2 = states.XRayGen2kV;
                    MaWorking2 = states.XRayGen2mA;
                }
                else
                {
                   
                    KvWorking2 = null;
                    MaWorking2 = null;
                }



                if (Type == XRayGeneratorType.XRayGen_KWA)
                {
                    
                    KvWorking2 = _kvSetting2;
                    MaWorking2 = _maSetting2;
                }
            }
        }

        /// <summary>
        /// 异步获取实时值
        /// </summary>
        private async void GetWorkingValuesAsync()
        {
            var states = await _settingController.GetXRayGenWorkingStatesAsync();
            if (states != null)
            {
                KvWorking = states.XRayGen1kV;
                MaWorking = states.XRayGen1mA;
                KvWorking2 = states.XRayGen2kV;
                MaWorking2 = states.XRayGen2mA;
            }
            else
            {
                KvWorking = null;
                MaWorking = null;
                KvWorking2 = null;
                MaWorking2 = null;
            }
        }
        void InitKVMA()
        {
            if (!ScannerConfig.Read(ConfigPath.XRayGenKV, out _kvSetting))
            {
                _kvSetting = 160.0;
            }
            _kvNew = _kvSetting;
            if (!ScannerConfig.Read(ConfigPath.XRayGenMA, out _maSetting))
            {
                _maSetting = 0.6;
            }
            _maNew = _maSetting;
            if (!ScannerConfig.Read(ConfigPath.XRayGenKV2, out _kvSetting2))
            {
                _kvSetting2 = 160.0;
            }
            _kvNew2 = _kvSetting2;
            if (!ScannerConfig.Read(ConfigPath.XRayGenMA2, out _maSetting2))
            {
                _maSetting2 = 0.6;
            }
            _maNew2 = _maSetting2;
        }

        private async void InitialWorkHours()
        {
            var db = new XRayGenUsageRecordDbSet();
            LastRecord = db.TakeLatest();

            //设备本次启动前的开机时间
            var totalBootHoursBeforeCurrentBootTask = WorkHoursController.Instance.GetTotalBootHoursBeforeCurrentBoot();
            //查询x光机总的使用时间
            var xrayGenTotalHoursTask = WorkHoursController.Instance.GetTotalXRayGenWorkHours();
            //查询光机当前使用记录
            var xrayGenCurrentWorkHoursTask = WorkHoursController.Instance.GetXRayGenWorkHoursSinceBoot();


            double workHourSinceMachineBoot = WorkHoursController.Instance.WorkHoursOfMachineSinceBoot;
            double xrayGenCurrentWorkHours = await xrayGenCurrentWorkHoursTask;
            double totalBootHoursBeforeCurrentBoot = await totalBootHoursBeforeCurrentBootTask;
            double xrayGenTotalHours = await xrayGenTotalHoursTask;


            XRayGenWorkHoursSinceBoot = xrayGenCurrentWorkHours + DevicePartsWorkTimingService.ServicePart.GetXrayWorkingTimeSinceBoot();//.ToString(CultureInfo.CurrentCulture);
            TotalXRayGenWorkHours = xrayGenTotalHours + DevicePartsWorkTimingService.ServicePart.GetXrayWorkingTimeSinceBoot();//.ToString(CultureInfo.CurrentCulture);
            XRayGenUsageCountSinceBoot = DevicePartsWorkTimingService.ServicePart.XrayWorkingCountSinceBoot;
            XrayGenTotalUsageCount = DevicePartsWorkTimingService.ServicePart.XrayTotalWorkingCount;

            TotalXRayGen1WorkHours = Math.Max(TotalXRayGenWorkHours + LastRecord.Gen1UsageTimeOffset, 0);
            TotalXRayGen2WorkHours = Math.Max(TotalXRayGenWorkHours + LastRecord.Gen2UsageTimeOffset, 0);

            XrayGen1TotalUsageCount = Math.Max(XrayGenTotalUsageCount + LastRecord.Gen1UsageCountOffset, 0);
            XrayGen2TotalUsageCount = Math.Max(XrayGenTotalUsageCount + LastRecord.Gen2UsageCountOffset, 0);


            //Tracer.TraceInfo(string.Format("gen1time:{0},gen1count:{1},gen2time{2},gen2count:{3}",
            //    LastRecord.Gen1UsageTimeOffset, LastRecord.Gen1UsageCountOffset, LastRecord.Gen2UsageTimeOffset, LastRecord.Gen2UsageCountOffset));
        }

        private void ResetButtonPressCommandExe(string para)
        {
            if (para == "Xray1WorkHours")
            {
                LastRecord.Gen1UsageTimeOffset = 0 - TotalXRayGenWorkHours;
            }
            else if (para == "Xray1UsageCount")
            {
                LastRecord.Gen1UsageCountOffset = 0 - XrayGenTotalUsageCount;
            }
            else if (para == "Xray2WorkHours")
            {
                LastRecord.Gen2UsageTimeOffset = 0 - TotalXRayGenWorkHours;
            }
            else if (para == "Xray2UsageCount")
            {
                LastRecord.Gen2UsageCountOffset = 0 - XrayGenTotalUsageCount;
            }
            LastRecord.AccountId = LoginAccountManager.Service.CurrentAccount.AccountId;
            LastRecord.ChangeTime = DateTime.Now;
            var db = new XRayGenUsageRecordDbSet();
            db.Update(LastRecord);

            InitialWorkHours();
        }

        private void SetGen1ErrorCode(int error)
        {
            Sparking1 = Convert.ToBoolean(error & 0x01);
            OverTemperature1 = Convert.ToBoolean(error & 0x02);
            OverVoltage1 = Convert.ToBoolean(error & 0x04);
            OverCurrent1 = Convert.ToBoolean(error & 0x08);
            OverloadPower1 = Convert.ToBoolean(error & 0x10);
            VAOutofControl1 = Convert.ToBoolean(error & 0x20);
            InterlockSwitchNotClosed1 = Convert.ToBoolean(error & 0x40);
        }
        private void SetGen2ErrorCode(int error)
        {
            Sparking2 = Convert.ToBoolean(error & 0x01);
            OverTemperature2 = Convert.ToBoolean(error & 0x02);
            OverVoltage2 = Convert.ToBoolean(error & 0x04);
            OverCurrent2 = Convert.ToBoolean(error & 0x08);
            OverloadPower2 = Convert.ToBoolean(error & 0x10);
            VAOutofControl2 = Convert.ToBoolean(error & 0x20);
            InterlockSwitchNotClosed2 = Convert.ToBoolean(error & 0x40);
        }

        public override void Cleanup()
        {
            base.Cleanup();

            _dispatcherTimer.Tick -= DispatcherTimerOnTick;
            _dispatcherTimer.Stop();

            try
            {
                _settingController.EmitXRay(false);
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        public override void OnKeyDown(System.Windows.Input.KeyEventArgs args)
        {

        }
    }
}
