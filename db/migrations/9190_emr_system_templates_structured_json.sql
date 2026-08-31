-- ============================================================
-- Migration: 9190_emr_system_templates_structured_json
-- Tac gia: Team Leader (fix QC) - 2026-08-31
-- Bug: UTC-EMR-08 / BUG-08 (Medium)
--   2 mau benh an HE THONG (seed o 0026_create_emr_templates.sql) co
--   structured_json = NULL -> API GET /emr-templates tra ve StructuredJson null
--   -> FE DynamicFormRenderer khong co field nao de render -> "form dong khong co noi dung".
--
-- Fix: dien structured_json = mang EmrFormField (khop type FE:
--   frontend/lib/api/types.ts EmrFormField / EmrFormSchema; render boi
--   frontend/components/emr/DynamicFormRenderer.tsx). Cac section lay theo chinh
--   content_json cua tung mau (heading trong Tiptap doc) de dong bo hien thi.
--
--   KHONG dung content_json (van la Tiptap doc de EmrVersion diff + chu ky hash),
--   chi bo sung structured_json (dinh nghia form muc don gian) theo dung nguyen tac
--   migration 9181/9182.
--
-- Idempotent: YES — chi UPDATE khi structured_json dang NULL/rong (khong de len
--   cau hinh da tuy bien). Match theo id co dinh cua 2 mau he thong.
-- Backward compatible: 100% — khong doi content_json, khong dung toi ban ghi benh an.
-- ============================================================
SET NAMES utf8mb4;

-- (1) Mau benh an tong quat (GENERAL)
UPDATE diab_his_cli_emr_templates
SET structured_json = CAST('[
  {"key":"ly_do_kham","label":"Lý do khám","type":"textarea","group":"Lý do khám","required":true,"colSpan":2},
  {"key":"tien_su","label":"Tiền sử (bản thân, gia đình, dị ứng)","type":"textarea","group":"Tiền sử","colSpan":2},
  {"key":"kham_lam_sang","label":"Khám lâm sàng","type":"textarea","group":"Khám lâm sàng","colSpan":2},
  {"key":"can_lam_sang","label":"Cận lâm sàng","type":"textarea","group":"Cận lâm sàng","colSpan":2},
  {"key":"chan_doan","label":"Chẩn đoán","type":"textarea","group":"Chẩn đoán","required":true,"colSpan":2},
  {"key":"huong_xu_tri","label":"Hướng xử trí","type":"textarea","group":"Hướng xử trí","colSpan":2}
]' AS JSON),
    updated_at = NOW()
WHERE id = 'aaaaaaaa-0001-0000-0000-000000000001'
  AND is_system = 1
  AND (structured_json IS NULL OR JSON_LENGTH(structured_json) = 0);

-- (2) Mau benh an dai thao duong (DIABETES)
UPDATE diab_his_cli_emr_templates
SET structured_json = CAST('[
  {"key":"ly_do_kham","label":"Lý do khám","type":"textarea","group":"Lý do khám","required":true,"colSpan":2},
  {"key":"tien_su_dtd","label":"Tiền sử đái tháo đường (thời gian mắc, điều trị hiện tại)","type":"textarea","group":"Tiền sử đái tháo đường","colSpan":2},
  {"key":"kham_lam_sang","label":"Khám lâm sàng","type":"textarea","group":"Khám lâm sàng","colSpan":2},
  {"key":"hba1c","label":"HbA1c","type":"number","unit":"%","group":"Cận lâm sàng (HbA1c, đường huyết, eGFR, ACR)"},
  {"key":"duong_huyet","label":"Đường huyết","type":"number","unit":"mmol/L","group":"Cận lâm sàng (HbA1c, đường huyết, eGFR, ACR)"},
  {"key":"egfr","label":"eGFR","type":"number","unit":"mL/phút/1.73m²","group":"Cận lâm sàng (HbA1c, đường huyết, eGFR, ACR)"},
  {"key":"acr","label":"ACR (Albumin/Creatinin niệu)","type":"number","unit":"mg/g","group":"Cận lâm sàng (HbA1c, đường huyết, eGFR, ACR)"},
  {"key":"chan_doan_bien_chung","label":"Chẩn đoán & biến chứng","type":"textarea","group":"Chẩn đoán & biến chứng","required":true,"colSpan":2},
  {"key":"muc_tieu_dieu_tri","label":"Mục tiêu điều trị & hướng xử trí","type":"textarea","group":"Mục tiêu điều trị & hướng xử trí","colSpan":2}
]' AS JSON),
    updated_at = NOW()
WHERE id = 'aaaaaaaa-0002-0000-0000-000000000002'
  AND is_system = 1
  AND (structured_json IS NULL OR JSON_LENGTH(structured_json) = 0);
-- ============================================================
