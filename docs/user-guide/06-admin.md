# 06 — Quản trị hệ thống

Dành cho vai trò: **Admin** (Quản trị viên phòng khám)

---

## 1. Tổng quan Quản trị

Vào **Quản trị** → trang Dashboard quản trị gồm:
- Thống kê người dùng, trạng thái hệ thống
- Lối tắt đến các trang cấu hình

---

## 2. Quản lý người dùng

### Xem danh sách người dùng
**Quản trị** → **Người dùng**: danh sách tất cả tài khoản trong phòng khám.

### Mời người dùng mới
1. Click **Mời người dùng**
2. Nhập Email, Họ tên
3. Chọn Vai trò (có thể gán nhiều vai trò)
4. Click **Gửi lời mời** → email mời tự động gửi
5. Người được mời có 48 giờ để kích hoạt tài khoản

### Khoá / Mở khoá tài khoản
- Click menu ⋮ bên cạnh tên người dùng → **Khoá tài khoản**
- Người dùng bị khoá không thể đăng nhập

### Gán vai trò
- Vào chi tiết người dùng → **Gán vai trò** → chọn/bỏ chọn vai trò

---

## 3. Phân quyền (Vai trò)

**Quản trị** → **Vai trò & Quyền hạn**

### Vai trò hệ thống (không xoá được)
| Vai trò | Quyền |
|---|---|
| Admin | Toàn quyền |
| BacSi | Khám bệnh, kê đơn, EMR |
| LeTan | Tiếp đón, hồ sơ BN |
| DuocSi | Kho dược, phát thuốc |
| KeToan | Thu ngân, báo cáo tài chính |
| DieuDuong | Sinh hiệu, kết quả XN |
| KyThuatVien | Kết quả XN, CĐHA |

### Tạo vai trò tuỳ chỉnh
1. Click **Tạo vai trò mới**
2. Nhập tên vai trò, mô tả
3. Tích chọn từng quyền trong **Ma trận quyền hạn**
4. Lưu → vai trò có thể gán cho người dùng ngay

---

## 4. Cấu hình BHYT

**Quản trị** → **BHYT**:

| Mục | Mô tả |
|---|---|
| Mã CSKCB | Mã cơ sở khám chữa bệnh do BYT cấp |
| Tên CSKCB | Tên đầy đủ |
| Tuyến KCB | Tuyến 1 / 2 / 3 / 4 |
| Hạng BV | Hạng I / II / III / IV |
| Số hợp đồng BHYT | Số hợp đồng với BHXH |
| Ngày hiệu lực | Ngày ký hợp đồng |

Lưu lại để hệ thống dùng khi export XML BHYT.

---

## 5. Xuất XML BHYT

**BHYT** trên sidebar:

### Tạo kỳ xuất
1. Click **Tạo kỳ mới**
2. Chọn kỳ (YYYY-MM)
3. Hệ thống tổng hợp tất cả lượt khám BHYT trong kỳ

### Quy trình xuất
```
Tạo nháp → Sinh XML → Validate XSD → Ký số → Gửi cổng BHYT
```

### Đối soát kết quả
Sau khi BHXH trả kết quả:
1. Tab **Đối soát** → **Tải kết quả giám định**
2. Upload file kết quả từ cổng BHYT
3. Hệ thống so sánh và hiển thị các dòng được duyệt/từ chối
4. Khiếu nại các dòng từ chối nếu cần

---

## 6. Cấu hình Đơn thuốc Quốc gia (ĐTQG)

**Quản trị** → **ĐTQG**:

1. Nhập **Mã cơ sở KCB**, **Token** từ cổng donthuocquocgia.vn
2. Test kết nối để xác nhận
3. Lưu → hệ thống tự động đẩy đơn thuốc khi bác sĩ ký

---

## 7. Cấu hình thông báo

**Quản trị** → **Cấu hình thông báo**:

- Bật/tắt VAPID key cho Web Push
- Cài đặt vị trí hiển thị toast notification
- Cài đặt âm thanh thông báo

---

## 8. Nhật ký thao tác (Audit Log)

**Quản trị** → **Nhật ký**:

- Theo dõi mọi CREATE/UPDATE/DELETE trên dữ liệu bệnh nhân
- Lọc theo: người dùng, loại thao tác, ngày, đối tượng
- Xuất báo cáo kiểm toán

---

## 9. API Partners

**Quản trị** → **API Partners**: quản lý tích hợp bên thứ 3 qua Public API.

- Tạo API key cho đối tác
- Cài đặt phạm vi (scope), rate limit, hạn mức ngày
- Xem log yêu cầu API
