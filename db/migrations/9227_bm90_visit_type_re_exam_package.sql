-- ============================================================
-- Migration: 9227_bm90_visit_type_re_exam_package
-- Mục đích: BM-90 (HIS-44) - bổ sung "Hình thức đến khám" = "Tái khám trong gói"
--   vào danh mục master code group VISIT_TYPE (diab_his_sys_code_detail).
--   Danh mục VISIT_TYPE hiện có: FIRST_VISIT (Khám lần đầu), FOLLOW_UP (Tái khám),
--   EMERGENCY (Cấp cứu), SPECIALIST (Khám chuyên khoa). Thêm RE_EXAM_IN_PACKAGE.
-- Idempotent: YES (INSERT ... SELECT ... WHERE NOT EXISTS)
-- ============================================================
SET NAMES utf8mb4;

INSERT INTO diab_his_sys_code_detail (code_master_id, code, name, sort_order)
SELECT 'VISIT_TYPE', 'RE_EXAM_IN_PACKAGE', N'Tái khám trong gói', 5
WHERE NOT EXISTS (
    SELECT 1 FROM diab_his_sys_code_detail
    WHERE code_master_id = 'VISIT_TYPE' AND code = 'RE_EXAM_IN_PACKAGE'
);

-- Rollback:
-- DELETE FROM diab_his_sys_code_detail WHERE code_master_id='VISIT_TYPE' AND code='RE_EXAM_IN_PACKAGE';
