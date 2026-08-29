-- ============================================================================
-- 9154_report_pii_plaintext_permission.sql
-- F/Đợt 3 — P1-04: mã quyền cho phép XEM PII plaintext trong báo cáo.
--   Report engine (GenericReportDataService + Report Builder preview) đã đổi sang
--   MASK PII mặc định; chỉ giải mã khi user có quyền này (super admin bypass sẵn).
--   Theo phần C2 của 9141: KHÔNG cấp cho role thường nào — chỉ super admin.
-- Schema thực tế: diab_his_sec_permissions(id, code, resource, action, description, created_at)
-- Idempotent: INSERT ... WHERE NOT EXISTS.
-- ============================================================================
SET NAMES utf8mb4;

INSERT INTO diab_his_sec_permissions (id, code, resource, action, description, created_at)
SELECT UUID(), 'report.pii_plaintext', 'report', 'pii_plaintext',
       'Xem PII (CCCD/so BHYT/dia chi) dang plaintext trong bao cao - mac dinh chi super admin', NOW()
FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM diab_his_sec_permissions WHERE code = 'report.pii_plaintext');

-- KHONG cap cho role thuong. Super admin da bypass moi permission check (is_super_admin).
-- Neu nghiep vu can 1 role cu the xem plaintext: grant tay ma tren + set descriptor.AllowPiiPlaintext=true.
