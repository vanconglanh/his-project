# 01 — Lễ tân / Tiếp đón bệnh nhân

Dành cho vai trò: **LeTan** (Lễ tân)

---

## 1. Màn hình Tiếp đón

Vào menu **Tiếp đón** (`g r` hoặc sidebar). Màn hình gồm:

- **Bảng hàng đợi** (Queue Board): toàn bộ bệnh nhân đang chờ/đang khám hôm nay
- **Nút Tiếp đón** (F2): tạo lượt tiếp đón mới
- **Thống kê nhanh**: số đang chờ, đang khám, đã xong

Hệ thống tự động làm mới dữ liệu mỗi 5 giây.

---

## 2. Tiếp đón bệnh nhân mới

### Bước 1: Mở form tiếp đón
- Nhấn nút **Tiếp đón** hoặc phím `F2`

### Bước 2: Tìm bệnh nhân
- Gõ tên, số điện thoại, CMND, hoặc mã bệnh nhân vào ô tìm kiếm
- Chọn bệnh nhân từ danh sách kết quả
- Nếu bệnh nhân chưa có hồ sơ: click **"Tạo bệnh nhân mới"**

### Bước 3: Điền thông tin tiếp đón
| Trường | Bắt buộc | Ghi chú |
|---|---|---|
| Bệnh nhân | Có | Tìm hoặc tạo mới |
| Phòng khám | Có | Chọn từ danh sách phòng |
| Dịch vụ | Không | Gói khám, dịch vụ CLS |
| Lý do khám | Không | Mô tả ngắn |
| Ưu tiên | Có | Thông thường / Ưu tiên / Khẩn cấp |
| Ghi chú | Không | Ghi chú nội bộ |

### Bước 4: Xác nhận
- Nhấn **Tiếp đón** để tạo phiếu → hệ thống tự in số thứ tự

---

## 3. Tạo hồ sơ bệnh nhân mới

Từ form tiếp đón hoặc vào **Bệnh nhân** → **Tạo bệnh nhân mới**:

| Trường | Bắt buộc |
|---|---|
| Họ và tên | Có |
| Giới tính | Có |
| Ngày sinh | Có |
| Số điện thoại | Không |
| CMND/CCCD | Không |
| Địa chỉ | Không |
| Nhóm máu | Không |

Thẻ BHYT có thể bổ sung sau trong hồ sơ bệnh nhân.

---

## 4. Quản lý hàng đợi

### Gọi bệnh nhân vào khám
1. Tìm tên bệnh nhân trên bảng hàng đợi
2. Click **Gọi vào** → trạng thái chuyển sang "Đang khám"

### Bỏ qua lượt
- Click **Bỏ qua** → bệnh nhân xuống cuối hàng đợi

### Huỷ lượt
- Click **Huỷ** → nhập lý do huỷ → xác nhận

---

## 5. In phiếu số thứ tự

Sau khi tiếp đón thành công:
- Hệ thống hiện dialog xem trước phiếu
- Click **In phiếu** hoặc dùng `Ctrl+P`

Phiếu gồm: tên bệnh nhân, số thứ tự, phòng khám, giờ tiếp đón, mã QR.

---

## 6. Phím tắt lễ tân

| Phím | Chức năng |
|---|---|
| `F2` | Mở form tiếp đón mới |
| `F4` | Lưu form đang mở |
| `Esc` | Đóng dialog / huỷ |
| `Ctrl+K` | Tìm kiếm bệnh nhân nhanh |
