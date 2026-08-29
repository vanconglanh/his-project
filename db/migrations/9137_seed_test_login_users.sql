-- ============================================================
-- Migration: 9137_seed_test_login_users
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Muc dich: Seed 1 tai khoan test cho MOI role (tru admin da co san
--   qc.admin@prodiab.test tu truoc) de phuc vu panel "Dang nhap test"
--   (chi hien tren FE khi NEXT_PUBLIC_TEST_LOGIN_PANEL=true, KHONG bao gio
--   bat o build production/staging).
-- Mat khau chung: Test@123 (bcrypt, workFactor 12) - CHI DUNG CHO MOI
--   TRUONG DEV/TEST LOCAL, KHONG duoc dung lai cho user that.
-- Idempotent: YES (INSERT IGNORE - id co dinh, chay lai khong tao trung)
-- ============================================================
SET NAMES utf8mb4;

-- Mat khau 'Test@123' -> bcrypt (workFactor 12)
-- Hash duoc sinh 1 lan, dan lai y nguyen o day de idempotent giua cac moi truong.
SET @pwd_hash = '$2b$12$pgNFQMZv44ickqoeAFzvtOMxU0jhVuTcrXxsUQd9aqXesvGYH/DKC';

-- Dong bo lai mat khau tai khoan admin da tao truoc do (qc.admin@prodiab.test tung
-- dung rieng 'Admin@123') ve chung 'Test@123' - panel "Dang nhap nhanh" gui 1 mat
-- khau duy nhat cho ca 6 nut role, khong tach rieng ngoai le cho admin.
UPDATE diab_his_sec_users SET password_hash = @pwd_hash
WHERE email = 'qc.admin@prodiab.test';

INSERT IGNORE INTO diab_his_sec_users (id, tenant_id, email, password_hash, full_name, user_status, is_active)
VALUES
    ('e210a28b-062d-4d90-98f9-693936cbcc5d', 1, 'bacsi.test@prodiab.test',  @pwd_hash, N'BS. Test Demo',  'ACTIVE', 1),
    ('14ca565a-1e49-4add-bb59-c8d343013dbc', 1, 'letan.test@prodiab.test',  @pwd_hash, N'LT. Test Demo',  'ACTIVE', 1),
    ('29f2838b-bebe-401e-9d0a-22fd39563864', 1, 'duocsi.test@prodiab.test', @pwd_hash, N'DS. Test Demo',  'ACTIVE', 1),
    ('394ec0a7-ccdc-448b-9a1b-43356b8abbef', 1, 'ketoan.test@prodiab.test', @pwd_hash, N'KT. Test Demo',  'ACTIVE', 1),
    ('60e291e1-6ee3-4388-aa71-bbfa3a0ed49b', 1, 'ktv.test@prodiab.test',    @pwd_hash, N'KTV. Test Demo', 'ACTIVE', 1);

INSERT IGNORE INTO diab_his_sec_user_roles (user_id, role_id, tenant_id) VALUES
    ('e210a28b-062d-4d90-98f9-693936cbcc5d', '00000000-0000-0000-0000-000000000002', 1), -- bac_si
    ('14ca565a-1e49-4add-bb59-c8d343013dbc', '00000000-0000-0000-0000-000000000003', 1), -- le_tan
    ('29f2838b-bebe-401e-9d0a-22fd39563864', '00000000-0000-0000-0000-000000000004', 1), -- duoc_si
    ('394ec0a7-ccdc-448b-9a1b-43356b8abbef', '00000000-0000-0000-0000-000000000005', 1), -- ke_toan
    ('60e291e1-6ee3-4388-aa71-bbfa3a0ed49b', '00000000-0000-0000-0000-000000000006', 1); -- ky_thuat_vien

INSERT IGNORE INTO diab_his_sec_user_branches (id, tenant_id, user_id, branch_id, is_primary) VALUES
    (UUID(), 1, 'e210a28b-062d-4d90-98f9-693936cbcc5d', 1, 1),
    (UUID(), 1, '14ca565a-1e49-4add-bb59-c8d343013dbc', 1, 1),
    (UUID(), 1, '29f2838b-bebe-401e-9d0a-22fd39563864', 1, 1),
    (UUID(), 1, '394ec0a7-ccdc-448b-9a1b-43356b8abbef', 1, 1),
    (UUID(), 1, '60e291e1-6ee3-4388-aa71-bbfa3a0ed49b', 1, 1);
