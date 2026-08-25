using System;
using System.Net;

namespace RemoteDesktop.Shared.Models
{
    public class ConnectionInfo
    {
        public string ServerId { get; set; }
        public string ClientId { get; set; }
        public string ServerIP { get; set; }
        public int Port { get; set; }
        public string Password { get; set; }
        public ConnectionStatus Status { get; set; }
        public DateTime ConnectionTime { get; set; }
    }

    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Failed,
        Authenticated
    }

    public class ScreenData
    {
        public byte[] ImageData { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public long Timestamp { get; set; }
    }

    public class MouseEventData
    {
        public MouseEventType EventType { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public MouseButton Button { get; set; }
        public int Delta { get; set; }
    }

    public enum MouseEventType
    {
        Move,
        Down,
        Up,
        Click,
        DoubleClick,
        Scroll
    }

    public enum MouseButton
    {
        Left,
        Right,
        Middle
    }

    public class KeyboardEventData
    {
        public KeyboardEventType EventType { get; set; }
        public int KeyCode { get; set; }
        public char KeyChar { get; set; }
        public bool IsSystemKey { get; set; }
    }

    public enum KeyboardEventType
    {
        KeyDown,
        KeyUp,
        KeyPress
    }
}