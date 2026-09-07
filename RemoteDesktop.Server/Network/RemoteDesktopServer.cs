using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using RemoteDesktop.Shared.Models;
using RemoteDesktop.Shared.Network;
using RemoteDesktop.Server.Services;

namespace RemoteDesktop.Server.Network
{
    public class RemoteDesktopServer
    {
        private TcpListener _listener;
        private readonly List<ClientSession> _clients = new List<ClientSession>();
        private readonly ScreenCaptureService _screenService;
        private bool _isRunning;
        private readonly object _lockObject = new object();

        public event EventHandler<ClientSession> ClientConnected;
        public event EventHandler<ClientSession> ClientDisconnected;
        public event EventHandler<Exception> ErrorOccurred;

        public RemoteDesktopServer()
        {
            _screenService = new ScreenCaptureService();
        }

        public async Task StartServerAsync(string ipAddress, int port, string password)
        {
            try
            {
                var localAddress = IPAddress.Parse(ipAddress);
                _listener = new TcpListener(localAddress, port);
                _listener.Start();
                _isRunning = true;

                Console.WriteLine($"Server started on {ipAddress}:{port}");

                while (_isRunning)
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(client, password));
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
                throw;
            }
        }

        private async Task HandleClientAsync(TcpClient client, string password)
        {
            var session = new ClientSession(client);
            try
            {
                var stream = client.GetStream();
                var authSuccess = await AuthenticateClientAsync(stream, password);

                if (authSuccess)
                {
                    lock (_lockObject)
                    {
                        _clients.Add(session);
                    }

                    session.Status = ConnectionStatus.Authenticated;
                    ClientConnected?.Invoke(this, session);

                    await session.HandleClientCommunicationAsync(_screenService);
                }
                else
                {
                    await SendErrorAsync(stream, "Authentication failed");
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
            }
            finally
            {
                lock (_lockObject)
                {
                    _clients.Remove(session);
                }
                ClientDisconnected?.Invoke(this, session);
                session.Dispose();
            }
        }

        private async Task<bool> AuthenticateClientAsync(NetworkStream stream, string password)
        {
            try
            {
                byte[] authBuffer = new byte[256];
                int bytesRead = await stream.ReadAsync(authBuffer, 0, authBuffer.Length);

                if (bytesRead > 0)
                {
                    string clientPassword = System.Text.Encoding.UTF8.GetString(authBuffer, 0, bytesRead).Trim();
                    return clientPassword == password;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private async Task SendErrorAsync(NetworkStream stream, string errorMessage)
        {
            var errorBytes = System.Text.Encoding.UTF8.GetBytes(errorMessage);
            await stream.WriteAsync(errorBytes, 0, errorBytes.Length);
        }

        public void StopServer()
        {
            _isRunning = false;
            _listener?.Stop();
            _screenService?.Dispose();

            lock (_lockObject)
            {
                foreach (var client in _clients)
                {
                    client.Dispose();
                }
                _clients.Clear();
            }
        }

        public List<ClientSession> GetConnectedClients()
        {
            lock (_lockObject)
            {
                return new List<ClientSession>(_clients);
            }
        }
    }

    public class ClientSession : IDisposable
    {
        private readonly TcpClient _client;
        private NetworkStream _stream;
        public ConnectionStatus Status { get; set; } = ConnectionStatus.Connecting;
        public string ClientIP { get; }
        public DateTime ConnectedAt { get; }

        public ClientSession(TcpClient client)
        {
            _client = client;
            _stream = client.GetStream();
            ClientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            ConnectedAt = DateTime.Now;
        }

        public async Task HandleClientCommunicationAsync(ScreenCaptureService screenService)
        {
            byte[] buffer = new byte[NetworkConstants.BufferSize];

            while (_client.Connected && Status != ConnectionStatus.Disconnected)
            {
                try
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        byte command = buffer[0];
                        byte[] data = new byte[bytesRead - 1];
                        Array.Copy(buffer, 1, data, 0, bytesRead - 1);

                        await ProcessCommandAsync(command, data, screenService);
                    }
                }
                catch
                {
                    break;
                }
            }
        }

        private async Task ProcessCommandAsync(byte command, byte[] data, ScreenCaptureService screenService)
        {
            switch (command)
            {
                case NetworkConstants.CMD_HEARTBEAT:
                    await SendCommandAsync(NetworkConstants.CMD_HEARTBEAT, new byte[] { 0x01 });
                    break;

                case NetworkConstants.CMD_SCREEN_DATA:
                    var screenData = await screenService.CaptureScreenAsync();
                    await SendScreenDataAsync(screenData);
                    break;

                case NetworkConstants.CMD_MOUSE_EVENT:
                    await ProcessMouseEventAsync(data);
                    break;

                case NetworkConstants.CMD_KEYBOARD_EVENT:
                    await ProcessKeyboardEventAsync(data);
                    break;

                case NetworkConstants.CMD_DISCONNECT:
                    Status = ConnectionStatus.Disconnected;
                    break;
            }
        }

        public async Task SendCommandAsync(byte command, byte[] data)
        {
            try
            {
                byte[] packet = new byte[data.Length + 1];
                packet[0] = command;
                Array.Copy(data, 0, packet, 1, data.Length);

                await _stream.WriteAsync(packet, 0, packet.Length);
                await _stream.FlushAsync();
            }
            catch
            {
                Status = ConnectionStatus.Failed;
            }
        }

        public async Task SendScreenDataAsync(ScreenData screenData)
        {
            var data = SerializeScreenData(screenData);
            await SendCommandAsync(NetworkConstants.CMD_SCREEN_DATA, data);
        }

        private byte[] SerializeScreenData(ScreenData screenData)
        {
            using var ms = new System.IO.MemoryStream();
            using var writer = new System.IO.BinaryWriter(ms);

            writer.Write(screenData.Width);
            writer.Write(screenData.Height);
            writer.Write(screenData.Timestamp);
            writer.Write(screenData.ImageData.Length);
            writer.Write(screenData.ImageData);

            return ms.ToArray();
        }

        private async Task ProcessMouseEventAsync(byte[] data)
        {
            try
            {
                using var ms = new System.IO.MemoryStream(data);
                using var reader = new System.IO.BinaryReader(ms);

                int x = reader.ReadInt32();
                int y = reader.ReadInt32();
                MouseEventType eventType = (MouseEventType)reader.ReadInt32();
                MouseButton button = (MouseButton)reader.ReadInt32();
                int delta = reader.ReadInt32();

                Console.WriteLine($"Mouse Event: {eventType} at ({x}, {y})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing mouse event: {ex.Message}");
            }
        }

        private async Task ProcessKeyboardEventAsync(byte[] data)
        {
            try
            {
                using var ms = new System.IO.MemoryStream(data);
                using var reader = new System.IO.BinaryReader(ms);

                int keyCode = reader.ReadInt32();
                char keyChar = reader.ReadChar();
                KeyboardEventType eventType = (KeyboardEventType)reader.ReadInt32();
                bool isSystemKey = reader.ReadBoolean();

                Console.WriteLine($"Keyboard Event: {eventType} - Key: {keyChar} (Code: {keyCode})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing keyboard event: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _stream?.Close();
            _stream?.Dispose();
            _client?.Close();
            _client?.Dispose();
        }
    }
}