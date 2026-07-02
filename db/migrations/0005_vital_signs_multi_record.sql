-- ============================================================
-- Migration: 0005_vital_signs_multi_record
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-05-22
-- Story refs: US-SUNS-09, US-N02
-- Idempotent: YES
-- Ghi chú: Hỗ trợ ghi nhiều bộ dấu hiệu sinh tồn trong 1 lượt khám
--   (điều dưỡng đo đầu giờ, bác sĩ đo lại sau xử lý).
-- ============================================================
SET NAMES utf8mb4;

-- Thời điểm ghi nhận dấu hiệu sinh tồn
CALL add_col_if_missing('cli_vital_signs', 'recorded_at',
    'DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT \'Thời điểm điều dưỡng/bác sĩ ghi nhận DHST\'');

-- Người thực hiện đo (có thể là điều dưỡng khác bác sĩ khám)
CALL add_col_if_missing('cli_vital_signs', 'recorded_by',
    'INT NULL COMMENT \'ID nhân viên ghi nhận dấu hiệu sinh tồn\'');

-- Số thứ tự bản ghi trong cùng 1 lượt khám (1=lần đầu, 2=tái đo...)
CALL add_col_if_missing('cli_vital_signs', 'record_sequence',
    'INT NOT NULL DEFAULT 1 COMMENT \'Thứ tự ghi nhận trong cùng 1 encounter (1=lần đầu)\'');

-- Index hỗ trợ truy vấn lịch sử DHST theo encounter, mới nhất trước
CALL add_index_if_missing('cli_vital_signs', 'idx_vs_encounter_recorded',
    '(encounter_id, recorded_at DESC)');
