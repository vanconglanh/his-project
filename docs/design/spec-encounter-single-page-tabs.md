# [UI-G1] Màn khám bệnh 1-route-nhiều-tab (Encounter Single Page)

> Tác giả: Linh (designer) · Ngày: 2026-08-18 · Trạng thái: Ready for dev
> Nguồn gap: `docs/designer/ui-gap-analysis-vs-med-his.md` — G1 (P0), có chạm G2 (P1)
> Chuẩn bám theo: `docs/design/design-system-standards.md` (nguồn chân lý), `input-form-layout-spec.md`
> Người implement: Nam (frontend) · Duyệt kỹ thuật: Lành (architect) · Test: Phượng · Gác cổng: Chi (qc)
> Route đích: `/encounters/[id]` — file chính `frontend/app/(dashboard)/encounters/[id]/_components/EncounterDetailClient.tsx` (hiện 915 dòng, cần tách)

---

## 1. Mục tiêu UX

Bác sĩ đang khám phải rời route sang `/labrad/results` (xem kết quả CLS) và `/prescriptions/new` (kê đơn) → mất context sinh hiệu + hồ sơ bệnh nhân đang xem, tăng 4–6 click/ca và phải nhớ lại số đo bằng trí nhớ. Spec này gom **toàn bộ luồng khám** về **1 route duy nhất** với **sidebar trái sticky luôn hiển thị hồ sơ + sinh hiệu + lịch sử** và **vùng phải là Tabs ngang**, để bác sĩ không bao giờ mất ngữ cảnh bệnh nhân trong suốt ca khám.

**Chỉ số thành công (đo được):**
| Chỉ số | Hiện tại | Mục tiêu |
|---|---|---|
| Số lần rời route trong 1 ca khám | 2–3 | 0 |
| Click từ "bắt đầu khám" → "kê xong đơn" | ~14 | ≤ 8 |
| Sinh hiệu luôn nhìn thấy khi kê đơn | Không | Có (sidebar sticky) |

---

## 2. Phạm vi

### Trong phạm vi
- Layout 2 vùng (sidebar sticky trái + tabs phải), 8 tab nghiệp vụ.
- Toolbar khám (trạng thái + chuyển phòng + chờ CLS + kết thúc khám).
- Banner khoá khi `DONE` + nút "Tạo bản đính chính".
- Đợt chỉ định CLS (`#1`, `#2`…) kèm badge trạng thái thanh toán.
- Responsive tablet: sidebar → drawer < 1024px.
- Token/component chuẩn, khử hardcode màu trong phạm vi file đụng tới.

### NGOÀI phạm vi (KHÔNG spec ở đây)
- Master-detail 2 cột cho `/billings`, `/labrad/results` (G3 — spec riêng).
- Toggle "thuốc trong kho / ngoài kho" (G7 — chờ po-analyst).
- Camera AI nhận diện, ghi âm cuộc khám (ngoài phạm vi sản phẩm).
- Tab "Ghi âm" — **không làm**.
- Thay đổi API contract — mọi endpoint mới ghi ở §11 dạng **yêu cầu gửi Lành**, không tự quyết.

---

## 3. Wireframe

### 3.1. Desktop ≥ 1280px (`xl`)

```
┌──────────────────────────────────────────────────────────────────────────────────────────┐
│ Topbar app 56px (đã có, không đụng)                                                      │
├──────────────────────────────────────────────────────────────────────────────────────────┤
│ ← Khám bệnh / Nguyễn Văn A                             (breadcrumb, text-sm, muted)      │
├──────────────────────────────────────────────────────────────────────────────────────────┤
│ ╔══ TOOLBAR KHÁM (sticky top-0, z-20, h-14, bg-surface/95 backdrop, border-b) ══════════╗ │
│ ║ [◉ Đang khám]  Phòng: [Phòng 102 ▾]   │  [⏸ Chờ kết quả CLS]  [🖨 In ▾]  [✔ Kết thúc]║ │
│ ╚════════════════════════════════════════════════════════════════════════════════════════╝ │
│ ┌── BANNER KHOÁ (chỉ khi status = DONE) ────────────────────────────────────────────────┐ │
│ │ 🔒 Bệnh án đã khoá — chỉ xem. Kết thúc lúc 14:32 03/08/2026.   [Tạo bản đính chính]  │ │
│ └────────────────────────────────────────────────────────────────────────────────────────┘ │
│ ┌── BANNER quá 12h (đã có EncounterAlertBanner) ───────────────────────────────────────┐  │
│ └────────────────────────────────────────────────────────────────────────────────────────┘ │
├────────────────────────────┬─────────────────────────────────────────────────────────────┤
│ SIDEBAR BỆNH NHÂN          │ VÙNG TABS (col-span-9)                                      │
│ (col-span-3, w 300–340px)  │                                                             │
│ sticky top-[calc(56px+     │ ┌─ TabsList (sticky top-[112px], z-10, overflow-x-auto) ──┐ │
│ 3.5rem)] · max-h-[calc(    │ │ Bệnh án │Tiền sử│CLS ②│Kết quả CLS ①│Chẩn đoán│Đơn thuốc│ │
│ 100vh-8rem)] overflow-auto │ │ │Tái khám│Tập tin                                        │ │
│                            │ └───────────────────────────────────────────────────────────┘ │
│ ┌ Card: Định danh ───────┐ │ ┌─ TabsContent (min-h-[520px]) ───────────────────────────┐ │
│ │ ⬤   Nguyễn Văn A       │ │ │                                                          │ │
│ │ 64  Nam · 58 tuổi      │ │ │   (nội dung tab đang chọn)                               │ │
│ │ px  BN000123           │ │ │                                                          │ │
│ │     [🛡 BHYT còn hạn]  │ │ │                                                          │ │
│ │ ─────────────────────  │ │ │                                                          │ │
│ │ Bác sĩ    BS. Trần B   │ │ │                                                          │ │
│ │ Phòng     Phòng 102    │ │ │                                                          │ │
│ │ Lý do     Đau đầu      │ │ │                                                          │ │
│ └────────────────────────┘ │ │                                                          │ │
│ ┌ Card: Sinh hiệu ───────┐ │ │                                                          │ │
│ │ Sinh hiệu   [Xem tất cả]│ │ │                                                          │ │
│ │ Nhiệt 37.0  Mạch 80    │ │ │                                                          │ │
│ │ HA 150/95⚠  SpO2 98%   │ │ │                                                          │ │
│ │ CN 65kg     ĐH 140     │ │ │                                                          │ │
│ │ ─ đo lúc 08:12 hôm nay │ │ │                                                          │ │
│ │        [+ Ghi sinh hiệu]│ │ │                                                          │ │
│ └────────────────────────┘ │ │                                                          │ │
│ ┌ Card: Cảnh báo ────────┐ │ │                                                          │ │
│ │ ⚠ Dị ứng: Penicillin   │ │ │                                                          │ │
│ │ ⚠ Dị ứng: Hải sản      │ │ │                                                          │ │
│ └────────────────────────┘ │ │                                                          │ │
│ ┌ Card: Tiền sử ─────────┐ │ │                                                          │ │
│ │ ĐTĐ type 2 (2019)      │ │ │                                                          │ │
│ │ THA độ 1               │ │ │                                                          │ │
│ │            [Xem chi tiết]│ │                                                          │ │
│ └────────────────────────┘ │ │                                                          │ │
│ ┌ Card: Lịch sử khám ────┐ │ │                                                          │ │
│ │ ● 03/08 Tái khám ĐTĐ   │ │ │                                                          │ │
│ │ ○ 05/07 Khám mới       │ │ │                                                          │ │
│ │ ○ 02/06 Tái khám       │ │ │                                                          │ │
│ │            [Xem tất cả] │ │ └──────────────────────────────────────────────────────────┘ │
│ └────────────────────────┘ │                                                             │
└────────────────────────────┴─────────────────────────────────────────────────────────────┘
```

