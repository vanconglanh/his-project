#!/usr/bin/env bash
# =============================================================================
# sync_incremental.sh
# Đồng bộ gia tăng diab_his → prodiab_his (production pro-his-db, tenant 1).
#
# Mục đích: chạy trước cutover (hoặc bất kỳ lúc nào) để kéo BN/lượt mới từ
#   nguồn diab_his (pro-db-my8) sang đích prodiab_his (pro-his-db).
#   Script idempotent — chạy lại an toàn, không sinh trùng.
#
# Cách chạy:
#   Từ máy local (qua LAN/VPN DiaB):
#     ./docs/migration/sync_incremental.sh
#   Từ server VNG (ssh vng hoặc vng-lan):
#     bash ~/sync_incremental.sh
#
# Yêu cầu:
#   - SSH đến VNG thành công (alias vng hoặc vng-lan; hoặc chạy trực tiếp trên server)
#   - Docker container pro-db-my8 và pro-his-db đang chạy
#   - migrate_legacy_his.py đã có tại /home/stackops/migrate_legacy_his.py
#     (nếu chưa có: scp -P 234 -i KEY docs/migration/migrate_legacy_his.py stackops@192.168.1.4:~/)
#   - fix_prod_data_20261016.sql và fix_prod_data_20261016b.sql tại /home/stackops/
#     (nếu chưa có: scp -P 234 -i KEY docs/migration/*.sql stackops@192.168.1.4:~/)
#
# Biến môi trường (tuỳ chọn ghi đè):
#   REMOTE_HOST   IP server VNG (mặc định: 192.168.1.4)
#   REMOTE_PORT   SSH port (mặc định: 234)
#   SSH_KEY       đường dẫn SSH key (mặc định: D:/diab/_Azure/diab-server.pem)
#   DRY_RUN       set=1 để chỉ xem, không ghi DB (mặc định: 0)
# =============================================================================
set -euo pipefail

REMOTE_HOST="${REMOTE_HOST:-192.168.1.4}"
REMOTE_PORT="${REMOTE_PORT:-234}"
SSH_KEY="${SSH_KEY:-D:/diab/_Azure/diab-server.pem}"
DRY_RUN="${DRY_RUN:-0}"

SSH="ssh -p $REMOTE_PORT -i $SSH_KEY -o StrictHostKeyChecking=no stackops@$REMOTE_HOST"
SCP="scp -P $REMOTE_PORT -i $SSH_KEY -o StrictHostKeyChecking=no"

echo "================================================================"
echo " sync_incremental.sh — $(date '+%Y-%m-%d %H:%M:%S')"
echo " Đích: $REMOTE_HOST, DRY_RUN=$DRY_RUN"
echo "================================================================"

# --- Hàm tiện ích ---
step() { echo; echo ">>> BƯỚC $*"; }
ok()   { echo "    [OK] $*"; }
fail() { echo "    [FAIL] $*"; exit 1; }

# =============================================================================
# BƯỚC 0: Lấy passwords container (không hardcode)
# =============================================================================
step "0: Lấy thông tin container"
SRC_PASS=$($SSH 'docker inspect pro-db-my8 --format "{{range .Config.Env}}{{.}}\n{{end}}"' 2>/dev/null \
           | grep MYSQL_ROOT_PASSWORD | cut -d= -f2)
DST_PASS=$($SSH 'docker inspect pro-his-db --format "{{range .Config.Env}}{{.}}\n{{end}}"' 2>/dev/null \
           | grep MYSQL_ROOT_PASSWORD | cut -d= -f2)
SRC_IP=$($SSH 'docker inspect pro-db-my8 --format "{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}"' 2>/dev/null)
DST_IP=$($SSH 'docker inspect pro-his-db --format "{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}"' 2>/dev/null)

[ -z "$SRC_PASS" ] && fail "Không lấy được password pro-db-my8"
[ -z "$DST_PASS" ] && fail "Không lấy được password pro-his-db"
ok "pro-db-my8 IP=$SRC_IP, pro-his-db IP=$DST_IP"

