using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteDesktop.Server.Network
{
    public class ServerSocket
    {
        private TcpListener _listener;
        private CancellationTokenSource _cts;
        public event Action<TcpClient> OnClientConnected;

        public bool IsRunning { get; private set; }

        public void Start(int port)
        {
            if (IsRunning) return;

            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _cts = new CancellationTokenSource();
            IsRunning = true;

            Task.Run(() => ListenAsync(_cts.Token));
        }

        private async Task ListenAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    OnClientConnected?.Invoke(client);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accepting client: {ex.Message}");
                }
            }
        }

        public void Stop()
        {
            if (!IsRunning) return;

            _cts?.Cancel();
            _listener?.Stop();
            IsRunning = false;
        }
    }
}