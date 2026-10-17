# Thủ tục deploy PRODUCTION HIS (chỉ code, Sso__Enabled=false) — CHỜ USER DUYỆT

> Production deploy là điểm dừng cứng: KHÔNG tự chạy. Chỉ thực hiện khi user duyệt.
> Mục tiêu: đưa code SSO lên prod nhưng TẮT (Sso__Enabled=false), KHÔNG đổi gì khác,
> verify phiên người dùng production hiện tại KHÔNG mất. KHÔNG đăng ký client IS prod,
> KHÔNG bật SSO prod (các việc đó là đợt sau, cần duyệt riêng).

## Mục tiêu & bất biến
- GIỮ NGUYÊN: Jwt secret/issuer/audience/lifetime, cookie, refresh token, DB.
- Migration 9221 (additive) áp prod DB (`pro-his-db` / prodiab_his) — backup trước.
- Image build LOCAL từ commit b705874 (HEAD test/lark-full-stack), overlay 2 file FE working-tree, build-arg prod.
- SSO tắt hoàn toàn (không có env Sso__* HOẶC Sso__Enabled=false). Không tạo backend.sso.env prod, HOẶC tạo với Enabled=false.

## Các bước (chạy khi được duyệt)
1. Backup prod DB: `docker exec -e MYSQL_PWD=... pro-his-db mysqldump --default-character-set=utf8mb4 -uroot prodiab_his diab_his_sec_sessions > backup`.
2. Áp migration 9221 lên prod DB (idempotent, additive): `mysql ... prodiab_his < db/migrations/9221_create_sec_user_external_logins.sql`. Verify table + 2 cột.
3. Build image local từ b705874:
   - `prodiab-his-backend:prod-<ts>` (backend/Dockerfile).
   - `prodiab-his-frontend:prod-<ts>` build-arg `NEXT_PUBLIC_API_BASE_URL=https://his.diab.com.vn` (XÁC NHẬN Q6: prod là his.diab.com.vn hay his.diab.vn), `NEXT_PUBLIC_TEST_LOGIN_PANEL=false` (prod KHÔNG bật test panel).
   - Overlay 2 file working-tree khi build FE (EmrTabPanel.tsx, prescriptions/page.tsx).
4. Trên server: `docker tag` image prod đang chạy thành `...:prev-<ts>` (rollback). `docker save | ssh docker load`.
5. compose prod (stack production): KHÔNG thêm backend.sso.env (hoặc thêm với Enabled=false). `docker compose up -d --no-deps --force-recreate backend frontend` → `docker restart <nginx prod>`.
6. VERIFY giữ phiên (bắt buộc): một phiên user prod đang đăng nhập (trình duyệt) gọi API vẫn 200, refresh 200, KHÔNG về /login. `/api/v1/auth/sso/config` → enabled=false. Nút SSO ẩn. Regression prod (nếu có BASE_URL prod + test panel — prod không bật panel nên có thể bỏ, dùng kiểm thủ công phiên).
7. Rollback nếu lỗi: `docker tag ...:prev-<ts> ...:latest` + recreate; migration để nguyên (nullable, code cũ bỏ qua).

## Rủi ro cần nói trước khi deploy prod
- R8/R9 KHÔNG liên quan bước này (không đụng IS, không đụng TimeSheet).
- Recreate container prod có downtime ngắn (vài giây) — chọn giờ thấp điểm.
- Q6 domain prod (his.diab.com.vn vs his.diab.vn) phải chốt để build-arg FE đúng.
