# Security Incident Response — Pro-Diab HIS

**Phiên bản:** 1.0 — Sprint 12
**Owner:** DevOps (Chương) + Backend Lead
**Review:** Hàng quý
**Classification:** INTERNAL — Không chia sẻ ra ngoài tổ chức

---

## 1. Phân loại Security Incident

| Loại | Mô tả | Mức độ |
|------|-------|--------|
| **Data Breach** | Truy cập trái phép vào dữ liệu bệnh nhân | P1 Critical |
| **Unauthorized Access** | Đăng nhập trái phép vào hệ thống | P1 Critical |
| **Ransomware** | Dữ liệu bị mã hóa bởi attacker | P1 Critical |
| **SQL Injection** | Tấn công injection thành công | P1 Critical |
| **DDoS** | Hệ thống bị flood traffic | P2 High |
| **Credential Leak** | Secret/credential lộ ra ngoài | P2 High |
| **Vulnerability Found** | Lỗ hổng được báo cáo (chưa bị exploit) | P3 Medium |
| **Brute Force** | Tấn công login brute force | P3 Medium |

---

## 2. Quy trình 5 bước

### STEP 1: DETECT & REPORT (0-15 phút)

**Dấu hiệu nhận biết:**
- Sentry báo nhiều `401/403` bất thường từ IP lạ
- Alertmanager: error rate đột biến
- Nginx log: scan pattern (sqlmap, nikto, masscan)
- Grafana: traffic spike từ một IP/subnet
- User báo cáo dữ liệu bị thay đổi không rõ lý do
- Gitleaks/TruffleHog CI fail

**Hành động ngay:**
```bash
# 1. Không xóa log — preserve evidence
# 2. Screenshot, export log
docker compose logs --timestamps > /tmp/incident-$(date +%Y%m%d_%H%M%S).log

# 3. Báo ngay cho Security Lead và IC
# Slack: @channel trong #security-alerts
```

### STEP 2: CONTAIN (15-60 phút)

**Cô lập hệ thống bị ảnh hưởng:**

```bash
# Block IP tấn công ngay lập tức
# Thêm vào nginx geo block:
sudo nano /opt/prodiab/ops/nginx/conf.d/prodiab.conf
# Thêm: ATTACKER_IP  1; vào geo $block_bad_actor {}
docker compose exec nginx nginx -s reload

# Hoặc dùng iptables cho block nhanh hơn
sudo iptables -A INPUT -s ATTACKER_IP -j DROP
sudo iptables-save > /etc/iptables/rules.v4

# Nếu breach nghiêm trọng — tắt toàn bộ external access
# CHỈ làm khi được IC authorize
sudo iptables -A INPUT -p tcp --dport 443 -j DROP
sudo iptables -A INPUT -p tcp --dport 80  -j DROP
```

**Rotate credentials bị lộ:**

```bash
# 1. Đổi DB password
docker compose exec mysql mysqladmin -u root -p password 'NewStrongPassword@2026'
# Update .env: DB_PASS=NewStrongPassword@2026
# Restart backend

# 2. Revoke JWT tokens (nếu secret bị lộ)
# Đổi JWT_SECRET trong .env → restart backend → tất cả token cũ invalid

# 3. Đổi MinIO access key
mc admin user add prodiab newuser NewsecretKey2026@
mc admin user remove prodiab compromised_user

# 4. Revoke API keys với third-party (ĐTQG, BHYT)
# Liên hệ nhà cung cấp để revoke ngay
```

### STEP 3: ERADICATE (1-4 giờ)

**Tìm và loại bỏ root cause:**

```bash
# Audit log — tìm activity bất thường
docker compose exec mysql mysql -u root -p prodiab_his \
  -e "SELECT * FROM his_audit_log
      WHERE created_at > NOW() - INTERVAL 24 HOUR
        AND action IN ('UPDATE','DELETE')
      ORDER BY created_at DESC LIMIT 100;"

# Tìm file lạ trong container
docker compose exec backend find /app -newer /app/ProDiab.API.dll -type f

# Kiểm tra cron job lạ
docker compose exec backend crontab -l 2>/dev/null || true

# Scan malware (nếu có)
docker run --rm -v /opt/prodiab:/scan:ro \
  clamav/clamav:stable clamscan --recursive /scan

# Review git log cho commit lạ
git log --all --oneline --since="48 hours ago"
```

**Patch lỗ hổng:**
- Cập nhật dependency bị lỗi
- Fix code nếu có SQL injection / XSS
- Tăng cường validation input

### STEP 4: RECOVER (2-8 giờ)

```bash
# 1. Restore dữ liệu từ backup sạch (nếu data bị modify)
./ops/scripts/restore.sh
# Chọn backup trước thời điểm incident

# 2. Deploy lại từ image sạch
docker compose pull
docker compose up -d --force-recreate

# 3. Verify toàn bộ integrity
./deploy.sh all prod --smoke-only

# 4. Monitor intensive trong 24 giờ sau
# Bật log level DEBUG tạm thời
# Tăng tần suất Alertmanager check
```

