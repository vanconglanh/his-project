# CLAUDE.md — Pro-Diab HIS (Hospital Information System)

> Quy tắc dùng chung cho **mọi agent** (architect, backend, frontend, devops, tester, qc, po-analyst, research).
> Đọc file này **trước** khi bắt đầu bất kỳ task nào. Xem thêm `WORKFLOW.md` để biết quy trình phối hợp.

---

## 1. Giới thiệu dự án

**Pro-Diab HIS** là phần mềm quản lý phòng khám đa khoa quy mô nhỏ (2-5 bác sĩ), triển khai dạng **Cloud SaaS multi-tenant**.

### Mục tiêu sản phẩm
1. Layout hiện đại, UX thân thiện cho lễ tân / bác sĩ / dược sĩ / kế toán
2. Dashboard + chart thống kê (doanh thu, lượt khám, top thuốc, công nợ, BHYT)
3. Tích hợp **Đơn thuốc Quốc gia** (donthuocquocgia.vn) — TT 27/2021/TT-BYT
4. Tích hợp **Cổng giám định BHYT** (XML 4210/QĐ 4750)
5. Chuẩn dữ liệu nội bộ theo **HL7 FHIR R4** (Patient, Encounter, MedicationRequest, Observation)
6. Quản lý kho dược đầy đủ: xuất/nhập/tồn/lô/HSD/kiểm kê
7. Phân tích dữ liệu phục vụ ra quyết định (BI nhẹ)

### Phạm vi nghiệp vụ
Tiếp đón → Hồ sơ bệnh nhân → Khám bệnh → CLS (XN/CĐHA) → Kê đơn → Thu ngân → Cấp phát thuốc → Tái khám → Báo cáo BHYT

---

## 2. Tech Stack

| Layer       | Tech                                                       |
|-------------|------------------------------------------------------------|
| Backend     | .NET 8 Web API, Dapper (read) + EF Core (write/migration)  |
| Database    | MySQL 8.0+ (Docker), multi-tenant filter ở application layer |
| Cache/Queue | Redis 7 (session, rate-limit, background job)              |
| Frontend    | Next.js 16 App Router, TypeScript, TailwindCSS, shadcn/ui  |
| Chart       | Recharts + Tremor                                          |
| State/Data  | TanStack Query, Zustand                                    |
| Auth        | JWT + Refresh token, RBAC                                  |
| AI (opt)    | Azure OpenAI GPT-4o (gợi ý chẩn đoán, tóm tắt bệnh án)     |
| Integration | Đơn thuốc QG REST, BHYT XML, HL7 FHIR                      |
| Storage     | MinIO (file CLS, ảnh, PDF đơn thuốc)                       |
| Deploy      | Docker Compose + Nginx, Ubuntu VM                          |
| Monitor     | Sentry + Serilog → Loki/Grafana                            |

---

## 3. Database

**Engine:** MySQL 8.0+ (Docker, InnoDB, `utf8mb4_0900_ai_ci`)

> **Lý do chọn MySQL:** schema gốc từ hệ thống tham chiếu đã chạy production là MySQL 8 dump. Pro-Diab kế thừa thiết kế này để giảm rủi ro chuyển đổi và rút ngắn thời gian go-live. MySQL **không có Row-Level Security native** — multi-tenant enforce ở **application layer** (xem mục Multi-tenant bên dưới).

```
Host:     mysql (compose service) / localhost (dev)
Port:     3306
Database: prodiab_his
User:     prodiab
Password: lấy từ DB_PASSWORD (.env)
Charset:  utf8mb4
Collation: utf8mb4_0900_ai_ci
```

### Quy ước
- Connection string: `backend/appsettings.Development.json` → `ConnectionStrings:DefaultConnection` (provider: `Pomelo.EntityFrameworkCore.MySql`)
- Schema dump production: `db/diab_his_*.sql` (read-only, KHÔNG sửa)
- Migration mới: `db/migrations/NNNN_description.sql` (prefix số tăng dần, idempotent)
- Seed: `db/seeds/`
- **Mỗi bảng nghiệp vụ phải có `tenant_id INT NOT NULL`** + index `(tenant_id, …)`
- Tên bảng UPPERCASE hoặc giữ theo schema dump hiện có (vd `pat_patients`, cột `PATIENT_ID`); bảng MỚI dùng snake_case lowercase (`diab_his_<group>_<entity>`)
- Primary key: `id INT AUTO_INCREMENT PRIMARY KEY` (kế thừa schema cũ). Bảng có yêu cầu phân tán/đồng bộ liên hệ thống → bổ sung cột phụ `uuid CHAR(36) UNIQUE DEFAULT (UUID())`.
- Audit columns: `created_at DATETIME DEFAULT CURRENT_TIMESTAMP`, `created_by INT`, `updated_at DATETIME ON UPDATE CURRENT_TIMESTAMP`, `updated_by INT`, `deleted_at DATETIME NULL` (soft delete)
- Idempotent migration: dùng pattern stored procedure check `information_schema` vì MySQL 8.0.23 KHÔNG hỗ trợ `ADD COLUMN IF NOT EXISTS`:
  ```sql
  DROP PROCEDURE IF EXISTS add_col_if_missing;
  CREATE PROCEDURE add_col_if_missing(...) BEGIN ... END;
  CALL add_col_if_missing('table', 'col', 'INT NULL');
  ```

