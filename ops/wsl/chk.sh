#!/usr/bin/env bash
CONN="-h 127.0.0.1 -uprodiab -pprodiab_dev_2026 prodiab_his"
echo "-- admin/user login --"
mysql $CONN -t -e "SELECT id, email, full_name, user_status, is_active FROM diab_his_sec_users WHERE email LIKE '%@prodiab.local' LIMIT 5;" 2>/dev/null
echo "-- counts --"
mysql $CONN -N -e "SELECT CONCAT('roles=',(SELECT COUNT(*) FROM diab_his_sec_roles),' perms=',(SELECT COUNT(*) FROM diab_his_sec_permissions),' user_roles=',(SELECT COUNT(*) FROM diab_his_sec_user_roles),' patients=',(SELECT COUNT(*) FROM diab_his_pat_patients),' tenants=',(SELECT COUNT(*) FROM diab_his_sys_tenants));" 2>/dev/null
echo "-- password_hash prefix (verify bcrypt seeded) --"
mysql $CONN -N -e "SELECT LEFT(password_hash,10) FROM diab_his_sec_users WHERE email='admin@prodiab.local';" 2>/dev/null
