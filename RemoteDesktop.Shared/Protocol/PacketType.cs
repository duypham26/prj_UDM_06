namespace RemoteDesktop.Shared.Protocol
{
    /// <summary>
    /// Danh sách các loại gói tin trao đổi giữa Client (máy điều khiển)
    /// và Server (máy đích) - xem chi tiết tại Docs/Protocol.md
    /// </summary>
    public enum PacketType : byte
    {
        // Kết nối
        ConnectRequest = 0,
        ConnectAccept = 1,
        ConnectReject = 2,

        // Màn hình
        ScreenFrame = 3,

        // Điều khiển chuột
        MouseMove = 4,
        MouseDown = 5,
        MouseUp = 6,
        MouseWheel = 7,

        // Điều khiển bàn phím
        KeyDown = 8,
        KeyUp = 9,

        // Phiên điều khiển
        Disconnect = 10,
        EmergencyStop = 11,

        // Kiểm tra kết nối
        Heartbeat = 12
    }
}