### 3.2. Tablet ngang / laptop nhỏ 1024–1279px (`lg`)
Giữ 2 vùng nhưng sidebar hẹp lại: `lg:col-span-4` (~280px) + tabs `lg:col-span-8`. Tab label rút gọn (xem §5.2).

### 3.3. Tablet dọc / < 1024px
Sidebar biến mất khỏi luồng, thay bằng **thanh tóm tắt sticky 1 dòng** + Sheet drawer:

```
┌────────────────────────────────────────────────────────────────┐
│ TOOLBAR KHÁM (như trên, nút gộp vào [⋯])                       │
├────────────────────────────────────────────────────────────────┤
│ ⬤ Nguyễn Văn A · Nam 58t · BN000123 · HA 150/95 ⚠  [Hồ sơ ▸]  │  ← PatientStripBar, h-12 sticky
├────────────────────────────────────────────────────────────────┤
│ TabsList cuộn ngang (scroll-snap, không wrap)  →→→             │
├────────────────────────────────────────────────────────────────┤
│ TabsContent full width                                         │
└────────────────────────────────────────────────────────────────┘
   nhấn [Hồ sơ ▸] → Sheet phải sm:max-w-xl, px-6 pb-6,
   chứa nguyên nội dung sidebar (5 card), title "Hồ sơ bệnh nhân"
```

### 3.4. Tab "Cận lâm sàng" — đợt chỉ định (yêu cầu #3)

```
┌ TabsContent: Cận lâm sàng ───────────────────────────────────────────────────┐
│  [+ Tạo đợt chỉ định mới]                          (primary, ẩn khi khoá)    │
│                                                                              │
│  ┌ Đợt #2 · 03/08/2026 14:05 · BS. Trần B ────────────────────────────────┐  │
│  │  [⏱ Chưa thanh toán]  1.250.000 ₫        [🖨 In phiếu] [✎ Sửa] [🗑]    │  │  ← header đợt
│  ├──────────────────────────────────────────────────────────────────────────┤  │
│  │ Mã      Dịch vụ                    Loại   Ưu tiên  Trạng thái   Giá      │  │
│  │ XN0012  Glucose máu đói            XN     Thường   [◷ Chờ]    120.000 ₫  │  │
│  │ XN0031  HbA1c                      XN     Khẩn     [◷ Chờ]    350.000 ₫  │  │
│  │ CDHA07  Siêu âm ổ bụng             CĐHA   Thường   [◷ Chờ]    780.000 ₫  │  │
│  └──────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  ┌ Đợt #1 · 03/08/2026 08:40 · BS. Trần B ────────────────────────────────┐  │
│  │  [✔ Đã thanh toán]  480.000 ₫   HĐ: HD-2026-00871   [🖨 In phiếu]      │  │
│  ├──────────────────────────────────────────────────────────────────────────┤  │
│  │ XN0002  Công thức máu              XN     Thường  [✔ Có KQ]   180.000 ₫ │  │
│  │ XN0009  Creatinin                  XN     Thường  [◉ Đang XL] 300.000 ₫ │  │
│  └──────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  (empty) 🧪 Chưa có chỉ định cận lâm sàng                                     │
│          Tạo đợt chỉ định để gửi yêu cầu XN/CĐHA cho bệnh nhân.               │
│          [+ Tạo đợt chỉ định mới]                                            │
└──────────────────────────────────────────────────────────────────────────────┘
```

**Quy tắc hiển thị đợt:**
- Sắp xếp **đợt mới nhất lên đầu**; đợt mới nhất mặc định mở (`defaultOpen`), các đợt cũ collapse (dùng `Collapsible`).
- Đợt **chưa thanh toán** → cho phép Sửa/Xoá dòng. Đợt **đã thanh toán** → chỉ đọc + In (không nút Sửa/Xoá).
- Tổng tiền mỗi đợt: `font-mono tabular-nums`, căn phải.
- Số hiệu đợt lấy từ BE (`batch_no`), FE **không tự đánh số theo index** (tránh lệch khi có đợt bị huỷ).

### 3.5. Dialog "Tạo đợt chỉ định mới"
`Dialog max-w-4xl` (bảng phức tạp, đúng §3 chuẩn): trái autocomplete dịch vụ (`DrugAutocomplete` pattern), phải bảng giỏ dịch vụ đã chọn + tổng tiền, footer sticky `[Huỷ] [Lưu đợt chỉ định] [Lưu & In phiếu]`.

---

## 4. Component tree

