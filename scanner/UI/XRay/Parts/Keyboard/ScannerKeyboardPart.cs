using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using UI.XRay.Business.Entities;
using UI.XRay.Business.DataAccess.Config;

namespace UI.XRay.Parts.Keyboard
{
    /// <summary>
    /// 使用盾威外壳，齐影内核的安检专用键盘
    /// </summary>
    public class ScannerKeyboardPart
    {
        public static ScannerKeyboardPart Keyboard { get; private set; }

        static ScannerKeyboardPart()
        {
            Keyboard = new ScannerKeyboardPart();
        }

        /// <summary>
        /// 受保护构造函数，不允许外部创建
        /// </summary>
        protected ScannerKeyboardPart()
        {
            string keyboardType = string.Empty;
            if (!ScannerConfig.Read(ConfigPath.KeyboardComName, out keyboardType))
            {
                keyboardType = "USB Serial Port";
            }
            if (keyboardType == "USB Common Keyboard")
            {
                IsUSBCommonKeyboard = true;
            }
            else
            {
                IsUSBCommonKeyboard = false;
            }

            //通用键盘
            if (IsUSBCommonKeyboard)
            {
                F1 = UIScannerKeyboard2.F1Key;
                F2 = UIScannerKeyboard2.F2Key;
                F3 = UIScannerKeyboard2.F3Key;
                Inverse = UIScannerKeyboard2.InverseImageKey;
                IncreaseAbsorb = UIScannerKeyboard2.IncreaseAbsorbKey;
                DecreaseAbsorb = UIScannerKeyboard2.DecreaseAbsorbKey;
                HighPenetrate = UIScannerKeyboard2.HighPenetrateKey;
                LowPenetrate = UIScannerKeyboard2.LowPenetrateKey;
                BlackWhite = UIScannerKeyboard2.BlackWhiteKey;
                Os = UIScannerKeyboard2.OsKey;
                Ms = UIScannerKeyboard2.MsKey;
                Sen = UIScannerKeyboard2.SenKey;
                EdgeEnhance = UIScannerKeyboard2.EdgeEnhanceKey;
                PullFront = UIScannerKeyboard2.PullFrontKey;
                PullBack = UIScannerKeyboard2.PullBackKey;
                Magnify = UIScannerKeyboard2.MagnifyKey;
                Shift = UIScannerKeyboard2.ShiftKey;
                Auto = UIScannerKeyboard2.AutoKey;
                Mark = UIScannerKeyboard2.MarkKey;
                Save = UIScannerKeyboard2.SaveKey;
                Ims = UIScannerKeyboard2.ImsKey;
                Menu = UIScannerKeyboard2.MenuKey;
                Esc = UIScannerKeyboard2.EscKey;
                Z789 = UIScannerKeyboard2.Z789Key;
                DynamicGST = UIScannerKeyboard2.DynamicGSTKey;
                Zoom1X = UIScannerKeyboard2.Zoom1X;
                ZoomOut = UIScannerKeyboard2.ZoomOutKey;
                ZoomIn = UIScannerKeyboard2.ZoomInKey;
                ConveyorLeft = UIScannerKeyboard2.ConveyorBackwardKey;
                ConveyorRight = UIScannerKeyboard2.ConveyorForwardKey;
                ConveyorStop = UIScannerKeyboard2.ConveyorStopKey;

                PushLeft = UIScannerKeyboard2.PushLeft;
                PushRight = UIScannerKeyboard2.PushRight;

                Up = UIScannerKeyboard2.UpKey;
                Left = UIScannerKeyboard2.LeftKey;
                Right = UIScannerKeyboard2.RightKey;
                Down = UIScannerKeyboard2.DownKey;

                LeftTop = UIScannerKeyboard2.LeftTop;
                LeftBot = UIScannerKeyboard2.LeftBot;
                RightTop = UIScannerKeyboard2.RightTop;
                RightBot = UIScannerKeyboard2.RightBot;
            }
            else
            {
                F1 = UIScannerKeyboard1.F1Key;
                F2 = UIScannerKeyboard1.F2Key;
                F3 = UIScannerKeyboard1.F3Key;
                Inverse = UIScannerKeyboard1.InverseImageKey;
                IncreaseAbsorb = UIScannerKeyboard1.IncreaseAbsorbKey;
                DecreaseAbsorb = UIScannerKeyboard1.DecreaseAbsorbKey;
                HighPenetrate = UIScannerKeyboard1.HighPenetrateKey;
                LowPenetrate = UIScannerKeyboard1.LowPenetrateKey;
                BlackWhite = UIScannerKeyboard1.BlackWhiteKey;
                Os = UIScannerKeyboard1.OsKey;
                Ms = UIScannerKeyboard1.MsKey;
                Sen = UIScannerKeyboard1.SenKey;
                EdgeEnhance = UIScannerKeyboard1.EdgeEnhanceKey;
                PullFront = UIScannerKeyboard1.PullFrontKey;
                PullBack = UIScannerKeyboard1.PullBackKey;
                Magnify = UIScannerKeyboard1.MagnifyKey;
                Shift = UIScannerKeyboard1.ShiftKey;
                Auto = UIScannerKeyboard1.AutoKey;
                Mark = UIScannerKeyboard1.MarkKey;
                Save = UIScannerKeyboard1.SaveKey;
                ContinuousScan = UIScannerKeyboard1.ContinuousScanKey;
                Ims = UIScannerKeyboard1.ImsKey;
                Menu = UIScannerKeyboard1.MenuKey;
                Esc = UIScannerKeyboard1.EscKey;
                Z789 = UIScannerKeyboard1.Z789Key;
                VFlip = UIScannerKeyboard1.VFlipKey;
                DynamicGST = UIScannerKeyboard1.DynamicGSTKey;
                Zoom1X = UIScannerKeyboard1.Zoom1X;
                ZoomOut = UIScannerKeyboard1.ZoomOutKey;
                ZoomIn = UIScannerKeyboard1.ZoomInKey;
                ConveyorLeft = UIScannerKeyboard1.ConveyorBackwardKey;
                ConveyorRight = UIScannerKeyboard1.ConveyorForwardKey;
                ConveyorStop = UIScannerKeyboard1.ConveyorStopKey;

                PushLeft = UIScannerKeyboard1.ConveyorBackwardKey;
                PushRight = UIScannerKeyboard1.ConveyorForwardKey;

                Up = UIScannerKeyboard1.UpKey;
                Left = UIScannerKeyboard1.LeftKey;
                Right = UIScannerKeyboard1.RightKey;
                Down = UIScannerKeyboard1.DownKey;

                LeftTop = UIScannerKeyboard2.LeftTop;
                LeftBot = UIScannerKeyboard2.LeftBot;
                RightTop = UIScannerKeyboard2.RightTop;
                RightBot = UIScannerKeyboard2.RightBot;
            }
            
        }

