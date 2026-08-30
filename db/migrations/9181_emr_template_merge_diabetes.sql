-- ============================================================
-- Migration (DRAFT - CHUA CHAY): 9181_emr_template_merge_diabetes
-- Tac gia: Lanh (architect) - 2026-08-30
-- Quyet dinh BO (Q5.1 - DA DONG):
--   Bac si CHON tu danh sach template co san -> he thong hien dung theo template do.
--   Lam theo cach DON GIAN (theo huong diaB), KHONG lam schema JSON phuc tap.
--   => XOA BO luong `diab_his_cli_diabetes_templates` rieng biet, chuyen moi ban ghi
--      thanh 1 dong trong `diab_his_cli_emr_templates` voi speciality='DIABETES'.
--
-- Nguyen tac giu nguyen (R5.4): KHONG doi y nghia `content_json`
--   (van la Tiptap ProseMirror doc, de EmrVersion diff + EmrSignature hash khong vo).
--   Phan cau hinh truong cua template cu (template_json/default_values/checklist)
--   -> do vao cot MOI `structured_json`.
--
-- Idempotent: YES (add_col_if_missing + INSERT ... WHERE NOT EXISTS)
-- Phu thuoc: 0000_helpers.sql, 0026_create_emr_templates.sql, 0015, 9135
-- Backward compatible: cot moi NULL-able; bang cu KHONG bi drop trong migration nay.
-- ============================================================
SET NAMES utf8mb4;

-- ------------------------------------------------------------
-- (1) Cot moi tren diab_his_cli_emr_templates
-- ------------------------------------------------------------
CALL add_col_if_missing('diab_his_cli_emr_templates', 'structured_json',
  "JSON NULL COMMENT 'Cau hinh truong/checklist cua template (muc don gian). NULL = template tu do Tiptap thuan. KHONG thay the content_json'");

CALL add_col_if_missing('diab_his_cli_emr_templates', 'is_default',
  "TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'Template mac dinh cua tenant theo speciality (goi y khi mo man kham)'");

CALL add_col_if_missing('diab_his_cli_emr_templates', 'legacy_source',
  "VARCHAR(50) NULL COMMENT 'Nguon di tru, vd diabetes_templates - de truy vet va chong migrate trung'");

CALL add_col_if_missing('diab_his_cli_emr_templates', 'legacy_source_id',
  "VARCHAR(50) NULL COMMENT 'Id ban ghi goc o bang legacy'");

CALL add_index_if_missing('diab_his_cli_emr_templates', 'idx_emr_tpl_legacy',
  '(legacy_source, legacy_source_id)');
CALL add_index_if_missing('diab_his_cli_emr_templates', 'idx_emr_tpl_spec_default',
  '(tenant_id, speciality, is_default)');

-- ------------------------------------------------------------
-- (2) CONVERT du lieu: diab_his_cli_diabetes_templates -> diab_his_cli_emr_templates
--     - name          -> name (giu nguyen)
--     - tenant_id     -> tenant_id (NULL = template he thong, cung quy uoc 2 ben)
--     - is_system     -> is_system
--     - is_default    -> is_default
--     - template_json + default_values + checklist -> structured_json (gom lai 1 object)
--     - content_json  -> doc Tiptap RONG hop le (KHONG nhet JSON cau hinh vao day)
--     - speciality    -> 'DIABETES'
--     Chong chay trung: WHERE NOT EXISTS theo (legacy_source, legacy_source_id).
-- ------------------------------------------------------------
INSERT INTO diab_his_cli_emr_templates
    (id, tenant_id, name, content_json, structured_json, speciality,
     is_system, is_default, legacy_source, legacy_source_id, created_at, created_by, updated_at)