```
EncounterDetailClient (route client, ~180 dòng sau khi tách)
├── EncounterBreadcrumb                       (inline, giữ nguyên)
├── EncounterToolbar                        ★ MỚI
│   ├── EncounterStatusBadge                  ♻ SỬA → wrap HisStatusBadge
│   ├── RoomTransferSelect                  ★ MỚI  (Select + ConfirmDialog)
│   ├── Button "Chờ kết quả CLS"              ♻ Button outline
│   ├── DropdownMenu "In"                     ♻ (gộp 2 nút in hiện có)
│   └── Button "Kết thúc khám"                ♻ Button (variant=default) + ConfirmDialog
├── EncounterLockBanner                     ★ MỚI  (yêu cầu #4)
├── EncounterAlertBanner                      ♻ TÁI DÙNG nguyên trạng (cảnh báo >12h)
├── PatientStripBar                         ★ MỚI  (chỉ < lg, mở drawer)
├── <div grid grid-cols-12 gap-4>
│   ├── EncounterPatientSidebar             ★ MỚI  (gom 5 card, dùng cả desktop + drawer)
│   │   ├── PatientIdentityCard             ★ MỚI  (SimpleAvatar ♻ + HisStatusBadge ♻)
│   │   ├── VitalSignsSummaryCard           ★ MỚI  (tách từ VitalSummary hiện có)
│   │   ├── AllergyAlertCard                ★ MỚI  (đọc useAllergies ♻)
│   │   ├── MedicalHistoryCard              ★ MỚI
│   │   └── PatientVisitHistoryCard         ★ MỚI  (list encounter cũ, KHÁC EncounterTimeline)
│   └── EncounterTabs                       ★ MỚI  (Tabs shadcn ♻)
│       ├── tab "emr"          → EmrTabPanel          ♻ EmrEditor + EmrTemplateSelector + EmrSignDialog
│       ├── tab "history"      → HistoryTabPanel      ★ MỚI (AllergyList ♻ + EmergencyContactList ♻ + tiền sử)
│       ├── tab "cls-orders"   → ClsOrderTabPanel     ★ MỚI (đợt chỉ định)
│       │   ├── ClsOrderBatchCard            ★ MỚI
│       │   │   ├── ClsBatchPaymentBadge     ★ MỚI (wrap HisStatusBadge)
│       │   │   └── ClsOrderItemTable        ★ MỚI (thay LabRadOrderList cũ)
│       │   └── ClsOrderBatchDialog          ★ MỚI (LabOrderForm ♻ + RadOrderForm ♻ nhúng vào)
│       ├── tab "cls-results"  → ClsResultTabPanel    ★ MỚI (LabResultTable ♻ + FlagBadge ♻ + ClsUploadList ♻)
│       ├── tab "diagnosis"    → DiagnosisTabPanel    ♻ tách nguyên từ file cũ (dòng 701–887)
│       ├── tab "prescription" → PrescriptionTabPanel ♻ PrescriptionForm nguyên trạng
│       ├── tab "followup"     → FollowUpTabPanel     ★ MỚI (useAppointments ♻)
│       └── tab "files"        → FileTabPanel         ★ MỚI (ClsUploadList ♻ + DicomUploadZone ♻)
├── VitalSignsHistoryDrawer                   ♻ TÁI DÙNG
├── PatientSidebarSheet                     ★ MỚI  (< lg, bọc EncounterPatientSidebar)
└── UnsavedChangesDialog                    ★ MỚI  (G5 — 3 action, dùng khi rời tab có draft)
```

**Chú thích:** ♻ = tái dùng/giữ nguyên · ★ = tạo mới.

### 4.1. Bảng component

| Component | Loại | File đề xuất | Ghi chú |
|---|---|---|---|
| `EncounterToolbar` | ★ mới | `components/domain/EncounterToolbar.tsx` | sticky, gom mọi hành động ca khám |
| `RoomTransferSelect` | ★ mới | `components/domain/RoomTransferSelect.tsx` | cần API `PUT /encounters/{id}` (đã có `room_id`) |
| `EncounterLockBanner` | ★ mới | `components/domain/EncounterLockBanner.tsx` | biến thể của `EncounterAlertBanner` |
| `PatientStripBar` | ★ mới | `components/domain/PatientStripBar.tsx` | chỉ hiện `< lg` |
| `EncounterPatientSidebar` | ★ mới | `components/domain/EncounterPatientSidebar.tsx` | dùng chung desktop + Sheet |
| `PatientIdentityCard` | ★ mới | cùng thư mục | |
| `VitalSignsSummaryCard` | ★ mới | cùng thư mục | tách từ `VitalSummary` (dòng 889–915 file cũ) |
| `AllergyAlertCard` | ★ mới | cùng thư mục | read-only, link sang tab Tiền sử |
| `MedicalHistoryCard` | ★ mới | cùng thư mục | read-only |
| `PatientVisitHistoryCard` | ★ mới | cùng thư mục | `listEncounters({patient_id})` |
| `ClsOrderBatchCard` | ★ mới | `components/domain/cls/ClsOrderBatchCard.tsx` | |
| `ClsBatchPaymentBadge` | ★ mới | `components/domain/cls/ClsBatchPaymentBadge.tsx` | wrap `HisStatusBadge` |
| `ClsOrderItemTable` | ★ mới | `components/domain/cls/ClsOrderItemTable.tsx` | thay `LabRadOrderList` |
| `ClsOrderBatchDialog` | ★ mới | `components/domain/cls/ClsOrderBatchDialog.tsx` | `max-w-4xl` |
| `FollowUpTabPanel` | ★ mới | `components/domain/FollowUpTabPanel.tsx` | dùng `use-appointments.ts` |
| `UnsavedChangesDialog` | ★ mới | `components/domain/UnsavedChangesDialog.tsx` | wrap `ConfirmDialog` 3 action |
| `EncounterStatusBadge` | ♻ sửa | `components/domain/EncounterStatusBadge.tsx` | **P1: đang hardcode `bg-yellow-100…` — phải wrap `HisStatusBadge`** |
| `LabRadOrderList` | ♻ deprecate | `components/domain/LabRadOrderList.tsx` | **P1: hardcode 10 class palette (dòng 12–26)** — thay bằng `ClsOrderItemTable` |
| `EmrEditor`, `EmrTemplateSelector`, `EmrSignDialog`, `Icd10Picker`, `DiagnosesList`, `PrescriptionForm`, `ClsUploadList`, `LabResultTable`, `FlagBadge`, `DicomUploadZone`, `AllergyList`, `EmergencyContactList`, `VitalSignsHistoryDrawer`, `DiabetesAssessmentForm`, `DiabetesTrendChart`, `SimpleAvatar`, `ConfirmDialog`, `EmptyState`, `HisStatusBadge` | ♻ tái dùng | — | không đổi API |

---

## 5. Tabs — danh sách chính thức

### 5.1. Bảng tab

| # | `value` | Nhãn VN | Icon (lucide) | Nội dung | Badge số | Hiện khi |
|---|---|---|---|---|---|---|
| 1 | `emr` | Bệnh án | `FileText` | EMR editor + template + trạng thái ký số | — | luôn (mặc định) |
| 2 | `history` | Tiền sử | `HeartPulse` | Dị ứng, bệnh nền, tiền sử gia đình, người liên hệ, đánh giá ĐTĐ + biểu đồ xu hướng | số dị ứng (nếu >0, màu `critical`) | luôn |
| 3 | `cls-orders` | Cận lâm sàng | `FlaskConical` | Đợt chỉ định CLS (§3.4) | số đợt chưa thanh toán (`warning`) | luôn |
| 4 | `cls-results` | Kết quả CLS | `ClipboardCheck` | Kết quả XN/CĐHA + flag bất thường + file đính kèm | số KQ mới chưa xem (`progress`) | luôn |
| 5 | `diagnosis` | Chẩn đoán | `Stethoscope` | ICD-10 picker + nhập nhanh nhiều dòng + danh sách | số chẩn đoán | luôn |
| 6 | `prescription` | Đơn thuốc | `Pill` | `PrescriptionForm` (kê + DDI + ký số + đẩy ĐTQG) | — | luôn |
| 7 | `followup` | Tái khám | `CalendarClock` | Đặt lịch tái khám, dặn dò, in giấy hẹn | ✓ nếu đã đặt | luôn |
| 8 | `files` | Tập tin | `Paperclip` | Upload/preview ảnh, PDF, DICOM | số file | luôn |

