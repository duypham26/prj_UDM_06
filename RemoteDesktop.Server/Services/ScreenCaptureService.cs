using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using RemoteDesktop.Shared.Models;

namespace RemoteDesktop.Server.Services
{
    public class ScreenCaptureService : IDisposable
    {
        private bool _isDisposed = false;

        public async Task<ScreenData> CaptureScreenAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var screenBounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;

                    using var bitmap = new Bitmap(screenBounds.Width, screenBounds.Height);
                    using var graphics = Graphics.FromImage(bitmap);

                    graphics.CopyFromScreen(screenBounds.X, screenBounds.Y, 0, 0, screenBounds.Size);

                    using var ms = new MemoryStream();
                    bitmap.Save(ms, ImageFormat.Jpeg);

                    return new ScreenData
                    {
                        ImageData = ms.ToArray(),
                        Width = screenBounds.Width,
                        Height = screenBounds.Height,
                        Timestamp = DateTime.Now.Ticks
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error capturing screen: {ex.Message}");
                    throw;
                }
            });
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
            }
        }
    }
}