# Ghi chú - những gì mình đã làm

## Mở project
Mở file `RemoteDesktop.sln` bằng Visual Studio 2022 (bản 17.8 trở lên để chắc chắn có SDK .NET 8).
Nếu máy chưa có .NET 8 SDK, cài tại: https://dotnet.microsoft.com/download/dotnet/8.0
(VS2022 mới thường có sẵn workload ".NET desktop development" là đủ).

Solution gồm 3 project:
- **RemoteDesktop.Shared** – thư viện dùng chung (giao thức, model, helper nén/ảnh).
- **RemoteDesktop.Server** – app WinForms chạy trên máy **bị điều khiển** (máy đích).
- **RemoteDesktop.Client** – app WinForms chạy trên máy **đi điều khiển**.

Chạy thử: đặt Server làm Startup Project, bấm F5 để mở app Server trước (bấm "Bắt đầu lắng nghe"),
sau đó chạy Client (Debug > project khác, hoặc mở thêm 1 instance từ thư mục bin), nhập đúng IP/Port/mật khẩu hiển thị bên Server rồi bấm KẾT NỐI. Bên Server sẽ hiện hộp thoại xin xác nhận, bấm Đồng ý để bắt đầu điều khiển.

## Vì sao phải viết lại nhiều thay vì chỉ "vá thêm"
Project gốc bị vài lỗi khiến **không build được**, nên mình không thể chỉ thêm phần thiếu:
- `RemoteDesktop.Client`: có 2 class UI trùng ý tưởng nhưng khác tên (`MainForm` viết tay và
  `Mainform` do Designer sinh ra, khác nhau ở chữ hoa/thường) → Designer thiếu code xử lý sự kiện,
  không build được. File `Network/RemoteDesktopClient.cs` cũng chưa được thêm vào csproj nên
  chưa từng được biên dịch.
- `NewRemoteDesktop.Server`: có 2 project con (`ScreenCapture/`, `ImageEncoder/`) nằm lồng bên trong,
  bị tự động gộp code trùng lặp với file ở ngoài, cộng thêm file `ScreenCapture.cs` bị lỗi cú pháp
  (thừa 1 ký tự "C" ở đầu dòng `using`). Server cũng mới chỉ là 1 console app echo gói tin, chưa có
  giao diện xác nhận kết nối, chưa gọi chụp màn hình / giả lập chuột-bàn phím.
- `RemoteDesktop.Shared` không được project Server tham chiếu tới dù dùng chung model.

Mình đã giữ nguyên ý tưởng thiết kế ban đầu (tên gói tin trong `Docs/Protocol.md`, cấu trúc
Client/Server/Shared, các class MouseController/KeyboardController/ScreenCapture...) nhưng viết lại
để mọi thứ khớp nhau và build chạy được.

## Những gì đã hoàn thiện
- Giao thức mạng thống nhất trong `RemoteDesktop.Shared/Protocol/PacketFramer.cs` +
  `PayloadCodec.cs`, đúng theo các loại gói tin mô tả trong `Docs/Protocol.md`
  (ConnectRequest/Accept/Reject, ScreenFrame, Mouse*, Key*, Disconnect, EmergencyStop, Heartbeat).
- **Server**: giao diện WinForms hiển thị IP/Port/mật khẩu, nút "Bắt đầu lắng nghe", hộp thoại
  xin xác nhận khi có người xin kết nối, nút "DỪNG KHẨN CẤP", chụp màn hình + gửi liên tục (~10 FPS,
  JPEG chất lượng 50 - chỉnh trong `ServerSession.JpegQuality`/`FrameIntervalMs`), nhận thao tác
  chuột/bàn phím từ xa và giả lập bằng Win32 API.
- **Client**: màn hình nhập IP/Port/mật khẩu, hộp thoại "Đang kết nối...", cửa sổ xem/điều khiển
  màn hình từ xa (tự quy đổi toạ độ chuột theo tỉ lệ ảnh), nút Ngắt kết nối.
- Chỉ cho phép 1 phiên điều khiển tại 1 thời điểm trên Server (đúng tinh thần thiết kế ban đầu).

## Giới hạn còn lại (bạn có thể phát triển thêm)
- Chưa mã hoá đường truyền (chỉ TCP thường) và chưa có xác thực người dùng nhiều lớp – chỉ có mật khẩu 1 lớp.
- Chỉ chạy được trong cùng mạng LAN hoặc khi đã mở port/port-forward, chưa có NAT traversal.
- Bàn phím gửi theo mã phím ảo (VK code) nên gõ tiếng Việt có dấu qua remote sẽ không chính xác.
- Chưa có clipboard dùng chung, chưa có truyền file.
- Các Form UI được viết trực tiếp bằng code (không tách file `.Designer.cs`) để tránh đúng lỗi
  "class trùng tên khác hoa/thường" đã gặp ở bản gốc — bạn vẫn sửa được bình thường trong code,
  chỉ là sẽ không kéo-thả được trên khung thiết kế (Designer view) của Visual Studio.
