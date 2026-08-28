-- ============================================================
-- Migration: 9082_seed_default_branch
-- Muc dich: moi tenant hien co duoc 1 branch mac dinh (code = 'MAIN'),
--   copy cskcb_code / address / phone / email tu diab_his_sys_tenants.
-- Idempotent: YES (NOT EXISTS theo tenant_id + code)
-- ============================================================
SET NAMES utf8mb4;

INSERT INTO `diab_his_sys_branches`
    (`tenant_id`, `clinic_id`, `code`, `name`, `cskcb_code`, `address`, `phone`, `email`,
     `is_active`, `is_default`, `sort_order`, `created_at`, `updated_at`)
SELECT t.`id`,
       NULL,
       'MAIN',
       COALESCE(t.`name`, CONCAT('Chi nhanh chinh #', t.`id`)),
       t.`cskcb_code`,
       t.`address`,
       t.`phone`,
       t.`email`,
       1, 1, 0, NOW(), NOW()
  FROM `diab_his_sys_tenants` t
 WHERE t.`deleted_at` IS NULL
   AND NOT EXISTS (
        SELECT 1 FROM `diab_his_sys_branches` b
         WHERE b.`tenant_id` = t.`id` AND b.`code` = 'MAIN'
   );

-- Neu tenant da co branch tu 9006 nhung chua co branch nao is_default=1
-- -> nang branch cu nhat len lam mac dinh.
UPDATE `diab_his_sys_branches` b
  JOIN (
        SELECT `tenant_id`, MIN(`id`) AS min_id
          FROM `diab_his_sys_branches`
         WHERE `deleted_at` IS NULL
         GROUP BY `tenant_id`
        HAVING SUM(`is_default`) = 0
  ) x ON x.min_id = b.`id`
   SET b.`is_default` = 1;
