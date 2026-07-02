#!/usr/bin/env bash
# backup-nightly.sh — Backup MySQL prodiab_his hàng đêm (nightly)
# Gọi bởi cron 02:00 hàng ngày. Xem ops/backup/crontab.txt
# Yêu cầu: mysqldump, gzip. mc (MinIO client) nếu ENABLE_REMOTE=true
# LF line ending, UTF-8 no BOM
set -euo pipefail

# ---------------------------------------------------------------------------
# Cấu hình — override bằng biến môi trường hoặc .env server
# ---------------------------------------------------------------------------
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-3306}"
DB_NAME="${DB_NAME:-prodiab_his}"
DB_USER="${DB_USER:-root}"
DB_PASS="${DB_PASS:-${MYSQL_ROOT_PASSWORD:-}}"

BACKUP_DIR="${BACKUP_DIR:-/opt/prodiab/backups/db}"
RETENTION_LOCAL_DAYS="${RETENTION_LOCAL_DAYS:-7}"
RETENTION_REMOTE_DAYS="${RETENTION_REMOTE_DAYS:-30}"

# MinIO / S3 — đặt ENABLE_REMOTE=true để bật upload
ENABLE_REMOTE="${ENABLE_REMOTE:-false}"
MC_ALIAS="${MC_ALIAS:-prodiab}"
REMOTE_BUCKET="${REMOTE_BUCKET:-prodiab-backup}"
REMOTE_PREFIX="${REMOTE_PREFIX:-db/daily}"

# Sentry webhook alert khi thất bại (tùy chọn)
SENTRY_DSN="${SENTRY_DSN:-}"

# ---------------------------------------------------------------------------
# Hàm tiện ích
# ---------------------------------------------------------------------------
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
DAY_OF_MONTH=$(date +"%d")
DATE_LABEL=$(date +"%Y-%m-%d")
FILENAME="${DB_NAME}_${TIMESTAMP}.sql.gz"
DEST_FILE="${BACKUP_DIR}/${FILENAME}"
LOG_TAG="backup-nightly"

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] [${LOG_TAG}] $*"
}

die() {
    log "ERROR: $*"
    if [[ -n "${SENTRY_DSN}" ]]; then
        curl -s -X POST "${SENTRY_DSN}" \
            -H 'Content-Type: application/json' \
            -d "{\"message\":\"[backup-nightly] Thất bại: $*\",\"level\":\"error\",\"tags\":{\"host\":\"$(hostname)\",\"db\":\"${DB_NAME}\"}}" \
            || true
    fi
    exit 1
}

# ---------------------------------------------------------------------------
# Kiểm tra điều kiện ban đầu
# ---------------------------------------------------------------------------
command -v mysqldump >/dev/null 2>&1 || die "mysqldump không tồn tại"
command -v gzip      >/dev/null 2>&1 || die "gzip không tồn tại"

mkdir -p "${BACKUP_DIR}" || die "Không tạo được thư mục: ${BACKUP_DIR}"

# Kiểm tra dung lượng đĩa tối thiểu 2 GB
AVAIL_KB=$(df -k "${BACKUP_DIR}" | awk 'NR==2{print $4}')
MIN_KB=$((2 * 1024 * 1024))
if [[ "${AVAIL_KB}" -lt "${MIN_KB}" ]]; then
    die "Dung lượng đĩa không đủ: ${AVAIL_KB}KB khả dụng, cần ít nhất ${MIN_KB}KB"
fi

# ---------------------------------------------------------------------------
# Thực hiện dump — single-transaction đảm bảo consistent snapshot
# ---------------------------------------------------------------------------
log "Bắt đầu mysqldump --single-transaction ${DB_NAME} → ${DEST_FILE}"

mysqldump \
    --host="${DB_HOST}" \
    --port="${DB_PORT}" \
    --user="${DB_USER}" \
    --password="${DB_PASS}" \
    --single-transaction \
    --routines \
    --triggers \
    --events \
    --add-drop-table \
    --default-character-set=utf8mb4 \
    --lock-tables=false \
    "${DB_NAME}" \
    | gzip -9 > "${DEST_FILE}" \
    || die "mysqldump thất bại"

FILESIZE=$(du -sh "${DEST_FILE}" | cut -f1)
log "Dump hoàn thành: ${DEST_FILE} (${FILESIZE})"

# ---------------------------------------------------------------------------
# Verify — gzip integrity check
# ---------------------------------------------------------------------------
gzip -t "${DEST_FILE}" || die "File backup bị hỏng (gzip integrity check thất bại)"
log "Integrity check: OK"

# ---------------------------------------------------------------------------
# Upload lên MinIO (nếu bật)
# ---------------------------------------------------------------------------
if [[ "${ENABLE_REMOTE}" == "true" ]]; then
    command -v mc >/dev/null 2>&1 || die "mc (MinIO client) không tồn tại"

    REMOTE_DAILY="${MC_ALIAS}/${REMOTE_BUCKET}/${REMOTE_PREFIX}/${DATE_LABEL}/${FILENAME}"
    log "Upload daily → ${REMOTE_DAILY}"
    mc cp "${DEST_FILE}" "${REMOTE_DAILY}" || die "Upload daily MinIO thất bại"

    # Monthly backup: lưu thêm bản ngày 01 hàng tháng
    if [[ "${DAY_OF_MONTH}" == "01" ]]; then
        MONTHLY_NAME="${DB_NAME}_monthly_$(date +%Y%m).sql.gz"
        REMOTE_MONTHLY="${MC_ALIAS}/${REMOTE_BUCKET}/db/monthly/${MONTHLY_NAME}"
        log "Upload monthly → ${REMOTE_MONTHLY}"
        mc cp "${DEST_FILE}" "${REMOTE_MONTHLY}" || log "WARN: Upload monthly thất bại (bỏ qua)"
    fi

    # Dọn dẹp remote daily cũ hơn RETENTION_REMOTE_DAYS
    log "Dọn dẹp remote backup cũ hơn ${RETENTION_REMOTE_DAYS} ngày"
    mc find "${MC_ALIAS}/${REMOTE_BUCKET}/db/daily/" \
        --older-than "${RETENTION_REMOTE_DAYS}d" \
        --name "*.sql.gz" \
        | xargs -r -I{} mc rm {} \
        || log "WARN: Dọn dẹp remote có lỗi (bỏ qua)"

    log "Upload MinIO hoàn tất"
fi

# ---------------------------------------------------------------------------
# Xóa backup local cũ hơn RETENTION_LOCAL_DAYS ngày
# ---------------------------------------------------------------------------
log "Dọn dẹp local backup cũ hơn ${RETENTION_LOCAL_DAYS} ngày"
find "${BACKUP_DIR}" -maxdepth 1 -name "${DB_NAME}_*.sql.gz" \
    -mtime "+${RETENTION_LOCAL_DAYS}" -delete \
    || log "WARN: Dọn dẹp local có lỗi (bỏ qua)"

log "Backup nightly hoàn tất — ${DEST_FILE}"
