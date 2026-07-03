#!/usr/bin/env bash
CONN="-h 127.0.0.1 -uprodiab -pprodiab_dev_2026 prodiab_his"
mysql $CONN -N -e "SELECT GROUP_CONCAT(column_name) FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='diab_his_sec_users' AND column_name IN ('failed_login_count','locked_until','user_status','is_active','deleted_at');" 2>/dev/null
# test the reset update actually works
mysql $CONN -e "UPDATE diab_his_sec_users SET user_status='ACTIVE', is_active=1, failed_login_count=0, locked_until=NULL, deleted_at=NULL WHERE email='admin@prodiab.local';" 2>&1 | grep -v "Using a password" && echo "RESET_UPDATE_OK" || echo "RESET_UPDATE_FAILED"
