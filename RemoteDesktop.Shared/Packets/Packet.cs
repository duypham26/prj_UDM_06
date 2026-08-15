using RemoteDesktop.Shared.Protocol;

namespace RemoteDesktop.Shared.Packets
{
    public class Packet
    {
        public PacketType Type;
        public string Data;

        public Packet()
        {
            Type = PacketType.ConnectRequest;
            Data = "";
        }

        public Packet(PacketType type, string data)
        {
            Type = type;
            Data = data;
        }
    }
}