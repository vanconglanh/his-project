#!/usr/bin/env bash
# Vong lap giu admin@prodiab.local luon ACTIVE trong luc E2E chay
# (thay cho 'docker exec prodiab-mysql' ma crud-actions.spec.ts dung — moi truong nay khong co docker).
CONN="-h 127.0.0.1 -uprodiab -pprodiab_dev_2026 prodiab_his"
for i in $(seq 1 900); do
  mysql $CONN -e "UPDATE diab_his_sec_users SET user_status='ACTIVE', is_active=1, failed_login_count=0, locked_until=NULL, deleted_at=NULL WHERE email='admin@prodiab.local';" 2>/dev/null
  sleep 2
done
