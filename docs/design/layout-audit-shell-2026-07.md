# Audit layout khung + panel + dialog + typography — Pro-Diab HIS

> Đối chiếu `docs/design/design-system-standards.md` (mục 2 Typography, mục 3 Spacing·Layout·Panel, mục 4 Component & Density).
> Người audit: Linh (designer) · Ngày: 2026-07-02 · Phạm vi: `frontend/app/(dashboard)/**`, `frontend/components/layout/**`, `frontend/components/ui/**`, `frontend/components/domain/**` (Dialog/Sheet).
> Chỉ đọc code, không sửa. Mọi finding đã đọc tận mắt file:dòng nêu bên dưới.

## Trạng thái xử lý (cập nhật 2026-07-02, cùng ngày)

| Finding | Trạng thái | Ghi chú |
|---------|-----------|---------|
| L-01 Topbar 64px | ✅ Đã sửa | `h-16` → `h-14` |
| L-02 Sidebar collapsed 56px | ✅ Đã sửa | `w-14` → `w-16` |
| L-03 Sheet thiếu `px-6 pb-6` | ✅ Đã sửa | 10/10 vị trí; 4 Sheet detail admin dùng `sm:max-w-xl` (theo F-05 audit form) |
| L-04 Sheet width arbitrary px | ✅ Đã sửa | Về thang `sm:max-w-xl`/`2xl` |
| L-05 Dialog max-w ngoài thang | ✅ Đã sửa | `max-w-lg`→`max-w-xl`; dialog chứa bảng→`max-w-4xl` |
| L-06 PatientForm ép Dialog 6xl | ⏳ Refactor đợt sau | Gộp luồng với `/patients/new` (xem F-01 audit form) |
| L-07 Page title `text-2xl` | ✅ Đã sửa | 36/36 vị trí → `text-xl font-bold`; ngoại lệ giữ lại: ô nhập OTP `account/security:295` (không phải title) |
| L-08 KPI không qua token | ✅ Đã sửa | Về `text-[length:var(--text-kpi)] … tabular-nums` |
| L-09 Card/Dialog `rounded-xl` | ✅ Đã sửa | Cả `card.tsx` + `dialog.tsx` → `rounded-lg` |
| L-10 Panel trộn radius | ✅ Đã sửa | 7 vị trí → `rounded-lg` |
| L-11 Bảng dài `py-3` | ✅ Đã sửa | 3 màn → `py-2` dense |
| L-12 Skeleton `p-8` | ✅ Đã sửa | → `p-6` |
| L-13 Select width arbitrary | ✅ Đã sửa | → `w-36/w-40/w-44` |
| L-14 Badge `text-[10px]` | ✅ Đã sửa | → `text-xs` |

> Nghiệm thu: `npm run build` (Next.js 16) PASS sau toàn bộ batch.

---

### L-01 — Topbar cao 64px thay vì 56px chuẩn

| Mức | File:dòng | Hiện tại | Chuẩn đúng | Sửa thành |
|-----|-----------|----------|-----------|-----------|
| P1 | `frontend/components/layout/AppTopbar.tsx:38` | `<header className="sticky top-0 z-30 flex h-16 shrink-0 items-center gap-3 border-b bg-background/95 backdrop-blur px-4">` — `h-16` = 64px | mục 3: "Topbar — 56px" | Đổi `h-16` → `h-14` (56px), rà lại padding icon/button bên trong để không bị vỡ layout |

---

### L-02 — Sidebar thu gọn (collapsed) 56px thay vì 64px chuẩn

| Mức | File:dòng | Hiện tại | Chuẩn đúng | Sửa thành |
|-----|-----------|----------|-----------|-----------|
| P2 | `frontend/components/layout/AppSidebar.tsx:58` | `sidebarCollapsed ? "w-14" : "w-60"` — `w-14` = 56px | mục 3: "Sidebar trái — 64px (collapsed)" | Đổi `w-14` → `w-16` (64px) khi collapsed; `w-60` (240px) expanded đã đúng, giữ nguyên |

---

### L-03 — Sheet/Drawer thiếu `px-6 pb-6` bắt buộc (10/11 chỗ dùng)

Base component `frontend/components/ui/sheet.tsx` (`SheetContent`, dòng 39-81) **không** có sẵn `px-6 pb-6` mặc định — mỗi màn phải tự thêm. Chỉ 1/11 nơi làm đúng.

