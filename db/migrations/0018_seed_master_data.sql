-- ============================================================
-- Migration: 0018_seed_master_data
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-22
-- Story refs: US-MASTER-01, US-MASTER-02
-- Idempotent: YES
-- Ghi chú: Dữ liệu master ban đầu: roles, đơn vị thuốc, ICD-10 ĐTĐ,
--   loại tài liệu CLS mẫu.
-- ============================================================
SET NAMES utf8mb4;

-- ── 1. Seed tất cả roles hệ thống ───────────────────────────
INSERT INTO `sec_roles`
    (`CODE`, `NAME`, `DESCRIPTION`, `ROLE_TYPE`, `IS_SYSTEM_ROLE`, `IS_DEFAULT_ROLE`,
     `PRIORITY_LEVEL`, `PHI_ACCESS_LEVEL`, `REQUIRES_MFA`, `APPROVAL_REQUIRED`,
     `CAN_DELEGATE`, `CAN_IMPERSONATE`, `AUDIT_ALL_ACTIONS`,
     `SESSION_CONCURRENT_LIMIT`, `MAX_SESSION_TIME`, `PASSWORD_POLICY`, `STATUS`)
VALUES
    ('ADMIN',        'Quản trị hệ thống', 'Quyền cao nhất, quản lý toàn bộ hệ thống',
     'SYSTEM', 1, 0, 100, 'FULL',    1, 0, 1, 1, 1, 1, 480, 'STRICT',   1),
    ('BACSI',        'Bác sĩ',            'Khám bệnh, kê đơn, chỉ định CLS',
     'HOSPITAL', 1, 0, 60,  'FULL',    1, 0, 0, 0, 1, 2, 480, 'STANDARD', 1),
    ('LETAN',        'Lễ tân',            'Tiếp đón, đặt lịch, thu hồ sơ',
     'HOSPITAL', 1, 0, 20,  'LIMITED', 0, 0, 0, 0, 0, 3, 480, 'STANDARD', 1),
    ('DUOCSI',       'Dược sĩ',           'Quản lý kho thuốc, cấp phát thuốc',
     'HOSPITAL', 1, 0, 40,  'PARTIAL', 0, 0, 0, 0, 1, 2, 480, 'STANDARD', 1),
    ('KETOAN',       'Kế toán',           'Thu ngân, quản lý hóa đơn, báo cáo tài chính',
     'HOSPITAL', 1, 0, 30,  'NONE',    0, 0, 0, 0, 0, 2, 480, 'STANDARD', 1),
    ('KYTHUATVIEN',  'Kỹ thuật viên',     'Thực hiện xét nghiệm, CĐHA',
     'HOSPITAL', 1, 0, 25,  'PARTIAL', 0, 0, 0, 0, 1, 2, 480, 'STANDARD', 1),
    ('DIEUDUONG',    'Điều dưỡng',        'Đo DHST, hỗ trợ bác sĩ, quản lý hồ sơ điều dưỡng',
     'HOSPITAL', 1, 0, 30,  'PARTIAL', 0, 0, 0, 1, 2, 480, 'STANDARD', 1)
ON DUPLICATE KEY UPDATE
    `NAME`        = VALUES(`NAME`),
    `DESCRIPTION` = VALUES(`DESCRIPTION`),
    `STATUS`      = VALUES(`STATUS`);

