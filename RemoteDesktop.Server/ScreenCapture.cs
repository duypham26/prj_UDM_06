using System.Drawing;

Cusing System;
using System.Drawing;
using System.Windows.Forms;
using RemoteDesktop.Shared.Helpers;

namespace RemoteDesktop.Server.Capture
{
    public class ScreenCapture
    {
        public static Bitmap CaptureScreen(bool includeCursor = true)
        {
            Rectangle bounds = Screen.PrimaryScreen.Bounds;
            Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);

                if (includeCursor)
                {
                    try
                    {
                        CURSORINFO cursorInfo;
                        cursorInfo.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(CURSORINFO));
                        if (GetCursorInfo(out cursorInfo) && cursorInfo.flags == CURSOR_SHOWING)
                        {
                            using (Icon icon = Icon.FromHandle(cursorInfo.hCursor))
                            {
                                int x = cursorInfo.ptScreenPos.X - icon.Width / 2;
                                int y = cursorInfo.ptScreenPos.Y - icon.Height / 2;
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

        public static byte[] CaptureAndCompress(long jpegQuality = 50L)
        {
            using (Bitmap screenshot = CaptureScreen())
            {
                byte[] jpegBytes = ImageEncoder.CompressJpeg(screenshot, jpegQuality);
                return CompressionHelper.Compress(jpegBytes);
            }
        }

        #region Win32 API for Cursor Capture
        private const int CURSOR_SHOWING = 0x00000001;

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetCursorInfo(out CURSORINFO pci);
        #endregion
    }
}
