using System;
using System.Runtime.InteropServices;

namespace RemoteDesktop.Server.Input
{
    /// <summary>
    /// Giả lập thao tác bàn phím trên máy đích bằng Win32 API (keybd_event).
    /// keyCode là mã phím ảo (Virtual-Key Code) giống với WinForms KeyValue.
    /// </summary>
    public static class KeyboardController
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        public static void KeyDown(int keyCode)
        {
            if (keyCode < 0 || keyCode > 255) return;
            keybd_event((byte)keyCode, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
        }

        public static void KeyUp(int keyCode)
        {
            if (keyCode < 0 || keyCode > 255) return;
            keybd_event((byte)keyCode, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
    }
}
