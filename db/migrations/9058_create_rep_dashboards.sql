-- ============================================================
-- Migration: 9058_create_rep_dashboards
-- Engine: MySQL 8.0+, InnoDB, utf8mb4 (utf8mb4_0900_ai_ci)
-- Mo ta: Bang luu Dashboard tuy bien (Report Builder P2.2) — moi tenant ghim nhieu
--   bao cao (code-defined hoac tu tao) thanh widget tren 1 luoi dashboard. Chi luu
--   widgets_json (report_code/title/widget_type/w/h/x/y) — khong luu SQL/du lieu,
--   moi lan xem se chay lai tung report_code qua GenericReportDataService.
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS).
-- ============================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS `diab_his_rep_dashboards` (
    `id`            CHAR(36)     NOT NULL DEFAULT (UUID()),
    `tenant_id`     INT          NOT NULL,
    `code`          VARCHAR(60)  NOT NULL COMMENT 'Ma dashboard tu sinh, vd db-a1b2c3d4',
    `title`         VARCHAR(200) NOT NULL,
    `widgets_json`  JSON         NOT NULL COMMENT '[{report_code,title,widget_type,w,h,x,y}]',
    `visibility`    VARCHAR(10)  NOT NULL DEFAULT 'TENANT' COMMENT 'PRIVATE|TENANT',
    `is_active`     TINYINT(1)   NOT NULL DEFAULT 1,
    `created_by`    CHAR(36)     NULL,
    `created_at`    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_by`    CHAR(36)     NULL,
    `updated_at`    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`    DATETIME     NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_rep_dashboards_tenant_code` (`tenant_id`, `code`),
    KEY `idx_rep_dashboards_tenant_active` (`tenant_id`, `is_active`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Dashboard tuy bien qua UI Report Builder (P2.2) — 1 dong = 1 dashboard cua tenant, ghim nhieu bao cao thanh widget';
