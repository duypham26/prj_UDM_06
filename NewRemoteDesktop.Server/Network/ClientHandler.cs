using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NewRemoteDesktop.Server.Network
{
    public class ClientHandler
    {
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;
        private bool _isConnected;

        public event Action<byte, byte[]> OnPacketReceived;
        public event Action<ClientHandler> OnDisconnected;

        public ClientHandler(TcpClient client)
        {
            _client = client;
            _stream = client.GetStream();
            _isConnected = true;
        }

        public void StartListening()
        {
            Task.Run(() => ReceiveLoopAsync());
        }

        private async Task ReceiveLoopAsync()
        {
            try
            {
                while (_isConnected && _client.Connected)
                {
                    byte[] header = new byte[5];
                    int bytesRead = await ReadExactAsync(header, 5);
                    if (bytesRead < 5) break;

                    byte packetType = header[0];
                    int payloadLength = BitConverter.ToInt32(header, 1);

                    byte[] payload = new byte[payloadLength];
                    if (payloadLength > 0)
                    {
                        await ReadExactAsync(payload, payloadLength);
                    }

                    OnPacketReceived?.Invoke(packetType, payload);
                }
            }
            catch (Exception)
            {
                // Mất kết nối hoặc lỗi luồng
            }
            finally
            {
                Disconnect();
            }
        }

        private async Task<int> ReadExactAsync(byte[] buffer, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = await _stream.ReadAsync(buffer, totalRead, count - totalRead);
                if (read == 0) break;
                totalRead += read;
            }
            return totalRead;
        }

        public async Task SendAsync(byte packetType, byte[] payload)
        {
            if (_isConnected && _stream != null)
            {
                byte[] packet = PacketSender.CreatePacket(packetType, payload);
                await _stream.WriteAsync(packet, 0, packet.Length);
            }
        }

        public void Disconnect()
        {
            if (!_isConnected) return;
            _isConnected = false;
            _stream?.Close();
            _client?.Close();
            OnDisconnected?.Invoke(this);
        }
    }
}