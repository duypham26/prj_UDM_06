# Remote Desktop Protocol

## 1. Giới thiệu

Protocol dùng để quy định cách Client và Server trao đổi dữ liệu
trong ứng dụng Remote Desktop.

Ứng dụng sử dụng TCP để truyền dữ liệu.

## 2. Phiên bản

Protocol version hiện tại:

1

## 3. Cấu trúc Packet

Một Packet gồm 2 phần chính:

- Type: cho biết loại dữ liệu.
- Data: dữ liệu đi kèm.

Ví dụ:

MouseMove
Data = "100,200"

Có nghĩa là chuột được di chuyển tới vị trí X = 100, Y = 200.

## 4. PacketType

### Kết nối

| PacketType | Ý nghĩa |
|---|---|
| ConnectRequest | Yêu cầu kết nối |
| ConnectAccept | Chấp nhận kết nối |
| ConnectReject | Từ chối kết nối |

### Màn hình

| PacketType | Ý nghĩa |
|---|---|
| ScreenFrame | Hình ảnh màn hình |

### Chuột

| PacketType | Ý nghĩa |
|---|---|
| MouseMove | Di chuyển chuột |
| MouseDown | Nhấn chuột |
| MouseUp | Thả chuột |
| MouseWheel | Cuộn chuột |

### Bàn phím

| PacketType | Ý nghĩa |
|---|---|
| KeyDown | Nhấn phím |
| KeyUp | Thả phím |

### Phiên điều khiển

| PacketType | Ý nghĩa |
|---|---|
| Disconnect | Kết thúc kết nối |
| EmergencyStop | Máy đích dừng phiên khẩn cấp |

### Kiểm tra kết nối

| PacketType | Ý nghĩa |
|---|---|
| Heartbeat | Kiểm tra kết nối |

## 5. Quy trình kết nối

Máy điều khiển gửi yêu cầu:

Controller -> Server

ConnectRequest

Server chuyển yêu cầu đến máy đích.

Máy đích hiển thị yêu cầu cho người dùng.

Nếu người dùng chấp nhận:

Target -> Server

ConnectAccept

Nếu người dùng từ chối:

Target -> Server

ConnectReject

Chỉ khi nhận được ConnectAccept thì phiên điều khiển mới được bắt đầu.

## 6. Truyền màn hình

Sau khi phiên điều khiển được chấp nhận:

Server -> Controller

ScreenFrame

ScreenFrame chứa dữ liệu hình ảnh màn hình.

## 7. Điều khiển chuột

Controller -> Server

Có thể sử dụng:

- MouseMove
- MouseDown
- MouseUp
- MouseWheel

## 8. Điều khiển bàn phím

Controller -> Server

Có thể sử dụng:

- KeyDown
- KeyUp

## 9. Dừng phiên khẩn cấp

Máy đích luôn có quyền dừng phiên.

Khi người dùng nhấn nút dừng khẩn cấp:

Target -> Server

EmergencyStop

Sau khi nhận EmergencyStop:

- Dừng nhận thao tác chuột từ xa.
- Dừng nhận thao tác bàn phím từ xa.
- Kết thúc phiên điều khiển.

## 10. Mất kết nối

Nếu kết nối TCP bị mất:

- Phiên điều khiển phải kết thúc.
- Không tiếp tục xử lý input từ xa.
- Giải phóng tài nguyên.
- Đóng kết nối.

## 11. Heartbeat

Heartbeat được dùng để kiểm tra kết nối giữa Client và Server.

Nếu kết nối không còn hoạt động thì phiên điều khiển phải được kết thúc.