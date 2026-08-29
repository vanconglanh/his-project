-- =============================================================================
-- 9153_rbac_p2_clinical_write_permission.sql
--
-- F/Đợt 3 — RBAC Đợt 3 (P0-01, P1-03, P2-06, P2-07).
-- Căn cứ: docs/prd/rbac-doi-chieu-chuan-20260829.md, phần C1 của
--         db/migrations/9141_rbac_standard_alignment.sql (trước đây bị comment).
--
-- Nội dung migration này CHỈ phần P2-06 (mã quyền mới cần SQL):
--   - P0-01 (audit VIEW): không cần thay đổi schema — bảng
--     diab_his_sec_audit_logs (entity AuditLog) đã có đủ cột
--     (action/resource_type/resource_id/user_id/tenant_id/branch_id/ip_address...).
--     Chỉ cần thêm code (AuditAction.View + ghi audit ở các query handler đọc
--     chi tiết 1 hồ sơ). Xem PR backend liên quan.
--   - P1-03 (chặn self-verify CLS): thuần code, không cần SQL.
--   - P2-07 (endpoint doctors/lookup hẹp): dùng lại permission có sẵn
--     'appointment.read' (le_tan + bac_si đã có từ 9139) — không cần SQL.
--
-- Idempotent: INSERT IGNORE theo code, chạy lại nhiều lần an toàn.
-- =============================================================================

SET @bacsi = '00000000-0000-0000-0000-000000000002';
SET @ktv   = '00000000-0000-0000-0000-000000000006';

-- ---------------------------------------------------------------------------
-- P2-06. Tách quyền sửa dữ liệu LÂM SÀNG (dị ứng thuốc) khỏi patient.write
--        (hành chính). Backend đã đổi PatientsController:
--        POST/DELETE /patients/{id}/allergies -> patient.clinical.write.
--        le_tan CHỈ có patient.write -> mất quyền sửa dị ứng một cách tự
--        nhiên (không cần DELETE thu hồi riêng trong migration này).
-- ---------------------------------------------------------------------------
-- Schema thực tế: diab_his_sec_permissions(id, code, resource, action, description, created_at)
INSERT IGNORE INTO diab_his_sec_permissions (id, code, resource, action, description, created_at)
VALUES (UUID(), 'patient.clinical.write', 'patient', 'clinical.write',
        'Cap nhat du lieu lam sang (di ung, tien su benh) - chi danh cho nguoi hanh nghe', NOW());

INSERT IGNORE INTO diab_his_sec_role_permissions (role_id, permission_id)
SELECT r.rid, p.id FROM diab_his_sec_permissions p
  JOIN (SELECT @bacsi AS rid UNION ALL SELECT @ktv) r
 WHERE p.code = 'patient.clinical.write';

-- =============================================================================
-- GHI CHÚ (P2-07): endpoint GET /api/v1/doctors/lookup mới được bảo vệ bằng
-- permission 'appointment.read' đã tồn tại (le_tan + bac_si đều có sẵn qua
-- 9139). MỤC TIÊU dài hạn: sau khi FE chuyển hẳn sang endpoint này, chạy
-- DELETE user.read khỏi le_tan/bac_si (phần B5 của 9141) — KHÔNG chạy ở đây.
-- =============================================================================
