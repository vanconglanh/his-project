#!/usr/bin/env bash
# Tao lai user prodiab voi caching_sha2_password (MySQL 8.4 bo mysql_native_password). Chay: wsl -u root.
set -uo pipefail

service mysql start 2>&1 | tail -1 || true
sleep 2

mysql <<'SQL'
CREATE DATABASE IF NOT EXISTS prodiab_his CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
DROP USER IF EXISTS 'prodiab'@'localhost';
DROP USER IF EXISTS 'prodiab'@'127.0.0.1';
DROP USER IF EXISTS 'prodiab'@'%';
CREATE USER 'prodiab'@'localhost'  IDENTIFIED BY 'prodiab_dev_2026';
CREATE USER 'prodiab'@'127.0.0.1'  IDENTIFIED BY 'prodiab_dev_2026';
CREATE USER 'prodiab'@'%'          IDENTIFIED BY 'prodiab_dev_2026';
GRANT ALL PRIVILEGES ON prodiab_his.* TO 'prodiab'@'localhost';
GRANT ALL PRIVILEGES ON prodiab_his.* TO 'prodiab'@'127.0.0.1';
GRANT ALL PRIVILEGES ON prodiab_his.* TO 'prodiab'@'%';
FLUSH PRIVILEGES;
SQL

echo "=== verify local socket (root) ==="
mysql -e "SELECT user, host, plugin FROM mysql.user WHERE user='prodiab';" 2>&1 | grep -v "Using a password"
echo "=== verify TCP connect as prodiab ==="
mysql -h 127.0.0.1 -P 3306 -uprodiab -pprodiab_dev_2026 -e "SELECT 'connect_ok' AS status, @@version AS ver, @@collation_database AS coll;" 2>&1 | grep -v "Using a password"
echo "=== listening ports ==="
ss -tlnp 2>/dev/null | grep -E ':3306|:6379' || true
echo DONE
