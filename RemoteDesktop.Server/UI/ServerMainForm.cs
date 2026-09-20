using System;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using RemoteDesktop.Server.Network;
using RemoteDesktop.Shared.Protocol;

namespace RemoteDesktop.Server.UI
{
    public class ServerMainForm : Form
    {
        private ServerSocket _serverSocket;
        private ServerSession _activeSession;

        private TextBox _txtPort;
        private TextBox _txtPassword;
        private Button _btnGeneratePassword;
        private Button _btnStartStop;
        private Button _btnEmergencyStop;
        private Label _lblYourId;
        private Label _lblStatus;
        private ListBox _lstLog;

        private bool _isListening;

        public ServerMainForm()
        {
            BuildUi();
            _txtPassword.Text = GenerateRandomPassword();
            _lblYourId.Text = $"IP của bạn: {GetLocalIPv4()}";
        }

        private void BuildUi()
        {
            Text = "Remote Desktop - Máy đích (Server)";
            ClientSize = new Size(520, 420);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Segoe UI", 9.5f);

            var header = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.FromArgb(30, 30, 46) };
            var lblTitle = new Label
            {
                Text = "REMOTE DESKTOP SERVER",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 20)
            };
            header.Controls.Add(lblTitle);

            _lblYourId = new Label { Location = new Point(20, 85), AutoSize = true, Font = new Font("Segoe UI", 10f, FontStyle.Bold) };

            var lblPort = new Label { Text = "Cổng lắng nghe (Port):", Location = new Point(20, 120), AutoSize = true };
            _txtPort = new TextBox { Text = Constants.DefaultPort.ToString(), Location = new Point(180, 117), Width = 100 };

            var lblPassword = new Label { Text = "Mật khẩu:", Location = new Point(20, 155), AutoSize = true };
            _txtPassword = new TextBox { Location = new Point(180, 152), Width = 160 };
            _btnGeneratePassword = new Button { Text = "Tạo mới", Location = new Point(345, 151), Width = 80 };
            _btnGeneratePassword.Click += (s, e) => _txtPassword.Text = GenerateRandomPassword();

