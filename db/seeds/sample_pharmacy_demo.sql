-- ============================================================
-- Seed: sample_pharmacy_demo.sql
-- Mục đích: Dữ liệu DEMO module Dược — thuốc thực tế VN, lô tồn kho,
--           nhà cung cấp, purchase order, đơn thuốc cho BN000002.
-- Idempotent: INSERT ... SELECT FROM DUAL WHERE NOT EXISTS
-- Yêu cầu: Đã chạy migrations, đã có BN000002 trong pat_patients.
-- Chạy:
--   mysql -u prodiab -p prodiab_his < db/seeds/sample_pharmacy_demo.sql
-- ============================================================
SET NAMES utf8mb4 COLLATE utf8mb4_unicode_ci;

-- ── Resolve tenant + patient + encounter + doctor ─────────────
SELECT id, tenant_id
  INTO @pat_id, @tid
  FROM pat_patients
 WHERE code = 'BN000002'
   AND deleted_at IS NULL
 LIMIT 1;

SELECT id, doctor_id
  INTO @enc_id, @doc_id
  FROM cli_visits
 WHERE patient_id = @pat_id
   AND tenant_id  = @tid
   AND deleted_at IS NULL
 ORDER BY started_at DESC
 LIMIT 1;

-- ============================================================
-- 1. DRUG MASTER — 20 thuốc thực tế VN
--    Unique key: (tenant_id, code)
-- ============================================================

-- 1.1 Giảm đau / hạ sốt / NSAID (5 thuốc, requires_prescription=0)
INSERT INTO pha_drug_master
  (tenant_id, code, name_vi, name_en, generic_name, trade_name,
   atc_code, strength, unit, form, manufacturer, country,
   price, requires_prescription, is_psychotropic, is_narcotic,
   dtqg_drug_code, status)
SELECT @tid, 'PARA500',
  'Paracetamol 500mg', 'Paracetamol 500mg', 'Paracetamol', 'Paracetamol Stada',
  'N02BE01', '500mg', 'viên', 'Viên nén', 'Stada', 'Việt Nam',
  500.00, 0, 0, 0, 'DTQG-PARA500', 'ACTIVE'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_drug_master WHERE code='PARA500' AND tenant_id=@tid);

INSERT INTO pha_drug_master
  (tenant_id, code, name_vi, name_en, generic_name, trade_name,
   atc_code, strength, unit, form, manufacturer, country,
   price, requires_prescription, is_psychotropic, is_narcotic,
   dtqg_drug_code, status)
SELECT @tid, 'EFFE500',
  'Efferalgan 500mg', 'Efferalgan 500mg', 'Paracetamol', 'Efferalgan',
  'N02BE01', '500mg', 'viên sủi', 'Viên sủi bọt', 'UPSA', 'Pháp',
  2500.00, 0, 0, 0, 'DTQG-EFFE500', 'ACTIVE'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_drug_master WHERE code='EFFE500' AND tenant_id=@tid);

INSERT INTO pha_drug_master
  (tenant_id, code, name_vi, name_en, generic_name, trade_name,
   atc_code, strength, unit, form, manufacturer, country,
   price, requires_prescription, is_psychotropic, is_narcotic,
   dtqg_drug_code, status)
SELECT @tid, 'IBUP400',
  'Ibuprofen 400mg', 'Ibuprofen 400mg', 'Ibuprofen', 'Ibuprofen Mekophar',
  'M01AE01', '400mg', 'viên', 'Viên nén bao phim', 'Mekophar', 'Việt Nam',
  1200.00, 0, 0, 0, 'DTQG-IBUP400', 'ACTIVE'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_drug_master WHERE code='IBUP400' AND tenant_id=@tid);

INSERT INTO pha_drug_master
  (tenant_id, code, name_vi, name_en, generic_name, trade_name,
   atc_code, strength, unit, form, manufacturer, country,
   price, requires_prescription, is_psychotropic, is_narcotic,
   dtqg_drug_code, status)
