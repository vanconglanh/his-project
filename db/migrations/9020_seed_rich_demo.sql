-- ============================================================
-- Migration: 9020_seed_rich_demo
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-30
-- Tác giả: Pro-Diab Team
-- Mô tả: Seed dữ liệu demo phong phú để hiển thị đầy đủ UI
--        - 30 thuốc (diab_his_pha_drugs)
--        - 30+ lô tồn kho với trạng thái hết hạn khác nhau
--        - 5 nhà cung cấp Việt Nam
--        - Lab orders + 30 kết quả xét nghiệm
--        - Report cache (dashboard widget)
--        - 5 notifications cho admin
--        - 10 lịch hẹn tương lai
--        - 15 payment bổ sung
-- Idempotent: YES (INSERT IGNORE / ON DUPLICATE KEY UPDATE)
-- LƯU Ý: Chỉ dùng cho môi trường DEV, KHÔNG chạy production
-- ============================================================
SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ============================================================
-- PHẦN 1: 30 THUỐC (bổ sung thêm 25 thuốc ngoài 5 đã có)
-- ============================================================
INSERT IGNORE INTO `diab_his_pha_drugs`
    (`id`, `tenant_id`, `code`, `name`, `generic_name`, `brand_name`, `drug_form`, `strength`, `unit`,
     `atc_code`, `drug_category`, `sell_price`, `bhyt_price`, `reorder_level`, `is_active`, `created_at`)
VALUES
    -- Đã có id d000000-0000-0000-0000-00000000000{1-5} từ 9008
    -- Thêm 25 thuốc mới
    ('d0000000-0000-0000-0000-000000000006', 1, 'TH006', 'Glibenclamide 5mg',     'Glibenclamide',          'Daonil',       'Viên nén',  '5mg',    'Viên', 'A10BB01', 'Đái tháo đường',        800,   600,   100, 1, NOW()),
    ('d0000000-0000-0000-0000-000000000007', 1, 'TH007', 'Gliclazide 80mg',       'Gliclazide',             'Diamicron',    'Viên nén',  '80mg',   'Viên', 'A10BB09', 'Đái tháo đường',        1200,  900,   50,  1, NOW()),
    ('d0000000-0000-0000-0000-000000000008', 1, 'TH008', 'Sitagliptin 100mg',     'Sitagliptin phosphate',  'Januvia',      'Viên nén',  '100mg',  'Viên', 'A10BH01', 'Đái tháo đường',        45000, 35000, 20,  1, NOW()),
    ('d0000000-0000-0000-0000-000000000009', 1, 'TH009', 'Empagliflozin 10mg',    'Empagliflozin',          'Jardiance',    'Viên nén',  '10mg',   'Viên', 'A10BK03', 'Đái tháo đường',        55000, 42000, 20,  1, NOW()),
    ('d0000000-0000-0000-0000-000000000010', 1, 'TH010', 'Insulin Glargine 100U', 'Insulin glargine',       'Lantus',       'Lọ tiêm',   '100U/ml','Lọ',  'A10AE04', 'Đái tháo đường',        350000,280000,10,  1, NOW()),
    ('d0000000-0000-0000-0000-000000000011', 1, 'TH011', 'Losartan 50mg',         'Losartan kali',          'Cozaar',       'Viên nén',  '50mg',   'Viên', 'C09CA01', 'Tim mạch - huyết áp',   3500,  2800,  50,  1, NOW()),
    ('d0000000-0000-0000-0000-000000000012', 1, 'TH012', 'Valsartan 80mg',        'Valsartan',              'Diovan',       'Viên nang', '80mg',   'Viên', 'C09CA03', 'Tim mạch - huyết áp',   5500,  4200,  50,  1, NOW()),
    ('d0000000-0000-0000-0000-000000000013', 1, 'TH013', 'Bisoprolol 5mg',        'Bisoprolol fumarate',    'Concor',       'Viên nén',  '5mg',    'Viên', 'C07AB07', 'Tim mạch',              3200,  2500,  50,  1, NOW()),
    ('d0000000-0000-0000-0000-000000000014', 1, 'TH014', 'Rosuvastatin 10mg',     'Rosuvastatin calci',     'Crestor',      'Viên nén',  '10mg',   'Viên', 'C10AA07', 'Rối loạn lipid',        8500,  6500,  30,  1, NOW()),
    ('d0000000-0000-0000-0000-000000000015', 1, 'TH015', 'Aspirin 81mg',          'Acetylsalicylic acid',   'Aspirin C',    'Viên nén',  '81mg',   'Viên', 'B01AC06', 'Chống kết tập tiểu cầu',200,   150,   200, 1, NOW()),
    ('d0000000-0000-0000-0000-000000000016', 1, 'TH016', 'Clopidogrel 75mg',      'Clopidogrel bisulfate',  'Plavix',       'Viên nén',  '75mg',   'Viên', 'B01AC04', 'Chống kết tập tiểu cầu',8000,  6000,  30,  1, NOW()),
    ('d0000000-0000-0000-0000-000000000017', 1, 'TH017', 'Furosemide 40mg',       'Furosemide',             'Lasix',        'Viên nén',  '40mg',   'Viên', 'C03CA01', 'Lợi tiểu',             300,   200,   100, 1, NOW()),
    ('d0000000-0000-0000-0000-000000000018', 1, 'TH018', 'Spironolactone 25mg',   'Spironolactone',         'Aldactone',    'Viên nén',  '25mg',   'Viên', 'C03DA01', 'Lợi tiểu',             600,   450,   50,  1, NOW()),
    ('d0000000-0000-0000-0000-000000000019', 1, 'TH019', 'Levothyroxine 50mcg',   'Levothyroxine sodium',   'Euthyrox',     'Viên nén',  '50mcg',  'Viên', 'H03AA01', 'Nội tiết',             2500,  1800,  50,  1, NOW()),
    ('d0000000-0000-0000-0000-000000000020', 1, 'TH020', 'Prednisolone 5mg',      'Prednisolone',           'Prednisolone', 'Viên nén',  '5mg',    'Viên', 'H02AB06', 'Corticosteroid',       400,   300,   100, 1, NOW()),
    ('d0000000-0000-0000-0000-000000000021', 1, 'TH021', 'Amoxicillin 500mg',     'Amoxicillin trihydrate', 'Augmentin',    'Viên nang', '500mg',  'Viên', 'J01CA04', 'Kháng sinh',           1500,  1000,  100, 1, NOW()),
    ('d0000000-0000-0000-0000-000000000022', 1, 'TH022', 'Azithromycin 500mg',    'Azithromycin',           'Zithromax',    'Viên nén',  '500mg',  'Viên', 'J01FA10', 'Kháng sinh',           8000,  6000,  30,  1, NOW()),
    ('d0000000-0000-0000-0000-000000000023', 1, 'TH023', 'Cetirizine 10mg',       'Cetirizine HCl',         'Zyrtec',       'Viên nén',  '10mg',   'Viên', 'R06AE07', 'Kháng histamine',      1200,  900,   100, 1, NOW()),
    ('d0000000-0000-0000-0000-000000000024', 1, 'TH024', 'Montelukast 10mg',      'Montelukast sodium',     'Singulair',    'Viên nhai', '10mg',   'Viên', 'R03DC03', 'Hô hấp',               12000, 9000,  30,  1, NOW()),
    ('d0000000-0000-0000-0000-000000000025', 1, 'TH025', 'Esomeprazole 40mg',     'Esomeprazole magnesium', 'Nexium',       'Viên nang', '40mg',   'Viên', 'A02BC05', 'Tiêu hóa',             8500,  6500,  50,  1, NOW()),
    ('d0000000-0000-0000-0000-000000000026', 1, 'TH026', 'Pantoprazole 40mg',     'Pantoprazole sodium',    'Protonix',     'Viên nén',  '40mg',   'Viên', 'A02BC02', 'Tiêu hóa',             4500,  3500,  50,  1, NOW()),
    ('d0000000-0000-0000-0000-000000000027', 1, 'TH027', 'Vitamin D3 1000IU',     'Cholecalciferol',        'Vigantol',     'Viên nhai', '1000IU', 'Viên', 'A11CC05', 'Vitamin khoáng chất',  3500,  NULL,  50,  1, NOW()),
    ('d0000000-0000-0000-0000-000000000028', 1, 'TH028', 'Calcium 500mg',         'Calcium carbonate',      'Caltrate',     'Viên nén',  '500mg',  'Viên', 'A12AA04', 'Vitamin khoáng chất',  2000,  NULL,  50,  1, NOW()),
    ('d0000000-0000-0000-0000-000000000029', 1, 'TH029', 'Metoprolol 50mg',       'Metoprolol succinate',   'Betaloc',      'Viên nén',  '50mg',   'Viên', 'C07AB02', 'Tim mạch',             2000,  1500,  50,  1, NOW()),
    ('d0000000-0000-0000-0000-000000000030', 1, 'TH030', 'Nifedipine 10mg',       'Nifedipine',             'Adalat',       'Viên nang', '10mg',   'Viên', 'C08CA05', 'Tim mạch - huyết áp',  1200,  900,   50,  1, NOW());

