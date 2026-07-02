# UI Action Gap Backlog

Tổng hợp các action UI còn thiếu hoặc lỗi, phát hiện qua [CRUD E2E test](./crud-evidence.md) (44 actions, 12/15 modules, PASS=19 / SKIP=22 / FAIL=3).

## Ưu tiên P1 — Critical user workflow (đợt 1)

### Patient (`/patients`)
- [ ] **Row inline action**: dropdown 3-chấm hoặc cụm button `Chi tiết` / `Sửa` / `Xoá` trên mỗi row trong list
- [ ] Click `Chi tiết` → navigate `/patients/{id}` (page đã có sẵn)
- [ ] Click `Sửa` → mở `PatientEditorLayout` mode `edit`
- [ ] Click `Xoá` → confirm dialog → `DELETE /api/v1/patients/{id}` (soft delete)
- BE endpoint đã có: `GET /patients/{id}`, `PUT /patients/{id}`, `DELETE /patients/{id}`

### Billing (`/billings`)
- [ ] Mỗi bill có nút `Xem chi tiết` → mở dialog/page hiển thị items
- [ ] Nút `Thêm dịch vụ` trong detail → mở form add billing item
- [ ] Nút `Cập nhật trạng thái` (Draft → Finalized → Paid → Voided)
- BE đã có: `GET /billings/{id}`, `POST /billings/{id}/items`, `PUT /billings/{id}/status`

### BHYT Export (`/bhyt`)
- [ ] Nút `Tạo export mới` → mở form chọn period (from/to date, optional clinic)
- [ ] Submit → `POST /bhyt/exports` → redirect detail
- [ ] Mỗi row có nút `Chi tiết` → mở page export detail với danh sách items
- BE đã có: `POST /bhyt/exports`, `GET /bhyt/exports/{id}`

### Admin Users (`/admin/users`)
- [ ] Nút `Mời người dùng` ở header → dialog form (email + full_name + role_codes)
- [ ] Submit → `POST /api/v1/users/invite`
- [ ] Mỗi row có dropdown: `Đổi vai trò` (đã có), `Khoá tài khoản` / `Mở khoá`, `Xoá`
- BE đã có: `POST /users/invite`, `PATCH /users/{id}/lock`, `DELETE /users/{id}`

## Ưu tiên P2 — Operational (đợt 2)

### Pharmacy Dispense (`/pharmacy/dispense`)
- [ ] Tab `Hàng chờ` + tab `Lịch sử` + tab `Hoàn trả`
- [ ] Trong tab Hàng chờ: mỗi đơn có nút `Phát thuốc` → form chọn items + quantity → `POST /pharmacy/dispense/{prescriptionId}`
- [ ] Tab Lịch sử: list các đợt phát đã hoàn tất
- BE đã có: `GET /pharmacy/dispense/queue`, `POST /pharmacy/dispense/{id}`, `GET /pharmacy/dispense/history`

### Cashier (`/cashier`)
- [ ] Fix LIST timeout (test #8 FAIL) — investigate `useCashierShift` hook + endpoint
- [ ] Mở bill cần thanh toán → click `Thanh toán` → form chọn phương thức (CASH/CARD/QR) → submit
- [ ] Nút `In hóa đơn` sau khi thanh toán xong
- BE đã có: `GET /cashier/shift`, `POST /payments`

### Encounter (`/encounters/{id}`)
- [ ] Tab/section `Sinh hiệu` với form ghi vital signs (mạch, HA, nhiệt, cân)
- [ ] Tab/section `Chẩn đoán` với autocomplete ICD-10 + add multiple diagnoses
- BE đã có: `POST /encounters/{id}/vital-signs`, `POST /encounters/{id}/diagnoses`

## Ưu tiên P3 — Admin (đợt 3, ít critical)

### Admin Roles (`/admin/roles`)
- [ ] Nút `Tạo vai trò` → form (code, name, description, permission_codes multiselect)
- [ ] Click row → mở edit permission matrix

### Admin Tenants (`/admin/tenants`)
- [ ] Nút `Tạo phòng khám` → form
- [ ] Mỗi row có dropdown: `Tạm ngừng` / `Kích hoạt`, `Xoá`

### Supplier (`/admin/suppliers`)
- [ ] Nút `Thêm nhà cung cấp` → form
- [ ] Mỗi row có Sửa/Xoá

## Bug realtime (đã catch)

### FAIL — Cashier LIST timeout 20s
- Endpoint: `GET /api/v1/cashier/shift`
- Có thể: query chậm, hoặc FE render loop. Cần xem console log + perf trace.

### FAIL — AdminUsers UpdateRole timeout
- Có thể trigger dropdown chậm hoặc network race condition.

## Strategy implement

1. **Đợt 1 (P1)** ~3-4 giờ: 4 module critical, mỗi module +2-4 button + dialog
2. **Đợt 2 (P2)** ~2-3 giờ: 3 module ops
3. **Đợt 3 (P3)** ~1-2 giờ: 3 module admin
4. **Mỗi đợt xong**: re-run `crud-actions.spec.ts` → đếm % PASS tăng
5. Target cuối: ≥ 80% PASS (35+/44 action)

## Pattern dùng chung

Component reusable nên tạo:
- `<RowActionsDropdown />` — 3-chấm dropdown với items: Chi tiết / Sửa / Xoá (theo permission)
- `<ConfirmDeleteDialog />` — wrap AlertDialog + callback delete
- `<CreateEntityDialog />` — wrapper dialog cho mọi form CREATE

Pattern hiện có để reuse:
- `EncountersPageClient.tsx` đã có `CreateEncounterDialog` — copy structure
- `UserMenu.tsx` đã có `DropdownMenuGroup` — copy cho RowActions