SELECT @tid, 'DICLO50',
  'Diclofenac 50mg', 'Diclofenac 50mg', 'Diclofenac', 'Voltaren 50mg',
  'M01AB05', '50mg', 'viên', 'Viên nén bao phim', 'Domesco', 'Việt Nam',
  800.00, 0, 0, 0, 'DTQG-DICLO50', 'ACTIVE'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_drug_master WHERE code='DICLO50' AND tenant_id=@tid);

INSERT INTO pha_drug_master
  (tenant_id, code, name_vi, name_en, generic_name, trade_name,
   atc_code, strength, unit, form, manufacturer, country,
   price, requires_prescription, is_psychotropic, is_narcotic,
   dtqg_drug_code, status)
SELECT @tid, 'ASPI81',
  'Aspirin 81mg', 'Aspirin 81mg', 'Acid acetylsalicylic', 'Aspirin Cardio 81mg',
  'B01AC06', '81mg', 'viên', 'Viên nén bao tan trong ruột', 'Bayer', 'Đức',
  600.00, 0, 0, 0, 'DTQG-ASPI81', 'ACTIVE'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_drug_master WHERE code='ASPI81' AND tenant_id=@tid);

-- 1.2 Kháng sinh (requires_prescription=1)
INSERT INTO pha_drug_master
  (tenant_id, code, name_vi, name_en, generic_name, trade_name,
   atc_code, strength, unit, form, manufacturer, country,
   price, requires_prescription, is_psychotropic, is_narcotic,
   dtqg_drug_code, status)
SELECT @tid, 'AMOX500',
  'Amoxicillin 500mg', 'Amoxicillin 500mg', 'Amoxicillin', 'Amoxicillin Imexpharm',
  'J01CA04', '500mg', 'viên', 'Viên nang cứng', 'Imexpharm', 'Việt Nam',
  1500.00, 1, 0, 0, 'DTQG-AMOX500', 'ACTIVE'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_drug_master WHERE code='AMOX500' AND tenant_id=@tid);

INSERT INTO pha_drug_master
  (tenant_id, code, name_vi, name_en, generic_name, trade_name,
   atc_code, strength, unit, form, manufacturer, country,
   price, requires_prescription, is_psychotropic, is_narcotic,
   dtqg_drug_code, status)
SELECT @tid, 'AUGM625',
  'Augmentin 625mg', 'Augmentin 625mg', 'Amoxicillin + Clavulanic acid', 'Augmentin 625',
  'J01CR02', '500mg/125mg', 'viên', 'Viên nén bao phim', 'GSK', 'Bỉ',
  12000.00, 1, 0, 0, 'DTQG-AUGM625', 'ACTIVE'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_drug_master WHERE code='AUGM625' AND tenant_id=@tid);

INSERT INTO pha_drug_master
  (tenant_id, code, name_vi, name_en, generic_name, trade_name,
   atc_code, strength, unit, form, manufacturer, country,
   price, requires_prescription, is_psychotropic, is_narcotic,
   dtqg_drug_code, status)
SELECT @tid, 'CEFU500',
  'Cefuroxime 500mg', 'Cefuroxime 500mg', 'Cefuroxime', 'Zinnat 500mg',
  'J01DC02', '500mg', 'viên', 'Viên nén bao phim', 'Pymepharco', 'Việt Nam',
  8000.00, 1, 0, 0, 'DTQG-CEFU500', 'ACTIVE'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_drug_master WHERE code='CEFU500' AND tenant_id=@tid);

INSERT INTO pha_drug_master
  (tenant_id, code, name_vi, name_en, generic_name, trade_name,
   atc_code, strength, unit, form, manufacturer, country,
   price, requires_prescription, is_psychotropic, is_narcotic,
   dtqg_drug_code, status)
SELECT @tid, 'AZIT500',
  'Azithromycin 500mg', 'Azithromycin 500mg', 'Azithromycin', 'Zithromax 500mg',
  'J01FA10', '500mg', 'viên', 'Viên nén bao phim', 'DHG Pharma', 'Việt Nam',
  9500.00, 1, 0, 0, 'DTQG-AZIT500', 'ACTIVE'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_drug_master WHERE code='AZIT500' AND tenant_id=@tid);

