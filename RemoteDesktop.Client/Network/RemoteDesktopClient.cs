using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using RemoteDesktop.Shared.Models;
using RemoteDesktop.Shared.Network;

namespace RemoteDesktop.Client.Network
{
    public class RemoteDesktopClient
    {
        private TcpClient _client;
        private NetworkStream _stream;
        public event EventHandler<ScreenData> ScreenDataReceived;
        public event EventHandler<ConnectionStatus> ConnectionStatusChanged;
        public event EventHandler<Exception> ErrorOccurred;

        public ConnectionStatus Status { get; private set; } = ConnectionStatus.Disconnected;

        public async Task<bool> ConnectAsync(string serverIP, int port, string password)
        {
            try
            {
                ConnectionStatusChanged?.Invoke(this, ConnectionStatus.Connecting);

                _client = new TcpClient();
                await _client.ConnectAsync(serverIP, port);
                _stream = _client.GetStream();

                var authSuccess = await AuthenticateAsync(password);

                if (authSuccess)
                {
                    Status = ConnectionStatus.Authenticated;
                    ConnectionStatusChanged?.Invoke(this, ConnectionStatus.Authenticated);

                    _ = Task.Run(() => ReceiveDataAsync());
                    return true;
                }
                else
                {
                    Status = ConnectionStatus.Failed;
                    ConnectionStatusChanged?.Invoke(this, ConnectionStatus.Failed);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Status = ConnectionStatus.Failed;
                ConnectionStatusChanged?.Invoke(this, ConnectionStatus.Failed);
                ErrorOccurred?.Invoke(this, ex);
                return false;
            }
        }

        private async Task<bool> AuthenticateAsync(string password)
        {
            try
            {
                var passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
                await _stream.WriteAsync(passwordBytes, 0, passwordBytes.Length);
                await _stream.FlushAsync();

                byte[] buffer = new byte[256];
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);

                if (bytesRead > 0)
                {
                    string response = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    return response.Contains("success", StringComparison.OrdinalIgnoreCase) || bytesRead == 0;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private async Task ReceiveDataAsync()
        {
            byte[] buffer = new byte[NetworkConstants.MaxPacketSize];

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

                        await ProcessReceivedDataAsync(command, data);
                    }
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, ex);
                    break;
                }
            }

            if (_client.Connected)
            {
                await DisconnectAsync();
            }
        }

        private async Task ProcessReceivedDataAsync(byte command, byte[] data)
        {
            switch (command)
            {
                case NetworkConstants.CMD_SCREEN_DATA:
                    var screenData = DeserializeScreenData(data);
                    ScreenDataReceived?.Invoke(this, screenData);
                    break;

                case NetworkConstants.CMD_HEARTBEAT:
                    await SendCommandAsync(NetworkConstants.CMD_HEARTBEAT, new byte[] { 0x01 });
                    break;

                case NetworkConstants.CMD_ERROR:
                    string errorMessage = System.Text.Encoding.UTF8.GetString(data);
                    ErrorOccurred?.Invoke(this, new Exception(errorMessage));
                    break;

                case NetworkConstants.CMD_DISCONNECT:
                    await DisconnectAsync();
                    break;
            }
        }

        private ScreenData DeserializeScreenData(byte[] data)
        {
            using var ms = new System.IO.MemoryStream(data);
            using var reader = new System.IO.BinaryReader(ms);

            var screenData = new ScreenData
            {
                Width = reader.ReadInt32(),
                Height = reader.ReadInt32(),
                Timestamp = reader.ReadInt64(),
            };

            int imageLength = reader.ReadInt32();
            screenData.ImageData = reader.ReadBytes(imageLength);

            return screenData;
        }

        public async Task SendCommandAsync(byte command, byte[] data)
        {
            if (_stream == null || !_client.Connected)
                return;

            try
            {
                byte[] packet = new byte[data.Length + 1];
                packet[0] = command;
                Array.Copy(data, 0, packet, 1, data.Length);

                await _stream.WriteAsync(packet, 0, packet.Length);
                await _stream.FlushAsync();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
            }
        }

        public async Task SendMouseEventAsync(MouseEventData mouseData)
        {
            using var ms = new System.IO.MemoryStream();
            using var writer = new System.IO.BinaryWriter(ms);

            writer.Write(mouseData.X);
            writer.Write(mouseData.Y);
            writer.Write((int)mouseData.EventType);
            writer.Write((int)mouseData.Button);
            writer.Write(mouseData.Delta);

            await SendCommandAsync(NetworkConstants.CMD_MOUSE_EVENT, ms.ToArray());
        }

        public async Task SendKeyboardEventAsync(KeyboardEventData keyData)
        {
            using var ms = new System.IO.MemoryStream();
            using var writer = new System.IO.BinaryWriter(ms);

            writer.Write(keyData.KeyCode);
            writer.Write(keyData.KeyChar);
            writer.Write((int)keyData.EventType);
            writer.Write(keyData.IsSystemKey);

            await SendCommandAsync(NetworkConstants.CMD_KEYBOARD_EVENT, ms.ToArray());
        }

        public async Task RequestScreenAsync()
        {
            await SendCommandAsync(NetworkConstants.CMD_SCREEN_DATA, new byte[0]);
        }

        public async Task DisconnectAsync()
        {
            if (Status != ConnectionStatus.Disconnected)
            {
                await SendCommandAsync(NetworkConstants.CMD_DISCONNECT, new byte[0]);
                Status = ConnectionStatus.Disconnected;
                ConnectionStatusChanged?.Invoke(this, ConnectionStatus.Disconnected);
            }

            _stream?.Close();
            _stream?.Dispose();
            _client?.Close();
            _client?.Dispose();
        }
    }
}