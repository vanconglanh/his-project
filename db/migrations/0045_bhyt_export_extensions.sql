-- ============================================================
-- Migration: 0045_bhyt_export_extensions
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-23
-- Story refs: US-BH01..BH05
-- Idempotent: YES
-- Ghi chu: Mo rong bang diab_his_int_bhyt_exports (Sprint 9)
--   - Them cac status moi: GENERATED, VALIDATED, SIGNED, PARTIALLY_REJECTED
--   - Them cac cols: encounter_count, totals, timestamps, signed/response cols
--   - Mo rong diab_his_int_bhyt_export_items: row-level storage
-- ============================================================
SET NAMES utf8mb4;

-- 1. ALTER diab_his_int_bhyt_exports
--    Them cot scope_filter_json
ALTER TABLE `diab_his_int_bhyt_exports`
    ADD COLUMN IF NOT EXISTS `scope_filter_json`   JSON         NULL          COMMENT 'Filter scope (clinic_id, doctor_id, date_from/to...)'
        AFTER `period_month`;

--    Them cot note
ALTER TABLE `diab_his_int_bhyt_exports`
    ADD COLUMN IF NOT EXISTS `note`                VARCHAR(1000) NULL         COMMENT 'Ghi chu ky export'
        AFTER `scope_filter_json`;

--    Doi status ENUM: them GENERATED, VALIDATED, SIGNED, PARTIALLY_REJECTED
ALTER TABLE `diab_his_int_bhyt_exports`
    MODIFY COLUMN `status`
        ENUM('DRAFT','GENERATED','VALIDATED','SIGNED','SUBMITTED','APPROVED','PARTIALLY_REJECTED','REJECTED')
        NOT NULL DEFAULT 'DRAFT'
        COMMENT 'Trang thai ho so BHYT theo QD 4750';

--    Them encounter_count
ALTER TABLE `diab_his_int_bhyt_exports`
    ADD COLUMN IF NOT EXISTS `encounter_count`            INT             NULL DEFAULT 0   COMMENT 'So luot kham trong ky'
        AFTER `status`;

--    Them total_requested_amount
ALTER TABLE `diab_his_int_bhyt_exports`
    ADD COLUMN IF NOT EXISTS `total_requested_amount`     DECIMAL(18,2)   NULL DEFAULT 0   COMMENT 'Tong tien yeu cau BHYT'
        AFTER `encounter_count`;

--    Them total_approved_amount
ALTER TABLE `diab_his_int_bhyt_exports`
    ADD COLUMN IF NOT EXISTS `total_approved_amount`      DECIMAL(18,2)   NULL DEFAULT 0   COMMENT 'Tong tien duoc duyet'
        AFTER `total_requested_amount`;

--    Them total_rejected_amount
ALTER TABLE `diab_his_int_bhyt_exports`
    ADD COLUMN IF NOT EXISTS `total_rejected_amount`      DECIMAL(18,2)   NULL DEFAULT 0   COMMENT 'Tong tien bi tu choi'
        AFTER `total_approved_amount`;

--    Them generated_at
ALTER TABLE `diab_his_int_bhyt_exports`
    ADD COLUMN IF NOT EXISTS `generated_at`               DATETIME        NULL              COMMENT 'Thoi diem generate XML thanh cong'
        AFTER `total_rejected_amount`;

--    Them validated_at
ALTER TABLE `diab_his_int_bhyt_exports`
    ADD COLUMN IF NOT EXISTS `validated_at`               DATETIME        NULL              COMMENT 'Thoi diem validate XSD thanh cong'
        AFTER `generated_at`;

--    Them signed_at
ALTER TABLE `diab_his_int_bhyt_exports`
    ADD COLUMN IF NOT EXISTS `signed_at`                  DATETIME        NULL              COMMENT 'Thoi diem ky so thanh cong'
        AFTER `validated_at`;

--    Them response_at
ALTER TABLE `diab_his_int_bhyt_exports`
    ADD COLUMN IF NOT EXISTS `response_at`                DATETIME        NULL              COMMENT 'Thoi diem nhan ket qua giam dinh'
        AFTER `submitted_at`;

--    Them bhyt_reference (ma tham chieu tu cong BHYT)
ALTER TABLE `diab_his_int_bhyt_exports`
    ADD COLUMN IF NOT EXISTS `bhyt_reference`             VARCHAR(200)    NULL              COMMENT 'Ma tham chieu tu cong giam dinh BHYT'
        AFTER `response_at`;

