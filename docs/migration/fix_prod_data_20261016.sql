-- =====================================================================
-- fix_prod_data_20261016.sql
-- Sua loi chat luong du lieu production pro-his-db (db prodiab_his, tenant 1)
-- phat hien trong dot kiem tra 2026-10-16 (xem docs/migration/prod-data-check-20261016.md).
--
-- AN TOAN: chi UPDATE tren bang nghiep vu HIS moi (diab_his_*), tenant_id=1.
--          KHONG dung toi SUNS / diab_his nguon / staging.
-- IDEMPOTENT: moi UPDATE chi nham vao gia tri SAI; chay lai -> 0 rows affected.
-- BACKUP truoc khi chay: /home/stackops/backups/pro-his/prodiab_his_20261016_013223.sql.gz
--
-- Chay: docker exec -i -e MYSQL_PWD=<root> pro-his-db \
--        mysql -uroot --default-character-set=utf8mb4 prodiab_his < fix_prod_data_20261016.sql
-- =====================================================================
SET @t := 1;  -- tenant

-- ---------------------------------------------------------------------
-- FIX-1: Lab ORDER status 'COMPLETED' (hoa) -> 'done' (app dung chu thuong:
--        ordered/sample_taken/processing/done/cancelled). Domain/Entities/ClsOrder.cs:41-60
-- ---------------------------------------------------------------------
UPDATE diab_his_cli_lab_orders
   SET status = 'done', updated_at = NOW()
 WHERE tenant_id = @t AND status = 'COMPLETED';

-- FIX-2: Lab ORDER priority 'ROUTINE' -> 'NORMAL' (ClsPriority: NORMAL/URGENT/STAT)
UPDATE diab_his_cli_lab_orders
   SET priority = 'NORMAL', updated_at = NOW()
 WHERE tenant_id = @t AND priority = 'ROUTINE';

-- ---------------------------------------------------------------------
-- FIX-3: Lab RESULT status 'PRELIMINARY' -> 'VERIFIED'
--        (app hop le: DRAFT/VERIFIED/AMENDED; PRELIMINARY chi la gia tri FHIR output,
--         khong doc duoc noi bo). Ket qua la du lieu lich su da hoan tat -> VERIFIED.
-- FIX-4: Lab RESULT source 'LEGACY_IMPORT' -> 'IMPORT' (hop le: MANUAL/IMPORT/PARTNER)
-- ---------------------------------------------------------------------
UPDATE diab_his_lab_results
   SET status = 'VERIFIED', updated_at = NOW()
 WHERE tenant_id = @t AND status = 'PRELIMINARY';

UPDATE diab_his_lab_results
   SET source = 'IMPORT', updated_at = NOW()
 WHERE tenant_id = @t AND source = 'LEGACY_IMPORT';

-- ---------------------------------------------------------------------
-- FIX-5: ENCOUNTER lich su (toan bo 649 la LEG-%) dang IN_PROGRESS -> DONE.
--        Ly do: lượt khám da migrate tu HIS cu la lich su DA XONG; de IN_PROGRESS
--        se ket vao hang cho lam viec cua bac si. IN_PROGRESS la gia tri HOP LE
--        nhung SAI ngu nghia cho du lieu lich su. (reversible tu backup)
--        + Xoa finished_at RAC (discharge < admission, 224 ban ghi) ve NULL.
-- ---------------------------------------------------------------------
UPDATE diab_his_enc_encounters
   SET finished_at = NULL, updated_at = NOW()
 WHERE tenant_id = @t AND finished_at IS NOT NULL AND finished_at < started_at;

UPDATE diab_his_enc_encounters
   SET status = 'DONE', updated_at = NOW()
 WHERE tenant_id = @t AND encounter_no LIKE 'LEG-%' AND status = 'IN_PROGRESS';

-- ---------------------------------------------------------------------
-- FIX-6: 4 thuoc gia khop SAI khi import SUNS -> dua ve 0 + ghi chu nhap tay.
--   * LEG Augmentin & Augmentin 500mg bi gan gia cua "AUGMENTIN 1G" (31.000d).
--   * Xigduo XR 10mg/500mg & 5mg/500mg bi gan gia cua "Xigduo XR 10mg/1000mg" (33.000d).
-- Ghi chu de nhan vien nhap lai gia dung.
-- ---------------------------------------------------------------------
UPDATE diab_his_pha_drugs
   SET sell_price = 0, price = 0, bhyt_price = NULL,
       note = CONCAT_WS(' | ', NULLIF(note,''),
              'GIA CAN NHAP TAY: bi gan sai gia khi import SUNS 2026-10-16, da reset ve 0'),
       updated_at = NOW()
 WHERE tenant_id = @t
   AND code IN ('LEG-8AA1BB33',   -- Augmentin
                'LEG-42FAEACA',   -- Augmentin 500mg
                'LEG-2366A0E3',   -- XIGDUO XR 10mg/500mg
                'LEG-BDAFD05E')   -- Xigduo XR 5mg/500mg
   AND sell_price <> 0;

-- ---------------------------------------------------------------------
-- FIX-7: 8 prescription_items tro drug_id khong ton tai -> relink theo drug_name
--        (chi khi drug_name khop DUY NHAT 1 thuoc con hieu luc cung tenant).
--        drug_name da denormalized nen hien thi van dung; fix nay khoi phuc FK.
-- ---------------------------------------------------------------------
-- Dung TRIM (ten item vs ten drug chi khac khoang trang thua, cung nguon legacy).
-- Chi relink khi TRIM(name) khop DUY NHAT 1 thuoc con hieu luc -> khong the mislink.
UPDATE diab_his_pha_prescription_items i
   SET i.drug_id = (SELECT d.id FROM diab_his_pha_drugs d
                     WHERE d.tenant_id = i.tenant_id AND d.deleted_at IS NULL
                       AND TRIM(d.name) = TRIM(i.drug_name) LIMIT 1),
       i.updated_at = NOW()
 WHERE i.tenant_id = @t
   AND NOT EXISTS (SELECT 1 FROM diab_his_pha_drugs dx WHERE dx.id = i.drug_id)   -- item orphan
   AND (SELECT COUNT(*) FROM diab_his_pha_drugs d
         WHERE d.tenant_id = i.tenant_id AND d.deleted_at IS NULL
           AND TRIM(d.name) = TRIM(i.drug_name)) = 1;                             -- khop duy nhat