**Quyết định thiết kế (giải thích cho Nam):**
- **Bỏ tab "Sinh hiệu"** khỏi tabs — sinh hiệu đã nằm cố định ở sidebar (card + nút "Ghi sinh hiệu" mở `VitalSignsHistoryDrawer` với form nhập). Đây chính là lợi ích cốt lõi của G1: sinh hiệu luôn thấy được ở mọi tab.
- **Bỏ tab "Timeline"** — timeline chi tiết chuyển thành nút "Xem tất cả" trong card Lịch sử khám ở sidebar (mở Sheet). Timeline là tra cứu, không phải bước nghiệp vụ, không xứng 1 tab ngang.
- **Gộp "Đánh giá ĐTĐ" vào tab Tiền sử** — đây là dữ liệu tiền sử/theo dõi mạn tính, không phải bước riêng; giảm từ 10 tab xuống 8 tab (8 là ngưỡng còn đọc được 1 dòng ở 1280px).
- **KHÔNG có tab Ghi âm** (ngoài phạm vi, đã chốt).

### 5.2. Nhãn tab theo breakpoint

| `value` | ≥ 1280px | 1024–1279px | < 1024px (cuộn ngang) |
|---|---|---|---|
| `emr` | Bệnh án | Bệnh án | Bệnh án |
| `history` | Tiền sử | Tiền sử | Tiền sử |
| `cls-orders` | Cận lâm sàng | CLS | CLS |
| `cls-results` | Kết quả CLS | Kết quả | Kết quả |
| `diagnosis` | Chẩn đoán | Chẩn đoán | Chẩn đoán |
| `prescription` | Đơn thuốc | Đơn thuốc | Đơn thuốc |
| `followup` | Tái khám | Tái khám | Tái khám |
| `files` | Tập tin | Tập tin | Tập tin |

Implement bằng 2 `<span>` (`hidden xl:inline` / `xl:hidden`), **không** dùng JS đo width.

### 5.3. Deep-link tab (bắt buộc)
Tab đồng bộ với query param: `/encounters/{id}?tab=prescription`.
- Đọc: `useSearchParams().get("tab")`, fallback `"emr"`, validate trong whitelist 8 value.
- Ghi: `router.replace(pathname + "?tab=" + v, { scroll: false })` → **không** tạo history entry rác; nút Back của trình duyệt vẫn quay về danh sách.
- Lý do: chia sẻ link, F5 không mất tab, và cho phép redirect từ nơi khác (vd nút "Kê đơn" ở queue → `?tab=prescription`).

---

## 6. Props (chi tiết đủ để code)

```ts
// EncounterToolbar
interface EncounterToolbarProps {
  encounterId: string;
  status: EncounterStatus;              // WAITING | IN_PROGRESS | DONE | CANCELLED
  roomId?: string | null;
  roomName?: string | null;
  finishedAt?: string | null;
  isEmrSigned: boolean;
  canEdit: boolean;                     // status === IN_PROGRESS && !isLocked
  onStart: () => void;
  onWaitForCls: () => void;             // đổi trạng thái → WAITING_CLS
  onClose: () => void;                  // kết thúc khám (mở ConfirmDialog trước)
  onTransferRoom: (roomId: string) => void;
  isPending?: boolean;                  // disable + spinner khi mutate
}

// EncounterLockBanner
interface EncounterLockBannerProps {
  finishedAt?: string | null;
  closedByName?: string | null;
  canAmend: boolean;                    // usePermissions().has("ENCOUNTER_AMEND")
  onAmend: () => void;
  amendmentCount?: number;              // >0 → hiện "Đã có N bản đính chính"
}

// EncounterPatientSidebar
interface EncounterPatientSidebarProps {
  encounter: EncounterDetailResponse;
  patientId: string;
  variant?: "desktop" | "drawer";       // drawer bỏ sticky/max-h, thêm px-6 pb-6
  onOpenVitalDrawer: () => void;
  onNavigateTab: (tab: EncounterTabValue) => void;   // "Xem chi tiết" tiền sử → tab history
}

// PatientIdentityCard
interface PatientIdentityCardProps {
  fullName: string;
  patientCode: string;                  // BN000123 — font-mono
  gender?: Gender;
  dob?: string | null;                  // tính tuổi FE: differenceInYears(now, dob)
  yearOfBirth?: number | null;          // fallback khi không có dob
  avatarUrl?: string | null;
  bhytSummary?: string | null;          // null → badge "Không BHYT" variant muted
  doctorName?: string | null;
  roomName?: string | null;
  reasonForVisit?: string | null;
}

// VitalSignsSummaryCard
interface VitalSignsSummaryCardProps {
  vital?: VitalSignsResponse | null;
  measuredAt?: string | null;
  onViewAll: () => void;
  onAddNew?: () => void;                // undefined → ẩn nút (khi khoá)
}

// ClsOrderBatchCard
interface ClsOrderBatchCardProps {
  batch: ClsOrderBatch;
  defaultOpen?: boolean;
  canEdit: boolean;                     // false khi batch.payment_status === "PAID" hoặc encounter khoá
  onPrint: (batchId: string) => void;
  onEdit?: (batchId: string) => void;
  onDelete?: (batchId: string) => void;
}

// Kiểu dữ liệu đề xuất cho BE (gửi Lành duyệt — xem §11)
type ClsPaymentStatus = "UNPAID" | "PAID" | "PARTIAL" | "CANCELLED";
interface ClsOrderBatch {
  id: string;
  encounter_id: string;
  batch_no: number;                     // 1, 2, 3… do BE cấp
  created_at: string;
  created_by_name: string;
  payment_status: ClsPaymentStatus;
  total_amount: number;
  invoice_no?: string | null;
  paid_at?: string | null;
  items: ClsOrderBatchItem[];
}
interface ClsOrderBatchItem {
  id: string;
  kind: "LAB" | "RAD";
  code: string;
  name: string;
  priority: "NORMAL" | "URGENT" | "EMERGENCY";
  status: string;                       // ordered | sample_taken | processing | done | cancelled
  price: number;
  has_result: boolean;
}

// EncounterTabs
type EncounterTabValue =
  | "emr" | "history" | "cls-orders" | "cls-results"
  | "diagnosis" | "prescription" | "followup" | "files";

interface EncounterTabsProps {
  encounter: EncounterDetailResponse;
  value: EncounterTabValue;
  onValueChange: (v: EncounterTabValue) => void;
  canEdit: boolean;
  counters: Partial<Record<EncounterTabValue, number>>;
}

// UnsavedChangesDialog
interface UnsavedChangesDialogProps {
  open: boolean;
  onSaveAndContinue: () => Promise<void>;
  onDiscard: () => void;
  onStay: () => void;
  isSaving?: boolean;
}
```

---

## 7. Token & className

### 7.1. Bảng token

