using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RemoteDesktop.Server.Capture
{
    /// <summary>
    /// Chụp ảnh màn hình hiện tại của máy đích (bao gồm cả con trỏ chuột).
    /// </summary>
    public static class ScreenCapture
    {
        public static Bitmap CaptureScreen(bool includeCursor = true)
        {
            Rectangle bounds = Screen.PrimaryScreen.Bounds;
            Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);

                if (includeCursor)
                {
                    try
                    {
                        CURSORINFO cursorInfo = default;
                        cursorInfo.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
                        if (GetCursorInfo(out cursorInfo) && cursorInfo.flags == CURSOR_SHOWING)
                        {
                            using (Icon icon = Icon.FromHandle(cursorInfo.hCursor))
                            {
                                int x = cursorInfo.ptScreenPos.X - bounds.Left - icon.Width / 2;
                                int y = cursorInfo.ptScreenPos.Y - bounds.Top - icon.Height / 2;
                                g.DrawIcon(icon, x, y);
                            }
                        }
                    }
                    catch
                    {
                        // Bỏ qua lỗi vẽ con trỏ nếu không lấy được Win32 handle
                    }
                }
            }

            return bitmap;
        }

        /// <summary>Chụp màn hình và nén JPEG theo chất lượng chỉ định (0-100).</summary>
        public static byte[] CaptureAndCompress(long jpegQuality = 50L)
        {
            using (Bitmap screenshot = CaptureScreen())
            {
                return RemoteDesktop.Shared.Helpers.ImageHelper.CompressJpeg(screenshot, jpegQuality);
            }
        }

        #region Win32 API for Cursor Capture
        private const int CURSOR_SHOWING = 0x00000001;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(out CURSORINFO pci);
        #endregion
    }
}
