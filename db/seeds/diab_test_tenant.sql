-- ============================================================
-- Seed: diab_test_tenant  (Tenant test độc lập cho mô phỏng phòng khám)
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Mô tả: Dựng 1 phòng khám TEST riêng (tenant_id = 2, code DIAB-TEST) phục vụ
--        bộ giả lập Playwright frontend/e2e/sim/. Tách hoàn toàn khỏi tenant
--        demo DIAB-HCM (id=1) để kiểm tra cả cách ly multi-tenant.
--
--        Nội dung: tenant + 7 user (6 vai trò) + clinic + 4 phòng
--                  + 8 thuốc (có thuốc THIẾU TỒN & thuốc CẬN HSD) + tồn kho
--                  + 1 luật tương tác thuốc CONTRAINDICATED (DDI)
--                  + ~15 bệnh nhân "cũ" (ambient) để populate hàng đợi/dashboard.
--
-- Điều kiện: chạy SAU khi đã áp các migration 9xxx (bảng đã tồn tại).
-- Idempotent: YES (INSERT IGNORE). Chạy lại nhiều lần không lỗi.
-- Mật khẩu mọi tài khoản: admin123  (dùng chung hash BCrypt với seed 9008)
-- LƯU Ý: chỉ dùng cho môi trường DEV/TEST, KHÔNG chạy trên production.
-- ============================================================
SET NAMES utf8mb4;

-- ============================================================
-- TENANT (id = 2)
-- ============================================================
INSERT IGNORE INTO `diab_his_sys_tenants`
    (`id`, `code`, `name`, `cskcb_code`, `status`, `address`, `phone`, `email`, `subdomain`, `created_at`)
VALUES
    (2, 'DIAB-TEST', 'Phòng khám ĐTĐ DiaB — Môi trường test', 'PKDT-TEST',
     'ACTIVE', 'Khu công nghệ, TP.HCM', '028-9999-0002', 'test@diabtest.local', 'diabtest', NOW());

-- ============================================================
-- USERS — 7 tài khoản / 6 vai trò (mật khẩu admin123)
-- Role UUID hệ thống (từ 9007): admin=...001, bac_si=...002, le_tan=...003,
--                               duoc_si=...004, ke_toan=...005, ky_thuat_vien=...006
-- ============================================================
INSERT IGNORE INTO `diab_his_sec_users`
    (`id`, `tenant_id`, `email`, `password_hash`, `full_name`, `phone`, `user_status`, `is_active`, `created_at`)
VALUES
    ('a2000000-0000-0000-0000-000000000001', 2, 'admin.test@diabtest.local',
     '$2a$12$p6lLXAObdFMPgDPW4l/wDuJ9Y1mh3sEWJlRHx/RgYOwhSNuQ8DhWa', 'Quản trị viên (Test)',     '0920000001', 'ACTIVE', 1, NOW()),
    ('a2000000-0000-0000-0000-000000000002', 2, 'letan.test@diabtest.local',
     '$2a$12$p6lLXAObdFMPgDPW4l/wDuJ9Y1mh3sEWJlRHx/RgYOwhSNuQ8DhWa', 'Lễ tân Test',              '0920000002', 'ACTIVE', 1, NOW()),
    ('a2000000-0000-0000-0000-000000000003', 2, 'bacsi.test@diabtest.local',
     '$2a$12$p6lLXAObdFMPgDPW4l/wDuJ9Y1mh3sEWJlRHx/RgYOwhSNuQ8DhWa', 'BS. Trần Thị Test 1',      '0920000003', 'ACTIVE', 1, NOW()),
    ('a2000000-0000-0000-0000-000000000004', 2, 'bacsi2.test@diabtest.local',
     '$2a$12$p6lLXAObdFMPgDPW4l/wDuJ9Y1mh3sEWJlRHx/RgYOwhSNuQ8DhWa', 'BS. Lê Văn Test 2',        '0920000004', 'ACTIVE', 1, NOW()),
    ('a2000000-0000-0000-0000-000000000005', 2, 'duocsi.test@diabtest.local',
     '$2a$12$p6lLXAObdFMPgDPW4l/wDuJ9Y1mh3sEWJlRHx/RgYOwhSNuQ8DhWa', 'Dược sĩ Test',             '0920000005', 'ACTIVE', 1, NOW()),
    ('a2000000-0000-0000-0000-000000000006', 2, 'ketoan.test@diabtest.local',
     '$2a$12$p6lLXAObdFMPgDPW4l/wDuJ9Y1mh3sEWJlRHx/RgYOwhSNuQ8DhWa', 'Kế toán Test',             '0920000006', 'ACTIVE', 1, NOW()),
    ('a2000000-0000-0000-0000-000000000007', 2, 'ktv.test@diabtest.local',
     '$2a$12$p6lLXAObdFMPgDPW4l/wDuJ9Y1mh3sEWJlRHx/RgYOwhSNuQ8DhWa', 'Kỹ thuật viên Test',       '0920000007', 'ACTIVE', 1, NOW());