-- 2. ALTER diab_his_int_bhyt_export_items
--    Xoa bang cu va tao lai voi schema day du (idempotent: kiem tra truoc)
--    Vi bang cu co ket cau don gian, ta ALTER tung col

--    Them record_index (vi tri dong trong bang)
ALTER TABLE `diab_his_int_bhyt_export_items`
    ADD COLUMN IF NOT EXISTS `record_index`           INT             NOT NULL DEFAULT 0   COMMENT 'Vi tri dong (index) trong bang N'
        AFTER `table_no`;

--    Doi payload_json -> row_data_json + them cac col bo sung
ALTER TABLE `diab_his_int_bhyt_export_items`
    ADD COLUMN IF NOT EXISTS `row_data_json`          JSON            NULL                 COMMENT 'Noi dung dong du lieu BHYT (1 row Bang N)'
        AFTER `record_index`;

--    Them source_encounter_id
ALTER TABLE `diab_his_int_bhyt_export_items`
    ADD COLUMN IF NOT EXISTS `source_encounter_id`    CHAR(36)        NULL                 COMMENT 'FK luot kham goc (UUID)'
        AFTER `row_data_json`;

--    Them source_billing_id
ALTER TABLE `diab_his_int_bhyt_export_items`
    ADD COLUMN IF NOT EXISTS `source_billing_id`      CHAR(36)        NULL                 COMMENT 'FK billing goc (UUID)'
        AFTER `source_encounter_id`;

--    Them ma_lien_ket (key noi bang 1-5)
ALTER TABLE `diab_his_int_bhyt_export_items`
    ADD COLUMN IF NOT EXISTS `ma_lien_ket`            VARCHAR(200)    NULL                 COMMENT 'Ma lien ket noi Bang 1 voi Bang 2/3/4/5'
        AFTER `source_billing_id`;

--    Them request_amount
ALTER TABLE `diab_his_int_bhyt_export_items`
    ADD COLUMN IF NOT EXISTS `request_amount`         DECIMAL(18,2)   NOT NULL DEFAULT 0   COMMENT 'Tien yeu cau BHYT (dong nay)'
        AFTER `ma_lien_ket`;

--    Them approved_amount
ALTER TABLE `diab_his_int_bhyt_export_items`
    ADD COLUMN IF NOT EXISTS `approved_amount`        DECIMAL(18,2)   NULL                 COMMENT 'Tien duoc duyet (sau doi soat)'
        AFTER `request_amount`;

--    Them rejection_code
ALTER TABLE `diab_his_int_bhyt_export_items`
    ADD COLUMN IF NOT EXISTS `rejection_code`         VARCHAR(50)     NULL                 COMMENT 'Ma tu choi (BHYT error code)'
        AFTER `approved_amount`;

--    Them rejection_reason
ALTER TABLE `diab_his_int_bhyt_export_items`
    ADD COLUMN IF NOT EXISTS `rejection_reason`       VARCHAR(500)    NULL                 COMMENT 'Ly do tu choi'
        AFTER `rejection_code`;

--    Them audit cols
ALTER TABLE `diab_his_int_bhyt_export_items`
    ADD COLUMN IF NOT EXISTS `created_at`             DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP   COMMENT 'Thoi diem tao'
        AFTER `rejection_reason`;

ALTER TABLE `diab_his_int_bhyt_export_items`
    ADD COLUMN IF NOT EXISTS `updated_at`             DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
                                                          ON UPDATE CURRENT_TIMESTAMP                       COMMENT 'Thoi diem cap nhat'
        AFTER `created_at`;

--    Them tenant_id cho RLS
ALTER TABLE `diab_his_int_bhyt_export_items`
    ADD COLUMN IF NOT EXISTS `tenant_id`              INT             NULL                 COMMENT 'ID tenant (RLS)'
        AFTER `id`;

-- 3. Index bo sung
CREATE INDEX IF NOT EXISTS `idx_bhyt_items_export_table`
    ON `diab_his_int_bhyt_export_items` (`export_id`, `table_no`);

CREATE INDEX IF NOT EXISTS `idx_bhyt_items_ma_lien_ket`
    ON `diab_his_int_bhyt_export_items` (`ma_lien_ket`);

CREATE INDEX IF NOT EXISTS `idx_bhyt_export_period_tenant`
    ON `diab_his_int_bhyt_exports` (`tenant_id`, `period_month`, `status`);
