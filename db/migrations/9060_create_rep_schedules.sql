-- ============================================================
-- Migration: 9060_create_rep_schedules
-- Engine: MySQL 8.0+, InnoDB, utf8mb4 (utf8mb4_0900_ai_ci)
-- Mo ta: Report Builder P3.3 — lich gui email dinh ky 1 bao cao (code-defined
--   hoac tu tao) toi danh sach nguoi nhan, theo tan suat DAILY|WEEKLY|MONTHLY.
--   Hangfire recurring job (ReportScheduleDispatchJob) quet moi gio, gui email
--   kem file PDF/Excel qua IEmailSender khi den han.
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS).
-- ============================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS `diab_his_rep_schedules` (
    `id`              CHAR(36)     NOT NULL DEFAULT (UUID()),
    `tenant_id`       INT          NOT NULL,
    `report_code`     VARCHAR(60)  NOT NULL COMMENT 'Ma bao cao (code-defined hoac tu tao ud-xxxxxxxx)',
    `title`           VARCHAR(200) NOT NULL,
    `frequency`       VARCHAR(10)  NOT NULL COMMENT 'DAILY|WEEKLY|MONTHLY',
    `hour`            TINYINT      NOT NULL DEFAULT 7 COMMENT 'Gio chay trong ngay (0-23, gio VN)',
    `day_of_week`     TINYINT      NULL COMMENT '0=CN..6=T7 — bat buoc khi frequency=WEEKLY',
    `day_of_month`    TINYINT      NULL COMMENT '1-28 — bat buoc khi frequency=MONTHLY',
    `period`          VARCHAR(10)  NOT NULL DEFAULT 'YESTERDAY' COMMENT 'TODAY|YESTERDAY|THIS_WEEK|THIS_MONTH|LAST_MONTH',
    `format`          VARCHAR(10)  NOT NULL DEFAULT 'PDF' COMMENT 'PDF|EXCEL',
    `recipients_json` JSON         NOT NULL COMMENT 'Danh sach email nguoi nhan, vd ["a@x.vn","b@x.vn"]',
    `enabled`         TINYINT(1)   NOT NULL DEFAULT 1,
    `last_run_at`     DATETIME     NULL,
    `created_by`      CHAR(36)     NULL,
    `created_at`      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_by`      CHAR(36)     NULL,
    `updated_at`      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `deleted_at`      DATETIME     NULL,
    PRIMARY KEY (`id`),
    KEY `idx_rep_schedules_tenant_active` (`tenant_id`, `enabled`),
    KEY `idx_rep_schedules_due_scan` (`enabled`, `frequency`, `hour`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Lich gui bao cao qua email dinh ky (Report Builder P3.3) — 1 dong = 1 lich cua tenant';
