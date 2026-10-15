-- 9220_widen_lab_result_flag.sql
-- FIX (2026-10-16): cot diab_his_lab_results.result_flag o production (pro-his-db) dang VARCHAR(5)
-- nen bi CAT CUT cac co hop le dai hon 5 ky tu khi import ("NORMAL" -> "NORMA", "CRITICAL" -> "CRITI",
-- "ABNORMAL" -> "ABNOR"). App sinh co tu LabResultFlag {NORMAL, H, L, HH, LL, CRITICAL} - gia tri dai
-- nhat "CRITICAL" = 8 ky tu. Noi cot len VARCHAR(20) de chua du moi gia tri (margin an toan).
-- Idempotent: chi MODIFY khi do dai hien tai < 20 (chay lai -> 'DO 0', khong doi gi).
-- Khong ghi de du lieu; chi doi dinh nghia cot. (Backfill gia tri bi cat cut khong lam o day vi
-- gia tri goc da mat - app dung cot `flag`/is_abnormal nen khong lo ra ngoai; neu can co the recompute
-- bang LabResultFlagCalculator sau.)

SET @col_len := (
  SELECT CHARACTER_MAXIMUM_LENGTH
  FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'diab_his_lab_results'
    AND COLUMN_NAME = 'result_flag'
);

SET @sql := IF(
  @col_len IS NOT NULL AND @col_len < 20,
  'ALTER TABLE diab_his_lab_results MODIFY COLUMN result_flag VARCHAR(20) NULL COMMENT ''Co: NORMAL|H|L|HH|LL|CRITICAL (noi tu VARCHAR(5) de het cat cut - 9220)''',
  'DO 0'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
