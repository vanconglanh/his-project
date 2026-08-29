-- =============================================================================
-- 9139_reconcile_role_permissions.sql
-- Đồng bộ ma trận phân quyền (role -> permission) với BỘ MÃ mà controller
-- thực sự enforce qua [RequirePermission("...")].
--
-- BỐI CẢNH: Các sprint seed trước gán cho role bộ mã LEGACY (patient.create,
-- vitals.write, cls.result, dispense.create, pharmacy.*, reception.create...)
-- KHÔNG controller nào enforce, đồng thời THIẾU mã mới thật sự bị enforce
-- (patient.write, vital_sign.write, lab_result.write, dispense.perform,
-- payment.collect, warehouse.read, dashboard.read...). Hậu quả: mọi role
-- non-admin bị 403 hàng loạt trên chính màn hình của mình (QC 2026-08-29).
--
-- CÁCH LÀM: REPLACE — xóa toàn bộ mapping của 5 role non-admin rồi insert lại
-- bộ curated CHỈ gồm mã enforced đúng nghiệp vụ từng role. Đồng thời loại bỏ
-- mã legacy chết khỏi JWT (giảm kích thước token, tránh tái phát bug JWT>4KB).
-- Admin KHÔNG đụng tới (bypass qua is_super_admin claim).
--
-- Idempotent: DELETE + INSERT ... SELECT theo code. Chạy lại nhiều lần an toàn.
-- Rollback: chạy lại các seed sprint cũ (không khuyến nghị).
-- =============================================================================

SET @admin  = '00000000-0000-0000-0000-000000000001';
SET @bacsi  = '00000000-0000-0000-0000-000000000002';
SET @letan  = '00000000-0000-0000-0000-000000000003';
SET @duocsi = '00000000-0000-0000-0000-000000000004';
SET @ketoan = '00000000-0000-0000-0000-000000000005';
SET @ktv    = '00000000-0000-0000-0000-000000000006';

-- Xóa mapping cũ của 5 role non-admin (admin giữ nguyên)
DELETE FROM diab_his_sec_role_permissions
 WHERE role_id IN (@bacsi, @letan, @duocsi, @ketoan, @ktv);

-- ---------------------------------------------------------------------------
-- LỄ TÂN: tiếp đón, hồ sơ bệnh nhân, lịch hẹn, hàng đợi, thu ngân đầu vào
-- ---------------------------------------------------------------------------
INSERT IGNORE INTO diab_his_sec_role_permissions (role_id, permission_id)
SELECT @letan, id FROM diab_his_sec_permissions WHERE code IN (
  'patient.read','patient.write',
  'reception.checkin','reception.queue.manage','reception.rooms.read',
  'reception.stats.read','reception.ticket.reassign',
  'appointment.read','appointment.write',
  'encounter.create','encounter.read',
  'room.read','service.read','service_package.read',
  'package.read','package_subscription.read','package_subscription.sell','package_subscription.collect',
  'billing.read','billing.create','billing.print',
  'icd10.read','recall.read','recall.manage',
  'bhyt.read','risk.read','report.read',
  'dashboard.read','notification.read'
);