| Mức | File:dòng | Hiện tại | Chuẩn đúng | Sửa thành |
|-----|-----------|----------|-----------|-----------|
| P1 | `frontend/app/(dashboard)/admin/users/page.tsx:254` | `<SheetContent className="w-[480px] sm:w-[480px] overflow-y-auto">` | mục 3: Sheet "luôn `px-6 pb-6`" | `className="w-full sm:max-w-2xl overflow-y-auto px-6 pb-6"` |
| P1 | `frontend/app/(dashboard)/admin/tenants/page.tsx:282` | `<SheetContent className="w-[480px] sm:w-[480px] overflow-y-auto">` | nt | `className="w-full sm:max-w-2xl overflow-y-auto px-6 pb-6"` |
| P1 | `frontend/app/(dashboard)/admin/roles/page.tsx:139` | `<SheetContent className="w-[600px] sm:w-[600px] overflow-y-auto">` | nt | `className="w-full sm:max-w-2xl overflow-y-auto px-6 pb-6"` |
| P1 | `frontend/app/(dashboard)/admin/audit/page.tsx:156` | `<SheetContent className="w-[500px] sm:w-[500px] overflow-y-auto">` | nt | `className="w-full sm:max-w-2xl overflow-y-auto px-6 pb-6"` |
| P1 | `frontend/app/(dashboard)/labrad/_components/RadResultsTab.tsx:145` | `<SheetContent side="right" className="w-full sm:max-w-lg overflow-y-auto">` | nt | thêm `px-6 pb-6` |
| P1 | `frontend/app/(dashboard)/labrad/_components/LabResultsTab.tsx:137` | `<SheetContent side="right" className="w-full sm:max-w-lg overflow-y-auto">` | nt | thêm `px-6 pb-6` |
| P1 | `frontend/app/(dashboard)/labrad/_components/LabPartnersTab.tsx:125` | `<SheetContent side="right" className="w-full sm:max-w-lg overflow-y-auto">` | nt | thêm `px-6 pb-6` |
| P1 | `frontend/app/(dashboard)/labrad/results/[id]/_components/LabResultDetailClient.tsx:184` | `<SheetContent side="right" className="w-full sm:max-w-lg overflow-y-auto">` | nt | thêm `px-6 pb-6` |
| P1 | `frontend/app/(dashboard)/nurse/_components/NursePageClient.tsx:148` | `<SheetContent side="right" className="w-full sm:max-w-xl overflow-y-auto">` | nt | thêm `px-6 pb-6` |
| P1 | `frontend/components/domain/VitalSignsHistoryDrawer.tsx:29` | `<SheetContent side="right" className="w-full sm:max-w-xl overflow-y-auto">` | nt | thêm `px-6 pb-6` |

**Mẫu đúng duy nhất (dùng làm tham chiếu):** `frontend/app/(dashboard)/drugs/_components/DrugsPageClient.tsx:224` → `<SheetContent className="w-full sm:max-w-2xl overflow-y-auto px-6 pb-6">`.

---

### L-04 — Sheet dùng chiều rộng arbitrary px cố định thay vì thang chuẩn

| Mức | File:dòng | Hiện tại | Chuẩn đúng | Sửa thành |
|-----|-----------|----------|-----------|-----------|
| P2 | `frontend/app/(dashboard)/admin/users/page.tsx:254` | `w-[480px] sm:w-[480px]` | mục 3: Sheet `sm:max-w-2xl` (thang Tailwind, không arbitrary) | `w-full sm:max-w-2xl` |
| P2 | `frontend/app/(dashboard)/admin/tenants/page.tsx:282` | `w-[480px] sm:w-[480px]` | nt | `w-full sm:max-w-2xl` |
| P2 | `frontend/app/(dashboard)/admin/roles/page.tsx:139` | `w-[600px] sm:w-[600px]` | nt | `w-full sm:max-w-2xl` |
| P2 | `frontend/app/(dashboard)/admin/audit/page.tsx:156` | `w-[500px] sm:w-[500px]` | nt | `w-full sm:max-w-2xl` |

---

### L-05 — Dialog max-width dùng tùy tiện nhiều giá trị ngoài thang chuẩn (`max-w-lg`, `max-w-2xl`)