SELECT
    UUID(),
    t.tenant_id,
    t.name,
    -- Tiptap doc rong hop le: bac si van soan tu do nhu hom nay
    '{"type":"doc","content":[{"type":"paragraph"}]}',
    JSON_OBJECT(
        'source',   'diabetes_templates',
        'fields',   t.template_json,
        'defaults', t.default_values,
        'checklist', t.checklist
    ),
    'DIABETES',
    COALESCE(t.is_system, 0),
    COALESCE(t.is_default, 0),
    'diabetes_templates',
    CAST(t.id AS CHAR),
    COALESCE(t.created_at, NOW()),
    NULL,                                   -- created_by 2 bang khac kieu (INT vs CHAR(36)) => de NULL
    NOW()
FROM diab_his_cli_diabetes_templates t
WHERE t.deleted_at IS NULL
  AND NOT EXISTS (
        SELECT 1 FROM diab_his_cli_emr_templates e
        WHERE e.legacy_source = 'diabetes_templates'
          AND e.legacy_source_id = CAST(t.id AS CHAR)
  );

-- ------------------------------------------------------------
-- (3) Danh dau bang cu la LEGACY - KHONG DROP trong migration nay
--     Chi drop sau khi xac nhan khong con code doc:
--       backend/src/ProDiabHis.Application/Diabetes/DiabetesHandlers.cs
--       backend/src/ProDiabHis.Infrastructure/Persistence/Configurations/DiabetesConfiguration.cs
--       (+ FE man hinh diabetes-templates neu con)
--     LUU Y: `diab_his_cli_diabetes_assessments` KHONG lien quan, GIU NGUYEN.
-- ------------------------------------------------------------
-- ALTER TABLE diab_his_cli_diabetes_templates
--   COMMENT='DEPRECATED 2026-08-30 - da gop vao diab_his_cli_emr_templates (speciality=DIABETES). Khong ghi moi';

-- ------------------------------------------------------------
-- (4) Bang noi template <-> goi dich vu (giu nguyen thiet ke muc 5.3.2)
--     Dung de loc danh sach template theo goi benh nhan dang dung.
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS diab_his_cli_emr_template_package_map (
    id             CHAR(36)    NOT NULL DEFAULT (UUID()),
    tenant_id      INT         NOT NULL,
    template_id    CHAR(36)    NOT NULL COMMENT 'FK logic -> diab_his_cli_emr_templates.id',
    package_id     CHAR(36)    NOT NULL COMMENT 'FK logic -> diab_his_pkg_service_packages.id',
    service_id     CHAR(36)    NULL     COMMENT 'NULL = ap dung moi dich vu trong goi',
    visit_sequence INT         NULL     COMMENT 'NULL = moi luot; 1 = luot dau tien...',
    is_default     TINYINT(1)  NOT NULL DEFAULT 1,
    created_at     DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    created_by     CHAR(36)    NULL,
    updated_at     DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
    updated_by     CHAR(36)    NULL,
    deleted_at     DATETIME(3) NULL,
    deleted_by     CHAR(36)    NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_tpm_pkg_svc_seq (package_id, service_id, visit_sequence),
    INDEX idx_tpm_tenant_pkg (tenant_id, package_id),
    INDEX idx_tpm_template   (template_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Anh xa mau benh an <-> goi dich vu (loc/goi y template cho luot kham thuoc goi)';
-- KHONG dat FK cung (dong bo cach lam cua pkg_entitlement_definitions), validate o service layer.

-- ------------------------------------------------------------
-- (5) Danh dau luot kham thuoc goi nao (T2 - muc 4.4.2)
--     SUA LOI TAI LIEU BAN DAU: ten bang encounter THUC TE la
--     `diab_his_enc_encounters` (theo docs/architecture/canonical-table-names.md:310),
--     KHONG phai `diab_his_cli_encounters`.
-- ------------------------------------------------------------
CALL add_col_if_missing('diab_his_enc_encounters', 'covered_by_subscription_id',
  "CHAR(36) NULL COMMENT 'Luot kham thuoc goi nao (denormalize tu pkg_usage_logs de hien thi/loc nhanh)'");
CALL add_index_if_missing('diab_his_enc_encounters', 'idx_enc_covered',
  '(tenant_id, covered_by_subscription_id)');
-- ============================================================
