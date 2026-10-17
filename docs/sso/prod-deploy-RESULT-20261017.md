# Kết quả deploy PRODUCTION SSO — HIS (2026-10-17)

> User duyệt rõ: "Deploy lên production luôn, và deploy cho timesheet luôn".
> HIS prod: ĐÃ DEPLOY + BẬT SSO, verify giữ phiên. TimeSheet: DỪNG (xem mục cuối).

## Hạ tầng đã chốt (verify qua SSH)
- IS production: container `pro-diab-is`, IssuerUri + discovery issuer = `https://is.diab.com.vn` (khớp, không lệch host như staging). ConfigurationStore = SQL Server `pro-db-mssql` / catalog `DiaB_Identity_Server`.
- JWKS kid = `27BF7E5D...RS256` (cùng signing cert hết hạn 2020 — R10). HIS dựa `Sso__IgnoreSigningCertExpiry` mặc định TRUE → vẫn verify chữ ký qua JWKS.
- Domain prod HIS = `https://his.diab.com.vn` (Q6 chốt: CORS origin + FE build-arg + discovery).

## Đã thực hiện (theo thứ tự, mỗi bước verify tận mắt)
1. Backup bảng cấu hình IS (pro-db-mssql/DiaB_Identity_Server): 10 bảng `Client*_ssobak_202610171605`.
2. JWKS baseline sha256 = `254d496240a3a41600e2756c6525ad84b569c3ba8af4b74ac8cb839a47afd31b`.
3. INSERT client `prodiab-his` (Id=6): authorization_code + PKCE (S256), RequireClientSecret, scopes `openid profile email`,
   redirect `https://his.diab.com.vn/api/v1/auth/sso/callback`, post-logout `https://his.diab.com.vn/login`,
   back-channel `https://his.diab.com.vn/api/v1/auth/sso/backchannel-logout`. Secret sinh ngẫu nhiên trên server;
   hash SHA256-base64 trong `ClientSecrets`; plaintext CHỈ ở `/srv/apps/pro/his/backend.sso.env` (600). **KHÔNG restart IS.**
4. JWKS sau INSERT = GIỐNG HỆT baseline (GS7 PASS) → phiên portal/app/mobile KHÔNG bị ảnh hưởng.
5. Backup DB HIS: `/home/stackops/backups/his/pro_sessions_20261017-1607.sql`. Áp `0000_helpers.sql` + `9221_create_sec_user_external_logins.sql`
   (CREATE TABLE IF NOT EXISTS + ADD COLUMN NULL, additive, idempotent). Ghi `_schema_migrations`.
6. Build image LOCAL từ HEAD 460f243 + 2 overlay FE (EmrTabPanel.tsx, prescriptions/page.tsx):
   `prodiab-his-backend:prod-20261017-1605`, `prodiab-his-frontend:prod-20261017-1605`
   (build-arg `NEXT_PUBLIC_API_BASE_URL=https://his.diab.com.vn`, `NEXT_PUBLIC_TEST_LOGIN_PANEL=false`). `docker save | ssh docker load`.
7. Cutover: tag image cũ thành `prev-20261017-1605` (be+fe); backup `.env` + `docker-compose.yml`; thêm `backend.sso.env` vào
   `env_file` của backend; `PRO_HIS_TAG=prod-20261017-1605`; `docker compose up -d --no-deps --force-recreate backend frontend` + `docker restart pro-his-nginx`.
   Deploy lần đầu với `Sso__Enabled=false`.
8. Kiểm giữ phiên (GS2): phiên user non-admin (accounting@diab.com.vn, ke_toan) đăng nhập TRƯỚC deploy → sau recreate gọi `GET /api/v1/patients` = **200** (JWT/refresh không đổi). `/sso/config` = `enabled:false`, login page 200.
9. Bật SSO: `Sso__Enabled=true` → recreate backend → `/sso/config` = `enabled:true`; `GET /sso/login` = **302** tới
   `https://is.diab.com.vn/connect/authorize?client_id=prodiab-his&...redirect_uri=https://his.diab.com.vn/api/v1/auth/sso/callback&code_challenge_method=S256`.
   GS3: phiên cũ vẫn 200. JWKS vẫn `254d4962...` (GS7).

## Rollback
- Mức 1 (nhanh): `sed -i 's/^Sso__Enabled=true/Sso__Enabled=false/' /srv/apps/pro/his/backend.sso.env` → `docker compose up -d --no-deps --force-recreate backend` → ẩn SSO, phiên cũ không đổi.
- Mức 2 (image): `cd /srv/apps/pro/his; sed -i 's/^PRO_HIS_TAG=.*/PRO_HIS_TAG=prev-20261017-1605/' .env` → `docker compose up -d --no-deps --force-recreate backend frontend` → `docker restart pro-his-nginx`. (Migration để nguyên — nullable, code cũ bỏ qua.)
- Mức 3 (IS): chạy `docs/sso/is-register-clients-prod-rollback.sql` nhưng CHỈ cho `prodiab-his` (diab-timesheet chưa đăng ký). Không restart IS.

## Hướng dẫn USER test SSO production (cần tài khoản portal thật — không tự test được)
1. Mở trình duyệt đã đăng nhập portal DiaB (hoặc đăng nhập `https://portal.diab.com.vn`).
2. Vào `https://his.diab.com.vn/login` → bấm nút "Đăng nhập với DiaB".
3. Kỳ vọng: redirect qua `is.diab.com.vn` (nếu còn phiên portal thì KHÔNG hỏi mật khẩu lại) → quay về HIS → vào dashboard,
   VỚI ĐIỀU KIỆN email portal trùng email 1 user HIS đang active (ánh xạ theo email). Nếu email không khớp → HIS trả
   `AUTH_SSO_LOGIN_DENIED` (đúng thiết kế, cần đối soát danh sách email).
4. Single Logout: đăng xuất HIS → phiên SSO ở portal/HIS cùng phiên bị đăng xuất; phiên đăng nhập bằng mật khẩu ở thiết bị khác KHÔNG bị ảnh hưởng.
5. Nếu lỗi: `ssh ... 'docker logs --tail 80 pro-his-backend'` — OidcTokenValidator log chẩn đoán chi tiết (iss/aud/kid/nonce).

## TimeSheet — DỪNG (không deploy)
Không deploy staging/prod. Lý do: code `feature/sso-oidc` (và mọi branch repo) `Program.cs` luôn `UseSqlite(operations.db)`,
BỎ QUA `MYSQL_CONN`; trong khi image prod `pro-docosan-ts-be` đang chạy dùng Pomelo MySQL `pro-db-my57/timesheet_production`
(data thật: 27 users, 648 shifts...). Repo KHÔNG có Dockerfile/CI → image prod build từ nguồn ngoài repo. Deploy branch này =
app chuyển sang SQLite rỗng, mất truy cập data + mất phiên toàn bộ user TimeSheet. Chưa đăng ký client `diab-timesheet` trên IS prod.
Cần dev: (1) wire Pomelo MySQL khớp image prod, (2) chuyển migration SSO sang MySQL, (3) tìm pipeline build thật (stg-gitlab), rồi mới deploy.