-- ============================================================
-- PHẦN 2: 30+ LÔ TỒN KHO với trạng thái hết hạn đa dạng
-- Ngày hôm nay: 2026-05-30
-- Sắp hết hạn (<30 ngày): hết hạn trước 2026-06-29
-- Gần hết hạn (30-90 ngày): hết hạn 2026-06-29 đến 2026-08-28
-- Bình thường (>90 ngày): hết hạn sau 2026-08-28
-- ============================================================

-- 5 lô SẮP HẾT HẠN (<30 ngày — hiển thị badge đỏ)
INSERT IGNORE INTO `diab_his_pha_stock`
    (`id`, `tenant_id`, `drug_id`, `lot_number`, `mfg_date`, `exp_date`, `quantity`, `import_price`, `created_at`)
VALUES
    ('s0100000-0000-0000-0000-000000000001', 1, 'd0000000-0000-0000-0000-000000000001', 'LOT-EXP-001', '2024-06-01', '2026-06-05', 45,  280, NOW()),
    ('s0100000-0000-0000-0000-000000000002', 1, 'd0000000-0000-0000-0000-000000000002', 'LOT-EXP-002', '2024-06-01', '2026-06-10', 12,  850, NOW()),
    ('s0100000-0000-0000-0000-000000000003', 1, 'd0000000-0000-0000-0000-000000000003', 'LOT-EXP-003', '2024-06-01', '2026-06-15', 8,   1900,NOW()),
    ('s0100000-0000-0000-0000-000000000004', 1, 'd0000000-0000-0000-0000-000000000006', 'LOT-EXP-004', '2024-06-01', '2026-06-20', 30,  600, NOW()),
    ('s0100000-0000-0000-0000-000000000005', 1, 'd0000000-0000-0000-0000-000000000011', 'LOT-EXP-005', '2024-07-01', '2026-06-28', 20,  2700,NOW());

-- 8 lô GẦN HẾT HẠN (30-90 ngày — badge vàng)
INSERT IGNORE INTO `diab_his_pha_stock`
    (`id`, `tenant_id`, `drug_id`, `lot_number`, `mfg_date`, `exp_date`, `quantity`, `import_price`, `created_at`)
VALUES
    ('s0200000-0000-0000-0000-000000000001', 1, 'd0000000-0000-0000-0000-000000000007', 'LOT-NEAR-001', '2025-01-01', '2026-07-15', 60,  900, NOW()),
    ('s0200000-0000-0000-0000-000000000002', 1, 'd0000000-0000-0000-0000-000000000008', 'LOT-NEAR-002', '2025-01-01', '2026-07-20', 25,  34000,NOW()),
    ('s0200000-0000-0000-0000-000000000003', 1, 'd0000000-0000-0000-0000-000000000009', 'LOT-NEAR-003', '2025-02-01', '2026-07-30', 15,  41000,NOW()),
    ('s0200000-0000-0000-0000-000000000004', 1, 'd0000000-0000-0000-0000-000000000012', 'LOT-NEAR-004', '2025-02-01', '2026-08-05', 40,  4000, NOW()),
    ('s0200000-0000-0000-0000-000000000005', 1, 'd0000000-0000-0000-0000-000000000013', 'LOT-NEAR-005', '2025-03-01', '2026-08-10', 55,  2300, NOW()),
    ('s0200000-0000-0000-0000-000000000006', 1, 'd0000000-0000-0000-0000-000000000015', 'LOT-NEAR-006', '2025-03-01', '2026-08-15', 120, 130,  NOW()),
    ('s0200000-0000-0000-0000-000000000007', 1, 'd0000000-0000-0000-0000-000000000021', 'LOT-NEAR-007', '2025-04-01', '2026-08-20', 80,  1100, NOW()),
    ('s0200000-0000-0000-0000-000000000008', 1, 'd0000000-0000-0000-0000-000000000023', 'LOT-NEAR-008', '2025-04-01', '2026-08-25', 90,  850,  NOW());

