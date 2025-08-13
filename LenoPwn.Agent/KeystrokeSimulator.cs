using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace LenoPwn.Agent
{
    public static class KeystrokeSimulator
    {
        #region WinAPI Structures and Enums

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint Type;
            public InputUnion Data;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        #endregion

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        public static void Send(List<string> modifiers, string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            var modifierKeys = modifiers?
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Select(m => GetVirtualKeyCode(m))
                .Where(vk => vk != 0)
                .ToList() ?? new List<ushort>();

            var mainKey = GetVirtualKeyCode(key);
            if (mainKey == 0)
                return;

            var inputs = new List<INPUT>();

            foreach (var modifierKey in modifierKeys)
            {
                inputs.Add(CreateKeyInput(modifierKey, false));
            }

            inputs.Add(CreateKeyInput(mainKey, false));
            inputs.Add(CreateKeyInput(mainKey, true));

            for (int i = modifierKeys.Count - 1; i >= 0; i--)
            {
                inputs.Add(CreateKeyInput(modifierKeys[i], true));
            }

            if (inputs.Count > 0)
            {
                SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf(typeof(INPUT)));
            }
        }

        private static INPUT CreateKeyInput(ushort virtualKeyCode, bool keyUp)
        {
            uint flags = 0;
            if (keyUp)
            {
                flags |= KEYEVENTF_KEYUP;
            }

            return new INPUT
            {
                Type = INPUT_KEYBOARD,
                Data = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = virtualKeyCode,
                        wScan = 0,
                        dwFlags = flags,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
        }

        private static ushort GetVirtualKeyCode(string key)
        {
            if (string.IsNullOrEmpty(key))
                return 0;

            key = key.ToLowerInvariant().Trim();

            if (key.Length == 1)
            {
                char c = key[0];
                if (char.IsLetter(c))
                {
                    return (ushort)char.ToUpper(c);
                }
                if (char.IsDigit(c))
                {
                    return (ushort)c;
                }
            }

            return key switch
            {
                "control" or "ctrl" => 0x11, // VK_CONTROL
                "alt" => 0x12,                // VK_MENU
                "shift" => 0x10,              // VK_SHIFT
                "win" or "windows" => 0x5B,   // VK_LWIN
                "enter" or "return" => 0x0D,  // VK_RETURN
                "tab" => 0x09,                // VK_TAB
                "escape" or "esc" => 0x1B,    // VK_ESCAPE
                "space" => 0x20,              // VK_SPACE
                "backspace" => 0x08,          // VK_BACK
                "delete" or "del" => 0x2E,    // VK_DELETE
                "home" => 0x24,               // VK_HOME
                "end" => 0x23,                // VK_END
                "pageup" or "pgup" => 0x21,   // VK_PRIOR
                "pagedown" or "pgdn" => 0x22, // VK_NEXT
                "insert" or "ins" => 0x2D,    // VK_INSERT
                "f1" => 0x70,
                "f2" => 0x71,
                "f3" => 0x72,
                "f4" => 0x73,
                "f5" => 0x74,
                "f6" => 0x75,
                "f7" => 0x76,
                "f8" => 0x77,
                "f9" => 0x78,
                "f10" => 0x79,
                "f11" => 0x7A,
                "f12" => 0x7B,
                "up" => 0x26,
                "down" => 0x28,
                "left" => 0x25,
                "right" => 0x27,
                _ => 0
            };
        }
    }
}