//////////////////////////////////////////////////////////////////////////////////////////
//
// UIKeyboardMapping1：
// 设计说明：可以使用指定的串口号调用Open来打开键盘；
// 如果调用Open时未指定串口号，则会默认尝试打开带有 Usb Serial Port 描述的串口；如果
// 没有找到此串口，则再使用默认的串口1来打开键盘。
// 
/////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using UI.Common.Tracers;

namespace UI.XRay.Parts.Keyboard
{
    /// <summary>
    /// UI keyboard mapping. 
    /// </summary>
    public static class UIScannerKeyboard1
    {
        #region 键值映射
        public const Key InverseImageKey = Key.A;
        public const Key IncreaseAbsorbKey = Key.B;
        public const Key DecreaseAbsorbKey = Key.C;
        public const Key HighPenetrateKey = Key.D;

        public const Key BlackWhiteKey = Key.E;
        public const Key OsKey = Key.F;
        public const Key MsKey = Key.G;
        public const Key SenKey = Key.H;

        #region 以下仅定义键值，但是串口键盘中并不包含这些键，只能使用计算机键盘来触发
        public const Key LowPenetrateKey = Key.O;

        public const Key EdgeEnhanceKey = Key.P;

        public const Key PullFrontKey = Key.Oem4;
        public const Key PullBackKey = Key.Oem6;

        /// <summary>
        /// 放大镜按键
        /// </summary>
        public const Key MagnifyKey = Key.R;

        #endregion

        public const Key ShiftKey = Key.LeftShift;
        public const Key AutoKey = Key.LeftCtrl;

        public const Key MarkKey = Key.D1;
        public const Key SaveKey = Key.D2;

        /// <summary>
        /// Continuous scan key. 
        /// </summary>
        public const Key ContinuousScanKey = Key.D3;
        public const Key ImsKey = Key.D4;
        public const Key MenuKey = Key.D5;
        public const Key EscKey = Key.D6;
        public const Key Z789Key = Key.D7;

        /// <summary>
        /// 垂直翻转图像
        /// </summary>
        public const Key VFlipKey = Key.D8;

        /// <summary>
        /// Dynamic Gray-Scale Transformation
        /// 动态灰度扫描
        /// </summary>
        public const Key DynamicGSTKey = Key.D9;

        public const Key Zoom1X = Key.D0;

        // 缩小
        public const Key ZoomOutKey = Key.OemMinus;

        // 放大
        public const Key ZoomInKey = Key.OemPlus;

        public const Key F1Key = Key.F1;
        public const Key F2Key = Key.F2;
        public const Key F3Key = Key.F3;

        public const Key ConveyorBackwardKey = Key.F4;
        public const Key ConveyorStopKey = Key.F5;
        public const Key ConveyorForwardKey = Key.F6;

        public const Key UpKey = Key.Up;
        public const Key DownKey = Key.Down;
        public const Key LeftKey = Key.Left;
        public const Key RightKey = Key.Right;

        public const Key LeftTop = Key.U;
        public const Key LeftBot = Key.J;
        public const Key RightTop = Key.I;
        public const Key RightBot = Key.K;

        /// <summary>
        /// 系统关机键。当长按电机停止键超过3秒钟时，虚拟化出系统关机键
        /// </summary>
        public const Key ShutdownKey = Key.End;

        /// <summary>
        /// 键值映射：将键盘板发来的键值，转换为WPF枚举的键值
        /// </summary>
        private static Dictionary<byte, Key> _codeKeyDict = new Dictionary<byte, Key>(48);
        #endregion 键值映射

        private static SerialPortKeyboard1 _keyboard1;

        public static void SetWitchInputStates(bool state)
        {
            if (_keyboard1 != null)
            {
                _keyboard1.IsSwitchInputEnable = state;
            }            
        }

        /// <summary>
        /// 要打开的串口的名称
        /// </summary>
        public static readonly string PortDesc = "USB Serial Port";

        /// <summary>
        /// Is shiftkey is down now.
        /// </summary>
        public static bool IsSystemShiftKeyDown { get; private set; }

