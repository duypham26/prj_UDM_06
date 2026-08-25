namespace RemoteDesktop.Shared.Network
{
    public static class NetworkConstants
    {
        public const int DefaultPort = 3389;
        public const int BufferSize = 4096;
        public const int MaxPacketSize = 1024 * 1024;

        public const byte CMD_AUTH = 0x01;
        public const byte CMD_SCREEN_DATA = 0x02;
        public const byte CMD_MOUSE_EVENT = 0x03;
        public const byte CMD_KEYBOARD_EVENT = 0x04;
        public const byte CMD_HEARTBEAT = 0x05;
        public const byte CMD_DISCONNECT = 0x06;
        public const byte CMD_SCREEN_CHANGE = 0x07;
        public const byte CMD_ERROR = 0xFF;
    }
}