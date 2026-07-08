-- ============================================================
-- Migration: 9037_rad_results_add_workflow_columns
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Bug tien nhiem #2 (ho tro): RadResultHandlers.cs truoc day tro toi bang
--   "cli_rad_results" khong ton tai. Bang that la diab_his_rad_results
--   (FK diab_his_rad_results.order_id -> diab_his_rad_orders.id, xem
--   fk_rad_results_order). Bang that con thieu mot so cot can cho workflow
--   DRAFT/VERIFIED/AMENDED + dem so anh DICOM theo dung OpenAPI contract
--   docs/api/openapi/rad-results.yaml (status, verified_at, verified_by,
--   dicom_count) va cot "conclusion" (tach biet voi "impression").
--   Cac cot "description"/"impression"/"recommendation" GIU NGUYEN vi da
--   duoc dung dung boi ExportRadResultPdfQueryHandler + LabResultQuestPdfExporter.
-- Idempotent: YES (dung add_col_if_missing tu 0000_helpers.sql)
-- ============================================================
SET NAMES utf8mb4;

CALL add_col_if_missing('diab_his_rad_results', 'conclusion',   'TEXT NULL AFTER impression');
CALL add_col_if_missing('diab_his_rad_results', 'status',       "VARCHAR(20) NOT NULL DEFAULT 'DRAFT' AFTER recommendation");
CALL add_col_if_missing('diab_his_rad_results', 'verified_at',  'DATETIME NULL AFTER status');
CALL add_col_if_missing('diab_his_rad_results', 'verified_by',  'CHAR(36) NULL AFTER verified_at');
CALL add_col_if_missing('diab_his_rad_results', 'dicom_count',  'INT NOT NULL DEFAULT 0 AFTER verified_by');

CALL add_index_if_missing('diab_his_rad_results', 'idx_rad_results_tenant_status', '(tenant_id, status)');
CALL add_index_if_missing('diab_his_rad_results', 'idx_rad_results_tenant_order', '(tenant_id, order_id)');