            _btnStartStop = new Button
            {
                Text = "Bắt đầu lắng nghe",
                Location = new Point(20, 195),
                Size = new Size(200, 40),
                BackColor = Color.FromArgb(46, 125, 250),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnStartStop.Click += BtnStartStop_Click;

            _btnEmergencyStop = new Button
            {
                Text = "DỪNG KHẨN CẤP",
                Location = new Point(300, 195),
                Size = new Size(200, 40),
                BackColor = Color.FromArgb(200, 45, 45),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            _btnEmergencyStop.Click += BtnEmergencyStop_Click;

            _lblStatus = new Label
            {
                Text = "Trạng thái: Chưa lắng nghe",
                Location = new Point(20, 245),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            var lblLog = new Label { Text = "Nhật ký:", Location = new Point(20, 275), AutoSize = true };
            _lstLog = new ListBox { Location = new Point(20, 297), Size = new Size(480, 100), IntegralHeight = false };

            Controls.Add(header);
            Controls.Add(_lblYourId);
            Controls.Add(lblPort);
            Controls.Add(_txtPort);
            Controls.Add(lblPassword);
            Controls.Add(_txtPassword);
            Controls.Add(_btnGeneratePassword);
            Controls.Add(_btnStartStop);
            Controls.Add(_btnEmergencyStop);
            Controls.Add(_lblStatus);
            Controls.Add(lblLog);
            Controls.Add(_lstLog);

            FormClosing += (s, e) => StopListening();
        }

        private void BtnStartStop_Click(object sender, EventArgs e)
        {
            if (!_isListening)
            {
                if (!int.TryParse(_txtPort.Text, out int port) || port <= 0 || port > 65535)
                {
                    MessageBox.Show(this, "Cổng không hợp lệ.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(_txtPassword.Text))
                {
                    MessageBox.Show(this, "Vui lòng nhập mật khẩu.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                StartListening(port);
            }
            else
            {
                StopListening();
            }
        }

        private void StartListening(int port)
        {
            _serverSocket = new ServerSocket(port);
            _serverSocket.OnClientConnected += OnClientConnected;
            _serverSocket.OnError += ex => LogAsync($"Lỗi socket: {ex.Message}");
            _serverSocket.Start();

            _isListening = true;
            _btnStartStop.Text = "Dừng lắng nghe";
            _txtPort.Enabled = false;
            _lblStatus.Text = $"Trạng thái: Đang lắng nghe ở cổng {port}...";
            Log($"Bắt đầu lắng nghe ở cổng {port}.");
        }

        private void StopListening()
        {
            _activeSession?.TriggerEmergencyStop();
            _serverSocket?.Stop();
            _isListening = false;
            if (!IsDisposed)
            {
                _btnStartStop.Text = "Bắt đầu lắng nghe";
                _txtPort.Enabled = true;
                _lblStatus.Text = "Trạng thái: Chưa lắng nghe";
                _btnEmergencyStop.Enabled = false;
            }
            Log("Đã dừng lắng nghe.");
        }

        private void OnClientConnected(TcpClient tcpClient)
        {
            var session = new ServerSession(tcpClient)
            {
                // TODO: cho phép chỉnh chất lượng / FPS từ UI nếu cần
            };
            session.ConnectRequestReceived += OnConnectRequestReceived;
            session.SessionStarted += OnSessionStarted;
            session.SessionEnded += OnSessionEnded;
            session.LogMessage += (s, msg) => LogAsync(msg);

            _ = session.RunAsync(_txtPassword.Text);
        }

        private void OnConnectRequestReceived(ServerSession session, string remoteEndPoint)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnConnectRequestReceived(session, remoteEndPoint)));
                return;
            }

            // Chỉ cho phép 1 phiên điều khiển tại một thời điểm
            if (_activeSession != null && _activeSession.IsActive)
            {
                session.Reject();
                Log($"Từ chối {remoteEndPoint}: đang có phiên điều khiển khác.");
                return;
            }

            using var dlg = new IncomingRequestForm(remoteEndPoint);
            var result = dlg.ShowDialog(this);
            if (result == DialogResult.OK && dlg.Accepted)
            {
                session.Accept();
            }
            else
            {
                session.Reject();
                Log($"Đã từ chối kết nối từ {remoteEndPoint}.");
            }
        }

        private void OnSessionStarted(ServerSession session)
        {
            if (InvokeRequired) { Invoke(new Action(() => OnSessionStarted(session))); return; }

            _activeSession = session;
            _btnEmergencyStop.Enabled = true;
            _lblStatus.Text = $"Trạng thái: Đang bị điều khiển bởi {session.RemoteEndPoint}";
            Log($"Phiên điều khiển bắt đầu với {session.RemoteEndPoint}.");
        }

        private void OnSessionEnded(ServerSession session, string reason)
        {
            if (InvokeRequired) { Invoke(new Action(() => OnSessionEnded(session, reason))); return; }

            if (_activeSession == session)
            {
                _activeSession = null;
                _btnEmergencyStop.Enabled = false;
                if (_isListening)
                {
                    _lblStatus.Text = $"Trạng thái: Đang lắng nghe ở cổng {_txtPort.Text}...";
                }
            }
            Log($"Kết thúc phiên với {session.RemoteEndPoint}: {reason}");
        }

        private void BtnEmergencyStop_Click(object sender, EventArgs e)
        {
            _activeSession?.TriggerEmergencyStop();
            Log("Người dùng đã bấm DỪNG KHẨN CẤP.");
        }

        private void Log(string message)
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            _lstLog.Items.Add(line);
            if (_lstLog.Items.Count > 0)
            {
                _lstLog.TopIndex = _lstLog.Items.Count - 1;
            }
        }

        private void LogAsync(string message)
        {
            if (InvokeRequired) { Invoke(new Action(() => Log(message))); }
            else { Log(message); }
        }

        private static string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var rnd = new Random();
            return new string(Enumerable.Range(0, 8).Select(_ => chars[rnd.Next(chars.Length)]).ToArray());
        }

        private static string GetLocalIPv4()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var ip = host.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
                return ip?.ToString() ?? "Không xác định";
            }
            catch
            {
                return "Không xác định";
            }
        }
    }
}