-- 17 lô BÌNH THƯỜNG (>90 ngày)
INSERT IGNORE INTO `diab_his_pha_stock`
    (`id`, `tenant_id`, `drug_id`, `lot_number`, `mfg_date`, `exp_date`, `quantity`, `import_price`, `created_at`)
VALUES
    ('s0300000-0000-0000-0000-000000000001', 1, 'd0000000-0000-0000-0000-000000000010', 'LOT-OK-001', '2025-06-01', '2027-05-31', 8,   260000,NOW()),
    ('s0300000-0000-0000-0000-000000000002', 1, 'd0000000-0000-0000-0000-000000000014', 'LOT-OK-002', '2025-06-01', '2027-06-30', 150, 6200, NOW()),
    ('s0300000-0000-0000-0000-000000000003', 1, 'd0000000-0000-0000-0000-000000000016', 'LOT-OK-003', '2025-07-01', '2027-07-31', 60,  5800, NOW()),
    ('s0300000-0000-0000-0000-000000000004', 1, 'd0000000-0000-0000-0000-000000000017', 'LOT-OK-004', '2025-07-01', '2027-08-31', 200, 180,  NOW()),
    ('s0300000-0000-0000-0000-000000000005', 1, 'd0000000-0000-0000-0000-000000000018', 'LOT-OK-005', '2025-08-01', '2027-09-30', 100, 420,  NOW()),
    ('s0300000-0000-0000-0000-000000000006', 1, 'd0000000-0000-0000-0000-000000000019', 'LOT-OK-006', '2025-08-01', '2027-08-31', 80,  1800, NOW()),
    ('s0300000-0000-0000-0000-000000000007', 1, 'd0000000-0000-0000-0000-000000000020', 'LOT-OK-007', '2025-09-01', '2027-09-30', 150, 280,  NOW()),
    ('s0300000-0000-0000-0000-000000000008', 1, 'd0000000-0000-0000-0000-000000000022', 'LOT-OK-008', '2025-09-01', '2027-10-31', 40,  6000, NOW()),
    ('s0300000-0000-0000-0000-000000000009', 1, 'd0000000-0000-0000-0000-000000000024', 'LOT-OK-009', '2025-10-01', '2027-10-31', 50,  8800, NOW()),
    ('s0300000-0000-0000-0000-000000000010', 1, 'd0000000-0000-0000-0000-000000000025', 'LOT-OK-010', '2025-10-01', '2027-11-30', 70,  6200, NOW()),
    ('s0300000-0000-0000-0000-000000000011', 1, 'd0000000-0000-0000-0000-000000000026', 'LOT-OK-011', '2025-11-01', '2027-11-30', 90,  3200, NOW()),
    ('s0300000-0000-0000-0000-000000000012', 1, 'd0000000-0000-0000-0000-000000000027', 'LOT-OK-012', '2025-11-01', '2027-12-31', 200, 2600, NOW()),
    ('s0300000-0000-0000-0000-000000000013', 1, 'd0000000-0000-0000-0000-000000000028', 'LOT-OK-013', '2025-12-01', '2027-12-31', 180, 1400, NOW()),
    ('s0300000-0000-0000-0000-000000000014', 1, 'd0000000-0000-0000-0000-000000000029', 'LOT-OK-014', '2025-12-01', '2028-01-31', 100, 1400, NOW()),
    ('s0300000-0000-0000-0000-000000000015', 1, 'd0000000-0000-0000-0000-000000000030', 'LOT-OK-015', '2026-01-01', '2028-02-28', 120, 850,  NOW()),
    -- Lô TỒN THẤP (<50 đơn vị)
    ('s0300000-0000-0000-0000-000000000016', 1, 'd0000000-0000-0000-0000-000000000010', 'LOT-LOW-001', '2025-05-01', '2027-04-30', 5,   255000,NOW()),
    ('s0300000-0000-0000-0000-000000000017', 1, 'd0000000-0000-0000-0000-000000000008', 'LOT-LOW-002', '2025-05-01', '2027-05-31', 18,  33000, NOW());

-- ============================================================
-- PHẦN 3: 5 NHÀ CUNG CẤP VIỆT NAM
-- ============================================================
INSERT IGNORE INTO `diab_his_pha_suppliers`
    (`id`, `tenant_id`, `code`, `name`, `contact_name`, `phone`, `email`, `address`, `tax_code`, `is_active`, `created_at`)
VALUES
    ('sup00001-0000-0000-0000-000000000001', 1, 'NCC001', 'Công ty CP Xuất nhập khẩu Y tế Imexpharm', 'Nguyễn Thanh Hà',  '0292 3855 206', 'kinhdoanh@imexpharm.com.vn', '4/F Imexpharm Tower, 138A Hai Bà Trưng, Q.1, TP.HCM',  '1400157121', 1, NOW()),
    ('sup00001-0000-0000-0000-000000000002', 1, 'NCC002', 'Công ty CP Dược phẩm Domesco',             'Trần Minh Tuấn',   '0277 3869 637', 'domesco@domesco.com',          '66 Quốc lộ 30, P.Mỹ Phú, Q.Cao Lãnh, Đồng Tháp',      '1400099017', 1, NOW()),
    ('sup00001-0000-0000-0000-000000000003', 1, 'NCC003', 'Công ty CP Traphaco',                       'Lê Thị Thu Hương', '024 3827 5694', 'info@traphaco.com.vn',         '75 Yên Ninh, Ba Đình, Hà Nội',                          '0100110932', 1, NOW()),
    ('sup00001-0000-0000-0000-000000000004', 1, 'NCC004', 'Công ty CP Dược Hậu Giang',                'Phạm Văn Cường',   '0292 3891 433', 'dhg@dhgpharma.com.vn',         '288 Bis Nguyễn Văn Cừ, P.An Hòa, Q.Ninh Kiều, Cần Thơ', '1800152960', 1, NOW()),
    ('sup00001-0000-0000-0000-000000000005', 1, 'NCC005', 'STADA Việt Nam',                            'Vũ Thị Bích Ngọc', '028 3514 3858', 'info@stada.com.vn',            '40 Đại lộ Tự Do, KCN Việt Nam-Singapore, Bình Dương',   '3700351773', 1, NOW());

