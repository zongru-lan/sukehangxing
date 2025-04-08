using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace UI.XRay.Parts.Keyboard
{
    /// <summary>
    /// 安检专用键盘组件接口
    /// </summary>
    public interface ISecurityScannerKeyboardPart : IScannerKeyboardPart
    {
        Key F1Key { get; }
        Key F2Key { get; }
        Key F3Key { get; }
        Key InverseImageKey { get; }
        Key IncreaseAbsorbKey { get; }
        Key DecreaseAbsorbKey { get; }
        Key HighPenetrateKey { get; }
        Key BlackWhiteKey { get; }
        Key OsKey { get; }
        Key MsKey { get; }
        Key SenKey { get; }
        Key LowPenetrateKey { get; }
        Key EdgeEnhanceKey { get; }
        Key PullFrontKey { get; }
        Key PullBackKey { get; }

        /// <summary>
        /// 放大镜按键
        /// </summary>
        Key MagnifyKey { get; }

        Key ShiftKey { get; }
        Key AutoKey { get; }
        Key MarkKey { get; }
        Key SaveKey { get; }

        /// <summary>
        /// Continuous scan key. 
        /// </summary>
        Key ContinuousScanKey { get; }

        Key ImsKey { get; }
        Key MenuKey { get; }
        Key EscKey { get; }
        Key Z789Key { get; }

        /// <summary>
        /// 垂直翻转图像
        /// </summary>
        Key VFlipKey { get; }

        /// <summary>
        /// Dynamic Gray-Scale Transformation
        /// 动态灰度扫描
        /// </summary>
        Key DynamicGSTKey { get; }

        Key Zoom1X { get; }
        Key ZoomOutKey { get; }
        Key ZoomInKey { get; }
        Key ConveyorBackwardKey { get; }
        Key ConveyorStopKey { get; }
        Key ConveyorForwardKey { get; }
        Key UpKey { get; }
        Key DownKey { get; }
        Key LeftKey { get; }
        Key RightKey { get; }

        /// <summary>
        /// 系统关机键。
        /// </summary>
        Key ShutdownKey { get; }
    }
}
