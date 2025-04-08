//////////////////////////////////////////////////////////////////////////////////////////////////
//
// 通用控制系统接口。
// 1. 用于控制设备的电机、射线源、光障等，并同时提供这些部件的实时状态供外部查询；
// 2. 实时监控设备的急停按钮、限位开关等。
//
//////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.CompilerServices;
using UI.XRay.Control;
using UI.XRay.ControlWorkflows;

namespace UI.XRay.Flows.Services
{
    public interface IControlServicePart
    {
        #region Events
        /// <summary>
        /// Fired when successfully connected to control system.
        /// </summary>
        event EventHandler DeviceOpened;

        /// <summary>
        /// Fired when successfully disconnect from control system.
        /// </summary>
        event EventHandler DeviceClosed;

        /// <summary>
        /// Event to update connection state. True for Ok while False for error.
        /// </summary>
        event EventHandler<bool> ConnectionStateUpdated;

        /// <summary>
        /// Event fired when conveyor direction has benn changed.
        /// </summary>
        event EventHandler<ConveyorDirectionChangedEventArgs> ConveyorDirectionChanged;

        /// <summary>
        /// Subsribe to this event to receive X-Ray gen alive state changed event. 
        /// </summary>
        event EventHandler<XRayGenAliveChangedEventArgs> XRayGenAliveChanged;

        /// <summary>
        /// Fired when PESensor detect object or not.
        /// </summary>
        event EventHandler<PESensorStateChangedEventArgs> PESensorStateChanged;

        /// <summary>
        /// 事件：在准备要关闭射线之前，激发此事件。订阅者可以在处理事件时，拒绝关闭射线，并指定
        /// 延时一段时间后再关闭射线。
        /// 用途：防止截包。应该仅支持一个订阅者使用。
        /// </summary>
        event EventHandler<PreviewXRayClosingEventArgs> PreviewXRayClosing;

        /// <summary>
        /// 事件：射线的状态变化事件
        /// </summary>
        event EventHandler<XRayStateChangedEventArgs> XRayStateChanged;

        /// <summary>
        /// Fired when work mode changed.
        /// </summary>
        event EventHandler<WorkModeChangedEventArgs> WorkModeChanged;

        /// <summary>
        /// Fired when some switch is turned off or on.
        /// </summary>
        event EventHandler<SwitchStateChangedEventArgs> SwitchStateChanged;

        event EventHandler<ScannerWorkingStates> ScannerWorkingStatesUpdated;

        event EventHandler<bool> SystemShutDown;

        /// <summary>
        /// 事件：进入中断拼图模式
        /// </summary>
        event EventHandler EnterInterruptMode;

        /// <summary>进入维护模式</summary>
        event EventHandler EnterMaintenanceMode;
        #endregion

        /// <summary>
        /// Current work mode.
        /// </summary>
        ScannerWorkMode WorkMode { get; }

        /// <summary>
        /// 是否支持双向扫描：true表示从两个方向都可以扫描成像；false表示仅支持正向扫描，反向时不显示图像
        /// 注意：在改变此标志位时，输送机会被强行停止
        /// </summary>
        bool BidirectionalScan { get; }

        /// <summary>
        /// Is connected to control system now.
        /// </summary>
        bool IsOpened { get; }

        #region Methods
        /// <summary>
        /// 连接至控制板。
        /// </summary>
        bool Open();

        /// <summary>
        /// 断开控制系统连接。
        /// </summary>
        void Close();

        /// <summary>
        /// Reboot control board.
        /// </summary>
        /// <returns></returns>
        bool RebootControlSys();

        /// <summary>
        /// Shutdown scanner.
        /// </summary>
        /// <returns></returns>
        bool ShutdownScanner();

        /// <summary>
        /// Enable or disable bidirectional scan.
        /// </summary>
        /// <param name="enabled">true to enbale bidirectional scan, or false to disable.</param>
        /// <returns>true for success, or false for failed</returns>
        bool EnableBidirectionalScan(bool enabled);

        /// <summary>
        /// 转动电机：适用于所有工作模式。
        /// 对于Regular正常工作模式，只 需要 驱动输送机即可。
        /// </summary>
        /// <param name="direction"></param>
        bool DriveConveyor(ConveyorDirection direction);

        /// <summary>
        /// Radiate x-ray or close x-ray manually
        /// </summary>
        /// <returns></returns>
        bool RadiateXRay(bool radiate);

