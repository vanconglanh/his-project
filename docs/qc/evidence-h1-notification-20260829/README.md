# Evidence — H-1 (FR-112) Nhắc lịch hẹn tự động qua SMS/Zalo ZNS

Ngày verify: 2026-08-29. Branch `develop`. Verify bởi Leader agent (build + API thật + browser thật).

## Phạm vi đã kiểm chứng

| # | Hạng mục | Kết quả |
|---|---|---|
| 1 | Migration `9160_notification_channels.sql` apply vào MySQL | PASS — tạo bảng `diab_his_int_notification_channels` (14 cột), thêm cột `reminder_sent_at` vào `diab_his_sch_appointments`, seed 2 permission + grant cho role `admin` |
| 2 | `dotnet build` (ProDiabHis.Api) | PASS — 0 Error, 8 Warning (không liên quan) |
| 3 | `npx tsc --noEmit` (frontend) | PASS — 0 error |
| 4 | CRUD API `/api/v1/notification-channels` | PASS — create/list, mã hoá config + mask secret |
| 5 | Test kết nối SMS (eSMS) với key giả | PASS — gọi API eSMS thật, trả lỗi tiếng Việt |
| 6 | Test kết nối Zalo ZNS với token giả | PASS — gọi API Zalo thật, trả lỗi tiếng Việt |
| 7 | Trang admin `/admin/notification-channels` render | PASS — 2 card SMS/Zalo, nút Test/Sửa/Xóa, toàn tiếng Việt |
| 8 | Nút "Test kết nối" trên UI hiện toast lỗi tiếng Việt | PASS — toast "ApiKey/SecretKey không hợp lệ (mã 101)" |
| 9 | Dialog "Sửa" — mask secret, prefill non-secret | PASS — API Key hiện "****1234", Brandname prefill "Baotri" |

## Bằng chứng API thật (curl → backend local :5100 → provider thật)

### Create SMS channel (config giả)
```
POST /api/v1/notification-channels {"channel":"SMS","provider":"ESMS","config":{"api_key":"FAKE-API-KEY-1234","secret_key":"FAKE-SECRET-9999","brand_name":"Baotri"},"is_active":true}
→ 200 {"data":{"id":"...","channel":"SMS","provider":"ESMS",
        "config_masked":{"api_key":"****1234","secret_key":"****9999","brand_name":"Baotri"}, ...}}
```
→ Secret được mã hoá AES-256-GCM khi lưu, chỉ trả về dạng mask; `brand_name` (không nhạy cảm) trả plaintext.

### Test kết nối SMS (eSMS) — key giả
```
POST /api/v1/notification-channels/{id}/test
→ 200 {"data":{"ok":false,"message":"ApiKey/SecretKey không hợp lệ (mã 101)."}}
```
→ `SmsSender` gọi eSMS `GetBalance` thật, eSMS trả CodeResponse=101, map sang message tiếng Việt.

### Test kết nối Zalo ZNS — token giả
```
POST /api/v1/notification-channels/{id}/test
→ 200 {"data":{"ok":false,"message":"access_token Zalo OA không hợp lệ (error=-124): Access token invalid"}}
```
→ `ZaloZnsSender` gọi `https://business.openapi.zalo.me/template/all` thật, Zalo trả error=-124, map sang message tiếng Việt.

## Browser thật (Next dev :3100 → backend :5100 → MySQL docker)

- Đăng nhập `qc.admin@prodiab.test` (role admin) → vào `/admin/notification-channels`.
- Trang hiển thị 2 card "SMS (eSMS)" và "Zalo ZNS (Official Account)", badge "Đang bật" + "Chưa kiểm tra", nút "Test kết nối / Sửa / Xóa".
- Bấm "Test kết nối" card SMS → toast đỏ góc phải hiện "ApiKey/SecretKey không hợp lệ (mã 101)"; timestamp kiểm tra cập nhật.
- Bấm "Sửa" → dialog "Sửa cấu hình — SMS (eSMS)": API Key/Secret Key ô password placeholder "Nhập mới để thay đổi..." + dòng "Giá trị hiện tại: ****1234 — để trống nếu giữ nguyên"; Brandname prefill "Baotri"; toggle "Kích hoạt kênh".

## Cấu hình được / reset được (không cần deploy lại)

- Config lưu trong DB (mã hoá), `NotificationChannelCredentialProvider` đọc lại mỗi lần gửi → sửa/xoá qua UI có hiệu lực ngay, không cache lâu, không hardcode key.
- Endpoint Zalo ZNS hardcode (URL chuẩn Zalo); access_token/template đọc từ config mã hoá.

## Nhắc lịch tự động

- Job `AppointmentReminderNotifyJob` (Hangfire, cron `0 * * * *`) quét lịch hẹn trong ngưỡng giờ cấu hình (`Notifications:AppointmentReminderHours`, mặc định 24h), status PENDING/CONFIRMED, `reminder_sent_at IS NULL`, gửi qua Zalo (ưu tiên) → fallback SMS, đánh dấu `reminder_sent_at` chống gửi trùng.
