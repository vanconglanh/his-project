-- ============================================================
-- Migration (DRAFT - CHUA CHAY): 9180_drug_route_bhyt_code
-- Tac gia: Lanh (architect) - 2026-08-30
-- Quyet dinh BO: chot bo cot 9005 (drug_form/sell_price/requires_rx/name)
--   la NGUON SU THAT. Bo cot 9010 (form/price/requires_prescription/name_vi)
--   -> DEPRECATED, giu lai, KHONG drop trong migration nay.
--
-- Migration nay gom 3 phan:
--   (A) PRE-CHECK  : query kiem tra thuc te truoc khi chay (BAT BUOC chay tay truoc)
--   (B) SYNC       : dong bo 1 chieu 9010 -> 9005 (chi khi 9005 rong/0)
--   (C) ADD COLUMN : them `route` (BAT BUOC - va lo hong hardcode "uong")
--                    + `bhyt_code` cho thuoc
--   (D) DEPRECATE  : gan COMMENT 'DEPRECATED' len cot 9010
--
-- Bang thuc: diab_his_pha_drugs
--   LUU Y: code cu con dung alias `pha_drug_master` (ClosedXmlImporter.cs,
--   db/seeds/sample_pharmacy_demo.sql). Dev PHAI xac nhan alias nay la VIEW
--   tren diab_his_pha_drugs hay la BANG RIENG truoc khi chay. Neu la bang rieng
--   => phai chay ca 2, va do la mot no ky thuat khac phai bao cao lai.
--
-- Idempotent: YES (add_col_if_missing / add_index_if_missing tu 0000_helpers.sql)
-- Phu thuoc: 0000_helpers.sql, 9005_create_pharmacy.sql, 9010, 9110
-- Backward compatible: cot moi deu NULL-able; SYNC chi GHI KHI DICH DANG RONG.
-- ============================================================
SET NAMES utf8mb4;

-- ------------------------------------------------------------
-- (A) PRE-CHECK - CHAY TAY TRUOC, DOC KET QUA ROI MOI QUYET DINH CHAY (B)
--     Muc dich: tra loi cau hoi "bo 9010 co du lieu ma 9005 khong co khong?"
--     KHONG duoc gia dinh. Neu tat ca cot _need_sync = 0 => bo qua han phan (B).
-- ------------------------------------------------------------
-- SELECT
--   COUNT(*)                                                            AS total_rows,
--   SUM(CASE WHEN COALESCE(NULLIF(TRIM(`name`),''),NULL) IS NULL
--             AND COALESCE(NULLIF(TRIM(`name_vi`),''),NULL) IS NOT NULL
--            THEN 1 ELSE 0 END)                                         AS name_need_sync,
--   SUM(CASE WHEN COALESCE(NULLIF(TRIM(`drug_form`),''),NULL) IS NULL
--             AND COALESCE(NULLIF(TRIM(`form`),''),NULL) IS NOT NULL
--            THEN 1 ELSE 0 END)                                         AS form_need_sync,
--   SUM(CASE WHEN COALESCE(`sell_price`,0) = 0
--             AND COALESCE(`price`,0) > 0
--            THEN 1 ELSE 0 END)                                         AS price_need_sync,
--   SUM(CASE WHEN COALESCE(`requires_rx`,0) = 0
--             AND COALESCE(`requires_prescription`,0) = 1
--            THEN 1 ELSE 0 END)                                         AS rx_need_sync,
--   SUM(CASE WHEN COALESCE(`is_controlled`,0) = 0
--             AND (COALESCE(`is_narcotic`,0) = 1 OR COALESCE(`is_psychotropic`,0) = 1)
--            THEN 1 ELSE 0 END)                                         AS control_need_sync
-- FROM diab_his_pha_drugs
-- WHERE deleted_at IS NULL;
--
-- BANG CHUNG VI SAO PHAN (B) CAN TON TAI (doc tu code, khong doan):
--   - backend/.../Pharmacy/ClosedXmlImporter.cs:104-122 - luong IMPORT EXCEL thuoc
--     GHI VAO BO 9010 (name_vi/form/price/requires_prescription/is_psychotropic/
--     is_narcotic), KHONG ghi vao bo 9005. => moi thuoc nhap bang Excel deu co
--     nguy co: bo 9005 rong, bo 9010 co du lieu.
--   - backend/.../Reports/ReportRegistry.cs:1967 comment "uu tien name_vi
--     (hien dang rong o data that) roi name" + hang loat truy van
--     COALESCE(NULLIF(d.name_vi,''), d.name) => bao cao dang phong thu 2 chieu.
--   - ReportRegistry.cs:2255-2256 COALESCE(NULLIF(d.sell_price,0), d.price, 0)
--     va (d.requires_prescription = 1 OR d.requires_rx = 1)
--     => xac nhan 2 bo cot dang thuc su chua du lieu lech nhau.

