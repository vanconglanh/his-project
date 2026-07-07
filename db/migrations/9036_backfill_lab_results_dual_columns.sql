-- ============================================================
-- Migration: 9036_backfill_lab_results_dual_columns
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Bug tien nhiem #3: diab_his_lab_results co 2 bo cot song song
--   (legacy: result_value/result_unit/order_id  VS  moi: value/unit/
--    lab_order_id/patient_id/encounter_id/flag). EF entity LabResult
--    map cot MOI, non-null (Value, LabOrderId, PatientId, EncounterId)
--    -> doc du lieu seed cu (chi co cot legacy) -> InvalidCastException
--    DBNull -> String khi GET /lab-results.
-- Fix: backfill cot moi tu cot legacy/join khi cot moi con NULL.
--   value          <- result_value
--   unit           <- result_unit
--   lab_order_id   <- order_id (FK cu, luon co)
--   patient_id     <- diab_his_enc_encounters.patient_id (join qua diab_his_lab_orders.encounter_id)
--   encounter_id   <- diab_his_lab_orders.encounter_id (join qua order_id)
--   flag           <- result_flag (chi khi flag dang mac dinh 'NORMAL' va result_flag co gia tri H/L/HH/LL)
-- Idempotent: YES (moi UPDATE co dieu kien "chi ghi khi con NULL/mac dinh")
-- ============================================================
SET NAMES utf8mb4;

-- 1) value <- result_value
UPDATE diab_his_lab_results
   SET value = result_value
 WHERE value IS NULL AND result_value IS NOT NULL;

-- 2) unit <- result_unit
UPDATE diab_his_lab_results
   SET unit = result_unit
 WHERE unit IS NULL AND result_unit IS NOT NULL;

-- 3) lab_order_id <- order_id (cot FK legacy, luon duoc dien khi tao ban ghi)
UPDATE diab_his_lab_results
   SET lab_order_id = order_id
 WHERE lab_order_id IS NULL AND order_id IS NOT NULL;

-- 4) patient_id / encounter_id <- join qua diab_his_lab_orders -> diab_his_enc_encounters
UPDATE diab_his_lab_results r
  JOIN diab_his_lab_orders lo ON lo.id = r.order_id
  LEFT JOIN diab_his_enc_encounters enc ON enc.id = lo.encounter_id
   SET r.encounter_id = COALESCE(r.encounter_id, lo.encounter_id),
       r.patient_id   = COALESCE(r.patient_id, enc.patient_id)
 WHERE (r.encounter_id IS NULL OR r.patient_id IS NULL);

-- 5) flag <- result_flag (chi ghi de khi flag con o gia tri mac dinh NORMAL
--    va result_flag mang gia tri hop le H/L/HH/LL; khong dong bo CRITICAL vi
--    result_flag legacy chi co H/L trong du lieu hien tai)
UPDATE diab_his_lab_results
   SET flag = result_flag
 WHERE flag = 'NORMAL'
   AND result_flag IS NOT NULL
   AND result_flag IN ('H','L','HH','LL');

-- ============================================================
-- Verify (chay sau khi apply):
-- SELECT COUNT(*) FROM diab_his_lab_results WHERE value IS NULL OR lab_order_id IS NULL
--        OR patient_id IS NULL OR encounter_id IS NULL;
-- -> ky vong = 0 (tru khi order_id tro toi ban ghi lab_orders/encounters da bi xoa - hien khong co).
-- ============================================================