        /// <summary>
        /// Is keyboard alive now.
        /// </summary>
        public static bool IsAlive
        {
            get
            {
                lock (typeof(UIScannerKeyboard1))
                {
                    if (_keyboard1 != null)
                    {
                        return _keyboard1.IsAlive;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Make buzzer on keyboard buzzing.
        /// </summary>
        private static bool Beeping
        {
            get
            {
                if (_keyboard1 != null)
                {
                    return _keyboard1.IsBuzzing; 
                }
                return false;
            }
            set
            {
                if (_keyboard1 != null)
                {
                    _keyboard1.IsBuzzing = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets key switch indicator state.
        /// </summary>
        public static bool IsKeySwitchIndicatorOn
        {
            get
            {
                if (_keyboard1 != null)
                    return _keyboard1.IsKeySwitchIndicatorOn;
                return false;
            }
            set
            {
                if (_keyboard1 != null)
                {
                    _keyboard1.IsKeySwitchIndicatorOn = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets power indicator state.
        /// </summary>
        public static bool IsPowerIndicatorOn
        {
            get
            {
                if (_keyboard1 != null)
                    return _keyboard1.IsSysPowerIndicatorOn;
                return false;
            }
            set
            {
                if (_keyboard1 != null)
                    _keyboard1.IsSysPowerIndicatorOn = value;
            }
        }

        /// <summary>
        /// Gets or sets X-Ray indicator 1 state.
        /// </summary>
        public static bool IsXRayIndicator1On
        {
            get
            {
                if (_keyboard1 != null)
                    return _keyboard1.IsXRayIndicator1On;
                return false;
            }
            set
            {
                if (_keyboard1 != null)
                    _keyboard1.IsXRayIndicator1On = value;
            }
        }

        /// <summary>
        /// Gets or sets X-Ray indicator 2 state.
        /// </summary>
        public static bool IsXRayIndicator2On
        {
            get
            {
                if (_keyboard1 != null)
                    return _keyboard1.IsXRayIndicator2On;
                return false;
            }
            set
            {
                if (_keyboard1 != null)
                    _keyboard1.IsXRayIndicator2On = value;
            }
        }

        static UIScannerKeyboard1()
        {
            CreateCodeKeysMap();
        }

        #region Beeping thread operation

        /// <summary>
        /// 周期性蜂鸣的专用线程是否已经存在：同一时刻，仅允许一个线程在修改蜂鸣器，否则可能会导致蜂鸣声音很不规律
        /// </summary>
        private static bool _beepingThreadAlive;

        /// <summary>
        /// 当前正在蜂鸣的时长、间隔、次数等信息
        /// </summary>
        private static BeepingDesc _beepingDesc;

        /// <summary>
        /// 按指定的时间间隔和次数进行蜂鸣告警
        /// </summary>
        /// <param name="pulseWidth">每次蜂鸣的时长</param>
        /// <param name="interval">每两次蜂鸣之间的间隔时间</param>
        /// <param name="times">蜂鸣的次数</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void StartBeep(TimeSpan pulseWidth, TimeSpan interval, int times)
        {
            // 更新蜂鸣参数
            _beepingDesc = new BeepingDesc(pulseWidth, interval, times);

            // 如果当前已经有线程在蜂鸣，则不需要再次分配专属线程
            // 如果当前没有正在运行的蜂鸣线程，则在线程池中启动一个新的线程来工作
            if (!_beepingThreadAlive)
            {
                ThreadPool.QueueUserWorkItem(BeepingThreadCallback);

                // 新线程已经创建。
                _beepingThreadAlive = true;
            }
        }

        /// <summary>
        /// 停止蜂鸣器
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void StopBeep()
        {
            _beepingDesc = null;
            Beeping = false;
        }

        private static void BeepingThreadCallback(object state)
        {
            TimeSpan pulse = new TimeSpan(0);
            TimeSpan interval = new TimeSpan(0);
            int times = 1;

            while (times>0)
            {
                lock (typeof(UIScannerKeyboard1))
                {
                    if (_beepingDesc != null)
                    {
                        // 取出最新的蜂鸣参数（在蜂鸣的过程中，外部由可能更新了蜂鸣参数，因此需要再次取出最新的蜂鸣）
                        pulse = _beepingDesc.PulseWidth;
                        interval = _beepingDesc.Interval;
                        times = _beepingDesc.Times;

                        _beepingDesc.Times--;
                    }
                    else
                    {
                        // 如果 _beepingDesc == null 了，可能是外部突然停止了蜂鸣，因此将不会再继续
                        times = 0;
                    }

                    // 当剩余的蜂鸣次数为0时，将结束蜂鸣线程
                    if (times == 0)
                    {
                        _beepingDesc = null;
                        _beepingThreadAlive = false;
                        break;
                    }
                }

                // 蜂鸣
                Beeping = true;
                Thread.Sleep(pulse);

                // 间隔停止蜂鸣
                Beeping = false;
                Thread.Sleep(interval);
            }
        }

        #endregion beeping thread.

        /// <summary>
        /// 使用WMI接口，从系统中枚举所有的串口，并从中找出包含 PartDesc 字符串的串口名
        /// </summary>
        /// <returns>包含 PartDesc 的串口名称，如 COM4。如果未找到则返回空字符串</returns>
        private static string GetKeyboardComPortName(string portName)
        {
            try
            {
                var allComNames = SerialPortKeyboard1.EnumAllCOMPortNames();

                if (allComNames != null && allComNames.Length > 0)
                {
                    foreach (var name in allComNames)
                    {
                        if (name.Contains(portName))
                        {
                            var flag = name.IndexOf('(');
                            if (flag >= 0)
                            {
                                var result = name.Remove(0, flag + 1);
                                result = result.Trim(new[] { '(', ')' });
                                return result;
                            }
                        }
                    }
                }

            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception, "Could not find Serial Port keyboard with description: " + PortDesc);
            }

            return string.Empty;
        }

        /// <summary>
        /// 创建键值映射：键盘发来的键值码与WPF的Key的枚举值不是对应关系，因此需要进行转换映射
        /// </summary>
        private static void CreateCodeKeysMap()
        {
            _codeKeyDict.Add(0x70, F1Key);
            _codeKeyDict.Add(0x71, F2Key);
            _codeKeyDict.Add(0x72, F3Key);
            _codeKeyDict.Add(0x73, ConveyorBackwardKey);
            _codeKeyDict.Add(0x74, ConveyorStopKey);
            _codeKeyDict.Add(0x75, ConveyorForwardKey);

            _codeKeyDict.Add(0x41, InverseImageKey);
            _codeKeyDict.Add(0x42, IncreaseAbsorbKey);
            _codeKeyDict.Add(0x43, DecreaseAbsorbKey);
            _codeKeyDict.Add(0x44, HighPenetrateKey);

            _codeKeyDict.Add(0x45, BlackWhiteKey);
            _codeKeyDict.Add(0x46, OsKey);
            _codeKeyDict.Add(0x47, MsKey);
            _codeKeyDict.Add(0x48, SenKey);

            _codeKeyDict.Add(0x49, Key.I);               // I
            _codeKeyDict.Add(0x4A, Key.J);               // J
            _codeKeyDict.Add(0x4B, Key.K);               // K
            _codeKeyDict.Add(0x4C, Key.L);               // L
            _codeKeyDict.Add(0x4D, Key.M);               // M
            _codeKeyDict.Add(0x4E, Key.N);               // N
            _codeKeyDict.Add(0x4F, LowPenetrateKey);     // O
            _codeKeyDict.Add(0x50, EdgeEnhanceKey);      // P
            _codeKeyDict.Add(0x51, Key.Q);               // Q
            _codeKeyDict.Add(0x52, MagnifyKey);          // R
            _codeKeyDict.Add(0x53, Key.S);               // S
            _codeKeyDict.Add(0x54, Key.T);               // T
            _codeKeyDict.Add(0x55, Key.U);               // U
            _codeKeyDict.Add(0x56, Key.V);               // V
            _codeKeyDict.Add(0x57, Key.W);               // W
            _codeKeyDict.Add(0x58, Key.X);               // X
            _codeKeyDict.Add(0x59, Key.Y);               // Y
            _codeKeyDict.Add(0x5A, Key.Z);               // Z

            // left shift
            _codeKeyDict.Add(0x10, ShiftKey);

            // left ctrl
            _codeKeyDict.Add(0x11, AutoKey);

            _codeKeyDict.Add(0x60, Zoom1X);
            _codeKeyDict.Add(0x61, MarkKey);
            _codeKeyDict.Add(0x62, SaveKey);
            _codeKeyDict.Add(0x63, ContinuousScanKey);
            _codeKeyDict.Add(0x64, ImsKey);
            _codeKeyDict.Add(0x65, MenuKey);
            _codeKeyDict.Add(0x66, EscKey);
            _codeKeyDict.Add(0x67, Z789Key);
            _codeKeyDict.Add(0x68, VFlipKey);
            _codeKeyDict.Add(0x69, DynamicGSTKey);

            _codeKeyDict.Add(0x25, LeftKey);
            _codeKeyDict.Add(0x27, RightKey);
            _codeKeyDict.Add(0x26, UpKey);
            _codeKeyDict.Add(0x28, DownKey);

            _codeKeyDict.Add(0x6d, ZoomOutKey);
            _codeKeyDict.Add(0x6B, ZoomInKey);
        }

        /// <summary>
        /// Open Keyboard to listen for key event.
        /// </summary>
        /// <param name="portName">串口键盘在计算机中的串口号。如果为空，则将默认连接第一个描述为 USB Serial Port 的串口去连接</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static bool Open(string portName = null)
        {
            Close();

            // 如果输入的串口名称为空，则尝试获取描述为 USB Serial Port 的串口编号
            if (string.IsNullOrEmpty(portName))
            {
                Tracer.TraceWarning("portName is not set for SerialPort Keyboard, will try to resolve port with description contains " + PortDesc);
                portName = GetKeyboardComPortName(PortDesc);
            }
            else 
            {
                // 如果输入的是非法的串口名称，则解释为串口描述，根据串口描述来获取串口名称
                if (!IsValidPortName(portName))
                {
                    portName = GetKeyboardComPortName(portName);
                }
            }

            // 如果输入的串口名称为空，或者未找到描述为USB Serial Port的串口，也未找到根据用户指定的串口描述对应的串口，则使用默认的 COM1。
            if (string.IsNullOrEmpty(portName))
            {
                portName = "COM1";
                Tracer.TraceError("SerialPort Keyboard with desciption " + PortDesc + " could not be found. " +
                                  "Please make sure the Keyboard has been connected. Use default COM1 to try to connect to keyboard.");
            }

            _keyboard1 = new SerialPortKeyboard1(portName);
            _keyboard1.KeyDown += Keyboard1OnKeyDown;
            _keyboard1.KeyUp += Keyboard1OnKeyUp;

            IsPowerIndicatorOn = true;

            return _keyboard1.Open();
        }

        /// <summary>
        /// 判断一个输入的字符串是否是合法的串口名：如 COM1
        /// </summary>
        /// <param name="portName"></param>
        /// <returns></returns>
        private static bool IsValidPortName(string portName)
        {
            if (string.IsNullOrEmpty(portName) || portName.Length < 4 || portName.Length > 5)
            {
                return false;
            }

            var pre = portName.Substring(0, 3);
            return string.Equals(pre, "com", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Close Keyboard, and can be opened again.
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void Close()
        {
            if (_keyboard1 != null)
            {
                _keyboard1.Close();
                _keyboard1.KeyDown -= Keyboard1OnKeyDown;
                _keyboard1.KeyUp -= Keyboard1OnKeyUp;
                _keyboard1.Dispose();
                _keyboard1 = null;
            }
        }

        /// <summary>
        /// 处理串口键盘发来的按键弹起事件，并且以虚拟按键的形式发送到系统中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private static void Keyboard1OnKeyUp(object sender, UIKeyEventArgs args)
        {
            Key k;
            if (_codeKeyDict.TryGetValue(args.KeyValue, out k))
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // 对Shift键需要进行特殊处理：每当专用键盘中的shift键弹起时，虚拟化系统的Shift键按下和弹起事件。
                    //if (k == ShiftKey)
                    //{
                    //    if (IsSystemShiftKeyDown)
                    //    {
                    //        // 模拟弹起Shift键
                    //        SendKeyEvents.Release(k);
                    //    }
                    //    else
                    //    {
                    //        // 模拟按下Shift键
                    //        SendKeyEvents.Press(k);
                    //    }
                    //    IsSystemShiftKeyDown = !IsSystemShiftKeyDown;
                    //}
                    //else
                    {
                        SendKeyEvents.Release(k);
                    }
                });
            }
        }

        /// <summary>
        /// 事件响应：在专用的串口键盘中按下了某个按键
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private static void Keyboard1OnKeyDown(object sender, UIKeyEventArgs args)
        {
            Key k;
            if (_codeKeyDict.TryGetValue(args.KeyValue, out k))
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // 当按下的是Shift按键时，不做处理。在Shift键弹起时做处理
                    //if (k != ShiftKey)
                    {
                        SendKeyEvents.Press(k);
                    }
                });
            }
            else
            {
                Tracer.TraceError("Could not find Key mapping for keycode: " + args.KeyValue);
            }
        }

        /// <summary>
        /// 有关蜂鸣的描述：时长、间隔、次数
        /// </summary>
        class BeepingDesc
        {
            public BeepingDesc(TimeSpan pulse, TimeSpan interval, int times)
            {
                PulseWidth = pulse;
                Interval = interval;
                Times = times;
            }

            /// <summary>
            /// 每次蜂鸣时间长度
            /// </summary>
            public TimeSpan PulseWidth;

            /// <summary>
            /// 每两次蜂鸣之间的间隔
            /// </summary>
            public TimeSpan Interval;

            /// <summary>
            /// 蜂鸣的总次数
            /// </summary>
            public int Times;
        }
    }
}