Chuẩn (mục 3 + mục 7) chỉ quy định 3 giá trị: `sm:max-w-md` (form ≤4 field), `max-w-xl` (form ngắn), `max-w-4xl` (bảng phức tạp). Khảo sát 36 `DialogContent` trong repo: 9 dùng `max-w-lg`, 9 dùng `max-w-2xl` — cả hai đều **không** nằm trong thang chuẩn; `max-w-4xl` chưa từng được dùng dù nhiều dialog chứa bảng dữ liệu (WarehouseTab, AdjustmentTab, tenants, roles).

| Mức | File:dòng | Hiện tại | Chuẩn đúng | Sửa thành |
|-----|-----------|----------|-----------|-----------|
| P1 | `frontend/components/layout/ShortcutsModal.tsx:66` | `max-w-lg max-h-[80vh] overflow-y-auto` | form ngắn → `max-w-xl` | `max-w-xl max-h-[80vh] overflow-y-auto` |
| P1 | `frontend/components/domain/DispenseConfirmDialog.tsx:58` | `max-w-lg` | `max-w-xl` | `max-w-xl` |
| P1 | `frontend/components/domain/PaymentDialog.tsx:186` | `max-w-lg` | `max-w-xl` | `max-w-xl` |
| P1 | `frontend/components/domain/ServiceForm.tsx:124` | `max-w-lg` | `max-w-xl` | `max-w-xl` |
| P1 | `frontend/app/(dashboard)/admin/api-partners/page.tsx:280,294,311` | `max-w-lg` (max-h-[90vh] overflow-y-auto) | `max-w-xl` | `max-w-xl` |
| P1 | `frontend/app/(dashboard)/admin/users/page.tsx:237,264` | `max-w-lg` | `max-w-xl` | `max-w-xl` |
| P1 | `frontend/app/(dashboard)/pharmacy/_components/WarehouseTab.tsx:118,128` | `max-w-2xl` (chứa bảng nhập/xuất kho) | bảng phức tạp → `max-w-4xl` | `max-w-4xl max-h-[90vh] overflow-y-auto` |
| P1 | `frontend/app/(dashboard)/pharmacy/_components/PharmacyPageClient.tsx:86` | `max-w-2xl max-h-[90vh] overflow-y-auto` | tùy nội dung: form ngắn → `max-w-xl`, có bảng → `max-w-4xl` | xem nội dung, chọn 1 trong 2 |
| P1 | `frontend/app/(dashboard)/drugs/_components/DrugsPageClient.tsx:216` | `max-w-2xl` | nt | xem nội dung, chọn 1 trong 2 |
| P1 | `frontend/app/(dashboard)/pharmacy/_components/AdjustmentTab.tsx:146` | `max-w-2xl max-h-[90vh] overflow-y-auto` (chứa bảng kiểm kê) | bảng phức tạp → `max-w-4xl` | `max-w-4xl max-h-[90vh] overflow-y-auto` |
| P1 | `frontend/app/(dashboard)/admin/tenants/page.tsx:239,258` | `max-w-2xl max-h-[90vh] overflow-y-auto` | tùy nội dung | xem nội dung, chọn `max-w-xl`/`max-w-4xl` |
| P1 | `frontend/app/(dashboard)/admin/roles/page.tsx:149,168` | `max-w-2xl max-h-[90vh] overflow-y-auto` (form gán quyền có bảng permission) | bảng phức tạp → `max-w-4xl` | `max-w-4xl max-h-[90vh] overflow-y-auto` |

---

### L-06 — Dialog `PatientForm.tsx` ép form Fullpage vào modal bằng override `!important` + arbitrary vh/vw

| Mức | File:dòng | Hiện tại | Chuẩn đúng | Sửa thành |
|-----|-----------|----------|-----------|-----------|
| P1 | `frontend/components/domain/PatientForm.tsx:134` | `<DialogContent className="!max-w-6xl sm:!max-w-6xl w-[95vw] h-[90vh] max-h-[90vh] overflow-hidden flex flex-col !p-0 !gap-0">` | mục 7 phân loại Fullpage: "> 5 field/nhiều section/có sub-list (**bệnh nhân**, khám, kê đơn) → `max-w-5xl`, chia `FormSection`, sticky action bar dưới" — form bệnh nhân đã có bản Fullpage đúng chuẩn tại `PatientEditorLayout.tsx:287` (`max-w-5xl mx-auto px-4 lg:px-8 py-8`) | Bỏ bản Dialog trùng lặp, điều hướng sang trang Fullpage `patients/new` (có `returnTo`) hoặc — nếu bắt buộc giữ inline tại Reception — chuyển hẳn sang `Sheet` `sm:max-w-2xl px-6 pb-6` thay vì Dialog cưỡng ép 6xl/vh/vw với `!important` |

