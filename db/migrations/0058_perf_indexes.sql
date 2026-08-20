-- Migration 0058: Performance Indexes
-- Sprint 12 EPIC 10 Hardening
-- MySQL 8 — missing composite indexes for hot query paths
-- FIX (2026-08-20, devops): MySQL 8.0 KHONG ho tro CREATE INDEX IF NOT EXISTS
--   (bao gio cung throw ERROR 1064 - loi cu phap). Chuyen sang stored procedure
--   add_index_if_missing() dinh nghia trong 0000_helpers.sql de idempotent an toan.
--   Khong doi ten/cot index, khong DROP gi ca -> an toan voi DB da co du lieu.

CALL add_index_if_missing('cli_visits', 'idx_cli_visits_tenant_status_time',
    '(`tenant_id`, `status`, `started_at` DESC)');

CALL add_index_if_missing('bil_billing', 'idx_bil_billing_tenant_status_time',
    '(`tenant_id`, `status`, `created_at` DESC)');

CALL add_index_if_missing('pha_prescriptions', 'idx_pha_presc_tenant_doctor_time',
    '(`tenant_id`, `doctor_id`, `prescribed_at` DESC)');

CALL add_index_if_missing('sec_audit_logs', 'idx_audit_tenant_time',
    '(`tenant_id`, `created_at` DESC)');
