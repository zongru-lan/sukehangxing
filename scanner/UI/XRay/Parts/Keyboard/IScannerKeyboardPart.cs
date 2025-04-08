using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace UI.XRay.Parts.Keyboard
{
    /// <summary>
    /// 通用键盘接口组件，适用于安检、食品检测等
    /// </summary>
    public interface IScannerKeyboardPart
    {
        /// <summary>
        /// Get or set key switch led on keyboard
        /// </summary>
        bool IsKeySwitchLedOn { get; set; }

        /// <summary>
        /// Get or set power led on keyboard
        /// </summary>
        bool IsPowerLedOn { get; set; }

        /// <summary>
        /// get or set x-ray gen1 led on keyboard
        /// </summary>
        bool IsXRayGen1LedOn { get; set; }

        /// <summary>
        /// get or set x-ray gen2 led on keyboard
        /// </summary>
        bool IsXRayGen2LedOn { get; set; }

        /// <summary>
        /// 键盘是否包含音频警报器
        /// </summary>
        bool HasBeeper { get; }

        /// <summary>
        /// 按指定的时间间隔和次数进行蜂鸣告警
        /// </summary>
        /// <param name="pulseWidth"></param>
        /// <param name="interval"></param>
        /// <param name="times"></param>
        /// <returns>true if supports, or false if not supported</returns>
        bool StartBeep(TimeSpan pulseWidth, TimeSpan interval, int times);

        /// <summary>
        /// Stop beeping
        /// </summary>
        void StopBeep();

        /// <summary>
        /// Open Keyboard.
        /// </summary>
        /// <returns></returns>
        bool Open();

        /// <summary>
        /// Close Keyboard.
        /// </summary>
        void Close();
    }
}
