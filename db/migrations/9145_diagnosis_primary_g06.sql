-- =============================================================
-- 9080_diagnosis_primary_g06.sql
-- G06: Chan doan CHINH vs KEM THEO (map MA_BENH / MA_BENH_KHAC - QD 4750)
-- Bang: diab_his_enc_diagnoses (cot `type` ENUM PRIMARY/SECONDARY)
-- Idempotent: dung add_col_if_missing / add_index_if_missing (0000_helpers.sql)
-- MySQL 8 KHONG ho tro ADD COLUMN IF NOT EXISTS -> bat buoc dung helper.
-- =============================================================

-- 1) Cot sap xep thu tu chan doan hien thi / xuat XML
CALL add_col_if_missing('diab_his_enc_diagnoses', 'sort_order', 'INT NOT NULL DEFAULT 0');

-- 2) Index phuc vu truy van tach chinh/phu theo tenant
CALL add_index_if_missing('diab_his_enc_diagnoses', 'idx_enc_diag_tenant_enc_type',
                          '(tenant_id, encounter_id, type)');

-- -------------------------------------------------------------
-- 3) Chuan hoa du lieu cu: dam bao moi encounter co DUNG 1 PRIMARY
--    MySQL khong co partial unique index -> rang buoc enforce o application layer
--    (AddDiagnosisCommandHandler), migration nay chi don du lieu lich su.
-- -------------------------------------------------------------

-- 3a) Encounter co NHIEU HON 1 PRIMARY -> giu ban ghi cu nhat, ha cap phan con lai
UPDATE diab_his_enc_diagnoses d
JOIN (
    SELECT x.id
    FROM diab_his_enc_diagnoses x
    JOIN (
        SELECT tenant_id, encounter_id, MIN(created_at) AS keep_at
        FROM diab_his_enc_diagnoses
        WHERE type = 'PRIMARY' AND deleted_at IS NULL
        GROUP BY tenant_id, encounter_id
        HAVING COUNT(*) > 1
    ) k ON k.tenant_id = x.tenant_id
       AND k.encounter_id = x.encounter_id
    WHERE x.type = 'PRIMARY'
      AND x.deleted_at IS NULL
      AND x.created_at > k.keep_at
) dup ON dup.id = d.id
SET d.type = 'SECONDARY',
    d.updated_at = NOW();

-- 3b) Encounter CO chan doan nhung KHONG co PRIMARY -> nang ban ghi cu nhat len PRIMARY
UPDATE diab_his_enc_diagnoses d
JOIN (
    SELECT x.id
    FROM diab_his_enc_diagnoses x
    JOIN (
        SELECT tenant_id, encounter_id, MIN(created_at) AS first_at
        FROM diab_his_enc_diagnoses
        WHERE deleted_at IS NULL
        GROUP BY tenant_id, encounter_id
        HAVING SUM(type = 'PRIMARY') = 0
    ) k ON k.tenant_id = x.tenant_id
       AND k.encounter_id = x.encounter_id
       AND x.created_at = k.first_at
    WHERE x.deleted_at IS NULL
) fix ON fix.id = d.id
SET d.type = 'PRIMARY',
    d.updated_at = NOW();
