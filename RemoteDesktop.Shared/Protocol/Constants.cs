namespace RemoteDesktop.Shared.Protocol
{
    public static class Constants
    {
        // Phiên bản hiện tại của giao thức
        public const int ProtocolVersion = 1;

        // Cổng TCP mặc định
        public const int DefaultPort = 5000;

        // Khoảng thời gian giữa các lần gửi heartbeat, tính bằng mili giây
        public const int HeartbeatIntervalMs = 5000;

        // Thời gian chờ heartbeat trước khi xác định kết nối không còn hoạt động
        public const int HeartbeatTimeoutMs = 15000;

        // Tên cũ để tương thích với code hiện tại
        public const int HeartbeatTime = HeartbeatIntervalMs;
    }
}