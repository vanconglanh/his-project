#!/usr/bin/env bash
# backup-minio.sh — Mirror toàn bộ MinIO data bucket lên offsite storage
# Dùng `mc mirror` để đồng bộ incremental sang MinIO offsite hoặc S3
# LF line ending, UTF-8 no BOM
set -euo pipefail

# ---------------------------------------------------------------------------
# Cấu hình
# ---------------------------------------------------------------------------
# Alias source (MinIO nội bộ, đã cấu hình qua: mc alias set prodiab ...)
SRC_ALIAS="${SRC_ALIAS:-prodiab}"
SRC_BUCKET="${SRC_BUCKET:-prodiab-data}"

# Alias đích offsite (MinIO offsite hoặc S3-compatible)
DST_ALIAS="${DST_ALIAS:-prodiab-offsite}"
DST_BUCKET="${DST_BUCKET:-prodiab-offsite-mirror}"

# Cờ mirror
MIRROR_FLAGS="${MIRROR_FLAGS:---watch=false --remove}"
# --remove: xóa file đích không còn ở source
# --watch=false: chạy một lần rồi thoát (phù hợp cho cron)

LOG_TAG="backup-minio"
LOG_FILE="${LOG_FILE:-/var/log/prodiab/backup-minio.log}"

# Sentry DSN alert khi thất bại (tùy chọn)
SENTRY_DSN="${SENTRY_DSN:-}"

# ---------------------------------------------------------------------------
# Hàm tiện ích
# ---------------------------------------------------------------------------
log() {
    local msg="[$(date '+%Y-%m-%d %H:%M:%S')] [${LOG_TAG}] $*"
    echo "${msg}"
    # Ghi vào log file nếu thư mục tồn tại
    local log_dir
    log_dir=$(dirname "${LOG_FILE}")
    if [[ -d "${log_dir}" ]]; then
        echo "${msg}" >> "${LOG_FILE}"
    fi
}

die() {
    log "ERROR: $*"
    if [[ -n "${SENTRY_DSN}" ]]; then
        curl -s -X POST "${SENTRY_DSN}" \
            -H 'Content-Type: application/json' \
            -d "{\"message\":\"[backup-minio] Mirror thất bại: $*\",\"level\":\"error\",\"tags\":{\"host\":\"$(hostname)\"}}" \
            || true
    fi
    exit 1
}

# ---------------------------------------------------------------------------
# Kiểm tra mc
# ---------------------------------------------------------------------------
command -v mc >/dev/null 2>&1 || die "mc (MinIO client) không tồn tại. Cài: https://min.io/docs/minio/linux/reference/minio-mc.html"

# ---------------------------------------------------------------------------
# Kiểm tra alias source và dest đã được cấu hình
# ---------------------------------------------------------------------------
mc alias ls "${SRC_ALIAS}" >/dev/null 2>&1 \
    || die "mc alias '${SRC_ALIAS}' chưa được cấu hình. Chạy: mc alias set ${SRC_ALIAS} <URL> <ACCESS_KEY> <SECRET_KEY>"

mc alias ls "${DST_ALIAS}" >/dev/null 2>&1 \
    || die "mc alias '${DST_ALIAS}' chưa được cấu hình. Chạy: mc alias set ${DST_ALIAS} <URL> <ACCESS_KEY> <SECRET_KEY>"

# ---------------------------------------------------------------------------
# Tạo bucket đích nếu chưa có
# ---------------------------------------------------------------------------
log "Đảm bảo bucket đích tồn tại: ${DST_ALIAS}/${DST_BUCKET}"
mc mb --ignore-existing "${DST_ALIAS}/${DST_BUCKET}" \
    || die "Không tạo được bucket đích"

# ---------------------------------------------------------------------------
# Mirror
# ---------------------------------------------------------------------------
log "Bắt đầu mirror: ${SRC_ALIAS}/${SRC_BUCKET} → ${DST_ALIAS}/${DST_BUCKET}"
log "Flags: ${MIRROR_FLAGS}"

# shellcheck disable=SC2086
mc mirror ${MIRROR_FLAGS} \
    "${SRC_ALIAS}/${SRC_BUCKET}" \
    "${DST_ALIAS}/${DST_BUCKET}" \
    || die "mc mirror thất bại"

# ---------------------------------------------------------------------------
# Thống kê sau mirror
# ---------------------------------------------------------------------------
SRC_SIZE=$(mc du --recursive "${SRC_ALIAS}/${SRC_BUCKET}" 2>/dev/null | tail -1 | awk '{print $1}' || echo "?")
DST_SIZE=$(mc du --recursive "${DST_ALIAS}/${DST_BUCKET}" 2>/dev/null | tail -1 | awk '{print $1}' || echo "?")

log "Mirror hoàn tất — Source: ${SRC_SIZE} | Dest: ${DST_SIZE}"

# ---------------------------------------------------------------------------
# Mirror thêm bucket backup nếu muốn (tùy chọn)
# ---------------------------------------------------------------------------
BACKUP_SRC_BUCKET="${BACKUP_SRC_BUCKET:-prodiab-backup}"
BACKUP_DST_BUCKET="${BACKUP_DST_BUCKET:-prodiab-backup-offsite}"

if mc ls "${SRC_ALIAS}/${BACKUP_SRC_BUCKET}" >/dev/null 2>&1; then
    log "Mirror bucket backup: ${SRC_ALIAS}/${BACKUP_SRC_BUCKET} → ${DST_ALIAS}/${BACKUP_DST_BUCKET}"
    mc mb --ignore-existing "${DST_ALIAS}/${BACKUP_DST_BUCKET}" || true
    # shellcheck disable=SC2086
    mc mirror ${MIRROR_FLAGS} \
        "${SRC_ALIAS}/${BACKUP_SRC_BUCKET}" \
        "${DST_ALIAS}/${BACKUP_DST_BUCKET}" \
        || log "WARN: Mirror bucket backup thất bại (bỏ qua)"
    log "Mirror bucket backup hoàn tất"
fi

log "backup-minio.sh kết thúc OK"
