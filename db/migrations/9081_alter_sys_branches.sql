-- ============================================================
-- Migration: 9081_alter_sys_branches
-- Muc dich: mo rong diab_his_sys_branches (da tao o 9006) de phuc vu
--   mo hinh Tenant -> N Branch theo SRS V2. Bo tang trung gian clinics.
-- Idempotent: YES
-- ============================================================
SET NAMES utf8mb4;

-- Phong truong hop 9006 chua duoc chay (moi truong sach)
CREATE TABLE IF NOT EXISTS `diab_his_sys_branches` (
    `id`            INT             NOT NULL AUTO_INCREMENT,
    `tenant_id`     INT             NOT NULL,
    `clinic_id`     INT             NULL,
    `code`          VARCHAR(20)     NOT NULL,
    `name`          VARCHAR(255)    NOT NULL,
    `address`       TEXT            NULL,
    `phone`         VARCHAR(30)     NULL,
    `is_active`     TINYINT(1)      NOT NULL DEFAULT 1,
    `created_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`    CHAR(36)        NULL,
    `updated_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`    CHAR(36)        NULL,
    `deleted_at`    DATETIME        NULL,
    `deleted_by`    CHAR(36)        NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_branches_code_tenant` (`tenant_id`, `code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Chi nhanh / co so cua to chuc (tenant)';

-- 1. Go rang buoc clinic (deprecate tang trung gian)
CALL drop_fk_if_exists('diab_his_sys_branches', 'fk_branches_clinic');
ALTER TABLE `diab_his_sys_branches`
    MODIFY COLUMN `clinic_id` INT NULL COMMENT 'DEPRECATED - giu de tuong thich nguoc';

-- 2. Cot moi
CALL add_col_if_missing('diab_his_sys_branches', 'cskcb_code',
     'VARCHAR(20) NULL COMMENT ''Ma CSKCB Bo Y te cap rieng cho chi nhanh (lien thong DTQG/BHYT)''');
CALL add_col_if_missing('diab_his_sys_branches', 'email',
     'VARCHAR(255) NULL COMMENT ''Email lien he chi nhanh''');
CALL add_col_if_missing('diab_his_sys_branches', 'working_hours',
     'VARCHAR(255) NULL COMMENT ''Gio lam viec, vd T2-T6: 7:30-17:00''');
CALL add_col_if_missing('diab_his_sys_branches', 'timezone',
     'VARCHAR(50) NOT NULL DEFAULT ''Asia/Ho_Chi_Minh''');
CALL add_col_if_missing('diab_his_sys_branches', 'is_default',
     'TINYINT(1) NOT NULL DEFAULT 0 COMMENT ''Chi nhanh mac dinh - dung 1 per tenant''');
CALL add_col_if_missing('diab_his_sys_branches', 'sort_order',
     'INT NOT NULL DEFAULT 0');

-- 3. Index
CALL add_index_if_missing('diab_his_sys_branches', 'idx_branches_tenant_active',
     '(`tenant_id`, `is_active`, `sort_order`)');
CALL add_index_if_missing('diab_his_sys_branches', 'idx_branches_default',
     '(`tenant_id`, `is_default`)');
CALL add_index_if_missing('diab_his_sys_branches', 'idx_branches_cskcb', '(`cskcb_code`)');

-- Luu y: UNIQUE(cskcb_code) CHUA dat o migration nay. Chi bat sau khi van hanh xac nhan
-- khong co tenant nao dung trung ma CSKCB - tao migration rieng khi can.
