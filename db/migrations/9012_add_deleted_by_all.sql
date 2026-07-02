-- ============================================================
-- Migration: 9012_add_deleted_by_all
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-29
-- Mo ta: Them cot deleted_by CHAR(36) cho tat ca bang con thieu
-- Idempotent: YES (stored procedure kiem tra information_schema)
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS add_deleted_by;
DELIMITER $$
CREATE PROCEDURE add_deleted_by(tbl VARCHAR(128))
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = tbl
      AND column_name = 'deleted_by'
  ) THEN
    SET @sql = CONCAT('ALTER TABLE `', tbl, '` ADD COLUMN `deleted_by` CHAR(36) NULL');
    PREPARE stmt FROM @sql;
    EXECUTE stmt;
    DEALLOCATE PREPARE stmt;
  END IF;
END$$
DELIMITER ;

CALL add_deleted_by('diab_his_bhyt_export_items');
CALL add_deleted_by('diab_his_bhyt_exports');
CALL add_deleted_by('diab_his_bhyt_reconcile_items');
CALL add_deleted_by('diab_his_bhyt_reconcile_uploads');
CALL add_deleted_by('diab_his_bil_billing_items');
CALL add_deleted_by('diab_his_bil_cashier_shifts');
CALL add_deleted_by('diab_his_bil_einvoices');
CALL add_deleted_by('diab_his_bil_payments');
CALL add_deleted_by('diab_his_bil_qr_codes');
CALL add_deleted_by('diab_his_bil_service_package_items');
CALL add_deleted_by('diab_his_bil_service_packages');
CALL add_deleted_by('diab_his_bil_services');
CALL add_deleted_by('diab_his_cli_diabetes_assessments');
CALL add_deleted_by('diab_his_cli_diabetes_templates');
CALL add_deleted_by('diab_his_cli_emr_contents');
CALL add_deleted_by('diab_his_cli_emr_signatures');
CALL add_deleted_by('diab_his_cli_emr_templates');
CALL add_deleted_by('diab_his_cli_emr_versions');
CALL add_deleted_by('diab_his_cls_uploads');
CALL add_deleted_by('diab_his_int_dtqg_credentials');
CALL add_deleted_by('diab_his_int_dtqg_submissions');
CALL add_deleted_by('diab_his_int_lab_partners');
CALL add_deleted_by('diab_his_nti_notifications');
CALL add_deleted_by('diab_his_nti_preferences');
CALL add_deleted_by('diab_his_nti_vapid_keys');
CALL add_deleted_by('diab_his_nti_web_push_subs');
CALL add_deleted_by('diab_his_pat_allergies');
CALL add_deleted_by('diab_his_pat_consents');
CALL add_deleted_by('diab_his_pat_emergency_contacts');
CALL add_deleted_by('diab_his_pat_insurances');
CALL add_deleted_by('diab_his_pat_portal_accounts');
CALL add_deleted_by('diab_his_pat_portal_otp_log');
CALL add_deleted_by('diab_his_pat_portal_sessions');
CALL add_deleted_by('diab_his_pha_ddi_rules');
CALL add_deleted_by('diab_his_pha_dispense_items');
CALL add_deleted_by('diab_his_pha_dispense_records');
CALL add_deleted_by('diab_his_pha_dispenses');
CALL add_deleted_by('diab_his_pha_drug_categories');
CALL add_deleted_by('diab_his_pha_prescription_items');
CALL add_deleted_by('diab_his_pha_prescription_print_history');
CALL add_deleted_by('diab_his_pha_stock');
CALL add_deleted_by('diab_his_pha_stock_movements');
CALL add_deleted_by('diab_his_rcp_queue_tickets');
CALL add_deleted_by('diab_his_ref_drug_units');
CALL add_deleted_by('diab_his_ref_icd10');
CALL add_deleted_by('diab_his_ref_services');
CALL add_deleted_by('diab_his_sch_appointments');
CALL add_deleted_by('diab_his_sec_audit_logs');
CALL add_deleted_by('diab_his_sec_encryption_keys');
CALL add_deleted_by('diab_his_sec_permissions');
CALL add_deleted_by('diab_his_sec_role_permissions');
CALL add_deleted_by('diab_his_sec_sessions');
CALL add_deleted_by('diab_his_sec_user_roles');
CALL add_deleted_by('diab_his_sys_feature_flags');

DROP PROCEDURE IF EXISTS add_deleted_by;
