-- ============================================================
-- Migration: 9120_merge_dispense_records
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-08-21
-- Ly do: nghiep vu cap phat thuoc bi che doi 2 bang cung ton tai du
--   lieu that tren production:
--     - diab_his_pha_dispense_records (tao boi 0038, dung boi TOAN BO
--       luong nghiep vu song: DispensingHandlers 11 cho, BillingCalculatorImpl
--       tinh tien thuoc) -> 12 dong
--     - diab_his_pha_dispenses (tao boi 9005, chi con EF DbSet + config
--       ToTable, KHONG co handler nghiep vu nao doc/ghi qua _db.Dispenses)
--       -> 15 dong (du lieu mo côi tu flow EF cu, khong con duong code
--       nao tao ra dong moi trong bang nay nua)
--   CANONICAL: diab_his_pha_dispense_records, vi:
--     1. La bang duy nhat dang duoc doc/ghi boi code nghiep vu hien hanh
--        (kiem tra trung don da phat, tinh doanh thu thuoc trong billing).
--     2. Schema chuan hoa day du hon: co bang con dispense_items voi
--        batch_no/expiry_date/unit_cost tung dong thuoc (can cho bao cao
--        ton kho/xuat kho va XML BHYT Bang 2 mahieu-lo/han-dung); trong khi
--        pha_dispenses chi luu items_json dang blob, khong the join truc
--        tiep voi pha_stock/pha_drugs de ra bao cao ton kho.
--     3. unique key (prescription_id, tenant_id) tren dispense_records da
--        chan duoc 1 don phat 2 lan -> dung lam khoa chong trung khi hop nhat.
--   KHONG DROP/XOA du lieu goc: pha_dispenses giu nguyen lam ban sao an
--   toan, chi doc-va-chep du lieu con thieu sang dispense_records.
-- Idempotent: YES (INSERT ... WHERE NOT EXISTS, rerun khong nhan doi)
-- ============================================================
SET NAMES utf8mb4;

-- Danh dau pha_dispenses la bang deprecated (chi de comment, KHONG DROP)
ALTER TABLE `diab_his_pha_dispenses`
    COMMENT = 'DEPRECATED (migration 9120) - ban sao an toan, KHONG dung trong code moi. Canonical: diab_his_pha_dispense_records';

-- ------------------------------------------------------------------
-- Buoc 1: chep header. Chi chep nhung don CHUA co trong dispense_records
-- (khop theo tenant_id + prescription_id, giong dung unique key that
-- da co san tren bang canonical -> tu dong loai trung lap giua 12 va 15
-- dong neu cung 1 lan cap phat da duoc ghi o ca hai noi).
-- dispensed_by cua pha_dispenses la CHAR(36) UUID (sec_users.id) trong khi
-- dispense_records.dispensed_by la INT legacy (code hien tai luon ghi 0)
-- -> khong the map truc tiep, giu NULL va luu lai UUID goc vao note de
-- khong mat du lieu audit.
-- ------------------------------------------------------------------
INSERT INTO `diab_his_pha_dispense_records`
    (id, tenant_id, prescription_id, warehouse_id, dispensed_at, dispensed_by,
     status, note, total_amount, created_at, created_by, updated_at, updated_by, deleted_at)
SELECT
    d.id,
    d.tenant_id,
    d.prescription_id,
    'default' AS warehouse_id,
    d.dispensed_at,
    NULL AS dispensed_by,
    'DISPENSED' AS status,
    CONCAT('[Migrated tu diab_his_pha_dispenses migration 9120] ',
           'legacy_dispensed_by=', d.dispensed_by,
           CASE WHEN d.note IS NULL OR d.note = '' THEN '' ELSE CONCAT(' | ', d.note) END) AS note,
    0 AS total_amount,
    d.created_at,
    NULL AS created_by,
    d.updated_at,
    NULL AS updated_by,
    NULL AS deleted_at
FROM `diab_his_pha_dispenses` d
WHERE NOT EXISTS (
    SELECT 1 FROM `diab_his_pha_dispense_records` dr
    WHERE dr.tenant_id = d.tenant_id
      AND dr.prescription_id = d.prescription_id
);

-- ------------------------------------------------------------------
-- Buoc 2: chep chi tiet tung dong thuoc tu items_json (neu co) sang
-- dispense_items, chi cho cac header vua duoc chep o Buoc 1 (nhan dien
-- qua note bat dau bang chuoi danh dau) va chua co dispense_items nao
-- (idempotent: neu chay lai, header da ton tai + da co items thi bo qua).
-- items_json ky vong dang [{"drug_id":"...","lot_number":"...","qty":n}, ...]
-- Field ten co the lech (batch_no/lot_number, qty/quantity) nen dung
-- COALESCE JSON_EXTRACT nhieu key cho an toan. Dong nao thieu drug_id thi bo qua.
-- ------------------------------------------------------------------
INSERT INTO `diab_his_pha_dispense_items`
    (id, tenant_id, dispense_record_id, prescription_item_id, drug_id,
     batch_no, expiry_date, quantity, unit_cost, is_returned, returned_quantity,
     created_at, updated_at, deleted_at)
SELECT
    UUID() AS id,
    dr.tenant_id,
    dr.id AS dispense_record_id,
    '' AS prescription_item_id,
    CAST(JSON_UNQUOTE(COALESCE(jt.drug_id, JSON_EXTRACT(jt.raw, '$.drugId'))) AS CHAR(36)) AS drug_id,
    CAST(COALESCE(JSON_UNQUOTE(jt.batch_no), JSON_UNQUOTE(jt.lot_number), 'UNKNOWN') AS CHAR(50)) AS batch_no,
    '1970-01-01' AS expiry_date,
    COALESCE(CAST(jt.qty AS DECIMAL(10,2)), CAST(jt.quantity AS DECIMAL(10,2)), 0) AS quantity,
    0 AS unit_cost,
    0 AS is_returned,
    0 AS returned_quantity,
    NOW() AS created_at,
    NOW() AS updated_at,
    NULL AS deleted_at
FROM `diab_his_pha_dispense_records` dr
INNER JOIN `diab_his_pha_dispenses` d
    ON d.id = dr.id AND d.tenant_id = dr.tenant_id
JOIN JSON_TABLE(
    d.items_json,
    '$[*]' COLUMNS (
        raw       JSON       PATH '$',
        drug_id   JSON       PATH '$.drug_id',
        batch_no  JSON       PATH '$.batch_no',
        lot_number JSON      PATH '$.lot_number',
        qty       JSON       PATH '$.qty',
        quantity  JSON       PATH '$.quantity'
    )
) AS jt ON 1 = 1
WHERE dr.note LIKE '[Migrated tu diab_his_pha_dispenses migration 9120]%'
  AND COALESCE(jt.drug_id, JSON_EXTRACT(jt.raw, '$.drugId')) IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM `diab_his_pha_dispense_items` di WHERE di.dispense_record_id = dr.id
  );