-- 1.3 Tiểu đường
INSERT INTO pha_drug_master
  (tenant_id, code, name_vi, name_en, generic_name, trade_name,
   atc_code, strength, unit, form, manufacturer, country,
   price, requires_prescription, is_psychotropic, is_narcotic,
   dtqg_drug_code, status)
SELECT @tid, 'METF500',
  'Metformin 500mg', 'Metformin 500mg', 'Metformin hydrochloride', 'Glucophage 500',
  'A10BA02', '500mg', 'viên', 'Viên nén bao phim', 'Stada', 'Việt Nam',
  700.00, 1, 0, 0, 'DTQG-METF500', 'ACTIVE'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_drug_master WHERE code='METF500' AND tenant_id=@tid);

INSERT INTO pha_drug_master
  (tenant_id, code, name_vi, name_en, generic_name, trade_name,
   atc_code, strength, unit, form, manufacturer, country,
   price, requires_prescription, is_psychotropic, is_narcotic,
   dtqg_drug_code, status)
SELECT @tid, 'METF1000XR',
  'Metformin XR 1000mg', 'Metformin XR 1000mg', 'Metformin hydrochloride', 'Glucophage XR 1000',
  'A10BA02', '1000mg', 'viên', 'Viên nén giải phóng kéo dài', 'Merck', 'Đức',
  3500.00, 1, 0, 0, 'DTQG-METF1000XR', 'ACTIVE'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_drug_master WHERE code='METF1000XR' AND tenant_id=@tid);

INSERT INTO pha_drug_master
  (tenant_id, code, name_vi, name_en, generic_name, trade_name,
   atc_code, strength, unit, form, manufacturer, country,
   price, requires_prescription, is_psychotropic, is_narcotic,
   dtqg_drug_code, status)
SELECT @tid, 'JANUMET5050',
  'Janumet 50/500mg', 'Janumet 50/500mg', 'Sitagliptin + Metformin', 'Janumet 50/500',
  'A10BD07', '50mg/500mg', 'viên', 'Viên nén bao phim', 'MSD', 'Mỹ',
  18000.00, 1, 0, 0, 'DTQG-JANUMET5050', 'ACTIVE'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_drug_master WHERE code='JANUMET5050' AND tenant_id=@tid);

INSERT INTO pha_drug_master
  (tenant_id, code, name_vi, name_en, generic_name, trade_name,
   atc_code, strength, unit, form, manufacturer, country,
   price, requires_prescription, is_psychotropic, is_narcotic,
   dtqg_drug_code, status)
SELECT @tid, 'INSMIX30',
  'Insulin Mixtard 30', 'Insulin Mixtard 30', 'Insulin (human, isophane)', 'Mixtard 30',
  'A10AD01', '100 UI/mL', 'lọ 10mL', 'Dung dịch tiêm', 'Novo Nordisk', 'Đan Mạch',
  120000.00, 1, 0, 0, 'DTQG-INSMIX30', 'ACTIVE'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_drug_master WHERE code='INSMIX30' AND tenant_id=@tid);

-- 1.4 Tim mạch / lipid
INSERT INTO pha_drug_master
  (tenant_id, code, name_vi, name_en, generic_name, trade_name,
   atc_code, strength, unit, form, manufacturer, country,
   price, requires_prescription, is_psychotropic, is_narcotic,
   dtqg_drug_code, status)
SELECT @tid, 'ATOR10',
  'Atorvastatin 10mg', 'Atorvastatin 10mg', 'Atorvastatin', 'Lipitor 10mg',
  'C10AA05', '10mg', 'viên', 'Viên nén bao phim', 'Pfizer', 'Mỹ',
  2500.00, 1, 0, 0, 'DTQG-ATOR10', 'ACTIVE'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_drug_master WHERE code='ATOR10' AND tenant_id=@tid);

INSERT INTO pha_drug_master
  (tenant_id, code, name_vi, name_en, generic_name, trade_name,
   atc_code, strength, unit, form, manufacturer, country,
   price, requires_prescription, is_psychotropic, is_narcotic,
   dtqg_drug_code, status)
