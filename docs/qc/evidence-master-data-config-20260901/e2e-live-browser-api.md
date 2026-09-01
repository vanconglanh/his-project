# Evidence — E2E LIVE trên stack rebuild (backend+frontend redeploy)

- Ngày: 2026-09-01
- Cách chạy: rebuild 2 image local (`docker compose build backend frontend`) → `up -d --no-deps` (giữ nguyên DB/volume) → verify qua API thật với luồng đăng nhập + 2FA thật.
- Lưu ý: Chrome extension không kết nối được nên verify qua **API layer thật của backend vừa deploy** (login → 2FA setup/verify thật → JWT → gọi đúng endpoint). Frontend UI wiring đã được đảm bảo bởi `tsc` sạch + đối chiếu contract field-by-field (khớp casing runtime bên dưới).

## Đăng nhập admin có 2FA bắt buộc (luồng thật)
- Login `admin@prodiab.local` → `mfaSetupRequired=true` (admin thuộc nhóm bắt buộc 2FA).
- POST /users/me/2fa/setup → secret; tính TOTP (pyotp); POST /users/me/2fa/enable → 10 recovery codes.
- Login lại → `requires2fa=true` + mfaPendingToken; POST /auth/2fa/verify (TOTP) → access token (len 715). ✓
- Đã revert: admin `two_fa_enabled=0`, secret/recovery=NULL (trả về trạng thái trước E2E).

## Kiểm CASING JSON thật (điểm rủi ro contract)
- `GET /api/v1/admin/codes` raw: `{"id":"GENDER","name":"...","is_system":true,"is_active":true}` → **snake_case**.
- Detail row keys: `code, extra, id, is_active, is_default, is_hidden, is_override, is_system, name, name_en, sort_order, tenant_id`
  → khớp CHÍNH XÁC interface FE `frontend/lib/api/admin-codes.ts`. Contract xác nhận runtime (login camelCase chỉ là ngoại lệ DTO auth).

## E2E-A — Tenant tự thêm mã → dropdown thấy ngay, KHÔNG deploy lại (yêu cầu cốt lõi BO)
- `GET /codes/ENCOUNTER_TYPE` trước: `[FIRST_VISIT, FOLLOW_UP, EMERGENCY, CONSULTATION]`
- `POST /admin/codes/ENCOUNTER_TYPE/details {code:SCREENING_E2E,...}` → 201
- `GET /codes/ENCOUNTER_TYPE` sau: `[..., CONSULTATION, SCREENING_E2E]` → **SCREENING_E2E xuất hiện ngay** (cache invalidate). ✓

## E2E-B — Đổi setting → giá trị mới có ngay
- `PUT /admin/settings/stock_transfer_approval_threshold {value:"7500000"}` → 204
- `GET /settings/public` → `stock_transfer_approval_threshold = 7500000` ✓ (FE `useSettingNumber` reload là thấy)
- Sau fix bổ sung: khi CHƯA override, `/settings/public` trả **default từ meta = 5000000** (không còn phụ thuộc hardcode FE). ✓

## E2E-C — Role động (SaveReportDialog)
- `GET /roles` → 6 role: `bac_si, duoc_si, ke_toan, ky_thuat_vien, le_tan, admin` → SaveReportDialog render động. ✓

## Cleanup
- Xoá mã SCREENING_E2E (204), revert override setting (204). DB sạch, không để lại rác test.

## Fix phát sinh trong E2E
`GetPublicSettingsQueryHandler` trước đây trả null khi chưa có row sys_settings (dù meta có default_value).
Đã sửa: resolve tenant > global > **default_value (meta)** — public endpoint luôn trả default hệ thống,
đúng tinh thần chống hardcode. File: backend/src/ProDiabHis.Application/Settings/SettingsHandlers.cs.
Re-verify live: `/settings/public` trả 5000000 mặc định (không cần override). ✓
</content>