| Vùng | Token | className gợi ý | Lý do |
|---|---|---|---|
| Nền trang | `--bg-base` | `bg-background` | chuẩn §1 |
| Card sidebar | `--bg-surface` + `--border-subtle` | `bg-card border border-border rounded-lg p-4` | §3 chuẩn panel |
| Toolbar sticky | `--bg-surface` | `sticky top-0 z-20 h-14 bg-card/95 backdrop-blur border-b border-border` | tránh che nội dung khi cuộn |
| TabsList sticky | `--bg-surface` | `sticky top-14 z-10 bg-card/95 backdrop-blur` | tab luôn với tới được |
| Tab active | `--accent-primary` | `data-[state=active]:text-primary data-[state=active]:border-b-2 data-[state=active]:border-primary` | link/tab active dùng primary |
| Banner khoá | `--status-warning` | `border-[color:var(--status-warning)]/30 bg-[color:var(--status-warning)]/10 text-[color:var(--status-warning)]` | khoá = cảnh báo, KHÔNG dùng critical (không phải lỗi) |
| Badge "Chưa thanh toán" | `--status-warning` | `<HisStatusBadge variant="warning">Chưa thanh toán</HisStatusBadge>` | ánh xạ `PENDING→warning` |
| Badge "Đã thanh toán" | `--status-done` | `<HisStatusBadge variant="done">Đã thanh toán</HisStatusBadge>` | `PAID→done` |
| Badge "Thanh toán một phần" | `--status-progress` | `variant="progress"` | |
| Badge "Đợt đã huỷ" | `--status-critical` | `variant="critical"` | `CANCELLED→critical` |
| Badge BHYT | `--status-insurance` | `variant="insurance"` | |
| Sinh hiệu bất thường | `--status-critical` | `text-[color:var(--status-critical)] font-semibold` + icon `AlertTriangle` | **kèm icon** — WCAG 1.4.1 |
| Nút "Kết thúc khám" | `--primary` | `variant="default"` (KHÔNG `bg-green-600`) | file cũ dòng 390 hardcode |
| Nút xoá | `--status-critical` | `variant="ghost" className="text-destructive"` | file cũ dùng `text-red-500` |
| Tiền / mã BN / mã dịch vụ | — | `font-mono tabular-nums` | §2 chuẩn |
| Metadata | `--text-muted` | `text-sm text-muted-foreground` | |
| Card title | — | `text-sm font-semibold` (CardTitle) | §2 làm rõ |
| Tiêu đề đợt CLS (ngoài Card header) | — | `text-lg font-semibold` cho tiêu đề khu vực "Đợt chỉ định" | §2 |
| Focus | `--focus-ring` | `focus-visible:ring-2 focus-visible:ring-[color:var(--focus-ring)]` | §8 |

### 7.2. Cấm tuyệt đối trong PR này
`bg-green-*`, `bg-yellow-*`, `bg-red-*`, `bg-blue-*`, `bg-purple-*`, `text-red-500/600`, `text-green-600`, `bg-red-50`, `border-green-200`, `#hex`, `style={{ backgroundColor }}`.
File cũ đang vi phạm tại: `EncounterDetailClient.tsx` dòng 265, 390, 401–402, 433–434, 446, 452, 630, 641, 650, 657, 823, 873 · `LabRadOrderList.tsx` dòng 12–26 · `EncounterStatusBadge.tsx` dòng 11–14 · `EncounterTimeline.tsx` dòng 27–31. **Phải khử hết trong phạm vi refactor này.**

---

## 8. Grid & breakpoint

| Breakpoint | Sidebar | Tabs | Padding | Tab label | Ghi chú |
|---|---|---|---|---|---|
| `< 768px` (dọc, hiếm) | Sheet | `col-span-12` | `p-4` | ngắn, cuộn ngang | không tối ưu, chỉ không vỡ |
| `768–1023px` (tablet dọc) | Sheet `sm:max-w-xl` + `PatientStripBar` | `col-span-12` | `p-4` | ngắn, cuộn ngang | |
| `1024–1279px` (`lg`, **tablet ngang — ưu tiên**) | `lg:col-span-4` sticky | `lg:col-span-8` | `p-4` | ngắn | mục tiêu chính |
| `≥ 1280px` (`xl`) | `xl:col-span-3` sticky | `xl:col-span-9` | `p-6` | đầy đủ | |

**Class grid chuẩn:**
```
grid grid-cols-12 gap-4 xl:gap-6
  sidebar: hidden lg:block lg:col-span-4 xl:col-span-3
  tabs:    col-span-12 lg:col-span-8 xl:col-span-9
```

**Sticky sidebar:**
```
sticky top-[7rem] max-h-[calc(100vh-8rem)] overflow-y-auto space-y-4 pr-1
scrollbar mảnh; KHÔNG dùng h-screen (sẽ vỡ khi có banner)
```
`7rem` = topbar 56px + toolbar 56px. Nếu banner khoá hiện → dùng CSS var `--encounter-sticky-top` set bằng `style` trên container cha, tránh hardcode nhiều giá trị.

**Cuộn:** vùng tabs cuộn theo trang (không cuộn nội bộ) để cuộn 1 ngón trên tablet không bị "scroll trap".

---

## 9. State đầy đủ

### 9.1. Trạng thái encounter → UI

| `status` | Toolbar | Banner | Form các tab | CTA chính |
|---|---|---|---|---|
| `WAITING` | badge `waiting` "Chờ khám" + Chuyển phòng | — | read-only | **Bắt đầu khám** (primary) |
| `IN_PROGRESS` | badge `progress` "Đang khám" + Chuyển phòng + Chờ KQ CLS | — | editable | **Kết thúc khám** |
| `WAITING_CLS` *(mới)* | badge `waiting` "Chờ kết quả CLS" | info: "Đang chờ kết quả CLS — có thể tiếp tục khám bệnh nhân khác" | editable | **Tiếp tục khám** |
| `DONE` | badge `done` "Hoàn thành", ẩn nút hành động, giữ nút In | **Banner khoá** | read-only toàn bộ | **Tạo bản đính chính** (nếu có quyền) |
| `CANCELLED` | badge `critical` "Đã huỷ" | banner critical "Lượt khám đã huỷ" | read-only | — |

### 9.2. Loading
- **Toàn trang**: skeleton đúng khung layout mới — 1 dòng toolbar `h-14`, sidebar `col-span-3` 4 khối skeleton, tabs `col-span-9` 1 khối `h-[520px]`. (Skeleton hiện tại ở dòng 111–122 là 3 cột — phải sửa cho khớp layout mới, nếu không sẽ nhảy layout.)
- **Trong tab**: mỗi tab tự loading skeleton nội bộ, **không** làm trắng cả trang.
- **Button submit**: `disabled` + `<Loader2 className="h-4 w-4 animate-spin" />` + đổi chữ ("Đang lưu…").
- **Chuyển tab**: instant (dữ liệu prefetch bằng TanStack Query `staleTime` 30s), không spinner.

### 9.3. Empty (dùng `EmptyState`, icon 48px `text-muted-foreground`)