SELECT @tid, 'ATOR20',
  'Atorvastatin 20mg', 'Atorvastatin 20mg', 'Atorvastatin', 'Lipitor 20mg',
  'C10AA05', '20mg', 'viên', 'Viên nén bao phim', 'Pfizer', 'Mỹ',
  4000.00, 1, 0, 0, 'DTQG-ATOR20', 'ACTIVE'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_drug_master WHERE code='ATOR20' AND tenant_id=@tid);

INSERT INTO pha_drug_master
  (tenant_id, code, name_vi, name_en, generic_name, trade_name,
   atc_code, strength, unit, form, manufacturer, country,
   price, requires_prescription, is_psychotropic, is_narcotic,
   dtqg_drug_code, status)
SELECT @tid, 'AMLO5',
  'Amlodipine 5mg', 'Amlodipine 5mg', 'Amlodipine', 'Norvasc 5mg',
  'C08CA01', '5mg', 'viên', 'Viên nén', 'Pfizer', 'Mỹ',
  1500.00, 1, 0, 0, 'DTQG-AMLO5', 'ACTIVE'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_drug_master WHERE code='AMLO5' AND tenant_id=@tid);

INSERT INTO pha_drug_master
  (tenant_id, code, name_vi, name_en, generic_name, trade_name,
   atc_code, strength, unit, form, manufacturer, country,
   price, requires_prescription, is_psychotropic, is_narcotic,
   dtqg_drug_code, status)
SELECT @tid, 'LOSA50',
  'Losartan 50mg', 'Losartan 50mg', 'Losartan kali', 'Cozaar 50mg',
  'C09CA01', '50mg', 'viên', 'Viên nén bao phim', 'MSD', 'Mỹ',
  5000.00, 1, 0, 0, 'DTQG-LOSA50', 'ACTIVE'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_drug_master WHERE code='LOSA50' AND tenant_id=@tid);

-- 1.5 Tiêu hóa
INSERT INTO pha_drug_master
  (tenant_id, code, name_vi, name_en, generic_name, trade_name,
   atc_code, strength, unit, form, manufacturer, country,
   price, requires_prescription, is_psychotropic, is_narcotic,
   dtqg_drug_code, status)
SELECT @tid, 'OMEP20',
  'Omeprazole 20mg', 'Omeprazole 20mg', 'Omeprazole', 'Losec 20mg',
  'A02BC01', '20mg', 'viên', 'Viên nang cứng', 'AstraZeneca', 'Thụy Điển',
  1800.00, 1, 0, 0, 'DTQG-OMEP20', 'ACTIVE'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_drug_master WHERE code='OMEP20' AND tenant_id=@tid);

INSERT INTO pha_drug_master
  (tenant_id, code, name_vi, name_en, generic_name, trade_name,
   atc_code, strength, unit, form, manufacturer, country,
   price, requires_prescription, is_psychotropic, is_narcotic,
   dtqg_drug_code, status)
SELECT @tid, 'ESOP40',
  'Esomeprazole 40mg', 'Esomeprazole 40mg', 'Esomeprazole', 'Nexium 40mg',
  'A02BC05', '40mg', 'viên', 'Viên nén bao tan trong ruột', 'AstraZeneca', 'Thụy Điển',
  8500.00, 1, 0, 0, 'DTQG-ESOP40', 'ACTIVE'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_drug_master WHERE code='ESOP40' AND tenant_id=@tid);

-- 1.6 Thuốc hướng thần (is_psychotropic=1)
INSERT INTO pha_drug_master
  (tenant_id, code, name_vi, name_en, generic_name, trade_name,
   atc_code, strength, unit, form, manufacturer, country,
   price, requires_prescription, is_psychotropic, is_narcotic,
   dtqg_drug_code, status)
SELECT @tid, 'DIAZ5',
  'Diazepam 5mg', 'Diazepam 5mg', 'Diazepam', 'Seduxen 5mg',
  'N05BA01', '5mg', 'viên', 'Viên nén', 'Dược phẩm TW1', 'Việt Nam',
  500.00, 1, 1, 0, 'DTQG-DIAZ5', 'ACTIVE'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_drug_master WHERE code='DIAZ5' AND tenant_id=@tid);

