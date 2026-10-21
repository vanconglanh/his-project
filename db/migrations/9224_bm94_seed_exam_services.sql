-- ============================================================
-- Migration: 9224_bm94_seed_exam_services
-- Mục đích: BM-94 (HIS-48) - "Loại khám" chọn dịch vụ khám có giá.
--   1) Thêm cột pat_patients.exam_service_id CHAR(36) NULL (FK mềm -> diab_his_bil_services.id)
--   2) Seed 2 dịch vụ mới cho MỌI tenant: KBTH "Khám thường" 350.000đ, KBCG "Khám chuyên gia" 700.000đ
--      (KHÔNG đụng KB001/KB002/KB003). vat_rate lấy theo KB001 của tenant (mặc định 0).
-- Idempotent: YES
-- Lưu ý: chưa tự đưa dịch vụ khám vào hoá đơn khi check-in (cần hoàn thiện sau).
-- ============================================================
SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS add_col_if_missing_9224;
DELIMITER $$
CREATE PROCEDURE add_col_if_missing_9224(IN p_table VARCHAR(100), IN p_col VARCHAR(100), IN p_def VARCHAR(300))
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = p_table AND COLUMN_NAME = p_col
    ) THEN
        SET @sql = CONCAT('ALTER TABLE `', p_table, '` ADD COLUMN `', p_col, '` ', p_def);
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
END$$
DELIMITER ;

CALL add_col_if_missing_9224('pat_patients', 'exam_service_id', 'CHAR(36) NULL COMMENT "Dich vu kham da chon (diab_his_bil_services.id)"');

DROP PROCEDURE IF EXISTS add_col_if_missing_9224;

-- Seed dịch vụ khám (mỗi tenant, theo code)
INSERT INTO diab_his_bil_services (id, tenant_id, code, name, category, price, vat_rate, is_active)
SELECT UUID(), t.id, 'KBTH', 'Khám thường', 'EXAM', 350000,
       COALESCE((SELECT s.vat_rate FROM diab_his_bil_services s
                 WHERE s.tenant_id = t.id AND s.code = 'KB001' LIMIT 1), 0), 1
FROM diab_his_sys_tenants t
WHERE NOT EXISTS (SELECT 1 FROM diab_his_bil_services x WHERE x.tenant_id = t.id AND x.code = 'KBTH');

INSERT INTO diab_his_bil_services (id, tenant_id, code, name, category, price, vat_rate, is_active)
SELECT UUID(), t.id, 'KBCG', 'Khám chuyên gia', 'EXAM', 700000,
       COALESCE((SELECT s.vat_rate FROM diab_his_bil_services s
                 WHERE s.tenant_id = t.id AND s.code = 'KB001' LIMIT 1), 0), 1
FROM diab_his_sys_tenants t
WHERE NOT EXISTS (SELECT 1 FROM diab_his_bil_services x WHERE x.tenant_id = t.id AND x.code = 'KBCG');

-- Rollback:
-- DELETE FROM diab_his_bil_services WHERE code IN ('KBTH','KBCG');
-- ALTER TABLE pat_patients DROP COLUMN exam_service_id;