| Tab | Icon | Tiêu đề | Phụ đề | CTA |
|---|---|---|---|---|
| Bệnh án | `FileText` | Chưa có nội dung bệnh án | Chọn mẫu bệnh án để bắt đầu ghi chép. | Chọn mẫu bệnh án |
| Tiền sử | `HeartPulse` | Chưa ghi nhận tiền sử | Thêm dị ứng, bệnh nền để hệ thống cảnh báo khi kê đơn. | Thêm tiền sử |
| Cận lâm sàng | `FlaskConical` | Chưa có chỉ định cận lâm sàng | Tạo đợt chỉ định để gửi yêu cầu XN/CĐHA cho bệnh nhân. | Tạo đợt chỉ định mới |
| Kết quả CLS | `ClipboardCheck` | Chưa có kết quả | Kết quả sẽ hiện tại đây khi khoa CLS trả về. | — |
| Chẩn đoán | `Stethoscope` | Chưa có chẩn đoán | Thêm mã ICD-10 để hoàn tất bệnh án. | Thêm chẩn đoán |
| Đơn thuốc | `Pill` | Chưa kê đơn thuốc | Thêm thuốc vào đơn hoặc sao chép đơn cũ của bệnh nhân. | Thêm thuốc |
| Tái khám | `CalendarClock` | Chưa hẹn tái khám | Đặt lịch tái khám để nhắc bệnh nhân qua SMS/Zalo. | Đặt lịch tái khám |
| Tập tin | `Paperclip` | Chưa có tập tin đính kèm | Kéo thả ảnh, PDF hoặc file DICOM vào đây. | Chọn tập tin |
| Sidebar — sinh hiệu | `Activity` | Chưa có sinh hiệu | — | Ghi sinh hiệu |
| Sidebar — lịch sử | `History` | Lần khám đầu tiên | Bệnh nhân chưa có lượt khám nào trước đây. | — |

### 9.4. Error
- Không tải được encounter → full-page error, icon `AlertTriangle`, "Không tải được thông tin lượt khám", CTA `[Thử lại]` + `[Quay lại danh sách]`.
- Lỗi trong 1 tab → error inline trong tab đó (`Alert` destructive + nút "Thử lại"), các tab khác vẫn dùng được.
- Lỗi mutate (lưu, chuyển phòng, kết thúc) → toast destructive, giữ nguyên dữ liệu form (không reset).
- Validation → inline dưới field, hiện khi blur.

### 9.5. Bảo vệ dữ liệu chưa lưu (G5 — bắt buộc trong PR này)
Khi chuyển tab / rời route mà tab hiện tại có draft chưa lưu → `UnsavedChangesDialog`:

| Nút | Nhãn | Variant | Hành vi |
|---|---|---|---|
| 1 | Lưu và tiếp tục | `default` (primary) | submit → chuyển |
| 2 | Rời đi, không lưu | `outline` | bỏ draft → chuyển |
| 3 | Ở lại trang | `ghost` | đóng dialog |

Áp dụng cho tab `emr`, `diagnosis` (form nhập nhanh), `prescription`. Kèm `beforeunload` cho đóng tab trình duyệt.

---

## 10. Microcopy tiếng Việt

| Vị trí | Nội dung | Ghi chú |
|---|---|---|
| Banner khoá (title) | **Bệnh án đã khoá — chỉ xem** | icon `Lock`, đậm |
| Banner khoá (mô tả) | Lượt khám kết thúc lúc {HH:mm dd/MM/yyyy} bởi {tên BS}. Mọi thay đổi phải tạo bản đính chính. | `text-sm` |
| Banner khoá (CTA) | Tạo bản đính chính | chỉ khi có quyền `ENCOUNTER_AMEND` |
| Banner khoá (đã có đính chính) | Đã có {n} bản đính chính · [Xem] | |
| Toolbar — chuyển phòng | Chuyển phòng | placeholder Select: "Chọn phòng khám" |
| Confirm chuyển phòng | **Chuyển bệnh nhân sang phòng khác?** / Bệnh nhân {tên} sẽ được chuyển từ {phòng cũ} sang {phòng mới}. Bác sĩ phòng mới sẽ tiếp nhận lượt khám này. | `[Huỷ]` `[Xác nhận chuyển]` |
| Toolbar — chờ CLS | Chờ kết quả CLS | tooltip: "Tạm dừng ca khám, gọi bệnh nhân tiếp theo" |
| Toast chờ CLS | Đã chuyển sang trạng thái chờ kết quả cận lâm sàng. | success |
| Toolbar — kết thúc | Kết thúc khám | |
| Confirm kết thúc | **Kết thúc lượt khám?** / Sau khi kết thúc, bệnh án sẽ bị khoá và chỉ có thể sửa bằng bản đính chính. | `[Xem lại]` `[Kết thúc khám]` |
| Confirm kết thúc — cảnh báo thiếu | ⚠ Chưa có chẩn đoán ICD-10. Bệnh án thiếu chẩn đoán sẽ không xuất được XML giám định BHYT. | hiện trong dialog nếu `diagnoses.length === 0` |
| Confirm kết thúc — chưa ký | ⚠ Bệnh án chưa được ký số. | |
| Toast kết thúc | Đã kết thúc lượt khám. Bệnh án đã khoá. | success |
| Nút bắt đầu | Bắt đầu khám | |
| Đợt CLS — tiêu đề | Đợt #{n} · {dd/MM/yyyy HH:mm} · {tên BS} | |
| Đợt CLS — badge | Chưa thanh toán / Đã thanh toán / Thanh toán một phần / Đã huỷ | luôn kèm icon |
| Đợt CLS — tổng | Tổng: {số} ₫ | `font-mono tabular-nums` |
| Đợt CLS — nút | Tạo đợt chỉ định mới · In phiếu · Sửa đợt · Huỷ đợt | |
| Confirm huỷ đợt | **Huỷ đợt chỉ định #{n}?** / Toàn bộ {m} dịch vụ trong đợt này sẽ bị huỷ. Không thể hoàn tác. | destructive |
| Không cho sửa đợt đã trả tiền | Đợt này đã thanh toán, không thể chỉnh sửa. Liên hệ thu ngân nếu cần điều chỉnh. | tooltip trên nút disabled |
| Strip bar tablet | Hồ sơ | nút mở drawer, `aria-label="Mở hồ sơ bệnh nhân"` |
| Sheet title | Hồ sơ bệnh nhân | |
| Sidebar — BHYT rỗng | Không có BHYT | badge muted, không dùng `insurance` |
| Sidebar — sinh hiệu | Sinh hiệu · đo lúc {HH:mm} | nút "Xem tất cả", "Ghi sinh hiệu" |
| Sidebar — dị ứng | Dị ứng thuốc | icon `AlertTriangle`, token critical |
| Sidebar — lịch sử | Lịch sử khám | nút "Xem tất cả" |
| Unsaved dialog | **Có thay đổi chưa được lưu** / Bạn có muốn lưu nội dung đang nhập trước khi rời khỏi tab này không? | 3 nút §9.5 |

---

## 11. Yêu cầu gửi Lành (architect) — KHÔNG tự quyết API

