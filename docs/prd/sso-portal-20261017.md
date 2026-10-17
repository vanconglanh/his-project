# PRD — Đăng nhập một lần (SSO) bằng tài khoản DiaB Portal

- Ngày: 2026-10-17
- Tác giả: Lành (architect, kiêm phân tích nghiệp vụ)
- Trạng thái: Bản nháp chờ duyệt
- Tài liệu thiết kế kèm theo: `docs/api/sso-design-20261017.md`

## 1. Bối cảnh

Nhân viên DiaB đang có tài khoản trên `portal.diab.com.vn`, xác thực bởi IdentityServer `https://is.diab.com.vn` (IdentityServer4 + ASP.NET Identity). Hiện họ phải nhớ thêm tài khoản riêng cho:

1. **Pro-Diab HIS** — `his.diab.com.vn` (staging `his.diab.vn`): .NET 8 + Next.js 16, JWT nội bộ + refresh token, RBAC, multi-tenant, admin bắt buộc 2FA.
2. **DiaB TimeSheet** — `timesheet.diab.com.vn`: repo `D:\diab\dev\git\pro-diab-ai\diab-timesheet` (backend .NET 10 `src/Services/OperationsService`, JWT HMAC tự phát; frontend React 19 + Vite; có app mobile).

Mục tiêu: dùng một tài khoản DiaB đăng nhập cả hai hệ thống, nhưng **quyền vẫn do từng hệ thống quản lý**.

## 2. Quyết định đã chốt

| # | Quyết định |
|---|-----------|
| D1 | Ánh xạ tài khoản theo **email** (claim `email`, yêu cầu `email_verified=true`) với user của hệ thống đích; lần đầu thành công thì lưu liên kết `(provider, sub)` → user, các lần sau tra theo `sub` |
| D2 | User portal chưa có user tương ứng → **chặn**, hiển thị "Tài khoản chưa được cấp quyền HIS" (timesheet: "Tài khoản chưa được cấp quyền TimeSheet"). Không tự tạo user |
| D3 | Giữ đăng nhập email/mật khẩu làm phương án dự phòng |
| D4 | Vai trò/quyền/tenant/phòng khám do HIS (và timesheet) quản lý; không dùng claim `role` từ IdP |
| D5 | Admin HIS: SSO **không** bỏ qua 2FA của HIS, trừ khi IdP chứng minh MFA qua claim `amr` chứa `mfa`/`otp`/`hwk` (cấu hình bật/tắt, mặc định tắt) |
| D6 | Single Logout: đăng xuất 1 hệ thống (portal/HIS/TimeSheet) thì đăng xuất tất cả qua IdP (RP-initiated + back-channel). Deploy không được làm toàn bộ user phải đăng nhập lại |

## 3. Đối tượng

- Lễ tân, bác sĩ, dược sĩ, kế toán, KTV, điều dưỡng (HIS)
- Admin HIS (bắt buộc 2FA)
- Nhân viên chấm công, quản lý (TimeSheet)
- Quản trị IdentityServer (đăng ký client)

## 4. User story + Acceptance criteria

### HIS

**US-SSO-01 — Đăng nhập bằng tài khoản DiaB**
Là nhân viên phòng khám, tôi muốn bấm "Đăng nhập bằng tài khoản DiaB" để vào HIS mà không nhập mật khẩu HIS.
- AC1: Trang `/login` có nút "Đăng nhập bằng tài khoản DiaB" phía trên/dưới form mật khẩu.
- AC2: Bấm nút → chuyển tới `is.diab.com.vn`; nếu đã đăng nhập portal thì không phải nhập lại.
- AC3: Quay về HIS, user có email trùng (không phân biệt hoa thường, đã trim) và `email_verified=true` → vào dashboard với đúng vai trò/tenant/phòng khám như đăng nhập mật khẩu.
- AC4: Phiên HIS dùng cùng cơ chế JWT + refresh token hiện tại (cookie httpOnly), không lưu token IdP ở trình duyệt.
- AC5: Ghi audit log `AUTH_SSO_LOGIN_SUCCESS` (user_id, tenant_id, sub, IP, user-agent).

**US-SSO-02 — Chặn tài khoản chưa được cấp quyền**
- AC1: Không tìm thấy user HIS active theo email (và chưa có liên kết `sub`) → về `/login?error=SSO_USER_NOT_PROVISIONED`, hiển thị "Tài khoản chưa được cấp quyền HIS. Vui lòng liên hệ quản trị phòng khám."
- AC2: User HIS bị khóa/soft-delete → `SSO_USER_DISABLED` "Tài khoản HIS đã bị khóa".
- AC3: `email_verified` thiếu/false → `SSO_EMAIL_NOT_VERIFIED`.
- AC4: Một email khớp nhiều user ở nhiều tenant → hiện trang chọn phòng khám (chỉ liệt kê tenant user có tài khoản); nếu chưa triển khai trang chọn ở pha 1 thì chặn với `SSO_AMBIGUOUS_ACCOUNT`.
- AC5: Mọi trường hợp chặn đều ghi audit `AUTH_SSO_LOGIN_DENIED` kèm lý do; không tạo user.

**US-SSO-03 — Admin và 2FA**
- AC1: User có vai trò Admin đăng nhập SSO → vẫn chuyển sang bước nhập mã 2FA của HIS (`/login/2fa`) như luồng mật khẩu.
- AC2: Nếu `Sso__TrustIdpMfa=true` và id_token có `amr` chứa một trong `mfa|otp|hwk` thì bỏ qua 2FA HIS; ghi audit kèm `amr`.
- AC3: Admin chưa thiết lập 2FA → vào màn hình thiết lập 2FA như hiện nay.

