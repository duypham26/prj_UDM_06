using System;
using System.Drawing;
using System.Windows.Forms;
using RemoteDesktop.Client.Network;
using RemoteDesktop.Shared.Models;
using RemoteDesktop.Shared.Network;

namespace RemoteDesktop.Client.UI
{
    public partial class MainForm : Form
    {
        private RemoteDesktopClient _client;
        private PictureBox _screenPictureBox;
        private Button _connectButton;
        private Button _disconnectButton;
        private TextBox _serverIPTextBox;
        private TextBox _portTextBox;
        private TextBox _passwordTextBox;
        private Label _statusLabel;
        private Timer _refreshTimer;

        public MainForm()
        {
            InitializeComponent();
            _client = new RemoteDesktopClient();
            SetupEvents();
            SetupTimer();
        }

        private void InitializeComponent()
        {
            this.Text = "Remote Desktop Client";
            this.Size = new Size(1024, 768);
            this.StartPosition = FormStartPosition.CenterScreen;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10)
            };

            var controlPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(5)
            };

            _serverIPTextBox = new TextBox { Text = "127.0.0.1", Width = 150, Margin = new Padding(5) };
            _portTextBox = new TextBox { Text = NetworkConstants.DefaultPort.ToString(), Width = 80, Margin = new Padding(5) };
            _passwordTextBox = new TextBox { Text = "", Width = 120, PasswordChar = '*', Margin = new Padding(5) };
            _connectButton = new Button { Text = "Connect", Width = 100, Margin = new Padding(5) };
            _disconnectButton = new Button { Text = "Disconnect", Width = 100, Margin = new Padding(5), Enabled = false };
            _statusLabel = new Label { Text = "Status: Disconnected", AutoSize = true, Margin = new Padding(10, 5, 5, 5) };

            controlPanel.Controls.AddRange(new Control[] {
                new Label { Text = "Server IP:", AutoSize = true, Margin = new Padding(5) },
                _serverIPTextBox,
                new Label { Text = "Port:", AutoSize = true, Margin = new Padding(5) },
                _portTextBox,
                new Label { Text = "Password:", AutoSize = true, Margin = new Padding(5) },
                _passwordTextBox,
                _connectButton,
                _disconnectButton,
                _statusLabel
            });

            _screenPictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle
            };

            mainPanel.Controls.Add(controlPanel, 0, 0);
            mainPanel.Controls.Add(_screenPictureBox, 0, 1);
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            this.Controls.Add(mainPanel);

            _connectButton.Click += ConnectButton_Click;
            _disconnectButton.Click += DisconnectButton_Click;
            _screenPictureBox.MouseMove += ScreenPictureBox_MouseMove;
            _screenPictureBox.MouseDown += ScreenPictureBox_MouseDown;
            _screenPictureBox.MouseUp += ScreenPictureBox_MouseUp;
            _screenPictureBox.MouseWheel += ScreenPictureBox_MouseWheel;
            this.KeyDown += MainForm_KeyDown;
            this.KeyUp += MainForm_KeyUp;
            this.FormClosing += MainForm_FormClosing;
        }

        private void SetupEvents()
        {
            _client.ScreenDataReceived += OnScreenDataReceived;
            _client.ConnectionStatusChanged += OnConnectionStatusChanged;
            _client.ErrorOccurred += OnErrorOccurred;
        }

        private void SetupTimer()
        {
            _refreshTimer = new Timer();
            _refreshTimer.Interval = 100;
            _refreshTimer.Tick += RefreshTimer_Tick;
        }

        private async void ConnectButton_Click(object sender, EventArgs e)
        {
            try
            {
                _connectButton.Enabled = false;
                _connectButton.Text = "Connecting...";
                _statusLabel.Text = "Status: Connecting...";

                string serverIP = _serverIPTextBox.Text;
                int port = int.Parse(_portTextBox.Text);
                string password = _passwordTextBox.Text;

                var success = await _client.ConnectAsync(serverIP, port, password);

                if (success)
                {
                    _statusLabel.Text = "Status: Connected";
                    _connectButton.Enabled = false;
                    _disconnectButton.Enabled = true;
                    _refreshTimer.Start();
                }
                else
                {
                    _statusLabel.Text = "Status: Connection Failed";
                    _connectButton.Enabled = true;
                    _connectButton.Text = "Connect";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _connectButton.Enabled = true;
                _connectButton.Text = "Connect";
                _statusLabel.Text = "Status: Error";
            }
        }

        private async void DisconnectButton_Click(object sender, EventArgs e)
        {
            await _client.DisconnectAsync();
            _refreshTimer.Stop();
            _connectButton.Enabled = true;
            _connectButton.Text = "Connect";
            _disconnectButton.Enabled = false;
            _statusLabel.Text = "Status: Disconnected";
            _screenPictureBox.Image = null;
        }

        private async void RefreshTimer_Tick(object sender, EventArgs e)
        {
            await _client.RequestScreenAsync();
        }

        private void OnScreenDataReceived(object sender, ScreenData screenData)
        {
            if (screenData != null && screenData.ImageData != null)
            {
                try
                {
                    using var ms = new System.IO.MemoryStream(screenData.ImageData);
                    var image = Image.FromStream(ms);

                    if (_screenPictureBox.Image != null)
                    {
                        _screenPictureBox.Image.Dispose();
                    }

                    _screenPictureBox.Image = (Image)image.Clone();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error displaying screen: {ex.Message}");
                }
            }
        }

        private void OnConnectionStatusChanged(object sender, ConnectionStatus status)
        {
            // Update UI if needed
        }

        private void OnErrorOccurred(object sender, Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Remote Desktop Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private async void ScreenPictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (_client.Status != ConnectionStatus.Authenticated) return;

            var mouseData = new MouseEventData
            {
                EventType = MouseEventType.Move,
                X = e.X,
                Y = e.Y,
                Button = MouseButton.Left,
                Delta = 0
            };
            await _client.SendMouseEventAsync(mouseData);
        }

        private async void ScreenPictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (_client.Status != ConnectionStatus.Authenticated) return;

            var button = ConvertMouseButton(e.Button);
            var mouseData = new MouseEventData
            {
                EventType = MouseEventType.Down,
                X = e.X,
                Y = e.Y,
                Button = button,
                Delta = 0
            };
            await _client.SendMouseEventAsync(mouseData);
        }

        private async void ScreenPictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (_client.Status != ConnectionStatus.Authenticated) return;

            var button = ConvertMouseButton(e.Button);
            var mouseData = new MouseEventData
            {
                EventType = MouseEventType.Up,
                X = e.X,
                Y = e.Y,
                Button = button,
                Delta = 0
            };
            await _client.SendMouseEventAsync(mouseData);
        }

        private async void ScreenPictureBox_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_client.Status != ConnectionStatus.Authenticated) return;

            var mouseData = new MouseEventData
            {
                EventType = MouseEventType.Scroll,
                X = e.X,
                Y = e.Y,
                Button = MouseButton.Left,
                Delta = e.Delta
            };
            await _client.SendMouseEventAsync(mouseData);
        }

        private async void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (_client.Status != ConnectionStatus.Authenticated) return;

            var keyData = new KeyboardEventData
            {
                KeyCode = e.KeyValue,
                KeyChar = (char)e.KeyValue,
                EventType = KeyboardEventType.KeyDown,
                IsSystemKey = e.Alt || e.Control || e.Shift
            };
            await _client.SendKeyboardEventAsync(keyData);
        }

        private async void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            if (_client.Status != ConnectionStatus.Authenticated) return;

            var keyData = new KeyboardEventData
            {
                KeyCode = e.KeyValue,
                KeyChar = (char)e.KeyValue,
                EventType = KeyboardEventType.KeyUp,
                IsSystemKey = e.Alt || e.Control || e.Shift
            };
            await _client.SendKeyboardEventAsync(keyData);
        }

        private MouseButton ConvertMouseButton(MouseButtons button)
        {
            switch (button)
            {
                case MouseButtons.Left: return MouseButton.Left;
                case MouseButtons.Right: return MouseButton.Right;
                case MouseButtons.Middle: return MouseButton.Middle;
                default: return MouseButton.Left;
            }
        }

        private async void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _refreshTimer.Stop();
            await _client.DisconnectAsync();
        }
    }
}