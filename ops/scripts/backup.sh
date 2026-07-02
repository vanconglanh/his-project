#!/bin/bash
# backup.sh
# Backup MySQL prodiab_his -> gzip -> MinIO (tuy chon)
# Chay hang ngay luc 02:00 qua cron hoac docker scheduled task
# Retention: 30 ngay daily + giu monthly (ngay 1 hang thang)
# LF line ending, UTF-8 no BOM
set -e

# --- Config (doc tu env hoac dung gia tri mac dinh) ---
DB_HOST=${DB_HOST:-mysql}
DB_USER=${DB_USER:-root}
DB_PASS=${MYSQL_ROOT_PASSWORD:-root_dev}
DB_NAME=${DB_NAME:-prodiab_his}

BACKUP_DIR=${BACKUP_DIR:-/var/backups/prodiab}
RETENTION_DAYS=${RETENTION_DAYS:-30}

MINIO_ENDPOINT=${MINIO_ENDPOINT:-http://minio:9000}
MINIO_ACCESS_KEY=${MINIO_ROOT_USER:-minio}
MINIO_SECRET_KEY=${MINIO_ROOT_PASSWORD:-minio_dev_2026}
MINIO_BUCKET=${MINIO_BUCKET:-prodiab-backup}
UPLOAD_TO_MINIO=${UPLOAD_TO_MINIO:-false}

DATE=$(date +%Y%m%d_%H%M%S)
DAY_OF_MONTH=$(date +%d)
FILENAME="prodiab_his_${DATE}.sql.gz"
FILEPATH="$BACKUP_DIR/$FILENAME"

echo "[backup] Bat dau backup $DB_NAME luc $(date)"
mkdir -p "$BACKUP_DIR"

# --- Dump ---
mysqldump \
    -h "$DB_HOST" \
    -u "$DB_USER" \
    -p"$DB_PASS" \
    --single-transaction \
    --routines \
    --triggers \
    --events \
    --add-drop-table \
    --default-character-set=utf8mb4 \
    "$DB_NAME" \
    | gzip > "$FILEPATH"

echo "[backup] Da tao: $FILEPATH ($(du -sh "$FILEPATH" | cut -f1))"

# --- Upload len MinIO (tuy chon) ---
if [ "$UPLOAD_TO_MINIO" = "true" ]; then
    echo "[backup] Upload len MinIO: $MINIO_BUCKET/daily/$FILENAME"
    # Kiem tra mc (MinIO client) ton tai
    if command -v mc &>/dev/null; then
        mc alias set prodiab "$MINIO_ENDPOINT" "$MINIO_ACCESS_KEY" "$MINIO_SECRET_KEY" --quiet
        mc mb --ignore-existing "prodiab/$MINIO_BUCKET"
        mc cp "$FILEPATH" "prodiab/$MINIO_BUCKET/daily/$FILENAME"
        echo "[backup] Upload hoan tat."

        # Monthly backup: giu ban ngay 1
        if [ "$DAY_OF_MONTH" = "01" ]; then
            MONTHLY_NAME="prodiab_his_monthly_$(date +%Y%m).sql.gz"
            mc cp "$FILEPATH" "prodiab/$MINIO_BUCKET/monthly/$MONTHLY_NAME"
            echo "[backup] Da luu monthly: $MONTHLY_NAME"
        fi
    else
        echo "[backup] WARN: Lenh 'mc' khong tim thay, bo qua upload MinIO."
    fi
fi

# --- Xoa backup cu (> RETENTION_DAYS ngay) ---
echo "[backup] Xoa backup cu hon $RETENTION_DAYS ngay..."
find "$BACKUP_DIR" -name "prodiab_his_*.sql.gz" -mtime +"$RETENTION_DAYS" -delete
echo "[backup] Retention cleanup hoan tat."

echo "[backup] Backup thanh cong: $FILEPATH"
echo "[backup] Ket thuc luc $(date)"
