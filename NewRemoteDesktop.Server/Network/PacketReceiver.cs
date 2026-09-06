using System;
namespace NewRemoteDesktop.Server.Network
{
    public static class PacketReceiver
    {
        public static void ParsePacket(byte[] rawData, out byte packetType, out byte[] payload)
        {
            // Xử lý bóc tách dữ liệu gói tin tùy theo giao thức của nhóm
            packetType = rawData[0];
            int length = BitConverter.ToInt32(rawData, 1);
            payload = new byte[length];
            Array.Copy(rawData, 5, payload, 0, length);
        }
    }
}