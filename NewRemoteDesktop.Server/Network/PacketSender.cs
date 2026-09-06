using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewRemoteDesktop.Server.Network
{
    public static class PacketSender
    {
        public static byte[] CreatePacket(byte packetType, byte[] payload)
        {
            int length = payload?.Length ?? 0;
            byte[] packet = new byte[1 + 4 + length];
            packet[0] = packetType;
            BitConverter.GetBytes(length).CopyTo(packet, 1);
            if (length > 0)
            {
                payload.CopyTo(packet, 5);
            }
            return packet;
            // Định dạng dữ liệu trước khi gửi đi
        }
    }
}
