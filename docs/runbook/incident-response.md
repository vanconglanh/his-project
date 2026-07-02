# Incident Response Runbook — Pro-Diab HIS

**Phiên bản:** 1.0 — Sprint 12
**Owner:** DevOps (Chương)
**Review:** Hàng quý

---

## 1. Phân loại Incident

| Mức độ | Định nghĩa | SLA Response | SLA Resolve |
|--------|-----------|-------------|------------|
| **P1 — Critical** | Production down hoàn toàn, mất data, security breach | 15 phút | 4 giờ |
| **P2 — High** | Chức năng quan trọng bị lỗi (kê đơn, thu ngân), ảnh hưởng > 50% user | 30 phút | 8 giờ |
| **P3 — Medium** | Chức năng phụ bị lỗi, performance giảm nhưng vẫn dùng được | 2 giờ | 24 giờ |
| **P4 — Low** | UI glitch, lỗi không ảnh hưởng nghiệp vụ | 1 ngày làm việc | 1 tuần |

### Ví dụ phân loại

**P1:**
- Toàn bộ production down (nginx 502)
- Database không kết nối được
- Data breach / unauthorized access
- Mất dữ liệu bệnh nhân

**P2:**
- Module kê đơn bị lỗi
- Thu ngân không tạo được hóa đơn
- Login không hoạt động
- Backup thất bại 2 ngày liên tiếp

**P3:**
- Dashboard load chậm (> 5s)
- Export PDF bị lỗi
- Một vài API endpoint timeout intermittent
- Cảnh báo disk > 80%

**P4:**
- Lỗi UI nhỏ không ảnh hưởng chức năng
- Typo trong thông báo

---

## 2. Kênh liên lạc

| Kênh | Mục đích |
|------|---------|
| `#ops-alerts` Slack | Alert tự động từ Alertmanager / Sentry |
| `#incident-p1` Slack | War room P1 (tạo khi có incident) |
| `#ops-devops` Slack | DevOps team |
| `#ops-backend` Slack | Backend team |
| PagerDuty | Oncall escalation P1/P2 |

---

## 3. Oncall Rotation

| Tuần | DevOps Oncall | Backend Oncall |
|------|--------------|---------------|
| Tuần 1 | Chương | TBD |
| Tuần 2 | Chương | TBD |

Lịch xoay vòng: cập nhật trong PagerDuty schedule.

**Giờ oncall:**
- Trong giờ hành chính (8:00-18:00): response trong 15 phút (P1)
- Ngoài giờ: P1 only, response trong 30 phút qua PagerDuty

---

## 4. Quy trình xử lý Incident

### 4.1 Phát hiện (Detection)

Incident được phát hiện qua:
1. Alert tự động từ Alertmanager → Slack `#ops-alerts`
2. Alert từ Sentry (error rate spike)
3. Uptime monitoring (Uptime Robot / Better Uptime)
4. Người dùng báo cáo qua support channel

### 4.2 Triage (5-10 phút cho P1/P2)

```
1. Confirm incident thật (không phải false positive)
2. Phân loại P1/P2/P3/P4
3. Assign Incident Commander (IC) — thường là DevOps oncall
4. Tạo incident ticket trong GitHub Issues hoặc Linear
5. Mở war room #incident-[ticket-id] trên Slack (P1/P2)
6. Notify stakeholders theo SLA
```

### 4.3 Contain & Investigate

**P1 — Ngay lập tức:**

```bash
# 1. Kiểm tra status tổng quan
ssh deploy@prod-vm-01
docker compose -f /opt/prodiab/docker-compose.yml ps
docker compose logs --tail=50 --timestamps

# 2. Check resource
df -h && free -h && uptime

# 3. Kiểm tra network
curl -v https://prodiab.example.com/healthz

# 4. Xem Grafana/Loki để tìm root cause
# URL: http://monitoring.prodiab.example.com:3100
```

**Quyết định: Rollback hay Fix-forward?**
- Nếu issue do deploy mới → Rollback ngay
- Nếu issue hạ tầng → Fix-forward
- Nếu không chắc → Rollback trước, investigate sau

**Rollback nhanh:**

```bash
# Rollback image về version trước
docker compose -f /opt/prodiab/docker-compose.yml \
  pull backend:sha-{previous-commit}

docker compose -f /opt/prodiab/docker-compose.yml \
  up -d --no-deps backend

# Verify
curl -f https://prodiab.example.com/healthz
```

### 4.4 Resolve

Khi issue đã fix:
1. Deploy fix hoặc confirm rollback ổn định
2. Smoke test đầy đủ
3. Monitor 30 phút sau fix
4. Đóng war room Slack

### 4.5 Post-mortem

**Mandatory cho P1 và P2 kéo dài > 2 giờ.**

Template post-mortem:

```markdown
## Post-mortem — [Tên incident] — [Ngày]

**Incident ID:** INC-YYYYMMDD-NNN
**Severity:** P1
**Duration:** HH:MM
**IC:** [Tên]

### Timeline
- HH:MM — Alert triggered
- HH:MM — IC assigned
- HH:MM — Root cause identified
- HH:MM — Fix deployed
- HH:MM — Resolved

### Root Cause
[Mô tả kỹ thuật]

### Impact
- User bị ảnh hưởng: ~N
- Revenue impact: ~Xtr (ước tính)
- Data loss: Có / Không

### Bài học rút ra (Action Items)
- [ ] [Action] — Owner — Deadline
- [ ] [Action] — Owner — Deadline

### Điều nên làm lại
[...]

### Điều cần cải thiện
[...]
```

---

## 5. Runbook nhanh theo triệu chứng

| Triệu chứng | Action |
|------------|--------|
| nginx 502 Bad Gateway | Kiểm tra backend container; `docker compose restart backend` |
| nginx 504 Timeout | Kiểm tra DB connection; backend CPU/memory |
| Login loop | Kiểm tra Redis (session store); restart redis |
| High error rate Sentry | Check Sentry issue list; xem log Loki |
| Disk > 90% | Dọn log cũ: `docker system prune --volumes`; xóa backup local cũ |
| Memory OOM | Check `docker stats`; restart service bị leak; tăng mem_limit |
| Database slow | Kiểm tra slow query log; KILL long-running queries |
| SSL cert expired | `certbot renew --nginx`; `docker compose restart nginx` |

---

## 6. Thông báo cho khách hàng

**P1 (down > 15 phút):**
- Gửi email thông báo qua PO trong 30 phút
- Cập nhật status page (nếu có)
- Template: "Hệ thống Pro-Diab HIS đang gặp sự cố kỹ thuật. Chúng tôi đang khắc phục và sẽ cập nhật trong [X] phút."

**Sau khi resolve P1:**
- Gửi email "Hệ thống đã phục hồi lúc HH:MM. Nguyên nhân và biện pháp phòng ngừa sẽ được gửi trong post-mortem."
