# 00 — Bắt đầu với Pro-Diab HIS

## 1. Đăng nhập lần đầu

1. Mở trình duyệt (Chrome, Firefox, Safari) và truy cập URL phòng khám của bạn:  
   `https://<ten-phong-kham>.prodiab.vn`
2. Nhập **Email** và **Mật khẩu** đã được quản trị viên gửi qua email mời.
3. Lần đầu đăng nhập, hệ thống yêu cầu bạn **đặt lại mật khẩu mới**.

### Yêu cầu mật khẩu
- Tối thiểu **12 ký tự**
- Gồm: chữ hoa, chữ thường, chữ số, ký tự đặc biệt (`!@#$%^&*`)
- Không được trùng với 3 mật khẩu gần nhất

---

## 2. Đổi mật khẩu

1. Click vào **avatar** góc trên phải → **Tài khoản** → **Bảo mật**.
2. Nhập mật khẩu hiện tại và mật khẩu mới.
3. Nhấn **Lưu**.

---

## 3. Bật xác thực 2 lớp (2FA) — Khuyến nghị

Xác thực 2 lớp (TOTP) tăng cường bảo mật tài khoản. Thực hiện:

1. Vào **Tài khoản** → **Bảo mật** → **Bật 2FA**.
2. Tải ứng dụng Authenticator trên điện thoại (Google Authenticator, Authy, Microsoft Authenticator).
3. Quét mã QR hiển thị trên màn hình bằng ứng dụng Authenticator.
4. Nhập mã 6 số từ ứng dụng để xác nhận.
5. **Lưu lại 10 mã khôi phục** vào nơi an toàn — chỉ hiển thị 1 lần.

Kể từ lần đăng nhập tiếp theo, hệ thống sẽ yêu cầu mã 6 số từ ứng dụng Authenticator.

---

## 4. Giao diện tổng quan

Sau khi đăng nhập thành công:

```
┌─────────────────────────────────────────────────────┐
│  [Logo]  [Tìm kiếm Ctrl+K]          [🔔] [🌙] [👤] │  ← Topbar
├─────────┬───────────────────────────────────────────┤
│ Khám bệnh│                                           │
│  Tiếp đón│         Nội dung trang                   │
│  Bệnh nhân│                                          │
│  Khám bệnh│                                          │
├─────────┤                                           │
│  Dược   │                                           │
│  Kê đơn │                                           │
│  Kho dược│                                          │
└─────────┴───────────────────────────────────────────┘
```

- **Sidebar trái**: điều hướng theo nhóm chức năng. Click vào `<` để thu gọn.
- **Topbar**: tìm kiếm nhanh, thông báo, đổi theme, menu tài khoản.
- **Nội dung**: nơi hiển thị trang hiện tại.

---

## 5. Phím tắt bàn phím

Nhấn `?` để xem toàn bộ danh sách phím tắt. Một số phím tắt hay dùng:

| Phím | Chức năng |
|---|---|
| `Ctrl+K` / `Cmd+K` | Mở Command Palette (tìm kiếm nhanh) |
| `?` | Hiện bảng phím tắt |
| `g p` | Đến trang Bệnh nhân |
| `g e` | Đến trang Khám bệnh |
| `g r` | Đến trang Tiếp đón |
| `g c` | Đến trang Thu ngân |
| `g h` | Về Tổng quan |
| `F2` | Thêm bệnh nhân mới (trang Lễ tân) |

---

## 6. Command Palette (Tìm kiếm nhanh)

Nhấn `Ctrl+K` (Windows/Linux) hoặc `Cmd+K` (Mac) để mở Command Palette:

- **Tìm bệnh nhân**: gõ tên, mã BN, số điện thoại
- **Điều hướng nhanh**: gõ tên trang (ví dụ: "thu ngân", "kê đơn")
- **Thao tác nhanh**: "tạo bệnh nhân mới"
- Dùng `↑↓` để chọn, `Enter` để mở, `Esc` để đóng

---

## 7. Đổi giao diện (Theme)

Click vào biểu tượng mặt trăng/mặt trời trên Topbar để chuyển đổi:
- **Sáng** (Light)
- **Tối** (Dark)
- **Theo hệ thống** (System — tự động theo cài đặt máy tính)

---

## 8. Đăng xuất

Click **avatar** → **Đăng xuất**. Hệ thống tự động đăng xuất sau 8 giờ không hoạt động.

---

## 9. Quên mật khẩu

1. Trên trang đăng nhập, click **Quên mật khẩu?**
2. Nhập email tài khoản → **Gửi liên kết đặt lại**.
3. Kiểm tra hộp thư email, click vào liên kết (có hiệu lực trong 24 giờ).
4. Đặt mật khẩu mới.
