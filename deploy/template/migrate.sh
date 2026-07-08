#!/usr/bin/env bash
# One-shot migrator cho 1 phong kham: apply schema 9xxx (tru demo) + seed.sql.
# Migration idempotent (IF NOT EXISTS / stored procedure) nen chay lai an toan.
set -euo pipefail

HOST=mysql
USER=root
PWD_="${DB_ROOT_PASSWORD}"
DB="${DB_NAME}"

mysql_run() { mysql --host="$HOST" --user="$USER" --password="$PWD_" --default-character-set=utf8mb4 "$@"; }

echo "[migrate] Cho MySQL san sang..."
for i in $(seq 1 30); do
  if mysqladmin --host="$HOST" --user="$USER" --password="$PWD_" ping >/dev/null 2>&1; then break; fi
  sleep 2
done

echo "[migrate] Ap dung schema 9xxx (tru demo seed) vao $DB"
for f in $(ls /db/migrations/9*.sql | sort); do
  base=$(basename "$f")
  case "$base" in
    9008_seed_demo.sql|9020_seed_rich_demo.sql) echo "  skip $base (demo)"; continue;;
  esac
  echo "  apply $base"
  mysql_run "$DB" < "$f"
done

if [ -f /seed.sql ]; then
  echo "[migrate] Ap dung seed.sql (du lieu phong kham)"
  mysql_run "$DB" < /seed.sql
fi

echo "[migrate] HOAN TAT"