-- ============================================================
-- PHẦN 4: LAB ORDERS + 30 KẾT QUẢ XÉT NGHIỆM
-- Tạo 10 lab orders cho 10 encounters đã có
-- ============================================================
INSERT IGNORE INTO `diab_his_lab_orders`
    (`id`, `tenant_id`, `encounter_id`, `test_code`, `test_name`, `sample_type`, `status`,
     `ordered_at`, `ordered_by`, `created_at`)
VALUES
    ('lo000001-0000-0000-0000-000000000001', 1, 'e0000001-0000-0000-0000-000000000001', 'PANEL-DIAB', 'Bộ xét nghiệm đái tháo đường', 'Máu toàn phần', 'done', DATE_SUB(NOW(), INTERVAL 30 DAY), 'a0000000-0000-0000-0000-000000000002', DATE_SUB(NOW(), INTERVAL 30 DAY)),
    ('lo000001-0000-0000-0000-000000000002', 1, 'e0000001-0000-0000-0000-000000000002', 'PANEL-DIAB', 'Bộ xét nghiệm đái tháo đường', 'Máu toàn phần', 'done', DATE_SUB(NOW(), INTERVAL 7 DAY),  'a0000000-0000-0000-0000-000000000002', DATE_SUB(NOW(), INTERVAL 7 DAY)),
    ('lo000001-0000-0000-0000-000000000003', 1, 'e0000001-0000-0000-0000-000000000003', 'PANEL-LIPID','Bộ xét nghiệm lipid máu',       'Máu tĩnh mạch', 'done', DATE_SUB(NOW(), INTERVAL 25 DAY), 'a0000000-0000-0000-0000-000000000002', DATE_SUB(NOW(), INTERVAL 25 DAY)),
    ('lo000001-0000-0000-0000-000000000004', 1, 'e0000001-0000-0000-0000-000000000004', 'PANEL-LIPID','Bộ xét nghiệm lipid máu',       'Máu tĩnh mạch', 'done', DATE_SUB(NOW(), INTERVAL 22 DAY), 'a0000000-0000-0000-0000-000000000002', DATE_SUB(NOW(), INTERVAL 22 DAY)),
    ('lo000001-0000-0000-0000-000000000005', 1, 'e0000001-0000-0000-0000-000000000006', 'PANEL-CARD', 'Bộ xét nghiệm tim mạch',        'Máu tĩnh mạch', 'done', DATE_SUB(NOW(), INTERVAL 18 DAY), 'a0000000-0000-0000-0000-000000000002', DATE_SUB(NOW(), INTERVAL 18 DAY)),
    ('lo000001-0000-0000-0000-000000000006', 1, 'e0000001-0000-0000-0000-000000000008', 'PANEL-DIAB', 'Bộ xét nghiệm đái tháo đường', 'Máu toàn phần', 'done', DATE_SUB(NOW(), INTERVAL 12 DAY), 'a0000000-0000-0000-0000-000000000002', DATE_SUB(NOW(), INTERVAL 12 DAY)),
    ('lo000001-0000-0000-0000-000000000007', 1, 'e0000001-0000-0000-0000-000000000010', 'PANEL-FULL', 'Tổng phân tích máu + sinh hóa', 'Máu toàn phần', 'done', DATE_SUB(NOW(), INTERVAL 8 DAY),  'a0000000-0000-0000-0000-000000000002', DATE_SUB(NOW(), INTERVAL 8 DAY)),
    ('lo000001-0000-0000-0000-000000000008', 1, 'e0000001-0000-0000-0000-000000000011', 'PANEL-CARD', 'Bộ xét nghiệm tim mạch',        'Máu tĩnh mạch', 'done', DATE_SUB(NOW(), INTERVAL 6 DAY),  'a0000000-0000-0000-0000-000000000002', DATE_SUB(NOW(), INTERVAL 6 DAY)),
    ('lo000001-0000-0000-0000-000000000009', 1, 'e0000001-0000-0000-0000-000000000012', 'PANEL-DIAB', 'Bộ xét nghiệm đái tháo đường', 'Máu toàn phần', 'done', DATE_SUB(NOW(), INTERVAL 5 DAY),  'a0000000-0000-0000-0000-000000000002', DATE_SUB(NOW(), INTERVAL 5 DAY)),
    ('lo000001-0000-0000-0000-000000000010', 1, 'e0000001-0000-0000-0000-000000000013', 'PANEL-LIPID','Bộ xét nghiệm lipid máu',       'Máu tĩnh mạch', 'done', DATE_SUB(NOW(), INTERVAL 4 DAY),  'a0000000-0000-0000-0000-000000000002', DATE_SUB(NOW(), INTERVAL 4 DAY));

-- 30 KẾT QUẢ XÉT NGHIỆM (3 chỉ số × 10 orders)
INSERT IGNORE INTO `diab_his_lab_results`
    (`id`, `tenant_id`, `order_id`, `test_code`, `test_name`, `result_value`, `result_unit`,
     `normal_range`, `is_abnormal`, `result_flag`, `performed_at`, `performed_by`, `created_at`)
