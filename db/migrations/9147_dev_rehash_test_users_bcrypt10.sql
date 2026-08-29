-- ============================================================
-- 9147_dev_rehash_test_users_bcrypt10.sql
--
-- BUG UX#7 (đăng nhập ~6.9s do bcrypt): hạ work factor của bcrypt cho các tài
-- khoản TEST/DEV từ 12 -> 10 để đăng nhập nhanh hơn trong môi trường phát triển.
-- BCrypt.Verify suy cost trực tiếp từ hash, nên phải re-hash mới có hiệu lực.
--
-- CHỈ ÁP DỤNG CHO MÔI TRƯỜNG DEV/TEST LOCAL. Đây là các tài khoản *.test@prodiab.test
-- (seed ở 9137, chỉ dùng với panel "Đăng nhập test", KHÔNG bật ở prod/staging).
-- Mật khẩu giữ nguyên 'Test@123'. Hash factor 10 sinh 1 lần, dán cố định (idempotent).
--
-- Song song: PasswordHasher đọc Security:BCryptWorkFactor (mặc định 12, Development=10)
-- => mọi hash MỚI tạo ở dev cũng dùng factor 10.
-- ============================================================
SET NAMES utf8mb4;

-- bcrypt('Test@123', workFactor=10)
SET @pwd_hash10 = '$2b$10$PP40y4zV0hBOpTuuE7WnP.pxDgBesCREin3rlaIC7cdV.hMbiyNA.';

UPDATE diab_his_sec_users
   SET password_hash = @pwd_hash10
 WHERE email IN (
   'qc.admin@prodiab.test',
   'bacsi.test@prodiab.test',
   'letan.test@prodiab.test',
   'duocsi.test@prodiab.test',
   'ketoan.test@prodiab.test',
   'ktv.test@prodiab.test'
 );
