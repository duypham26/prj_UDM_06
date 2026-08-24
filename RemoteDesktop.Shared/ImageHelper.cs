using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace RemoteDesktop.Shared.Helpers
{
    public static class ImageHelper
    {
        // Chuyển Bitmap thành mảng byte với định dạng chỉ định (mặc định JPEG)
        public static byte[] BitmapToByteArray(Bitmap bitmap, ImageFormat format = null)
        {
            if (bitmap == null) return null;
            format ??= ImageFormat.Jpeg;

            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, format);
                return ms.ToArray();
            }
        }

        // Chuyển mảng byte ngược lại thành Bitmap
        public static Bitmap ByteArrayToBitmap(byte[] data)
        {
            if (data == null || data.Length == 0) return null;

            using (var ms = new MemoryStream(data))
            {
                return new Bitmap(ms);
            }
        }
    }
}