# Changelog

All notable changes to **Pro-Diab HIS** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.0.0] — 2026-05-23

### Added

#### Authentication & Security
- JWT + Refresh Token auth with secure httpOnly cookie storage
- RBAC with 7 system roles (Admin, BacSi, LeTan, DuocSi, KeToan, DieuDuong, KyThuatVien)
- TOTP-based Two-Factor Authentication (2FA) with 10 recovery codes
- Password reset via email with 24-hour expiry link
- User invite flow with 48-hour activation window
- AES-256-GCM encryption for sensitive columns (CMND, BHYT card, EMR notes)
- Rate limiting: 100 req/min/user, 1000 req/min/tenant
- Audit log for all INSERT/UPDATE/DELETE on patient, encounter, prescription tables

#### Multi-tenant
- Cloud SaaS architecture with Row-Level Security (RLS) on PostgreSQL
- Tenant provisioning with subdomain routing
- Tenant status management (Active, Suspended, Terminated)
- Per-tenant storage quota (MinIO)
- JWT contains tenant_id + clinic_id + user_id + role

#### Patient Management
- Complete patient CRUD with auto-generated patient codes
- Allergy tracking: allergen, reaction, severity (Mild/Moderate/Severe/Life-Threatening)
- Emergency contacts with relationship types
- Treatment consent tracking
- BHYT (health insurance) card management with validity tracking
- External CLS result uploads (PDF/PNG/JPEG, max 10MB, MinIO storage)
- Patient avatar upload (PNG/JPEG, max 2MB)

#### Reception & Queue
- Real-time patient queue board (5-second polling)
- Priority levels: Normal / Priority / Emergency
- Queue actions: Call In, Skip, Cancel
- Ticket printing
- Daily statistics: waiting, in-progress, done, cancelled, average wait time

#### Encounter & EMR
- Encounter lifecycle: Waiting → In Progress → Done / Cancelled
- 3-column detail layout: patient profile / EMR editor / prescriptions+CLS
- Tiptap rich-text EMR editor with table, image support
- EMR templates (custom templates per clinic)
- Digital signing of EMR (doctor signature)
- Versioned EMR history

#### Vital Signs
- Comprehensive vital signs: temperature, HR, BP, SpO2, RR, weight, height, BMI, glucose, pain scale
- Automatic range validation with warnings
- Historical trend charts per patient

#### Diagnosis
- ICD-10 lookup with full Vietnamese disease names
- Primary and secondary diagnoses per encounter
- Differential diagnosis support

#### Diabetes Module
- Diabetes assessment: HbA1c, fasting/post-meal glucose, eGFR
- Complication tracking (eye, kidney, cardiovascular, neuro, foot)
- Treatment target setting
- HbA1c trend chart
- Dashboard: HbA1c distribution chart, complication rate chart

#### Lab/Rad (CLS)
- Lab orders with priority (Normal/Urgent)
- Radiology orders: modality, body part, contrast
- Lab result entry with reference ranges and abnormal flags (L/H/LL/HH/CRITICAL)
- Lab result verification by KTV (technician)
- CSV/HL7 ORU result import
- DICOM image upload for radiology
- External lab partner integration (REST/HL7 MLLP)
- Lab integration dashboard with outbound/inbound message tracking

#### Prescriptions & DTQG
- Prescription creation with drug autocomplete
- Drug-drug interaction (DDI) warnings
- Doctor digital signing of prescriptions
- Automatic push to Đơn thuốc Quốc gia (donthuocquocgia.vn) per TT 27/2021
- QR code generation for prescriptions

#### Pharmacy & Inventory
- Drug catalog with import from Excel
- Sync from National Drug Database (DTQG)
- Multi-warehouse support (Main, Dispensing, Cold Chain, Narcotic)
- Goods Receipt Notes (GRN) with lot/batch tracking and expiry dates
- FEFO (First Expired First Out) dispensing logic
- Dispensing queue with batch selection
- Stock adjustment / physical inventory count
- Low stock and near-expiry alerts
- Purchase orders
- Supplier management

#### Cashier & Billing
- Automatic invoice generation on encounter completion
- Payment methods: Cash, Bank Transfer, VietQR, MoMo, VNPay, Visa/Mastercard
- QR payment with webhook confirmation
- Shift management: open/close with cash reconciliation
- Accounts receivable (partial payments / debt tracking)
- Invoice void/refund with reason
- E-Invoice (HĐĐT) issuance via third-party provider

#### BHYT
- XML export per QĐ 4750/QĐ-BYT (5 tables: XML 1-5)
- XSD validation before submission
- Digital signing of XML package
- Submission to BHYT portal
- Reconciliation result upload and dispute workflow
- BHYT statistics chart (approved vs. rejected amounts)