# =============================================================================
# BƯỚC 1: Backup pro-his-db
# =============================================================================
step "1: Backup pro-his-db"
BACKUP_FILE=$($SSH "
  mkdir -p /home/stackops/backups/pro-his
  TS=\$(date +%Y%m%d_%H%M%S)
  OUT=/home/stackops/backups/pro-his/prodiab_his_\${TS}_pre_sync.sql.gz
  docker exec pro-his-db bash -c \"MYSQL_PWD=$DST_PASS mysqldump -uroot --single-transaction --default-character-set=utf8mb4 prodiab_his\" | gzip > \$OUT
  echo \$OUT
" 2>/dev/null)
ok "Backup: $BACKUP_FILE"

# =============================================================================
# BƯỚC 2: Ghi số đếm TRƯỚC migration
# =============================================================================
step "2: Đếm BN/lượt TRƯỚC"
BEFORE=$($SSH "docker exec pro-his-db bash -c \"MYSQL_PWD=$DST_PASS mysql -uroot --default-character-set=utf8mb4 prodiab_his -sNe \\\"
  SELECT CONCAT('pat=',COUNT(*)) FROM diab_his_pat_patients WHERE tenant_id=1 AND patient_source='LEG-IMPORT-2026';
  SELECT CONCAT('enc=',COUNT(*)) FROM diab_his_enc_encounters WHERE tenant_id=1 AND encounter_no LIKE 'LEG-%';
\\\"\"" 2>/dev/null | tr '\n' ' ')
ok "Trước: $BEFORE"

# =============================================================================
# BƯỚC 3: Tạo user migrator tạm (chỉ đọc src, ghi dst)
# =============================================================================
step "3: Tạo migrator user tạm"
MIG_PASS="mig_sync_$(date +%s)"

$SSH "
docker exec pro-db-my8 bash -c \"MYSQL_PWD=$SRC_PASS mysql -uroot -e \\\"
  CREATE USER IF NOT EXISTS 'migrator'@'%' IDENTIFIED BY '$MIG_PASS';
  GRANT SELECT ON diab_his.* TO 'migrator'@'%';
  FLUSH PRIVILEGES;\\\"\"
docker exec pro-his-db bash -c \"MYSQL_PWD=$DST_PASS mysql -uroot -e \\\"
  CREATE USER IF NOT EXISTS 'migrator'@'%' IDENTIFIED BY '$MIG_PASS';
  GRANT ALL PRIVILEGES ON prodiab_his.* TO 'migrator'@'%';
  FLUSH PRIVILEGES;\\\"\"
" 2>/dev/null
ok "migrator user tạo OK"

# Cleanup on exit
cleanup() {
  $SSH "
    docker exec pro-db-my8 bash -c \"MYSQL_PWD=$SRC_PASS mysql -uroot -e \\\"DROP USER IF EXISTS 'migrator'@'%'; FLUSH PRIVILEGES;\\\"\" 2>/dev/null || true
    docker exec pro-his-db bash -c \"MYSQL_PWD=$DST_PASS mysql -uroot -e \\\"DROP USER IF EXISTS 'migrator'@'%'; FLUSH PRIVILEGES;\\\"\" 2>/dev/null || true
    docker stop mig-runner 2>/dev/null && docker rm mig-runner 2>/dev/null || true
  " 2>/dev/null || true
  ok "Cleanup OK"
}
trap cleanup EXIT

# =============================================================================
# BƯỚC 4: Khởi container mig-runner nối cả 2 network
# =============================================================================
step "4: Khởi container mig-runner"
$SSH "
  docker stop mig-runner 2>/dev/null; docker rm mig-runner 2>/dev/null; true
  docker run --rm --network pro-net --name mig-runner -d python:3.11-slim sleep 600
  docker network connect pro-his-net mig-runner
  docker exec mig-runner mkdir -p /app
  docker cp /home/stackops/migrate_legacy_his.py mig-runner:/app/
  docker exec mig-runner pip install pymysql -q
" 2>/dev/null
ok "mig-runner khởi động OK"

# =============================================================================
# BƯỚC 5: Chạy migrate (dry-run hoặc real)
# =============================================================================
step "5: Chạy migrate_legacy_his.py${DRY_RUN:+ --dry-run}"
DRY_FLAG=""
[ "$DRY_RUN" = "1" ] && DRY_FLAG="--dry-run"

$SSH "
docker exec \
  -e HIS_SRC_HOST=$SRC_IP -e HIS_SRC_PORT=3306 \
  -e HIS_SRC_USER=migrator -e HIS_SRC_PASS=$MIG_PASS \
  -e HIS_DST_HOST=$DST_IP -e HIS_DST_PORT=3306 \
  -e HIS_DST_USER=migrator -e HIS_DST_PASS=$MIG_PASS \
  mig-runner python /app/migrate_legacy_his.py $DRY_FLAG 2>&1
" 2>/dev/null
ok "migrate_legacy_his.py hoàn tất"

if [ "$DRY_RUN" = "1" ]; then
  echo "=== DRY-RUN done — không ghi gì. Thoát. ==="
  exit 0
fi

# =============================================================================
# BƯỚC 6: Áp fix SQL (idempotent — an toàn chạy lại)
# =============================================================================
step "6: Áp fix_prod_data_20261016.sql"
$SSH "docker exec -i -e MYSQL_PWD=$DST_PASS pro-his-db mysql -uroot --default-character-set=utf8mb4 prodiab_his" \
  < "$(dirname "$0")/fix_prod_data_20261016.sql" 2>/dev/null || \
  $SSH "docker exec -i -e MYSQL_PWD=$DST_PASS pro-his-db mysql -uroot --default-character-set=utf8mb4 prodiab_his < /home/stackops/fix_prod_data_20261016.sql" 2>/dev/null
ok "fix_prod_data_20261016.sql done"

step "6b: Áp fix_prod_data_20261016b.sql"
$SSH "docker exec -i -e MYSQL_PWD=$DST_PASS pro-his-db mysql -uroot --default-character-set=utf8mb4 prodiab_his" \
  < "$(dirname "$0")/fix_prod_data_20261016b.sql" 2>/dev/null || \
  $SSH "docker exec -i -e MYSQL_PWD=$DST_PASS pro-his-db mysql -uroot --default-character-set=utf8mb4 prodiab_his < /home/stackops/fix_prod_data_20261016b.sql" 2>/dev/null
ok "fix_prod_data_20261016b.sql done"

# =============================================================================
# BƯỚC 7: Blind index backfill
# =============================================================================
step "7: Blind index backfill"
$SSH "docker exec pro-his-backend sh -c 'cd /app && dotnet ProDiabHis.Api.dll backfill-bidx 1' 2>&1 | grep -E 'PiiBackfill|backfill|ERROR'" 2>/dev/null
ok "Backfill done"

# =============================================================================
# BƯỚC 8: Verify
# =============================================================================
step "8: Đếm BN/lượt SAU"
AFTER=$($SSH "docker exec pro-his-db bash -c \"MYSQL_PWD=$DST_PASS mysql -uroot --default-character-set=utf8mb4 prodiab_his -sNe \\\"
  SELECT CONCAT('pat=',COUNT(*)) FROM diab_his_pat_patients WHERE tenant_id=1 AND patient_source='LEG-IMPORT-2026';
  SELECT CONCAT('enc=',COUNT(*)) FROM diab_his_enc_encounters WHERE tenant_id=1 AND encounter_no LIKE 'LEG-%';
  SELECT CONCAT('test_active=',COUNT(*)) FROM diab_his_pat_patients WHERE tenant_id=1 AND deleted_at IS NULL AND UPPER(full_name) LIKE '%TEST%';
  SELECT CONCAT('enc_done_pct=',ROUND(100*SUM(status=\\\"DONE\\\")/COUNT(*),1),'%') FROM diab_his_enc_encounters WHERE tenant_id=1 AND encounter_no LIKE 'LEG-%' AND deleted_at IS NULL;
\\\"\"" 2>/dev/null | tr '\n' ' ')

echo
echo "================================================================"
echo " KẾT QUẢ SYNC"
echo " Trước: $BEFORE"
echo " Sau:   $AFTER"
echo " Nguồn diab_his:"
SRC_COUNTS=$($SSH "docker exec pro-db-my8 bash -c \"MYSQL_PWD=$SRC_PASS mysql -uroot --default-character-set=utf8mb4 diab_his -sNe \\\"
  SELECT CONCAT('src_pat=',COUNT(*)) FROM pat_patients WHERE STATUS=1;
  SELECT CONCAT('src_enc=',COUNT(*)) FROM cli_visits WHERE STATUS=1;
\\\"\"" 2>/dev/null | tr '\n' ' ')
echo " $SRC_COUNTS"
echo " Log backend (5 phút gần nhất):"
$SSH "docker logs pro-his-backend --since=5m 2>&1 | grep -cE 'ERROR|Exception' | xargs echo '   Errors:'" 2>/dev/null || true
echo "================================================================"
echo " DONE — $(date '+%Y-%m-%d %H:%M:%S')"
echo "================================================================"
