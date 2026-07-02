#!/bin/bash
# seed-dev.sh
# Seed du lieu phat trien: tenant demo + admin user + danh muc co ban
# Chi chay trong moi truong DEV/staging, KHONG chay production
# LF line ending, UTF-8 no BOM
set -e

DB_HOST=${DB_HOST:-mysql}
DB_USER=${DB_USER:-root}
DB_PASS=${DB_PASS:-root_dev}
DB_NAME=${DB_NAME:-prodiab_his}
SEED_DIR=${SEED_DIR:-/db/seeds}

echo "[seed-dev] Bat dau seed du lieu dev luc $(date)"

# --- Kiem tra MySQL san sang ---
until mysqladmin ping -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASS" --silent 2>/dev/null; do
    echo "[seed-dev] Doi MySQL..."
    sleep 2
done

# --- Apply seed files theo thu tu ---
if [ -d "$SEED_DIR" ]; then
    SEED_FILES=("$SEED_DIR"/*.sql)
    if [ -e "${SEED_FILES[0]}" ]; then
        for f in $(ls "$SEED_DIR"/*.sql | sort); do
            echo "[seed-dev]   Applying seed: $(basename "$f")"
            mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASS" \
                --default-character-set=utf8mb4 \
                "$DB_NAME" < "$f"
        done
    else
        echo "[seed-dev] Khong tim thay file seed trong $SEED_DIR"
    fi
else
    echo "[seed-dev] WARN: Thu muc seed $SEED_DIR khong ton tai."
fi

# --- Seed inline: tenant demo + admin user (fallback neu khong co file seed) ---
mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASS" --default-character-set=utf8mb4 "$DB_NAME" << 'SQL'
-- Tenant demo (bo qua neu da ton tai)
INSERT IGNORE INTO `diab_his_tenants` (
    `name`, `code`, `status`, `created_at`
) VALUES (
    'Phong kham Demo Pro-Diab',
    'DEMO001',
    'active',
    NOW()
) ON DUPLICATE KEY UPDATE `name` = VALUES(`name`);

-- Thong bao
SELECT 'Seed dev hoan tat.' AS status;
SQL

echo "[seed-dev] Seed hoan tat luc $(date)"
