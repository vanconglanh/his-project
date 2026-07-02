# WORKFLOW — Pro-Diab HIS

## Agent roles

| Agent         | Tên     | Model  | Vai trò |
|---------------|---------|--------|---------|
| `po-analyst`  | Đăng    | opus   | Phân tích nghiệp vụ, viết PRD / User Story / AC |
| `architect`   | Lành    | opus   | API contract + ERD + sequence integration |
| `research`    | Lành    | sonnet | Tra cứu ĐTQG, BHYT, FHIR, ICD-10 — không viết code (kiêm vai architect) |
| `designer`    | Linh    | sonnet | Design system, wireframe, token, a11y + **audit nhất quán UI** — không viết code production |
| `backend`     | Thảo    | sonnet | .NET 8 API, Dapper/EF Core, migration |
| `frontend`    | Nam     | sonnet | Next.js 15, shadcn/ui, chart |
| `tester`      | Phượng  | opus   | E2E test theo Acceptance Criteria |
| `qc`          | Chi     | opus   | Final gate: code quality + security |
| `devops`      | Chương  | sonnet | Docker, CI/CD, deploy staging/prod |

---

## Workflow chuẩn cho 1 feature

```
User/PO mô tả yêu cầu
        │
        ▼
  po-analyst (Đăng)       ← viết PRD + Acceptance Criteria
        │                    output: docs/prd/{module}.md
        ▼
  designer (Linh)         ← spec design + token + wireframe (bám design-system-standards.md)
        │                    output: docs/design/{module}-{topic}.md
        ▼
  architect (Lành)        ← API contract + ERD diff
        │                    output: docs/api/{module}.yaml, docs/erd/
        │
        │  (nếu cần research)
        ├──► research (Lành) ← tra cứu chuẩn ngoài (ĐTQG, BHYT, FHIR)
        │
        ▼
  ┌─────────────┬──────────────┐
  │ song song   │              │
  ▼             ▼              ▼
backend       frontend       devops (chuẩn bị infra nếu cần)
(Thảo)        (Nam)
  │             │
  └──────┬──────┘
         ▼
  tester (Phượng)         ← E2E test theo AC
         │
   PASS  │  FAIL → quay lại dev agent → lặp
         ▼
    qc (Chi)              ← review code + security check
         │
 APPROVE │  BLOCK → quay lại dev agent → tester → qc
         ▼
  devops (Chương)         ← merge dev → main + deploy
         ▼
     Production ✅
```

### Nguyên tắc parallelism
- `backend` + `frontend` chạy **song song** sau khi có API contract từ architect
- `research` chạy song song với `architect` khi cần đào sâu chuẩn ngoài
- `tester` chỉ chạy **sau khi** backend + frontend xong
- `qc` chỉ chạy **sau khi** tester PASS
- Chỉ `devops` merge `dev → main`

---

## PO Checklist — Trước khi viết ticket

Mỗi yêu cầu phải qua checklist này trước khi thành ticket chính thức.

### 1. Completeness — Nội dung đầy đủ
- [ ] Tên feature rõ, không mơ hồ
- [ ] Actor xác định: ai (vai trò gì) làm gì trong ngữ cảnh nào
- [ ] Trigger → Action → Result đủ 3 phần
- [ ] Acceptance Criteria dạng Given/When/Then
- [ ] Module liên quan: Reception / Encounter / Pharmacy / ...

### 2. Reality check — Phù hợp thực tế nghiệp vụ y tế
- [ ] Có happy path + ít nhất 1 edge case
- [ ] Trường hợp BHYT vs viện phí có khác nhau không?
- [ ] Trường hợp bệnh nhân quay lại (tái khám) xử lý sao?
- [ ] Có ràng buộc luật/thông tư BYT không (TT 27/2021, QĐ 4750)?

### 3. Integration — Liên kết module
- [ ] API endpoint nào bị ảnh hưởng / cần thêm?
- [ ] Schema DB: thêm bảng / cột / index gì?
- [ ] RBAC: role nào được truy cập?
- [ ] Có push lên ĐTQG / xuất XML BHYT không?
- [ ] Audit log: cần ghi gì?

### 4. System impact — Ảnh hưởng tổng thể
- [ ] Tính năng có đổi behavior tính năng cũ không?
- [ ] Race condition (đặc biệt thu ngân, cấp phát thuốc)?
- [ ] Performance: query nặng → cần index/cache?
- [ ] Rollback plan nếu deploy lỗi?

### 5. Risk — Rủi ro & phụ thuộc
- [ ] Phụ thuộc ticket khác chưa xong?
- [ ] Cần seed/migration trước deploy?
- [ ] Impact nếu lỗi production (critical / minor)?

### 6. UX — Trải nghiệm người dùng
- [ ] Lễ tân thao tác bằng bàn phím được không (tránh chuột nhiều)?
- [ ] Loading / error state rõ ràng?
- [ ] Touch target ≥ 44px cho tablet?
- [ ] Mobile/tablet responsive?

### 7. Definition of Done
- [ ] Code merge vào `dev` qua PR, ≥1 reviewer approve
- [ ] Tester PASS theo AC
- [ ] QC approve
- [ ] Không lỗi console / TypeScript / lint
- [ ] Audit log hoạt động (nếu yêu cầu)
- [ ] Đã deploy staging và xác nhận hoạt động

---

## Quy tắc branch

```
main     ← production (chỉ merge từ dev qua devops)
  │
dev      ← integration (merge feature)
  │
feature/{module}-{desc}   ← nhánh task
```

## Prefix commit message

| Prefix      | Khi dùng |
|-------------|----------|
| `feat:`     | Tính năng mới |
| `fix:`      | Sửa bug |
| `refactor:` | Cải thiện code |
| `style:`    | UI/CSS |
| `chore:`    | Config, package, CI |
| `docs:`     | Tài liệu |
| `db:`       | Migration / seed |

---

## Roadmap milestones

| Milestone | Scope                                                       |
|-----------|-------------------------------------------------------------|
| M1 — Core | Auth, Tenant, Users, Patient, Reception, Encounter cơ bản  |
| M2 — Rx   | Prescription + tích hợp Đơn thuốc Quốc gia                 |
| M3 — Pharm| Kho thuốc đầy đủ (nhập/xuất/tồn/lô/HSD)                    |
| M4 — Cash | Thu ngân, hóa đơn, công nợ                                  |
| M5 — BHYT | Export XML giám định BHYT                                   |
| M6 — BI   | Dashboard, chart, báo cáo, phân tích                        |
| M7 — FHIR | Map data sang FHIR R4, expose `/fhir/` endpoints           |
