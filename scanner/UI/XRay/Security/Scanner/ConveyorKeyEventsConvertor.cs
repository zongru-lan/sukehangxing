using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using UI.XRay.Parts.Keyboard;

namespace UI.XRay.Security.Scanner
{
    /// <summary>
    /// 电机按键事件转换：用于在设置窗口、登录窗口等弹出式窗口中，将电机按键虚拟化为tab键、退格键、空格键等，以用于操作用户界面元素
    /// </summary>
    public static class ConveyorKeyEventsConvertor
    {
        public static bool HandlePreviewKeyDown(KeyEventArgs args)
        {
            switch (args.Key)
            {
                // 输送机后退键，模拟为 Backspace
                case Key.F4:
                    if ((args.KeyboardDevice.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift)
                    {
                        SendKeyEvents.Type(Key.Back);
                        args.Handled = true;
                        return true;
                    }
                    break;

                // 输送机前进键，模拟为Tab
                case Key.F6:
                    if ((args.KeyboardDevice.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift)
                    {
                        SendKeyEvents.Type(Key.Tab);
                        args.Handled = true;
                        return true;
                    }
                    break;

                // 电机停止键，模拟为空格键，用于选中
                case Key.F5:
                    if ((args.KeyboardDevice.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift)
                    {
                        SendKeyEvents.Press(Key.Space);
                        args.Handled = true;
                        return true;
                    }
                    break;
            }

            return false;
        }

        public static bool HandlePreviewKeyUp(KeyEventArgs args)
        {
            // 由于F5模拟为空格键，用于模拟选中事件，可能导致窗口跳转，必须使用Preview事件
            if (args.Key == Key.F5)
            {
                SendKeyEvents.Release(Key.Space);
                args.Handled = true;
            }

            return false;
        }
    }
}
