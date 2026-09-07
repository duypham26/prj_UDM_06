using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RemoteDesktop.Client.Network
{
    public class ClientSocket
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private bool _isConnected;

        public bool IsConnected => _isConnected;

        public async Task<bool> ConnectAsync(string serverIp, int port)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(serverIp, port);
                _stream = _client.GetStream();
                _isConnected = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($""Connection failed: {ex.Message}"");
                return false;
            }
        }

        public async Task SendAsync(byte[] data)
        {
            if (!_isConnected) throw new InvalidOperationException(""Not connected"");
            await _stream.WriteAsync(data, 0, data.Length);
        }

        public async Task<byte[]> ReceiveAsync(int bufferSize = 4096)
        {
            if (!_isConnected) throw new InvalidOperationException(""Not connected"");
            var buffer = new byte[bufferSize];
            int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
            Array.Resize(ref buffer, bytesRead);
            return buffer;
        }

        public void Disconnect()
        {
            _stream?.Close();
            _client?.Close();
            _isConnected = false;
        }
    }
}