        DateTime _lastKeyPressTime = DateTime.Now;
        public void AddKey(byte keyValue)
        {
            if (IsUSBCommonKeyboard)
            {
                if (DateTime.Now - _lastKeyPressTime < TimeSpan.FromMilliseconds(50))
                {
                    return;
                }
                UIScannerKeyboard2.Instance.AddKey(keyValue);
                _lastKeyPressTime = DateTime.Now;
            }
        }

        /// <summary>
        /// 是否是通用键盘
        /// </summary>
        public bool IsUSBCommonKeyboard { get; set; }

        /// <summary>
        /// 判断一个按键是否是功能可逆的图像特效键（按键弹起后可以恢复至按下前的特效）
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool IsReversibleImageEffectsKey(Key key)
        {
            return key == this.BlackWhite ||
                   key == HighPenetrate ||
                   //key == LowPenetrate ||
                   key == EdgeEnhance ||
                   key == this.Sen ||
                   key == this.Inverse ||
                   key == this.Os ||
                   key == this.Ms ||
                   key == this.Z789;
        }

        #region 安检键值映射

        public readonly Key F1;

        public readonly Key F2;

        public readonly Key F3;

        public readonly Key Inverse;
        public readonly Key IncreaseAbsorb;

        public readonly Key DecreaseAbsorb;
        public readonly Key HighPenetrate;
        public readonly Key BlackWhite;
        public readonly Key Os;

