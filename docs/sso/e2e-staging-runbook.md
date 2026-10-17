# Runbook E2E/SLO SSO staging — phần cần tài khoản portal thật

Các bước dưới đây CẦN đăng nhập `portal.staging.diab.com.vn` bằng tài khoản DiaB thật nên không chạy được trong phiên tự động (không nhập mật khẩu portal thật). Trạng thái staging hiện tại: SSO đã BẬT (`Sso__Enabled=true`), client `prodiab-his-staging` đã đăng ký trên IS staging, HIS staging = `https://his.diab.vn`.

Điều kiện tiên quyết để "không phải đăng nhập lại": email tài khoản portal phải TRÙNG (không phân biệt hoa thường) với email một user HIS đang active trong tenant staging, và user HIS đó phải có vai trò hợp lệ. Nếu không có user HIS khớp → hệ thống CHẶN đúng theo thiết kế (xem bước 2).

## Chuẩn bị (giữ phiên — GS)
- Trình duyệt A: đăng nhập sẵn `portal.staging.diab.com.vn`.
- Trình duyệt B: đăng nhập sẵn HIS `https://his.diab.vn` bằng MẬT KHẨU (dùng panel đăng nhập nhanh hoặc tài khoản thật) — gồm 1 phiên bác sĩ + 1 phiên admin (2FA). Ghi lại thời điểm.
- (Nếu test TimeSheet) Trình duyệt C: đăng nhập sẵn TimeSheet. (TimeSheet pha này CHƯA deploy SSO — chỉ kiểm phiên không bị đá.)

## Test chính

### Login SSO thành công (US-SSO-01)
1. Trình duyệt A (đã đăng nhập portal), mở `https://his.diab.vn/login`.
2. Bấm "Đăng nhập bằng tài khoản DiaB".
3. KỲ VỌNG: chuyển sang `is.staging.diab.com.vn`, KHÔNG phải nhập lại mật khẩu (vì đã có phiên portal), quay về HIS vào thẳng `/dashboard` đúng vai trò/tenant. (Nếu IS staging không chia sẻ cookie phiên với portal → sẽ hiện màn đăng nhập IS; đây là điểm cần xác nhận hạ tầng — xem Ghi chú.)
4. Kiểm audit: bảng `diab_his_sec_audit_logs` có `AUTH_SSO_LOGIN_SUCCESS`.

### Chặn tài khoản chưa cấp quyền (US-SSO-02)
1. Đăng nhập portal bằng tài khoản KHÔNG có user HIS tương ứng, rồi bấm SSO ở HIS.
2. KỲ VỌNG: về `/login?error=SSO_USER_NOT_PROVISIONED`, hiện "Tài khoản chưa được cấp quyền HIS. Vui lòng liên hệ quản trị phòng khám." Audit `AUTH_SSO_LOGIN_DENIED`.

### Admin vẫn 2FA (US-SSO-03)
1. Đăng nhập SSO bằng tài khoản là Admin HIS.
2. KỲ VỌNG: vẫn chuyển sang bước nhập mã 2FA HIS (vì IdP không phát `amr=mfa`, `Sso__TrustIdpMfa=false`).

### GS4 — đăng xuất portal không đá phiên mật khẩu
1. Đăng nhập SSO ở trình duyệt D (phiên SSO). Trình duyệt B vẫn là phiên MẬT KHẨU cùng user.
2. Đăng xuất portal (hoặc gọi back-channel).
3. KỲ VỌNG: D bị đăng xuất HIS (refresh token SSO thu hồi); B (phiên mật khẩu) VẪN dùng được — vì back-channel chỉ thu hồi `sso_sid IS NOT NULL`.

### SLO1 — đăng xuất HIS → portal/TimeSheet đăng xuất
1. Ở phiên SSO HIS (trình duyệt D), bấm "Đăng xuất".
2. KỲ VỌNG: HIS gọi `end_session_endpoint` (RP-initiated), IS back-channel tới các client còn lại; portal (và TimeSheet nếu đã có SSO) đăng xuất.

### SLO2 — đăng xuất portal → HIS phiên SSO đăng xuất
1. Đăng xuất portal.
2. KỲ VỌNG: IS gọi `POST https://his.diab.vn/api/v1/auth/sso/backchannel-logout` → HIS thu hồi phiên SSO theo `sid`; phiên mật khẩu ở thiết bị khác KHÔNG bị ảnh hưởng.
   - Kiểm log HIS: `AUTH_SSO_BACKCHANNEL_LOGOUT`. Nếu IS không gọi tới được (mạng outbound) → phiên SSO HIS tồn tại tới hết refresh token (rủi ro R5).

## Ghi chú hạ tầng cần xác nhận
- Issuer token staging = `https://is.stg.savvycom.asia` (khác host `is.staging.diab.com.vn`). HIS đã cấu hình validate issuer lấy từ discovery nên khớp.
- "Không phải đăng nhập lại" phụ thuộc việc portal.staging và HIS cùng dùng host IS `is.staging.diab.com.vn` cho phiên (cookie phiên IS). Nếu portal đăng nhập qua host IS khác, bước 3 (login) sẽ hiện màn IS — cần chủ IS xác nhận host.
- Back-channel logout cần IS staging có outbound HTTPS tới `https://his.diab.vn/api/v1/auth/sso/backchannel-logout`. Nhờ chủ IS xác nhận/mở nếu SLO2 không hoạt động.

## Rollback nhanh nếu có sự cố
- Tắt SSO: trên server `/srv/apps/stg/his`, sửa `backend.sso.env` `Sso__Enabled=false` → `docker compose up -d --no-deps --force-recreate backend` → `docker restart stg-his-nginx`. Nút SSO ẩn, phiên cũ không ảnh hưởng.
- Gỡ client IS: chạy `docs/sso/is-register-clients-staging-rollback.sql`.
