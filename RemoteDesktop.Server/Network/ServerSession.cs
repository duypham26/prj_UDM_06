using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using RemoteDesktop.Server.Capture;
using RemoteDesktop.Server.Input;
using RemoteDesktop.Shared.Models;
using RemoteDesktop.Shared.Protocol;

namespace RemoteDesktop.Server.Network
{
    /// <summary>
    /// Quản lý toàn bộ vòng đời của 1 kết nối từ máy điều khiển tới máy đích:
    /// xin kết nối -> chờ người dùng máy đích Accept/Reject -> stream màn hình
    /// -> nhận & giả lập thao tác chuột/bàn phím -> kết thúc phiên.
    /// </summary>
    public class ServerSession
    {
        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _stream;
        private readonly CancellationTokenSource _cts = new();
        private TaskCompletionSource<bool> _decisionTcs;

        public string RemoteEndPoint { get; }
        public bool IsActive { get; private set; }

        /// <summary>Chất lượng JPEG khi stream màn hình (0-100).</summary>
        public int JpegQuality { get; set; } = 50;

        /// <summary>Khoảng thời gian giữa 2 khung hình (mili giây).</summary>
        public int FrameIntervalMs { get; set; } = 100; // ~10 FPS

        public event Action<ServerSession, string> ConnectRequestReceived; // password đã đúng, chờ người dùng xác nhận
        public event Action<ServerSession> SessionStarted;
        public event Action<ServerSession, string> SessionEnded;
        public event Action<ServerSession, string> LogMessage;

        public ServerSession(TcpClient tcpClient)
        {
            _tcpClient = tcpClient;
            _stream = tcpClient.GetStream();
            RemoteEndPoint = tcpClient.Client.RemoteEndPoint?.ToString() ?? "unknown";
        }

        public async Task RunAsync(string expectedPassword)
        {
            try
            {
                var first = await PacketFramer.ReadAsync(_stream);
                if (first == null || first.Value.Type != PacketType.ConnectRequest)
                {
                    End("Gói tin đầu tiên không hợp lệ.");
                    return;
                }

                string password = PayloadCodec.DecodeString(first.Value.Payload);
                if (!string.Equals(password, expectedPassword, StringComparison.Ordinal))
                {
                    await PacketFramer.WriteAsync(_stream, PacketType.ConnectReject, PayloadCodec.EncodeString("Sai mật khẩu."));
                    End("Sai mật khẩu.");
                    return;
                }

                // Mật khẩu đúng -> hỏi ý kiến người dùng máy đích
                _decisionTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                ConnectRequestReceived?.Invoke(this, RemoteEndPoint);

                bool accepted = await _decisionTcs.Task;
                if (!accepted)
                {
                    await PacketFramer.WriteAsync(_stream, PacketType.ConnectReject, PayloadCodec.EncodeString("Người dùng từ chối kết nối."));
                    End("Người dùng từ chối kết nối.");
                    return;
                }

                await PacketFramer.WriteAsync(_stream, PacketType.ConnectAccept);
                IsActive = true;
                SessionStarted?.Invoke(this);

                var screenLoop = Task.Run(() => ScreenStreamLoopAsync(_cts.Token));
                var receiveLoop = Task.Run(() => ReceiveLoopAsync(_cts.Token));

                await Task.WhenAny(screenLoop, receiveLoop);
                _cts.Cancel();
                await Task.WhenAll(SafeAwait(screenLoop), SafeAwait(receiveLoop));

                End("Phiên kết thúc.");
            }
            catch (Exception ex)
            {
                End($"Lỗi: {ex.Message}");
            }
        }

        private static async Task SafeAwait(Task t)
        {
            try { await t; } catch { /* đã xử lý log ở nơi khác */ }
        }

        /// <summary>Người dùng máy đích bấm Accept trên hộp thoại xin kết nối.</summary>
        public void Accept() => _decisionTcs?.TrySetResult(true);

        /// <summary>Người dùng máy đích bấm Reject trên hộp thoại xin kết nối.</summary>
        public void Reject() => _decisionTcs?.TrySetResult(false);

        /// <summary>Người dùng máy đích bấm nút Dừng khẩn cấp trong lúc đang bị điều khiển.</summary>
        public async void TriggerEmergencyStop()
        {
            try
            {
                if (IsActive)
                {
                    await PacketFramer.WriteAsync(_stream, PacketType.EmergencyStop);
                }
            }
            catch { /* ignore */ }
            finally
            {
                _cts.Cancel();
            }
        }

        private async Task ScreenStreamLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    byte[] jpeg = ScreenCapture.CaptureAndCompress(JpegQuality);
                    var bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
                    var frame = new ScreenData
                    {
                        ImageData = jpeg,
                        Width = bounds.Width,
                        Height = bounds.Height,
                        Timestamp = DateTime.UtcNow.Ticks
                    };
                    await PacketFramer.WriteAsync(_stream, PacketType.ScreenFrame, PayloadCodec.EncodeScreenFrame(frame));
                }
                catch (Exception ex)
                {
                    LogMessage?.Invoke(this, $"Lỗi khi gửi khung hình: {ex.Message}");
                    return;
                }

                try
                {
                    await Task.Delay(FrameIntervalMs, token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var packet = await PacketFramer.ReadAsync(_stream);
                    if (packet == null) return; // đối phương đóng kết nối

                    switch (packet.Value.Type)
                    {
                        case PacketType.MouseMove:
                            InputHandler.HandleMouse(PayloadCodec.DecodeMouse(packet.Value.Payload, MouseEventType.Move));
                            break;
                        case PacketType.MouseDown:
                            InputHandler.HandleMouse(PayloadCodec.DecodeMouse(packet.Value.Payload, MouseEventType.Down));
                            break;
                        case PacketType.MouseUp:
                            InputHandler.HandleMouse(PayloadCodec.DecodeMouse(packet.Value.Payload, MouseEventType.Up));
                            break;
                        case PacketType.MouseWheel:
                            InputHandler.HandleMouse(PayloadCodec.DecodeMouse(packet.Value.Payload, MouseEventType.Scroll));
                            break;
                        case PacketType.KeyDown:
                            InputHandler.HandleKeyboard(PayloadCodec.DecodeKeyboard(packet.Value.Payload, KeyboardEventType.KeyDown));
                            break;
                        case PacketType.KeyUp:
                            InputHandler.HandleKeyboard(PayloadCodec.DecodeKeyboard(packet.Value.Payload, KeyboardEventType.KeyUp));
                            break;
                        case PacketType.Heartbeat:
                            await PacketFramer.WriteAsync(_stream, PacketType.Heartbeat);
                            break;
                        case PacketType.Disconnect:
                            return;
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(this, $"Mất kết nối: {ex.Message}");
            }
        }

        private void End(string reason)
        {
            IsActive = false;
            try { _cts.Cancel(); } catch { }
            try { _stream?.Close(); } catch { }
            try { _tcpClient?.Close(); } catch { }
            SessionEnded?.Invoke(this, reason);
        }
    }
}
