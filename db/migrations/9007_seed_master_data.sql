-- ============================================================
-- Migration: 9007_seed_master_data
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-29
-- Tác giả: Pro-Diab Team
-- Mô tả: Seed dữ liệu danh mục hệ thống:
--        - 60 quyền hạn (permissions) theo pattern resource.action
--        - 6 vai trò chuẩn + phân quyền mặc định
--        - ~100 mã ICD-10 thông dụng
--        - Đơn vị thuốc
--        - 50 dịch vụ khám/CLS/thủ thuật cơ bản
-- Idempotent: YES (INSERT IGNORE)
-- ============================================================
SET NAMES utf8mb4;

-- ============================================================
-- PHẦN 1: Bảng tham chiếu ICD-10
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_ref_icd10` (
    `id`            INT             NOT NULL AUTO_INCREMENT  COMMENT 'ID tự tăng',
    `code`          VARCHAR(10)     NOT NULL                 COMMENT 'Mã ICD-10',
    `name_vi`       VARCHAR(500)    NOT NULL                 COMMENT 'Tên bệnh tiếng Việt',
    `category`      VARCHAR(50)     NULL                     COMMENT 'Nhóm bệnh',

    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_icd10_code` (`code`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Danh mục mã bệnh ICD-10 thông dụng';

-- ============================================================
-- PHẦN 2: Bảng đơn vị thuốc
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_ref_drug_units` (
    `id`    INT             NOT NULL AUTO_INCREMENT  COMMENT 'ID tự tăng',
    `code`  VARCHAR(20)     NOT NULL                 COMMENT 'Mã đơn vị',
    `name`  VARCHAR(50)     NOT NULL                 COMMENT 'Tên đơn vị',

    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_drug_units_code` (`code`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Danh mục đơn vị tính thuốc';

-- ============================================================
-- PHẦN 3: Bảng danh mục dịch vụ
-- ============================================================
CREATE TABLE IF NOT EXISTS `diab_his_ref_services` (
    `id`            INT             NOT NULL AUTO_INCREMENT  COMMENT 'ID tự tăng',
    `code`          VARCHAR(30)     NOT NULL                 COMMENT 'Mã dịch vụ',
    `name`          VARCHAR(255)    NOT NULL                 COMMENT 'Tên dịch vụ',
    `category`      VARCHAR(50)     NOT NULL                 COMMENT 'Nhóm: EXAM, LAB, RADIOLOGY, PROCEDURE',
    `unit`          VARCHAR(20)     NOT NULL DEFAULT 'Lần'   COMMENT 'Đơn vị tính',
    `base_price`    DECIMAL(12,2)   NOT NULL DEFAULT 0       COMMENT 'Giá cơ bản (VNĐ)',
    `bhyt_price`    DECIMAL(12,2)   NULL                     COMMENT 'Giá BHYT thanh toán',
    `is_active`     TINYINT(1)      NOT NULL DEFAULT 1       COMMENT 'Còn áp dụng',

    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_services_code` (`code`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Danh mục dịch vụ khám chữa bệnh và giá';

-- ============================================================
-- SEED: Quyền hạn (permissions)
-- ============================================================
INSERT IGNORE INTO `diab_his_sec_permissions`
    (`id`, `code`, `resource`, `action`, `description`, `created_at`)
VALUES
    -- Quản lý bệnh nhân
    (UUID(), 'patient.read',            'patient',      'read',     'Xem thông tin bệnh nhân',                  NOW()),
    (UUID(), 'patient.create',          'patient',      'create',   'Tạo hồ sơ bệnh nhân mới',                  NOW()),
    (UUID(), 'patient.update',          'patient',      'update',   'Cập nhật hồ sơ bệnh nhân',                 NOW()),
    (UUID(), 'patient.delete',          'patient',      'delete',   'Xóa hồ sơ bệnh nhân',                      NOW()),
    (UUID(), 'patient.export',          'patient',      'export',   'Xuất danh sách bệnh nhân',                 NOW()),
    -- Tiếp đón
    (UUID(), 'reception.read',          'reception',    'read',     'Xem danh sách tiếp đón',                   NOW()),
    (UUID(), 'reception.create',        'reception',    'create',   'Tạo phiếu tiếp đón',                       NOW()),
    (UUID(), 'reception.update',        'reception',    'update',   'Cập nhật phiếu tiếp đón',                  NOW()),
    (UUID(), 'reception.cancel',        'reception',    'cancel',   'Hủy phiếu tiếp đón',                       NOW()),
    -- Lượt khám
    (UUID(), 'encounter.read',          'encounter',    'read',     'Xem lượt khám',                            NOW()),
    (UUID(), 'encounter.create',        'encounter',    'create',   'Tạo lượt khám mới',                        NOW()),
    (UUID(), 'encounter.update',        'encounter',    'update',   'Cập nhật lượt khám',                       NOW()),
    (UUID(), 'encounter.delete',        'encounter',    'delete',   'Xóa lượt khám',                            NOW()),
    (UUID(), 'encounter.sign',          'encounter',    'sign',     'Ký số bệnh án điện tử',                    NOW()),
    -- Chẩn đoán
    (UUID(), 'diagnosis.read',          'diagnosis',    'read',     'Xem chẩn đoán ICD-10',                     NOW()),
    (UUID(), 'diagnosis.write',         'diagnosis',    'write',    'Nhập/sửa chẩn đoán ICD-10',                NOW()),
    -- Sinh hiệu
    (UUID(), 'vitals.read',             'vitals',       'read',     'Xem chỉ số sinh hiệu',                     NOW()),
    (UUID(), 'vitals.write',            'vitals',       'write',    'Nhập/cập nhật sinh hiệu',                  NOW()),
    -- Cận lâm sàng
    (UUID(), 'cls.read',                'cls',          'read',     'Xem chỉ định và kết quả CLS',              NOW()),
    (UUID(), 'cls.order',               'cls',          'order',    'Chỉ định xét nghiệm / CĐHA',               NOW()),
    (UUID(), 'cls.result',              'cls',          'result',   'Nhập kết quả xét nghiệm / CĐHA',           NOW()),
    (UUID(), 'cls.upload',              'cls',          'upload',   'Upload tài liệu CLS',                      NOW()),
    -- Kê đơn thuốc
    (UUID(), 'prescription.read',       'prescription', 'read',     'Xem đơn thuốc',                            NOW()),
    (UUID(), 'prescription.create',     'prescription', 'create',   'Kê đơn thuốc mới',                         NOW()),
    (UUID(), 'prescription.update',     'prescription', 'update',   'Sửa đơn thuốc chưa ký',                    NOW()),
    (UUID(), 'prescription.sign',       'prescription', 'sign',     'Ký đơn thuốc',                             NOW()),
    (UUID(), 'prescription.cancel',     'prescription', 'cancel',   'Hủy đơn thuốc',                            NOW()),
    (UUID(), 'prescription.dtqg',       'prescription', 'dtqg',     'Đẩy đơn lên Đơn thuốc Quốc gia',          NOW()),
    -- Cấp phát thuốc
    (UUID(), 'dispense.read',           'dispense',     'read',     'Xem phiếu cấp phát',                       NOW()),
    (UUID(), 'dispense.create',         'dispense',     'create',   'Cấp phát thuốc',                           NOW()),
    -- Kho dược
    (UUID(), 'pharmacy.read',           'pharmacy',     'read',     'Xem tồn kho',                              NOW()),
    (UUID(), 'pharmacy.import',         'pharmacy',     'import',   'Nhập kho thuốc',                           NOW()),
    (UUID(), 'pharmacy.adjust',         'pharmacy',     'adjust',   'Điều chỉnh tồn kho',                       NOW()),
    (UUID(), 'pharmacy.drug_create',    'pharmacy',     'drug_create','Thêm thuốc vào danh mục',                NOW()),
    (UUID(), 'pharmacy.drug_update',    'pharmacy',     'drug_update','Sửa thông tin thuốc',                    NOW()),
    (UUID(), 'pharmacy.drug_delete',    'pharmacy',     'drug_delete','Xóa thuốc khỏi danh mục',                NOW()),
    (UUID(), 'pharmacy.inventory',      'pharmacy',     'inventory', 'Kiểm kê kho',                             NOW()),
    -- Thu ngân
    (UUID(), 'billing.read',            'billing',      'read',     'Xem hóa đơn',                              NOW()),
    (UUID(), 'billing.create',          'billing',      'create',   'Tạo hóa đơn',                              NOW()),
    (UUID(), 'billing.update',          'billing',      'update',   'Sửa hóa đơn chưa thanh toán',              NOW()),
    (UUID(), 'billing.void',            'billing',      'void',     'Hủy hóa đơn',                              NOW()),
    (UUID(), 'billing.payment',         'billing',      'payment',  'Ghi nhận thanh toán',                      NOW()),
    (UUID(), 'billing.refund',          'billing',      'refund',   'Hoàn tiền',                                NOW()),
    -- BHYT
    (UUID(), 'bhyt.read',               'bhyt',         'read',     'Xem thông tin BHYT',                       NOW()),
    (UUID(), 'bhyt.export',             'bhyt',         'export',   'Xuất XML hồ sơ BHYT',                      NOW()),
    (UUID(), 'bhyt.submit',             'bhyt',         'submit',   'Nộp hồ sơ giám định BHYT',                 NOW()),
    -- Báo cáo
    (UUID(), 'report.read',             'report',       'read',     'Xem báo cáo',                              NOW()),
    (UUID(), 'report.export',           'report',       'export',   'Xuất báo cáo (PDF/Excel)',                  NOW()),
    (UUID(), 'report.financial',        'report',       'financial','Xem báo cáo tài chính',                    NOW()),
    (UUID(), 'report.clinical',         'report',       'clinical', 'Xem báo cáo lâm sàng',                     NOW()),
    (UUID(), 'report.pharmacy',         'report',       'pharmacy', 'Xem báo cáo dược',                         NOW()),
    -- Quản trị hệ thống
    (UUID(), 'admin.user_manage',       'admin',        'user_manage',  'Quản lý người dùng',                   NOW()),
    (UUID(), 'admin.role_manage',       'admin',        'role_manage',  'Quản lý vai trò và phân quyền',        NOW()),
    (UUID(), 'admin.tenant_config',     'admin',        'tenant_config','Cấu hình phòng khám',                  NOW()),
    (UUID(), 'admin.integration',       'admin',        'integration',  'Cấu hình tích hợp bên thứ ba',        NOW()),
    (UUID(), 'admin.audit_log',         'admin',        'audit_log',    'Xem nhật ký kiểm tra',                 NOW()),
    (UUID(), 'admin.backup',            'admin',        'backup',       'Sao lưu / phục hồi dữ liệu',           NOW()),
    -- Danh mục
    (UUID(), 'catalog.read',            'catalog',      'read',     'Xem danh mục (ICD-10, thuốc, dịch vụ)',    NOW()),
    (UUID(), 'catalog.write',           'catalog',      'write',    'Thêm/sửa danh mục',                        NOW()),
    (UUID(), 'catalog.delete',          'catalog',      'delete',   'Xóa mục trong danh mục',                   NOW());

-- ============================================================
-- SEED: Vai trò hệ thống
-- ============================================================
INSERT IGNORE INTO `diab_his_sec_roles`
    (`id`, `code`, `name`, `description`, `role_type`, `tenant_id`, `is_active`, `created_at`)
VALUES
    ('00000000-0000-0000-0000-000000000001', 'admin',            'Quản trị viên',        'Toàn quyền hệ thống',                          'SYSTEM', NULL, 1, NOW()),
    ('00000000-0000-0000-0000-000000000002', 'bac_si',           'Bác sĩ',               'Khám bệnh, kê đơn, xem hồ sơ bệnh nhân',       'SYSTEM', NULL, 1, NOW()),
    ('00000000-0000-0000-0000-000000000003', 'le_tan',           'Lễ tân',               'Tiếp đón, quản lý lịch hẹn, thu ngân cơ bản',  'SYSTEM', NULL, 1, NOW()),
    ('00000000-0000-0000-0000-000000000004', 'duoc_si',          'Dược sĩ',              'Cấp phát thuốc, quản lý kho dược',             'SYSTEM', NULL, 1, NOW()),
    ('00000000-0000-0000-0000-000000000005', 'ke_toan',          'Kế toán',              'Thu ngân, hóa đơn, báo cáo tài chính',         'SYSTEM', NULL, 1, NOW()),
    ('00000000-0000-0000-0000-000000000006', 'ky_thuat_vien',    'Kỹ thuật viên',        'Thực hiện xét nghiệm, nhập kết quả CLS',       'SYSTEM', NULL, 1, NOW());

-- ============================================================
-- SEED: Phân quyền cho vai trò
-- admin = tất cả quyền
-- ============================================================
INSERT IGNORE INTO `diab_his_sec_role_permissions` (`role_id`, `permission_id`)
SELECT '00000000-0000-0000-0000-000000000001', id FROM `diab_his_sec_permissions`;

-- bac_si: khám bệnh, kê đơn, xem CLS, xem báo cáo lâm sàng
INSERT IGNORE INTO `diab_his_sec_role_permissions` (`role_id`, `permission_id`)
SELECT '00000000-0000-0000-0000-000000000002', id
FROM `diab_his_sec_permissions`
WHERE `code` IN (
    'patient.read', 'patient.create', 'patient.update',
    'reception.read',
    'encounter.read', 'encounter.create', 'encounter.update', 'encounter.sign',
    'diagnosis.read', 'diagnosis.write',
    'vitals.read', 'vitals.write',
    'cls.read', 'cls.order',
    'prescription.read', 'prescription.create', 'prescription.update',
    'prescription.sign', 'prescription.cancel', 'prescription.dtqg',
    'billing.read',
    'bhyt.read',
    'report.read', 'report.clinical',
    'catalog.read'
);

-- le_tan: tiếp đón, bệnh nhân, thu ngân cơ bản
INSERT IGNORE INTO `diab_his_sec_role_permissions` (`role_id`, `permission_id`)
SELECT '00000000-0000-0000-0000-000000000003', id
FROM `diab_his_sec_permissions`
WHERE `code` IN (
    'patient.read', 'patient.create', 'patient.update',
    'reception.read', 'reception.create', 'reception.update', 'reception.cancel',
    'encounter.read',
    'billing.read', 'billing.create', 'billing.update', 'billing.payment',
    'report.read',
    'catalog.read'
);

-- duoc_si: kho dược, cấp phát, xem đơn thuốc
INSERT IGNORE INTO `diab_his_sec_role_permissions` (`role_id`, `permission_id`)
SELECT '00000000-0000-0000-0000-000000000004', id
FROM `diab_his_sec_permissions`
WHERE `code` IN (
    'patient.read',
    'prescription.read',
    'dispense.read', 'dispense.create',
    'pharmacy.read', 'pharmacy.import', 'pharmacy.adjust',
    'pharmacy.drug_create', 'pharmacy.drug_update', 'pharmacy.inventory',
    'report.read', 'report.pharmacy',
    'catalog.read', 'catalog.write'
);

-- ke_toan: hóa đơn, thanh toán, báo cáo tài chính, BHYT
INSERT IGNORE INTO `diab_his_sec_role_permissions` (`role_id`, `permission_id`)
SELECT '00000000-0000-0000-0000-000000000005', id
FROM `diab_his_sec_permissions`
WHERE `code` IN (
    'patient.read',
    'billing.read', 'billing.create', 'billing.update', 'billing.void',
    'billing.payment', 'billing.refund',
    'bhyt.read', 'bhyt.export', 'bhyt.submit',
    'report.read', 'report.export', 'report.financial',
    'catalog.read'
);

-- ky_thuat_vien: xem chỉ định CLS, nhập kết quả
INSERT IGNORE INTO `diab_his_sec_role_permissions` (`role_id`, `permission_id`)
SELECT '00000000-0000-0000-0000-000000000006', id
FROM `diab_his_sec_permissions`
WHERE `code` IN (
    'patient.read',
    'cls.read', 'cls.result', 'cls.upload',
    'vitals.read', 'vitals.write',
    'catalog.read'
);

-- ============================================================
-- SEED: Mã ICD-10 thông dụng (~100 mã)
-- ============================================================
INSERT IGNORE INTO `diab_his_ref_icd10` (`code`, `name_vi`, `category`) VALUES
-- Đái tháo đường
('E10',    'Đái tháo đường type 1',                                            'Nội tiết'),
('E10.0',  'Đái tháo đường type 1 với hôn mê',                                 'Nội tiết'),
('E10.1',  'Đái tháo đường type 1 với nhiễm toan ceton',                       'Nội tiết'),
('E10.2',  'Đái tháo đường type 1 với biến chứng thận',                        'Nội tiết'),
('E10.3',  'Đái tháo đường type 1 với biến chứng mắt',                         'Nội tiết'),
('E10.4',  'Đái tháo đường type 1 với biến chứng thần kinh',                   'Nội tiết'),
('E10.5',  'Đái tháo đường type 1 với biến chứng tuần hoàn ngoại vi',          'Nội tiết'),
('E10.6',  'Đái tháo đường type 1 với các biến chứng khác đã xác định',        'Nội tiết'),
('E10.7',  'Đái tháo đường type 1 với nhiều biến chứng',                       'Nội tiết'),
('E10.9',  'Đái tháo đường type 1 không có biến chứng',                        'Nội tiết'),
('E11',    'Đái tháo đường type 2',                                            'Nội tiết'),
('E11.0',  'Đái tháo đường type 2 với hôn mê',                                 'Nội tiết'),
('E11.1',  'Đái tháo đường type 2 với nhiễm toan ceton',                       'Nội tiết'),
('E11.2',  'Đái tháo đường type 2 với biến chứng thận',                        'Nội tiết'),
('E11.3',  'Đái tháo đường type 2 với biến chứng mắt',                         'Nội tiết'),
('E11.4',  'Đái tháo đường type 2 với biến chứng thần kinh',                   'Nội tiết'),
('E11.5',  'Đái tháo đường type 2 với biến chứng tuần hoàn ngoại vi',          'Nội tiết'),
('E11.6',  'Đái tháo đường type 2 với các biến chứng khác đã xác định',        'Nội tiết'),
('E11.7',  'Đái tháo đường type 2 với nhiều biến chứng',                       'Nội tiết'),
('E11.9',  'Đái tháo đường type 2 không có biến chứng',                        'Nội tiết'),
-- Rối loạn lipid máu
('E78',    'Rối loạn chuyển hóa lipoprotein',                                  'Nội tiết'),
('E78.0',  'Tăng cholesterol máu đơn thuần',                                   'Nội tiết'),
('E78.1',  'Tăng glycerid máu đơn thuần',                                      'Nội tiết'),
('E78.2',  'Tăng lipid máu hỗn hợp',                                           'Nội tiết'),
('E78.5',  'Tăng lipid máu không xác định',                                    'Nội tiết'),
-- Tăng huyết áp
('I10',    'Tăng huyết áp nguyên phát',                                        'Tim mạch'),
('I11',    'Tăng huyết áp có bệnh tim',                                        'Tim mạch'),
('I11.0',  'Tăng huyết áp có suy tim',                                         'Tim mạch'),
('I11.9',  'Tăng huyết áp có bệnh tim không có suy tim',                       'Tim mạch'),
('I12',    'Tăng huyết áp có bệnh thận',                                       'Tim mạch'),
('I13',    'Tăng huyết áp có bệnh tim và thận',                                'Tim mạch'),
('I15',    'Tăng huyết áp thứ phát',                                           'Tim mạch'),
-- Bệnh tim mạch
('I20',    'Đau thắt ngực',                                                    'Tim mạch'),
('I20.0',  'Đau thắt ngực không ổn định',                                      'Tim mạch'),
('I21',    'Nhồi máu cơ tim cấp',                                              'Tim mạch'),
('I25',    'Bệnh tim do thiếu máu cục bộ mạn tính',                            'Tim mạch'),
('I50',    'Suy tim',                                                          'Tim mạch'),
('I63',    'Nhồi máu não',                                                     'Tim mạch'),
('I64',    'Đột quỵ không xác định là xuất huyết hay nhồi máu',                'Tim mạch'),
-- Thận
('N18',    'Bệnh thận mạn tính',                                               'Thận tiết niệu'),
('N18.1',  'Bệnh thận mạn giai đoạn 1',                                        'Thận tiết niệu'),
('N18.2',  'Bệnh thận mạn giai đoạn 2',                                        'Thận tiết niệu'),
('N18.3',  'Bệnh thận mạn giai đoạn 3',                                        'Thận tiết niệu'),
('N18.4',  'Bệnh thận mạn giai đoạn 4',                                        'Thận tiết niệu'),
('N18.5',  'Bệnh thận mạn giai đoạn 5',                                        'Thận tiết niệu'),
('N39.0',  'Nhiễm khuẩn đường tiết niệu không xác định vị trí',                'Thận tiết niệu'),
-- Tiêu hóa
('K29',    'Viêm dạ dày và tá tràng',                                          'Tiêu hóa'),
('K29.0',  'Viêm dạ dày cấp do xuất huyết',                                    'Tiêu hóa'),
('K29.5',  'Viêm dạ dày mạn tính không xác định',                              'Tiêu hóa'),
('K29.7',  'Viêm dạ dày không xác định',                                       'Tiêu hóa'),
('K21',    'Bệnh trào ngược dạ dày thực quản',                                 'Tiêu hóa'),
('K21.0',  'Trào ngược dạ dày thực quản kèm viêm thực quản',                   'Tiêu hóa'),
('K57',    'Bệnh túi thừa ruột',                                               'Tiêu hóa'),
('K74',    'Xơ hóa và xơ gan',                                                 'Tiêu hóa'),
('K80',    'Sỏi mật',                                                          'Tiêu hóa'),
('K85',    'Viêm tụy cấp',                                                     'Tiêu hóa'),
-- Hô hấp
('J06',    'Nhiễm khuẩn hô hấp trên cấp tính',                                'Hô hấp'),
('J06.0',  'Viêm amygdal cấp do virus',                                        'Hô hấp'),
('J06.9',  'Nhiễm khuẩn hô hấp trên cấp tính không xác định',                  'Hô hấp'),
('J18',    'Viêm phổi không xác định vi sinh vật',                             'Hô hấp'),
('J18.0',  'Viêm phổi thùy phổi không xác định',                               'Hô hấp'),
('J18.9',  'Viêm phổi không xác định',                                         'Hô hấp'),
('J44',    'Bệnh phổi tắc nghẽn mạn tính',                                     'Hô hấp'),
('J45',    'Hen phế quản',                                                     'Hô hấp'),
('J45.0',  'Hen phế quản dị ứng',                                              'Hô hấp'),
('J45.1',  'Hen phế quản không dị ứng',                                        'Hô hấp'),
('J45.9',  'Hen phế quản không xác định',                                      'Hô hấp'),
-- Tuyến giáp
('E01',    'Rối loạn tuyến giáp liên quan đến thiếu iod',                       'Nội tiết'),
('E05',    'Bướu giáp nhiễm độc',                                              'Nội tiết'),
('E05.0',  'Bướu giáp nhiễm độc lan tỏa',                                      'Nội tiết'),
('E06',    'Viêm tuyến giáp',                                                  'Nội tiết'),
('E03',    'Suy giáp khác',                                                    'Nội tiết'),
('E03.9',  'Suy giáp không xác định',                                          'Nội tiết'),
-- Cơ xương khớp
('M05',    'Viêm khớp dạng thấp huyết thanh dương tính',                       'Cơ xương khớp'),
('M06',    'Viêm khớp dạng thấp khác',                                         'Cơ xương khớp'),
('M10',    'Bệnh gút',                                                         'Cơ xương khớp'),
('M10.0',  'Bệnh gút vô căn',                                                  'Cơ xương khớp'),
('M47',    'Thoái hóa đốt sống',                                               'Cơ xương khớp'),
('M54',    'Đau lưng',                                                         'Cơ xương khớp'),
('M54.5',  'Đau thắt lưng',                                                    'Cơ xương khớp'),
-- Thần kinh
('G43',    'Đau nửa đầu (migraine)',                                           'Thần kinh'),
('G40',    'Động kinh',                                                        'Thần kinh'),
('G45',    'Thiếu máu não thoáng qua',                                         'Thần kinh'),
('F32',    'Giai đoạn trầm cảm',                                               'Tâm thần'),
('F41',    'Rối loạn lo âu khác',                                              'Tâm thần'),
-- Da liễu
('L20',    'Viêm da dị ứng',                                                   'Da liễu'),
('L30',    'Viêm da khác',                                                     'Da liễu'),
('L40',    'Vảy nến',                                                          'Da liễu'),
-- Mắt
('H35',    'Rối loạn võng mạc khác',                                           'Mắt'),
('H36',    'Rối loạn võng mạc do bệnh toàn thân',                              'Mắt'),
('H40',    'Glaucoma',                                                         'Mắt'),
-- Tai mũi họng
('H65',    'Viêm tai giữa không mủ',                                           'Tai mũi họng'),
('H66',    'Viêm tai giữa có mủ và không xác định',                            'Tai mũi họng'),
-- Phụ khoa
('N92',    'Kinh nguyệt nhiều, thường xuyên và không đều',                     'Sản phụ khoa'),
('N94',    'Đau và các trạng thái khác liên quan đến cơ quan sinh dục nữ',     'Sản phụ khoa'),
-- COVID-19 / Nhiễm trùng
('U07.1',  'COVID-19, virus được xác nhận',                                    'Bệnh truyền nhiễm'),
('A09',    'Nhiễm trùng đường ruột không xác định',                            'Bệnh truyền nhiễm'),
('B19',    'Viêm gan do virus không xác định',                                 'Bệnh truyền nhiễm');

-- ============================================================
-- SEED: Đơn vị thuốc
-- ============================================================
INSERT IGNORE INTO `diab_his_ref_drug_units` (`code`, `name`) VALUES
    ('VIEN',    'Viên'),
    ('NANG',    'Nang'),
    ('GOI',     'Gói'),
    ('LO',      'Lọ'),
    ('ONG',     'Ống'),
    ('TUÝP',    'Tuýp'),
    ('CHAI',    'Chai'),
    ('HOP',     'Hộp'),
    ('VI',      'Vỉ'),
    ('TAP',     'Tập'),
    ('THANH',   'Thành'),
    ('ML',      'mL'),
    ('G',       'g'),
    ('MG',      'mg'),
    ('UI',      'UI');

-- ============================================================
-- SEED: Danh mục dịch vụ (50 dịch vụ)
-- ============================================================
INSERT IGNORE INTO `diab_his_ref_services` (`code`, `name`, `category`, `unit`, `base_price`, `bhyt_price`) VALUES
-- Khám bệnh
('KB001', 'Khám bệnh thông thường',                     'EXAM',         'Lần', 100000,   39000),
('KB002', 'Khám bệnh chuyên khoa',                      'EXAM',         'Lần', 150000,   58000),
('KB003', 'Khám bệnh ngoài giờ / cấp cứu',              'EXAM',         'Lần', 200000,   NULL),
('KB004', 'Khám sức khỏe tổng quát',                    'EXAM',         'Lần', 500000,   NULL),
('KB005', 'Tư vấn dinh dưỡng',                          'EXAM',         'Lần', 100000,   NULL),
('KB006', 'Tư vấn tâm lý',                              'EXAM',         'Lần', 200000,   NULL),
('KB007', 'Khám tai - mũi - họng',                      'EXAM',         'Lần', 150000,   58000),
('KB008', 'Khám mắt',                                   'EXAM',         'Lần', 150000,   58000),
('KB009', 'Khám da liễu',                               'EXAM',         'Lần', 120000,   45000),
('KB010', 'Khám nội tiết / đái tháo đường',             'EXAM',         'Lần', 150000,   58000),
-- Xét nghiệm
('XN001', 'Đường huyết lúc đói (FPG)',                  'LAB',          'Lần', 25000,    10000),
('XN002', 'HbA1c',                                      'LAB',          'Lần', 120000,   85000),
('XN003', 'Công thức máu toàn bộ (CBC)',                 'LAB',          'Lần', 80000,    55000),
('XN004', 'Sinh hóa máu cơ bản (BMP)',                  'LAB',          'Lần', 150000,   100000),
('XN005', 'Cholesterol toàn phần',                      'LAB',          'Lần', 35000,    20000),
('XN006', 'Triglycerid',                                'LAB',          'Lần', 35000,    20000),
('XN007', 'HDL-Cholesterol',                            'LAB',          'Lần', 50000,    35000),
('XN008', 'LDL-Cholesterol',                            'LAB',          'Lần', 50000,    35000),
('XN009', 'Bộ lipid máu (TC, TG, HDL, LDL)',            'LAB',          'Lần', 160000,   120000),
('XN010', 'Creatinin máu',                              'LAB',          'Lần', 30000,    20000),
('XN011', 'Ure máu',                                    'LAB',          'Lần', 25000,    18000),
('XN012', 'eGFR (ước tính mức lọc cầu thận)',           'LAB',          'Lần', 30000,    NULL),
('XN013', 'UACR (albumin/creatinin niệu)',               'LAB',          'Lần', 80000,    55000),
('XN014', 'AST (GOT)',                                  'LAB',          'Lần', 25000,    18000),
('XN015', 'ALT (GPT)',                                  'LAB',          'Lần', 25000,    18000),
('XN016', 'TSH (Thyroid Stimulating Hormone)',          'LAB',          'Lần', 150000,   100000),
('XN017', 'FT4 (Free Thyroxine)',                       'LAB',          'Lần', 150000,   100000),
('XN018', 'HbsAg (viêm gan B)',                         'LAB',          'Lần', 80000,    NULL),
('XN019', 'Anti-HCV (viêm gan C)',                      'LAB',          'Lần', 80000,    NULL),
('XN020', 'Tổng phân tích nước tiểu (UA)',              'LAB',          'Lần', 40000,    25000),
('XN021', 'Microalbumin niệu',                          'LAB',          'Lần', 80000,    55000),
('XN022', 'Xét nghiệm COVID-19 nhanh (Ag)',             'LAB',          'Lần', 80000,    NULL),
('XN023', 'Xét nghiệm COVID-19 (RT-PCR)',               'LAB',          'Lần', 700000,   NULL),
('XN024', 'INR / PT (đông máu)',                        'LAB',          'Lần', 60000,    45000),
('XN025', 'CRP (C-Reactive Protein)',                   'LAB',          'Lần', 80000,    55000),
-- Chẩn đoán hình ảnh
('HA001', 'X-quang ngực thẳng',                         'RADIOLOGY',    'Lần', 120000,   55000),
('HA002', 'X-quang ngực nghiêng',                       'RADIOLOGY',    'Lần', 80000,    40000),
('HA003', 'X-quang bụng',                               'RADIOLOGY',    'Lần', 120000,   55000),
('HA004', 'Siêu âm bụng tổng quát',                     'RADIOLOGY',    'Lần', 200000,   100000),
('HA005', 'Siêu âm tim (2D Echo)',                      'RADIOLOGY',    'Lần', 400000,   200000),
('HA006', 'Siêu âm Doppler tim',                        'RADIOLOGY',    'Lần', 500000,   250000),
('HA007', 'Siêu âm tuyến giáp',                         'RADIOLOGY',    'Lần', 200000,   100000),
('HA008', 'Điện tim (ECG 12 chuyển đạo)',               'RADIOLOGY',    'Lần', 100000,   45000),
('HA009', 'CT Scanner ngực',                            'RADIOLOGY',    'Lần', 1500000,  800000),
('HA010', 'CT Scanner bụng',                            'RADIOLOGY',    'Lần', 1500000,  800000),
-- Thủ thuật
('TT001', 'Đo huyết áp 24 giờ (Holter HA)',             'PROCEDURE',    'Lần', 500000,   250000),
('TT002', 'Holter ECG 24 giờ',                          'PROCEDURE',    'Lần', 700000,   350000),
('TT003', 'Test dung nạp glucose (OGTT)',               'PROCEDURE',    'Lần', 200000,   150000),
('TT004', 'Đo mật độ xương (DEXA)',                     'PROCEDURE',    'Lần', 600000,   300000),
('TT005', 'Thử nghiệm gắng sức (TMT)',                  'PROCEDURE',    'Lần', 800000,   400000);
