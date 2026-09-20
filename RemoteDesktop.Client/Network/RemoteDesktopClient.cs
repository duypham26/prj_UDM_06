using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using RemoteDesktop.Shared.Models;
using RemoteDesktop.Shared.Protocol;

namespace RemoteDesktop.Client.Network
{
    /// <summary>
    /// Kết nối tới RemoteDesktop.Server, thực hiện bắt tay theo Docs/Protocol.md
    /// rồi nhận luồng ScreenFrame / gửi thao tác chuột-bàn phím.
    /// </summary>
    public class RemoteDesktopClient
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;

        public event EventHandler<ScreenData> ScreenDataReceived;
        public event EventHandler<ConnectionStatus> ConnectionStatusChanged;
        public event EventHandler<string> ErrorOccurred;
        /// <summary>Được gọi khi máy đích chủ động bấm "Dừng khẩn cấp" hoặc mất kết nối.</summary>
        public event EventHandler<string> SessionEnded;

        public ConnectionStatus Status { get; private set; } = ConnectionStatus.Disconnected;

        /// <summary>
        /// Kết nối + xin quyền điều khiển. Trả về (thành công, lý do nếu thất bại).
        /// </summary>
        public async Task<(bool Success, string Message)> ConnectAsync(string serverIp, int port, string password)
        {
            try
            {
                ConnectionStatusChanged?.Invoke(this, ConnectionStatus.Connecting);
                Status = ConnectionStatus.Connecting;

                _client = new TcpClient();
                var connectTask = _client.ConnectAsync(serverIp, port);
                if (await Task.WhenAny(connectTask, Task.Delay(8000)) != connectTask)
                {
                    throw new TimeoutException("Hết thời gian chờ kết nối tới máy chủ.");
                }
                await connectTask;

                _stream = _client.GetStream();

                await PacketFramer.WriteAsync(_stream, PacketType.ConnectRequest, PayloadCodec.EncodeString(password));

                var response = await PacketFramer.ReadAsync(_stream);
                if (response == null)
                {
                    throw new IOException("Máy chủ đã đóng kết nối.");
                }

                if (response.Value.Type == PacketType.ConnectReject)
                {
                    string reason = PayloadCodec.DecodeString(response.Value.Payload);
                    Status = ConnectionStatus.Failed;
                    ConnectionStatusChanged?.Invoke(this, ConnectionStatus.Failed);
                    CleanupSocket();
                    return (false, string.IsNullOrEmpty(reason) ? "Kết nối bị từ chối." : reason);
                }

                if (response.Value.Type != PacketType.ConnectAccept)
                {
                    throw new IOException("Phản hồi không hợp lệ từ máy chủ.");
                }

                Status = ConnectionStatus.Authenticated;
                ConnectionStatusChanged?.Invoke(this, ConnectionStatus.Authenticated);

                _cts = new CancellationTokenSource();
                _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                Status = ConnectionStatus.Failed;
                ConnectionStatusChanged?.Invoke(this, ConnectionStatus.Failed);
                ErrorOccurred?.Invoke(this, ex.Message);
                CleanupSocket();
                return (false, ex.Message);
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var packet = await PacketFramer.ReadAsync(_stream);
                    if (packet == null)
                    {
                        RaiseSessionEnded("Mất kết nối tới máy chủ.");
                        return;
                    }

                    switch (packet.Value.Type)
                    {
                        case PacketType.ScreenFrame:
                            var frame = PayloadCodec.DecodeScreenFrame(packet.Value.Payload);
                            ScreenDataReceived?.Invoke(this, frame);
                            break;

                        case PacketType.Heartbeat:
                            await PacketFramer.WriteAsync(_stream, PacketType.Heartbeat);
                            break;

                        case PacketType.EmergencyStop:
                            RaiseSessionEnded("Máy đích đã dừng phiên (Emergency Stop).");
                            return;

                        case PacketType.Disconnect:
                            RaiseSessionEnded("Máy đích đã ngắt kết nối.");
                            return;
                    }
                }
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                {
                    RaiseSessionEnded($"Lỗi kết nối: {ex.Message}");
                }
            }
        }

        private void RaiseSessionEnded(string reason)
        {
            Status = ConnectionStatus.Disconnected;
            ConnectionStatusChanged?.Invoke(this, ConnectionStatus.Disconnected);
            SessionEnded?.Invoke(this, reason);
            CleanupSocket();
        }

        public async Task SendMouseEventAsync(MouseEventData data)
        {
            if (Status != ConnectionStatus.Authenticated) return;
            var type = data.EventType switch
            {
                MouseEventType.Move => PacketType.MouseMove,
                MouseEventType.Down => PacketType.MouseDown,
                MouseEventType.Up => PacketType.MouseUp,
                MouseEventType.Scroll => PacketType.MouseWheel,
                _ => PacketType.MouseMove
            };
            await SafeWriteAsync(type, PayloadCodec.EncodeMouse(data));
        }

        public async Task SendKeyboardEventAsync(KeyboardEventData data)
        {
            if (Status != ConnectionStatus.Authenticated) return;
            var type = data.EventType == KeyboardEventType.KeyUp ? PacketType.KeyUp : PacketType.KeyDown;
            await SafeWriteAsync(type, PayloadCodec.EncodeKeyboard(data));
        }

        private async Task SafeWriteAsync(PacketType type, byte[] payload)
        {
            try
            {
                if (_stream != null)
                {
                    await PacketFramer.WriteAsync(_stream, type, payload);
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex.Message);
            }
        }

        public async Task DisconnectAsync()
        {
            if (Status == ConnectionStatus.Disconnected)
            {
                CleanupSocket();
                return;
            }

            try
            {
                if (_stream != null)
                {
                    await PacketFramer.WriteAsync(_stream, PacketType.Disconnect);
                }
            }
            catch { /* ignore */ }

            Status = ConnectionStatus.Disconnected;
            ConnectionStatusChanged?.Invoke(this, ConnectionStatus.Disconnected);
            CleanupSocket();
        }

        private void CleanupSocket()
        {
            try { _cts?.Cancel(); } catch { }
            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }
            _stream = null;
            _client = null;
        }
    }
}
