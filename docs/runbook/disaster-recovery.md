# Disaster Recovery Runbook — Pro-Diab HIS

**RTO (Recovery Time Objective): 4 giờ**
**RPO (Recovery Point Objective): 24 giờ**
**Phiên bản:** 1.0 — Sprint 12
**Owner:** DevOps (Chương)
**Review:** Hàng quý

---

## 1. Tổng quan

Tài liệu này mô tả quy trình phục hồi thảm họa (DR) cho Pro-Diab HIS trong các kịch bản nghiêm trọng. Mọi thao tác restore phải được ghi log vào incident ticket.

### 1.1 Thông tin liên lạc khẩn cấp

| Vai trò | Người | Contact |
|---------|-------|---------|
| DevOps Lead | Chương | #oncall-devops Slack |
| Backend Lead | | #oncall-backend Slack |
| QC Lead | Chi | #oncall-qc Slack |
| PO / Khách hàng | | Qua PO |

### 1.2 Hệ thống và vị trí

| Thành phần | Server | Path |
|------------|--------|------|
| Docker Compose | prod-vm-01 | /opt/prodiab |
| Backup local | prod-vm-01 | /opt/prodiab/backups/db |
| Backup MinIO offsite | minio-offsite | bucket: prodiab-backup |
| Git repo | GitHub | github.com/org/pro-diab-his |
| Env files | prod-vm-01 | /opt/prodiab/.env (KHÔNG trong git) |

---

## 2. Kịch bản DR-01: MySQL Crash / Corrupt

### Triệu chứng
- Backend trả lỗi `Database connection failed`
- Container `prodiab_mysql` restart liên tục
- Log: `[ERROR] InnoDB: Database page corruption on disk`

### Bước xử lý

#### Bước 1: Assess (5-10 phút)

```bash
# SSH vào production server
ssh deploy@prod-vm-01

# Kiểm tra trạng thái container
docker compose -f /opt/prodiab/docker-compose.yml ps

# Xem log MySQL
docker compose -f /opt/prodiab/docker-compose.yml logs --tail=100 mysql

# Kiểm tra disk
df -h
du -sh /opt/prodiab/volumes/mysql_data/
```

#### Bước 2: Thử repair (15-30 phút)

```bash
# Thử restart MySQL trước
docker compose -f /opt/prodiab/docker-compose.yml restart mysql
sleep 30
docker compose -f /opt/prodiab/docker-compose.yml logs mysql | tail -20

# Nếu vẫn lỗi, thử mysqlcheck
docker compose -f /opt/prodiab/docker-compose.yml exec mysql \
  mysqlcheck --all-databases \
  -u root -p"${MYSQL_ROOT_PASSWORD}" \
  --auto-repair
```

#### Bước 3: Restore từ backup (30-90 phút)

Nếu repair thất bại, restore từ backup gần nhất:

```bash
# Tìm backup gần nhất
ls -lt /opt/prodiab/backups/db/ | head -5

# Chạy restore script interactive
cd /opt/prodiab
./ops/scripts/restore.sh
# → Chọn file backup gần nhất
# → Nhập 'yes' để xác nhận
```

Nếu không có backup local, tải từ MinIO:

```bash
# List backup remote
mc ls prodiab/prodiab-backup/db/daily/ | sort | tail -5

# Thiết lập ENABLE_REMOTE=true và chạy restore
export ENABLE_REMOTE=true
./ops/scripts/restore.sh
```

#### Bước 4: Verify và restart services (10-20 phút)

```bash
# Restart toàn bộ stack
docker compose -f /opt/prodiab/docker-compose.yml up -d

# Smoke test
curl -f https://prodiab.example.com/healthz || echo "HEALTHZ FAILED"

# Kiểm tra table count
docker compose exec mysql mysql -u root -p"${MYSQL_ROOT_PASSWORD}" \
  -e "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='prodiab_his';"
```

#### Bước 5: Thông báo

- Update incident ticket với thời gian restore và data loss (nếu có)
- Thông báo PO về RPO thực tế
- Post-mortem trong vòng 48 giờ

---

## 3. Kịch bản DR-02: MinIO Mất Data

### Triệu chứng
- File CLS/PDF không tải được
- MinIO console báo bucket missing hoặc object not found
- Container `prodiab_minio` crash

### Bước xử lý

#### Bước 1: Assess

```bash
docker compose logs minio | tail -50
mc ls prodiab/prodiab-data/ | head -20
```

#### Bước 2: Khởi động lại MinIO

```bash
docker compose restart minio
sleep 10
mc admin info prodiab
```

#### Bước 3: Restore từ offsite mirror

```bash
# Restore bucket data từ offsite
mc mirror \
  prodiab-offsite/prodiab-offsite-mirror \
  prodiab/prodiab-data \
  --overwrite

# Verify
mc ls prodiab/prodiab-data/ | wc -l
```

#### Bước 4: Kiểm tra bucket policy