-- ── 2. Bảng đơn vị thuốc ────────────────────────────────────
CREATE TABLE IF NOT EXISTS `diab_his_dict_drug_units` (
    `id`         INT          NOT NULL AUTO_INCREMENT PRIMARY KEY COMMENT 'Khóa chính tự tăng',
    `code`       VARCHAR(20)  NOT NULL UNIQUE                      COMMENT 'Mã đơn vị (vd: VIEN, ONG)',
    `name`       VARCHAR(50)  NOT NULL                              COMMENT 'Tên đơn vị hiển thị (vd: Viên, Ống)',
    `created_at` DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP    COMMENT 'Thời điểm tạo'
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Danh mục đơn vị thuốc';

INSERT INTO `diab_his_dict_drug_units` (`code`, `name`) VALUES
    ('VIEN',   'Viên'),
    ('ONG',    'Ống'),
    ('LO',     'Lọ'),
    ('GOI',    'Gói'),
    ('CHAI',   'Chai'),
    ('TUP',    'Tuýp'),
    ('HOP',    'Hộp'),
    ('VI',     'Vỉ'),
    ('TUIP',   'Túi'),
    ('KG',     'Kg'),
    ('G',      'Gram'),
    ('ML',     'mL')
ON DUPLICATE KEY UPDATE `name` = VALUES(`name`);

-- ── 3. Bảng ICD-10 mã đái tháo đường ───────────────────────
CREATE TABLE IF NOT EXISTS `diab_his_dict_icd10` (
    `id`          INT          NOT NULL AUTO_INCREMENT PRIMARY KEY COMMENT 'Khóa chính tự tăng',
    `code`        VARCHAR(10)  NOT NULL UNIQUE                      COMMENT 'Mã ICD-10 (vd: E11, E11.9)',
    `name_vi`     VARCHAR(500) NOT NULL                              COMMENT 'Tên bệnh tiếng Việt',
    `name_en`     VARCHAR(500) NULL                                  COMMENT 'Tên bệnh tiếng Anh',
    `parent_code` VARCHAR(10)  NULL                                  COMMENT 'Mã cha (vd: E11 là cha của E11.9)',
    `category`    VARCHAR(50)  NULL                                  COMMENT 'Nhóm bệnh (Endocrine, v.v.)',
    `is_active`   TINYINT(1)   NOT NULL DEFAULT 1                   COMMENT 'Còn sử dụng trong hệ thống',
    `created_at`  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP    COMMENT 'Thời điểm tạo',

    INDEX `idx_icd10_parent` (`parent_code`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Danh mục ICD-10 (ưu tiên nhóm đái tháo đường E10-E14)';

INSERT INTO `diab_his_dict_icd10` (`code`, `name_vi`, `name_en`, `parent_code`, `category`) VALUES
    ('E10',   'Đái tháo đường typ 1',                              'Type 1 diabetes mellitus',                    NULL,  'Endocrine'),
    ('E10.0', 'ĐTĐ typ 1 với hôn mê',                             'Type 1 DM with coma',                         'E10', 'Endocrine'),
    ('E10.1', 'ĐTĐ typ 1 với nhiễm toan ceton',                   'Type 1 DM with ketoacidosis',                 'E10', 'Endocrine'),
    ('E10.2', 'ĐTĐ typ 1 với biến chứng thận',                    'Type 1 DM with renal complications',          'E10', 'Endocrine'),
    ('E10.3', 'ĐTĐ typ 1 với biến chứng mắt',                     'Type 1 DM with ophthalmic complications',    'E10', 'Endocrine'),
    ('E10.4', 'ĐTĐ typ 1 với biến chứng thần kinh',               'Type 1 DM with neurological complications',  'E10', 'Endocrine'),
    ('E10.5', 'ĐTĐ typ 1 với biến chứng tuần hoàn ngoại vi',      'Type 1 DM with peripheral circulatory comp', 'E10', 'Endocrine'),
    ('E10.6', 'ĐTĐ typ 1 với các biến chứng khác',                'Type 1 DM with other specified complications','E10', 'Endocrine'),
    ('E10.7', 'ĐTĐ typ 1 với nhiều biến chứng',                   'Type 1 DM with multiple complications',       'E10', 'Endocrine'),
    ('E10.8', 'ĐTĐ typ 1 với biến chứng không đặc hiệu',          'Type 1 DM with unspecified complications',   'E10', 'Endocrine'),
    ('E10.9', 'ĐTĐ typ 1 không có biến chứng',                    'Type 1 DM without complications',             'E10', 'Endocrine'),
    ('E11',   'Đái tháo đường typ 2',                              'Type 2 diabetes mellitus',                    NULL,  'Endocrine'),
    ('E11.0', 'ĐTĐ typ 2 với hôn mê',                             'Type 2 DM with coma',                         'E11', 'Endocrine'),
    ('E11.1', 'ĐTĐ typ 2 với nhiễm toan ceton',                   'Type 2 DM with ketoacidosis',                 'E11', 'Endocrine'),
    ('E11.2', 'ĐTĐ typ 2 với biến chứng thận',                    'Type 2 DM with renal complications',          'E11', 'Endocrine'),
    ('E11.3', 'ĐTĐ typ 2 với biến chứng mắt',                     'Type 2 DM with ophthalmic complications',    'E11', 'Endocrine'),
    ('E11.4', 'ĐTĐ typ 2 với biến chứng thần kinh',               'Type 2 DM with neurological complications',  'E11', 'Endocrine'),
    ('E11.5', 'ĐTĐ typ 2 với biến chứng tuần hoàn ngoại vi',      'Type 2 DM with peripheral circulatory comp', 'E11', 'Endocrine'),
    ('E11.6', 'ĐTĐ typ 2 với các biến chứng khác',                'Type 2 DM with other specified complications','E11', 'Endocrine'),
    ('E11.7', 'ĐTĐ typ 2 với nhiều biến chứng',                   'Type 2 DM with multiple complications',       'E11', 'Endocrine'),
    ('E11.8', 'ĐTĐ typ 2 với biến chứng không đặc hiệu',          'Type 2 DM with unspecified complications',   'E11', 'Endocrine'),
    ('E11.9', 'ĐTĐ typ 2 không có biến chứng',                    'Type 2 DM without complications',             'E11', 'Endocrine'),
    ('E12',   'Đái tháo đường liên quan đến suy dinh dưỡng',      'Malnutrition-related diabetes mellitus',      NULL,  'Endocrine'),
    ('E12.9', 'ĐTĐ liên quan suy dinh dưỡng không biến chứng',   'Malnutrition-related DM without comp',        'E12', 'Endocrine'),
    ('E13',   'Đái tháo đường đặc hiệu khác',                     'Other specified diabetes mellitus',            NULL,  'Endocrine'),
    ('E13.9', 'ĐTĐ đặc hiệu khác không biến chứng',              'Other specified DM without complications',    'E13', 'Endocrine'),
    ('E14',   'Đái tháo đường không đặc hiệu',                    'Unspecified diabetes mellitus',               NULL,  'Endocrine'),
    ('E14.9', 'ĐTĐ không đặc hiệu, không biến chứng',            'Unspecified DM without complications',        'E14', 'Endocrine')
ON DUPLICATE KEY UPDATE
    `name_vi` = VALUES(`name_vi`),
    `name_en` = VALUES(`name_en`);

-- ── 4. Bảng loại tài liệu CLS mẫu ──────────────────────────
CREATE TABLE IF NOT EXISTS `diab_his_dict_doc_types` (
    `id`         INT          NOT NULL AUTO_INCREMENT PRIMARY KEY COMMENT 'Khóa chính tự tăng',
    `code`       VARCHAR(50)  NOT NULL UNIQUE                      COMMENT 'Mã loại tài liệu',
    `name`       VARCHAR(100) NOT NULL                              COMMENT 'Tên loại tài liệu hiển thị',
    `category`   VARCHAR(50)  NULL                                  COMMENT 'Nhóm: LAB (xét nghiệm), RAD (CĐHA), OTHER',
    `is_active`  TINYINT(1)   NOT NULL DEFAULT 1                   COMMENT 'Đang sử dụng',
    `created_at` DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP    COMMENT 'Thời điểm tạo'
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Danh mục loại tài liệu CLS dùng trong diab_his_fil_cls_uploads';

INSERT INTO `diab_his_dict_doc_types` (`code`, `name`, `category`) VALUES
    ('LAB_BLOOD',   'Xét nghiệm máu',       'LAB'),
    ('LAB_URINE',   'Xét nghiệm nước tiểu', 'LAB'),
    ('LAB_OTHER',   'Xét nghiệm khác',       'LAB'),
    ('RAD_XRAY',    'X-quang',               'RAD'),
    ('RAD_ULTRASOUND', 'Siêu âm',            'RAD'),
    ('RAD_CT',      'CT scan',               'RAD'),
    ('RAD_MRI',     'MRI',                   'RAD'),
    ('RAD_ECG',     'Điện tim (ECG)',         'RAD'),
    ('RAD_ECHO',    'Siêu âm tim (Echo)',     'RAD'),
    ('OTHER',       'Tài liệu khác',         'OTHER')
ON DUPLICATE KEY UPDATE `name` = VALUES(`name`), `category` = VALUES(`category`);
