-- ============================================================================
-- 9190_seed_lab_reference_ranges.sql
--
-- Bug A (phan bo sung du lieu): sau khi sua CreateLabResultCommandHandler join
-- diab_his_dict_lab_tests lay reference_range_low/high de tinh flag (NORMAL/H/L/
-- HH/LL/CRITICAL), phat hien danh muc XN co cot range NHUNG CHUA CO DU LIEU (13
-- dong deu NULL) -> flag van luon NORMAL luc runtime. Migration nay seed khoang
-- tham chieu nguoi lon chuan cho cac XN thuong quy Noi tiet/Tieu duong da co
-- trong danh muc, de chuc nang canh bao gia tri bat thuong hoat dong THAT.
--
-- Nguyen tac an toan: CHI cap nhat dong dang NULL (khong de len tuy bien tenant
-- neu sau nay co) -> idempotent, chay nhieu lan khong doi ket qua.
-- Range la khoang tham chieu "binh thuong" nguoi lon (khong phai nguong dieu tri).
-- ============================================================================
SET NAMES utf8mb4;

-- Duong huyet doi (mmol/L)
UPDATE diab_his_dict_lab_tests SET reference_range_low = 3.9,  reference_range_high = 5.5   WHERE code = 'GLU_F'  AND reference_range_low IS NULL AND reference_range_high IS NULL;
-- Duong huyet sau an 2h (mmol/L)
UPDATE diab_his_dict_lab_tests SET reference_range_low = 3.9,  reference_range_high = 7.8   WHERE code = 'GLU_PP' AND reference_range_low IS NULL AND reference_range_high IS NULL;
-- Duong huyet ngau nhien (mmol/L)
UPDATE diab_his_dict_lab_tests SET reference_range_low = 3.9,  reference_range_high = 7.8   WHERE code = 'GLU_R'  AND reference_range_low IS NULL AND reference_range_high IS NULL;
-- HbA1c (%)
UPDATE diab_his_dict_lab_tests SET reference_range_low = 4.0,  reference_range_high = 5.6   WHERE code = 'HBA1C'  AND reference_range_low IS NULL AND reference_range_high IS NULL;
-- TSH (mIU/L)
UPDATE diab_his_dict_lab_tests SET reference_range_low = 0.4,  reference_range_high = 4.0   WHERE code = 'TSH'    AND reference_range_low IS NULL AND reference_range_high IS NULL;
-- ALT / SGPT (U/L)
UPDATE diab_his_dict_lab_tests SET reference_range_low = 0,    reference_range_high = 41    WHERE code = 'ALT'    AND reference_range_low IS NULL AND reference_range_high IS NULL;
-- AST / SGOT (U/L)
UPDATE diab_his_dict_lab_tests SET reference_range_low = 0,    reference_range_high = 40    WHERE code = 'AST'    AND reference_range_low IS NULL AND reference_range_high IS NULL;
-- Creatinine huyet thanh (umol/L)
UPDATE diab_his_dict_lab_tests SET reference_range_low = 53,   reference_range_high = 115   WHERE code = 'CREAT'  AND reference_range_low IS NULL AND reference_range_high IS NULL;
-- eGFR (mL/min/1.73m2) - binh thuong >= 90 (chi co nguong duoi)
UPDATE diab_his_dict_lab_tests SET reference_range_low = 90,   reference_range_high = NULL  WHERE code = 'EGFR'   AND reference_range_low IS NULL AND reference_range_high IS NULL;
-- Uric Acid (umol/L)
UPDATE diab_his_dict_lab_tests SET reference_range_low = 150,  reference_range_high = 420   WHERE code = 'UA'     AND reference_range_low IS NULL AND reference_range_high IS NULL;
-- Albumin/Creatinine Ratio nuoc tieu (mg/mmol) - binh thuong < 3.0
UPDATE diab_his_dict_lab_tests SET reference_range_low = 0,    reference_range_high = 3.0   WHERE code = 'ACR'    AND reference_range_low IS NULL AND reference_range_high IS NULL;

-- LIPID va CBC la bo XN tong hop (nhieu chi so con) -> KHONG dat range don gia tri.
-- Neu sau nay tach tung chi so (TC/TG/LDL/HDL, WBC/RBC/HGB...) thi seed rieng.