VALUES
    -- Order 1: BN Trần Văn Bình — đái tháo đường
    (UUID(), 1, 'lo000001-0000-0000-0000-000000000001', 'GLU',   'Đường huyết lúc đói',       '12.4', 'mmol/L',  '3.9 - 6.0',   1, 'H', DATE_SUB(NOW(), INTERVAL 30 DAY), 'KTV. Nguyễn Thị Hoa', NOW()),
    (UUID(), 1, 'lo000001-0000-0000-0000-000000000001', 'HBA1C', 'HbA1c',                     '8.2',  '%',       '< 6.5',        1, 'H', DATE_SUB(NOW(), INTERVAL 30 DAY), 'KTV. Nguyễn Thị Hoa', NOW()),
    (UUID(), 1, 'lo000001-0000-0000-0000-000000000001', 'CHOL',  'Cholesterol toàn phần',     '5.8',  'mmol/L',  '< 5.2',        1, 'H', DATE_SUB(NOW(), INTERVAL 30 DAY), 'KTV. Nguyễn Thị Hoa', NOW()),
    -- Order 2: BN Trần Văn Bình — tái khám
    (UUID(), 1, 'lo000001-0000-0000-0000-000000000002', 'GLU',   'Đường huyết lúc đói',       '9.8',  'mmol/L',  '3.9 - 6.0',   1, 'H', DATE_SUB(NOW(), INTERVAL 7 DAY),  'KTV. Nguyễn Thị Hoa', NOW()),
    (UUID(), 1, 'lo000001-0000-0000-0000-000000000002', 'HBA1C', 'HbA1c',                     '7.5',  '%',       '< 6.5',        1, 'H', DATE_SUB(NOW(), INTERVAL 7 DAY),  'KTV. Nguyễn Thị Hoa', NOW()),
    (UUID(), 1, 'lo000001-0000-0000-0000-000000000002', 'TRIG',  'Triglyceride',               '2.1',  'mmol/L',  '< 1.7',        1, 'H', DATE_SUB(NOW(), INTERVAL 7 DAY),  'KTV. Nguyễn Thị Hoa', NOW()),
    -- Order 3: BN Nguyễn Thị Lan — lipid
    (UUID(), 1, 'lo000001-0000-0000-0000-000000000003', 'CHOL',  'Cholesterol toàn phần',     '6.2',  'mmol/L',  '< 5.2',        1, 'H', DATE_SUB(NOW(), INTERVAL 25 DAY), 'KTV. Lê Văn Bảo',    NOW()),
    (UUID(), 1, 'lo000001-0000-0000-0000-000000000003', 'LDL',   'LDL-Cholesterol',            '4.1',  'mmol/L',  '< 3.0',        1, 'H', DATE_SUB(NOW(), INTERVAL 25 DAY), 'KTV. Lê Văn Bảo',    NOW()),
    (UUID(), 1, 'lo000001-0000-0000-0000-000000000003', 'TRIG',  'Triglyceride',               '1.5',  'mmol/L',  '< 1.7',        0, NULL, DATE_SUB(NOW(), INTERVAL 25 DAY), 'KTV. Lê Văn Bảo',   NOW()),
    -- Order 4: BN Lê Minh Tuấn — lipid
    (UUID(), 1, 'lo000001-0000-0000-0000-000000000004', 'CHOL',  'Cholesterol toàn phần',     '7.1',  'mmol/L',  '< 5.2',        1, 'H', DATE_SUB(NOW(), INTERVAL 22 DAY), 'KTV. Lê Văn Bảo',    NOW()),
    (UUID(), 1, 'lo000001-0000-0000-0000-000000000004', 'LDL',   'LDL-Cholesterol',            '4.9',  'mmol/L',  '< 3.0',        1, 'H', DATE_SUB(NOW(), INTERVAL 22 DAY), 'KTV. Lê Văn Bảo',    NOW()),
    (UUID(), 1, 'lo000001-0000-0000-0000-000000000004', 'HDL',   'HDL-Cholesterol',            '0.9',  'mmol/L',  '> 1.0',        1, 'L', DATE_SUB(NOW(), INTERVAL 22 DAY), 'KTV. Lê Văn Bảo',    NOW()),
    -- Order 5: BN Hoàng Văn Đức — tim mạch
    (UUID(), 1, 'lo000001-0000-0000-0000-000000000005', 'TROP',  'Troponin I',                 '0.02', 'ng/mL',   '< 0.04',       0, NULL, DATE_SUB(NOW(), INTERVAL 18 DAY), 'KTV. Nguyễn Thị Hoa',NOW()),
    (UUID(), 1, 'lo000001-0000-0000-0000-000000000005', 'CK',    'Creatine Kinase (CK)',       '88',   'U/L',     '< 200',        0, NULL, DATE_SUB(NOW(), INTERVAL 18 DAY), 'KTV. Nguyễn Thị Hoa',NOW()),
    (UUID(), 1, 'lo000001-0000-0000-0000-000000000005', 'GLU',   'Đường huyết lúc đói',       '11.3', 'mmol/L',  '3.9 - 6.0',   1, 'H', DATE_SUB(NOW(), INTERVAL 18 DAY), 'KTV. Nguyễn Thị Hoa',NOW()),
    -- Order 6: BN Đặng Quốc Hùng — đái tháo đường
    (UUID(), 1, 'lo000001-0000-0000-0000-000000000006', 'GLU',   'Đường huyết lúc đói',       '8.1',  'mmol/L',  '3.9 - 6.0',   1, 'H', DATE_SUB(NOW(), INTERVAL 12 DAY), 'KTV. Lê Văn Bảo',    NOW()),
    (UUID(), 1, 'lo000001-0000-0000-0000-000000000006', 'HBA1C', 'HbA1c',                     '7.1',  '%',       '< 6.5',        1, 'H', DATE_SUB(NOW(), INTERVAL 12 DAY), 'KTV. Lê Văn Bảo',    NOW()),
    (UUID(), 1, 'lo000001-0000-0000-0000-000000000006', 'UREA',  'Urê máu',                   '5.2',  'mmol/L',  '2.8 - 7.5',   0, NULL, DATE_SUB(NOW(), INTERVAL 12 DAY), 'KTV. Lê Văn Bảo',    NOW()),
    -- Order 7: BN Lý Văn Phong — toàn phần
    (UUID(), 1, 'lo000001-0000-0000-0000-000000000007', 'CBC',   'Công thức máu toàn phần',   'Xem chi tiết', NULL, 'Xem bình thường', 0, NULL, DATE_SUB(NOW(), INTERVAL 8 DAY), 'KTV. Nguyễn Thị Hoa',NOW()),
    (UUID(), 1, 'lo000001-0000-0000-0000-000000000007', 'GLU',   'Đường huyết lúc đói',       '5.1',  'mmol/L',  '3.9 - 6.0',   0, NULL, DATE_SUB(NOW(), INTERVAL 8 DAY),  'KTV. Nguyễn Thị Hoa',NOW()),
    (UUID(), 1, 'lo000001-0000-0000-0000-000000000007', 'CREA',  'Creatinine máu',             '82',   'µmol/L',  '62 - 115',     0, NULL, DATE_SUB(NOW(), INTERVAL 8 DAY),  'KTV. Nguyễn Thị Hoa',NOW()),
    -- Order 8: BN Trịnh Thị Nga — tim mạch
    (UUID(), 1, 'lo000001-0000-0000-0000-000000000008', 'CHOL',  'Cholesterol toàn phần',     '6.5',  'mmol/L',  '< 5.2',        1, 'H', DATE_SUB(NOW(), INTERVAL 6 DAY),  'KTV. Lê Văn Bảo',    NOW()),
    (UUID(), 1, 'lo000001-0000-0000-0000-000000000008', 'TRIG',  'Triglyceride',               '3.2',  'mmol/L',  '< 1.7',        1, 'H', DATE_SUB(NOW(), INTERVAL 6 DAY),  'KTV. Lê Văn Bảo',    NOW()),
    (UUID(), 1, 'lo000001-0000-0000-0000-000000000008', 'GLU',   'Đường huyết lúc đói',       '5.8',  'mmol/L',  '3.9 - 6.0',   0, NULL, DATE_SUB(NOW(), INTERVAL 6 DAY),  'KTV. Lê Văn Bảo',    NOW()),
    -- Order 9: BN Trần Văn Bình — tái khám 2
    (UUID(), 1, 'lo000001-0000-0000-0000-000000000009', 'GLU',   'Đường huyết lúc đói',       '8.9',  'mmol/L',  '3.9 - 6.0',   1, 'H', DATE_SUB(NOW(), INTERVAL 5 DAY),  'KTV. Nguyễn Thị Hoa',NOW()),
    (UUID(), 1, 'lo000001-0000-0000-0000-000000000009', 'HBA1C', 'HbA1c',                     '7.0',  '%',       '< 6.5',        1, 'H', DATE_SUB(NOW(), INTERVAL 5 DAY),  'KTV. Nguyễn Thị Hoa',NOW()),
    (UUID(), 1, 'lo000001-0000-0000-0000-000000000009', 'TRIG',  'Triglyceride',               '1.9',  'mmol/L',  '< 1.7',        1, 'H', DATE_SUB(NOW(), INTERVAL 5 DAY),  'KTV. Nguyễn Thị Hoa',NOW()),
    -- Order 10: BN Lê Minh Tuấn — tái khám lipid
    (UUID(), 1, 'lo000001-0000-0000-0000-000000000010', 'CHOL',  'Cholesterol toàn phần',     '5.9',  'mmol/L',  '< 5.2',        1, 'H', DATE_SUB(NOW(), INTERVAL 4 DAY),  'KTV. Lê Văn Bảo',    NOW()),
    (UUID(), 1, 'lo000001-0000-0000-0000-000000000010', 'LDL',   'LDL-Cholesterol',            '3.8',  'mmol/L',  '< 3.0',        1, 'H', DATE_SUB(NOW(), INTERVAL 4 DAY),  'KTV. Lê Văn Bảo',    NOW()),
    (UUID(), 1, 'lo000001-0000-0000-0000-000000000010', 'HDL',   'HDL-Cholesterol',            '1.1',  'mmol/L',  '> 1.0',        0, NULL, DATE_SUB(NOW(), INTERVAL 4 DAY),  'KTV. Lê Văn Bảo',    NOW());

