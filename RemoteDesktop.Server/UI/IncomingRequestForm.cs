using System.Drawing;
using System.Windows.Forms;

namespace RemoteDesktop.Server.UI
{
    /// <summary>
    /// Hộp thoại hiển thị cho người dùng máy đích khi có máy điều khiển
    /// xin kết nối tới. Người dùng phải bấm Đồng ý hoặc Từ chối.
    /// </summary>
    public class IncomingRequestForm : Form
    {
        public bool Accepted { get; private set; }

        public IncomingRequestForm(string remoteEndPoint)
        {
            Text = "Yêu cầu kết nối";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(380, 170);
            MaximizeBox = false;
            MinimizeBox = false;
            TopMost = true;

            var lblIcon = new Label
            {
                Text = "🖥",
                Font = new Font("Segoe UI Emoji", 28f),
                AutoSize = true,
                Location = new Point(20, 20)
            };

            var lblMessage = new Label
            {
                Text = $"Máy có địa chỉ:\r\n{remoteEndPoint}\r\n\r\nđang yêu cầu điều khiển máy tính của bạn.\r\nBạn có đồng ý không?",
                AutoSize = false,
                Location = new Point(90, 15),
                Size = new Size(270, 90),
                Font = new Font("Segoe UI", 9.5f)
            };

            var btnAccept = new Button
            {
                Text = "Đồng ý",
                Location = new Point(90, 115),
                Size = new Size(110, 36),
                BackColor = Color.FromArgb(46, 160, 67),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnAccept.Click += (s, e) => { Accepted = true; DialogResult = DialogResult.OK; Close(); };

            var btnReject = new Button
            {
                Text = "Từ chối",
                Location = new Point(230, 115),
                Size = new Size(110, 36),
                BackColor = Color.FromArgb(200, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnReject.Click += (s, e) => { Accepted = false; DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(lblIcon);
            Controls.Add(lblMessage);
            Controls.Add(btnAccept);
            Controls.Add(btnReject);

            AcceptButton = btnAccept;
            CancelButton = btnReject;
        }
    }
}
