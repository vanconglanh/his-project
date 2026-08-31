-- ============================================================================
-- 9171_drop_legacy_lab_rad_orders.sql
--
-- C (Nợ kỹ thuật schema) — BƯỚC 3 (cuối): DROP 2 bảng chết chỉ định CLS legacy.
--   diab_his_lab_orders / diab_his_rad_orders (họ 9004, legacy).
--
-- Tiền đề đã hoàn tất:
--   - 9148: đã BACKUP + COPY toàn bộ order legacy sang họ CLI (giữ nguyên id),
--           verify LIVE 0 dòng orphan.
--   - commit c09b5e4: đã rewrite 30 tham chiếu code (report/exporter/handler/EF)
--           từ *_orders -> cli_*_orders (build + 747 unit test xanh).
--   - Verify lại trên DB thật ngay trước migration này (2026-08-29):
--       * SELECT ... lab_results  LEFT JOIN cli_lab_orders  -> 0 orphan
--       * SELECT ... rad_results  LEFT JOIN cli_rad_orders  -> 0 orphan
--       * Cột cli_rad_orders khớp đủ modality/body_part/contrast/procedure_code/
--         procedure_name/priority/status/ordered_at/ordered_by/note (RadOrderConfiguration)
--       * diab_his_rad_results = 0 dòng
--   => An toàn để DROP.
--
-- FK: diab_his_rad_results.fk_rad_results_order còn trỏ tới bảng chết rad_orders,
--     phải DROP FK trước, re-point sang cli_rad_orders cho toàn vẹn dữ liệu.
--
-- Idempotent: kiểm information_schema trước mọi thao tác + DROP TABLE IF EXISTS.
-- ============================================================================

-- --- 1) Bỏ FK cũ trỏ tới bảng chết rad_orders (nếu còn) ----------------------
SET @db := DATABASE();

SET @fk_exists := (
  SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
  WHERE CONSTRAINT_SCHEMA = @db
    AND TABLE_NAME = 'diab_his_rad_results'
    AND CONSTRAINT_NAME = 'fk_rad_results_order'
    AND CONSTRAINT_TYPE = 'FOREIGN KEY'
);
SET @sql := IF(@fk_exists > 0,
  'ALTER TABLE diab_his_rad_results DROP FOREIGN KEY fk_rad_results_order',
  'SELECT "fk_rad_results_order khong ton tai, bo qua"');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- --- 2) Re-point FK sang cli_rad_orders (nếu chưa có FK nào tren order_id) ----
-- Chi re-add khi bang cli_rad_orders ton tai va chua co FK ten fk_rad_results_order_cli.
SET @cli_exists := (
  SELECT COUNT(*) FROM information_schema.TABLES
  WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'diab_his_cli_rad_orders'
);
SET @fk_new_exists := (
  SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
  WHERE CONSTRAINT_SCHEMA = @db
    AND TABLE_NAME = 'diab_his_rad_results'
    AND CONSTRAINT_NAME = 'fk_rad_results_order_cli'
    AND CONSTRAINT_TYPE = 'FOREIGN KEY'
);
SET @sql := IF(@cli_exists > 0 AND @fk_new_exists = 0,
  'ALTER TABLE diab_his_rad_results
     ADD CONSTRAINT fk_rad_results_order_cli
     FOREIGN KEY (order_id) REFERENCES diab_his_cli_rad_orders(id)',
  'SELECT "bo qua re-point FK (cli chua co hoac FK da ton tai)"');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- --- 2b) Bỏ FK cũ phía lab_results trỏ tới bảng chết lab_orders (nếu còn) ------
-- BUG: truoc day chi xu ly phia rad_results, quen fk_lab_results_order tren
-- diab_his_lab_results -> DROP TABLE diab_his_lab_orders bi FK chan (loi 3730).
SET @fk_lab_exists := (
  SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
  WHERE CONSTRAINT_SCHEMA = @db
    AND TABLE_NAME = 'diab_his_lab_results'
    AND CONSTRAINT_NAME = 'fk_lab_results_order'
    AND CONSTRAINT_TYPE = 'FOREIGN KEY'
);
SET @sql := IF(@fk_lab_exists > 0,
  'ALTER TABLE diab_his_lab_results DROP FOREIGN KEY fk_lab_results_order',
  'SELECT "fk_lab_results_order khong ton tai, bo qua"');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Re-point FK sang cli_lab_orders (nếu bảng live tồn tại và chưa có FK mới) -----
SET @cli_lab_exists := (
  SELECT COUNT(*) FROM information_schema.TABLES
  WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'diab_his_cli_lab_orders'
);
SET @fk_lab_new_exists := (
  SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
  WHERE CONSTRAINT_SCHEMA = @db
    AND TABLE_NAME = 'diab_his_lab_results'
    AND CONSTRAINT_NAME = 'fk_lab_results_order_cli'
    AND CONSTRAINT_TYPE = 'FOREIGN KEY'
);
SET @sql := IF(@cli_lab_exists > 0 AND @fk_lab_new_exists = 0,
  'ALTER TABLE diab_his_lab_results
     ADD CONSTRAINT fk_lab_results_order_cli
     FOREIGN KEY (order_id) REFERENCES diab_his_cli_lab_orders(id)',
  'SELECT "bo qua re-point FK lab (cli chua co hoac FK da ton tai)"');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- --- 3) DROP 2 bảng chết ------------------------------------------------------
DROP TABLE IF EXISTS diab_his_lab_orders;
DROP TABLE IF EXISTS diab_his_rad_orders;