-- Gán vai trò (role hệ thống dùng chung, gán trong phạm vi tenant_id = 2)
INSERT IGNORE INTO `diab_his_sec_user_roles` (`user_id`, `role_id`, `tenant_id`) VALUES
    ('a2000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001', 2), -- admin
    ('a2000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000003', 2), -- le_tan
    ('a2000000-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000002', 2), -- bac_si
    ('a2000000-0000-0000-0000-000000000004', '00000000-0000-0000-0000-000000000002', 2), -- bac_si
    ('a2000000-0000-0000-0000-000000000005', '00000000-0000-0000-0000-000000000004', 2), -- duoc_si
    ('a2000000-0000-0000-0000-000000000006', '00000000-0000-0000-0000-000000000005', 2), -- ke_toan
    ('a2000000-0000-0000-0000-000000000007', '00000000-0000-0000-0000-000000000006', 2); -- ky_thuat_vien

-- ============================================================
-- CLINIC (id = 2) + 4 PHÒNG
-- ============================================================
INSERT IGNORE INTO `diab_his_sys_clinics`
    (`id`, `tenant_id`, `code`, `name`, `cskcb_code`, `address`, `phone`, `head_doctor_id`, `created_at`)
VALUES
    (2, 2, 'PK-TEST', 'Phòng khám DiaB Test', 'PKDT-TEST', 'Khu công nghệ, TP.HCM',
     '028-9999-0002', 'a2000000-0000-0000-0000-000000000003', NOW());

INSERT IGNORE INTO `diab_his_sys_rooms`
    (`id`, `tenant_id`, `branch_id`, `code`, `name`, `room_type`, `is_active`, `created_at`)
VALUES
    ('c2000000-0000-0000-0000-000000000001', 2, NULL, 'PK01', 'Phòng khám số 1', 'EXAM',    1, NOW()),
    ('c2000000-0000-0000-0000-000000000002', 2, NULL, 'PK02', 'Phòng khám số 2', 'EXAM',    1, NOW()),
    ('c2000000-0000-0000-0000-000000000003', 2, NULL, 'PK03', 'Phòng khám số 3', 'EXAM',    1, NOW()),
    ('c2000000-0000-0000-0000-000000000004', 2, NULL, 'TC01', 'Quầy thu ngân',   'CASHIER', 1, NOW());

-- ============================================================
-- DANH MỤC THUỐC (8 thuốc) — chủ đích tạo dữ liệu kích hoạt ngoại lệ
--   TH006 Gliclazide: tồn RẤT THẤP  -> test "hết thuốc"
--   TH007 Insulin:     2 lô, 1 cận HSD -> test FEFO / near-expiry
--   TH008 Gemfibrozil: cặp DDI CONTRAINDICATED với Atorvastatin (TH003)
-- ============================================================
INSERT IGNORE INTO `diab_his_pha_drugs`
    (`id`, `tenant_id`, `code`, `name`, `generic_name`, `drug_form`, `strength`, `unit`,
     `sell_price`, `bhyt_price`, `reorder_level`, `is_active`, `created_at`)
VALUES
    ('d2000000-0000-0000-0000-000000000001', 2, 'TH001', 'Metformin 500mg',        'Metformin HCl',       'Viên nén',  '500mg',      'Viên', 500,   350,  100, 1, NOW()),
    ('d2000000-0000-0000-0000-000000000002', 2, 'TH002', 'Amlodipine 5mg',         'Amlodipine besylate', 'Viên nén',  '5mg',        'Viên', 1500,  1000, 50,  1, NOW()),
    ('d2000000-0000-0000-0000-000000000003', 2, 'TH003', 'Atorvastatin 20mg',      'Atorvastatin',        'Viên nén',  '20mg',       'Viên', 3500,  2500, 50,  1, NOW()),
    ('d2000000-0000-0000-0000-000000000004', 2, 'TH004', 'Paracetamol 500mg',      'Paracetamol',         'Viên nén',  '500mg',      'Viên', 300,   200,  200, 1, NOW()),
    ('d2000000-0000-0000-0000-000000000005', 2, 'TH005', 'Omeprazole 20mg',        'Omeprazole',          'Viên nang', '20mg',       'Viên', 1200,  800,  100, 1, NOW()),
    ('d2000000-0000-0000-0000-000000000006', 2, 'TH006', 'Gliclazide 30mg',        'Gliclazide MR',       'Viên nén',  '30mg',       'Viên', 2000,  1400, 100, 1, NOW()),
    ('d2000000-0000-0000-0000-000000000007', 2, 'TH007', 'Insulin Glargine 100UI/ml','Insulin glargine',  'Bút tiêm',  '100UI/ml',   'Bút',  250000,180000,20,  1, NOW()),
    ('d2000000-0000-0000-0000-000000000008', 2, 'TH008', 'Gemfibrozil 600mg',      'Gemfibrozil',         'Viên nén',  '600mg',      'Viên', 4000,  0,    30,  1, NOW());

