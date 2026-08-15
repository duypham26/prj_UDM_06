using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteDesktop.Shared.Protocol
{
    public enum PacketType
    {
        //kết nối
        ConnectRequest,
        ConnectAccept,
        ConnectReject,
        //màn hình
        ScreenFrame,
        //điều khiển chuột
        MouseMove,
        MouseDown,
        MouseUp,
        MouseWheel,
        // điều khiển bàn phím
        KeyDown,
        KeyUp,
        //phiên điều khiển 
        Disconnect,
        EmergencyStop, 
        //kiểm tra kết nối
        Heartbeat
    }
}
