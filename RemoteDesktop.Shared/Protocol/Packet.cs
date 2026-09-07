namespace RemoteDesktop.Shared.Protocol
{
    public class Packet
    {
        public PacketType Type { get; set; }

        public string Data { get; set; }

        public Packet()
        {
            Type = PacketType.ConnectRequest;
            Data = string.Empty;
        }

        public Packet(PacketType type, string data)
        {
            Type = type;
            Data = data ?? string.Empty;
        }
    }
}