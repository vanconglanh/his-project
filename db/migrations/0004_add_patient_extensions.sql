-- ============================================================
-- Migration: 0004_add_patient_extensions
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-22
-- Story refs: US-SUNS-01, US-SUNS-02, US-PT-EXT-01
-- Idempotent: YES
-- ============================================================
SET NAMES utf8mb4;

-- Thêm URL ảnh đại diện bệnh nhân (lễ tân upload qua MinIO)
CALL add_col_if_missing('pat_patients', 'avatar_url', 'VARCHAR(500) NULL COMMENT \'Đường dẫn ảnh đại diện bệnh nhân (MinIO URL)\'');

-- Ghi chú nhanh của lễ tân khi tiếp đón bệnh nhân
CALL add_col_if_missing('pat_patients', 'reception_note', 'TEXT NULL COMMENT \'Ghi chú nhanh của lễ tân khi tiếp đón\'');
