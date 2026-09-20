using System;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using RemoteDesktop.Client.Network;
using RemoteDesktop.Shared.Protocol;

namespace RemoteDesktop.Client.UI
{
    public class MainForm : Form
    {
        private RemoteDesktopClient _client;

        private PictureBox _banner;
        private Label _lblYourIp;
        private TextBox _txtServerIp;
        private TextBox _txtPort;
        private TextBox _txtPassword;
        private Button _btnConnect;
        private Label _lblStatus;

        public MainForm()
        {
            BuildUi();
        }

        private void BuildUi()
        {
            Text = "Remote Desktop - Điều khiển từ xa";
            ClientSize = new Size(520, 480);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Segoe UI", 9.5f);

            var bannerImg = AppImages.TryLoad("LTMICON1.png");
            _banner = new PictureBox
            {
                Location = new Point(0, 0),
                Size = new Size(520, 120),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Image = bannerImg,
                BackColor = bannerImg == null ? Color.FromArgb(30, 30, 46) : Color.Black
            };
            if (bannerImg == null)
            {
                var lblTitle = new Label
                {
                    Text = "REMOTE DESKTOP",
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                    AutoSize = true,
                    Location = new Point(150, 40)
                };
                _banner.Controls.Add(lblTitle);
            }

            _lblYourIp = new Label
            {
                Text = $"Máy của bạn (IP): {GetLocalIPv4()}",
                Location = new Point(20, 135),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            var groupBox = new GroupBox
            {
                Text = "Kết nối tới máy đích",
                Location = new Point(20, 165),
                Size = new Size(480, 200)
            };

            var lblIp = new Label { Text = "Địa chỉ IP:", Location = new Point(20, 35), AutoSize = true };
            _txtServerIp = new TextBox { Text = "127.0.0.1", Location = new Point(150, 32), Width = 200 };

            var lblPort = new Label { Text = "Cổng (Port):", Location = new Point(20, 70), AutoSize = true };
            _txtPort = new TextBox { Text = Constants.DefaultPort.ToString(), Location = new Point(150, 67), Width = 200 };

            var lblPassword = new Label { Text = "Mật khẩu:", Location = new Point(20, 105), AutoSize = true };
            _txtPassword = new TextBox { Location = new Point(150, 102), Width = 200, PasswordChar = '\u25CF' };

            _btnConnect = new Button
            {
                Text = "KẾT NỐI",
                Location = new Point(150, 145),
                Size = new Size(160, 40),
                BackColor = Color.FromArgb(46, 125, 250),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnConnect.Click += BtnConnect_Click;

            groupBox.Controls.Add(lblIp);
            groupBox.Controls.Add(_txtServerIp);
            groupBox.Controls.Add(lblPort);
            groupBox.Controls.Add(_txtPort);
            groupBox.Controls.Add(lblPassword);
            groupBox.Controls.Add(_txtPassword);
            groupBox.Controls.Add(_btnConnect);

            _lblStatus = new Label
            {
                Text = "",
                Location = new Point(20, 380),
                AutoSize = false,
                Size = new Size(480, 60),
                ForeColor = Color.DarkRed,
                Font = new Font("Segoe UI", 9f)
            };

            Controls.Add(_banner);
            Controls.Add(_lblYourIp);
            Controls.Add(groupBox);
            Controls.Add(_lblStatus);
        }

        private async void BtnConnect_Click(object sender, EventArgs e)
        {
            string ip = _txtServerIp.Text.Trim();
            string password = _txtPassword.Text;

            if (string.IsNullOrWhiteSpace(ip))
            {
                ShowError("Vui lòng nhập địa chỉ IP máy đích.");
                return;
            }
            if (!int.TryParse(_txtPort.Text.Trim(), out int port) || port <= 0 || port > 65535)
            {
                ShowError("Cổng không hợp lệ.");
                return;
            }

            _btnConnect.Enabled = false;
            _lblStatus.ForeColor = Color.DarkRed;
            _lblStatus.Text = "";

            _client = new RemoteDesktopClient();

            using var connectingForm = new ConnectingForm();
            connectingForm.Shown += async (s, args) =>
            {
                var (success, message) = await _client.ConnectAsync(ip, port, password);
                connectingForm.Invoke(new Action(() => connectingForm.Close()));

                if (!success)
                {
                    ShowError(string.IsNullOrEmpty(message) ? "Không thể kết nối." : message);
                    _btnConnect.Enabled = true;
                    return;
                }

                var viewer = new ViewerForm(_client, $"{ip}:{port}");
                viewer.FormClosed += (vs, ve) => _btnConnect.Enabled = true;
                viewer.Show();
            };
            connectingForm.ShowDialog(this);
        }

        private void ShowError(string message)
        {
            if (InvokeRequired) { Invoke(new Action(() => ShowError(message))); return; }
            _lblStatus.ForeColor = Color.DarkRed;
            _lblStatus.Text = message;
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
