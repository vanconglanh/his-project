-- ============================================================
-- Migration: 9055_create_rep_definitions
-- Engine: MySQL 8.0+, InnoDB, utf8mb4 (utf8mb4_0900_ai_ci)
-- Mo ta: Bang luu bao cao tu tao (Report Builder P1) — moi tenant tu tao
--   bao cao bang/chart tren 4 Dataset whitelist (IDatasetRegistry), KHONG
--   luu SQL tho — chi luu definition_json (columns/filters/groupBy/sort/kpis)
--   de SafeQueryBuilder dung khi resolve dong qua CompositeReportRegistry.
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS).
-- ============================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS `diab_his_rep_definitions` (
    `id`              CHAR(36)     NOT NULL DEFAULT (UUID()),
    `tenant_id`       INT          NOT NULL,
    `code`            VARCHAR(60)  NOT NULL COMMENT 'Ma bao cao tu sinh, vd ud-a1b2c3d4',
    `title`           VARCHAR(200) NOT NULL,
    `dataset_key`     VARCHAR(50)  NOT NULL COMMENT 'Khoa dataset whitelist (IDatasetRegistry)',
    `definition_json` JSON         NOT NULL COMMENT '{columns[],filters[],groupBy[],sort[],kpis[]}',
    `chart_json`      JSON         NULL     COMMENT '{type,dims,measure} — null neu view_type=TABLE',
    `view_type`       VARCHAR(10)  NOT NULL DEFAULT 'TABLE' COMMENT 'TABLE|CHART',
    `orientation`     VARCHAR(10)  NOT NULL DEFAULT 'AUTO' COMMENT 'AUTO|PORTRAIT|LANDSCAPE',
    `visibility`      VARCHAR(10)  NOT NULL DEFAULT 'TENANT' COMMENT 'PRIVATE|TENANT',
    `is_active`       TINYINT(1)   NOT NULL DEFAULT 1,
    `created_by`      CHAR(36)     NULL,
    `created_at`      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_by`      CHAR(36)     NULL,
    `updated_at`      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`      DATETIME     NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_rep_definitions_tenant_code` (`tenant_id`, `code`),
    KEY `idx_rep_definitions_tenant_active` (`tenant_id`, `is_active`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Bao cao tu tao qua UI Report Builder (P1) — 1 dong = 1 dinh nghia bao cao cua tenant';