        public readonly Key Ms;

        public readonly Key Sen;

        public readonly Key LowPenetrate;
        public readonly Key EdgeEnhance;
        public readonly Key PullFront;

        public readonly Key PullBack;

        /// <summary>
        /// 放大镜按键
        /// </summary>
        public readonly Key Magnify;

        public readonly Key Shift;

        public readonly Key Auto;
        public readonly Key Mark;

        public readonly Key Save;

        /// <summary>
        /// Continuous scan key. 
        /// </summary>
        public readonly Key ContinuousScan;

        public readonly Key Ims;

        public readonly Key Menu;
        public readonly Key Esc;
        public readonly Key Z789;

        /// <summary>
        /// 垂直翻转图像
        /// </summary>
        public readonly Key VFlip;

        /// <summary>
        /// Dynamic Gray-Scale Transformation
        /// 动态灰度扫描
        /// </summary>
        public readonly Key DynamicGST;

        public readonly Key Zoom1X;
        // 缩小
        public readonly Key ZoomOut;
        // 放大
        public readonly Key ZoomIn;
        public readonly Key ConveyorLeft;
        public readonly Key ConveyorStop;
        public readonly Key ConveyorRight;

        public readonly Key PushLeft;
        public readonly Key PushRight;

        public readonly Key Up;
        public readonly Key Down;
        public readonly Key Left;

        public readonly Key Right;

        public readonly Key LeftTop;

        public readonly Key LeftBot;

        public readonly Key RightTop;

        public readonly Key RightBot;

        /// <summary>
        /// 系统关机键。
        /// </summary>
        public readonly Key Shutdown;

        #endregion 安检键值映射

        public bool IsKeySwitchLedOn
        {
            get { return UIScannerKeyboard1.IsKeySwitchIndicatorOn; }
            set { UIScannerKeyboard1.IsKeySwitchIndicatorOn = value; }
        }

        public bool IsPowerLedOn
        {
            get { return UIScannerKeyboard1.IsPowerIndicatorOn; }
            set { UIScannerKeyboard1.IsPowerIndicatorOn = value; }
        }

        public bool IsXRayGen1LedOn
        {
            get { return UIScannerKeyboard1.IsXRayIndicator1On; }
            set { UIScannerKeyboard1.IsXRayIndicator1On = value; }
        }

        public bool IsXRayGen2LedOn
        {
            get { return UIScannerKeyboard1.IsXRayIndicator2On; }
            set { UIScannerKeyboard1.IsXRayIndicator2On = value; }
        }

        public bool HasBeeper
        {
            get { return true; }
        }

        public bool IsAlive
        {
            get
            {
                if (IsUSBCommonKeyboard)
                {
                    return true;
                }
                return UIScannerKeyboard1.IsAlive;
            }
        }

        public bool StartBeep(TimeSpan pulseWidth, TimeSpan interval, int times)
        {
            if (HasBeeper)
            {
                UIScannerKeyboard1.StartBeep(pulseWidth, interval, times);
                return true;
            }

            return false;
        }

        public void StopBeep()
        {
            UIScannerKeyboard1.StopBeep();
        }

        public bool Open()
        {
            if (IsUSBCommonKeyboard)
            {
                return UIScannerKeyboard2.Instance.Open();
            }
            return UIScannerKeyboard1.Open();
        }

        public void Close()
        {
            if (IsUSBCommonKeyboard)
            {
                UIScannerKeyboard2.Instance.Close();
            }
            UIScannerKeyboard1.Close();
        }
    }
}
