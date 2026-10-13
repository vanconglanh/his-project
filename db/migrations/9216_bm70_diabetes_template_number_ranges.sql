-- ============================================================
-- Migration: 9216_bm70_diabetes_template_number_ranges
-- Tac gia: QC (Claude) - 2026-10-13
-- Bug: BM-70 [HIS-25] Mau benh an nhan gia tri vo ly (HbA1c=99, duong huyet=-5)
--   Cac o so trong mau benh an dai thao duong (template he thong DIABETES,
--   id aaaaaaaa-0002-...) KHONG co min/max trong structured_json nen nhap
--   HbA1c=999 van luu duoc, khong canh bao / khong chan.
--
--   FE DA SAN SANG (khong can sua code):
--   - frontend/lib/api/types.ts: EmrFormField ho tro min?/max?/plausibleHint?
--   - frontend/components/emr/DynamicFormRenderer.tsx: hien canh bao amber khi
--     value < min hoac > max; validateRequiredFields() tra ve "ngoai khoang hop ly"
--   - EmrTabPanel.handleBeforeSave() CHAN luu khi validateRequiredFields co loi.
--   => Chi can bo sung min/max (+ plausibleHint) vao DATA template.
--
-- Fix: UPDATE structured_json cua template he thong DIABETES, them min/max cho
--   4 truong so: hba1c, duong_huyet (mmol/L), egfr, acr. Giu nguyen unit
--   (viec thong nhat don vi duong huyet mg/dL vs mmol/L la BM-69, tach rieng).
--
-- Khoang chon rong rai (tranh false positive, chi chan gia tri PHI LY):
--   hba1c      3 - 20 %
--   duong_huyet 1 - 40 mmol/L  (nang ~30-40; -5 hoac 999 se bi chan)
--   egfr       1 - 200 mL/phut/1.73m2
--   acr        0 - 3000 mg/g
--
-- Idempotent: YES - set lai schema co dinh cua template he thong (id co dinh,
--   is_system=1). Khong dung toi content_json (Tiptap doc) hay ban ghi benh an.
-- ============================================================
SET NAMES utf8mb4;

UPDATE diab_his_cli_emr_templates
SET structured_json = CAST('[
  {"key":"ly_do_kham","label":"Lý do khám","type":"textarea","group":"Lý do khám","required":true,"colSpan":2},
  {"key":"tien_su_dtd","label":"Tiền sử đái tháo đường (thời gian mắc, điều trị hiện tại)","type":"textarea","group":"Tiền sử đái tháo đường","colSpan":2},
  {"key":"kham_lam_sang","label":"Khám lâm sàng","type":"textarea","group":"Khám lâm sàng","colSpan":2},
  {"key":"hba1c","label":"HbA1c","type":"number","unit":"%","min":3,"max":20,"plausibleHint":"HbA1c hợp lý khoảng 3–20%","group":"Cận lâm sàng (HbA1c, đường huyết, eGFR, ACR)"},
  {"key":"duong_huyet","label":"Đường huyết","type":"number","unit":"mmol/L","min":1,"max":40,"plausibleHint":"Đường huyết hợp lý khoảng 1–40 mmol/L","group":"Cận lâm sàng (HbA1c, đường huyết, eGFR, ACR)"},
  {"key":"egfr","label":"eGFR","type":"number","unit":"mL/phút/1.73m²","min":1,"max":200,"plausibleHint":"eGFR hợp lý khoảng 1–200","group":"Cận lâm sàng (HbA1c, đường huyết, eGFR, ACR)"},
  {"key":"acr","label":"ACR (Albumin/Creatinin niệu)","type":"number","unit":"mg/g","min":0,"max":3000,"plausibleHint":"ACR hợp lý khoảng 0–3000 mg/g","group":"Cận lâm sàng (HbA1c, đường huyết, eGFR, ACR)"},
  {"key":"chan_doan_bien_chung","label":"Chẩn đoán & biến chứng","type":"textarea","group":"Chẩn đoán & biến chứng","required":true,"colSpan":2},
  {"key":"muc_tieu_dieu_tri","label":"Mục tiêu điều trị & hướng xử trí","type":"textarea","group":"Mục tiêu điều trị & hướng xử trí","colSpan":2}
]' AS JSON),
    updated_at = NOW()
WHERE id = 'aaaaaaaa-0002-0000-0000-000000000002'
  AND is_system = 1;
-- ============================================================
