-- Migration 0056: Audit Log Extensions
-- Sprint 12 EPIC 10 Hardening
-- MySQL 8 — ADD columns to sec_audit_logs if they don't exist

-- FIX: MySQL 8 khong ho tro ADD COLUMN/INDEX IF NOT EXISTS -> dung helper (bo AFTER)
CALL add_col_if_missing('sec_audit_logs', 'severity',             "ENUM('INFO','WARN','ERROR','CRITICAL') NOT NULL DEFAULT 'INFO' COMMENT 'Muc do nghiem trong cua su kien audit'");
CALL add_col_if_missing('sec_audit_logs', 'cross_tenant_attempt', "TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'Phat hien truy cap cheo tenant'");
CALL add_col_if_missing('sec_audit_logs', 'request_id',           "VARCHAR(64) NULL COMMENT 'HTTP Request ID de trace'");

CALL add_index_if_missing('sec_audit_logs', 'idx_audit_severity_tenant_time', '(`tenant_id`, `severity`, `created_at` DESC)');
CALL add_index_if_missing('sec_audit_logs', 'idx_audit_cross_tenant',         '(`cross_tenant_attempt`, `created_at` DESC)');
