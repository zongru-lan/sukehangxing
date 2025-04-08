using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Input;

namespace UI.XRay.Parts.Keyboard
{
    /// <summary>
    /// Exposes a simple interface to common mouse operations, allowing the user to simulate mouse input.
    /// </summary>
    /// <example>The following code moves to screen coordinate 100,100 and left clicks.
    /// <code>
    /**
        MouseEventsSender.MoveTo(new Point(100, 100));
        MouseEventsSender.Click(MouseButton.Left);
    */
    /// </code>
    /// </example>
    public static class SendMouseEvents
    {
        /// <summary>
        /// Clicks a mouse button.
        /// </summary>
        /// <param name="mouseButton">The mouse button to click.</param>
        public static void Click(MouseButton mouseButton)
        {
            Down(mouseButton);
            Up(mouseButton);
        }

        /// <summary>
        /// Double-clicks a mouse button.
        /// </summary>
        /// <param name="mouseButton">The mouse button to click.</param>
        public static void DoubleClick(MouseButton mouseButton)
        {
            Click(mouseButton);
            Click(mouseButton);
        }

        /// <summary>
        /// Performs a mouse-down operation for a specified mouse button.
        /// </summary>
        /// <param name="mouseButton">The mouse button to use.</param>
        public static void Down(MouseButton mouseButton)
        {
            switch (mouseButton)
            {
                case MouseButton.Left:
                    SendMouseInput(0, 0, 0, NativeMethodsAdapter.SendMouseInputFlags.LeftDown);
                    break;
                case MouseButton.Right:
                    SendMouseInput(0, 0, 0, NativeMethodsAdapter.SendMouseInputFlags.RightDown);
                    break;
                case MouseButton.Middle:
                    SendMouseInput(0, 0, 0, NativeMethodsAdapter.SendMouseInputFlags.MiddleDown);
                    break;
                case MouseButton.XButton1:
                    SendMouseInput(0, 0, NativeMethodsAdapter.XButton1, NativeMethodsAdapter.SendMouseInputFlags.XDown);
                    break;
                case MouseButton.XButton2:
                    SendMouseInput(0, 0, NativeMethodsAdapter.XButton2, NativeMethodsAdapter.SendMouseInputFlags.XDown);
                    break;
                default:
                    throw new InvalidOperationException("Unsupported MouseButton input.");
            }
        }

        /// <summary>
        /// Moves the mouse pointer to the specified screen coordinates.
        /// </summary>
        /// <param name="point">The screen coordinates to move to.</param>
        public static void MoveTo(Point point)
        {
            SendMouseInput((int)point.X, (int)point.Y, 0, NativeMethodsAdapter.SendMouseInputFlags.Move | NativeMethodsAdapter.SendMouseInputFlags.Absolute);
        }

        /// <summary>
        /// Resets the system mouse to a clean state.
        /// </summary>
        public static void Reset()
        {
            MoveTo(new Point(0, 0));

            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                SendMouseInput(0, 0, 0, NativeMethodsAdapter.SendMouseInputFlags.LeftUp);
            }

            if (Mouse.MiddleButton == MouseButtonState.Pressed)
            {
                SendMouseInput(0, 0, 0, NativeMethodsAdapter.SendMouseInputFlags.MiddleUp);
            }

            if (Mouse.RightButton == MouseButtonState.Pressed)
            {
                SendMouseInput(0, 0, 0, NativeMethodsAdapter.SendMouseInputFlags.RightUp);
            }

            if (Mouse.XButton1 == MouseButtonState.Pressed)
            {
                SendMouseInput(0, 0, NativeMethodsAdapter.XButton1, NativeMethodsAdapter.SendMouseInputFlags.XUp);
            }

            if (Mouse.XButton2 == MouseButtonState.Pressed)
            {
                SendMouseInput(0, 0, NativeMethodsAdapter.XButton2, NativeMethodsAdapter.SendMouseInputFlags.XUp);
            }
        }

        /// <summary>
        /// Simulates scrolling of the mouse wheel up or down.
        /// </summary>
        /// <param name="lines">The number of lines to scroll. Use positive numbers to scroll up and negative numbers to scroll down.</param>
        public static void Scroll(double lines)
        {
            int amount = (int)(NativeMethodsAdapter.WheelDelta * lines);

            SendMouseInput(0, 0, amount, NativeMethodsAdapter.SendMouseInputFlags.Wheel);
        }