Ghi chú: đây là 2 cách triển khai khác nhau cho cùng 1 nghiệp vụ "tạo/sửa bệnh nhân" — 1 đúng chuẩn Fullpage, 1 sai chuẩn Dialog — cần thống nhất về 1 pattern.

---

### L-07 — Page title toàn hệ thống dùng `text-2xl` (ngoài thang `--text-*` tự định nghĩa) thay vì `text-xl` chuẩn

`frontend/app/globals.css:86-97` định nghĩa thang chữ riêng cho dự án: `--text-xs/sm/base/md/lg/xl` (không định nghĩa lại `--text-2xl`, `--text-3xl` nên 2 lớp này vẫn lấy giá trị mặc định Tailwind, **nằm ngoài** thang thiết kế riêng của HIS). Mục 2 quy định "Page title: `text-xl` + `font-bold`" (1.375rem) nhưng gần như toàn bộ tiêu đề trang trong `(dashboard)` lại dùng `text-2xl font-bold tracking-tight` (1.5rem mặc định Tailwind — không phải token dự án).

Đối chứng: `(auth)` và `(portal)` route group đã dùng đúng `text-xl font-bold`/`text-xl font-semibold` cho tiêu đề (vd `frontend/app/(auth)/login/page.tsx:16`, `frontend/app/(portal)/portal/me/page.tsx:35`) — chứng tỏ đây không phải giới hạn kỹ thuật, chỉ là `(dashboard)` chưa áp theo chuẩn.

