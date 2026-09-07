using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace RemoteDesktop.Server.Capture
{
    public static class ImageEncoder
    {
        public static byte[] CompressJpeg(Bitmap bitmap, long quality = 50L)
        {
            if (bitmap == null) return null;

            ImageCodecInfo jpegEncoder = GetEncoder(ImageFormat.Jpeg);
            if (jpegEncoder == null) return null;

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
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }
}
