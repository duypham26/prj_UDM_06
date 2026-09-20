using RemoteDesktop.Shared.Models;

namespace RemoteDesktop.Server.Input
{
    /// <summary>
    /// Nhận các sự kiện chuột/bàn phím đã giải mã từ mạng và thực thi
    /// trên máy đích thông qua MouseController / KeyboardController.
    /// </summary>
    public static class InputHandler
    {
        public static void HandleMouse(MouseEventData data)
        {
            switch (data.EventType)
            {
                case MouseEventType.Move:
                    MouseController.Move(data.X, data.Y);
                    break;
                case MouseEventType.Down:
                    MouseController.Move(data.X, data.Y);
                    MouseController.Down(data.Button);
                    break;
                case MouseEventType.Up:
                    MouseController.Move(data.X, data.Y);
                    MouseController.Up(data.Button);
                    break;
                case MouseEventType.Scroll:
                    MouseController.Move(data.X, data.Y);
                    MouseController.Scroll(data.Delta);
                    break;
            }
        }

        public static void HandleKeyboard(KeyboardEventData data)
        {
            switch (data.EventType)
            {
                case KeyboardEventType.KeyDown:
                    KeyboardController.KeyDown(data.KeyCode);
                    break;
                case KeyboardEventType.KeyUp:
                    KeyboardController.KeyUp(data.KeyCode);
                    break;
            }
        }
    }
}