| Mức | File:dòng | Hiện tại | Chuẩn đúng | Sửa thành |
|-----|-----------|----------|-----------|-----------|
| P1 | `frontend/app/(dashboard)/_components/DashboardOverview.tsx:117` | `<h2 className="text-2xl font-bold tracking-tight">{t("title")}</h2>` | `text-xl font-bold` | `text-xl font-bold tracking-tight` |
| P1 | `frontend/app/(dashboard)/icd10/_components/Icd10PageClient.tsx:18` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/bhyt/page.tsx:269` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/nurse/_components/NursePageClient.tsx:57` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/cashier/_components/CashierPageClient.tsx:51` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/notifications/page.tsx:41` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/labrad/_components/LabRadPageClient.tsx:13` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/patients/[id]/page.tsx:125` | `<h1 className="text-2xl font-bold">{patient.full_name}</h1>` | nt | nt |
| P1 | `frontend/app/(dashboard)/encounters/_components/EncountersPageClient.tsx:75` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/patients/page.tsx:211` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/drugs/page.tsx:10` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/reception/page.tsx:63` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/admin/users/page.tsx:176` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/labrad/results/page.tsx:7` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/cashier/debts/page.tsx:7` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/admin/tenants/page.tsx:181` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/services/_components/ServicesPageClient.tsx:88` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/labrad/partners/page.tsx:7` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/pharmacy/page.tsx:10` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/prescriptions/page.tsx:10` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/prescriptions/_components/PrescriptionsPageClient.tsx:167` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/billings/_components/BillingsPageClient.tsx:63` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/pharmacy/dispense/page.tsx:10` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/admin/suppliers/page.tsx:10` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/admin/notifications-config/page.tsx:50` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/admin/roles/page.tsx:119` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/admin/page.tsx:9` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/admin/emr-templates/_components/EmrTemplatesPageClient.tsx:24` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/admin/dtqg/page.tsx:10` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/admin/audit/page.tsx:112` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/admin/einvoice/_components/EInvoiceAdminClient.tsx:63` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/reports/_components/ReportsPageClient.tsx:13` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/admin/api-partners/page.tsx:115` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/account/notifications/page.tsx:103` | `text-2xl font-bold tracking-tight` | nt | nt |
| P1 | `frontend/app/(dashboard)/account/profile/page.tsx:28` | `text-2xl font-semibold` | nt | `text-xl font-bold` |
| P1 | `frontend/app/(dashboard)/account/security/page.tsx:130` | `text-2xl font-bold tracking-tight` | nt | nt |

**Ghi chú quan trọng:** đây là finding có phạm vi rất rộng (34 file). Vì gần như 100% màn hình `(dashboard)` bị lệch giống nhau, đề xuất xử lý gốc: tạo 1 component `PageTitle`/`PageHeader` dùng chung `text-xl font-bold` (thay vì mỗi PageClient tự viết `<h1>/<h2>` rời), rồi thay thế dần — tránh sửa tay 34 chỗ rồi vẫn tiếp tục lệch ở màn mới.

---

### L-08 — Số liệu thống kê (KPI/stat) dùng cỡ chữ không nhất quán, không qua token `--text-kpi`/component `KpiCard`

`frontend/components/reports/kpi-card.tsx:89` đã cài đúng chuẩn: `className="text-[length:var(--text-kpi)] leading-[var(--text-kpi--line-height)] font-bold tabular-nums text-[color:var(--text-primary)]"`. Nhưng nhiều nơi khác tự vẽ số liệu lớn bằng Tailwind rời, không đồng nhất kích thước và không qua token/`KpiCard`:

| Mức | File:dòng | Hiện tại | Chuẩn đúng | Sửa thành |
|-----|-----------|----------|-----------|-----------|
| P2 | `frontend/app/(dashboard)/reception/page.tsx:87` | `<p className="text-3xl font-bold">{value ?? 0}</p>` | mục 2: KPI card → token `text-kpi`/`text-kpi-lg` | Dùng `<KpiCard>` hoặc `text-[length:var(--text-kpi)] tabular-nums` |
| P2 | `frontend/app/(dashboard)/_components/DashboardOverview.tsx:243,251,264` | `text-2xl font-bold` (3 chỗ) | nt | nt |
| P2 | `frontend/app/(dashboard)/cashier/_components/CashierPageClient.tsx:181` | `<p className="text-2xl font-bold">{value}</p>` | nt | nt |
| P2 | `frontend/app/(dashboard)/reports/_components/ClinicalTab.tsx:119,123` | `text-2xl font-bold` | nt | nt |
| P2 | `frontend/app/(dashboard)/reports/_components/ClinicalTab.tsx:127,131` | `text-xl font-bold` (cùng loại số liệu nhưng khác cỡ so với dòng 119/123 ở trên) | nt | dùng cùng 1 cỡ cho cùng nhóm KPI |
| P2 | `frontend/app/(dashboard)/reports/_components/PharmacyTab.tsx:90` | `text-2xl font-bold` | nt | nt |
| P2 | `frontend/app/(dashboard)/reports/_components/PharmacyTab.tsx:130,134` | `text-xl font-bold` | nt | nt |
| P2 | `frontend/app/(dashboard)/bhyt/exports/[id]/page.tsx:98,106,114` | `text-xl font-bold` | nt | nt |

---

### L-09 — Base component `Card` dùng `rounded-xl` thay vì `rounded-lg` chuẩn (ảnh hưởng toàn hệ thống)

| Mức | File:dòng | Hiện tại | Chuẩn đúng | Sửa thành |
|-----|-----------|----------|-----------|-----------|
| P1 | `frontend/components/ui/card.tsx:15` | `"group/card flex flex-col gap-4 overflow-hidden rounded-xl bg-card py-4 ..."` | mục 3: "Card / Panel — `rounded-lg` (radius gốc 10px)" | Đổi `rounded-xl` → `rounded-lg` (và `rounded-t-xl`/`rounded-b-xl` ở `CardHeader`/`CardFooter`, dòng 28 & 87, `*:[img:first-child]:rounded-t-xl` dòng 15 → đồng bộ `rounded-t-lg`/`rounded-b-lg`) |
| P1 | `frontend/components/ui/dialog.tsx:56` | `"...gap-4 rounded-xl bg-popover p-4..."` | Dialog cũng nên theo cùng thang bo góc panel (`rounded-lg`) trừ khi có quyết định riêng từ kiến trúc | Xác nhận với Lành (architect) có cố ý tách riêng bo góc Dialog hay đồng bộ `rounded-lg` |

Đây là finding **gốc**: vì `Card` là base component dùng ở hầu hết dashboard/report/KPI, mọi nơi `<Card>` mặc định đang bo góc 14px (`--radius-xl` = 0.625rem×1.4) thay vì 10px (`--radius-lg`) như tài liệu quy định.

---

### L-10 — Panel tự vẽ (`div bg-card`) trộn lẫn `rounded-lg` và `rounded-xl` cho cùng vai trò panel

| Mức | File:dòng | Hiện tại | Chuẩn đúng | Sửa thành |
|-----|-----------|----------|-----------|-----------|
| P2 | `frontend/app/(dashboard)/reception/page.tsx:97,102` | `<div className="border rounded-xl p-4 bg-card">` | mục 3: `rounded-lg` | `rounded-lg` |
| P2 | `frontend/app/(dashboard)/encounters/new/page.tsx:147,239` | `<div className="rounded-xl border bg-card p-6 space-y-4">` | nt | `rounded-lg` |
| P2 | `frontend/app/(dashboard)/prescriptions/new/page.tsx:177,286,366` | `<div className="rounded-xl border bg-card p-6 space-y-4">` | nt | `rounded-lg` |
| — | (đối chứng đã đúng) `frontend/app/(dashboard)/admin/emr-templates/_components/EmrTemplatesPageClient.tsx:83` | `rounded-lg border bg-card p-4` | đúng | giữ nguyên |
| — | (đối chứng đã đúng) `frontend/app/(dashboard)/nurse/_components/NursePageClient.tsx:208` | `rounded-lg border bg-card p-3` | đúng | giữ nguyên |

---

### L-11 — Bảng danh sách dài dùng mật độ "comfortable" (`py-3`) thay vì "dense" (`py-2`)

Các trang dùng `DataTable`/`Table` dùng chung (`patients`, `prescriptions`, `drugs`, `admin/users`, `admin/tenants`, `admin/roles`, `admin/suppliers`, `admin/audit`) kế thừa `TableCell` mặc định `p-2` (8px, tương đương dense `py-2`) — đúng chuẩn. Nhưng 1 số màn tự vẽ `<table>` HTML rời lại dùng `py-3` (12px, mật độ "comfortable") dù đều là danh sách dài (>20 dòng):

| Mức | File:dòng | Hiện tại | Chuẩn đúng | Sửa thành |
|-----|-----------|----------|-----------|-----------|
| P2 | `frontend/app/(dashboard)/encounters/_components/EncountersPageClient.tsx:173-179,187,263-301` | `<th className="... px-4 py-3 ...">` / `<td className="px-4 py-3">` — danh sách Khám bệnh (list chính, có thể >20 dòng/ca trực) | mục 4: "dense `py-2` cho danh sách > 20 dòng" | Đổi `py-3` → `py-2` cho toàn bộ `<th>`/`<td>` trong bảng này |
| P2 | `frontend/app/(dashboard)/pharmacy/_components/DispenseTab.tsx:123-148,179-197` | `<th>`/`<td className="px-4 py-3">` — lịch sử phát thuốc | nt | Đổi `py-3` → `py-2` |
| P2 | `frontend/app/(dashboard)/icd10/_components/Icd10PageClient.tsx:38-49,80-88` | `<th>`/`<td className="px-4 py-3">` — tra cứu ICD-10 (danh mục hàng nghìn dòng) | nt | Đổi `py-3` → `py-2` |

---

### L-12 — Skeleton loading dùng `p-8` thay vì `p-4`/`p-6` chuẩn content padding

| Mức | File:dòng | Hiện tại | Chuẩn đúng | Sửa thành |
|-----|-----------|----------|-----------|-----------|
| P3 | `frontend/app/(dashboard)/patients/new/page.tsx:46` | `<div className="p-8 space-y-4">` (fallback `Suspense`, lồng bên trong `<main className="p-4 md:p-6">` của layout → cộng dồn padding) | mục 3: "Content padding — `p-6` desktop · `p-4` tablet" | `<div className="p-6 space-y-4">` hoặc bỏ padding, để `main` xử lý |
| P3 | `frontend/app/(dashboard)/patients/[id]/edit/page.tsx:24` | `<div className="p-8 space-y-4">` | nt | nt |
| P3 | `frontend/app/(dashboard)/encounters/[id]/print/_components/EncounterPrintClient.tsx:32,43` | `<div className="p-8 space-y-4">` / `<div className="p-8 text-center">` | nt | `p-6` |
| P3 | `frontend/app/(dashboard)/encounters/[id]/cls-print/_components/ClsOrderPrintClient.tsx:62,73` | `<div className="p-8 space-y-4">` / `<div className="p-8 text-center">` | nt | `p-6` |

---

### L-13 — Select/filter trigger dùng width arbitrary px thay vì thang chuẩn

| Mức | File:dòng | Hiện tại | Chuẩn đúng | Sửa thành |
|-----|-----------|----------|-----------|-----------|
| P3 | `frontend/app/(dashboard)/admin/users/page.tsx:198` | `<SelectTrigger className="w-[160px]">` | mục 3: "khoảng cách chỉ dùng scale Tailwind... không `p-[13px]`" (áp dụng chung cho width) | `w-40` (160px, khớp scale sẵn có) |
| P3 | `frontend/app/(dashboard)/admin/users/page.tsx:213` | `w-[150px]` | nt | `w-36`(144px) hoặc `w-40`(160px) |
| P3 | `frontend/app/(dashboard)/admin/tenants/page.tsx:215` | `w-[160px]` | nt | `w-40` |
| P3 | `frontend/app/(dashboard)/admin/audit/page.tsx:129` | `w-[170px]` | nt | `w-44`(176px) |

---

### L-14 — Badge dùng `text-[10px]` ngoài thang `--text-*` (nhỏ hơn cả `text-xs`)

| Mức | File:dòng | Hiện tại | Chuẩn đúng | Sửa thành |
|-----|-----------|----------|-----------|-----------|
| P3 | `frontend/app/(dashboard)/drugs/_components/DrugsPageClient.tsx:83,86,89` | `<Badge variant="outline" className="text-[10px] px-1">Kê đơn</Badge>` (và 2 badge tương tự) | mục 2: "chỉ dùng thang `--text-*` đã định nghĩa; cấm `text-[13px]`, `text-[15px]` tùy tiện" (nhỏ nhất là `text-xs` = 0.75rem) | `text-xs px-1` |

---

## Đạt chuẩn (để không sửa nhầm)

- Sidebar mở rộng (`AppSidebar.tsx:58`, `w-60` = 240px) khớp đúng chuẩn 240px.
- Content padding vùng chính `frontend/app/(dashboard)/layout.tsx:17` (`<main className="flex-1 overflow-y-auto p-4 md:p-6">`) khớp đúng "p-4 tablet / p-6 desktop".
- Sheet `frontend/app/(dashboard)/drugs/_components/DrugsPageClient.tsx:224` (`sm:max-w-2xl overflow-y-auto px-6 pb-6`) là **mẫu chuẩn duy nhất** đúng 100% mục 3 — nên dùng làm tham chiếu khi sửa các Sheet khác.
- Nhóm Dialog dùng `sm:max-w-md`/`max-w-md` (BhytReconcileTable, BhytExportForm, ExportReportDialog, EmrSignDialog, EInvoiceIssueDialog, SignPrescriptionWizard, admin/notifications-config) khớp đúng mục 7 "Dialog ≤4 field → `sm:max-w-md`".
- Dialog `max-w-xl` (DrugsPageClient:236, SuppliersPageClient:124,131) khớp đúng mục 3 "form ngắn → `max-w-xl`".
- Bảng dùng `DataTable`/`Table` dùng chung (patients, prescriptions, drugs, admin/users, admin/tenants, admin/roles, admin/suppliers, admin/audit) kế thừa `TableCell` mặc định `p-2` — tương đương dense `py-2`, khớp chuẩn mục 4 cho danh sách dài.
- Tiêu đề trang trong route group `(auth)` và `(portal)` đã dùng đúng `text-xl font-bold`/`font-semibold` — khớp chuẩn mục 2 (khác với `(dashboard)` đang lệch ở L-07).
- Component `frontend/components/reports/kpi-card.tsx:89` đã cài đúng token `--text-kpi` + `tabular-nums` — mẫu chuẩn nên nhân rộng thay cho các số liệu KPI viết tay ở L-08.
- Không phát hiện double-padding ở phần lớn `*PageClient.tsx` (không có wrapper `p-4`/`p-6` thừa lồng trong `<main>`).
- Không phát hiện `rounded-2xl`/`rounded-3xl` lạm dụng cho panel/card trong `(dashboard)`.
