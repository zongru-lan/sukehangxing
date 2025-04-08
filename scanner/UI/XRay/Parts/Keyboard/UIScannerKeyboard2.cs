using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using UI.Common.Tracers;

namespace UI.XRay.Parts.Keyboard
{

    public class KeyRecordStruct
    {
        public byte KeyValue { get; set; }
        public DateTime LastKeyPressDateTime { get; set; }
        public DateTime KeyUpDateTime { get; set; }

        public int Index { get; set; }

        public bool IsWaitUp { get; set; }

        public List<byte> KeyList { get; set; }

        public KeyRecordStruct(byte keyvalue,DateTime lastpress,DateTime keyup,int index,bool isWaitUp,List<byte> keylist)
        {
            KeyValue = keyvalue;
            LastKeyPressDateTime = lastpress;
            KeyUpDateTime = keyup;
            Index = index;
            IsWaitUp = isWaitUp;
            KeyList = keylist;
        }
    }

    /// <summary>
    /// USB 通用键盘
    /// </summary>
    public class UIScannerKeyboard2
    {
        public static UIScannerKeyboard2 Instance { get; private set; }

        static UIScannerKeyboard2()
        {
            Instance = new UIScannerKeyboard2();
        }


        #region 键值映射
        public const Key InverseImageKey = Key.OemTilde;
        public const Key IncreaseAbsorbKey = Key.J;
        public const Key DecreaseAbsorbKey = Key.F;
        public const Key HighPenetrateKey = Key.L;

        public const Key BlackWhiteKey = Key.V;
        public const Key OsKey = Key.F4;
        public const Key MsKey = Key.C;
        public const Key SenKey = Key.X;

        #region 以下仅定义键值，但是串口键盘中并不包含这些键，只能使用计算机键盘来触发
        public const Key LowPenetrateKey = Key.O;

        public const Key EdgeEnhanceKey = Key.P;

        public const Key PullFrontKey = Key.Z;
        public const Key PullBackKey = Key.A;

        /// <summary>
        /// 放大镜按键
        /// </summary>
        public const Key MagnifyKey = Key.R;

        #endregion

        public const Key ShiftKey = Key.D8;
        public const Key AutoKey = Key.D2;

        public const Key MarkKey = Key.D1;
        public const Key SaveKey = Key.D3;

        /// <summary>
        /// Continuous scan key. 
        /// </summary>
        public const Key ImsKey = Key.D4;
        public const Key MenuKey = Key.D5;
        public const Key EscKey = Key.D6;
        public const Key Z789Key = Key.D7;

        /// <summary>
        /// Dynamic Gray-Scale Transformation
        /// 动态灰度扫描
        /// </summary>
        public const Key DynamicGSTKey = Key.D9;

        public const Key Zoom1X = Key.D0;

        // 缩小
        public const Key ZoomOutKey = Key.System;

        // 放大
        public const Key ZoomInKey = Key.F9;

        public const Key F1Key = Key.F1;
        public const Key F2Key = Key.F2;
        public const Key F3Key = Key.F3;

        public const Key ConveyorBackwardKey = Key.O;
        public const Key ConveyorStopKey = Key.S;
        public const Key ConveyorForwardKey = Key.I;

        public const Key PushLeft = Key.Z;
        public const Key PushRight = Key.A;

        public const Key UpKey = Key.M;
        public const Key DownKey = Key.T;
        public const Key LeftKey = Key.B;
        public const Key RightKey = Key.W;

        public const Key LeftTop = Key.N;
        public const Key LeftBot = Key.Y;
        public const Key RightTop = Key.Q;
        public const Key RightBot = Key.E;

        /// <summary>
        /// 系统关机键。当长按电机停止键超过3秒钟时，虚拟化出系统关机键
        /// </summary>
        public const Key ShutdownKey = Key.End;

        /// <summary>
        /// 数字2-9转换
        /// </summary>
        Dictionary<byte, List<byte>> _convertWithNumber = new Dictionary<byte, List<byte>>();

        #endregion 键值映射

        /// <summary>
        /// 要打开的串口的名称
        /// </summary>
        public static readonly string PortDesc = "USB Common Keyboard";


        /// <summary>
        /// 当前缓存的处于摁下状态的按键的值的字典，包含上一次被摁下的时刻
        /// </summary>
        private ConcurrentDictionary<byte, DateTime> _keydownTimeStamps = new ConcurrentDictionary<byte, DateTime>();

        private List<KeyRecordStruct> _keyRecordInfoList = new List<KeyRecordStruct>(9);

        /// <summary>
        /// 用于周期性检测按键弹起的定时器，每隔80ms做一次检查。
        /// </summary>
        private Timer _keyUpCheckingTimer;

        private int _index = 1;

        public UIScannerKeyboard2()
        {
            _initConvertWithNumber();
        }