-- ---------------------------------------------------------------------------
-- BÁC SĨ: khám bệnh, sinh hiệu, chẩn đoán, chỉ định CLS, chốt đợt CLS, kê đơn
-- ---------------------------------------------------------------------------
INSERT IGNORE INTO diab_his_sec_role_permissions (role_id, permission_id)
SELECT @bacsi, id FROM diab_his_sec_permissions WHERE code IN (
  'patient.read',
  'encounter.create','encounter.read','encounter.start','encounter.update',
  'encounter.close','encounter.amend','encounter.amend.read',
  'vital_sign.read','vital_sign.write',
  'lab_order.create','lab_order.read','lab_order.update','lab_order.delete',
  'rad_order.create','rad_order.read','rad_order.update','rad_order.delete',
  'lab_result.read','rad_result.read',
  'cls_round.create','cls_round.read','cls_round.submit','cls_round.pay','cls_round.waive',
  'cls_upload.read',
  'prescription.create','prescription.read','prescription.update','prescription.sign','prescription.cancel',
  'diabetes.assess','risk.read','cdss.read','cdss.override','ai.suggest','ddi.check',
  'icd10.read','drug.read','service.read',
  'emr.read','emr.write','emr.sign','emr.export','emr_template.read','emr_template.write','fhir.read',
  'bhyt.read','billing.read','dtqg.submit',
  'appointment.read','appointment.write','room.read',
  -- widget hàng chờ / phòng trên màn khám: bác sĩ gọi bệnh nhân vào khám + xem phòng
  'reception.queue.manage','reception.rooms.read',
  'recall.read','recall.manage','report.read','report.build',
  'file.upload','file_annotation.read','file_annotation.write','file_annotation.delete',
  'dashboard.read','notification.read'
);

-- ---------------------------------------------------------------------------
-- KỸ THUẬT VIÊN: nhập/xác nhận kết quả XN + CĐHA, upload file CLS
-- ---------------------------------------------------------------------------
INSERT IGNORE INTO diab_his_sec_role_permissions (role_id, permission_id)
SELECT @ktv, id FROM diab_his_sec_permissions WHERE code IN (
  'patient.read','encounter.read',
  'lab_order.read','lab_order.update','rad_order.read','rad_order.update',
  'lab_result.read','lab_result.write','lab_result.verify','lab_result.import',
  'rad_result.read','rad_result.write','rad_result.verify',
  'cls_upload.create','cls_upload.read','cls_upload.delete',
  'cls_round.read',
  'icd10.read',
  'file.upload','file.delete','file_annotation.read','file_annotation.write','file_annotation.delete',
  'dashboard.read','notification.read'
);

-- ---------------------------------------------------------------------------
-- DƯỢC SĨ: cấp phát thuốc, kho dược, danh mục thuốc, nhà cung cấp
-- ---------------------------------------------------------------------------
INSERT IGNORE INTO diab_his_sec_role_permissions (role_id, permission_id)
SELECT @duocsi, id FROM diab_his_sec_permissions WHERE code IN (
  'patient.read','encounter.read','prescription.read',
  'dispense.perform','dispense.queue','dispense.reject','dispense.return',
  'warehouse.read','drug.read','drug.write','drug.import','drug.sync',
  'stock.read','stock.adjust',
  'supplier.read','supplier.write',
  'package.read','service.read',
  'dtqg.submit','report.read','report.build',
  'file_annotation.read',
  'dashboard.read','notification.read'
);

-- ---------------------------------------------------------------------------
-- KẾ TOÁN: thu ngân, hóa đơn, thanh toán, hóa đơn điện tử, BHYT, báo cáo tài chính
-- ---------------------------------------------------------------------------
INSERT IGNORE INTO diab_his_sec_role_permissions (role_id, permission_id)
SELECT @ketoan, id FROM diab_his_sec_permissions WHERE code IN (
  'patient.read','branch.read','branch.cross_view',
  'billing.read','billing.create','billing.update','billing.finalize',
  'billing.void','billing.print','billing.apply_bhyt',
  'payment.read','payment.collect','payment.refund','payment.void','payment_qr.generate',
  'cashier.report','cashier.shift_open','cashier.shift_close','cashier.print_receipt','cashier.debt_view',
  'einvoice.issue','einvoice.read','einvoice.cancel',
  'bhyt.read','bhyt.export','bhyt.submit','bhyt.generate','bhyt.validate','bhyt.reconcile','bhyt.sign',
  'report.read','report.build','report.export',
  'package.read','package_subscription.read','package_subscription.sell',
  'package_subscription.collect','package_subscription.cancel',
  'service.read',
  'dashboard.read','notification.read'
);
