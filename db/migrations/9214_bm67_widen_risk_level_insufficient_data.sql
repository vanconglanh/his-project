-- 9214_bm67_widen_risk_level_insufficient_data.sql
-- BM-67 [HIS-22]: Benh nhan chua co chi so (HbA1c/eGFR/huyet ap) van bi xep "Thap" thay vi
-- "Chua du du lieu". Backend da co logic dat risk_level = 'INSUFFICIENT_DATA' (17 ky tu),
-- nhung cot risk_level dang VARCHAR(10) nen khong luu duoc -> job bi tran/loi, du lieu ket lai 'LOW'.
-- Migration nay noi cot va recompute cac dong khong co chi so nao ve 'INSUFFICIENT_DATA'.
-- Idempotent: chi MODIFY khi do dai hien tai < 20; UPDATE co dieu kien.

SET @col_len := (
  SELECT CHARACTER_MAXIMUM_LENGTH
  FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'diab_his_cli_patient_risk_flag'
    AND COLUMN_NAME = 'risk_level'
);

SET @sql := IF(
  @col_len IS NOT NULL AND @col_len < 20,
  'ALTER TABLE diab_his_cli_patient_risk_flag MODIFY COLUMN risk_level VARCHAR(20) NOT NULL DEFAULT ''LOW'' COMMENT ''HIGH|MEDIUM|LOW|INSUFFICIENT_DATA''',
  'DO 0'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Recompute: khong co chi so nao -> INSUFFICIENT_DATA (dong bo voi PatientRiskStratificationJob).
UPDATE diab_his_cli_patient_risk_flag
SET risk_level = 'INSUFFICIENT_DATA'
WHERE latest_hba1c IS NULL
  AND latest_egfr IS NULL
  AND latest_bp_sys IS NULL
  AND risk_level <> 'INSUFFICIENT_DATA';
