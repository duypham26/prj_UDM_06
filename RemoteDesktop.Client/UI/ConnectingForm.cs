using System;
using System.Drawing;
using System.Windows.Forms;

namespace RemoteDesktop.Client.UI
{
    /// <summary>
    /// Hộp thoại nhỏ hiển thị "Đang kết nối..." trong lúc chờ máy đích
    /// chấp nhận/từ chối yêu cầu điều khiển. Có nút Hủy.
    /// </summary>
    public class ConnectingForm : Form
    {
        public bool CancelRequested { get; private set; }

        private readonly Label _lblMessage;

        public ConnectingForm()
        {
            Text = "Đang kết nối";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(340, 140);
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;

            _lblMessage = new Label
            {
                Text = "Đang kết nối tới máy đích...\r\nVui lòng chờ máy đích xác nhận.",
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 20),
                Size = new Size(300, 50),
                Font = new Font("Segoe UI", 9.5f)
            };

            var progress = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Location = new Point(20, 75),
                Size = new Size(300, 20)
            };

            var btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(120, 100),
                Size = new Size(100, 30)
            };
            btnCancel.Click += (s, e) =>
            {
                CancelRequested = true;
                DialogResult = DialogResult.Cancel;
                Close();
            };

            Controls.Add(_lblMessage);
            Controls.Add(progress);
            Controls.Add(btnCancel);
        }

        public void SetMessage(string text)
        {
            if (InvokeRequired) { Invoke(new Action(() => SetMessage(text))); return; }
            _lblMessage.Text = text;
        }
    }
}