### Multi-tenant (application-layer, không RLS)
- JWT chứa `tenant_id` + `clinic_id` + `user_id` + `role`
- Middleware `TenantScopeMiddleware` set `HttpContext.Items["TenantId"]` mỗi request
- **EF Core Global Query Filter:** `modelBuilder.Entity<T>().HasQueryFilter(e => e.TenantId == _tenantProvider.TenantId)` cho mọi entity nghiệp vụ
- **Dapper read:** mọi query SELECT bắt buộc có `WHERE tenant_id = @tenantId`; review code phải reject PR thiếu filter
- Insert/Update: service tự gán `tenant_id` từ `ITenantProvider`, không trust input client
- Audit log riêng (`diab_his_sec_audit_logs`) ghi mọi truy cập cross-tenant attempt

---

## 4. Module nghiệp vụ

| Module              | Mô tả                                                       |
|---------------------|-------------------------------------------------------------|
| **Tenant/Clinic**   | Đăng ký phòng khám, cấu hình BHYT, mã CSKCB                |
| **Users & RBAC**    | Admin / BacSi / LeTan / DuocSi / KeToan / KyThuatVien      |
| **Patient**         | Hồ sơ bệnh nhân, BHYT, lịch sử khám                        |
| **Reception**       | Tiếp đón, lấy số, phân phòng                                |
| **Encounter**       | Khám bệnh, chẩn đoán ICD-10, chỉ định CLS                  |
| **LabRad (CLS)**    | Xét nghiệm + Chẩn đoán hình ảnh, kết quả                   |
| **Prescription**    | Kê đơn → đẩy ĐTQG → mã đơn QR                              |
| **Pharmacy**        | Kho thuốc: nhập/xuất/tồn/lô/HSD/kiểm kê/cảnh báo           |
| **Cashier**         | Thu phí, công nợ, hóa đơn điện tử                          |
| **BHYT**            | Export XML 4210, đối soát giám định                        |
| **Report/BI**       | Dashboard, doanh thu, top thuốc, KPI bác sĩ                |
| **Audit Log**       | Mọi thao tác trên dữ liệu bệnh nhân                        |

---

## 5. Tích hợp

### Đơn thuốc Quốc gia (donthuocquocgia.vn)
- Theo TT 27/2021/TT-BYT
- Mỗi tenant có `co_so_kham_chua_benh_id` + token riêng → lưu mã hóa trong `his_tenant_integration`
- Workflow: kê đơn → call API đẩy → nhận `ma_don_thuoc` → in QR code

### Giám định BHYT
- Export XML theo QĐ 4750/QĐ-BYT (hồ sơ bệnh án + đơn thuốc + dịch vụ)
- Module: `Bhyt.ExportService`

### HL7 FHIR R4
- Internal data model — entity nghiệp vụ map được sang FHIR resource
- Resource chính: `Patient`, `Encounter`, `Observation`, `Condition`, `MedicationRequest`, `Procedure`

---

## 6. Quy ước code

### Ngôn ngữ mặc định: TIẾNG VIỆT (bắt buộc cho mọi tầng)

**Áp dụng cho mọi agent (architect, backend, frontend, devops, tester, qc, po-analyst, research) và mọi output sinh ra trong dự án.**

1. **Giao tiếp & tài liệu**
   - Mọi PRD, user story, use case, ADR, README, comment giải thích (khi cần) → **tiếng Việt**
   - Commit message, PR title/description → **tiếng Việt** (vd: `feat(patient): them API tim kiem benh nhan theo BHYT`)
   - Phản hồi của Claude/subagent với user → **tiếng Việt**
   - Tên file tài liệu có thể dùng kebab-case tiếng Anh (vd `feature-matrix.md`) nhưng **nội dung tiếng Việt**