-- DrugAutocomplete / drugs-search hiển thị & lọc theo cột name_vi → set name_vi = name (idempotent)
UPDATE `diab_his_pha_drugs` SET `name_vi` = `name` WHERE `tenant_id` = 2 AND (`name_vi` IS NULL OR `name_vi` = '');

-- TỒN KHO theo lô (schema demo diab_his_pha_stock)
INSERT IGNORE INTO `diab_his_pha_stock`
    (`id`, `tenant_id`, `drug_id`, `lot_number`, `mfg_date`, `exp_date`, `quantity`, `import_price`, `created_at`)
VALUES
    -- Thuốc thường: đủ tồn
    (UUID(), 2, 'd2000000-0000-0000-0000-000000000001', 'T-M001', '2025-01-01', '2027-12-31', 1000, 300,   NOW()),
    (UUID(), 2, 'd2000000-0000-0000-0000-000000000002', 'T-A001', '2025-01-01', '2027-12-31', 500,  900,   NOW()),
    (UUID(), 2, 'd2000000-0000-0000-0000-000000000003', 'T-S001', '2025-01-01', '2027-12-31', 500,  2000,  NOW()),
    (UUID(), 2, 'd2000000-0000-0000-0000-000000000004', 'T-P001', '2025-01-01', '2027-12-31', 2000, 150,   NOW()),
    (UUID(), 2, 'd2000000-0000-0000-0000-000000000005', 'T-O001', '2025-01-01', '2027-12-31', 500,  700,   NOW()),
    (UUID(), 2, 'd2000000-0000-0000-0000-000000000008', 'T-G001', '2025-01-01', '2027-12-31', 200,  3500,  NOW()),
    -- TH006 Gliclazide: tồn RẤT THẤP (3 viên) -> kê 30 sẽ báo hết thuốc
    (UUID(), 2, 'd2000000-0000-0000-0000-000000000006', 'T-GL01', '2025-01-01', '2027-12-31', 3,    1500,  NOW()),
    -- TH007 Insulin: LÔ 1 cận HSD (còn ~20 ngày) + LÔ 2 hạn xa -> FEFO ưu tiên lô cận HSD
    (UUID(), 2, 'd2000000-0000-0000-0000-000000000007', 'T-IN01', '2025-01-01', DATE_ADD(CURDATE(), INTERVAL 20 DAY), 50,  180000, NOW()),
    (UUID(), 2, 'd2000000-0000-0000-0000-000000000007', 'T-IN02', '2025-06-01', '2027-12-31',                          100, 178000, NOW());

-- ============================================================
-- LUẬT TƯƠNG TÁC THUỐC (DDI) — Atorvastatin + Gemfibrozil = CHỐNG CHỈ ĐỊNH
-- (thực tế: statin + fibrate làm tăng nguy cơ tiêu cơ vân)
-- ============================================================
INSERT IGNORE INTO `diab_his_pha_ddi_rules`
    (`id`, `tenant_id`, `drug1_id`, `drug2_id`, `severity`, `description`, `created_at`)
VALUES
    ('dd200000-0000-0000-0000-000000000001', 2,
     'd2000000-0000-0000-0000-000000000003', 'd2000000-0000-0000-0000-000000000008',
     'CONTRAINDICATED', 'Atorvastatin + Gemfibrozil: tăng nguy cơ tiêu cơ vân (rhabdomyolysis) — chống chỉ định phối hợp.', NOW());

-- ============================================================
-- ~15 BỆNH NHÂN "CŨ" (ambient) — để populate hàng đợi/dashboard.
-- Persona harness (mới/cũ) hoạt động theo cơ chế "search-or-create" nên không
-- phụ thuộc cứng vào danh sách này; đây chỉ là dữ liệu nền cho phòng khám.
-- Mã BN prefix "BNT" để không đụng mã BN00001.. của tenant 1.
-- ============================================================
INSERT IGNORE INTO `diab_his_pat_patients`
    (`id`, `tenant_id`, `code`, `full_name`, `gender`, `date_of_birth`,
     `phone`, `province_code`, `street`, `blood_type`, `status`, `patient_type`, `created_at`)
