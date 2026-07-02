-- ============================================================
-- Migration: 0031_create_lab_rad_orders
-- Engine: MySQL 8.0+
-- Generated: 2026-05-23
-- Story refs: US-E08 (CLS Orders)
-- Idempotent: YES
-- ============================================================
SET NAMES utf8mb4;

-- Lab orders
CREATE TABLE IF NOT EXISTS diab_his_cli_lab_orders (
    id             CHAR(36)     NOT NULL DEFAULT (UUID()),
    tenant_id      INT          NOT NULL,
    encounter_id   CHAR(36)     NOT NULL,
    test_code      VARCHAR(50)  NOT NULL,
    test_name      VARCHAR(300) NOT NULL DEFAULT '',
    sample_type    VARCHAR(100) NULL,
    priority       VARCHAR(10)  NOT NULL DEFAULT 'NORMAL' COMMENT 'NORMAL|URGENT|STAT',
    status         VARCHAR(20)  NOT NULL DEFAULT 'ordered'
                   COMMENT 'ordered|sample_taken|processing|done|cancelled',
    ordered_at     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ordered_by     CHAR(36)     NULL,
    scheduled_for  DATETIME     NULL,
    lab_partner_id CHAR(36)     NULL,
    note           TEXT         NULL,
    created_at     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by     CHAR(36)     NULL,
    updated_at     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    updated_by     CHAR(36)     NULL,
    deleted_at     DATETIME     NULL,
    PRIMARY KEY (id),
    INDEX idx_lab_encounter (tenant_id, encounter_id),
    INDEX idx_lab_status    (tenant_id, status, ordered_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Lab (XN) orders per encounter';

-- ADD cols neu bang cu (cli_lab_orders) da co
CALL add_col_if_missing('diab_his_cli_lab_orders', 'priority',       "VARCHAR(10) NOT NULL DEFAULT 'NORMAL'");
CALL add_col_if_missing('diab_his_cli_lab_orders', 'scheduled_for',  'DATETIME NULL');
CALL add_col_if_missing('diab_his_cli_lab_orders', 'lab_partner_id', 'CHAR(36) NULL');

-- Rad orders
CREATE TABLE IF NOT EXISTS diab_his_cli_rad_orders (
    id             CHAR(36)     NOT NULL DEFAULT (UUID()),
    tenant_id      INT          NOT NULL,
    encounter_id   CHAR(36)     NOT NULL,
    modality       VARCHAR(20)  NOT NULL COMMENT 'XRAY|US|CT|MRI|MAMMO|ECG|ENDO',
    body_part      VARCHAR(100) NULL,
    contrast       TINYINT(1)   NOT NULL DEFAULT 0,
    procedure_code VARCHAR(50)  NOT NULL,
    procedure_name VARCHAR(300) NOT NULL DEFAULT '',
    priority       VARCHAR(10)  NOT NULL DEFAULT 'NORMAL',
    status         VARCHAR(20)  NOT NULL DEFAULT 'ordered'
                   COMMENT 'ordered|scheduled|in_progress|done|cancelled',
    ordered_at     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ordered_by     CHAR(36)     NULL,
    note           TEXT         NULL,
    created_at     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by     CHAR(36)     NULL,
    updated_at     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    updated_by     CHAR(36)     NULL,
    deleted_at     DATETIME     NULL,
    PRIMARY KEY (id),
    INDEX idx_rad_encounter (tenant_id, encounter_id),
    INDEX idx_rad_status    (tenant_id, status, ordered_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Radiology (CĐHA) orders per encounter';

CALL add_col_if_missing('diab_his_cli_rad_orders', 'modality',       "VARCHAR(20) NOT NULL DEFAULT 'XRAY'");
CALL add_col_if_missing('diab_his_cli_rad_orders', 'contrast',       'TINYINT(1) NOT NULL DEFAULT 0');
CALL add_col_if_missing('diab_his_cli_rad_orders', 'procedure_code', "VARCHAR(50) NOT NULL DEFAULT ''");

-- Lab test catalog
CREATE TABLE IF NOT EXISTS diab_his_dict_lab_tests (
    code          VARCHAR(50)  NOT NULL,
    name          VARCHAR(300) NOT NULL,
    sample_type   VARCHAR(100) NULL,
    default_price DECIMAL(12,2) NULL,
    bhyt_price    DECIMAL(12,2) NULL,
    is_active     TINYINT(1)   NOT NULL DEFAULT 1,
    PRIMARY KEY (code),
    FULLTEXT INDEX ft_lab_name (name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Lab test catalog';

-- Rad procedure catalog
CREATE TABLE IF NOT EXISTS diab_his_dict_rad_procedures (
    code          VARCHAR(50)  NOT NULL,
    name          VARCHAR(300) NOT NULL,
    modality      VARCHAR(20)  NULL,
    default_price DECIMAL(12,2) NULL,
    bhyt_price    DECIMAL(12,2) NULL,
    is_active     TINYINT(1)   NOT NULL DEFAULT 1,
    PRIMARY KEY (code),
    FULLTEXT INDEX ft_rad_name (name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Radiology procedure catalog';

-- Seed sample tests
INSERT IGNORE INTO diab_his_dict_lab_tests (code, name, sample_type, default_price, bhyt_price) VALUES
('HBA1C',   'HbA1c (Glycated Hemoglobin)',            'Máu tĩnh mạch', 120000, 100000),
('GLU_F',   'Đường huyết đói (Fasting Glucose)',      'Máu tĩnh mạch',  35000,  28000),
('GLU_PP',  'Đường huyết sau ăn 2h (Postprandial)',  'Máu tĩnh mạch',  35000,  28000),
('GLU_R',   'Đường huyết ngẫu nhiên',                 'Máu mao mạch',   25000,  20000),
('CREAT',   'Creatinine huyết thanh',                 'Máu tĩnh mạch',  35000,  28000),
('EGFR',    'eGFR (Estimated GFR)',                   'Máu tĩnh mạch',  50000,  40000),
('ACR',     'Albumin/Creatinine Ratio (nước tiểu)',   'Nước tiểu',      65000,  55000),
('LIPID',   'Bộ lipid (TC, TG, LDL, HDL)',           'Máu tĩnh mạch',  95000,  80000),
('CBC',     'Công thức máu toàn phần',                'Máu tĩnh mạch',  55000,  45000),
('TSH',     'TSH (Thyroid Stimulating Hormone)',       'Máu tĩnh mạch', 120000, 100000),
('UA',      'Uric Acid',                              'Máu tĩnh mạch',  35000,  28000),
('ALT',     'ALT (SGPT)',                             'Máu tĩnh mạch',  30000,  24000),
('AST',     'AST (SGOT)',                             'Máu tĩnh mạch',  30000,  24000);

INSERT IGNORE INTO diab_his_dict_rad_procedures (code, name, modality, default_price, bhyt_price) VALUES
('US_ABD',   'Siêu âm ổ bụng tổng quát',    'US',   180000, 150000),
('US_CARD',  'Siêu âm tim',                  'US',   250000, 200000),
('XRAY_CXR', 'X-quang ngực thẳng',           'XRAY',  80000,  65000),
('XRAY_LS',  'X-quang cột sống thắt lưng',  'XRAY',  90000,  72000),
('ECG12',    'Điện tâm đồ 12 chuyển đạo',   'ECG',   60000,  50000),
('CT_ABD',   'CT scanner ổ bụng',            'CT',  1200000, 900000),
('MRI_BRAIN','MRI não',                      'MRI', 2500000,1800000);

-- Notifications table (dung cho Hangfire alert job)
CREATE TABLE IF NOT EXISTS diab_his_nti_notifications (
    id            CHAR(36)     NOT NULL DEFAULT (UUID()),
    tenant_id     INT          NOT NULL,
    recipient_id  CHAR(36)     NOT NULL,
    type          VARCHAR(50)  NOT NULL,
    title         VARCHAR(200) NOT NULL,
    body          TEXT         NULL,
    ref_type      VARCHAR(50)  NULL,
    ref_id        CHAR(36)     NULL,
    is_read       TINYINT(1)   NOT NULL DEFAULT 0,
    created_at    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_nti_recipient (tenant_id, recipient_id, is_read, created_at DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='In-app notifications (including Hangfire alert jobs)';
