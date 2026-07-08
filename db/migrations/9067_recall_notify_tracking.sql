-- ============================================================
-- Migration: 9067_recall_notify_tracking
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-07-08
-- Story refs: Patient Portal — Phase 1 (nhac tai kham tu dong)
-- Mo ta: Them cot theo doi da gui nhac (chong gui trung) cho recall.
--   RecallNotifyJob quet due_date T-3/T-1, notified_at NULL -> gui push/email.
-- Idempotent: YES (add_col_if_missing)
-- ============================================================
SET NAMES utf8mb4;

CALL add_col_if_missing('diab_his_cli_followup_recall', 'notified_at',    'DATETIME NULL COMMENT "Thoi diem da gui nhac tu dong (NULL = chua gui)"');
CALL add_col_if_missing('diab_his_cli_followup_recall', 'notify_channel', 'VARCHAR(12) NULL COMMENT "Kenh da gui: WEBPUSH|EMAIL"');
CALL add_col_if_missing('diab_his_cli_followup_recall', 'notify_status',  'VARCHAR(12) NULL COMMENT "SENT|FAILED"');
CALL add_index_if_missing('diab_his_cli_followup_recall', 'idx_recall_notify', '(tenant_id, notified_at, due_date)');
