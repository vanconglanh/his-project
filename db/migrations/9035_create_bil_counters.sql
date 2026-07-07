-- ============================================================
-- Migration: 9035_create_bil_counters
-- Mo ta: "Quay thu" (collection counter) la mot CHIEU cua doanh thu —
--   diem thu tien: Quay dich vu / Quay nha thuoc / Quay CLS. Bao cao
--   Doanh Thu Ngay (A1) loc + gom doanh thu theo quay thu (nhu HIS SUNS:
--   "QUAY THU DICH VU"). Migration nay:
--     1) Tao bang danh muc quay thu (tenant-scoped).
--     2) Seed 3 quay mac dinh cho moi tenant hien co.
--     3) Them cot counter_id vao diab_his_bil_billing (phieu thu thuoc quay nao).
-- Idempotent: YES (IF NOT EXISTS + WHERE NOT EXISTS + check cot).
-- ============================================================
SET NAMES utf8mb4;

-- 1) Bang danh muc quay thu -----------------------------------------------
CREATE TABLE IF NOT EXISTS diab_his_bil_counters (
    id          CHAR(36)       NOT NULL DEFAULT (UUID()),
    tenant_id   INT            NOT NULL,
    code        VARCHAR(30)    NOT NULL COMMENT 'DICH_VU|NHA_THUOC|CLS|...',
    name        VARCHAR(100)   NOT NULL COMMENT 'Ten quay thu (hien thi)',
    sort_order  INT            NOT NULL DEFAULT 0,
    status      TINYINT(1)     NOT NULL DEFAULT 1 COMMENT '1=active,0=inactive',
    created_at  DATETIME(3)    NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    created_by  CHAR(36)       NULL,
    updated_at  DATETIME(3)    NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    updated_by  CHAR(36)       NULL,
    deleted_at  DATETIME(3)    NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_counter_code (tenant_id, code),
    INDEX idx_counter_tenant (tenant_id, status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 2) Seed 3 quay mac dinh cho moi tenant (idempotent qua WHERE NOT EXISTS) -
INSERT INTO diab_his_bil_counters (id, tenant_id, code, name, sort_order)
SELECT UUID(), t.id, x.code, x.name, x.sort_order
  FROM diab_his_sys_tenants t
  CROSS JOIN (
        SELECT 'DICH_VU'   AS code, 'Quầy thu dịch vụ'        AS name, 1 AS sort_order
  UNION ALL SELECT 'NHA_THUOC',      'Quầy thu nhà thuốc',            2
  UNION ALL SELECT 'CLS',            'Quầy thu CLS (XN/CĐHA)',        3
  ) x
 WHERE NOT EXISTS (
        SELECT 1 FROM diab_his_bil_counters c
         WHERE c.tenant_id = t.id AND c.code = x.code
 );

-- 3) Them cot counter_id vao billing --------------------------------------
DROP PROCEDURE IF EXISTS _add_billing_counter_9035;
DELIMITER $$
CREATE PROCEDURE _add_billing_counter_9035()
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
         WHERE TABLE_SCHEMA = DATABASE()
           AND TABLE_NAME   = 'diab_his_bil_billing'
           AND COLUMN_NAME  = 'counter_id'
    ) THEN
        ALTER TABLE diab_his_bil_billing
            ADD COLUMN counter_id CHAR(36) NULL COMMENT 'Quay thu (diab_his_bil_counters.id)' AFTER created_by;
        ALTER TABLE diab_his_bil_billing
            ADD INDEX idx_billing_counter (tenant_id, counter_id);
    END IF;
END$$
DELIMITER ;
CALL _add_billing_counter_9035();
DROP PROCEDURE IF EXISTS _add_billing_counter_9035;
