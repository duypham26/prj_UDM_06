using System;
namespace NewRemoteDesktop.Server.Network
{
    public static class PacketReceiver
    {
        public static void ParsePacket(byte[] rawData, out byte packetType, out byte[] payload)
        {
            // Validate rawData length first
            if (rawData == null || rawData.Length < 5)
            {
                throw new ArgumentException("Invalid packet: insufficient header length.");
            }

            packetType = rawData[0];
            int length = BitConverter.ToInt32(rawData, 1);

            if (length < 0 || rawData.Length < 5 + length)
            {
                throw new ArgumentException("Invalid packet: payload length mismatch.");
            }

            payload = new byte[length];
            if (length > 0)
            {
                Array.Copy(rawData, 5, payload, 0, length);
            }
        }
    }
}