-- ============================================================
-- PHẦN 5: REPORT CACHE — Dashboard widget
-- tenant_id cột là CHAR(36) theo schema 0053
-- ============================================================
INSERT INTO `diab_his_rep_daily_revenue_cache`
    (`tenant_id`, `period_key`, `data_json`, `refreshed_at`)
VALUES
    ('1', '2026-05-30', JSON_OBJECT('total', 3850000, 'cash', 1250000, 'card', 980000, 'qr', 420000, 'bhyt', 1200000, 'visits', 12), NOW()),
    ('1', '2026-05-29', JSON_OBJECT('total', 4120000, 'cash', 1650000, 'card', 870000, 'qr', 600000, 'bhyt', 1000000, 'visits', 14), NOW()),
    ('1', '2026-05-28', JSON_OBJECT('total', 2980000, 'cash', 980000,  'card', 750000, 'qr', 250000, 'bhyt', 1000000, 'visits', 9),  NOW()),
    ('1', '2026-05',    JSON_OBJECT('total', 87500000,'cash', 28000000,'card', 22000000,'qr',12000000,'bhyt',25500000,'visits',282), NOW()),
    ('1', '2026-04',    JSON_OBJECT('total', 79200000,'cash', 25500000,'card', 19800000,'qr',10200000,'bhyt',23700000,'visits',257), NOW())
ON DUPLICATE KEY UPDATE data_json = VALUES(data_json), refreshed_at = NOW();

INSERT INTO `diab_his_rep_doctor_kpi_cache`
    (`tenant_id`, `period_key`, `data_json`, `refreshed_at`)
VALUES
    ('1', '2026-05', JSON_ARRAY(
        JSON_OBJECT('doctor_id', 'a0000000-0000-0000-0000-000000000002', 'name', 'BS. Nguyễn Văn An', 'visits', 282, 'revenue', 87500000, 'avg_time_min', 28, 'satisfaction', 4.7)
    ), NOW())
ON DUPLICATE KEY UPDATE data_json = VALUES(data_json), refreshed_at = NOW();

INSERT INTO `diab_his_rep_top_drugs_cache`
    (`tenant_id`, `period_key`, `data_json`, `refreshed_at`)
VALUES
    ('1', '2026-05', JSON_ARRAY(
        JSON_OBJECT('drug_id', 'd0000000-0000-0000-0000-000000000001', 'name', 'Metformin 500mg', 'qty', 1860, 'revenue', 930000),
        JSON_OBJECT('drug_id', 'd0000000-0000-0000-0000-000000000002', 'name', 'Amlodipine 5mg',  'qty', 870,  'revenue', 1305000),
        JSON_OBJECT('drug_id', 'd0000000-0000-0000-0000-000000000003', 'name', 'Atorvastatin 20mg','qty',660,  'revenue', 2310000),
        JSON_OBJECT('drug_id', 'd0000000-0000-0000-0000-000000000011', 'name', 'Losartan 50mg',   'qty', 540,  'revenue', 1890000),
        JSON_OBJECT('drug_id', 'd0000000-0000-0000-0000-000000000005', 'name', 'Omeprazole 20mg', 'qty', 420,  'revenue', 504000)
    ), NOW())
ON DUPLICATE KEY UPDATE data_json = VALUES(data_json), refreshed_at = NOW();

INSERT INTO `diab_his_rep_inventory_value_cache`
    (`tenant_id`, `period_key`, `data_json`, `refreshed_at`)
VALUES
    ('1', '2026-05-30', JSON_OBJECT('total_value', 125800000, 'drug_count', 30, 'stock_entries', 30, 'near_expiry_count', 8, 'expired_count', 0, 'low_stock_count', 5), NOW())
ON DUPLICATE KEY UPDATE data_json = VALUES(data_json), refreshed_at = NOW();

INSERT INTO `diab_his_rep_diabetes_cohort_cache`
    (`tenant_id`, `period_key`, `data_json`, `refreshed_at`)
VALUES
    ('1', '2026-05', JSON_OBJECT('total_patients', 6, 'hba1c_controlled', 2, 'hba1c_uncontrolled', 4, 'avg_hba1c', 7.6, 'avg_fasting_glucose', 9.4, 'on_insulin', 1, 'on_oral_only', 5), NOW())
