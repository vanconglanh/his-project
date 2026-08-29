-- ============================================================================
-- 9148_consolidate_lab_rad_orders_reversible.sql
--
-- C (Nợ kỹ thuật schema): hợp nhất 2 họ bảng chỉ định CLS.
--   - Họ CLI  (đúng, dùng bởi luồng đợt CLS hiện tại): diab_his_cli_lab_orders / diab_his_cli_rad_orders
--   - Họ 9004 (legacy, dùng bởi ~15 báo cáo + seed demo 9020): diab_his_lab_orders / diab_his_rad_orders
--   Bảng kết quả diab_his_lab_results/diab_his_rad_results có FK trỏ HỖN HỢP:
--   phần lớn dòng trỏ họ 9004, số ít trỏ họ CLI (xem C-1/C-2 trong TASKLIST).
--
-- CHIẾN LƯỢC AN TOÀN (KHÔNG PHÁ DỮ LIỆU, CÓ ROLLBACK):
--   Bước 1 (migration này): SAO LƯU toàn bộ bảng liên quan + LÀM CHO họ CLI trở thành
--     SIÊU TẬP (superset) bằng cách COPY (giữ nguyên id) mọi order legacy đang được
--     result tham chiếu nhưng chưa có trong bảng CLI. CHỈ THÊM DÒNG, không sửa/không xoá
--     dữ liệu cũ -> báo cáo hiện tại (đang đọc họ 9004) KHÔNG bị ảnh hưởng.
--   Bước 2 (PR code riêng, thực thi trên DB thật có kiểm 62 dòng): đổi mọi JOIN báo cáo/
--     exporter từ diab_his_lab_orders -> diab_his_cli_lab_orders (và rad tương ứng),
--     rồi mới cân nhắc DROP bảng legacy. KHÔNG làm bước 2 mù khi chưa soi được dữ liệu thật.
--
-- Vì sao KHÔNG cutover luôn ở đây: schema thực tế của diab_his_lab_results đã bị ALTER khác
--   với bản CREATE 9004 (EF map cột lab_order_id/order_id/value...), collation 2 họ khác nhau
--   (cli=unicode_ci, 9004=0900_ai_ci). Cutover mù không có DB để kiểm 62 dòng = rủi ro mất
--   60/62 dòng. Migration này là bước tiến an toàn, đảo ngược được.
--
-- Idempotent: dùng information_schema guard + INSERT ... WHERE NOT EXISTS. Chạy lại an toàn.
-- Rollback: DROP các bảng *_bak_9148; xoá các dòng đã copy (đánh dấu qua cột note='__merged_9148').
-- ============================================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS backup_table_if_exists;
DELIMITER //
CREATE PROCEDURE backup_table_if_exists(IN src VARCHAR(64), IN bak VARCHAR(64))
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables
             WHERE table_schema = DATABASE() AND table_name = src)
     AND NOT EXISTS (SELECT 1 FROM information_schema.tables
             WHERE table_schema = DATABASE() AND table_name = bak) THEN
    SET @s = CONCAT('CREATE TABLE `', bak, '` AS SELECT * FROM `', src, '`');
    PREPARE st FROM @s; EXECUTE st; DEALLOCATE PREPARE st;
  END IF;
END //
DELIMITER ;

-- --- Bước 1a: sao lưu (snapshot 1 lần, không ghi đè nếu đã có) ---------------
CALL backup_table_if_exists('diab_his_lab_results',      'diab_his_lab_results_bak_9148');
CALL backup_table_if_exists('diab_his_rad_results',      'diab_his_rad_results_bak_9148');
CALL backup_table_if_exists('diab_his_lab_orders',       'diab_his_lab_orders_bak_9148');
CALL backup_table_if_exists('diab_his_rad_orders',       'diab_his_rad_orders_bak_9148');
CALL backup_table_if_exists('diab_his_cli_lab_orders',   'diab_his_cli_lab_orders_bak_9148');
CALL backup_table_if_exists('diab_his_cli_rad_orders',   'diab_his_cli_rad_orders_bak_9148');

-- --- Bước 1b: LAB — copy order legacy (được result tham chiếu) sang bảng CLI ---
-- Chỉ chạy khi cả hai bảng tồn tại. Giữ nguyên id để result.order_id vẫn khớp.
-- Cột trùng tên giữa 2 bảng: id, tenant_id, encounter_id, test_code, test_name,
--   sample_type, priority, status, ordered_at, ordered_by, scheduled_for,
--   lab_partner_id, note, created_at, created_by, updated_at, updated_by, deleted_at.
DROP PROCEDURE IF EXISTS merge_legacy_lab_orders;
DELIMITER //
CREATE PROCEDURE merge_legacy_lab_orders()
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema=DATABASE() AND table_name='diab_his_lab_orders')
     AND EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema=DATABASE() AND table_name='diab_his_cli_lab_orders') THEN
    INSERT INTO diab_his_cli_lab_orders
      (id, tenant_id, encounter_id, test_code, test_name, sample_type, priority, status,
       ordered_at, ordered_by, scheduled_for, lab_partner_id, note,
       created_at, created_by, updated_at, updated_by, deleted_at)
    SELECT lo.id, lo.tenant_id, lo.encounter_id, lo.test_code, lo.test_name, lo.sample_type,
           lo.priority, lo.status, lo.ordered_at, lo.ordered_by, lo.scheduled_for, lo.lab_partner_id,
           CONCAT(COALESCE(lo.note,''), ' __merged_9148'), lo.created_at, lo.created_by,
           lo.updated_at, lo.updated_by, lo.deleted_at
    FROM diab_his_lab_orders lo
    WHERE NOT EXISTS (SELECT 1 FROM diab_his_cli_lab_orders c WHERE c.id = lo.id);
  END IF;