-- ============================================================
-- 2. TỒN KHO — 6 lô tồn kho (warehouse_id=1)
--    Idempotent theo (tenant_id, warehouse_id, drug_id, batch_no)
-- ============================================================

-- Lô EXP7: Paracetamol sắp hết hạn 7 ngày
INSERT INTO pha_stocks
  (tenant_id, warehouse_id, drug_id, batch_no, manufacture_date, expiry_date,
   quantity_available, quantity_reserved, unit_cost, reorder_level, status)
SELECT @tid, 1,
  (SELECT id FROM pha_drug_master WHERE code='PARA500' AND tenant_id=@tid LIMIT 1),
  'PAR-EXP7',
  DATE_SUB(CURDATE(), INTERVAL 2 YEAR),
  DATE_ADD(CURDATE(), INTERVAL 7 DAY),
  50.00, 0.00, 450.00, 20.00, 'ACTIVE'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_stocks
   WHERE batch_no='PAR-EXP7' AND tenant_id=@tid AND warehouse_id=1);

-- Lô EXP30: Amoxicillin hết hạn 30 ngày
INSERT INTO pha_stocks
  (tenant_id, warehouse_id, drug_id, batch_no, manufacture_date, expiry_date,
   quantity_available, quantity_reserved, unit_cost, reorder_level, status)
SELECT @tid, 1,
  (SELECT id FROM pha_drug_master WHERE code='AMOX500' AND tenant_id=@tid LIMIT 1),
  'AMX-EXP30',
  DATE_SUB(CURDATE(), INTERVAL 1 YEAR),
  DATE_ADD(CURDATE(), INTERVAL 30 DAY),
  100.00, 0.00, 1300.00, 30.00, 'ACTIVE'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_stocks
   WHERE batch_no='AMX-EXP30' AND tenant_id=@tid AND warehouse_id=1);

-- Lô EXP60: Metformin hết hạn 60 ngày
INSERT INTO pha_stocks
  (tenant_id, warehouse_id, drug_id, batch_no, manufacture_date, expiry_date,
   quantity_available, quantity_reserved, unit_cost, reorder_level, status)
SELECT @tid, 1,
  (SELECT id FROM pha_drug_master WHERE code='METF500' AND tenant_id=@tid LIMIT 1),
  'MET-EXP60',
  DATE_SUB(CURDATE(), INTERVAL 1 YEAR),
  DATE_ADD(CURDATE(), INTERVAL 60 DAY),
  200.00, 0.00, 600.00, 50.00, 'ACTIVE'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_stocks
   WHERE batch_no='MET-EXP60' AND tenant_id=@tid AND warehouse_id=1);

-- Lô EXP90: Atorvastatin hết hạn 90 ngày
INSERT INTO pha_stocks
  (tenant_id, warehouse_id, drug_id, batch_no, manufacture_date, expiry_date,
   quantity_available, quantity_reserved, unit_cost, reorder_level, status)
SELECT @tid, 1,
  (SELECT id FROM pha_drug_master WHERE code='ATOR10' AND tenant_id=@tid LIMIT 1),
  'ATV-EXP90',
  DATE_SUB(CURDATE(), INTERVAL 1 YEAR),
  DATE_ADD(CURDATE(), INTERVAL 90 DAY),
  150.00, 0.00, 2200.00, 30.00, 'ACTIVE'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_stocks
   WHERE batch_no='ATV-EXP90' AND tenant_id=@tid AND warehouse_id=1);

-- Lô LOW: Omeprazole tồn thấp dưới mức cảnh báo
INSERT INTO pha_stocks
  (tenant_id, warehouse_id, drug_id, batch_no, manufacture_date, expiry_date,
   quantity_available, quantity_reserved, unit_cost, reorder_level, status)
