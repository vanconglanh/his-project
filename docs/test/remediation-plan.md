# Remediation Plan — Sau CRUD E2E R3

> Tổng hợp 6 yêu cầu user sau khi xem [crud-evidence.md](./crud-evidence.md) (R3: 22/42 PASS, 14/15 module).

## Phạm vi

### #1 Kê đơn thiếu Bệnh nhân + Bác sĩ
**Hiện trạng:** form `/prescriptions/new` thiếu 2 field bắt buộc (`patient_id` + `doctor_id`).
**Cần:** thêm 2 autocomplete searchable, BE đã có endpoint `/patients/search` + danh sách user role `bac_si`.
**Owner:** Frontend

### #2 Module FAIL cần xử lý
| Module | Lỗi từ R3 |
|---|---|
| BHYT Export | LIST 0/1 PASS (lỗi từ R3) — verify endpoint + UI |
| Cashier | LIST timeout 20s — `/api/v1/cashier/shift` chậm hoặc lỗi handler |
| ServiceCatalog | LIST FAIL — verify endpoint + UI |
| Supplier | LIST FAIL — verify endpoint + UI |

**Owner:** Backend (timeout/query) + Frontend (selector regex/list page nếu có)

### #3 UI gaps — thêm button còn thiếu
Từ `ui-gap-backlog.md`:
- Patient row: dropdown Sửa/Xoá đã có (verify selector spec)
- Pharmacy Dispense: tab History + Hoàn trả + nút MarkComplete
- BHYT Export: ViewDetail page
- Encounter detail: nút Add Vital Signs + Add Diagnosis (tab/section)
- AdminRoles: nút Tạo vai trò mới
- Supplier: nút Update inline

**Owner:** Frontend

### #4 Layout input đồng nhất + full screen
**Hiện trạng:** một số form input nằm trong dialog nhỏ (PatientEditor, EncounterCreate), một số fullpage (/patients/new).
**Cần:** Designer (Linh) audit tất cả input screen → spec layout chuẩn:
- Form bệnh nhân/lượt khám/kê đơn/billing item → **fullpage** (route riêng `/xxx/new`, `/xxx/[id]/edit`)
- Form ngắn (lock user, void bill) → giữ dialog
- Token spacing/grid/section header chuẩn

**Owner:** Designer → spec MD → Frontend implement

### #5 Report screens chạy được + screenshot evidence
**Hiện trạng:** `/reports/print/[type]` đã có (Financial/Clinical/Pharmacy). Patient Journey 9/9 PASS có cover step in báo cáo.
**Cần:** spec E2E mới `e2e/reports.spec.ts`:
- Login → vào `/reports`
- Click 3 tab → screenshot dashboard chart
- Click "Xem trước & In" mỗi loại → screenshot preview A4
- Click "Tải PDF" → verify download
- Doctor KPI, Diabetes Cohort, Top Drugs widget render đúng

Tạo `docs/test/reports-evidence.md` với screenshot.

**Owner:** Tester (Phượng)

### #6 Architect + PO check tổng thể
- **Architect (Lành):** review schema vs API contract, đảm bảo response shape khớp DTO định nghĩa.
- **PO (Đăng):** review user story Tiếp đón → Kê đơn → Thu ngân — acceptance criteria có đầy đủ field bắt buộc không? Đặc biệt kê đơn (#1).
- Output: `docs/review/system-review-2026-05-31.md`

**Owner:** Architect + PO

---

## Thứ tự triển khai (waves)

### Wave A (song song) — 30 phút
- Architect + PO review tổng thể (spec output)
- Designer audit layout input + viết token spec

### Wave B (song song, sau A) — 1.5 giờ
- Backend fix 4 module FAIL (#2)
- Frontend implement: Kê đơn form (#1) + Designer layout fixes (#4) + UI gap buttons (#3)

### Wave C (sequential, sau B) — 30 phút
- Tester chạy CRUD R4 + Report E2E + screenshot evidence (#5)
- Generate evidence MD mới

### Wave D — commit + memory
- Commit toàn bộ
- Update MEMORY.md

**Tổng:** ~3-4 giờ team song song.

## Definition of Done

- [ ] CRUD R4 ≥ 30/42 PASS (current 22/42), 0 5xx, 0 crash
- [ ] 4 module FAIL → 0
- [ ] Prescription form có patient + doctor select work
- [ ] Designer spec layout MD + ≥ 3 form refactor fullpage
- [ ] Reports E2E spec + evidence MD
- [ ] Architect + PO review MD
- [ ] All committed + memory updated