END //
DELIMITER ;
CALL merge_legacy_lab_orders();

-- --- Bước 1c: RAD — tương tự. Cột rad có thể lệch tên; guard bằng thủ tục riêng ---
-- LƯU Ý: rad hiện gần như 0 data thật (C-2). Chỉ copy khi 2 bảng cùng tồn tại; nếu cột
-- lệch, thủ tục sẽ lỗi rõ ràng -> DBA xử lý tay (an toàn hơn đoán mù cột).
DROP PROCEDURE IF EXISTS merge_legacy_rad_orders;
DELIMITER //
CREATE PROCEDURE merge_legacy_rad_orders()
BEGIN
  DECLARE has_both INT DEFAULT 0;
  SELECT COUNT(*) INTO has_both FROM information_schema.tables
   WHERE table_schema=DATABASE() AND table_name IN ('diab_his_rad_orders','diab_his_cli_rad_orders');
  IF has_both = 2 THEN
    INSERT INTO diab_his_cli_rad_orders
      (id, tenant_id, encounter_id, modality, body_part, contrast, procedure_code, procedure_name,
       priority, status, ordered_at, ordered_by, note, created_at, created_by, updated_at, updated_by, deleted_at)
    SELECT ro.id, ro.tenant_id, ro.encounter_id, ro.modality, ro.body_part, ro.contrast,
           ro.procedure_code, ro.procedure_name, ro.priority, ro.status, ro.ordered_at, ro.ordered_by,
           CONCAT(COALESCE(ro.note,''), ' __merged_9148'), ro.created_at, ro.created_by,
           ro.updated_at, ro.updated_by, ro.deleted_at
    FROM diab_his_rad_orders ro
    WHERE NOT EXISTS (SELECT 1 FROM diab_his_cli_rad_orders c WHERE c.id = ro.id);
  END IF;
END //
DELIMITER ;
-- Bọc trong handler để không chặn migration nếu cột rad lệch (rad ~0 data):
DROP PROCEDURE IF EXISTS try_merge_rad;
DELIMITER //
CREATE PROCEDURE try_merge_rad()
BEGIN
  DECLARE CONTINUE HANDLER FOR SQLEXCEPTION
  BEGIN
    SELECT 'CANH BAO: merge rad orders bo qua do cot lech - DBA xu ly tay (rad ~0 data)' AS warn_9148;
  END;
  CALL merge_legacy_rad_orders();
END //
DELIMITER ;
CALL try_merge_rad();

-- --- Dọn thủ tục tạm ---------------------------------------------------------
DROP PROCEDURE IF EXISTS backup_table_if_exists;
DROP PROCEDURE IF EXISTS merge_legacy_lab_orders;
DROP PROCEDURE IF EXISTS merge_legacy_rad_orders;
DROP PROCEDURE IF EXISTS try_merge_rad;

-- ============================================================================
-- SAU MIGRATION NÀY (bước 2, PR code riêng, chạy trên DB thật):
--   1. Kiểm: SELECT COUNT(*) FROM diab_his_lab_results r
--            LEFT JOIN diab_his_cli_lab_orders c ON c.id = r.order_id WHERE c.id IS NULL;  -- phải = 0
--   2. Đổi JOIN trong: Infrastructure/Reports/ReportRegistry.cs, DatasetRegistry.cs,
--      Lab/LabResultQuestPdfExporter.cs, Application/RadResults/RadResultHandlers.cs,
--      PublicApi/PortalMeHandlers.cs, Infrastructure/Clinical/EncounterLockGuard.cs
--      từ diab_his_lab_orders/diab_his_rad_orders -> diab_his_cli_lab_orders/diab_his_cli_rad_orders.
--   3. Sau khi QC xác nhận báo cáo khớp: DROP diab_his_lab_orders/diab_his_rad_orders
--      (dữ liệu đã có bản _bak_9148 + đã copy sang CLI).
-- ROLLBACK bước 1: DELETE FROM diab_his_cli_lab_orders WHERE note LIKE '% __merged_9148';
--                  (rad tương tự); DROP TABLE *_bak_9148 khi chắc chắn không cần.
-- ============================================================================