SELECT @tid, 1,
  (SELECT id FROM pha_drug_master WHERE code='OMEP20' AND tenant_id=@tid LIMIT 1),
  'OME-LOW',
  DATE_SUB(CURDATE(), INTERVAL 6 MONTH),
  DATE_ADD(CURDATE(), INTERVAL 18 MONTH),
  5.00, 0.00, 1600.00, 20.00, 'ACTIVE'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_stocks
   WHERE batch_no='OME-LOW' AND tenant_id=@tid AND warehouse_id=1);

-- Lô NORM: Amlodipine tồn bình thường đến 2027
INSERT INTO pha_stocks
  (tenant_id, warehouse_id, drug_id, batch_no, manufacture_date, expiry_date,
   quantity_available, quantity_reserved, unit_cost, reorder_level, status)
SELECT @tid, 1,
  (SELECT id FROM pha_drug_master WHERE code='AMLO5' AND tenant_id=@tid LIMIT 1),
  'AML-NORM-2027',
  DATE_SUB(CURDATE(), INTERVAL 3 MONTH),
  '2027-12-31',
  300.00, 0.00, 1300.00, 50.00, 'ACTIVE'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_stocks
   WHERE batch_no='AML-NORM-2027' AND tenant_id=@tid AND warehouse_id=1);

-- ============================================================
-- 3. NHÀ CUNG CẤP — 3 công ty dược
-- ============================================================

INSERT INTO diab_his_pha_suppliers
  (tenant_id, code, name, tax_code, address, phone, email, contact_name, is_active)
SELECT @tid, 'VIMEDIMEX',
  'Công ty CP Đầu tư và Thương mại Vimedimex',
  '0100100502',
  '66 Hoàng Minh Giám, Nhân Chính, Thanh Xuân, Hà Nội',
  '024 3835 2939',
  'kinhdoanh@vimedimex.com.vn',
  'Nguyễn Văn Hùng',
  1
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM diab_his_pha_suppliers WHERE code='VIMEDIMEX' AND tenant_id=@tid);

INSERT INTO diab_his_pha_suppliers
  (tenant_id, code, name, tax_code, address, phone, email, contact_name, is_active)
SELECT @tid, 'DHG',
  'Công ty CP Dược Hậu Giang',
  '1800156801',
  '288 Bis Nguyễn Văn Cừ, An Hòa, Ninh Kiều, Cần Thơ',
  '0292 389 1433',
  'info@dhgpharma.com.vn',
  'Trần Thị Lan',
  1
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM diab_his_pha_suppliers WHERE code='DHG' AND tenant_id=@tid);

INSERT INTO diab_his_pha_suppliers
  (tenant_id, code, name, tax_code, address, phone, email, contact_name, is_active)
SELECT @tid, 'PYMEPHARCO',
  'Công ty CP Pymepharco',
  '0200386616',
  '166-170 Nguyễn Huệ, TP Tuy Hòa, Phú Yên',
  '0257 384 6325',
  'info@pymepharco.com',
  'Lê Minh Tuấn',
  1
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM diab_his_pha_suppliers WHERE code='PYMEPHARCO' AND tenant_id=@tid);

-- ============================================================
-- 4. PURCHASE ORDERS — 2 đơn đặt hàng
--    TODO: diab_his_pha_purchase_order_items có drug_id INT nhưng
--          pha_drug_master.id là CHAR(36) — cần migration sửa FK type
--          trước khi seed items. KHÔNG seed items ở đây.
-- ============================================================

INSERT INTO diab_his_pha_purchase_orders
  (tenant_id, supplier_id, warehouse_id, order_no, status,
   ordered_at, expected_delivery, total_amount, note)
SELECT @tid,
  (SELECT id FROM diab_his_pha_suppliers WHERE code='VIMEDIMEX' AND tenant_id=@tid LIMIT 1),
  1, 'PO-2026-001', 'RECEIVED',
  DATE_SUB(NOW(), INTERVAL 30 DAY),
  DATE_SUB(CURDATE(), INTERVAL 25 DAY),
  5000000.00,
  'Nhập hàng quý 2/2026 — đã nhận đủ'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM diab_his_pha_purchase_orders WHERE order_no='PO-2026-001' AND tenant_id=@tid);

