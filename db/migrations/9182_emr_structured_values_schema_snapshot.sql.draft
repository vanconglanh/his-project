-- ============================================================
-- Migration: 9182_emr_structured_values_schema_snapshot   [DE XUAT - CHUA CHAY]
-- Tai lieu: docs/prd/kien-truc-master-data-package-emr-20260830.md  §5.8
-- Muc dich:
--   (QD4) Tach GIA TRI bac si nhap ra khoi DINH NGHIA template.
--         EmrTemplate.structured_json = dinh nghia form (dung chung).
--         Gia tri cua 1 luot kham -> luu tren ban ghi benh an cua luot kham do.
--   (QD5) Snapshot schema tai thoi diem tao/ky ban ghi, theo dung pattern
--         snapshot da co o module goi dich vu
--         (PackageSubscriptionHandlers.cs:171-202 - package_*_snapshot +
--          nhan ban PackageEntitlementDefinition -> pkg_entitlement_balances).
--         Sua template goc SAU khi ky KHONG duoc lam doi hien thi benh an cu.
--
-- Bang thuc te (da doc code, khong phong doan):
--   EmrVersion   -> diab_his_cli_emr_versions   (EncounterConfiguration.cs:139)
--   EmrContent   -> diab_his_enc_emr_contents   (EncounterConfiguration.cs:112)
--   EmrTemplate  -> diab_his_cli_emr_templates  (EncounterConfiguration.cs:185)
--
-- PHU THUOC: 0000_helpers.sql, 0027_create_emr_signatures.sql,
--            9181_emr_template_merge_diabetes.sql.draft  (tao structured_json
--            tren diab_his_cli_emr_templates)  <-- PHAI CHAY 9181 TRUOC
--
-- Idempotent: YES (add_col_if_missing / add_index_if_missing)
-- Backward compatible: 100% - moi cot NULL-able, KHONG backfill, KHONG FK cung.
--
-- !!! CANH BAO ANH HUONG DU LIEU CU (xem §5.8.4) !!!
--   Benh an DA KY truoc migration nay se co schema_snapshot_json = NULL.
--   - Hien thi: render nhu hien tai theo content_json. KHONG suy dien form.
--   - Chu ky: van hop le theo payload v1 (chi content_json).
--   - TUYET DOI KHONG backfill snapshot va KHONG yeu cau ky lai hang loat.
-- ============================================================
SET NAMES utf8mb4;

-- ------------------------------------------------------------
-- (1) EmrVersion - ban chup tung phien ban benh an
-- ------------------------------------------------------------
CALL add_col_if_missing('diab_his_cli_emr_versions', 'template_id',
  "CHAR(36) NULL COMMENT 'Mau benh an da dung (tham chieu logic diab_his_cli_emr_templates.id, KHONG FK cung). Chi dung de truy vet/bao cao - KHONG dung de render'");

CALL add_col_if_missing('diab_his_cli_emr_versions', 'structured_values_json',
  "JSON NULL COMMENT 'PHI - Gia tri bac si nhap cho luot kham nay: {key: value} khop key trong structured_json cua template da dung'");

CALL add_col_if_missing('diab_his_cli_emr_versions', 'schema_snapshot_json',
  "JSON NULL COMMENT 'Chup nguyen ven EmrTemplate.structured_json tai thoi diem tao/ky ban ghi nay. LUON render benh an theo cot nay, KHONG BAO GIO doc lai template hien tai. NULL = ban ghi tao truoc migration 9182'");

-- ------------------------------------------------------------
-- (2) EmrContent - working copy dang soan tren man kham
--     (template_id da ton tai san - EmrContent.cs:12)
-- ------------------------------------------------------------
CALL add_col_if_missing('diab_his_enc_emr_contents', 'structured_values_json',
  "JSON NULL COMMENT 'PHI - Gia tri form dang soan. Moi lan luu nhap duoc chup sang diab_his_cli_emr_versions giong cach content_json dang lam (EmrHandlers.cs:136-148)'");

-- ------------------------------------------------------------
-- (3) Index
-- ------------------------------------------------------------
CALL add_index_if_missing('diab_his_cli_emr_versions', 'idx_emr_ver_template', '(tenant_id, template_id)');

-- ------------------------------------------------------------
-- (4) KHONG backfill - co y.
--     Chup template HIEN TAI vao benh an QUA KHU = lam gia bang chung,
--     dung thu ma quyet dinh nay sinh ra de ngan (§5.8.4).
--     Ban ghi cu de NULL: "tao truoc khi co co che snapshot".
-- ------------------------------------------------------------

-- ------------------------------------------------------------
-- Query kiem tra sau khi chay (chay tay):
--   SELECT COUNT(*) AS ver_v1_khong_snapshot
--   FROM diab_his_cli_emr_versions
--   WHERE schema_snapshot_json IS NULL;      -- = toan bo ban ghi cu, dung nhu ky vong
--
--   SELECT COUNT(*) AS ver_da_ky_v1
--   FROM diab_his_cli_emr_versions
--   WHERE is_signed = 1 AND schema_snapshot_json IS NULL;  -- nhom phai giu duong verify v1
-- ------------------------------------------------------------

-- ============================================================
-- VIEC CODE PHAI LAM KEM (migration nay KHONG du - xem §5.8.6):
--  1. Entity: them StructuredValuesJson/TemplateId/SchemaSnapshotJson vao EmrVersion,
--     StructuredValuesJson vao EmrContent (Domain/Entities/EmrContent.cs)
--  2. Map cot moi trong EncounterConfiguration.cs (dong 112 va 139)
--  3. SaveDraft (EmrHandlers.cs:102-148): ghi structured values + CHUP
--     structured_json cua template dang chon vao schema_snapshot_json + set template_id
--  4. LO HONG TUAN THU - BAT BUOC: Sign (EmrHandlers.cs:182) hien hash CHI tren
--     content_json:
--         var contentBytes = Encoding.UTF8.GetBytes(emr.ContentJson);
--     -> doi sang payload v2 gop 3 phan:
--         "v2\n" + content_json + "\n" + (structured_values_json ?? "")
--                + "\n" + (schema_snapshot_json ?? "")
--     Neu khong sua: sau khi ky van co the sua gia tri form / schema snapshot
--     ma chu ky VAN HOP LE.
--     (EmrSignatureVerifierAdapter.cs:30 giu nguyen thuat toan SHA256, chi doi dau vao)
--  5. Giu duong verify v1 cho ban ghi co ca 2 cot moi = NULL. KHONG ky lai hang loat.
--  6. Test: EmrSignFlowTests.cs - sua structured_values_json sau ky => verify PHAI fail.
-- ============================================================
