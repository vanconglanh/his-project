-- =============================================================================
-- 9146_rbac_sod_revocations.sql
--
-- F/Đợt 2 — THU HỒI QUYỀN theo nguyên tắc least-privilege / segregation of duties.
-- Căn cứ: docs/prd/rbac-doi-chieu-chuan-20260829.md (Phần B của 9141, trước đây bị comment).
--
-- BỐI CẢNH QUYẾT ĐỊNH (2026-08-29):
--   Không liên hệ được chủ phòng khám (BO) trong phiên này. Theo yêu cầu điều phối,
--   TỰ QUYẾT theo nguyên tắc chuẩn (least-privilege + SoD của TT 13/2025/TT-BYT,
--   Luật KCB 2023 Đ.69) và GHI RÕ GIẢ ĐỊNH để BO review lại sau.
--
-- GIẢ ĐỊNH ĐÃ DÙNG (BO cần xác nhận):
--   GĐ-1: Bác sĩ KHÔNG trực tiếp thu tiền / miễn phí CLS tại chỗ. Việc thu tiền &
--         miễn phí CLS thuộc kế toán (đã được cấp cls_round.pay/waive ở 9141 phần A).
--         => Thu hồi cls_round.pay + cls_round.waive khỏi bac_si.
--         Nếu SAI (bác sĩ có thu tiền CLS tại chỗ): re-grant bằng cách chạy lại
--         khối A1-tương-đương cho @bacsi, hoặc rollback ở cuối file.
--   GĐ-2: Phòng khám có tách vai trò kiểm kê/điều chỉnh tồn khỏi người nhập hàng.
--         => Thu hồi stock.adjust khỏi duoc_si (dược sĩ vẫn giữ drug.import,
--         supplier.write, stock.read). Nếu phòng khám CHỈ có 1 dược sĩ kiêm kiểm kê:
--         re-grant stock.adjust cho duoc_si VÀ bắt buộc backend enforce field
--         `reason` + audit Severity=WARN cho mọi lần adjust (xem GHI CHÚ cuối file).
--   GĐ-3: Dược sĩ không có need-to-know dữ liệu PII/chẩn đoán trong report engine.
--         => Thu hồi report.build khỏi duoc_si (giữ report.read cho báo cáo kho dựng sẵn).
--         Đây là giảm thiểu tạm; fix gốc là mask PII mặc định trong report engine
--         (P1-04, xử lý ở đợt code riêng).
--
-- Idempotent: DELETE theo code, chạy lại nhiều lần an toàn.
-- =============================================================================

SET @bacsi  = '00000000-0000-0000-0000-000000000002';
SET @duocsi = '00000000-0000-0000-0000-000000000004';

-- GĐ-1: Thu hồi quyền chốt thanh toán + miễn phí CLS khỏi BÁC SĨ ---------------
DELETE rp FROM diab_his_sec_role_permissions rp
  JOIN diab_his_sec_permissions p ON p.id = rp.permission_id
 WHERE rp.role_id = @bacsi AND p.code IN ('cls_round.pay', 'cls_round.waive');

-- GĐ-2: Thu hồi điều chỉnh tồn kho khỏi DƯỢC SĨ (SoD kho dược) -----------------
DELETE rp FROM diab_his_sec_role_permissions rp
  JOIN diab_his_sec_permissions p ON p.id = rp.permission_id
 WHERE rp.role_id = @duocsi AND p.code = 'stock.adjust';

-- GĐ-3: Thu hồi report.build khỏi DƯỢC SĨ (need-to-know PII) -------------------
DELETE rp FROM diab_his_sec_role_permissions rp
  JOIN diab_his_sec_permissions p ON p.id = rp.permission_id
 WHERE rp.role_id = @duocsi AND p.code = 'report.build';

-- =============================================================================
-- ROLLBACK THỦ CÔNG (nếu BO xác nhận giả định sai) ----------------------------
-- INSERT IGNORE INTO diab_his_sec_role_permissions (role_id, permission_id)
--   SELECT @bacsi, id FROM diab_his_sec_permissions WHERE code IN ('cls_round.pay','cls_round.waive');
-- INSERT IGNORE INTO diab_his_sec_role_permissions (role_id, permission_id)
--   SELECT @duocsi, id FROM diab_his_sec_permissions WHERE code IN ('stock.adjust','report.build');
--
-- GHI CHÚ (GĐ-2 phương án 1-dược-sĩ): nếu re-grant stock.adjust, yêu cầu backend
--   StockAdjustCommandHandler bắt buộc field `reason` non-empty + ghi audit
--   Severity=WARN. Chưa enforce trong migration này (thuộc code, không phải SQL).
-- =============================================================================
