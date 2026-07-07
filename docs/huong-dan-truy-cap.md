# Hướng dẫn truy cập — Pro-Diab HIS (bản deploy)

> Hệ thống đã deploy và đang chạy. Dùng thông tin dưới đây để đăng nhập.
> Cập nhật: 03/07/2026.

---

## 🌐 Địa chỉ truy cập

**https://his.diab.com.vn**

- Đã bật HTTPS (chứng chỉ Let's Encrypt, tự động gia hạn).
- Chạy song song, độc lập với các hệ thống khác trên cùng máy chủ.

---

## 🔑 Tài khoản đăng nhập

> **Tất cả mật khẩu = `admin123`**

### Tenant 1 — Demo (`DIAB-HCM`)

| Vai trò | Email | Mật khẩu |
|---|---|---|
| **Quản trị (full quyền)** | `admin@prodiab.local` | `admin123` |
| Bác sĩ | `bacsi1@prodiab.local` | `admin123` |

### Tenant 2 — Test (`DIAB-TEST`, đủ 6 vai trò)

| Vai trò | Email | Mật khẩu |
|---|---|---|
| Admin | `admin.test@diabtest.local` | `admin123` |
| Lễ tân | `letan.test@diabtest.local` | `admin123` |
| Bác sĩ | `bacsi.test@diabtest.local` | `admin123` |
| Bác sĩ 2 | `bacsi2.test@diabtest.local` | `admin123` |
| Dược sĩ | `duocsi.test@diabtest.local` | `admin123` |
| Kế toán | `ketoan.test@diabtest.local` | `admin123` |
| Kỹ thuật viên | `ktv.test@diabtest.local` | `admin123` |

---

## ✅ Đăng nhập nhanh (khuyên dùng)

```
Email:    admin@prodiab.local
Mật khẩu: admin123
```

Đây là tài khoản **Quản trị**, đầy đủ quyền để duyệt toàn bộ chức năng.

---

## ⚠️ Lưu ý bảo mật

- Đây là **mật khẩu seed demo** (`admin123`). Trước khi đưa vào sử dụng thật/công khai, nên:
  - **Đổi mật khẩu** các tài khoản (nhất là tài khoản admin).
  - Cân nhắc **xoá bớt tài khoản test** (tenant 2 `*.test@diabtest.local`).
- Đổi mật khẩu: đăng nhập → menu tài khoản (góc phải trên) → **Bảo mật / Đổi mật khẩu**.
