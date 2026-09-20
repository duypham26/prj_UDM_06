using System;
using System.Drawing;
using System.IO;

namespace RemoteDesktop.Client.UI
{
    /// <summary>
    /// Nạp các ảnh trong thư mục Resources (được copy ra thư mục build nhờ csproj)
    /// một cách an toàn - nếu thiếu file thì trả về null thay vì crash ứng dụng.
    /// </summary>
    internal static class AppImages
    {
        public static Image TryLoad(string fileName)
        {
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, "Resources", fileName);
                if (File.Exists(path))
                {
                    using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                    return Image.FromStream(fs);
                }
            }
            catch
            {
                // Bỏ qua - giao diện sẽ hiển thị mà không có ảnh banner
            }
            return null;
        }
    }
}