ON DUPLICATE KEY UPDATE data_json = VALUES(data_json), refreshed_at = NOW();

-- ============================================================
-- PHẦN 6: 5 THÔNG BÁO CHO ADMIN
-- recipient_user_id dùng INT — admin user id cần xác định từ sec_users
-- Dùng subquery để lấy id của admin
-- ============================================================
-- user_id CHAR(36), id của admin trong sec_users
INSERT IGNORE INTO `diab_his_nti_notifications`
    (`tenant_id`, `user_id`, `type`, `title`, `body`, `data_json`, `created_at`)
SELECT
    1,
    (SELECT id FROM diab_his_sec_users WHERE email = 'admin@prodiab.local' LIMIT 1),
    'SYSTEM_WELCOME',
    'Chào mừng đến với Pro-Diab HIS',
    'Hệ thống đã được khởi tạo thành công. Hãy bắt đầu bằng cách thiết lập thông tin phòng khám.',
    JSON_OBJECT('action', 'SETUP_CLINIC'),
    DATE_SUB(NOW(), INTERVAL 30 DAY)
UNION ALL
SELECT
    1,
    (SELECT id FROM diab_his_sec_users WHERE email = 'admin@prodiab.local' LIMIT 1),
    'DRUG_EXPIRY_ALERT',
    'Cảnh báo: 5 lô thuốc sắp hết hạn',
    'Có 5 lô thuốc sẽ hết hạn trong vòng 30 ngày tới. Vui lòng kiểm tra và xử lý kịp thời.',
    JSON_OBJECT('count', 5, 'action', 'VIEW_EXPIRY', 'url', '/pharmacy?tab=stock&filter=near_expiry'),
    DATE_SUB(NOW(), INTERVAL 2 DAY)
UNION ALL
SELECT
    1,
    (SELECT id FROM diab_his_sec_users WHERE email = 'admin@prodiab.local' LIMIT 1),
    'PAYMENT_RECEIVED',
    'Thanh toán nhận được: 3.850.000 VNĐ',
    'Tổng doanh thu hôm nay đã đạt 3.850.000 VNĐ từ 12 lượt khám.',
    JSON_OBJECT('amount', 3850000, 'visits', 12, 'date', '2026-05-30'),
    DATE_SUB(NOW(), INTERVAL 1 HOUR)
UNION ALL
SELECT
    1,
    (SELECT id FROM diab_his_sec_users WHERE email = 'admin@prodiab.local' LIMIT 1),
    'LOW_STOCK_ALERT',
    'Cảnh báo tồn kho thấp: Insulin Glargine',
    'Insulin Glargine 100U/ml chỉ còn 5 lọ trong kho. Vui lòng đặt hàng bổ sung.',
    JSON_OBJECT('drug_id', 'd0000000-0000-0000-0000-000000000010', 'drug_name', 'Insulin Glargine 100U', 'quantity', 5, 'reorder_level', 10),
    DATE_SUB(NOW(), INTERVAL 3 HOUR)
UNION ALL
SELECT
    1,
    (SELECT id FROM diab_his_sec_users WHERE email = 'admin@prodiab.local' LIMIT 1),
    'LAB_RESULT_READY',
    'Kết quả xét nghiệm đã sẵn sàng',
    'Kết quả xét nghiệm HbA1c và đường huyết của BN Trần Văn Bình (BN00001) đã có.',
    JSON_OBJECT('patient_id', 'f0000000-0000-0000-0000-000000000001', 'patient_name', 'Trần Văn Bình', 'order_id', 'lo000001-0000-0000-0000-000000000009'),
    DATE_SUB(NOW(), INTERVAL 5 HOUR);

-- ============================================================
-- PHẦN 7: 10 LỊCH HẸN TƯƠNG LAI
-- 3 hôm nay, 5 tuần này, 2 tháng tới
-- ============================================================
INSERT IGNORE INTO `diab_his_sch_appointments`
    (`id`, `tenant_id`, `patient_id`, `doctor_id`, `appointment_date`, `appointment_time`, `status`, `note`, `created_at`)
VALUES
    -- Hôm nay 2026-05-30
    ('apt00001-0000-0000-0000-000000000001', 1, 'f0000000-0000-0000-0000-000000000001', 'a0000000-0000-0000-0000-000000000002', '2026-05-30', '08:30:00', 'SCHEDULED', 'Tái khám đái tháo đường 3 tháng', NOW()),
    ('apt00001-0000-0000-0000-000000000002', 1, 'f0000000-0000-0000-0000-000000000003', 'a0000000-0000-0000-0000-000000000002', '2026-05-30', '10:00:00', 'SCHEDULED', 'Tái khám kiểm tra lipid máu', NOW()),
    ('apt00001-0000-0000-0000-000000000003', 1, 'f0000000-0000-0000-0000-000000000005', 'a0000000-0000-0000-0000-000000000002', '2026-05-30', '14:30:00', 'SCHEDULED', 'Tái khám suy tim — đo ECG', NOW()),
    -- Tuần này (2026-05-31 đến 2026-06-05)
    ('apt00001-0000-0000-0000-000000000004', 1, 'f0000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000002', '2026-06-02', '09:00:00', 'SCHEDULED', 'Tái khám tăng huyết áp 1 tháng', NOW()),
    ('apt00001-0000-0000-0000-000000000005', 1, 'f0000000-0000-0000-0000-000000000004', 'a0000000-0000-0000-0000-000000000002', '2026-06-03', '08:30:00', 'SCHEDULED', 'Kiểm tra sau điều trị đau lưng', NOW()),
    ('apt00001-0000-0000-0000-000000000006', 1, 'f0000000-0000-0000-0000-000000000007', 'a0000000-0000-0000-0000-000000000002', '2026-06-03', '10:30:00', 'SCHEDULED', 'Tái khám đái tháo đường + XN đường huyết', NOW()),
    ('apt00001-0000-0000-0000-000000000007', 1, 'f0000000-0000-0000-0000-000000000008', 'a0000000-0000-0000-0000-000000000002', '2026-06-04', '09:00:00', 'SCHEDULED', 'Tái khám viêm dạ dày 2 tuần', NOW()),
    ('apt00001-0000-0000-0000-000000000008', 1, 'f0000000-0000-0000-0000-000000000010', 'a0000000-0000-0000-0000-000000000002', '2026-06-05', '15:00:00', 'SCHEDULED', 'Tái khám tuyến giáp — kiểm tra hormone', NOW()),
    -- Tháng tới
    ('apt00001-0000-0000-0000-000000000009', 1, 'f0000000-0000-0000-0000-000000000006', 'a0000000-0000-0000-0000-000000000002', '2026-06-15', '08:30:00', 'SCHEDULED', 'Khám sức khỏe định kỳ 6 tháng', NOW()),
    ('apt00001-0000-0000-0000-000000000010', 1, 'f0000000-0000-0000-0000-000000000009', 'a0000000-0000-0000-0000-000000000002', '2026-06-20', '10:00:00', 'SCHEDULED', 'Tái khám đái tháo đường 3 tháng + XN HbA1c', NOW());

