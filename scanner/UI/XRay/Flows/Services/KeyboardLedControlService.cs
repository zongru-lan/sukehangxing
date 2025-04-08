using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UI.XRay.Control;
using UI.XRay.ControlWorkflows;
using UI.XRay.Parts.Keyboard;

namespace UI.XRay.Flows.Services
{
    /// <summary>
    /// 键盘指示灯控制服务：根据射线、开关状态，及时更新键盘指示灯
    /// </summary>
    public class KeyboardLedControlService
    {
        public static KeyboardLedControlService Service { get; private set; }

        static KeyboardLedControlService()
        {
            Service = new KeyboardLedControlService();
        }

        protected KeyboardLedControlService()
        {
            ScannerKeyboardPart.Keyboard.IsKeySwitchLedOn = true;
        }

        /// <summary>
        /// 启动键盘指示灯控制服务
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void StartService()
        {
            ControlService.ServicePart.ScannerWorkingStatesUpdated += ServicePartOnXRayStateChanged;
            ControlService.ServicePart.SwitchStateChanged += ServicePartOnSwitchStateChanged;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void StopService()
        {
            ControlService.ServicePart.ScannerWorkingStatesUpdated -= ServicePartOnXRayStateChanged;
            ControlService.ServicePart.SwitchStateChanged -= ServicePartOnSwitchStateChanged;
        }

        private void ServicePartOnSwitchStateChanged(object sender, SwitchStateChangedEventArgs switchStateChangedEventArgs)
        {
            if (switchStateChangedEventArgs.Switch == CtrlSysSwitch.PowerSwitch)
            {
                ScannerKeyboardPart.Keyboard.IsKeySwitchLedOn = switchStateChangedEventArgs.New;
            }
        }

        private void ServicePartOnXRayStateChanged(object sender, ScannerWorkingStates scanWorkingStates)
        {
            ScannerKeyboardPart.Keyboard.IsXRayGen1LedOn = scanWorkingStates.IsXRayGen1Radiating;
            ScannerKeyboardPart.Keyboard.IsXRayGen2LedOn = scanWorkingStates.IsXRayGen2Radiating;
        }
    }
}