        private void _initConvertWithNumber()
        {
            _keyRecordInfoList.Add(new KeyRecordStruct(0x23, DateTime.Now, DateTime.Now, 0, false, new List<byte> { 0x23, 0x2C, 0x2D, 0x2E }));
            _keyRecordInfoList.Add(new KeyRecordStruct(0x24, DateTime.Now, DateTime.Now, 0, false, new List<byte> { 0x24, 0x2F, 0x30, 0x31 }));
            _keyRecordInfoList.Add(new KeyRecordStruct(0x25, DateTime.Now, DateTime.Now, 0, false, new List<byte> { 0x25, 0x32, 0x33, 0x34 }));
            _keyRecordInfoList.Add(new KeyRecordStruct(0x26, DateTime.Now, DateTime.Now, 0, false, new List<byte> { 0x26, 0x35, 0x36, 0x37 }));
            _keyRecordInfoList.Add(new KeyRecordStruct(0x27, DateTime.Now, DateTime.Now, 0, false, new List<byte> { 0x27, 0x38, 0x39, 0x3A }));
            _keyRecordInfoList.Add(new KeyRecordStruct(0x28, DateTime.Now, DateTime.Now, 0, false, new List<byte> { 0x28, 0x3B, 0x3C, 0x3D }));
            _keyRecordInfoList.Add(new KeyRecordStruct(0x29, DateTime.Now, DateTime.Now, 0, false, new List<byte> { 0x29, 0x3E, 0x3F, 0x40 }));
            _keyRecordInfoList.Add(new KeyRecordStruct(0x2A, DateTime.Now, DateTime.Now, 0, false, new List<byte> { 0x2A, 0x41, 0x42, 0x43 }));
            _keyRecordInfoList.Add(new KeyRecordStruct(0x2B, DateTime.Now, DateTime.Now, 0, false, new List<byte> { 0x2B, 0x44, 0x45 }));
        }     

        private void KeyUpCheckingTimerCallback(object state)
        {
            lock (this)
            {
                // lock this to avoid calling after disposed.
                if (_keyUpCheckingTimer != null)
                {
                    SwitchHandleKeysEventMessage();
                    _keyUpCheckingTimer.Change(30, Timeout.Infinite);
                }
            }
        }

        byte lastKeyPress = 0x00;
        public void AddKey(byte keyValue)
        {
            if (lastKeyPress != keyValue && IsKeyValid(lastKeyPress))
            {
                var record = _keyRecordInfoList[lastKeyPress - 0x23];
                if (record.Index > 0)
                {
                    record.KeyUpDateTime = DateTime.Now;
                    record.IsWaitUp = true;
                    Keyboard1OnKeyDown(this, new UIKeyEventArgs(record.KeyList[(record.Index - 1) % record.KeyList.Count], true, false));
                    record.Index = 0;
                }
            }

            if (IsKeyValid(keyValue))
            {
                _keyRecordInfoList[keyValue - 0x23].LastKeyPressDateTime = DateTime.Now;
                _keyRecordInfoList[keyValue - 0x23].Index++;
            }

            lastKeyPress = keyValue;
        }

        /// <summary>
        /// 按键是否有效
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private bool IsKeyValid(byte key)
        {
            return key >= 0x23 && key <= 0x2B;
        }

        private void SwitchHandleKeysEventMessage()
        {
            try
            {
                lock (_keyRecordInfoList)
                {
                    foreach (var record in _keyRecordInfoList)
                    {
                        if (record.Index > 0 && !record.IsWaitUp)
                        {
                            if (DateTime.Now - record.LastKeyPressDateTime > TimeSpan.FromMilliseconds(400))
                            {
                                Keyboard1OnKeyDown(this, new UIKeyEventArgs(record.KeyList[(record.Index-1) % record.KeyList.Count ], true, false));
                                record.IsWaitUp = true;
                                record.KeyUpDateTime = DateTime.Now;
                            }
                        }
                        if (record.IsWaitUp)
                        {
                            if (DateTime.Now - record.KeyUpDateTime > TimeSpan.FromMilliseconds(500))
                            {
                                Keyboard1OnKeyUp(this, new UIKeyEventArgs(record.KeyList[(record.Index-1) % record.KeyList.Count ], true, false));
                                record.IsWaitUp = false;
                                record.Index = 0;
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }


        /// <summary>
        /// Open Keyboard to listen for key event.
        /// </summary>
        /// <param name="portName">串口键盘在计算机中的串口号。如果为空，则将默认连接第一个描述为 USB Serial Port 的串口去连接</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool Open()
        {
            _keyUpCheckingTimer = new Timer(KeyUpCheckingTimerCallback, null, 25, Timeout.Infinite);
            return true;
        }

       

        /// <summary>
        /// Close Keyboard, and can be opened again.
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Close()
        {
            if (_keyUpCheckingTimer != null)
            {
                _keyUpCheckingTimer.Dispose();
                _keyUpCheckingTimer = null;
            }
        }

        /// <summary>
        /// 处理串口键盘发来的按键弹起事件，并且以虚拟按键的形式发送到系统中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private static void Keyboard1OnKeyUp(object sender, UIKeyEventArgs args)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                SendKeyEvents.Release((Key)(args.KeyValue));
            });
        }

        /// <summary>
        /// 事件响应：在专用的串口键盘中按下了某个按键
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private static void Keyboard1OnKeyDown(object sender, UIKeyEventArgs args)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // 当按下的是Shift按键时，不做处理。在Shift键弹起时做处理
                //if (k != ShiftKey)
                {
                    SendKeyEvents.Press((Key)(args.KeyValue));
                }
            });
        }
    }
}
