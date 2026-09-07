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
            // Định dạng dữ liệu trước khi gửi đi
            return payload;
        }
    }
}