| # | Nội dung | Mức | Vì sao FE cần |
|---|---|---|---|
| A1 | `GET /api/v1/encounters/{id}/cls-batches` trả `ClsOrderBatch[]` (§6) gồm `batch_no`, `payment_status`, `total_amount`, `invoice_no` | **P0** | Không có → tab CLS không nhóm được theo đợt, phải hiển thị phẳng như hiện tại |
| A2 | `POST /api/v1/encounters/{id}/cls-batches` (tạo đợt gồm nhiều lab+rad order trong 1 transaction) | **P0** | Tránh 2 request lab/rad rời rạc rồi lệch đợt |
| A3 | `DELETE /api/v1/cls-batches/{batchId}` — chặn khi `payment_status = PAID` | P1 | |
| A4 | Bổ sung `EncounterStatus = "WAITING_CLS"` | P1 | Nút "Chờ kết quả CLS" cần trạng thái đích |
| A5 | `POST /api/v1/encounters/{id}/transfer-room { room_id, reason }` (hoặc xác nhận dùng `PUT /encounters/{id}` sẵn có) + ghi audit | P1 | Chuyển phòng phải có audit log |
| A6 | `POST /api/v1/encounters/{id}/amendments` + permission code `ENCOUNTER_AMEND` + field `amendment_count` trong detail | P1 | Nút "Tạo bản đính chính" |
| A7 | Bổ sung `EncounterDetailResponse`: `patient_summary.avatar_url`, `patient_summary.age`, `closed_by_name`, `allergies_summary[]`, `chronic_conditions[]` | P1 | Sidebar hiện đủ mà không cần 4 request phụ |
| A8 | `GET /encounters?patient_id=&page_size=5&sort=-created_at` — xác nhận đã hỗ trợ | P2 | Card lịch sử khám |

**Cho tới khi A1/A2 sẵn sàng:** tab Cận lâm sàng render 1 "đợt ảo" duy nhất (`Đợt #1`) gom toàn bộ lab+rad order hiện có, badge thanh toán hiện `—` (không đoán). **Không fake `payment_status`.**

---

## 12. Route cũ — giữ hay bỏ

| Route | Quyết định | Lý do |
|---|---|---|
| `/labrad/results` | **GIỮ NGUYÊN** | Là bàn làm việc của KTV/kỹ thuật viên CLS — họ nhập kết quả theo *hàng loạt phiếu trong ngày*, không theo từng bệnh nhân. Xoá sẽ chặn hoàn toàn nghiệp vụ của role `KyThuatVien`. Bác sĩ chỉ đơn giản không cần vào nữa. |
| `/labrad/results/[id]` | **GIỮ NGUYÊN** | Deep-link từ thông báo / in phiếu. |
| `/labrad`, `/labrad/partners` | **GIỮ NGUYÊN** | Cấu hình tích hợp — role admin. |
| `/prescriptions` | **GIỮ NGUYÊN** | Danh sách đơn cho dược sĩ cấp phát + tra cứu ĐTQG. |
| `/prescriptions/[id]` | **GIỮ NGUYÊN** | Chi tiết đơn cho dược sĩ, in lại, deep-link QR. |
| `/prescriptions/new` | **GIỮ, nhưng ĐỔI VAI TRÒ** | Không xoá (còn ca kê đơn không gắn encounter). **Nhưng**: nếu vào `/prescriptions/new?encounter_id=X` → `redirect("/encounters/X?tab=prescription")`. Và **gỡ mọi entry point từ màn khám** trỏ tới route này. |

**Điều hướng menu (`lib/config/nav-items.ts`):**
- `/labrad/results` gắn permission `LAB_RESULT_WRITE` / `RAD_RESULT_WRITE` → **bác sĩ không thấy trong sidebar** (hiện đúng cơ chế `isItemVisible`). Kiểm tra lại permission gán cho role `BacSi` — nếu bác sĩ đang có `LAB_RESULT_WRITE` thì đề nghị Lành/Đăng rà lại ma trận quyền.
- Trong màn khám: **cấm** mọi `<Link href="/labrad/results">` và `<Link href="/prescriptions/new">`.

---

## 13. Phím tắt (theo chuẩn §7 design-system-standards)

| Phím | Hành động | Điều kiện |
|---|---|---|
| `F8` / `Ctrl+S` | Lưu nội dung tab hiện tại | `canEdit` |
| `F9` | Mở dropdown In | luôn |
| `Alt+1..8` | Nhảy tới tab thứ n | luôn |
| `Ctrl+Enter` | Kết thúc khám (mở confirm) | `IN_PROGRESS` |
| `Esc` | Đóng Dialog/Sheet/drawer | |
| `F1` | Mở `ShortcutsModal` | |

Hiện `<kbd>` trên nút chính (Lưu, In, Kết thúc) — theo pattern đã có ở màn reception.

---

## 14. A11y checklist (Phượng test theo bảng này)

- [ ] Contrast ≥ 4.5:1 mọi text, ≥ 3:1 icon/viền — **kiểm cả light và dark**.
- [ ] Tabs dùng `role="tablist"/"tab"/"tabpanel"` (shadcn Radix có sẵn) — **không** tự chế bằng `<button>` rời.
- [ ] Mũi tên ←/→ chuyển tab, `Home`/`End` về tab đầu/cuối (Radix mặc định — không được `preventDefault`).
- [ ] `TabsTrigger` có `aria-controls` + panel có `aria-labelledby` (Radix tự sinh — không override `id`).
- [ ] Mọi tab trigger cao ≥ 44px (`min-h-[44px] px-4`).
- [ ] Focus ring hiện rõ khi Tab, dùng `--focus-ring`; không có `outline:none` trần.
- [ ] Badge thanh toán có **icon + chữ**, không chỉ màu (`HisStatusBadge` đã đảm bảo).
- [ ] Sinh hiệu bất thường: kèm icon `AlertTriangle` + `aria-label="Giá trị bất thường"`, không chỉ đổi màu chữ đỏ.
- [ ] Banner khoá có `role="status"` + `aria-live="polite"`.
- [ ] Sheet hồ sơ: focus trap, `Esc` đóng, focus trả về nút "Hồ sơ" khi đóng.
- [ ] Nút icon-only (In, Sửa, Xoá đợt) có `aria-label` tiếng Việt đầy đủ.
- [ ] Bảng dịch vụ trong đợt: `<caption class="sr-only">Danh sách dịch vụ chỉ định đợt #n</caption>`.
- [ ] Chuyển tab công bố qua `aria-live` vùng tiêu đề panel (cho screen reader biết đã đổi).
- [ ] Sidebar sticky **không** che nội dung khi zoom 200%; test zoom 200% ở 1280px.
- [ ] Không cuộn ngang body ở 1024px và 768px.
- [ ] Số liệu (tiền, mã) đọc được bằng screen reader (`font-mono` không ảnh hưởng, nhưng `₫` cần `aria-label="đồng"` ở tổng tiền).
- [ ] `forced-colors` mode: banner khoá và badge vẫn phân biệt được.

---

## 15. Hand-off cho Nam (frontend)