#### Reports & Dashboard
- KPI cards: today's revenue, encounters, new patients, prescriptions
- 30-day revenue trend chart (Recharts)
- 30-day encounter trend chart
- Top 10 doctors by revenue (horizontal bar chart)
- Top 10 drugs by revenue
- Financial reports: daily/monthly/quarterly, by payment method
- Clinical reports: doctor KPI, top ICD-10 diagnoses
- Pharmacy reports: top drugs, inventory alerts
- Export to Excel and PDF

#### Patient Portal
- OTP-based authentication via SMS (no password required)
- View personal profile, insurance card
- Visit history
- Prescription download (PDF)
- Lab result viewing with trend charts
- Appointment booking and cancellation

#### API Partners
- Public API key management with scopes
- Rate limiting and daily quota per partner
- Request log viewer
- Webhook log viewer

#### Infrastructure
- Next.js 15 App Router with TypeScript
- TailwindCSS v4 + shadcn/ui design system
- TanStack Query for server state
- Zustand for UI state
- next-intl i18n (Vietnamese default, English)
- Sentry error tracking (frontend + backend)
- Serilog → Loki/Grafana logging pipeline
- Docker Compose with Nginx reverse proxy

#### v1.0 Polish (Sprint 13)
- Tablet responsive layout (768-1024px): sidebar auto-collapses at < 1024px
- DataTable horizontal scroll on mobile
- Command Palette (Ctrl+K/Cmd+K): patient search, navigation, quick actions
- Recent items and localStorage persistence for command palette
- Vim-style keyboard navigation (g+p, g+e, g+r, g+c, g+h)
- Shortcuts modal (? key) with full shortcut reference
- Empty state components with icons and CTA for all list views
- Skeleton loading: table, card grid, chart, form variants
- Route-level error boundary (Next.js error.tsx)
- Print stylesheet: prescriptions, billing receipts, BHYT export
- PWA manifest with app shortcuts
- Medical color theme (teal primary palette)
- Optimized chart color palette for medical data visualization
- Complete i18n: vi.json + en.json with all keys for every page
- User guide documentation (8 guides: getting started, reception, doctor, nurse, pharmacy, cashier, admin, portal)

---

## [Unreleased]

### Added
- Trang in **Phiếu chỉ định cận lâm sàng** (`/encounters/[id]/cls-print`): gộp lab-orders + rad-orders của lượt khám thành một phiếu A4, có letterhead theo tenant, nút "In phiếu chỉ định CLS" trong chi tiết lượt khám.
- **HTTP client ĐTQG** (`HttpDtqgClient` + `DtqgOptions`, backend Infrastructure): gọi cổng donthuocquocgia.vn qua named `HttpClient`, bật/tắt bằng cấu hình `Dtqg:Enabled` (mặc định `false` → vẫn dùng `MockDtqgClient` cho dev/sandbox).
- **Token ĐTQG per-tenant** (`IDtqgCredentialProvider` + `DtqgCredentialProvider`): resolve token xác thực theo tenant hiện tại — đọc `diab_his_int_dtqg_credentials` + **giải mã AES-256-GCM** (`IEncryptionService.Decrypt`), đặt `Authorization: Bearer` per-request (fallback token cấu hình); tự điền `cskcb_id`/`partner_code` khi payload rỗng. Backend build pass (Infrastructure + Api) + **12 unit test** (`HttpDtqgClientTests`) pass. Khi go-live cần: spec API chính thức (path/schema) + ký USB token thật.
- **Dữ liệu đơn thuốc ĐTQG** (`IDtqgPrescriptionPayloadBuilder` + `DtqgPrescriptionPayloadBuilder`): dựng trường `don_thuoc` (bệnh nhân + chẩn đoán ICD-10 chính + danh sách thuốc kèm hoạt chất/mã ĐTQG) từ schema canonical, giải mã số thẻ BHYT; + **5 unit test** cho ánh xạ thuần `MapPayload`.
- Bộ trang trạng thái dự án (`docs/status/`): `dashboard-status.html`, `lo-trinh.html`, thư viện 7 template in print-ready (`bieu-mau-in/`, khổ A4/A5/K80/K58).
- **Vital Signs Trend API** (`GET /api/v1/patients/{patientId}/vital-signs/trend`): thống kê sinh hiệu theo bệnh nhân — 8 metric (glucose, HA tâm thu/trương, mạch, nhịp thở, SpO2, cân nặng, nhiệt độ), trả count/min/max/average/first/latest + series theo thời gian, lọc `date_from`/`date_to` (biên inclusive). Contract: `docs/api/openapi/vital-signs.yaml`. Verify: **21/21 unit test** pass (hardening qua review đối kháng 4 lens). Evidence: `docs/test-reports/2026-07-02_vital-signs-trend.md`.
- **Bộ mô phỏng phòng khám (clinic sim)**: harness Playwright `frontend/e2e/sim/` (50 persona × 5 ngày, agent Lễ tân/Bác sĩ/Dược/Thu ngân + 10 kịch bản ngoại lệ), seed tenant test `db/seeds/diab_test_tenant.sql` (`DIAB-TEST`, tenant_id=2), biểu mẫu evidence `docs/test/clinic-sim-evidence.md` + gallery 14 ảnh `docs/test/evidence.html`. **Kết quả chạy live 02/07/2026** (MySQL 8.0.39 portable + backend :5000 + frontend :3100): **6/6 BN đi trọn luồng lâm sàng trên UI thật** — tiếp đón → khám → sinh hiệu → chẩn đoán → ký bệnh án → đóng lượt → kê đơn → ký & gửi ĐTQG → mở ca thu ngân (103 bước: 87 PASS / 1 FAIL / 15 SKIP); bước cấp phát/thu tiền SKIP chờ rework Dispensing.

