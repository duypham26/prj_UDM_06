using System;
using System.Runtime.InteropServices;

namespace RemoteDesktop.Server.Input
{
    /// <summary>
    /// Giả lập thao tác chuột trên máy đích bằng Win32 API (SetCursorPos + mouse_event).
    /// </summary>
    public static class MouseController
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;

        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        public static void Move(int x, int y)
        {
            SetCursorPos(x, y);
        }

        public static void Down(RemoteDesktop.Shared.Models.MouseButton button)
        {
            switch (button)
            {
                case RemoteDesktop.Shared.Models.MouseButton.Left:
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                    break;
                case RemoteDesktop.Shared.Models.MouseButton.Right:
                    mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
                    break;
                case RemoteDesktop.Shared.Models.MouseButton.Middle:
                    mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, UIntPtr.Zero);
                    break;
            }
        }

        public static void Up(RemoteDesktop.Shared.Models.MouseButton button)
        {
            switch (button)
            {
                case RemoteDesktop.Shared.Models.MouseButton.Left:
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                    break;
                case RemoteDesktop.Shared.Models.MouseButton.Right:
                    mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
                    break;
                case RemoteDesktop.Shared.Models.MouseButton.Middle:
                    mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, UIntPtr.Zero);
                    break;
            }
        }

        public static void Scroll(int delta)
        {
            mouse_event(MOUSEEVENTF_WHEEL, 0, 0, unchecked((uint)delta), UIntPtr.Zero);
        }
    }
}
