using System.IO;
using RemoteDesktop.Shared.Models;

namespace RemoteDesktop.Shared.Protocol
{
    /// <summary>
    /// Chuyển đổi qua lại giữa các model (MouseEventData, KeyboardEventData, ScreenData...)
    /// và mảng byte để gửi qua PacketFramer. Đặt chung ở đây để Client và Server
    /// luôn dùng đúng 1 định dạng, tránh lệch nhau.
    /// </summary>
    public static class PayloadCodec
    {
        // ---------- Mouse ----------
        public static byte[] EncodeMouse(MouseEventData data)
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(data.X);
            w.Write(data.Y);
            w.Write((byte)data.Button);
            w.Write(data.Delta);
            return ms.ToArray();
        }

        public static MouseEventData DecodeMouse(byte[] payload, MouseEventType eventType)
        {
            using var ms = new MemoryStream(payload);
            using var r = new BinaryReader(ms);
            return new MouseEventData
            {
                EventType = eventType,
                X = r.ReadInt32(),
                Y = r.ReadInt32(),
                Button = (MouseButton)r.ReadByte(),
                Delta = r.ReadInt32()
            };
        }

        // ---------- Keyboard ----------
        public static byte[] EncodeKeyboard(KeyboardEventData data)
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(data.KeyCode);
            return ms.ToArray();
        }

        public static KeyboardEventData DecodeKeyboard(byte[] payload, KeyboardEventType eventType)
        {
            using var ms = new MemoryStream(payload);
            using var r = new BinaryReader(ms);
            return new KeyboardEventData
            {
                EventType = eventType,
                KeyCode = r.ReadInt32()
            };
        }

        // ---------- Screen frame ----------
        public static byte[] EncodeScreenFrame(ScreenData data)
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(data.Width);
            w.Write(data.Height);
            w.Write(data.Timestamp);
            w.Write(data.ImageData?.Length ?? 0);
            if (data.ImageData is { Length: > 0 })
            {
                w.Write(data.ImageData);
            }
            return ms.ToArray();
        }

        public static ScreenData DecodeScreenFrame(byte[] payload)
        {
            using var ms = new MemoryStream(payload);
            using var r = new BinaryReader(ms);
            var data = new ScreenData
            {
                Width = r.ReadInt32(),
                Height = r.ReadInt32(),
                Timestamp = r.ReadInt64()
            };
            int len = r.ReadInt32();
            data.ImageData = len > 0 ? r.ReadBytes(len) : System.Array.Empty<byte>();
            return data;
        }

        // ---------- Chuỗi văn bản (dùng cho ConnectRequest/Reject...) ----------
        public static byte[] EncodeString(string text)
        {
            return System.Text.Encoding.UTF8.GetBytes(text ?? string.Empty);
        }

        public static string DecodeString(byte[] payload)
        {
            if (payload == null || payload.Length == 0) return string.Empty;
            return System.Text.Encoding.UTF8.GetString(payload);
        }
    }
}