INSERT INTO diab_his_pha_purchase_orders
  (tenant_id, supplier_id, warehouse_id, order_no, status,
   ordered_at, expected_delivery, total_amount, note)
SELECT @tid,
  (SELECT id FROM diab_his_pha_suppliers WHERE code='DHG' AND tenant_id=@tid LIMIT 1),
  1, 'PO-2026-002', 'SENT',
  DATE_SUB(NOW(), INTERVAL 5 DAY),
  DATE_ADD(CURDATE(), INTERVAL 7 DAY),
  3200000.00,
  'Đơn đặt hàng kháng sinh tháng 5/2026 — chờ giao'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM diab_his_pha_purchase_orders WHERE order_no='PO-2026-002' AND tenant_id=@tid);

-- ============================================================
-- 5. ĐƠN THUỐC cho BN000002
-- ============================================================

-- Đơn 1: DISPENSED — 30 ngày trước (đã cấp phát, đã gửi ĐTQG)
INSERT INTO pha_prescriptions
  (tenant_id, encounter_id, patient_id, doctor_id, status,
   prescribed_at, signed_at, signed_by,
   dtqg_code, dtqg_status, total_amount,
   note)
SELECT @tid, @enc_id, @pat_id, @doc_id, 'DISPENSED',
  DATE_SUB(NOW(), INTERVAL 30 DAY),
  DATE_SUB(NOW(), INTERVAL 30 DAY),
  @doc_id,
  'DTQG-DEMO-001', 'ACCEPTED', 85000.00,
  'Đơn thuốc điều trị đái tháo đường type 2 và rối loạn lipid máu'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_prescriptions WHERE dtqg_code='DTQG-DEMO-001' AND tenant_id=@tid);

-- Lưu id đơn 1
SELECT id INTO @rx1_id FROM pha_prescriptions
 WHERE dtqg_code='DTQG-DEMO-001' AND tenant_id=@tid LIMIT 1;

-- Items đơn 1: Metformin 500mg x60v + Atorvastatin 10mg x30v + Amlodipine 5mg x30v
INSERT INTO pha_prescription_items
  (tenant_id, prescription_id, drug_id, dosage, frequency, route,
   duration_days, quantity, instructions, batch_dispensed)
SELECT @tid, @rx1_id,
  (SELECT id FROM pha_drug_master WHERE code='METF500' AND tenant_id=@tid LIMIT 1),
  '1 viên', '2 lần/ngày', 'Uống',
  30, 60.00,
  'Uống trong hoặc ngay sau bữa ăn sáng và tối để giảm tác dụng phụ tiêu hóa',
  'MET-EXP60'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_prescription_items
   WHERE prescription_id=@rx1_id AND tenant_id=@tid
     AND drug_id=(SELECT id FROM pha_drug_master WHERE code='METF500' AND tenant_id=@tid LIMIT 1));

INSERT INTO pha_prescription_items
  (tenant_id, prescription_id, drug_id, dosage, frequency, route,
   duration_days, quantity, instructions, batch_dispensed)
SELECT @tid, @rx1_id,
  (SELECT id FROM pha_drug_master WHERE code='ATOR10' AND tenant_id=@tid LIMIT 1),
  '1 viên', '1 lần/ngày', 'Uống',
  30, 30.00,
  'Uống vào buổi tối trước khi ngủ để tăng hiệu quả hạ lipid',
  'ATV-EXP90'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_prescription_items
   WHERE prescription_id=@rx1_id AND tenant_id=@tid
     AND drug_id=(SELECT id FROM pha_drug_master WHERE code='ATOR10' AND tenant_id=@tid LIMIT 1));

INSERT INTO pha_prescription_items
  (tenant_id, prescription_id, drug_id, dosage, frequency, route,
   duration_days, quantity, instructions, batch_dispensed)
SELECT @tid, @rx1_id,
  (SELECT id FROM pha_drug_master WHERE code='AMLO5' AND tenant_id=@tid LIMIT 1),
  '1 viên', '1 lần/ngày', 'Uống',
  30, 30.00,
  'Uống vào buổi sáng, có thể uống cùng hoặc không cùng bữa ăn',
  'AML-NORM-2027'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_prescription_items
   WHERE prescription_id=@rx1_id AND tenant_id=@tid
     AND drug_id=(SELECT id FROM pha_drug_master WHERE code='AMLO5' AND tenant_id=@tid LIMIT 1));

