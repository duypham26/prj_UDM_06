using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RemoteDesktop.Server.Network
{
    /// <summary>
    /// Bọc quanh TcpListener, phát sự kiện mỗi khi có client (máy điều khiển) kết nối tới.
    /// </summary>
    public class ServerSocket
    {
        private TcpListener _listener;
        private bool _isRunning;
        private readonly int _port;

        public event Action<TcpClient> OnClientConnected;
        public event Action<Exception> OnError;

        public ServerSocket(int port)
        {
            _port = port;
        }

        public void Start()
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            _isRunning = true;
            _ = Task.Run(ListenForClientsAsync);
        }

        private async Task ListenForClientsAsync()
        {
            while (_isRunning)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    OnClientConnected?.Invoke(client);
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        OnError?.Invoke(ex);
                    }
                    break;
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
        }
    }
}