-- ============================================================
-- PHẦN 8: 15 PAYMENT BỔ SUNG (tổng > 25 payments)
-- Các hóa đơn b0000001-... đã có trong 9008, bổ sung thêm
-- ============================================================
INSERT IGNORE INTO `diab_his_bil_billing`
    (`id`, `tenant_id`, `patient_id`, `encounter_id`, `bill_no`,
     `payer`, `subtotal`, `patient_payable`, `paid_amount`, `balance`,
     `status`, `finalized_at`, `created_at`)
VALUES
    ('b0000002-0000-0000-0000-000000000001', 1, 'f0000000-0000-0000-0000-000000000006', 'e0000001-0000-0000-0000-000000000007', 'HD-2025-00011', 'SELF',   150000, 150000, 150000, 0, 'PAID', DATE_SUB(NOW(), INTERVAL 15 DAY), DATE_SUB(NOW(), INTERVAL 15 DAY)),
    ('b0000002-0000-0000-0000-000000000002', 1, 'f0000000-0000-0000-0000-000000000007', 'e0000001-0000-0000-0000-000000000008', 'HD-2025-00012', 'BHYT',   320000, 80000,  80000,  0, 'PAID', DATE_SUB(NOW(), INTERVAL 12 DAY), DATE_SUB(NOW(), INTERVAL 12 DAY)),
    ('b0000002-0000-0000-0000-000000000003', 1, 'f0000000-0000-0000-0000-000000000008', 'e0000001-0000-0000-0000-000000000009', 'HD-2025-00013', 'BHYT',   186000, 50000,  50000,  0, 'PAID', DATE_SUB(NOW(), INTERVAL 10 DAY), DATE_SUB(NOW(), INTERVAL 10 DAY)),
    ('b0000002-0000-0000-0000-000000000004', 1, 'f0000000-0000-0000-0000-000000000009', 'e0000001-0000-0000-0000-000000000010', 'HD-2025-00014', 'SELF',   280000, 280000, 280000, 0, 'PAID', DATE_SUB(NOW(), INTERVAL 8 DAY),  DATE_SUB(NOW(), INTERVAL 8 DAY)),
    ('b0000002-0000-0000-0000-000000000005', 1, 'f0000000-0000-0000-0000-000000000010', 'e0000001-0000-0000-0000-000000000011', 'HD-2025-00015', 'BHYT',   145000, 45000,  45000,  0, 'PAID', DATE_SUB(NOW(), INTERVAL 6 DAY),  DATE_SUB(NOW(), INTERVAL 6 DAY)),
    ('b0000002-0000-0000-0000-000000000006', 1, 'f0000000-0000-0000-0000-000000000001', 'e0000001-0000-0000-0000-000000000012', 'HD-2025-00016', 'BHYT',   175000, 55000,  55000,  0, 'PAID', DATE_SUB(NOW(), INTERVAL 5 DAY),  DATE_SUB(NOW(), INTERVAL 5 DAY)),
    ('b0000002-0000-0000-0000-000000000007', 1, 'f0000000-0000-0000-0000-000000000003', 'e0000001-0000-0000-0000-000000000013', 'HD-2025-00017', 'SELF',   220000, 220000, 220000, 0, 'PAID', DATE_SUB(NOW(), INTERVAL 4 DAY),  DATE_SUB(NOW(), INTERVAL 4 DAY)),
    ('b0000002-0000-0000-0000-000000000008', 1, 'f0000000-0000-0000-0000-000000000005', 'e0000001-0000-0000-0000-000000000014', 'HD-2025-00018', 'BHYT',   390000, 110000, 110000, 0, 'PAID', DATE_SUB(NOW(), INTERVAL 3 DAY),  DATE_SUB(NOW(), INTERVAL 3 DAY)),
    ('b0000002-0000-0000-0000-000000000009', 1, 'f0000000-0000-0000-0000-000000000002', 'e0000001-0000-0000-0000-000000000015', 'HD-2025-00019', 'SELF',   195000, 195000, 195000, 0, 'PAID', DATE_SUB(NOW(), INTERVAL 2 DAY),  DATE_SUB(NOW(), INTERVAL 2 DAY)),
    ('b0000002-0000-0000-0000-000000000010', 1, 'f0000000-0000-0000-0000-000000000004', 'e0000001-0000-0000-0000-000000000016', 'HD-2025-00020', 'SELF',   165000, 165000, 150000, 15000, 'PARTIAL_PAID', DATE_SUB(NOW(), INTERVAL 1 DAY), DATE_SUB(NOW(), INTERVAL 1 DAY)),
    -- 5 hóa đơn hôm nay
    ('b0000002-0000-0000-0000-000000000011', 1, 'f0000000-0000-0000-0000-000000000001', 'e0000001-0000-0000-0000-000000000017', 'HD-2026-00001', 'BHYT',   260000, 75000,  75000,  0, 'PAID', NOW(), NOW()),
    ('b0000002-0000-0000-0000-000000000012', 1, 'f0000000-0000-0000-0000-000000000003', 'e0000001-0000-0000-0000-000000000018', 'HD-2026-00002', 'SELF',   180000, 180000, 180000, 0, 'PAID', NOW(), NOW()),
    ('b0000002-0000-0000-0000-000000000013', 1, 'f0000000-0000-0000-0000-000000000006', NULL,                                   'HD-2026-00003', 'SELF',   320000, 320000, 0,      320000,'FINALIZED', NOW(), NOW()),
    ('b0000002-0000-0000-0000-000000000014', 1, 'f0000000-0000-0000-0000-000000000008', NULL,                                   'HD-2026-00004', 'BHYT',   420000, 120000, 120000, 0, 'PAID', NOW(), NOW()),
    ('b0000002-0000-0000-0000-000000000015', 1, 'f0000000-0000-0000-0000-000000000009', NULL,                                   'HD-2026-00005', 'SELF',   150000, 150000, 150000, 0, 'PAID', NOW(), NOW());

SET FOREIGN_KEY_CHECKS = 1;
