using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UI.XRay.Business.Entities
{
    /// <summary>
    /// X-Ray Scanner Configuration Item paths.
    /// </summary>
    public static class ConfigPath
    {
        #region 系统级的配置 System

        public const string SystemDecryptStr = "System/DecryptStr";
        public const string WorkReminderTime = "System/ReminderTime";//工作时长提醒 单位-分钟
        public const string IsWorkIntervalReminder = "System/IsWorkIntervalReminder";//工作时长提醒 单位-分钟
        public const string IsLeaveHarborEnable = "System/IsLeaveHarborEnable"; // 是否开启离港信息显示
        /// <summary>
        /// 字符串：可取值包括：English, ChineseS, ChineseT, Russian
        /// </summary>
        public const string SystemLanguage = "System/Language";
        public const string SystemTouchUI = "System/TouchUI";

        /// <summary>是否允许手动检查</summary>
        public const string SystemIsAllowManualInspection = "System/IsAllowManualInspection";

        /// <summary>
        /// 是否开启Shift功能键
        /// </summary>
        public const string SystemEnableShiftKey = "System/IsEnableShiftKey";
        /// <summary>
        /// 系统日期格式定义
        /// </summary>
        public const string SystemDateFormat = "System/DateFormat";

        public const string SystemImageStorePath = "System/ImageStorePath";

        public const string SystemCompanyName = "System/CompanyName";
        public const string SystemMachineNum = "System/MachineNum";
        public const string SystemMachineDate = "System/ProductionDate";
        public const string SystemDescription = "System/Description";

        /// <summary>
        /// 系统自生产后，扫描物体总数
        /// </summary>
        public const string SystemTotalBagCount = "System/TotalBagCount";

        /// <summary>
        /// 系统定期提醒检查硬件工作是否正常
        /// </summary>
        public const string IsLoopRemind = "System/IsLoopRemind";
        public const string LastMaintenanceTime = "System/LastMaintenanceTime";
        public const string SystemRemindTimeInterval = "System/RemindTimeInterval";

        /// <summary>
        /// 设备型号：字符串
        /// </summary>
        public const string SystemModel = "System/Model";

        /// <summary>
        /// 软件版本
        /// </summary>
        public const string SystemSoftwareVersion = "System/Version/Software";
        /// <summary>
        /// 算法版本
        /// </summary>
        public const string SystemAlgorithmVersion = "System/Version/Algorithm";

        /// <summary>
        /// 保存通用格式（universal picture format）图像时是否自动保存Xray图像
        /// </summary>
        public const string AutoStoreUpfImage = "System/AutoStoreUPFImage/AutoStoreUpfImage";
        public const string AutoStoreXrayImage = "System/AutoStoreUPFImage/AutoStoreXrayImage";
        public const string AutoStoreUpfImagePath = "System/AutoStoreUPFImage/AutoStoreUpfImagePath";
        public const string AutoStoreUpfImageStrategy = "System/AutoStoreUPFImage/AutoStoreUpfImageStrategy";
        public const string AutoStoreUpfImageFormat = "System/AutoStoreUPFImage/AutoStoreUpfImageFormat";
        public const string LimitAutoStoreUpfImageCount = "System/AutoStoreUPFImage/LimitAutoStoreUpfImageCount";
        public const string AutoStoreUpfImageCount = "System/AutoStoreUPFImage/AutoStoreUpfImageCount";

        public const string SystemHttpServiceIp = "System/Http/ServiceIp";
        public const string SystemHttpLocalIp = "System/Http/LocalIp";
        public const string SystemHttpEnable = "System/Http/Enable";

        /// <summary>
        /// 是否启用模拟数据，
        /// </summary>
        public const string EnableTestData = "System/EnableTestData";

        /// <summary>
        /// 是否拼接两个视角的图像，todo：注意只在双视角设备中暴露选项
        /// </summary>
        public const string MergeTwoViewImage = "System/AutoStoreUPFImage/MergeTwoViewImage";

        /// <summary>
        /// 清空通道运行时间
        /// </summary>
        public const string CleanTunnelContinueTime = "System/CleanTunnelContinueTime";
        /// <summary>
        /// 清空通道运行方向，默认回退
        /// </summary>
        public const string CleanTunnelMoveBackward = "System/CleanTunnelMoveBackward";

        #endregion 系统级的配置

        #region 设备物理参数配置 Machine

        /// <summary>
        /// M/S，浮点型
        /// </summary>
        public const string MachineConveyorSpeed = "Machine/ConveyorSpeed";
        public const string SanitizeConveySpeed = "Machine/SanitizeConveySpeed";
        public const string MachineCanChangeConveyorSpeed = "Machine/CanChangeConveyorSpeed";//是否需要设置速度变频器
        public const string MultiFfrequency = "Machine/MultiFfrequency";//是否需要设置速度变频器
        public const string MachineConveyorStartTime = "Machine/ConveyorStartTime";//输送带启动时间
        public const string MachineConveyorStopTime = "Machine/ConveyorStopTime";//输送带停止时间
        public const string SanitizeConveyStartTime = "Machine/SanitizeConveyStartTime";//消杀输送带启动时间
        public const string SanitizeConveyStopTime = "Machine/SanitizeConveyStopTime";//消杀输送带停止时间
        public const string MachineConveyorSpeedChangeRatio = "Machine/ConveyorSpeeedChangeRatio";//速度(m/s)到变频器频率转换系数

        public const string MachineConveyorMaxSpeed = "Machine/ConveyorMaxSpeed";
        public const string MachineConveyorMinSpeed = "Machine/ConveyorMinSpeed";

        public const string MachineConveyorDefaultDirection = "Machine/ConveyorDefaultDirection";

        /// <summary>
        /// 整个输送机的长度：单位为米，浮点型
        /// </summary>
        public const string MachineConveyorLength = "Machine/ConveyorLength";

        /// <summary>
        /// 扫描时停止输送机，再次启动时是否自动倒带
        /// </summary>
        public const string MachineAutoRewind = "Machine/InterruptMode/AutoRewind";
        public const string MachineInterruptModeStartCalCols = "Machine/InterruptMode/StartCalCols";
        public const string MachineInterruptModeRegisterDataCacheCount = "Machine/InterruptMode/RegisterDataCacheCount";
        public const string MachineInterruptModeBackgroundValue = "Machine/InterruptMode/BackgroundValue";
        public const string MachineInterruptModeRollbackDistance = "Machine/InterruptMode/RollbackDistance";
        public const string MachineInterruptModeMatchLineCount = "Machine/InterruptMode/MatchLineCount";
        /// <summary>
        /// 是否启动双向扫描
        /// </summary>
        public const string MachineBiDirectionScan = "Machine/BiDirectionScan";
        /// <summary>
        /// 是否启动连续扫描
        /// </summary>
        public const string MachineContinuousScan = "Machine/ContinuousScan";
        /// <summary>
        /// 采集系统视角个数,1 or 2
        /// </summary>
        public const string MachineViewsCount = "Machine/ViewsCount";

        /// <summary>
        /// 两个采集视角之间的距离 ，米
        /// </summary>
        public const string MachineViewsDistanceForForward = "Machine/ViewsDistance";

        public const string MachineViewsDistanceForBackward = "Machine/ViewsDistanceForBackward";

        /// <summary>
        /// 入口光障到最近射线源的距离，单位为米，浮点型
        /// </summary>
        public const string MachineEntryPESensorToXRayGenDistance = "Machine/EntryPESensorToXRayGenDistance";

        /// <summary>
        /// 出口光障到最近射线源的距离，单位为米，浮点型
        /// </summary>
        public const string MachineExitPESensorToXRayGenDistance = "Machine/ExitPESensorToXRayGenDistance";

        /// <summary>
        /// 射线提前发射的位置，单位 米
        /// </summary>
        public const string MachineXRayOnPosBeforeXRayGen = "Machine/XRayOnPosBeforeXRayGen";

        /// <summary>
        /// 射线相对于最后一个射线源延迟关闭的位置，单位 米
        /// </summary>
        public const string MachineXRayOffPosAfterXRayGen = "Machine/XRayOffPosAfterXRayGen";

        /// <summary>
        /// 视角1的皮带边缘的起始位置
        /// </summary>
        public const string MachineView1BeltEdgeAtBegin = "Machine/BeltEdge/View1/BeginPosition";

        public const string IsUseTipImage1 = "Machine/TipInject/IsUseTipImage1";

        /// <summary>
        /// 视角1的皮带边缘的结束位置（从最后一个点倒数算起）
        /// </summary>
        public const string MachineView1BeltEdgeAtEnd = "Machine/BeltEdge/View1/EndPosition";

        /// <summary>
        /// 视角2的皮带边缘的起始位置
        /// </summary>
        public const string MachineView2BeltEdgeAtBegin = "Machine/BeltEdge/View2/BeginPosition";

        /// <summary>
        /// 视角2的皮带边缘的结束位置（从最后一个点倒数算起）
        /// </summary>
        public const string MachineView2BeltEdgeAtEnd = "Machine/BeltEdge/View2/EndPosition";

        /// <summary>
        /// 上面所述的皮带边缘主要是去除探测器L形上下边缘处皮带或者射线未照射对于图像的影响，不能限制在探测器中央的皮带边的影响，此处皮带边
        /// </summary>
        public const string MachineView1BeltEdge1Start = "Machine/BeltEdge/View1/Edge1StartPosition";
        public const string MachineView1BeltEdge1End = "Machine/BeltEdge/View1/Edge1EndPosition";
        public const string MachineView1BeltEdge2Start = "Machine/BeltEdge/View1/Edge2StartPosition";
        public const string MachineView1BeltEdge2End = "Machine/BeltEdge/View1/Edge2EndPosition";
        public const string MachineView2BeltEdge1Start = "Machine/BeltEdge/View2/Edge1StartPosition";
        public const string MachineView2BeltEdge1End = "Machine/BeltEdge/View2/Edge1EndPosition";
        public const string MachineView2BeltEdge2Start = "Machine/BeltEdge/View2/Edge2StartPosition";
        public const string MachineView2BeltEdge2End = "Machine/BeltEdge/View2/Edge2EndPosition";


        /// <summary>
        /// 皮带边上下各取的像素数
        /// </summary>
        public const string MachineBeltEdgeRoundPixelCount = "Machine/BeltEdge/RoundPixelCount";
        /// <summary>
        /// 皮带边上下各一段区域取均值，皮带边区域上下都是空白，皮带边区域可置白
        /// </summary>
        public const string MachineBeltEdgeAirThreshold = "Machine/BeltEdge/AirThreshold";

        /// <summary>
        /// 右行机设计可能存在多个硬件组件反转的情况，保持参数设置不变情况下通过标志位来控制
        /// 默认（左行机）情况下，入口位置为1，出口为2
        /// </summary>
        public const string MachineDirectionExchangePESensor = "Machine/Direction/ExchangePESensor";
        public const string MachineDirectionExchangeConveyor = "Machine/Direction/ExchangeConveyor";
        public const string MachineDirectionExchangeXrayGen = "Machine/Direction/ExchangeXrayGen";
        public const string MachineDirectionExchangeDetector = "Machine/Direction/ExchangeDetector";

        public const string MachineView1DislocationOffsets = "Machine/View1DislocationOffsets";
        public const string MachineView2DislocationOffsets = "Machine/View2DislocationOffsets";
        #endregion 设备物理参数配置

        #region 预处理参数配置 PreProc

        /// <summary>
        /// IsNormalizationEnabled：True or False
        /// </summary>
        public const string PreProcIsNormalizationEnabled = "PreProc/Correction/IsNormalizationEnabled";

        public const string PreProcGroundHighAvgUpper = "PreProc/Correction/GroundHighAvgUpper";
        public const string PreProcGroundHighSingleUpper = "PreProc/Correction/GroundHighSingleUpper";
        public const string PreProcGroundLowAvgUpper = "PreProc/Correction/GroundLowAvgUpper";
        public const string PreProcGroundLowSingleUpper = "PreProc/Correction/GroundLowSingleUpper";
        public const string PreProcGroundUpdateRateUpper = "PreProc/Correction/GroundUpdateRateUpper";
        public const string PreProcGroundUpdateAfterXRayOff = "PreProc/Correction/GroundUpdateAfterXRayOff";
        public const string PreProcGroundUpdatePreXRayOn = "PreProc/Correction/GroundUpdatePreXRayOn";

        public const string PreProcAirHighAvgLower = "PreProc/Correction/AirHighAvgLower";
        public const string PreProcAirHighSingleLower = "PreProc/Correction/AirHighSingleLower";
        public const string PreProcAirLowAvgLower = "PreProc/Correction/AirLowAvgLower";
        public const string PreProcAirLowSingleLower = "PreProc/Correction/AirLowSingleLower";
        public const string PreProcAirUpdateRateUpper = "PreProc/Correction/AirUpdateRateUpper";

        public const string PreProcAirUpdateMode = "PreProc/Correction/AirUpdateMode";
        public const string PreProcAirPixelThreshold = "PreProc/Correction/AirPixelThreshold";
        public const string PreProcAvgFilterWindowSize = "PreProc/Correction/AvgFilterWindowSize";
        public const string PreProcAvgChangeRate = "PreProc/Correction/AvgChangeRate";
        public const string PreProcDynamicUpdateLineCount = "PreProc/Correction/DynamicUpdateLineCount";
        public const string PreProcOnlyAllAirLine = "PreProc/Correction/OnlyAllAirLine";
        public const string PreProcForceUpdateRefrenceAirTime = "PreProc/Correction/ForceUpdateRefrenceAirTime";
        public const string PreProcForceAirUpdateRateUpper = "PreProc/Correction/ForceAirUpdateRateUpper";

        public const string PreProcAutoDetectBadChannels = "PreProc/Correction/AutoDetectBadChannels";
        public const string PreProcOpenNewPenetrationAlgorithm = "PreProc/Correction/OpenNewPenetrationAlgorithm";
        //是否总是使用最新的亮场数据，默认是false(变化率在范围内才可用)
        public const string PreProcAlwaysUseLastAirUpdate = "PreProc/Correction/AlwaysUseNewAirUpdate";

        public const string PreProcUnpenetratableUpper = "PreProc/Class/UnpenetratableThreshold";
        public const string PreProcBkgThreshold = "PreProc/Class/BkgThreshold";
        public const string PreProcBgbThreshold = "PreProc/Class/BgbThreshold";

        public const string PreProcDarkFieldAverage = "PreProc/Class/DarkFieldAverage";

        public const string PreProcFilterEnable = "PreProc/Filter/Enable";
        public const string PreProcBlankPoint = "PreProc/Filter/BlankPoint";
        public const string PreProcFilterKernelSize = "PreProc/Filter/KernelSize";
        public const string PreProcFilterSmoothFunc = "PreProc/Filter/SmoothFunc";
        public const string PreProcFilterLinesCount = "PreProc/Filter/LinesCount";

        public const string PreProcBagEdgeReserved = "PreProc/Filter/BagEdgeReserved";

        public const string PreProcTestFilterSize = "PreProc/Filter/TestFilterSize";
        public const string PreProcTestPixelCountThr = "PreProc/Filter/TestPixelCountThr";

        public const string PreProcFindBorder = "PreProc/Filter/FindBorder";
        public const string PreProcAirRegionsMinDistance = "PreProc/Filter/AirRegionsMinDistance";
        public const string PreProcFilterDirtyBKG = "PreProc/Filter/DirtyBKG";

        public const string PreProcWienerLinesCount = "PreProc/WienerLinesCount";
        public const string PreProcImageProcessLinesCount = "PreProc/ImageProcessLinesCount";
        public const string PreProcImageProcessCacheCount = "PreProc/ImageProcessCacheCount";

        public const string PreProcHistogramBegin = "PreProc/Histogram/HistogramBegin";
        public const string PreProcHistogramEnd = "PreProc/Histogram/HistogramEnd";
        public const string PreProcHistogramStretchBegin = "PreProc/Histogram/StretchBegin";
        public const string PreProcHistogramStretchEnd = "PreProc/Histogram/StretchEnd";
        public const string PreProcHistogramShowThreshold = "PreProc/Histogram/ShowThreshold";
        public const string PreProcHistogramEnableFilter = "PreProc/Histogram/EnableFilter";
        public const string PreProcHistogramWindowSize = "PreProc/Histogram/WindowSize";
        public const string PreProcHistogramNewEnable = "PreProc/Histogram/NewEnable";
        public const string PreProcHistogramSifenbian = "PreProc/Histogram/Sifenbian";
        public const string PreProcHistogramCalHistogramAlpha = "PreProc/Histogram/CalHistogramAlpha";
        public const string PreProcHistogramTestMode = "PreProc/Histogram/TestMode";
        public const string PreProcHistogramView1High = "PreProc/Histogram/View1High";
        public const string PreProcHistogramView1Low = "PreProc/Histogram/View1Low";
        public const string PreProcHistogramView2High = "PreProc/Histogram/View2High";
        public const string PreProcHistogramView2Low = "PreProc/Histogram/View2Low";
        public const string PreProcHistogramNum1 = "PreProc/Histogram/Num1";
        public const string PreProcHistogramNum2 = "PreProc/Histogram/Num2";
        public const string PreProcHistogramNum3 = "PreProc/Histogram/Num3";
        public const string PreProcHistogramNum4 = "PreProc/Histogram/Num4";
        public const string PreProcHistogramNum5 = "PreProc/Histogram/Num5";
        public const string PreProcHistogramNum6 = "PreProc/Histogram/Num6";
        public const string PreProcHistogramNum7 = "PreProc/Histogram/Num7";
        public const string PreProcHistogramNum8 = "PreProc/Histogram/Num8";
        public const string PreProcHistogramBool1 = "PreProc/Histogram/Bool1";
        public const string PreProcHistogramBool2 = "PreProc/Histogram/Bool2";

        public const string PreProcWienerEnabled = "PreProc/WienerEnabled";

        public const string PreProcHistogramDivideMode = "PreProc/Histogram/DivideMode";
        public const string PreProcHistogramUnpenetrateHeight = "PreProc/Histogram/UnpenHeight";
        public const string PreProcHistogramUnpenetrateWidth = "PreProc/Histogram/UnpenWidth";
        public const string PreProcHistogramUnpenetrateUnpenetrateLowerlimit = "PreProc/Histogram/Lower";
        public const string PreProcHistogramUnpenetrateUnpenetrateUpperlimit = "PreProc/Histogram/Upper";
        public const string PreProcHistogramFindEndThreshold = "PreProc/Histogram/FindEndThreshold";


        //显示缓存池子大小
        public const string PreProcShowCacheCount = "PreProc/ShowCache/CacheCount";
        //数据显示完成后 ms
        public const string PreProcShowCacheShowTimespan = "PreProc/ShowCache/ShowTimespan";
        //数据接收完成后 ms
        public const string PreProcShowCacheAppendTimespan = "PreProc/ShowCache/AppendTimespan";


        public const string PreProcDetectConstantLineIsEnable = "PreProc/DetectConstantLine/IsEnable";
        public const string PreProcDetectConstantLineView1PointIndex = "PreProc/DetectConstantLine/View1PointIndex";
        public const string PreProcDetectConstantLineView1AirThr = "PreProc/DetectConstantLine/View1AirThr";
        public const string PreProcDetectConstantLineView1Diff = "PreProc/DetectConstantLine/View1Diff";
        public const string PreProcDetectConstantLineView2PointIndex = "PreProc/DetectConstantLine/View2PointIndex";
        public const string PreProcDetectConstantLineView2AirThr = "PreProc/DetectConstantLine/View2AirThr";
        public const string PreProcDetectConstantLineView2Diff = "PreProc/DetectConstantLine/View2Diff";

        #endregion 预处理参数配置

        #region 算法参数配置 Algorithm

        /// <summary>
        /// IsNormalizationEnabled：True or False
        /// </summary>
        public const string AlgoEnableImageProcessRecommend = "Algorithm/ImageProcessAlgoRecommend/Enable";
        public const string AlgoUnshowInOrganicLimit = "Algorithm/ImageProcessAlgoRecommend/UnshowInOrganicLimit";
        public const string AlgoUnshowOrganicLimit = "Algorithm/ImageProcessAlgoRecommend/UnshowOrganicLimit";
        public const string AlgoImageValueUpperLimit = "Algorithm/ImageProcessAlgoRecommend/ImageValueUpperLimit";
        public const string AlgoImageValueLowerLimit = "Algorithm/ImageProcessAlgoRecommend/ImageValueLowerLimit";
        public const string AlgoOrganicAndMixtureZLimit = "Algorithm/ImageProcessAlgoRecommend/OrganicAndMixtureZLimit";
        public const string AlgoMixtureAndInOrganicZLimit = "Algorithm/ImageProcessAlgoRecommend/MixtureAndInOrganicZLimit";

        public const string AlgoEnhanceView1Circle4 = "Algorithm/CirclePenetration/View1Circle4Threshold";
        public const string AlgoEnhanceView1Circle5 = "Algorithm/CirclePenetration/View1Circle5Threshold";
        public const string AlgoEnhanceView2Circle4 = "Algorithm/CirclePenetration/View2Circle4Threshold";
        public const string AlgoEnhanceView2Circle5 = "Algorithm/CirclePenetration/View2Circle5Threshold";

        public const string gamma1 = "Algorithm/ImageProcessAlgoRecommend/gamma1";
        public const string gamma2 = "Algorithm/ImageProcessAlgoRecommend/gamma2";
        public const string AlgoBanFengOldEnable = "Algorithm/BanFeng/OldVersionEnable";
        #endregion 预处理参数配置

        #region 图像采集系统配置 CaptureSys

        /// <summary>
        /// 采集系统类型：DT、UI
        /// </summary>
        public const string CaptureSysType = "CaptureSys/Type";
        public const string CaptureSysBoardCount = "CaptureSys/BoardCount";

        /// <summary>
        /// 每块探测板的像素数，包括高能和低能
        /// </summary>
        //public const string CaptureSysDetBoardPixelsCount = "CaptureSys/DetBoardPixelsCount";

        /// <summary>
        /// 每个独立的采集系统的探测板的数量。如果包含两个视角，则两个视角的板数必须相同
        /// </summary>
        //public const string CaptureSysDetBoardsCountPerView = "CaptureSys/DetBoardsCountPerView";

        /// <summary>
        /// 是否为双能采集：True or False
        /// </summary>
        public const string CaptureSysIsDualEnergy = "CaptureSys/IsDualEnergy";

        public const string CaptureSysRemoteIP1 = "CaptureSys/RemoteIP1";
        public const string CaptureSysRemoteCmdPort1 = "CaptureSys/RemoteCmdPort1";
        public const string CaptureSysRemoteImagePort1 = "CaptureSys/RemoteImagePort1";

        public const string CaptureSysRemoteIP2 = "CaptureSys/RemoteIP2";
        public const string CaptureSysRemoteCmdPort2 = "CaptureSys/RemoteCmdPort2";
        public const string CaptureSysRemoteImagePort2 = "CaptureSys/RemoteImagePort2";

        public const string CaptureSysHostIP = "CaptureSys/HostIP";

        public const string CaptureSysPingTime = "CaptureSys/PingTime";

        public const string CaptureSysPingEnable = "CaptureSys/PingEnable";

        /// <summary>
        /// 单线采集的积分时间，单位为毫秒，浮点型，如3.5ms
        /// </summary>
        public const string CaptureSysLineIntegrationTime = "CaptureSys/LineIntegrationTime";

        public const string CaptureSysIntegrationMultiplySpeed = "CaptureSys/IntegrationMultiplySpeed";

        public const string CheckCaptureSysTimeSpan = "CaptureSys/CheckCaptureSysTimeSpan";

        public const string ImageMovingSpeedArrayLen = "CaptureSys/ImageMovingSpeedArrayLen";

        /// <summary>
        /// DT数据采集板的类型
        /// </summary>
        public const string CaptureSysDTDetSysType = "CaptureSys/DT/DTDetSysType";

        /// <summary>
        /// DT采集系统的视角1的探测卡排列：如 "4，2，0，0" 表示6块板子的分布
        /// </summary>
        public const string CaptureSysDTView1CardsDist = "CaptureSys/DT/View1CardsDist";

        /// <summary>
        /// DT采集系统的视角1的探测卡排列：如 "2，4，0，0" 表示6块板子的分布
        /// </summary>
        public const string CaptureSysDTView2CardsDist = "CaptureSys/DT/View2CardsDist";

        public const string CaptureSysDTChannels = "CaptureSys/DT/DtChannels";

        #endregion 图像采集系统配置

        #region 控制系统配置ControlSys

        /// <summary>
        /// 控制系统类型：目前仅支持UI
        /// </summary>
        //public const string ControlSysType = "CaptureSys/Type";

        public const string ControlSysBoardIp = "ControlSys/ControlBoardIP";

        public const string ControlSysBoardPort = "ControlSys/ControlBoardPort";

        public const string ControlSysBoardCmdInterval = "ControlSys/ControlBoardCmdInterval";

        /// <summary>
        /// 计算机与控制系统相连的网卡的IP
        /// </summary>
        public const string ComputerIp = "ControlSys/ComputerIP";

        /// <summary>
        /// 计算机与控制系统相连的网卡的端口号
        /// </summary>
        public const string ComputerPort = "ControlSys/ComputerPort";

        /// <summary>
        /// 控制板固件
        /// </summary>
        public const string ControlFirmWare = "ControlSys/FirmWare";

        /// <summary>
        /// 控制板协议
        /// </summary>
        public const string ControlProtocol = "ControlSys/Protocol";

        /// <summary>
        /// 默认工作模式：Regular、Continuous
        /// </summary>
        //public const string ControlSysDefaultWorkMode = "ControlSys/DefaultWorkMode";

        /// <summary>
        /// 是否启用节能光障
        /// </summary>
        public const string EnableConveyorTriggerSensor = "ControlSys/ConveyorTriggerSensor/EnableConveyorTriggerSensor";

        /// <summary>
        /// 入口节能光障的index，2或者4，默认为2
        /// </summary>
        public const string EntryConveyorTriggerSensorIndex =
            "ControlSys/ConveyorTriggerSensor/EntryConveyorTriggerSensorIndex";

        /// <summary>
        /// 物品触发入口节能光障后停止电机的延时时间，为了物品离开皮带
        /// </summary>
        public const string ConveyorTriggerSensorMotorStopDelayTimeSpan = "ControlSys/ConveyorTriggerSensor/MotorStopDelayTimeSpan";


        #endregion 控制系统配置

        #region 键盘配置 Keyboard

        public const string KeyboardIsConveyorKeyReversed = "Keyboard/IsConveyorKeyReversed";

        public const string KeyboardIsKeyboardReversed = "Keyboard/IsKeyboardReversed";

        public const string KeyboardBackwardKeydownCountThr = "Keyboard/BackwardKeydownCountThr";

        public const string KeyboardIsFuctionKeyActionEnable = "Keyboard/FunctionKeys/EnableActionKey";


        /// <summary>
        /// 快捷键1的定义
        /// </summary>
        public const string KeyboardF1Type = "Keyboard/FunctionKeys/F1/Type";
        public const string KeyboardF1ActionType = "Keyboard/FunctionKeys/F1/ActionType";

        /// <summary>
        /// 快捷键1的特效组合，值的方式为ColorMode, Penetration, true, false。以逗号分隔，顺序依次为：颜色枚举，穿透枚举，反色，超增。
        /// 如果某一项不设置，则为空
        /// </summary>
        public const string KeyboardF1Effects = "Keyboard/FunctionKeys/F1/EffectsComposition";


        public const string KeyboardF2Type = "Keyboard/FunctionKeys/F2/Type";
        public const string KeyboardF2ActionType = "Keyboard/FunctionKeys/F2/ActionType";
        public const string KeyboardF2Effects = "Keyboard/FunctionKeys/F2/EffectsComposition";

        public const string KeyboardF3Type = "Keyboard/FunctionKeys/F3/Type";
        public const string KeyboardF3ActionType = "Keyboard/FunctionKeys/F3/ActionType";
        public const string KeyboardF3Effects = "Keyboard/FunctionKeys/F3/EffectsComposition";

        /// <summary>
        /// UISecurity：串口转USB的安检键盘，在计算机中的虚拟化串口号
        /// </summary>
        public const string KeyboardComName = "Keyboard/ComName";

        /// <summary>
        /// 是否自动检测键盘
        /// </summary>
        public const string KeyboardAutoDetect = "Keyboard/AutoDetect";

        #endregion 键盘配置

        #region 射线源配置 XRayGen

        public const string PcControlXRayGen = "XRayGen/PCControlXRayGen";

        public const string XRayGen1ComName = "XRayGen/XRayGen1ComName";
        public const string XRayGen2ComName = "XRayGen/XRayGen2ComName";

        /// <summary>
        /// 射线源类型：
        /// XRayGen_KWA, XRayGen_KWD, XRayGen_Spellman160, XRayGen_Spellman80, XRayGen_VJ
        /// </summary>
        public const string XRayGenType = "XRayGen/Type";

        /// <summary>
        /// 等待射线源响应时间
        /// </summary>
        public const string XRayGenWaitTimeout = "XRayGen/Timeout";

        public const string XRayGenKV = "XRayGen/KV";

        public const string XRayGenMA = "XRayGen/MA";

        public const string XRayGenKV2 = "XRayGen/KV2";

        public const string XRayGenMA2 = "XRayGen/MA2";

        public const string XRayGenMARatio = "XRayGen/MARatio";
        public const string XRayGenMARatio2 = "XRayGen/MARatio2";

        public const string XRayGenKVRatio = "XRayGen/KVRatio";
        public const string XRayGenKVRatio2 = "XRayGen/KVRatio2";

        public const string CalibrateWaitingXRayTimeSpan = "XRayGen/CalibrateWaitingXRayTimeSpan";

        public const string WarmupKvKWD = "XRayGen/Warmup/KWDKv";
        public const string WarmupMaKWD = "XRayGen/Warmup/KWDMa";

        public const string WarmupKvSpellman160 = "XRayGen/Warmup/Spellman160Kv";
        public const string WarmupMaSpellman160 = "XRayGen/Warmup/Spellman160Ma";

        public const string WarmupKvVJ160 = "XRayGen/Warmup/VJ160Kv";
        public const string WarmupMaVJ160 = "XRayGen/Warmup/VJ160Ma";

        public const string WarmupKvVJ200 = "XRayGen/Warmup/VJ200Kv";
        public const string WarmupMaVJ200 = "XRayGen/Warmup/VJ200Ma";

        public const string WarmupKvSAXG = "XRayGen/Warmup/SAXGKv";
        public const string WarmupMaSAXG = "XRayGen/Warmup/SAXGMa";

        /// <summary>
        /// 射线上升沿时间，单位为秒，如0.6
        /// </summary>
        public const string XRayGenRisingTimespan = "XRayGen/RisingTimespan";

        /// <summary>
        /// 射线源个数，仅支持1或者2
        /// </summary>
        public const string XRayGenCount = "XRayGen/Count";

        /// <summary>
        /// 射线源上一次预热的时间，类型为DateTime
        /// </summary>
        public const string XRayGenLastWarmupTime = "XRayGen/LastWarmupTime";

        public const string XRayGenLastWarmupResult = "XRayGen/XRayGenLastWarmupResult";

        /// <summary>
        /// 是否强制预热：True或False
        /// </summary>
        public const string XRayGenAlwaysWarmup = "XRayGen/AlwaysWarmup";

        public const string XRayGenWorkCount = "XRayGen/WorkCount";

        public const string XRayGenShowState = "XRayGen/ShowState";

        #endregion 射线源配置 XRayGen

        #region 图像参数设置 Images


        public const string BagMaxScanLines = "Images/BagMaxScanLines";

        /// <summary>
        /// 同时显示的图像的数量
        /// </summary>
        public const string ImagesCount = "Images/Count";


        public const string ImageSESifenbianRegionLower = "Images/SESifenbianRegionLower";
        public const string ImageSESifenbianRegionUpper = "Images/SESifenbianRegionUpper";

        /// <summary>
        /// 是否显示包裹图像之间的空白区域
        /// </summary>
        public const string ImagesShowBlankSpace = "Images/ShowBlankSpace";

        /// <summary>
        /// 包裹最小长度
        /// </summary>
        public const string BagMinLinesCount = "Images/BagMinLinesCount";

        /// <summary>
        /// XRay中包含高低能
        /// </summary>
        public const string SaveHighLowInXRay = "Images/SaveHighLowInXRay";

        /// <summary>
        /// 图像的默认吸收率 -25 到 25
        /// </summary>
        public const string ImagesDefaultAbsorbIndex = "Images/DefaultAbsorbIndex";

        /// <summary>
        /// 导出加亮图像时使用的吸收率
        /// </summary>
        public const string ImagesLightenAbsorbIndex = "Images/LightenAbsorbIndex";

        /// <summary>
        /// 导出图像效果
        /// </summary>
        public const string ImagesExportImage5Effect = "Images/ExportImage5Effect";

        /// <summary>
        /// 导出图像效果
        /// </summary>
        public const string ImagesExportImageFormat = "Images/ExportImageFormat";

        /// <summary>
        /// 保存图像时时间延迟
        /// </summary>
        public const string ImagesExportTimeDelay = "Images/ExportTimeDelay";

        /// <summary>
        /// 清除保存.xray图像队列的队列大小
        /// </summary>
        public const string ClearSaveXRayQueueCount = "Images/ClearSaveXRayQueueCount";

        /// <summary>
        /// 无法穿透区域以红色显示：True or False
        /// </summary>
        public const string ImagesShowUnpenetratableRed = "Images/ShowUnpenetratableRed";

        /// <summary>
        /// 最大放大倍数
        /// </summary>
        public const string ImagesMaxZoominTimes = "Images/MaxZoominTimes";

        /// <summary>
        /// 单词放大步长，如2.0表示每次放大一倍
        /// </summary>
        public const string ImagesZoominStep = "Images/ZoominStep";

        /// <summary>
        /// 图像额外边缘
        /// </summary>
        public const string ImageMargin = "Images/Margin";

        /// <summary>
        /// 电机启动时还原图像缩放
        /// </summary>
        public const string ImagesZoom1XWhenStart = "Images/Zoom1XWhenStart";

        /// <summary>
        /// 当分包时，图像聚焦到最新图像
        /// </summary>
        public const string ImageAnchorNewWhenEnd = "Images/AnchorNewWhenEnd";

        /// <summary>
        /// 默认true，图像放大情况下按左右按键是移动窗口，false则回拉
        /// </summary>
        public const string ImageCanMoveWhenZoomIn = "Images/CanMoveWhenZoomIn";

        /// <summary>
        /// 通用格式图像质量
        /// </summary>
        public const string CommonImageQuality = "Images/CommonImageQuality";

        /// <summary>
        /// 第3Mat位于第Index视角
        /// </summary>
        public const string MatDividedView = "Images/DividedView";

        /// <summary>
        /// 交换第3Mat方向
        /// </summary>
        public const string MatExchangeDivideDirection = "Images/ExchangeDivideDirection";

        /// <summary>
        /// 第3Mat和原有Mat分界点
        /// </summary>
        public const string MatDividedIndex = "Images/DividedIndex";

        /// <summary>
        /// 图像加亮吸收率
        /// </summary>
        public const string ImageBrighterAbsorptivity = "Images/BrighterAbsorptivity";

        /// <summary>
        /// 图像变暗吸收率
        /// </summary>
        public const string ImageDarkerAbsorptivity = "Images/DarkerAbsorptivity";

        /// <summary>
        /// 开机默认放大大小
        /// </summary>
        public const string DefaultZoomMultiples = "Images/DefaultZoomMultiples";
        /// <summary>
        /// 开机放大后的默认位置，U代表上，L代表左，D代表下，R代表右，组合后表示最终位置。出现冲突时以ULDR顺序取值。
        /// </summary>
        public const string DefaultZoomLocation = "Images/DefaultZoomLocation";

        /// <summary>
        /// Tip图像 True or False
        /// </summary>
        public const string TipsImage1VerticalFlip = "Images/Image1/TipVerticalFlip";

        public const string TipsImage2VerticalFlip = "Images/Image2/TipVerticalFlip";

        /// <summary>
        /// True or False
        /// </summary>
        public const string ImagesImage1VerticalFlip = "Images/Image1/VerticalFlip";

        public const string ImagesImage2VerticalFlip = "Images/Image2/VerticalFlip";

        /// <summary>
        /// 图像1显示的视角：View1或View2
        /// </summary>
        public const string ImagesImage1ShowingDetView = "Images/Image1/ShowingDetView";

        /// <summary>
        /// 图像2显示的视角：View1或View2
        /// </summary>
        public const string ImagesImage2ShowingDetView = "Images/Image2/ShowingDetView";

        /// <summary>
        /// bool
        /// </summary>
        public const string Image1MoveRightToLeft = "Images/Image1/MoveRightToLeft";

        /// <summary>
        /// bool
        /// </summary>
        public const string Image2MoveRightToLeft = "Images/Image2/MoveRightToLeft";

        public const string ImagesImage1ColorMode = "Images/Image1/ColorMode";

        public const string ImagesImage2ColorMode = "Images/Image2/ColorMode";

        public const string ImagesImage1Classification = "Images/Image1/MaterialClassificationIndex";

        public const string ImagesImage2Classification = "Images/Image2/MaterialClassificationIndex";

        public const string ImagesImage1EnableEdgeEnhance = "Images/Image1/EnableEdgeEnhance";

        public const string ImagesImage2EnableEdgeEnhance = "Images/Image2/EnableEdgeEnhance";

        public const string ImagesImage1EnableSuperEnhance = "Images/Image1/EnableSuperEnhance";

        public const string ImagesImage2EnableSuperEnhance = "Images/Image2/EnableSuperEnhance";

        /// <summary>
        /// True or False
        /// </summary>
        public const string ImagesImage1Inversed = "Images/Image1/Inversed";

        public const string ImagesImage2Inversed = "Images/Image2/Inversed";

        public const string ImagesImage1Penetration = "Images/Image1/Penetration";

        public const string ImagesImage2Penetration = "Images/Image2/Penetration";

        public const string ImagesImage1Height = "Images/Image1/Height";

        public const string ImagesImage2Height = "Images/Image2/Height";

        public const string ImagesImage1Anchor = "Images/Image1/Anchor";

        public const string ImagesImage2Anchor = "Images/Image2/Anchor";

        public const string ImagesImage1VerticalScale = "Images/Image1/VerticalScale";

        public const string ImagesImage2VerticalScale = "Images/Image2/VerticalScale";

        public const string ImagesImage1HorizonalScale = "Images/Image1/HorizonalScale";

        public const string ImagesImage2HorizonalScale = "Images/Image2/HorizonalScale";

        public const string ImagesImage1AddDataAtEnd = "Images/Image1/AddDataAtEnd";
        public const string ImagesImage2AddDataAtEnd = "Images/Image2/AddDataAtEnd";

        // 超级增强处理阈值，太淡的区域不做处理
        public const string ImagesSuperEnhanceThreshold = "Images/SuperEnhanceThreshold";

        #endregion 图像参数设置 Images

        #region 运行参数设定 Options （用户选择的一些与软件状态有关的配置）

        public const string AutoLoginIsEnabled = "Options/AutoLogin/IsEnabled";

        public const string AutoLoginUserId = "Options/AutoLogin/UserId";

        public const string IsGroup = "Options/Group/IsGroup";

        public const string IsNetDumpToOperator = "Options/IsNetDumpToOperator/IsEnabled";

        /// <summary>
        /// 启动图像清理服务的磁盘剩余空间比例
        /// </summary>
        public const string StartDiskSpaceCleanupEmergencyRatio = "Options/DiskManage/StartDiskSpaceCleanupEmergencyRatio";
        
        public const string StartDiskSpaceCleanupRatio = "Options/DiskManage/MinFreeRatioThreshold";
        public const string StopDiskSpaceCleanupRatio = "Options/DiskManage/StopCleanUpFreeRatioThreshold";

        public const string StartDiskSpaceCleanupTime = "Options/DiskManage/StartDiskSpaceCleanupTime";
        public const string StopDiskSpaceCleanupTime = "Options/DiskManage/StopDiskSpaceCleanupTime";

        /// <summary>
        /// 清理历史图像时，每次需要删除的图像数量
        /// </summary>
        public const string ImagesCountToDeleteWhenCleanup = "Options/DiskManage/ImagesCountToDelete";

        /// <summary>
        /// 包裹计数器类型：Total or Session
        /// </summary>
        //public const string PkgCounterType = "Options/PkgCounter/Type";

        /// <summary>
        /// True or False: 登陆后是否重置临时计数器
        /// </summary>
        public const string PkgCounterResetSessionCounterWhenLogin = "Options/PkgCounter/ResetSessionCounterWhenLogin";

        public const string ShowImageBasedOnMotorDirection = "Options/ShowImageBasedOnMotorDirection";
        public const string AddPureWhiteSeperateLinesBetweenObject = "Options/AddPureWhiteSeperateLinesBetweenObject";

        public const string IsRemoveEdgePointsInUI = "Options/RMInstableUI/IsRemoveEdge";

        /// <summary>网络查询时间间隔，单位为0.5s</summary>
        public const string NetworkStateCheckLoopTimes = "Options/NetworkStateCheckLoopTimes";

        /// <summary>是否为线体联动工作模式</summary>
        public const string IsFlowLineMode = "Options/IsFlowLineMode";

        /// <summary>倒带键是否单击即可触发</summary>
        public const string LockConveyorReverseKey = "Options/LockConveyorReverseKey";

        /// <summary>
        /// 回拉回放图像速度倍率
        /// </summary>
        public const string PullPlaySpeedRatio = "Options/PullPlaySpeedRatio";

        #endregion  运行参数设定

        #region 判图站配置
        public const string BagIDCurrentIndex = "Integrated/BagIDCurrentIndex";
        public const string GetJudgeResultLimitTime = "Integrated/GetJudgeResultLimitTime";
        #endregion

        /// <summary>
        /// 高密度告警是否启用：True or False
        /// </summary>
        /// 
        public const string AllowDC = "Images/AllowDoubleClick";
        public const string IntellisenseHdiEnabled = "Intellisense/IsHDIEnabled";

        /// <summary>
        /// 后四个颜色块算法参数
        /// </summary>
        public const string ColorBlockParam1 = "Images/ColorBlockParam1";
        public const string ColorBlockParam2 = "Images/ColorBlockParam2";
        public const string ColorBlockParam3 = "Images/ColorBlockParam3";
        public const string ColorBlockParam4 = "Images/ColorBlockParam4";
        /// <summary>
        /// 毒品检测是否启用：True or False
        /// </summary>
        public const string IntellisenseDeiEnabled = "Intellisense/IsDEIEnabled";

        /// <summary>
        /// 爆炸物检测是否启用
        /// </summary>
        public const string IntellisenseEiEnabled = "Intellisense/IsEIEnabled";


        public const string IntellisenseDeiAudibleAlarm = "Intellisense/DeiAudibleAlarm";

        public const string IntellisenseDeiLightAlarm = "Intellisense/DeiLightAlarm";

        public const string IntellisenseDeiStopConveyor = "Intellisense/DeiStopConveyor";

        public const string IntellisenseHdiAudibleAlarm = "Intellisense/HdiAudibleAlarm";

        public const string IntellisenseHdiLightAlarm = "Intellisense/HdiLightAlarm";

        public const string IntellisenseHdiStopConveyor = "Intellisense/HdiStopConveyor";

        public const string IntellisenseEiAudibleAlarm = "Intellisense/EiAudibleAlarm";

        public const string IntellisenseEiLightAlarm = "Intellisense/EiLightAlarm";

        public const string IntellisenseEiStopConveyor = "Intellisense/EiStopConveyor";

        /// <summary>
        /// 高密度报警灵敏度1-5
        /// </summary>
        public const string IntellisenseHdiSensitivity = "Intellisense/HDISensitivity";

        public const string IntellisenseHdiBorderColor = "Intellisense/HdiBorderColor";

        /// <summary>
        /// 毒品检测灵敏度1-5
        /// </summary>
        public const string IntellisenseDeiSensitivity = "Intellisense/DEISensitivity";

        /// <summary>
        /// 爆炸物检测灵敏度1-5
        /// </summary>
        public const string IntellisenseEiSensitivity = "Intellisense/EISensitivity";

        public const string IntellisenseDrugLowZ = "Intellisense/DrugLowZ";

        public const string IntellisenseDrugHighZ = "Intellisense/DrugHighZ";

        public const string IntellisenseDrugHighX = "Intellisense/DrugHighX";

        public const string IntellisenseExplosivesLowZ = "Intellisense/ExplosivesLowZ";

        public const string IntellisenseExplosivesHighZ = "Intellisense/ExplosivesHighZ";

        public const string IntellisenseExplosivesHighX = "Intellisense/ExplosivesHighX";

        public const string IntellisenseDeiBorderColor = "Intellisense/DeiBorderColor";

        public const string IntellisenseEiBorderColor = "Intellisense/EiBorderColor";

        /// <summary>
        /// 报警指示灯 1：红灯，4：黄灯
        /// </summary>
        public const string IntellisenseAlarmLight ="Intellisense/AlarmLight";

        #region 配合智能判图机器
        public const string IntellisenseSettingFilePath = @"D:\\SecurityScanner\\Config\\Dangers.xml";
        public const string AutoDetectionSendImageToAlgoEnable = "Intellisense/AutoDetection/IsSendImageToAlgo";
        public const string AutoDetectoinSendImageAlgoIp = "Intellisense/AutoDetection/SendImageAlgoIp";
        public const string AutoDetectoinSendImageAlgoPath = "Intellisense/AutoDetection/SendImageAlgoPath";

        public const string AutoDetectionLabelWidth = "Intellisense/AutoDetection/LabelWidth";
        public const string AutoDetectionLabelWidthRatio = "Intellisense/AutoDetection/LabelWidthRatio";
        public const string AutoDetectionLabelHeight = "Intellisense/AutoDetection/LabelHeight";
        public const string AutoDetectionLabelHeightRatio = "Intellisense/AutoDetection/LabelHeightRatio";
        public const string AutoDetectionLabelMaxHeightRatio = "Intellisense/AutoDetection/LabelMaxHeightRatio";
        public const string AutoDetectionLabelFontType = "Intellisense/AutoDetection/LabelFontType";
        public const string AutoDetectionLabelFontSize = "Intellisense/AutoDetection/LabelFontSize";
        public const string AutoDetectionLabelForeColor = "Intellisense/AutoDetection/LabelForeColor";
        public const string AutoDetectionLabelBackColor = "Intellisense/AutoDetection/LabelBackColor";
        public const string AutoDetectionShowLabel = "Intellisense/AutoDetection/ShowLabel";
        #endregion

        #region 培训设置

        /// <summary>
        /// 培训图像的循环模式：枚举类型
        /// </summary>
        public const string TrainingLoopMode = "Training/LoopMode";

        /// <summary>
        /// 培训图像的生成间隔，单位为秒
        /// </summary>
        public const string TrainingImageInterval = "Training/ImageInterval";

        /// <summary>
        /// 是否启动培训功能：True False
        /// </summary>
        public const string TrainingIsEnabled = "Training/IsEnabled";

        #endregion 培训设置 

        #region 一些配置存储路径
        public const string EnableTwoBadChannelFlags = "PreProc/ChannelBadFlags";

        /// <summary>
        /// 视角1的坏点标记列表文件路径（其中存储视角1每个探测通道的坏点标记）
        /// </summary>
        public const string View1BadChannelFlagsSettingFilePath = @"D:\\SecurityScanner\\ChannelBadFlags\\View1.xml";

        public const string View2BadChannelFlagsSettingFilePath = @"D:\\SecurityScanner\\ChannelBadFlags\\View2.xml";

        
        public const string View1NewBadChannelFlagsSettingFilePath = @"D:\\SecurityScanner\\NewChannelBadFlags\\View1.xml";

        public const string View2NewBadChannelFlagsSettingFilePath = @"D:\\SecurityScanner\\NewChannelBadFlags\\View2.xml";


        public const string View1InstableChennelsSettingFilePath = @"D:\\SecurityScanner\\ChannelBadFlags\\InstableChennelsView1.xml";

        public const string View2InstableChennelsSettingFilePath = @"D:\\SecurityScanner\\ChannelBadFlags\\InstableChennelsView2.xml";

        public const string ManualStorePath = @"D:\SecurityScanner\ManualIdentify";

        public const string LogsStorePath = @"D:\\SecurityScanner\\Logs";



        #endregion

        #region Extensions

        #region Htnova

        public const string EnableHtnova = "Extensions/Htnova/EnableHtnova";
        public const string HtonvaJpgImageStorePath = "Extensions/Htnova/JpgImageStorePath";

        public const string IsRuSaveModeEnable = "Extensions/Ru/IsSaveModeEnable";

        #endregion

        #region 西安
        /// <summary>
        /// 是否发送单独视角的通用格式图像
        /// </summary>
        public const string SingleViewIsSendSingleViewImage = "Extensions/SingleView/IsSendSingleViewImage";
        /// <summary>
        /// 两个视角图像的后缀，多数机器第一视角为侧视角，
        /// </summary>
        public const string SingleViewImageFileSuffix = "Extensions/SingleView/ImageFileSuffix";

        /// <summary>
        /// 获取截图时，视角是否要反转（默认视角一为侧视角）
        /// </summary>
        public const string SingleViewIsScreenShotViewReverse = "Extensions/SingleView/IsScreenShotViewReverse";

        public const string CropImage = "Extensions/SingleView/CropImage";
        public const string PrintInfoOnImage = "Extensions/SavePics/PrintInfoOnImage";
        #endregion

        #endregion
    }
}
