#!/usr/bin/env bash
# restore.sh — Khôi phục MySQL prodiab_his từ file backup
# Interactive: hiển thị danh sách backup, chọn file, restore và verify
# LF line ending, UTF-8 no BOM
set -euo pipefail

# ---------------------------------------------------------------------------
# Cấu hình
# ---------------------------------------------------------------------------
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-3306}"
DB_NAME="${DB_NAME:-prodiab_his}"
DB_USER="${DB_USER:-root}"
DB_PASS="${DB_PASS:-${MYSQL_ROOT_PASSWORD:-}}"

BACKUP_DIR="${BACKUP_DIR:-/opt/prodiab/backups/db}"

# MinIO (tùy chọn — để list và tải backup từ remote)
ENABLE_REMOTE="${ENABLE_REMOTE:-false}"
MC_ALIAS="${MC_ALIAS:-prodiab}"
REMOTE_BUCKET="${REMOTE_BUCKET:-prodiab-backup}"
REMOTE_PREFIX="${REMOTE_PREFIX:-db/daily}"

LOG_TAG="restore"

# ---------------------------------------------------------------------------
# Hàm tiện ích
# ---------------------------------------------------------------------------
log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] [${LOG_TAG}] $*"
}

die() {
    log "ERROR: $*"
    exit 1
}

confirm() {
    local prompt="$1"
    local answer
    printf "\n  %s [yes/NO]: " "${prompt}"
    read -r answer
    [[ "${answer}" == "yes" ]] || die "Người dùng hủy thao tác"
}

# ---------------------------------------------------------------------------
# Kiểm tra công cụ
# ---------------------------------------------------------------------------
command -v mysql    >/dev/null 2>&1 || die "mysql client không tồn tại"
command -v mysqldump >/dev/null 2>&1 || die "mysqldump không tồn tại"
command -v gzip     >/dev/null 2>&1 || die "gzip không tồn tại"

# ---------------------------------------------------------------------------
# Thu thập danh sách backup local
# ---------------------------------------------------------------------------
LIST_LOCAL=()
if [[ -d "${BACKUP_DIR}" ]]; then
    while IFS= read -r -d '' f; do
        LIST_LOCAL+=("LOCAL:${f}")
    done < <(find "${BACKUP_DIR}" -maxdepth 1 -name "${DB_NAME}_*.sql.gz" -print0 \
             | sort -rz | head -zn 20)
fi

# Thu thập danh sách backup remote (nếu bật)
LIST_REMOTE=()
if [[ "${ENABLE_REMOTE}" == "true" ]] && command -v mc >/dev/null 2>&1; then
    while IFS= read -r line; do
        [[ -n "${line}" ]] && LIST_REMOTE+=("REMOTE:${line}")
    done < <(mc ls --recursive "${MC_ALIAS}/${REMOTE_BUCKET}/${REMOTE_PREFIX}/" 2>/dev/null \
             | awk '{print $NF}' | grep '\.sql\.gz$' | sort -r | head -20 \
             || true)
fi

ALL_ITEMS=("${LIST_LOCAL[@]:-}" "${LIST_REMOTE[@]:-}")

