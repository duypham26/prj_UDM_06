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

        public event Action<byte[]> OnPacketReceived;
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
            byte[] buffer = new byte[8192];
            try
            {
                while (_isConnected && _client.Connected)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    byte[] data = new byte[bytesRead];
                    Array.Copy(buffer, data, bytesRead);
                    OnPacketReceived?.Invoke(data);
                }
            }
            catch (Exception)
            {
                // Mất kết nối hoặc lỗi đọc luồng
            }
            finally
            {
                Disconnect();
            }
        }

        public async Task SendAsync(byte[] data)
        {
            if (_isConnected && _stream != null)
            {
                await _stream.WriteAsync(data, 0, data.Length);
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