-- Đơn 2: DRAFT — 2 giờ trước (chưa ký, chưa cấp phát)
INSERT INTO pha_prescriptions
  (tenant_id, encounter_id, patient_id, doctor_id, status,
   prescribed_at, dtqg_status, total_amount,
   note)
SELECT @tid, @enc_id, @pat_id, @doc_id, 'DRAFT',
  DATE_SUB(NOW(), INTERVAL 2 HOUR),
  'NONE', 0.00,
  'Đơn kê thêm — điều trị triệu chứng tiêu hóa và giảm đau'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_prescriptions
   WHERE patient_id=@pat_id AND tenant_id=@tid AND status='DRAFT'
     AND prescribed_at >= DATE_SUB(NOW(), INTERVAL 3 HOUR));

-- Lưu id đơn 2
SELECT id INTO @rx2_id FROM pha_prescriptions
 WHERE patient_id=@pat_id AND tenant_id=@tid AND status='DRAFT'
   AND prescribed_at >= DATE_SUB(NOW(), INTERVAL 3 HOUR)
 LIMIT 1;

-- Items đơn 2: Paracetamol 500mg x10v + Omeprazole 20mg x14v
INSERT INTO pha_prescription_items
  (tenant_id, prescription_id, drug_id, dosage, frequency, route,
   duration_days, quantity, instructions)
SELECT @tid, @rx2_id,
  (SELECT id FROM pha_drug_master WHERE code='PARA500' AND tenant_id=@tid LIMIT 1),
  '1-2 viên', '3-4 lần/ngày khi đau', 'Uống',
  5, 10.00,
  'Uống khi sốt hoặc đau, cách nhau ít nhất 4-6 giờ. Không quá 8 viên/ngày'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_prescription_items
   WHERE prescription_id=@rx2_id AND tenant_id=@tid
     AND drug_id=(SELECT id FROM pha_drug_master WHERE code='PARA500' AND tenant_id=@tid LIMIT 1));

INSERT INTO pha_prescription_items
  (tenant_id, prescription_id, drug_id, dosage, frequency, route,
   duration_days, quantity, instructions)
SELECT @tid, @rx2_id,
  (SELECT id FROM pha_drug_master WHERE code='OMEP20' AND tenant_id=@tid LIMIT 1),
  '1 viên', '1 lần/ngày', 'Uống',
  14, 14.00,
  'Uống trước bữa ăn sáng 30 phút để đạt hiệu quả tốt nhất'
FROM DUAL WHERE NOT EXISTS (
  SELECT 1 FROM pha_prescription_items
   WHERE prescription_id=@rx2_id AND tenant_id=@tid
     AND drug_id=(SELECT id FROM pha_drug_master WHERE code='OMEP20' AND tenant_id=@tid LIMIT 1));

-- ============================================================
-- BÁO CÁO số bản ghi sau seed
-- ============================================================
SELECT
  (SELECT COUNT(*) FROM pha_drug_master       WHERE tenant_id=@tid AND deleted_at IS NULL) AS thuoc_drug_master,
  (SELECT COUNT(*) FROM pha_stocks            WHERE tenant_id=@tid AND deleted_at IS NULL) AS lo_pha_stocks,
  (SELECT COUNT(*) FROM diab_his_pha_suppliers WHERE tenant_id=@tid AND deleted_at IS NULL) AS nha_cung_cap,
  (SELECT COUNT(*) FROM diab_his_pha_purchase_orders WHERE tenant_id=@tid AND deleted_at IS NULL) AS purchase_orders,
  (SELECT COUNT(*) FROM pha_prescriptions     WHERE tenant_id=@tid AND deleted_at IS NULL) AS don_thuoc,
  (SELECT COUNT(*) FROM pha_prescription_items WHERE tenant_id=@tid AND deleted_at IS NULL) AS don_thuoc_items;