VALUES
    ('f2000000-0000-0000-0000-000000000001', 2, 'BNT00001', 'Nguyễn Văn Cũ',    'MALE',   '1960-01-10', '0931000001', '79', '1 Test, Q.1',  'A+',  'ACTIVE', 'BHYT',    NOW()),
    ('f2000000-0000-0000-0000-000000000002', 2, 'BNT00002', 'Trần Thị Bích',    'FEMALE', '1975-02-20', '0931000002', '79', '2 Test, Q.1',  'B+',  'ACTIVE', 'SERVICE', NOW()),
    ('f2000000-0000-0000-0000-000000000003', 2, 'BNT00003', 'Lê Văn Cường',     'MALE',   '1968-03-05', '0931000003', '79', '3 Test, Q.3',  'O+',  'ACTIVE', 'BHYT',    NOW()),
    ('f2000000-0000-0000-0000-000000000004', 2, 'BNT00004', 'Phạm Thị Dung',    'FEMALE', '1982-04-15', '0931000004', '79', '4 Test, Q.5',  'AB+', 'ACTIVE', 'SERVICE', NOW()),
    ('f2000000-0000-0000-0000-000000000005', 2, 'BNT00005', 'Hoàng Văn Em',     'MALE',   '1955-05-25', '0931000005', '79', '5 Test, Q.10', 'A-',  'ACTIVE', 'BHYT',    NOW()),
    ('f2000000-0000-0000-0000-000000000006', 2, 'BNT00006', 'Vũ Thị Phượng',    'FEMALE', '1990-06-30', '0931000006', '79', '6 Test, Q.BT', 'B-',  'ACTIVE', 'SERVICE', NOW()),
    ('f2000000-0000-0000-0000-000000000007', 2, 'BNT00007', 'Đặng Văn Giang',   'MALE',   '1972-07-12', '0931000007', '79', '7 Test, Q.3',  'O-',  'ACTIVE', 'BHYT',    NOW()),
    ('f2000000-0000-0000-0000-000000000008', 2, 'BNT00008', 'Bùi Thị Hạnh',     'FEMALE', '1963-08-18', '0931000008', '79', '8 Test, Q.1',  'A+',  'ACTIVE', 'BHYT',    NOW()),
    ('f2000000-0000-0000-0000-000000000009', 2, 'BNT00009', 'Lý Văn Inh',       'MALE',   '1985-09-22', '0931000009', '79', '9 Test, Q.1',  'B+',  'ACTIVE', 'SERVICE', NOW()),
    ('f2000000-0000-0000-0000-000000000010', 2, 'BNT00010', 'Trịnh Thị Kim',    'FEMALE', '1948-10-01', '0931000010', '79', '10 Test, TĐ',  'O+',  'ACTIVE', 'BHYT',    NOW()),
    ('f2000000-0000-0000-0000-000000000011', 2, 'BNT00011', 'Cao Văn Long',     'MALE',   '1979-11-11', '0931000011', '79', '11 Test, Q.7', 'A+',  'ACTIVE', 'BHYT',    NOW()),
    ('f2000000-0000-0000-0000-000000000012', 2, 'BNT00012', 'Dương Thị Mơ',     'FEMALE', '1958-12-24', '0931000012', '79', '12 Test, Q.8', 'B+',  'ACTIVE', 'SERVICE', NOW()),
    ('f2000000-0000-0000-0000-000000000013', 2, 'BNT00013', 'Ngô Văn Nam',      'MALE',   '1966-01-30', '0931000013', '79', '13 Test, Q.9', 'O+',  'ACTIVE', 'BHYT',    NOW()),
    ('f2000000-0000-0000-0000-000000000014', 2, 'BNT00014', 'Đỗ Thị Oanh',      'FEMALE', '1988-02-14', '0931000014', '79', '14 Test, Q.2', 'AB-', 'ACTIVE', 'SERVICE', NOW()),
    ('f2000000-0000-0000-0000-000000000015', 2, 'BNT00015', 'Hồ Văn Phúc',      'MALE',   '1951-03-19', '0931000015', '79', '15 Test, TĐ',  'A+',  'ACTIVE', 'BHYT',    NOW());

-- ============================================================
-- KIỂM CHỨNG NHANH (chạy thủ công sau khi seed):
--   SELECT COUNT(*) FROM diab_his_sec_users     WHERE tenant_id = 2;  -- kỳ vọng 7
--   SELECT COUNT(*) FROM diab_his_sys_rooms      WHERE tenant_id = 2;  -- kỳ vọng 4
--   SELECT COUNT(*) FROM diab_his_pha_drugs      WHERE tenant_id = 2;  -- kỳ vọng 8
--   SELECT COUNT(*) FROM diab_his_pat_patients   WHERE tenant_id = 2;  -- kỳ vọng 15
--   SELECT drug_id, SUM(quantity) FROM diab_his_pha_stock WHERE tenant_id = 2 GROUP BY drug_id;
-- ============================================================