### STEP 5: REPORT & NOTIFY (trong 72 giờ)

---

## 3. Notification Timeline (GDPR-like)

Pro-Diab HIS lưu trữ dữ liệu y tế — **NHẠY CẢM CỰC KỲ**. Tuân thủ:
- Nghị định 13/2023/NĐ-CP về bảo vệ dữ liệu cá nhân (Việt Nam)
- Thông tư 09/2012/TT-BYT về bảo mật thông tin bệnh nhân

### Timeline bắt buộc

| Thời điểm | Hành động | Người thực hiện |
|-----------|----------|----------------|
| T+0 | Phát hiện incident | IC |
| T+1h | Thông báo nội bộ Ban Giám Đốc | PO |
| T+4h | Assess scope: số bệnh nhân bị ảnh hưởng | DevOps + Backend |
| T+8h | Báo cáo sơ bộ cho khách hàng (clinic admin) | PO |
| T+24h | Báo cáo chi tiết cho khách hàng | PO + DevOps |
| T+72h | Nếu có data breach: báo cáo Bộ Y tế / Cục CNTT | Legal + PO |
| T+30 ngày | Báo cáo đầy đủ với biện pháp khắc phục | DevOps + PO |

### Template báo cáo cho khách hàng (clinic)

```
Kính gửi Ban Quản Lý [Tên Phòng Khám],

Chúng tôi phát hiện sự cố bảo mật xảy ra vào [thời gian] ảnh hưởng đến dữ liệu của phòng khám quý vị.

THÔNG TIN SỰ CỐ:
- Thời gian phát hiện: [HH:MM DD/MM/YYYY]
- Loại sự cố: [Mô tả ngắn]
- Dữ liệu có thể bị ảnh hưởng: [Danh sách loại dữ liệu]
- Số bệnh nhân ước tính: [N]

HÀNH ĐỘNG ĐÃ THỰC HIỆN:
1. [Action 1]
2. [Action 2]

BIỆN PHÁP PHÒNG NGỪA:
[...]

Chúng tôi cam kết cập nhật thông tin đầy đủ trong [X] ngày.
Mọi thắc mắc, vui lòng liên hệ: [email] hoặc [phone]

Trân trọng,
Đội Kỹ Thuật Pro-Diab HIS
```

---

## 4. Evidence Preservation

**Không bao giờ xóa log khi đang điều tra.**

```bash
# Export toàn bộ log ra file
INCIDENT_ID="INC-$(date +%Y%m%d-%H%M)"
mkdir -p /tmp/evidence/${INCIDENT_ID}

docker compose logs --timestamps > /tmp/evidence/${INCIDENT_ID}/docker-all.log
docker compose logs --timestamps nginx  > /tmp/evidence/${INCIDENT_ID}/nginx.log
docker compose logs --timestamps backend > /tmp/evidence/${INCIDENT_ID}/backend.log
docker compose logs --timestamps mysql  > /tmp/evidence/${INCIDENT_ID}/mysql.log

# Export nginx access log
cp /var/log/nginx/access.log /tmp/evidence/${INCIDENT_ID}/
cp /var/log/nginx/error.log  /tmp/evidence/${INCIDENT_ID}/

# Lưu trữ lên MinIO (bucket không public)
mc cp --recursive /tmp/evidence/${INCIDENT_ID}/ \
  prodiab/prodiab-backup/security-evidence/${INCIDENT_ID}/

echo "Evidence lưu tại: prodiab/prodiab-backup/security-evidence/${INCIDENT_ID}/"
```

---

## 5. Hardening sau Incident

Sau khi resolve, thực hiện hardening checklist:

- [ ] Rotate tất cả secrets và passwords
- [ ] Review và tăng cường rate limiting
- [ ] Enable 2FA cho tất cả admin accounts
- [ ] Audit toàn bộ user accounts — xóa tài khoản không cần thiết
- [ ] Review RBAC — principle of least privilege
- [ ] Chạy OWASP ZAP full scan
- [ ] Chạy dependency audit (`dotnet list package --vulnerable`, `npm audit`)
- [ ] Review nginx config — block thêm nếu cần
- [ ] Update WAF rules
- [ ] Training nhân viên về phishing / social engineering (nếu relevant)
- [ ] Update runbook với lessons learned

---

## 6. Contact List Khẩn Cấp

| Tổ chức | Liên hệ | Khi nào |
|---------|---------|---------|
| GitHub Security | security@github.com | Nếu repo bị compromise |
| Cloudflare | Dashboard | DDoS mitigation |
| Nhà cung cấp VM | Support portal | VM/infra issue |
| Cục CNTT - BYT VN | [TBD] | Data breach có > 1000 bệnh nhân |
| VNCERT | vncert@mic.gov.vn | Security incident nghiêm trọng |
