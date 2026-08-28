-- ============================================================
-- Migration: 9093_alter_billing_for_pkg
-- Muc dich: D11 - danh dau dong hoa don duoc goi dinh muc chi tra,
--   danh dau hoa don ban goi. Dung helper add_col_if_missing /
--   add_index_if_missing (0000_helpers.sql) vi MySQL 8.0.23 khong ho tro
--   ADD COLUMN IF NOT EXISTS.
-- Idempotent: YES
-- ============================================================
SET NAMES utf8mb4;

CALL add_col_if_missing('diab_his_bil_billing_items', 'covered_by_subscription_id',
    'CHAR(36) NULL COMMENT ''FK -> diab_his_pkg_subscriptions.id - dong nay duoc goi dinh muc chi tra''');
CALL add_col_if_missing('diab_his_bil_billing_items', 'covered_by_usage_log_id',
    'CHAR(36) NULL COMMENT ''FK -> diab_his_pkg_usage_logs.id''');
CALL add_index_if_missing('diab_his_bil_billing_items', 'idx_bi_covered', '(`covered_by_subscription_id`)');

CALL add_col_if_missing('diab_his_bil_billing', 'package_subscription_id',
    'CHAR(36) NULL COMMENT ''FK -> diab_his_pkg_subscriptions.id - hoa don ban goi dinh muc''');
CALL add_index_if_missing('diab_his_bil_billing', 'idx_billing_pkg_sub', '(`package_subscription_id`)');
