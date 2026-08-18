-- ============================================================
-- Migration: 9095_create_encounter_addenda_g03
-- Engine: MySQL 8.0+, InnoDB, utf8mb4_0900_ai_ci
-- Story refs: G03 - Khoa benh an sau khi ket thuc kham
--             (Luat KCB 2023 D.69 / TT 32-2023 / TT 46-2018: benh an dien tu
--              da hoan tat phai bat bien, moi sua doi phai co vet)
-- Mo ta: - Bang ban dinh chinh (addendum) cho benh an da khoa. KHONG ghi de ban goc.
--        - Bo sung cot khoa locked_at / locked_by / amendment_count tren encounter.
--        - Backfill: encounter da DONE/CANCELLED coi nhu da khoa.
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS + add_col_if_missing / add_index_if_missing)
-- Phu thuoc: 0000_helpers.sql, bang diab_his_enc_encounters
-- ============================================================
SET NAMES utf8mb4;

-- ---------- 1. Bang ban dinh chinh ----------
CREATE TABLE IF NOT EXISTS `diab_his_cli_encounter_addenda` (
    `id`                  CHAR(36)     NOT NULL DEFAULT (UUID())  COMMENT 'UUID khoa chinh',
    `tenant_id`           INT          NOT NULL                   COMMENT 'ID tenant (bat buoc filter moi query)',
    `encounter_id`        CHAR(36)     NOT NULL                   COMMENT 'FK -> diab_his_enc_encounters.id',
    `section`             VARCHAR(30)  NOT NULL                   COMMENT 'DIAGNOSIS|CLINICAL_NOTE|PRESCRIPTION|VITAL_SIGN|CLS_ORDER|OTHER',
    `target_table`        VARCHAR(64)  NULL                       COMMENT 'Bang bi dinh chinh (vd diab_his_enc_diagnoses)',
    `target_id`           CHAR(36)     NULL                       COMMENT 'ID ban ghi bi dinh chinh (NULL khi operation=ADD)',
    `operation`           VARCHAR(10)  NOT NULL DEFAULT 'UPDATE'  COMMENT 'UPDATE|ADD|REMOVE',
    `content_before`      JSON         NULL                       COMMENT 'Snapshot truoc khi dinh chinh (server tu chup, khong nhan tu client)',
    `content_after`       JSON         NULL                       COMMENT 'Noi dung sau khi dinh chinh',
    `reason`              TEXT         NOT NULL                   COMMENT 'Ly do dinh chinh - BAT BUOC theo TT 32/2023',
    `bhyt_submitted_flag` TINYINT(1)   NOT NULL DEFAULT 0         COMMENT '1 = ho so BHYT da gui giam dinh tai thoi diem dinh chinh',
    `bhyt_export_id`      INT          NULL                       COMMENT 'FK -> diab_his_int_bhyt_exports.id',
    `bhyt_resubmit_at`    DATETIME(3)  NULL                       COMMENT 'Thoi diem da gui lai XML sau dinh chinh',
    `audit_log_id`        CHAR(36)     NULL                       COMMENT 'Doi chieu sang sec_audit_logs (action=AMEND)',
    `created_at`          DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    `created_by`          CHAR(36)     NULL                       COMMENT 'Nguoi dinh chinh',
    `updated_at`          DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    `updated_by`          CHAR(36)     NULL,
    `deleted_at`          DATETIME(3)  NULL                       COMMENT 'CHI dung cho rac ky thuat - KHONG dung de xoa vet dinh chinh',
    `deleted_by`          CHAR(36)     NULL,
    PRIMARY KEY (`id`),
    INDEX `idx_adden_tenant_enc`  (`tenant_id`, `encounter_id`, `created_at`),
    INDEX `idx_adden_tenant_sect` (`tenant_id`, `section`, `created_at`),
    INDEX `idx_adden_bhyt`        (`tenant_id`, `bhyt_submitted_flag`, `bhyt_resubmit_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Ban dinh chinh benh an da khoa (addendum) - bat bien, khong ghi de ban goc';

-- ---------- 2. Cot khoa tren bang encounter ----------
CALL add_col_if_missing('diab_his_enc_encounters', 'locked_at',
     'DATETIME(3) NULL COMMENT ''Thoi diem benh an bi khoa (= finished_at khi dong ca)''');
CALL add_col_if_missing('diab_his_enc_encounters', 'locked_by',
     'CHAR(36) NULL COMMENT ''Nguoi thao tac dong ca lam khoa benh an''');
CALL add_col_if_missing('diab_his_enc_encounters', 'amendment_count',
     'INT NOT NULL DEFAULT 0 COMMENT ''So lan da dinh chinh''');

CALL add_index_if_missing('diab_his_enc_encounters', 'idx_enc_locked', '(`tenant_id`, `locked_at`)');

-- ---------- 3. Backfill: benh an da DONE/CANCELLED coi nhu da khoa ----------
UPDATE `diab_his_enc_encounters`
   SET `locked_at` = COALESCE(`finished_at`, `updated_at`, `created_at`)
 WHERE `status` IN ('DONE','CANCELLED')
   AND `locked_at` IS NULL;

-- Rollback:
--   DROP TABLE IF EXISTS diab_his_cli_encounter_addenda;
--   ALTER TABLE diab_his_enc_encounters
--     DROP COLUMN locked_at, DROP COLUMN locked_by, DROP COLUMN amendment_count;