-- ------------------------------------------------------------
-- (B) SYNC 1 CHIEU 9010 -> 9005
--     Nguyen tac an toan: CHI ghi khi dich dang rong/0. KHONG BAO GIO ghi de
--     gia tri da co o bo 9005 (bo 9005 luon thang).
--     Chay trong transaction, backup bang truoc khi chay tren production.
-- ------------------------------------------------------------
-- CREATE TABLE diab_his_pha_drugs_bak_9180 AS SELECT * FROM diab_his_pha_drugs;  -- backup

UPDATE diab_his_pha_drugs
SET `name` = TRIM(`name_vi`)
WHERE deleted_at IS NULL
  AND (`name` IS NULL OR TRIM(`name`) = '')
  AND `name_vi` IS NOT NULL AND TRIM(`name_vi`) <> '';

UPDATE diab_his_pha_drugs
SET `drug_form` = TRIM(`form`)
WHERE deleted_at IS NULL
  AND (`drug_form` IS NULL OR TRIM(`drug_form`) = '')
  AND `form` IS NOT NULL AND TRIM(`form`) <> '';

UPDATE diab_his_pha_drugs
SET `sell_price` = `price`
WHERE deleted_at IS NULL
  AND COALESCE(`sell_price`, 0) = 0
  AND COALESCE(`price`, 0) > 0;

UPDATE diab_his_pha_drugs
SET `requires_rx` = 1
WHERE deleted_at IS NULL
  AND COALESCE(`requires_rx`, 0) = 0
  AND COALESCE(`requires_prescription`, 0) = 1;

-- is_controlled la HOP (OR) cua is_narcotic / is_psychotropic (xem N4 trong tai lieu)
UPDATE diab_his_pha_drugs
SET `is_controlled` = 1
WHERE deleted_at IS NULL
  AND COALESCE(`is_controlled`, 0) = 0
  AND (COALESCE(`is_narcotic`, 0) = 1 OR COALESCE(`is_psychotropic`, 0) = 1);

-- ------------------------------------------------------------
-- (C) COT MOI - BAT BUOC
-- ------------------------------------------------------------
-- route: va lo hong hardcode "uong" tai
--   backend/src/ProDiabHis.Infrastructure/Bhyt/BhytXmlGeneratorImpl.cs:192
--   (DUONG_DUNG cua XML 4210 Bang 2 dang fallback cung "uong" khi
--    pha_prescription_items.route rong => XML giam dinh sai).
-- Sau khi co cot nay: BhytXmlSql lay theo thu tu uu tien
--   prescription_items.route -> drugs.route -> (neu ca 2 rong) BAO LOI, KHONG hardcode.
CALL add_col_if_missing('diab_his_pha_drugs', 'route',
  "VARCHAR(30) NULL COMMENT 'Duong dung chuan hoa (code_master group DRUG_ROUTE): uong|tiem_bap|tiem_tinh_mach|tiem_duoi_da|truyen_tinh_mach|ngam|dat|boi_ngoai|nho_mat|nho_mui|xit|hit|khac. Nguon cho XML 4210 Bang 2 DUONG_DUNG'");

-- bhyt_code: hien XML Bang 2 dang day MA NOI BO (`code`) => rui ro tu choi giam dinh (R3.1)
CALL add_col_if_missing('diab_his_pha_drugs', 'bhyt_code',
  "VARCHAR(50) NULL COMMENT 'Ma thuoc theo danh muc thuoc BHYT - dung cho XML 4210 Bang 2 thay vi ma noi bo'");

