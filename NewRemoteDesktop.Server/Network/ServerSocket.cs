using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NewRemoteDesktop.Server.Network
{
    public class ServerSocket
    {
        private TcpListener _listener;
        private bool _isRunning;
        private readonly int _port;

        public event Action<TcpClient> OnClientConnected;
        public ServerSocket(int port)
        {
            _port = port;
        }
        public void Start()
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            _isRunning = true;
            Task.Run(() => ListenForClientsAsync());
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
                catch (Exception)
                {
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
