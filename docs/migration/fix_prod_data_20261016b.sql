-- =====================================================================
-- fix_prod_data_20261016b.sql  (dot 2 - bo sung sau prod-data-check)
-- Sua du lieu production pro-his-db (db prodiab_his, tenant 1).
--
-- AN TOAN: chi UPDATE/soft-delete tren bang nghiep vu HIS moi (diab_his_*), tenant_id=1.
--          KHONG xoa cung (chi set deleted_at). KHONG dung SUNS / diab_his nguon / staging.
-- IDEMPOTENT: moi lenh chi nham gia tri CHUA xu ly (deleted_at IS NULL / unit_price=0);
--             chay lai -> 0 rows affected.
-- BACKUP truoc khi chay: xem docs/migration (backup moi tao trong phien deploy).
--
-- Chay: docker exec -i -e MYSQL_PWD=<root> pro-his-db \
--        mysql -uroot --default-character-set=utf8mb4 prodiab_his < fix_prod_data_20261016b.sql
-- =====================================================================
SET @t := 1;

-- ---------------------------------------------------------------------
-- C2: SOFT-DELETE benh nhan TEST + toan bo du lieu lien quan.
--   Tap test = full_name chua 'TEST' (ten tieng Viet that KHONG bao gio chua substring "TEST").
--   KHONG dong toi: 'MINH CHI' (khong co marker TEST - khong ro la test), va 2 ca THAT trung
--   phone_bidx (HOANG THI HUE, VU DINH BAO) - chi liet ke trong bao cao, KHONG gop/xoa.
--   Thu tu: con (items) -> cha; moi UPDATE guard deleted_at IS NULL de idempotent.
-- ---------------------------------------------------------------------

-- 2.1 Dong thuoc (theo don -> benh nhan test)
UPDATE diab_his_pha_prescription_items i
  JOIN diab_his_pha_prescriptions pr ON pr.id = i.prescription_id AND pr.tenant_id = i.tenant_id
  JOIN diab_his_pat_patients p ON p.id = pr.patient_id AND p.tenant_id = pr.tenant_id
   SET i.deleted_at = NOW(), i.updated_at = NOW()
 WHERE i.tenant_id = @t AND i.deleted_at IS NULL
   AND UPPER(p.full_name) LIKE '%TEST%';

-- 2.2 Don thuoc
UPDATE diab_his_pha_prescriptions pr
  JOIN diab_his_pat_patients p ON p.id = pr.patient_id AND p.tenant_id = pr.tenant_id
   SET pr.deleted_at = NOW(), pr.updated_at = NOW()
 WHERE pr.tenant_id = @t AND pr.deleted_at IS NULL
   AND UPPER(p.full_name) LIKE '%TEST%';

-- 2.3 Ket qua xet nghiem (lab_results co patient_id truc tiep)
UPDATE diab_his_lab_results r
  JOIN diab_his_pat_patients p ON p.id = r.patient_id AND p.tenant_id = r.tenant_id
   SET r.deleted_at = NOW(), r.updated_at = NOW()
 WHERE r.tenant_id = @t AND r.deleted_at IS NULL
   AND UPPER(p.full_name) LIKE '%TEST%';

-- 2.4 Chi dinh xet nghiem (lab_orders theo encounter -> benh nhan test)
UPDATE diab_his_cli_lab_orders o
  JOIN diab_his_enc_encounters e ON e.id = o.encounter_id AND e.tenant_id = o.tenant_id
  JOIN diab_his_pat_patients p ON p.id = e.patient_id AND p.tenant_id = e.tenant_id
   SET o.deleted_at = NOW(), o.updated_at = NOW()
 WHERE o.tenant_id = @t AND o.deleted_at IS NULL
   AND UPPER(p.full_name) LIKE '%TEST%';

-- 2.5 Chan doan / EMR / sinh hieu (theo encounter -> benh nhan test)
UPDATE diab_his_enc_diagnoses d
  JOIN diab_his_enc_encounters e ON e.id = d.encounter_id AND e.tenant_id = d.tenant_id
  JOIN diab_his_pat_patients p ON p.id = e.patient_id AND p.tenant_id = e.tenant_id
   SET d.deleted_at = NOW(), d.updated_at = NOW()
 WHERE d.tenant_id = @t AND d.deleted_at IS NULL
   AND UPPER(p.full_name) LIKE '%TEST%';

UPDATE diab_his_enc_emr_contents m
  JOIN diab_his_enc_encounters e ON e.id = m.encounter_id AND e.tenant_id = m.tenant_id
  JOIN diab_his_pat_patients p ON p.id = e.patient_id AND p.tenant_id = e.tenant_id
   SET m.deleted_at = NOW(), m.updated_at = NOW()
 WHERE m.tenant_id = @t AND m.deleted_at IS NULL
   AND UPPER(p.full_name) LIKE '%TEST%';

UPDATE diab_his_enc_vital_signs v
  JOIN diab_his_enc_encounters e ON e.id = v.encounter_id AND e.tenant_id = v.tenant_id
  JOIN diab_his_pat_patients p ON p.id = e.patient_id AND p.tenant_id = e.tenant_id
   SET v.deleted_at = NOW(), v.updated_at = NOW()
 WHERE v.tenant_id = @t AND v.deleted_at IS NULL
   AND UPPER(p.full_name) LIKE '%TEST%';

-- 2.6 Luot kham
UPDATE diab_his_enc_encounters e
  JOIN diab_his_pat_patients p ON p.id = e.patient_id AND p.tenant_id = e.tenant_id
   SET e.deleted_at = NOW(), e.updated_at = NOW()
 WHERE e.tenant_id = @t AND e.deleted_at IS NULL
   AND UPPER(p.full_name) LIKE '%TEST%';

-- 2.7 Benh nhan (cuoi cung)
UPDATE diab_his_pat_patients p
   SET p.deleted_at = NOW(), p.updated_at = NOW()
 WHERE p.tenant_id = @t AND p.deleted_at IS NULL
   AND UPPER(p.full_name) LIKE '%TEST%';

-- ---------------------------------------------------------------------
-- C3: BACKFILL don gia 517 dong don thuoc legacy (unit_price=0) tu gia danh muc
--   diab_his_pha_drugs.price (chi khi price > 0). line_total = unit_price * quantity.
--   Dong co drug.price = 0 GIU nguyen 0 (khong match). Chi lam cho dong CHUA bi soft-delete.
--   Ghi chu 1 lan vao note (guard unit_price=0 nen chay lai khong lap).
-- ---------------------------------------------------------------------
UPDATE diab_his_pha_prescription_items i
  JOIN diab_his_pha_drugs d ON d.id = i.drug_id AND d.tenant_id = i.tenant_id
   SET i.unit_price = d.price,
       i.line_total = ROUND(d.price * i.quantity, 2),
       i.note = CONCAT(COALESCE(i.note, ''), ' [gia danh muc tai thoi diem migrate 2026-10-16]'),
       i.updated_at = NOW()
 WHERE i.tenant_id = @t
   AND i.deleted_at IS NULL
   AND COALESCE(i.unit_price, 0) = 0
   AND d.price > 0;