if [[ ${#ALL_ITEMS[@]} -eq 0 ]]; then
    die "Không tìm thấy file backup nào. BACKUP_DIR=${BACKUP_DIR}"
fi

# ---------------------------------------------------------------------------
# Hiển thị menu
# ---------------------------------------------------------------------------
echo ""
echo "========================================================"
echo "  PRO-DIAB HIS — MySQL Restore Utility"
echo "========================================================"
printf "  DB Target : %s@%s:%s/%s\n" "${DB_USER}" "${DB_HOST}" "${DB_PORT}" "${DB_NAME}"
echo ""
echo "  Danh sách backup khả dụng:"
echo ""

for i in "${!ALL_ITEMS[@]}"; do
    item="${ALL_ITEMS[$i]}"
    type_label="${item%%:*}"
    path_val="${item#*:}"
    if [[ "${type_label}" == "LOCAL" ]]; then
        size=$(du -sh "${path_val}" 2>/dev/null | cut -f1 || echo "?")
        printf "  %3d) [LOCAL]  %-50s (%s)\n" "$((i+1))" "$(basename "${path_val}")" "${size}"
    else
        printf "  %3d) [REMOTE] %s\n" "$((i+1))" "${path_val}"
    fi
done

echo ""
printf "  Nhập số thứ tự (1-%d): " "${#ALL_ITEMS[@]}"
read -r CHOICE

# Validate
[[ "${CHOICE}" =~ ^[0-9]+$ ]] || die "Lựa chọn không hợp lệ: '${CHOICE}'"
INDEX=$((CHOICE - 1))
[[ "${INDEX}" -ge 0 && "${INDEX}" -lt "${#ALL_ITEMS[@]}" ]] \
    || die "Số thứ tự ${CHOICE} ngoài phạm vi 1-${#ALL_ITEMS[@]}"

SELECTED="${ALL_ITEMS[$INDEX]}"
SELECTED_TYPE="${SELECTED%%:*}"
SELECTED_PATH="${SELECTED#*:}"

# ---------------------------------------------------------------------------
# Tải về nếu là remote
# ---------------------------------------------------------------------------
RESTORE_FILE="${SELECTED_PATH}"
TEMP_FILE=""

if [[ "${SELECTED_TYPE}" == "REMOTE" ]]; then
    TEMP_FILE="/tmp/prodiab_restore_$(date +%s).sql.gz"
    log "Đang tải backup từ MinIO: ${SELECTED_PATH}"
    mc cp "${MC_ALIAS}/${REMOTE_BUCKET}/${REMOTE_PREFIX}/${SELECTED_PATH}" "${TEMP_FILE}" \
        || die "Tải file từ MinIO thất bại"
    RESTORE_FILE="${TEMP_FILE}"
fi

# ---------------------------------------------------------------------------
# Kiểm tra tính toàn vẹn
# ---------------------------------------------------------------------------
log "Kiểm tra integrity: ${RESTORE_FILE}"
gzip -t "${RESTORE_FILE}" || die "File backup bị hỏng (gzip test thất bại)"
log "Integrity OK"

# ---------------------------------------------------------------------------
# Xác nhận nguy hiểm
# ---------------------------------------------------------------------------
echo ""
printf "  File restore : %s\n" "$(basename "${RESTORE_FILE}")"
printf "  DB target    : %s/%s\n" "${DB_HOST}" "${DB_NAME}"
echo ""
echo "  CANH BAO: Toan bo du lieu hien tai trong '${DB_NAME}' se bi XOA!"
confirm "Bạn có chắc muốn tiếp tục không? (gõ đúng 'yes')"

# ---------------------------------------------------------------------------
# Backup database hiện tại trước khi restore
# ---------------------------------------------------------------------------
PRE_RESTORE_FILE="${BACKUP_DIR}/pre_restore_${DB_NAME}_$(date +%Y%m%d_%H%M%S).sql.gz"
log "Tạo backup an toàn trước khi restore → ${PRE_RESTORE_FILE}"
mkdir -p "${BACKUP_DIR}"
mysqldump \
    --host="${DB_HOST}" --port="${DB_PORT}" \
    --user="${DB_USER}" --password="${DB_PASS}" \
    --single-transaction --routines --triggers \
    "${DB_NAME}" \
    | gzip -9 > "${PRE_RESTORE_FILE}" \
    || log "WARN: Không backup được DB hiện tại (có thể DB đang down — tiếp tục restore)"

# ---------------------------------------------------------------------------
# Drop & recreate database
# ---------------------------------------------------------------------------
log "Drop database ${DB_NAME}..."
mysql \
    --host="${DB_HOST}" --port="${DB_PORT}" \
    --user="${DB_USER}" --password="${DB_PASS}" \
    -e "DROP DATABASE IF EXISTS \`${DB_NAME}\`;" \
    || die "DROP DATABASE thất bại"

log "Tạo lại database ${DB_NAME}..."
mysql \
    --host="${DB_HOST}" --port="${DB_PORT}" \
    --user="${DB_USER}" --password="${DB_PASS}" \
    -e "CREATE DATABASE \`${DB_NAME}\` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;" \
    || die "CREATE DATABASE thất bại"

# ---------------------------------------------------------------------------
# Restore dữ liệu
# ---------------------------------------------------------------------------
log "Đang restore dữ liệu từ ${RESTORE_FILE}..."
gunzip -c "${RESTORE_FILE}" \
    | mysql \
        --host="${DB_HOST}" --port="${DB_PORT}" \
        --user="${DB_USER}" --password="${DB_PASS}" \
        --default-character-set=utf8mb4 \
        "${DB_NAME}" \
    || die "Restore thất bại"

# ---------------------------------------------------------------------------
# Verify sau restore
# ---------------------------------------------------------------------------
log "Verify sau restore..."
TABLE_COUNT=$(mysql \
    --host="${DB_HOST}" --port="${DB_PORT}" \
    --user="${DB_USER}" --password="${DB_PASS}" \
    --skip-column-names --batch \
    -e "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='${DB_NAME}';" \
    2>/dev/null | tr -d '[:space:]' || echo "0")

if [[ -z "${TABLE_COUNT}" || "${TABLE_COUNT}" -eq 0 ]]; then
    die "Verify thất bại: không tìm thấy bảng nào sau restore"
fi
log "Verify OK: ${TABLE_COUNT} bảng được restore"

# ---------------------------------------------------------------------------
# Dọn dẹp temp file
# ---------------------------------------------------------------------------
if [[ -n "${TEMP_FILE}" && -f "${TEMP_FILE}" ]]; then
    rm -f "${TEMP_FILE}"
fi

echo ""
log "Restore hoàn tất thành công!"
log "Pre-restore backup lưu tại: ${PRE_RESTORE_FILE}"
