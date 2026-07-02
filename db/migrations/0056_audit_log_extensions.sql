-- Migration 0056: Audit Log Extensions
-- Sprint 12 EPIC 10 Hardening
-- MySQL 8 — ADD columns to sec_audit_logs if they don't exist

-- Add severity column
ALTER TABLE `sec_audit_logs`
    ADD COLUMN IF NOT EXISTS `severity` ENUM('INFO','WARN','ERROR','CRITICAL') NOT NULL DEFAULT 'INFO'
        COMMENT 'Muc do nghiem trong cua su kien audit' AFTER `user_agent`;

-- Add cross_tenant_attempt column
ALTER TABLE `sec_audit_logs`
    ADD COLUMN IF NOT EXISTS `cross_tenant_attempt` TINYINT(1) NOT NULL DEFAULT 0
        COMMENT 'Phat hien truy cap cheo tenant' AFTER `severity`;

-- Add request_id column for correlation
ALTER TABLE `sec_audit_logs`
    ADD COLUMN IF NOT EXISTS `request_id` VARCHAR(64) NULL
        COMMENT 'HTTP Request ID de trace' AFTER `cross_tenant_attempt`;

-- Composite index for severity + time queries (admin dashboard, anomaly detection)
CREATE INDEX IF NOT EXISTS `idx_audit_severity_tenant_time`
    ON `sec_audit_logs` (`tenant_id`, `severity`, `created_at` DESC);

-- Index for cross-tenant detection queries
CREATE INDEX IF NOT EXISTS `idx_audit_cross_tenant`
    ON `sec_audit_logs` (`cross_tenant_attempt`, `created_at` DESC);