        /// <summary>
        /// Performs a mouse-up operation for a specified mouse button.
        /// </summary>
        /// <param name="mouseButton">The mouse button to use.</param>
        public static void Up(MouseButton mouseButton)
        {
            switch (mouseButton)
            {
                case MouseButton.Left:
                    SendMouseInput(0, 0, 0, NativeMethodsAdapter.SendMouseInputFlags.LeftUp);
                    break;
                case MouseButton.Right:
                    SendMouseInput(0, 0, 0, NativeMethodsAdapter.SendMouseInputFlags.RightUp);
                    break;
                case MouseButton.Middle:
                    SendMouseInput(0, 0, 0, NativeMethodsAdapter.SendMouseInputFlags.MiddleUp);
                    break;
                case MouseButton.XButton1:
                    SendMouseInput(0, 0, NativeMethodsAdapter.XButton1, NativeMethodsAdapter.SendMouseInputFlags.XUp);
                    break;
                case MouseButton.XButton2:
                    SendMouseInput(0, 0, NativeMethodsAdapter.XButton2, NativeMethodsAdapter.SendMouseInputFlags.XUp);
                    break;
                default:
                    throw new InvalidOperationException("Unsupported MouseButton input.");
            }
        }

        /// <summary>
        /// Sends mouse input.
        /// </summary>
        /// <param name="x">x coordinate</param>
        /// <param name="y">y coordinate</param>
        /// <param name="data">scroll wheel amount</param>
        /// <param name="flags">SendMouseInputFlags flags</param>
        [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
        private static void SendMouseInput(int x, int y, int data, NativeMethodsAdapter.SendMouseInputFlags flags)
        {
            PermissionSet permissions = new PermissionSet(PermissionState.Unrestricted);
            permissions.Demand();

            int intflags = (int)flags;

            if ((intflags & (int)NativeMethodsAdapter.SendMouseInputFlags.Absolute) != 0)
            {
                // Absolute position requires normalized coordinates.
                NormalizeCoordinates(ref x, ref y);
                intflags |= NativeMethodsAdapter.MouseeventfVirtualdesk;
            }

            NativeMethodsAdapter.INPUT mi = new NativeMethodsAdapter.INPUT();
            mi.type = NativeMethodsAdapter.InputMouse;
            mi.union.mouseInput.dx = x;
            mi.union.mouseInput.dy = y;
            mi.union.mouseInput.mouseData = data;
            mi.union.mouseInput.dwFlags = intflags;
            mi.union.mouseInput.time = 0;
            mi.union.mouseInput.dwExtraInfo = new IntPtr(0);

            if (NativeMethodsAdapter.SendInput(1, ref mi, Marshal.SizeOf(mi)) == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        private static void NormalizeCoordinates(ref int x, ref int y)
        {
            int vScreenWidth = NativeMethodsAdapter.GetSystemMetrics(NativeMethodsAdapter.SMCxvirtualscreen);
            int vScreenHeight = NativeMethodsAdapter.GetSystemMetrics(NativeMethodsAdapter.SMCyvirtualscreen);
            int vScreenLeft = NativeMethodsAdapter.GetSystemMetrics(NativeMethodsAdapter.SMXvirtualscreen);
            int vScreenTop = NativeMethodsAdapter.GetSystemMetrics(NativeMethodsAdapter.SMYvirtualscreen);

            // Absolute input requires that input is in 'normalized' coords - with the entire
            // desktop being (0,0)...(65536,65536). Need to convert input x,y coords to this
            // first.
            //
            // In this normalized world, any pixel on the screen corresponds to a block of values
            // of normalized coords - eg. on a 1024x768 screen,
            // y pixel 0 corresponds to range 0 to 85.333,
            // y pixel 1 corresponds to range 85.333 to 170.666,
            // y pixel 2 correpsonds to range 170.666 to 256 - and so on.
            // Doing basic scaling math - (x-top)*65536/Width - gets us the start of the range.
            // However, because int math is used, this can end up being rounded into the wrong
            // pixel. For example, if we wanted pixel 1, we'd get 85.333, but that comes out as
            // 85 as an int, which falls into pixel 0's range - and that's where the pointer goes.
            // To avoid this, we add on half-a-"screen pixel"'s worth of normalized coords - to
            // push us into the middle of any given pixel's range - that's the 65536/(Width*2)
            // part of the formula. So now pixel 1 maps to 85+42 = 127 - which is comfortably
            // in the middle of that pixel's block.
            // The key ting here is that unlike points in coordinate geometry, pixels take up
            // space, so are often better treated like rectangles - and if you want to target
            // a particular pixel, target its rectangle's midpoint, not its edge.
            x = ((x - vScreenLeft) * 65536) / vScreenWidth + 65536 / (vScreenWidth * 2);
            y = ((y - vScreenTop) * 65536) / vScreenHeight + 65536 / (vScreenHeight * 2);
        }
    }

}
