#!/usr/bin/env bash
# Apply dump + migrations + seeds vao prodiab_his (MySQL trong WSL). Chay: wsl -u root.
set -uo pipefail
DB=prodiab_his
BASE=/mnt/c/claude-code/pro-diab-his/db
CONN="-h 127.0.0.1 -P 3306 -uprodiab -pprodiab_dev_2026"
LOG=/tmp/apply-errors.log; : > "$LOG"
n=0

apply() {
  local f="$1"
  n=$((n+1))
  { echo "SET FOREIGN_KEY_CHECKS=0; SET UNIQUE_CHECKS=0;"; cat "$f"; } \
    | mysql $CONN --force "$DB" 2>>"$LOG"
}

echo "=== A. dump legacy (structure+data) ==="
for f in "$BASE"/diab_his_*.sql; do apply "$f"; done
echo "applied dump: check"

echo "=== B. migrations (sorted) ==="
for f in $(ls "$BASE"/migrations/*.sql | sort); do apply "$f"; done

echo "=== C. seeds ==="
for f in "$BASE"/seeds/*.sql; do apply "$f"; done

echo "TOTAL_FILES_APPLIED=$n"
echo "=== verify: table count ==="
mysql $CONN "$DB" -e "SELECT COUNT(*) AS total_tables FROM information_schema.tables WHERE table_schema='$DB';" 2>&1 | grep -v "Using a password"
echo "=== verify: sec_users (login) ==="
mysql $CONN "$DB" -e "SELECT id, COALESCE(username,'-') u, COALESCE(email,'-') e, COALESCE(status,'-') st FROM diab_his_sec_users LIMIT 10;" 2>&1 | grep -v "Using a password"
echo "=== verify: key canonical tables ==="
mysql $CONN "$DB" -e "SELECT table_name FROM information_schema.tables WHERE table_schema='$DB' AND table_name IN ('diab_his_pat_patients','diab_his_enc_encounters','diab_his_pha_prescriptions','diab_his_sec_roles','diab_his_sec_permissions','diab_his_sys_tenants') ORDER BY table_name;" 2>&1 | grep -v "Using a password"
echo "=== ERROR LOG (unique, top 30) ==="
grep -vi "Using a password" "$LOG" | sort | uniq -c | sort -rn | head -30
echo APPLY_DONE