2. **Backend (.NET 8)**
   - Error message trong response JSON → **tiếng Việt có dấu** (vd `"message": "Không tìm thấy bệnh nhân"`)
   - Mã lỗi (`code`) giữ **tiếng Anh SCREAMING_SNAKE** (vd `PATIENT_NOT_FOUND`) để FE i18n
   - Swagger/OpenAPI summary + description → **tiếng Việt**
   - Log message (Serilog) → **tiếng Việt không dấu** hoặc tiếng Anh, KHÔNG dùng dấu trong log để tránh lỗi encoding
   - Validation message (FluentValidation) → **tiếng Việt có dấu**
   - Tên class/method/biến vẫn giữ **tiếng Anh PascalCase/camelCase**

3. **Frontend (Next.js 16)**
   - `next-intl` locale mặc định = `vi`, fallback = `vi` (không fallback sang en)
   - Mọi label, button, toast, empty state, error UI → **tiếng Việt có dấu**
   - File dịch: `frontend/messages/vi.json` (source of truth), `en.json` optional
   - Format số/ngày: locale `vi-VN`, tiền tệ `VND`, ngày `dd/MM/yyyy`

4. **Database**
   - Tên bảng/cột giữ **snake_case tiếng Anh** (vd `his_patient`, `full_name`)
   - Dữ liệu seed (role label, menu, danh mục ICD-10, đơn vị thuốc, lý do khám) → **tiếng Việt có dấu**
   - Collation: `vi-VN-x-icu` cho cột tên/địa chỉ để sort đúng tiếng Việt

5. **Tên định danh code (KHÔNG dịch)**
   - Class, method, biến, route, JSON key, env var → **tiếng Anh**
   - VD: `PatientService.SearchByInsuranceCardAsync()`, không phải `BenhNhanService.TimTheoTheBHYTAsync()`

### REST API
- Base: `/api/v1/{resource}`
- Verb chuẩn: `GET /patients`, `POST /patients`, `GET /patients/{id}`, `PUT /patients/{id}`, `DELETE /patients/{id}`
- Sub-resource: `/patients/{id}/encounters`
- Error envelope:
  ```json
  { "error": { "code": "PATIENT_NOT_FOUND", "message": "...", "details": {} } }
  ```
- Success:
  ```json
  { "data": {...}, "meta": { "page": 1, "total": 100 } }
  ```

### Frontend
- File path: `frontend/app/(dashboard)/patients/page.tsx`
- Component: PascalCase, hook: `useXxx`
- Style: Tailwind utility-first, dùng `cn()` helper từ `lib/utils.ts`
- i18n: `vi` mặc định, `en` optional, dùng `next-intl`

### Bảo mật
- Mã hóa AES-256-GCM cho cột nhạy cảm (CMND, số BHYT, ghi chú bệnh án)
- Audit log mọi `INSERT/UPDATE/DELETE` trên bảng `his_patient`, `his_encounter`, `his_prescription`
- Rate limit: 100 req/phút/user, 1000 req/phút/tenant
- HTTPS bắt buộc, HSTS, CSP headers

---

## 7. Cấu trúc thư mục

```
pro-diab-his/
├── CLAUDE.md
├── WORKFLOW.md
├── .claude/agents/        ← định nghĩa subagent
├── backend/               ← .NET 8 Web API
├── frontend/              ← Next.js 16
├── db/
│   ├── migrations/        ← SQL versioned NNNN_desc.sql
│   └── seeds/
├── docs/
│   ├── prd/               ← PRD theo module
│   ├── api/               ← OpenAPI spec
│   └── erd/               ← ERD diagram
└── docker-compose.yml
```

---

## 8. Branch & Deploy

- Nhánh chính: `main` (production), `dev` (staging)
- Feature branch: `feature/{module}-{short-desc}`
- PR vào `dev` → QC approve → merge → deploy staging
- PR `dev` → `main` chỉ DevOps thực hiện
- Deploy: `./deploy.sh [backend|frontend|all] [staging|prod]`

---

## 9. Glossary

| Thuật ngữ | Giải thích |
|-----------|-----------|
| CSKCB     | Cơ sở khám chữa bệnh |
| ĐTQG      | Đơn thuốc Quốc gia |
| CLS       | Cận lâm sàng (XN + CĐHA) |
| BHYT      | Bảo hiểm y tế |
| ICD-10    | International Classification of Diseases v10 |
| FHIR      | Fast Healthcare Interoperability Resources |
| Tenant    | Một phòng khám trong hệ thống SaaS |
| Encounter | Một lượt khám bệnh |
