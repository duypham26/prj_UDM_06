namespace RemoteDesktop.Shared.Protocol
{
    public class Packet
    {
        // Phiên bản giao thức của packet
        public int ProtocolVersion { get; set; }

        // Loại packet
        public PacketType Type { get; set; }

        // Dữ liệu của packet
        public string Data { get; set; }

        public Packet()
        {
            ProtocolVersion = Constants.ProtocolVersion;
            Type = PacketType.ConnectRequest;
            Data = string.Empty;
        }

        public Packet(PacketType type, string data)
        {
            ProtocolVersion = Constants.ProtocolVersion;
            Type = type;
            Data = data ?? string.Empty;
        }

        public Packet(int protocolVersion, PacketType type, string data)
        {
            ProtocolVersion = protocolVersion;
            Type = type;
            Data = data ?? string.Empty;
        }
    }
}