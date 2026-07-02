-- =====================================================
-- Pro-Diab HIS — Init DB
-- Chay tu dong khi MySQL container khoi dong lan dau
-- (docker-entrypoint-initdb.d)
-- =====================================================

-- Tao database neu chua ton tai
CREATE DATABASE IF NOT EXISTS `prodiab_his`
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_0900_ai_ci;

-- Tao user prodiab neu chua ton tai
CREATE USER IF NOT EXISTS 'prodiab'@'%' IDENTIFIED BY 'prodiab_dev_2026';

-- Cap quyen day du tren prodiab_his
GRANT ALL PRIVILEGES ON `prodiab_his`.* TO 'prodiab'@'%';

-- Cap quyen doc information_schema de check migration
GRANT SELECT ON `information_schema`.* TO 'prodiab'@'%';

FLUSH PRIVILEGES;

-- Thong bao
SELECT 'Database prodiab_his va user prodiab da duoc tao thanh cong.' AS status;
