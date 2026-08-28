-- ============================================================
-- Migration: 9083_create_user_branches
-- Muc dich: bang noi N-N user <-> branch + cot branch_id mac dinh tren sec_users
-- Idempotent: YES
-- ============================================================
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS `diab_his_sec_user_branches` (
    `id`          CHAR(36)   NOT NULL,
    `tenant_id`   INT        NOT NULL,
    `user_id`     CHAR(36)   NOT NULL COMMENT 'FK -> diab_his_sec_users.id',
    `branch_id`   INT        NOT NULL COMMENT 'FK -> diab_his_sys_branches.id',
    `is_primary`  TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'Chi nhanh chinh cua user',
    `created_at`  DATETIME   NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `created_by`  CHAR(36)   NULL,
    `updated_at`  DATETIME   NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `updated_by`  CHAR(36)   NULL,
    `deleted_at`  DATETIME   NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_user_branch` (`user_id`, `branch_id`),
    INDEX `idx_ub_branch` (`tenant_id`, `branch_id`),
    INDEX `idx_ub_user`   (`tenant_id`, `user_id`, `is_primary`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
  COMMENT='Phan cong nhan su vao chi nhanh (N-N)';

-- Cot branch mac dinh tren sec_users
CALL add_branch_col('diab_his_sec_users');

-- Gan toan bo user hien co vao branch mac dinh cua tenant
INSERT INTO `diab_his_sec_user_branches`
    (`id`, `tenant_id`, `user_id`, `branch_id`, `is_primary`, `created_at`, `updated_at`)
SELECT UUID(), u.`tenant_id`, u.`id`, b.`id`, 1, NOW(), NOW()
  FROM `diab_his_sec_users` u
  JOIN `diab_his_sys_branches` b
    ON b.`tenant_id` = u.`tenant_id` AND b.`is_default` = 1 AND b.`deleted_at` IS NULL
 WHERE u.`deleted_at` IS NULL
   AND NOT EXISTS (
        SELECT 1 FROM `diab_his_sec_user_branches` ub
         WHERE ub.`user_id` = u.`id` AND ub.`branch_id` = b.`id`
   );