**US-SSO-04 — Đăng xuất**
- AC1: Bấm "Đăng xuất" trong HIS → thu hồi refresh token HIS, xóa cookie, chuyển tới `end_session_endpoint` của IdP với `id_token_hint` và `post_logout_redirect_uri` → về `/login`.
- AC2: Đăng xuất trên portal → IdP gọi back-channel `POST /api/v1/auth/sso/backchannel-logout` → HIS thu hồi mọi refresh token gắn `sid`/`sub` đó; access token còn hạn tới hết thời hạn hiện tại (chấp nhận, không đổi thời hạn); chỉ phiên tạo từ SSO bị thu hồi, phiên mật khẩu không bị ảnh hưởng hoặc bị chặn ngay nếu bật kiểm tra blacklist `sid` trong Redis.
- AC3: Người dùng chỉ đăng xuất HIS có thể chọn "Chỉ đăng xuất HIS" (không đăng xuất portal) — tùy chọn pha 2.

**US-SSO-05 — Dự phòng mật khẩu**
- AC1: Form email/mật khẩu vẫn hoạt động bình thường kể cả khi IdP lỗi.
- AC2: IdP không phản hồi (discovery lỗi) → nút SSO hiển thị thông báo "Hệ thống đăng nhập DiaB tạm thời không khả dụng, vui lòng dùng email/mật khẩu".
- AC3: Admin tenant có thể tắt SSO theo tenant (`sso_enabled` trong cấu hình tenant) — pha 2.

**US-SSO-06 — Quản trị liên kết**
- AC1: Trang quản lý user (admin) hiển thị cột "Liên kết DiaB" (đã liên kết/chưa, thời điểm đăng nhập SSO gần nhất).
- AC2: Admin có thể gỡ liên kết (soft delete bản ghi liên kết); lần SSO sau sẽ ánh xạ lại theo email.
- AC3: Đổi email user HIS không làm mất liên kết (liên kết theo `sub`).

### TimeSheet

**US-TS-01** — Như US-SSO-01 cho `timesheet.diab.com.vn`: nút "Đăng nhập bằng tài khoản DiaB", ánh xạ theo email với user TimeSheet, phát JWT nội bộ TimeSheet như luồng đăng nhập hiện tại.
**US-TS-02** — Chặn user chưa có tài khoản TimeSheet: "Tài khoản chưa được cấp quyền TimeSheet".
**US-TS-03** — Đăng xuất đồng bộ (RP-initiated + back-channel) như US-SSO-04.
**US-TS-04** — App mobile TimeSheet: pha 2, dùng client public + PKCE riêng (`diab-timesheet-mobile`, redirect custom scheme).

### Bảo toàn phiên đăng nhập (bắt buộc)

**US-SSO-07 — Không làm mất session hiện có ở bất kỳ hệ thống nào**
Là người dùng đang đăng nhập portal/HIS/TimeSheet, tôi không muốn bị đá ra khi SSO được triển khai.
- AC1 (IdentityServer/portal): Đăng ký client mới chỉ bằng INSERT vào ConfigurationStore DB; **không** restart IS, **không** đổi signing key (pfx), **không** sửa client cũ, **không** đổi cookie scheme / Data Protection key của IS. Người đang đăng nhập portal trước khi đăng ký vẫn dùng bình thường sau đó.
- AC2 (HIS/TimeSheet): Bản deploy SSO **không** đổi JWT secret/issuer/audience/thời hạn token, tên/thuộc tính cookie, định dạng refresh token. Access token và refresh token phát trước deploy vẫn hợp lệ sau deploy.
- AC3: Đăng nhập email/mật khẩu cũ chạy song song, không đổi hành vi (kể cả 2FA admin).
- AC4: Back-channel logout chỉ thu hồi phiên được tạo từ SSO (refresh token có `sso_sid`/`sso_sub` khớp); phiên đăng nhập kiểu cũ (`sso_sid IS NULL`) không bị ảnh hưởng.
- AC5: Migration chỉ thêm bảng/cột nullable; không xóa/đổi/truncate bảng refresh token hay session.
- AC6: Có bài test "giữ session": đăng nhập trước deploy → sau deploy vẫn gọi API, refresh được, không bị chuyển về `/login` (HIS, TimeSheet, portal).
- AC7: Có kế hoạch rollback: tắt `Sso__Enabled=false` hoặc quay lại image cũ mà không làm mất phiên; xóa client mới trên IS không ảnh hưởng client cũ.

## 5. Ngoài phạm vi

- Tự động tạo user (JIT provisioning), đồng bộ vai trò từ IdP.
- SSO cho bệnh nhân / patient portal.
- Thay thế hoàn toàn đăng nhập mật khẩu.
- Nâng cấp IdentityServer4 (EOL) lên Duende — ghi nhận là rủi ro.

## 6. Chỉ số thành công

- >= 80% lượt đăng nhập nhân viên nội bộ DiaB qua SSO sau 1 tháng.
- 0 sự cố đăng nhập nhầm tenant / leo thang quyền.
- Thời gian đăng nhập SSO (đã có phiên portal) < 3 giây.

## 7. Phụ thuộc

- Chủ hệ thống IdentityServer đăng ký client (xem tài liệu thiết kế mục 9).
- Xác nhận IdP phát claim `email`, `email_verified`, `sid`, `amr` trong id_token.
