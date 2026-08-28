-- ============================================================
-- Migration: 9090_create_fil_file_annotations
-- Engine: MySQL 8.0+, InnoDB, utf8mb4
-- Generated: 2026-08-26
-- Story ref: FR-311 [P1] Đính kèm hình ảnh lâm sàng + annotation
-- Idempotent: YES (CREATE TABLE IF NOT EXISTS, INSERT IGNORE)
-- Ghi chú: annotation là layer JSON riêng, KHÔNG sửa file ảnh gốc
--   (non-destructive). Mỗi bản ghi gắn với 1 file (fil_files.id) và
--   ngữ cảnh bệnh nhân/lượt khám (nullable vì có thể chưa gắn encounter).
-- ============================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS `diab_his_fil_file_annotations` (
    `id`               CHAR(36)     NOT NULL DEFAULT (UUID()) COMMENT 'Khóa chính (GUID)',
    `tenant_id`        INT          NOT NULL                   COMMENT 'ID tenant sở hữu bản ghi',
    `file_id`          CHAR(36)     NOT NULL                   COMMENT 'FK -> fil_files.id (ảnh gốc)',
    `patient_id`       CHAR(36)     NULL                       COMMENT 'FK -> pat_patients.id (ngữ cảnh bệnh nhân)',
    `encounter_id`     CHAR(36)     NULL                       COMMENT 'FK -> cli_encounters.id (ngữ cảnh lượt khám)',
    `annotation_data`  JSON         NOT NULL                   COMMENT 'Danh sách shape (rectangle/circle/arrow/text) + toạ độ, màu, ghi chú',
    `version`          INT          NOT NULL DEFAULT 1         COMMENT 'Số phiên bản, tăng dần mỗi lần cập nhật',
    `created_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`       CHAR(36)     NULL,
    `updated_at`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`       CHAR(36)     NULL,
    `deleted_at`       DATETIME     NULL                       COMMENT 'Thời điểm xóa mềm',

    PRIMARY KEY (`id`),
    KEY `idx_fil_file_annotations_tenant_file` (`tenant_id`, `file_id`),
    KEY `idx_fil_file_annotations_patient`     (`tenant_id`, `patient_id`),
    KEY `idx_fil_file_annotations_encounter`   (`tenant_id`, `encounter_id`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Annotation (khoanh vùng/mũi tên/ghi chú) trên ảnh lâm sàng - layer JSON riêng, không sửa ảnh gốc';

-- ============================================================
-- Seed quyền backend gate (RequirePermission) cho annotation
-- ============================================================
INSERT IGNORE INTO `diab_his_sec_permissions` (`id`, `code`, `resource`, `action`, `description`, `created_at`)
SELECT UUID(), t.code, SUBSTRING_INDEX(t.code, '.', 1), SUBSTRING(t.code, LOCATE('.', t.code) + 1), t.code, NOW()
FROM (
    SELECT 'file_annotation.read'  AS code
    UNION ALL SELECT 'file_annotation.write'
    UNION ALL SELECT 'file_annotation.delete'
) AS t;

-- Cấp quyền đọc cho toàn bộ role nội bộ (bác sĩ, điều dưỡng/kỹ thuật viên, dược sĩ, lễ tân, kế toán)
INSERT INTO `diab_his_sec_role_permissions` (`role_id`, `permission_id`)
SELECT r.id, p.id
FROM `diab_his_sec_roles` r
CROSS JOIN `diab_his_sec_permissions` p
WHERE p.code = 'file_annotation.read'
  AND NOT EXISTS (
      SELECT 1 FROM `diab_his_sec_role_permissions` rp
      WHERE rp.role_id = r.id AND rp.permission_id = p.id
  );

-- Cấp quyền tạo/sửa/xóa annotation CHỈ cho admin + bác sĩ + kỹ thuật viên
-- (schema hiện KHÔNG có role "Điều dưỡng" riêng trong diab_his_sec_roles —
--  role gần nhất về nghiệp vụ lâm sàng phụ trợ là ky_thuat_vien; xem ghi chú
--  trong báo cáo triển khai để bổ sung role dieu_duong nếu cần).
INSERT INTO `diab_his_sec_role_permissions` (`role_id`, `permission_id`)
SELECT r.id, p.id
FROM `diab_his_sec_roles` r
CROSS JOIN `diab_his_sec_permissions` p
WHERE r.code IN ('admin', 'bac_si', 'ky_thuat_vien')
  AND p.code IN ('file_annotation.write', 'file_annotation.delete')
  AND NOT EXISTS (
      SELECT 1 FROM `diab_his_sec_role_permissions` rp
      WHERE rp.role_id = r.id AND rp.permission_id = p.id
  );
