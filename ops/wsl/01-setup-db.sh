#!/usr/bin/env bash
# Setup MySQL 8 + Redis trong WSL (chay as root qua: wsl -u root). Khong can admin Windows / sudo password.
set -uo pipefail

echo "=== whoami ==="; whoami
. /etc/os-release 2>/dev/null; echo "Distro: ${PRETTY_NAME:-?}"

echo "=== 1. apt update + install mysql-server redis-server ==="
export DEBIAN_FRONTEND=noninteractive
apt-get update -y -qq 2>&1 | tail -2
apt-get install -y -qq mysql-server redis-server 2>&1 | tail -6

echo "=== 2. start services ==="
service mysql start 2>&1 | tail -2 || true
service redis-server start 2>&1 | tail -2 || service redis start 2>&1 | tail -2 || true
sleep 4

echo "=== 3. mysql version ==="
mysql -e "SELECT VERSION() AS v;" 2>&1 | tail -3

echo "=== 4. create db + user (mysql_native_password) ==="
mysql <<'SQL' 2>&1 | tail -3
CREATE DATABASE IF NOT EXISTS prodiab_his CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
CREATE USER IF NOT EXISTS 'prodiab'@'localhost' IDENTIFIED WITH mysql_native_password BY 'prodiab_dev_2026';
CREATE USER IF NOT EXISTS 'prodiab'@'127.0.0.1' IDENTIFIED WITH mysql_native_password BY 'prodiab_dev_2026';
CREATE USER IF NOT EXISTS 'prodiab'@'%' IDENTIFIED WITH mysql_native_password BY 'prodiab_dev_2026';
GRANT ALL PRIVILEGES ON prodiab_his.* TO 'prodiab'@'localhost';
GRANT ALL PRIVILEGES ON prodiab_his.* TO 'prodiab'@'127.0.0.1';
GRANT ALL PRIVILEGES ON prodiab_his.* TO 'prodiab'@'%';
FLUSH PRIVILEGES;
SQL

echo "=== 5. bind-address 0.0.0.0 (de Windows ket noi qua WSL2 localhost forward) ==="
CNF=$(ls /etc/mysql/mysql.conf.d/mysqld.cnf 2>/dev/null || echo /etc/mysql/my.cnf)
if [ -f "$CNF" ]; then
  sed -i 's/^bind-address.*/bind-address = 0.0.0.0/' "$CNF" 2>/dev/null || true
  grep -q '^bind-address' "$CNF" || printf '[mysqld]\nbind-address = 0.0.0.0\n' >> "$CNF"
  service mysql restart 2>&1 | tail -1
  sleep 3
fi

echo "=== 6. verify TCP connect as prodiab ==="
mysql -h 127.0.0.1 -P 3306 -uprodiab -pprodiab_dev_2026 -e "SELECT 'connect_ok' AS status; SELECT @@version AS ver, @@collation_database AS coll;" 2>&1 | grep -v "Using a password"

echo "=== 7. redis ping ==="
redis-cli ping 2>&1 || echo "redis down"

echo "ALL_DONE"
