-- ============================================================================
-- 9189_inbody_report_soft_delete_cols.sql
--
-- GAP-1: Soft-delete bao cao InBody nhap nham. Bang diab_his_cli_inbody_report da
-- co san cot deleted_at (9173). Bo sung 2 cot:
--   - deleted_by   : user thuc hien xoa (audit)
--   - delete_reason: ly do xoa (nhap nham...) — luu de tra cuu sau
--
-- Idempotent: dung add_col_if_missing (0000_helpers.sql) — MySQL 8.0 khong ho tro
-- ALTER TABLE ADD COLUMN IF NOT EXISTS.
-- ============================================================================
SET NAMES utf8mb4;

CALL add_col_if_missing('diab_his_cli_inbody_report', 'deleted_by',    'CHAR(36) NULL COMMENT ''User soft-delete bao cao''');
CALL add_col_if_missing('diab_his_cli_inbody_report', 'delete_reason', 'VARCHAR(500) NULL COMMENT ''Ly do xoa (vd nhap nham)''');
