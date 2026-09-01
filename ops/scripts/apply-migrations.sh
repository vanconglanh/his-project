#!/bin/bash
# apply-migrations.sh
# Apply schema dump + migrations vao MySQL
# Chay tu migrator container (one-shot)
# LF line ending, UTF-8 no BOM
set -e

DB_HOST=${DB_HOST:-mysql}
DB_USER=${DB_USER:-root}
DB_PASS=${DB_PASS:-root_dev}
DB_NAME=${DB_NAME:-prodiab_his}

DUMP_DIR="/db"
MIGRATION_DIR="/db/migrations"

# --- Ham kiem tra MySQL san sang ---
wait_mysql() {
    echo "[migrator] Doi MySQL san sang tai $DB_HOST..."
    local retries=30
    until mysqladmin ping -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASS" --silent 2>/dev/null; do
        retries=$((retries - 1))
        if [ "$retries" -le 0 ]; then
            echo "[migrator] TIMEOUT: MySQL khong san sang sau 60s. Thoat."
            exit 1
        fi
        echo "[migrator] MySQL chua san sang, thu lai sau 2s... (con $retries lan)"
        sleep 2
    done
    echo "[migrator] MySQL da san sang."
}

wait_mysql

# --- Kiem tra DB co rong khong ---
TABLE_COUNT=$(mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASS" \
    -N -e "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='$DB_NAME';" 2>/dev/null || echo "0")

echo "[migrator] So bang hien co trong '$DB_NAME': $TABLE_COUNT"

# --- Apply schema dump neu DB rong ---
if [ "$TABLE_COUNT" -eq 0 ]; then
    echo "[migrator] DB rong, tim schema dump..."
    DUMP_FILES=("$DUMP_DIR"/diab_his_*.sql)
    if [ -e "${DUMP_FILES[0]}" ]; then
        for f in "${DUMP_FILES[@]}"; do
            echo "[migrator]   Applying dump: $(basename "$f")"
            # Loc bo dong SET @@GLOBAL.GTID_PURGED: server co gtid_mode=OFF se loi khi gap dong nay.
            # KHONG sua file dump goc (read-only) — chi loc luc nap.
            grep -v 'SET @@GLOBAL.GTID_PURGED' "$f" | \
            mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASS" \
                --default-character-set=utf8mb4 \
                "$DB_NAME"
        done
        echo "[migrator] Schema dump da duoc apply."
    else
        echo "[migrator] Khong tim thay file dump diab_his_*.sql, bo qua."
    fi
else
    echo "[migrator] DB da co du lieu, bo qua schema dump."
fi

# --- Bang theo doi migration da apply (tranh restart-loop) ---
# Van de da xay ra: container migrator restart (crash/host reboot/docker
# compose up lai) se chay lai TOAN BO script tu dau. Nhieu file 00xx (seed/
# ALTER truc tiep len bang legacy khong qua add_col_if_missing) tham chieu
# ten bang legacy (vd sec_roles, pat_pii_data) da bi 9000_drop_legacy.sql
# xoa hoac thay bang VIEW o lan chay dau -> lan chay lai bao loi 1146/1347
# -> container Restarting (1) vinh vien. Fix: chi ap dung 1 file migration
# 1 lan duy nhat, ghi nhan vao bang _schema_migrations sau khi thanh cong.
mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASS" --default-character-set=utf8mb4 "$DB_NAME" -e "
CREATE TABLE IF NOT EXISTS _schema_migrations (
    filename    VARCHAR(255) NOT NULL PRIMARY KEY,
    applied_at  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
" 2>&1 | grep -v '^mysql: \[Warning\]' || true

# --- Apply migrations (chi file CHUA duoc ghi nhan trong _schema_migrations) ---
if [ -d "$MIGRATION_DIR" ]; then
    MIGRATION_FILES=("$MIGRATION_DIR"/*.sql)
    if [ -e "${MIGRATION_FILES[0]}" ]; then
        echo "[migrator] Bat dau apply migrations..."
        for f in $(ls "$MIGRATION_DIR"/*.sql | sort); do
            fname=$(basename "$f")
            ALREADY=$(mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASS" --default-character-set=utf8mb4 \
                -N -e "SELECT COUNT(*) FROM _schema_migrations WHERE filename='$fname';" "$DB_NAME" 2>/dev/null || echo "0")
            if [ "$ALREADY" = "1" ]; then
                echo "[migrator]   Bo qua (da apply truoc do): $fname"
                continue
            fi
            echo "[migrator]   Applying migration: $fname"
            mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASS" \
                --default-character-set=utf8mb4 \
                "$DB_NAME" < "$f"
            mysql -h "$DB_HOST" -u "$DB_USER" -p"$DB_PASS" --default-character-set=utf8mb4 "$DB_NAME" \
                -e "INSERT INTO _schema_migrations (filename) VALUES ('$fname');" 2>&1 | grep -v '^mysql: \[Warning\]' || true
        done
        echo "[migrator] Tat ca migrations da duoc apply."
    else
        echo "[migrator] Khong co file migration nao trong $MIGRATION_DIR"
    fi
else
    echo "[migrator] WARN: Thu muc $MIGRATION_DIR khong ton tai."
fi

echo "[migrator] Hoan tat."
