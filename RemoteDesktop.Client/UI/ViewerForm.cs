using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using RemoteDesktop.Client.Network;
using RemoteDesktop.Shared.Models;

namespace RemoteDesktop.Client.UI
{
    /// <summary>
    /// Cửa sổ hiển thị màn hình máy đích và chuyển tiếp thao tác chuột/bàn phím.
    /// </summary>
    public class ViewerForm : Form
    {
        private readonly RemoteDesktopClient _client;

        private readonly Panel _topBar;
        private readonly Label _lblStatus;
        private readonly Button _btnDisconnect;
        private readonly PictureBox _screen;

        private int _remoteWidth = 1920;
        private int _remoteHeight = 1080;

        public ViewerForm(RemoteDesktopClient client, string serverEndpoint)
        {
            _client = client;

            Text = $"Remote Desktop - Đang điều khiển {serverEndpoint}";
            ClientSize = new Size(1024, 700);
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;
            BackColor = Color.Black;

            _topBar = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Color.FromArgb(30, 30, 46) };
            _lblStatus = new Label
            {
                Text = $"Đang kết nối tới {serverEndpoint}",
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(10, 9)
            };
            _btnDisconnect = new Button
            {
                Text = "Ngắt kết nối",
                Size = new Size(110, 26),
                Location = new Point(0, 5),
                BackColor = Color.FromArgb(200, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnDisconnect.Click += async (s, e) => await DisconnectAndCloseAsync();
            _topBar.Controls.Add(_lblStatus);
            _topBar.Controls.Add(_btnDisconnect);
            _topBar.Resize += (s, e) => _btnDisconnect.Location = new Point(_topBar.Width - _btnDisconnect.Width - 10, 5);

            _screen = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };
            _screen.MouseMove += Screen_MouseMove;
            _screen.MouseDown += Screen_MouseDown;
            _screen.MouseUp += Screen_MouseUp;
            _screen.MouseWheel += Screen_MouseWheel;

            Controls.Add(_screen);
            Controls.Add(_topBar);

            KeyDown += ViewerForm_KeyDown;
            KeyUp += ViewerForm_KeyUp;
            FormClosing += async (s, e) => await DisconnectAndCloseAsync(fromFormClosing: true);

            _client.ScreenDataReceived += Client_ScreenDataReceived;
            _client.SessionEnded += Client_SessionEnded;
            _client.ErrorOccurred += (s, msg) => SetStatus($"Lỗi: {msg}");
        }

        private void Client_ScreenDataReceived(object sender, ScreenData data)
        {
            if (data?.ImageData == null || data.ImageData.Length == 0) return;

            _remoteWidth = data.Width;
            _remoteHeight = data.Height;

            try
            {
                using var ms = new MemoryStream(data.ImageData);
                var img = Image.FromStream(ms);
                var old = _screen.Image;

                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        _screen.Image = img;
                        old?.Dispose();
                        SetStatus($"Đang điều khiển - {_remoteWidth}x{_remoteHeight}");
                    }));
                }
                else
                {
                    _screen.Image = img;
                    old?.Dispose();
                    SetStatus($"Đang điều khiển - {_remoteWidth}x{_remoteHeight}");
                }
            }
            catch
            {
                // Bỏ qua khung hình lỗi, chờ khung tiếp theo
            }
        }

        private void Client_SessionEnded(object sender, string reason)
        {
            if (InvokeRequired) { Invoke(new Action(() => Client_SessionEnded(sender, reason))); return; }

            MessageBox.Show(this, reason, "Phiên đã kết thúc", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }

        private void SetStatus(string text)
        {
            if (InvokeRequired) { Invoke(new Action(() => SetStatus(text))); return; }
            _lblStatus.Text = text;
        }

        /// <summary>Quy đổi tọa độ chuột trên PictureBox (chế độ Zoom) sang tọa độ màn hình thật của máy đích.</summary>
        private bool TryMapToRemote(Point local, out int remoteX, out int remoteY)
        {
            remoteX = 0;
            remoteY = 0;
            if (_screen.Image == null || _screen.Width == 0 || _screen.Height == 0) return false;

            float imgAspect = (float)_screen.Image.Width / _screen.Image.Height;
            float boxAspect = (float)_screen.Width / _screen.Height;

            Rectangle displayRect;
            if (imgAspect > boxAspect)
            {
                int displayHeight = (int)(_screen.Width / imgAspect);
                int y = (_screen.Height - displayHeight) / 2;
                displayRect = new Rectangle(0, y, _screen.Width, displayHeight);
            }
            else
            {
                int displayWidth = (int)(_screen.Height * imgAspect);
                int x = (_screen.Width - displayWidth) / 2;
                displayRect = new Rectangle(x, 0, displayWidth, _screen.Height);
            }

            if (!displayRect.Contains(local)) return false;

            float relX = (local.X - displayRect.X) / (float)displayRect.Width;
            float relY = (local.Y - displayRect.Y) / (float)displayRect.Height;

            remoteX = (int)(relX * _remoteWidth);
            remoteY = (int)(relY * _remoteHeight);
            return true;
        }

        private static MouseButton ConvertButton(MouseButtons b) => b switch
        {
            MouseButtons.Left => MouseButton.Left,
            MouseButtons.Right => MouseButton.Right,
            MouseButtons.Middle => MouseButton.Middle,
            _ => MouseButton.None
        };

        private async void Screen_MouseMove(object sender, MouseEventArgs e)
        {
            if (!TryMapToRemote(e.Location, out int x, out int y)) return;
            await _client.SendMouseEventAsync(new MouseEventData { EventType = MouseEventType.Move, X = x, Y = y });
        }

        private async void Screen_MouseDown(object sender, MouseEventArgs e)
        {
            if (!TryMapToRemote(e.Location, out int x, out int y)) return;
            await _client.SendMouseEventAsync(new MouseEventData { EventType = MouseEventType.Down, X = x, Y = y, Button = ConvertButton(e.Button) });
        }

        private async void Screen_MouseUp(object sender, MouseEventArgs e)
        {
            if (!TryMapToRemote(e.Location, out int x, out int y)) return;
            await _client.SendMouseEventAsync(new MouseEventData { EventType = MouseEventType.Up, X = x, Y = y, Button = ConvertButton(e.Button) });
        }

        private async void Screen_MouseWheel(object sender, MouseEventArgs e)
        {
            if (!TryMapToRemote(e.Location, out int x, out int y)) return;
            await _client.SendMouseEventAsync(new MouseEventData { EventType = MouseEventType.Scroll, X = x, Y = y, Delta = e.Delta });
        }

        private async void ViewerForm_KeyDown(object sender, KeyEventArgs e)
        {
            await _client.SendKeyboardEventAsync(new KeyboardEventData { EventType = KeyboardEventType.KeyDown, KeyCode = e.KeyValue });
            e.Handled = true;
        }

        private async void ViewerForm_KeyUp(object sender, KeyEventArgs e)
        {
            await _client.SendKeyboardEventAsync(new KeyboardEventData { EventType = KeyboardEventType.KeyUp, KeyCode = e.KeyValue });
            e.Handled = true;
        }

        private bool _disconnecting;
        private async System.Threading.Tasks.Task DisconnectAndCloseAsync(bool fromFormClosing = false)
        {
            if (_disconnecting) return;
            _disconnecting = true;
            try { await _client.DisconnectAsync(); } catch { }
            if (!fromFormClosing)
            {
                Close();
            }
        }
    }
}
