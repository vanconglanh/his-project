-- BM-82: Hang cho phat thuoc 500 (Unknown column 'p.is_bhyt').
-- Nguyen nhan: fix BM-82 truoc do da them p.is_bhyt vao query hang cho phat thuoc
-- (DispensingHandlers.cs GetDispenseQueueQuery) nhung CHUA co migration tao cot ->
-- khi co don thuoc da ky, GET /api/v1/pharmacy/dispense/queue nem 500 -> Duoc si
-- khong thay/khong phat duoc don. Bo sung cot is_bhyt (mac dinh 0 = khong BHYT).
-- Idempotent (MySQL 8.0.23+ khong ho tro ADD COLUMN IF NOT EXISTS).

DROP PROCEDURE IF EXISTS add_col_if_missing_9215;
DELIMITER $$
CREATE PROCEDURE add_col_if_missing_9215()
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = 'diab_his_pha_prescriptions'
      AND column_name = 'is_bhyt'
  ) THEN
    -- Dung TINYINT (KHONG phai TINYINT(1)): MySqlConnector map tinyint(1) -> bool
    -- (TreatTinyAsBoolean mac dinh), nhung code GetDispenseQueueHandler so sanh r.is_bhyt == 1
    -- (int) -> RuntimeBinderException. TINYINT thuong map sang so nguyen, so sanh == 1 hop le.
    ALTER TABLE `diab_his_pha_prescriptions`
      ADD COLUMN `is_bhyt` TINYINT NOT NULL DEFAULT 0
      COMMENT 'Don thuoc BHYT hay khong (0=khong, 1=co)';
  END IF;
END$$
DELIMITER ;

CALL add_col_if_missing_9215();
DROP PROCEDURE IF EXISTS add_col_if_missing_9215;