```bash
# Re-apply policy nếu cần
mc policy set-json /opt/prodiab/ops/minio/bucket-policy.json \
  prodiab/prodiab-data
```

---

## 4. Kịch bản DR-03: Full VM Down

### Triệu chứng
- Server không phản hồi ping
- SSH không kết nối được
- Toàn bộ service down

### Bước xử lý

#### Bước 1: Xác nhận tình trạng (5 phút)

```bash
# Kiểm tra từ máy khác
ping -c 5 prod-vm-01-ip
curl -f https://prodiab.example.com/healthz
```

Liên hệ nhà cung cấp hosting để restart VM.

#### Bước 2: Provision VM mới (nếu VM không phục hồi được)

Thời gian ước tính: 1-2 giờ

```bash
# 1. Tạo VM mới (Ubuntu 22.04, ≥ 4 CPU, 8GB RAM, 100GB SSD)
# 2. Cài Docker + Docker Compose
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker deploy

# 3. Clone repo
git clone git@github.com:org/pro-diab-his.git /opt/prodiab
cd /opt/prodiab

# 4. Khôi phục .env từ secret vault (1Password / Vault)
# KHÔNG lưu .env trong git
cp /path/to/secure/.env /opt/prodiab/.env

# 5. Pull images
docker compose -f ops/docker-compose.prod.yml pull

# 6. Start services (chưa có data)
docker compose -f ops/docker-compose.prod.yml up -d

# 7. Restore database
export DB_HOST=localhost
./ops/scripts/restore.sh
# Chọn backup gần nhất từ remote MinIO

# 8. Restore MinIO data
mc alias set prodiab-offsite http://offsite-minio:9000 ACCESS SECRET
mc mirror prodiab-offsite/prodiab-offsite-mirror prodiab/prodiab-data
```

#### Bước 3: Cập nhật DNS

```bash
# Trỏ A record domain về IP mới của VM
# TTL: 300s để DNS propagate nhanh
# Công cụ: dashboard của DNS provider (Cloudflare, etc.)
```

#### Bước 4: Smoke test toàn diện

```bash
./deploy.sh all prod --smoke-only
```

---

## 5. Backup Restore — Step-by-Step Chi Tiết

### 5.1 Restore database lên staging (monthly drill)

```bash
# 1. SSH vào staging server
ssh deploy@staging-vm-01

# 2. Tìm backup của ngày cần restore
mc ls prodiab/prodiab-backup/db/daily/ | grep "2026-05"

# 3. Tải về staging
mc cp prodiab/prodiab-backup/db/daily/2026-05-20/prodiab_his_20260520_020000.sql.gz \
  /tmp/restore_drill.sql.gz

# 4. Kiểm tra integrity
gzip -t /tmp/restore_drill.sql.gz && echo "OK"

# 5. Restore lên staging (KHÔNG restore production)
export DB_HOST=localhost
export DB_NAME=prodiab_his_staging
BACKUP_FILE=/tmp/restore_drill.sql.gz ./ops/scripts/restore.sh

# 6. Verify
mysql -u root -p -e "SELECT COUNT(*) FROM prodiab_his_staging.his_patient;"

# 7. Ghi kết quả vào ticket drill tháng này
```

### 5.2 Kiểm tra backup mỗi ngày

```bash
# Script check-backup-health.sh (chạy qua cron)
LATEST=$(ls -t /opt/prodiab/backups/db/prodiab_his_*.sql.gz 2>/dev/null | head -1)
if [[ -z "${LATEST}" ]]; then
    echo "CRITICAL: Không có backup file nào!"
    exit 1
fi

AGE_HOURS=$(( ($(date +%s) - $(stat -c %Y "${LATEST}")) / 3600 ))
if [[ ${AGE_HOURS} -gt 25 ]]; then
    echo "CRITICAL: Backup cũ hơn 25 giờ! File: ${LATEST}"
    exit 1
fi

echo "OK: Backup mới nhất: ${LATEST} (${AGE_HOURS} giờ trước)"
```

---

## 6. RTO / RPO Tracking

| Kịch bản | RTO Target | RPO Target | Actual RTO (drill) |
|----------|-----------|-----------|-------------------|
| MySQL crash + repair | 30 phút | 0 | - |
| MySQL crash + restore | 2 giờ | 24 giờ | - |
| MinIO data loss | 1 giờ | 24 giờ | - |
| Full VM down | 4 giờ | 24 giờ | - |

Drill định kỳ: **mỗi tháng vào staging**, ghi thực tế vào bảng trên.

---

## 7. Checklist sau DR

- [ ] Toàn bộ service `healthy` trong `docker compose ps`
- [ ] `/healthz` trả HTTP 200
- [ ] Login được với tài khoản test
- [ ] Tạo bệnh nhân test và verify data
- [ ] Sentry không có error rate spike
- [ ] Backup chạy thành công ngay sau khi restore
- [ ] Incident ticket được update đầy đủ
- [ ] Thông báo cho PO / khách hàng bị ảnh hưởng
- [ ] Schedule post-mortem trong 48 giờ