        /// <summary>
        /// Light or de-light x-ray lamp on machine
        /// </summary>
        /// <param name="on"></param>
        /// <returns></returns>
        bool LightXRayLamp(bool on);

        /// <summary>
        /// Yellow lamp on machine
        /// </summary>
        /// <param name="on"></param>
        /// <returns></returns>
        bool LightYellowLamp(bool on);


        /// <summary>
        /// Flicker XRay Lamp to light alarm
        /// </summary>
        /// <returns></returns>
        bool FlickerXRayLamp(bool fastMode);

        /// <summary>
        /// Enable or disable PESensor manually.
        /// </summary>
        /// <param name="activate"></param>
        /// <returns></returns>
        bool PowerOnPESensors(bool activate);

        /// <summary>
        /// Enable or disable PESensor manually.
        /// </summary>
        /// <param name="activate"></param>
        /// <param name="index">index of PE Sensor</param>
        /// <returns></returns>
        bool PowerOnPESensors(bool activate, int index);

        /// <summary>
        /// Make control system beeping.
        /// </summary>
        /// <param name="beep"></param>
        /// <returns></returns>
        bool Beep(bool beep);

        /// <summary>
        /// 更改射线源的配置
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        bool ChangeXRayGenSettings(XRayGeneratorSettings settings, XRayGeneratorIndex index);

        /// <summary>
        /// Set control system work mode.
        /// </summary>
        /// <param name="mode">work mode to set.</param>
        /// <returns>true if successful, false if failed, maybe not connected.</returns>
        bool SetWorkMode(ScannerWorkMode mode);

        /// <summary>
        /// 设置电机启动时间
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        bool SetConveyorStartTime(ushort time);

        /// <summary>
        /// 设置电机速度
        /// </summary>
        /// <param name="speed"></param>
        /// <returns></returns>
        bool SetConveyorSpeed(float speed);

        /// <summary>
        /// 设置电机停止时间
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        bool SetConveyorStopTime(ushort time);

        /// <summary>
        /// 设置消杀电机速度
        /// </summary>
        /// <param name="speed"></param>
        /// <returns></returns>
        bool SetSanitizeConveyorSpeed(float speed);

        /// <summary>
        /// 设置消杀电机启动时间
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        bool SetSanitizeConveyorStartTime(ushort time);

        /// <summary>
        /// 设置消杀电机停止时间
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        bool SetSanitizeConveyorStopTime(ushort time);

        /// <summary>
        /// 设置控制板查询光机指令时间间隔
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        bool SetXRayCmdInterval(ushort time);

        /// <summary>
        /// 设置包裹数
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        bool SetBagCount(byte mode, ushort count);

        /// <summary>
        /// 机器上的声音报警器
        /// </summary>
        /// <param name="isOn"></param>
        /// <returns></returns>
        bool SetContrabandAlarm(bool isOn);

        bool SetWorkTiming(bool isEnable);

        /// <summary>
        /// 控制板包裹总计数重新装载
        /// </summary>
        /// <param name="bagCount">硬件计数器读取的包裹数</param>
        /// <returns>设置是否成功</returns>
        bool ReloadTotalBagCount(uint bagCount);

        /// <summary>
        /// Gets control system working state.
        /// </summary>
        /// <param name="states"></param>
        /// <returns></returns>
        bool GetWorkingStates(out ScannerWorkingStates states);

        /// <summary>
        /// Get control system description, including version, manufacture data and so on.
        /// </summary>
        /// <param name="desc"></param>
        /// <returns></returns>
        bool GetSystemDesc(out string desc);

        /// <summary>
        /// 获取射线源的配置
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        bool GetXRayGenSettings(out XRayGeneratorSettings settings);

        /// <summary>
        /// 获取射线源的当前工作状态
        /// </summary>
        /// <param name="states"></param>
        /// <returns></returns>
        bool GetXRayGenWorkingStates(out XRayGeneratorWorkingStates states);

        /// <summary>
        /// 获取控制板固件版本和协议版本
        /// </summary>
        /// <param name="firmware"></param>
        /// <param name="protocol"></param>
        /// <returns></returns>
        bool GetSystemDesc(ref CtrlSysVersion firmware, ref CtrlSysVersion protocol);


        /// <summary>
        /// 获取包裹数
        /// </summary>
        /// <param name="bagCount">返回的包裹数</param>
        /// <returns>是否获取成功</returns>
        bool GetTotalBagCount(ref int bagCount);
        #endregion
    }
}
