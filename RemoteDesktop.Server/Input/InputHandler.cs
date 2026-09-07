namespace RemoteDesktop.Server.Input
{
    public enum InputType
    {
        MouseMove,
        LeftDown,
        LeftUp,
        RightDown,
        RightUp,
        Scroll,
        KeyDown,
        KeyUp
    }

    public class InputPayload
    {
        public InputType Type { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int ScrollAmount { get; set; }
        public byte KeyCode { get; set; }
    }

    public class InputHandler
    {
        public void ProcessInput(InputPayload payload)
        {
            switch (payload.Type)
            {
                case InputType.MouseMove:
                    MouseController.Move(payload.X, payload.Y);
                    break;

                case InputType.LeftDown:
                    MouseController.Move(payload.X, payload.Y);
                    MouseController.LeftDown();
                    break;

                case InputType.LeftUp:
                    MouseController.Move(payload.X, payload.Y);
                    MouseController.LeftUp();
                    break;

                case InputType.RightDown:
                    MouseController.Move(payload.X, payload.Y);
                    MouseController.RightDown();
                    break;

                case InputType.RightUp:
                    MouseController.Move(payload.X, payload.Y);
                    MouseController.RightUp();
                    break;

                case InputType.Scroll:
                    MouseController.Scroll(payload.ScrollAmount);
                    break;

                case InputType.KeyDown:
                    KeyboardController.KeyDown(payload.KeyCode);
                    break;

                case InputType.KeyUp:
                    KeyboardController.KeyUp(payload.KeyCode);
                    break;
            }
        }
    }
}