### Changed
- **Database:** chuyển nền tảng dữ liệu sang **MySQL 8** + multi-tenant ở application layer (EF Core Global Query Filter) theo ADR 0007 (Direction B). Các mô tả PostgreSQL 17 + Row-Level Security ở tài liệu v1.0 phản ánh trạng thái tại thời điểm phát hành, không còn đúng với kiến trúc hiện tại.
- **Frontend:** nâng lên **Next.js 16** (16.2.6, App Router + Turbopack).

### Fixed
- **5 bug thật do sim phòng khám phát hiện (02/07/2026):** ① thiếu view `sec_permissions`/`sec_role_permissions` (9009 bỏ sót) → `JwtService` nuốt lỗi, token vai trò thật mất claim quyền — mọi role thật 403 (mig `9022`); ② check-in query cột `queue_number` không tồn tại + `patient_id` sai kiểu INT→GUID → tiếp đón 500 (mig `9023` + code); ③ `/drugs/search` trỏ view cũ thiếu `name_vi` + seed rỗng → không kê được thuốc (code + data); ④ `/reception/queue` cùng gốc lỗi `queue_number` (code); ⑤ `drug_id`/`prescription_items` lệch kiểu INT↔GUID → thêm dòng thuốc 400/500 (mig `9024` + code). *Còn lại:* module **Dispensing** xây trên khóa INT lệch GUID → cấp phát/thu tiền cần rework riêng (bug #6); mã permission seed (`reception.create`) lệch mã controller (`reception.checkin`) cần rà soát.
- **VitalSignsTrend ordering:** khi trùng `RecordedAt`, First/Latest/Series không xác định trên SQL thật → thêm tie-break `OrderBy(RecordedAt).ThenBy(CreatedAt).ThenBy(Id)` (phát hiện qua review đối kháng, chốt bằng test ties).
- **Luồng gửi ĐTQG (`SubmitDtqgFromPrescriptionHandler`):** trước đây query/ghi nhầm bảng legacy `pha_prescriptions` (PK INT) + so sánh Guid với cột INT + cột sai `primary_icd10_code`, nên **không tìm thấy đơn thuốc tạo bởi CRUD** (ghi vào canonical `diab_his_pha_prescriptions` PK CHAR(36)) → sửa toàn bộ read/update sang schema canonical (đơn/dòng thuốc/chẩn đoán ICD-10) và điền `don_thuoc` bằng dữ liệu thật. Backend build + **35 unit test ĐTQG** pass. *Còn lại:* bảng `diab_his_int_dtqg_submissions.prescription_id` xung đột schema INT (0011) vs CHAR(36) (9011) — insert tracking đã bọc `try/catch`, cần reconcile trên DB thật.

### Planned for v1.1
- AI diagnostic suggestions (Azure OpenAI GPT-4o integration)
- EMR summarization AI
- Tích hợp Đơn thuốc Quốc gia (ĐTQG) production — thay MockDtqgClient bằng HTTP client thật gọi donthuocquocgia.vn + ký số USB token (TT 27/2021)
- Đồng bộ danh mục thuốc/dược Quốc gia (drug master sync từ ĐTQG)
- Advanced BI dashboards with custom date ranges
- Batch BHYT reconciliation processing
- Drug interaction database updates automation

[1.0.0]: https://github.com/prodiab/pro-diab-his/releases/tag/v1.0.0
