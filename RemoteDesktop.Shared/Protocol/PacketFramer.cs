using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RemoteDesktop.Shared.Protocol
{
    /// <summary>
    /// Kết quả đọc được một packet hoàn chỉnh từ luồng dữ liệu TCP.
    /// </summary>
    public readonly struct RawPacket
    {
        public PacketType Type { get; }
        public byte[] Payload { get; }

        public RawPacket(PacketType type, byte[] payload)
        {
            Type = type;
            Payload = payload ?? Array.Empty<byte>();
        }
    }

    /// <summary>
    /// Đóng khung (framing) dữ liệu khi gửi/nhận qua TCP.
    ///
    /// Cấu trúc 1 frame trên dây truyền:
    ///   [1 byte  PacketType]
    ///   [4 byte  độ dài payload, little-endian Int32]
    ///   [N byte  payload]
    /// </summary>
    public static class PacketFramer
    {
        private const int HeaderSize = 1 + 4;

        // Giới hạn an toàn cho kích thước 1 payload (tránh cấp phát bộ nhớ khổng lồ
        // nếu nhận phải dữ liệu rác/độc hại).
        public const int MaxPayloadSize = 16 * 1024 * 1024; // 16 MB, đủ cho 1 khung hình JPEG chất lượng cao

        public static async Task WriteAsync(Stream stream, PacketType type, byte[] payload)
        {
            payload ??= Array.Empty<byte>();

            byte[] header = new byte[HeaderSize];
            header[0] = (byte)type;
            BitConverter.GetBytes(payload.Length).CopyTo(header, 1);

            // Ghép header + payload thành 1 lần Write để giảm số gói TCP nhỏ lẻ
            byte[] frame = new byte[HeaderSize + payload.Length];
            Buffer.BlockCopy(header, 0, frame, 0, HeaderSize);
            if (payload.Length > 0)
            {
                Buffer.BlockCopy(payload, 0, frame, HeaderSize, payload.Length);
            }

            await stream.WriteAsync(frame, 0, frame.Length).ConfigureAwait(false);
        }

        public static Task WriteAsync(Stream stream, PacketType type) => WriteAsync(stream, type, Array.Empty<byte>());

        /// <summary>
        /// Đọc chính xác 1 packet từ stream. Trả về null nếu kết nối đã đóng (EOF).
        /// </summary>
        public static async Task<RawPacket?> ReadAsync(Stream stream)
        {
            byte[] header = new byte[HeaderSize];
            if (!await ReadExactAsync(stream, header, HeaderSize).ConfigureAwait(false))
            {
                return null;
            }

            byte typeByte = header[0];
            int length = BitConverter.ToInt32(header, 1);

            if (length < 0 || length > MaxPayloadSize)
            {
                throw new InvalidDataException($"Kích thước payload không hợp lệ: {length}");
            }

            byte[] payload = Array.Empty<byte>();
            if (length > 0)
            {
                payload = new byte[length];
                if (!await ReadExactAsync(stream, payload, length).ConfigureAwait(false))
                {
                    return null;
                }
            }

            if (!Enum.IsDefined(typeof(PacketType), typeByte))
            {
                throw new InvalidDataException($"PacketType không xác định: {typeByte}");
            }

            return new RawPacket((PacketType)typeByte, payload);
        }

        private static async Task<bool> ReadExactAsync(Stream stream, byte[] buffer, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = await stream.ReadAsync(buffer, totalRead, count - totalRead).ConfigureAwait(false);
                if (read == 0)
                {
                    // Đối phương đã đóng kết nối
                    return false;
                }
                totalRead += read;
            }
            return true;
        }
    }
}