### 15.1. File phải sửa
| File | Việc |
|---|---|
| `frontend/app/(dashboard)/encounters/[id]/_components/EncounterDetailClient.tsx` | **Tách từ 915 → ~180 dòng**: chỉ giữ orchestration (fetch, tab state, layout). Chuyển `VitalSignsTabContent` (525–679), `DiagnosisTabContent` (701–887), `VitalSummary` (889–915) ra file riêng. Khử toàn bộ hardcode màu (§7.2). |
| `frontend/app/(dashboard)/encounters/[id]/page.tsx` | Không đổi (server component mỏng). |
| `frontend/components/domain/EncounterStatusBadge.tsx` | Wrap `HisStatusBadge`: `WAITING→waiting`, `IN_PROGRESS→progress`, `DONE→done`, `CANCELLED→critical`, `WAITING_CLS→waiting`. Bỏ `STATUS_MAP` hardcode dòng 11–14. |
| `frontend/components/domain/LabRadOrderList.tsx` | Deprecate → thay bằng `ClsOrderItemTable`; xoá 2 map hardcode dòng 12–26. |
| `frontend/components/domain/EncounterTimeline.tsx` | Đổi `color: "text-blue-500"…` (dòng 27–31) sang token `--chart-1..6`. |
| `frontend/lib/api/encounters.ts` + `types.ts` | Thêm hàm/kiểu cho A1–A6 **sau khi Lành duyệt**. |
| `frontend/lib/hooks/use-cls-orders.ts` | Thêm `useClsBatches(encounterId)`, `useCreateClsBatch`, `useDeleteClsBatch`. |
| `frontend/lib/config/nav-items.ts` | Rà permission `/labrad/results` (§12). |

### 15.2. File tạo mới
`components/domain/`: `EncounterToolbar.tsx`, `EncounterLockBanner.tsx`, `RoomTransferSelect.tsx`, `PatientStripBar.tsx`, `EncounterPatientSidebar.tsx`, `PatientIdentityCard.tsx`, `VitalSignsSummaryCard.tsx`, `AllergyAlertCard.tsx`, `MedicalHistoryCard.tsx`, `PatientVisitHistoryCard.tsx`, `EncounterTabs.tsx`, `FollowUpTabPanel.tsx`, `UnsavedChangesDialog.tsx`
`components/domain/cls/`: `ClsOrderBatchCard.tsx`, `ClsBatchPaymentBadge.tsx`, `ClsOrderItemTable.tsx`, `ClsOrderBatchDialog.tsx`
`app/(dashboard)/encounters/[id]/_components/tabs/`: `EmrTabPanel.tsx`, `HistoryTabPanel.tsx`, `ClsOrderTabPanel.tsx`, `ClsResultTabPanel.tsx`, `DiagnosisTabPanel.tsx`, `PrescriptionTabPanel.tsx`, `FileTabPanel.tsx`

### 15.3. Snippet khung layout (minh hoạ, không phải code production)

```tsx
<div className="space-y-4">
  <EncounterToolbar {...} />           {/* sticky top-0 z-20 h-14 */}
  {isDone && <EncounterLockBanner {...} />}
  {encounter.alert_over_12h && <EncounterAlertBanner {...} />}
  <PatientStripBar className="lg:hidden" onOpen={() => setSheetOpen(true)} {...} />

  <div className="grid grid-cols-12 gap-4 xl:gap-6">
    <aside className="hidden lg:block lg:col-span-4 xl:col-span-3">
      <div className="sticky top-[7rem] max-h-[calc(100vh-8rem)] overflow-y-auto space-y-4 pr-1">
        <EncounterPatientSidebar variant="desktop" {...} />
      </div>
    </aside>

    <section className="col-span-12 lg:col-span-8 xl:col-span-9">
      <EncounterTabs value={tab} onValueChange={setTab} {...} />
    </section>
  </div>

  <Sheet open={sheetOpen} onOpenChange={setSheetOpen}>
    <SheetContent side="right" className="sm:max-w-xl px-6 pb-6 overflow-y-auto">
      <SheetHeader><SheetTitle>Hồ sơ bệnh nhân</SheetTitle></SheetHeader>
      <EncounterPatientSidebar variant="drawer" {...} />
    </SheetContent>
  </Sheet>
</div>
```

```tsx
{/* TabsList */}
<TabsList className="sticky top-14 z-10 w-full justify-start gap-1 overflow-x-auto
                     bg-card/95 backdrop-blur border-b border-border rounded-none p-0">
  <TabsTrigger value="cls-orders"
    className="min-h-[44px] px-4 gap-2 rounded-none border-b-2 border-transparent
               data-[state=active]:border-primary data-[state=active]:text-primary
               data-[state=active]:bg-transparent data-[state=active]:shadow-none">
    <FlaskConical className="h-4 w-4" aria-hidden="true" />
    <span className="hidden xl:inline">Cận lâm sàng</span>
    <span className="xl:hidden">CLS</span>
    {unpaidBatches > 0 && (
      <span className="ml-1 rounded-full px-1.5 text-xs font-medium
                       bg-[color:var(--status-warning)]/10 text-[color:var(--status-warning)]">
        {unpaidBatches}
      </span>
    )}
  </TabsTrigger>
</TabsList>
```

### 15.4. Thứ tự triển khai đề xuất
1. **PR1 — Refactor thuần layout** (không đổi API): tách file, dựng toolbar + sidebar sticky + 8 tab + deep-link `?tab=`, khử hardcode màu. Tab CLS tạm dùng "đợt ảo #1".
2. **PR2 — Khoá & an toàn dữ liệu**: `EncounterLockBanner`, `UnsavedChangesDialog`, `RoomTransferSelect`, "Chờ kết quả CLS" (chờ A4/A5/A6).
3. **PR3 — Đợt chỉ định CLS thật** (chờ A1–A3): `ClsOrderBatchCard` + Dialog tạo đợt + badge thanh toán.

---

## 16. Definition of Done

- [ ] Bác sĩ hoàn tất 1 ca khám (sinh hiệu → CLS → kết quả → chẩn đoán → đơn thuốc → tái khám) **không rời `/encounters/[id]`**.
- [ ] Sidebar hồ sơ + sinh hiệu **không đổi và không re-mount** khi chuyển tab (verify bằng React DevTools).
- [ ] `?tab=` deep-link hoạt động, F5 giữ nguyên tab.
- [ ] `status = DONE` → banner khoá hiện, mọi input `disabled`, chỉ còn nút In + Tạo bản đính chính (theo quyền).
- [ ] Tab CLS nhóm theo đợt, mỗi đợt có badge thanh toán bằng `HisStatusBadge`.
- [ ] `grep -E "bg-(green|yellow|red|blue|purple)-[0-9]{2,3}|text-(red|green)-[0-9]{2,3}|#[0-9A-Fa-f]{6}"` trên các file đụng tới → **0 kết quả**.
- [ ] Kiểm chứng thực tế: light + dark + 1024px + 1280px + zoom 200%.
- [ ] Đủ 16 mục A11y checklist §14.
- [ ] Không còn link tới `/labrad/results` và `/prescriptions/new` trong màn khám.
