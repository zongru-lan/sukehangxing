﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Windows.Input;

namespace UI.XRay.Parts.Keyboard
{

    /// <summary>
    /// Exposes a simple interface to common keyboard operations, allowing the user to simulate keyboard input.
    /// </summary>
    /// <example>
    /// The following code types "Hello world" with the specified casing,
    /// and then types "hello, capitalized world" which will be in all caps because
    /// the left shift key is being held down.
    /// <code>
    /**
            SendKeys.Type("Hello world");
            SendKeys.Press(Key.LeftShift);
            SendKeys.Type("hello, capitalized world");
            SendKeys.Release(Key.LeftShift);
    */
    /// </code>
    /// </example>
    public static class SendKeyEvents
    {
        #region Public Members

        /// <summary>
        /// Presses down a key.
        /// </summary>
        /// <param name="key">The key to press.</param>
        public static void Press(Key key)
        {
            SendKeyboardInput(key, true);
        }

        /// <summary>
        /// Releases a key.
        /// </summary>
        /// <param name="key">The key to release.</param>
        public static void Release(Key key)
        {
            SendKeyboardInput(key, false);
        }

        /// <summary>
        /// Performs a press-and-release operation for the specified key, which is effectively equivallent to typing.
        /// </summary>
        /// <param name="key">The key to press.</param>
        public static void Type(Key key)
        {
            Press(key);
            Release(key);
        }

        #endregion

         /// <summary>
        /// Resets the system keyboard to a clean state.
        /// </summary>
        public static void Reset()
        {
            foreach (Key key in Enum.GetValues(typeof(Key)))
            {
                if (key != Key.None && (System.Windows.Input.Keyboard.GetKeyStates(key) & KeyStates.Down) > 0)
                {
                    Release(key);
                }
            }
        }

        /// <summary>
        /// Types the specified text.
        /// </summary>
        /// <param name="text">The text to type.</param>
        public static void Type(string text)
        {
            foreach (char c in text)
            {
                // We get the vKey value for the character via a Win32 API. We then use bit masks to pull the
                // upper and lower bytes to get the shift state and key information. We then use WPF KeyInterop
                // to go from the vKey key info into a System.Windows.Input.Key data structure. This work is
                // necessary because Key doesn't distinguish between upper and lower case, so we have to wrap
                // the key type inside a shift press/release if necessary.
                int vKeyValue = NativeMethodsAdapter.VkKeyScan(c);
                bool keyIsShifted = (vKeyValue & NativeMethodsAdapter.VKeyShiftMask) == NativeMethodsAdapter.VKeyShiftMask;
                Key key = KeyInterop.KeyFromVirtualKey(vKeyValue & NativeMethodsAdapter.VKeyCharMask);

                if (keyIsShifted)
                {
                    Type(key, new Key[] { Key.LeftShift });
                }
                else
                {
                    Type(key);
                }
            }
        }

        /// <summary>
        /// Types a key while a set of modifier keys are being pressed. Modifer keys
        /// are pressed in the order specified and released in reverse order.
        /// </summary>
        /// <param name="key">Key to type.</param>
        /// <param name="modifierKeys">Set of keys to hold down with key is typed.</param>
        private static void Type(Key key, Key[] modifierKeys)
        {
            foreach (Key modiferKey in modifierKeys)
            {
                Press(modiferKey);
            }

            Type(key);

            foreach (Key modifierKey in modifierKeys.Reverse())
            {
                Release(modifierKey);
            }
        }


        #region Private Members

        /// <summary>
        /// Injects keyboard input into the system.
        /// </summary>
        /// <param name="key">Indicates the key pressed or released. Can be one of the constants defined in the Key enum.</param>
        /// <param name="press">True to inject a key press, false to inject a key release.</param>
        [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
        private static void SendKeyboardInput(Key key, bool press)
        {
            PermissionSet permissions = new PermissionSet(PermissionState.Unrestricted);
            permissions.Demand();

            NativeMethodsAdapter.INPUT ki = new NativeMethodsAdapter.INPUT();
            ki.type = NativeMethodsAdapter.InputKeyboard;
            ki.union.keyboardInput.wVk = (short)KeyInterop.VirtualKeyFromKey(key);
            ki.union.keyboardInput.wScan = (short)NativeMethodsAdapter.MapVirtualKey(ki.union.keyboardInput.wVk, 0);

            int dwFlags = 0;

            // todo 此处暂时不明白什么作用，但是会影响到NumLock的使用（无法控制），因此暂时注掉
            //if (ki.union.keyboardInput.wScan > 0)
            //{
            //    dwFlags |= NativeMethodsAdapter.KeyeventfScancode;
            //}

            if (!press)
            {
                dwFlags |= NativeMethodsAdapter.KeyeventfKeyup;
            }

            ki.union.keyboardInput.dwFlags = dwFlags;

            if (ExtendedKeys.Contains(key))
            {
                ki.union.keyboardInput.dwFlags |= NativeMethodsAdapter.KeyeventfExtendedkey;
            }

            ki.union.keyboardInput.time = 0;
            ki.union.keyboardInput.dwExtraInfo = new IntPtr(0);

            if (NativeMethodsAdapter.SendInput(1, ref ki, Marshal.SizeOf(ki)) == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        // From the SDK:
        // The extended-key flag indicates whether the keystroke message originated from one of
        // the additional keys on the enhanced keyboard. The extended keys consist of the ALT and
        // CTRL keys on the right-hand side of the keyboard; the INS, DEL, HOME, END, PAGE UP,
        // PAGE DOWN, and arrow keys in the clusters to the left of the numeric keypad; the NUM LOCK
        // key; the BREAK (CTRL+PAUSE) key; the PRINT SCRN key; and the divide (/) and ENTER keys in
        // the numeric keypad. The extended-key flag is set if the key is an extended key. 
        //
        // - docs appear to be incorrect. Use of Spy++ indicates that break is not an extended key.
        // Also, menu key and windows keys also appear to be extended.
        private static readonly Key[] ExtendedKeys = new Key[] { 
                                                                   Key.RightAlt,
                                                                   Key.RightCtrl,
                                                                   Key.NumLock,
                                                                   Key.Insert,
                                                                   Key.Delete,
                                                                   Key.Home,
                                                                   Key.End,
                                                                   Key.Prior,
                                                                   Key.Next,
                                                                   Key.Up,
                                                                   Key.Down,
                                                                   Key.Left,
                                                                   Key.Right,
                                                                   Key.Apps,
                                                                   Key.RWin,
                                                                   Key.LWin };
        // Note that there are no distinct values for the following keys:
        // numpad divide
        // numpad enter

        #endregion
    }
}