CALL add_index_if_missing('diab_his_pha_drugs', 'idx_drugs_route',     '(tenant_id, route)');
CALL add_index_if_missing('diab_his_pha_drugs', 'idx_drugs_bhyt_code', '(tenant_id, bhyt_code)');

-- Backfill route cho thuoc da co ke don: lay duong dung xuat hien nhieu nhat
-- trong lich su ke don cua chinh thuoc do (an toan hon de NULL roi hardcode).
UPDATE diab_his_pha_drugs d
JOIN (
    SELECT pi.drug_id,
           SUBSTRING_INDEX(
             GROUP_CONCAT(pi.route ORDER BY pi.id DESC SEPARATOR ','), ',', 1
           ) AS last_route
    FROM diab_his_pha_prescription_items pi
    WHERE pi.route IS NOT NULL AND TRIM(pi.route) <> ''
    GROUP BY pi.drug_id
) x ON x.drug_id = d.id
SET d.route = x.last_route
WHERE d.deleted_at IS NULL
  AND (d.route IS NULL OR TRIM(d.route) = '');
-- LUU Y: dev xac nhan ten cot khoa (pi.drug_id / pi.id) truoc khi chay.

-- ------------------------------------------------------------
-- (D) DANH DAU DEPRECATED bo cot 9010 - CHI SUA COMMENT, KHONG DROP
--     Muc dich: bat cu ai doc schema deu thay ngay cot nao khong dung cho code moi.
--     Viec DROP se lam o migration rieng SAU KHI xac nhan khong con code doc:
--       - ClosedXmlImporter.cs  (dang GHI vao 9010 -> phai sua truoc)
--       - ReportRegistry.cs     (dang DOC 9010 qua COALESCE -> don sau)
--       - ReportingServiceImpl.cs
--     => KHONG drop trong migration nay.
--     Dev PHAI doi kieu du lieu trong cau MODIFY cho khop schema thuc te
--     (lay tu SHOW CREATE TABLE diab_his_pha_drugs) - MySQL MODIFY se GHI DE kieu.
-- ------------------------------------------------------------
-- ALTER TABLE diab_his_pha_drugs
--   MODIFY COLUMN `name_vi` VARCHAR(255) NULL
--     COMMENT 'DEPRECATED 2026-08-30 (legacy 9010) - dung `name`. Khong dung cho code moi',
--   MODIFY COLUMN `form` VARCHAR(100) NULL
--     COMMENT 'DEPRECATED 2026-08-30 (legacy 9010) - dung `drug_form`',
--   MODIFY COLUMN `price` DECIMAL(15,2) NULL
--     COMMENT 'DEPRECATED 2026-08-30 (legacy 9010) - dung `sell_price`',
--   MODIFY COLUMN `requires_prescription` TINYINT(1) NULL
--     COMMENT 'DEPRECATED 2026-08-30 (legacy 9010) - dung `requires_rx`',
--   MODIFY COLUMN `is_narcotic` TINYINT(1) NULL
--     COMMENT 'DEPRECATED 2026-08-30 (legacy 9010) - dung `is_controlled` / `control_schedule`',
--   MODIFY COLUMN `is_psychotropic` TINYINT(1) NULL
--     COMMENT 'DEPRECATED 2026-08-30 (legacy 9010) - dung `is_controlled` / `control_schedule`';

-- ------------------------------------------------------------
-- VIEC CODE PHAI LAM KEM (khong thuoc migration):
--   1. ClosedXmlImporter.cs: doi INSERT/UPDATE sang bo 9005
--      (name, drug_form, sell_price, requires_rx, is_controlled) + them cot route.
--   2. BhytXmlGeneratorImpl.cs:192: bo fallback hardcode "uong",
--      doi sang lay drugs.route; neu van rong -> khong phat hanh XML, bao loi
--      DRUG_ROUTE_MISSING kem danh sach thuoc thieu.
--   3. Drug.cs (EF): map them Route, BhytCode.
--   4. Excel template nhap thuoc: bo sung cot `route` (optional o parser).
-- ============================================================
