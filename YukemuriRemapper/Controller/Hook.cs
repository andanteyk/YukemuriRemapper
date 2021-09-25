using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using YukemuriRemapper.Model;

namespace YukemuriRemapper.Controller
{
    public sealed class Hook : IDisposable
    {
        private IntPtr HookId;
        private Configuration Settings;
        private HookProc HookProcInstance;
        private Process TargetProcess;


        public Hook() { }
        public Hook(Configuration settings, Process targetProcess) => HookKey(settings, targetProcess);



        public void HookKey(Configuration settings, Process targetProcess)
        {
            if (HookId != default)
                return;

            Settings = settings;
            TargetProcess = targetProcess;
            if (TargetProcess == null)
                return;     // disabled

            HookProcInstance = HookProcedure;          // to avoid GC
            HookId = SetWindowsHookEx(WH_KEYBOARD_LL, HookProcInstance, GetModuleHandle(TargetProcess.MainModule.ModuleName), 0);
        }


        public void UnhookKey()
        {
            if (HookId == default)
                return;

            UnhookWindowsHookEx(HookId);
            HookId = default;
        }

        private IntPtr HookProcedure(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code < 0)       // maybe error; do nothing
            {
                return CallNextHookEx(HookId, code, wParam, lParam);
            }


            {       // focused?
                if (TargetProcess.HasExited)
                {
                    UnhookKey();
                    return CallNextHookEx(HookId, code, wParam, lParam);
                }

                GetWindowThreadProcessId(GetForegroundWindow(), out var procId);
                if (procId != TargetProcess.Id)
                {
                    return CallNextHookEx(HookId, code, wParam, lParam);
                }
            }

            switch ((WM)wParam)
            {
                case WM.KEYDOWN or WM.SYSKEYDOWN:
                    {
                        var keyboard = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

                        if (keyboard.dwExtraInfo == KEYBDINPUT.EXTRA_INFO_TAG)
                        {
                            return CallNextHookEx(HookId, code, wParam, lParam);
                        }


                        bool done = false;
                        foreach (var bind in Settings.KeyBinds)
                        {
                            if (bind.From != keyboard.vkCode)
                                continue;

                            SendKeyDown(bind.To, IsExtendedKey(bind.To));
                            done = true;
                        }

                        if (done)
                            return new IntPtr(1);       // discard original input
                    }
                    break;

                case WM.KEYUP or WM.SYSKEYUP:
                    {
                        var keyboard = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

                        if (keyboard.dwExtraInfo == KEYBDINPUT.EXTRA_INFO_TAG)
                        {
                            return CallNextHookEx(HookId, code, wParam, lParam);
                        }


                        bool done = false;
                        foreach (var bind in Settings.KeyBinds)
                        {
                            if (bind.From != keyboard.vkCode)
                                continue;

                            SendKeyUp(bind.To, IsExtendedKey(bind.To));
                            done = true;
                        }

                        if (done)
                            return new IntPtr(1);       // discard original input
                    }
                    break;
            }

            return CallNextHookEx(HookId, code, wParam, lParam);
        }


        // see https://docs.microsoft.com/en-us/windows/win32/inputdev/about-keyboard-input#extended-key-flag to specify isExtended
        private static void SendKeyDown(int code, bool isExtended)
        {
            var input = new[] { new INPUT {
                type = INPUT.INPUT_KEYBOARD,
                U = new InputUnion{
                    ki = new  KEYBDINPUT
                    {
                        wVk = (short)code,
                        wScan = (short)MapVirtualKey((uint)code, 0),        // arg2 means 0=key2scancode, 1=scancode2key
                        dwFlags = KEYEVENTF.KEYDOWN | (isExtended ? KEYEVENTF.EXTENDEDKEY : 0),
                        time = 0,
                        dwExtraInfo = KEYBDINPUT.EXTRA_INFO_TAG
                    }
                }
            } };
            _ = SendInput(1, input, Marshal.SizeOf(input[0]) * input.Length);
        }

