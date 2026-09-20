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
                // Clone để không giữ MemoryStream sống bên trong Bitmap gốc
                using (var img = Image.FromStream(ms))
                {
                    return new Bitmap(img);
                }
            }
        }

        // Nén ảnh JPEG với mức chất lượng tùy chỉnh (0-100)
        public static byte[] CompressJpeg(Bitmap bitmap, long quality = 50L)
        {
            if (bitmap == null) return null;

            ImageCodecInfo jpegEncoder = GetEncoder(ImageFormat.Jpeg);
            if (jpegEncoder == null) return BitmapToByteArray(bitmap, ImageFormat.Jpeg);

            using (var encoderParameters = new EncoderParameters(1))
            {
                encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);
                using (var ms = new MemoryStream())
                {
                    bitmap.Save(ms, jpegEncoder, encoderParameters);
                    return ms.ToArray();
                }
            }
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            foreach (var codec in ImageCodecInfo.GetImageEncoders())
            {
                if (codec.FormatID == format.Guid) return codec;
            }
            return null;
        }
    }
}