        private static void SendKeyUp(int code, bool isExtended)
        {
            var input = new[] { new INPUT {
                type = INPUT.INPUT_KEYBOARD,
                U = new InputUnion{
                    ki = new  KEYBDINPUT
                    {
                        wVk = (short)code,
                        wScan = (short)MapVirtualKey((uint)code, 0),
                         dwFlags = KEYEVENTF.KEYUP | (isExtended ? KEYEVENTF.EXTENDEDKEY : 0),
                        time = 0,
                        dwExtraInfo = KEYBDINPUT.EXTRA_INFO_TAG
                    }
                }
            } };
            _ = SendInput(1, input, Marshal.SizeOf(input[0]) * input.Length);
        }


        /// <summary>
        /// https://docs.microsoft.com/en-us/windows/win32/inputdev/about-keyboard-input#extended-key-flag
        /// </summary>
        private static bool IsExtendedKey(int keyCode)
        {
            return (Keys)keyCode switch
            {
                Keys.RMenu => true,
                Keys.RControlKey => true,
                Keys.Insert => true,
                Keys.Delete => true,
                Keys.Home => true,
                Keys.End => true,
                Keys.PageUp => true,
                Keys.PageDown => true,
                Keys.Up => true,
                Keys.Down => true,
                Keys.Left => true,
                Keys.Right => true,
                Keys.NumLock => true,
                Keys.Control | Keys.Pause => true,      // break key
                Keys.PrintScreen => true,
                Keys.Divide => true,
                // Keys.Return => true,         // TODO: numpad enter should be true, but may not distinguish with normal enter

                _ => false
            };
        }


        public void Dispose()
        {
            UnhookKey();
            TargetProcess?.Dispose();
        }




        #region extern

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetWindowsHookEx(int hookType, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

        const int WH_KEYBOARD_LL = 13;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr GetModuleHandle(string lpModuleName);


        [DllImport("user32.dll", SetLastError = true)]
        static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        public enum WM
        {
            KEYDOWN = 0x100,
            SYSKEYDOWN = 0x104,
            KEYUP = 0x101,
            SYSKEYUP = 0x105,
        }

        [StructLayout(LayoutKind.Sequential)]
        class KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public KBDLLHOOKSTRUCTFlags flags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [Flags]
        enum KBDLLHOOKSTRUCTFlags : uint
        {
            LLKHF_EXTENDED = 0x01,
            LLKHF_INJECTED = 0x10,
            LLKHF_ALTDOWN = 0x20,
            LLKHF_UP = 0x80,
        }

        [DllImport("user32.dll")]
        static extern uint SendInput(uint nInputs,
           [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs,
           int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            internal uint type;
            internal InputUnion U;
            internal static int Size
            {
                get { return Marshal.SizeOf(typeof(INPUT)); }
            }

            public const int INPUT_MOUSE = 0;
            public const int INPUT_KEYBOARD = 1;
            public const int INPUT_HARDWARE = 2;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct InputUnion
        {
            [FieldOffset(0)]
            internal MOUSEINPUT mi;
            [FieldOffset(0)]
            internal KEYBDINPUT ki;
            [FieldOffset(0)]
            internal HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            internal int dx;
            internal int dy;
            internal int mouseData;
            internal int dwFlags;
            internal uint time;
            internal UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            internal short wVk;         // virtual keycode
            internal short wScan;       // scancode
            internal KEYEVENTF dwFlags;
            internal int time;
            internal UIntPtr dwExtraInfo;

            /// <summary>
            /// user-defined ID (any value except 0. this is used to identify source of keyboard events. if dwExtraInfo is 0, it comes from physical keyboard.)
            /// </summary>
            internal const nuint EXTRA_INFO_TAG = 0x2463;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct HARDWAREINPUT
        {
            internal int uMsg;
            internal short wParamL;
            internal short wParamH;
        }

        [Flags]
        internal enum KEYEVENTF : uint
        {
            KEYDOWN = 0x0000,
            EXTENDEDKEY = 0x0001,
            KEYUP = 0x0002,
            SCANCODE = 0x0008,
            UNICODE = 0x0004
        }

        [DllImport("user32.dll")]
        static extern uint MapVirtualKey(uint uCode, uint uMapType);
        #endregion
    }
}
