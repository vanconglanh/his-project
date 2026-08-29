# Audit layout/popup toàn hệ thống — Pro-Diab HIS (2026-08-29)

**Kim chỉ nam:** Dễ nhìn — dễ thao tác — ít thao tác nhất vẫn nhập đúng dữ liệu.

**Người audit:** Linh (designer) · **Phạm vi:** toàn bộ Dialog/Sheet, các màn hình chính, form nhập liệu, token màu/typography, đối chiếu `docs/design/design-system-standards.md`.

**Giới hạn phương pháp:** phiên làm việc này KHÔNG có công cụ điều khiển trình duyệt (không có Playwright/DevTools MCP khả dụng) nên không chụp được screenshot trực tiếp hay resize viewport thực tế. Audit thực hiện bằng **đọc code + suy luận cascade CSS/Tailwind** (đã xác minh cơ chế, không phải phỏng đoán — xem finding #1). Đề nghị Nam xác nhận lại bằng mắt sau khi đọc report này; nếu cần ảnh chụp thật, nhờ Chi (qc) chạy lại bằng Playwright MCP.

---

## Finding #1 (P0 — ROOT CAUSE của popup "Tạo đợt chỉ định CLS" bị nhỏ)

**Khía cạnh:** Dialog/Sheet base component — cascade Tailwind bug, ảnh hưởng TOÀN HỆ THỐNG.

### Mô tả kỹ thuật
`frontend/components/ui/dialog.tsx:56` — class mặc định của `DialogContent`:
```
max-w-[calc(100%-2rem)] ... sm:max-w-sm
```
`sm:max-w-sm` (384px) là mặc định cứng cho MỌI Dialog khi viewport ≥ 640px (tức gần như mọi laptop/tablet ngang thực tế theo CLAUDE.md).

Khi một màn hình override bằng `className="max-w-4xl"` (không có tiền tố `sm:`), `cn()` dùng `twMerge` để dedupe. `twMerge` chỉ gộp các class **cùng breakpoint/modifier**. `max-w-4xl` (không modifier) và `sm:max-w-sm` (modifier `sm:`) được xem là **2 nhóm khác nhau** → cả hai cùng tồn tại trong class list cuối cùng. Trong CSS sinh ra bởi Tailwind, rule bọc trong `@media (min-width:640px)` (tức `.sm\:max-w-sm`) luôn được đặt **sau** rule thường trong stylesheet → khi viewport ≥640px, `sm:max-w-sm` **thắng cascade**, đè `max-w-4xl` dù nó không có mặt trong className list "hiển nhiên hơn". Kết quả: dialog luôn bị kẹp ở 384px trên mọi màn hình thực tế, bất kể dev đã set `max-w-xl/max-w-lg/max-w-4xl`.

**Đây chính là nguyên nhân của popup "Tạo đợt chỉ định cận lâm sàng" trong ảnh chụp** — `frontend/components/domain/cls/ClsRoundCreateDialog.tsx:96` đã set `max-w-4xl` (896px, đúng chuẩn "Dialog bảng phức tạp" mục 3 design-system-standards.md) nhưng bị override ngược về 384px do bug cascade trên.

### Danh sách Dialog bị ảnh hưởng (override thiếu tiền tố `sm:`)
| File:dòng | className hiện tại | Ý định | Kích thước thực tế render (≥640px) |
|---|---|---|---|
| `frontend/components/domain/cls/ClsRoundCreateDialog.tsx:96` | `max-w-4xl` | 896px (bảng phức tạp: tìm dịch vụ + giỏ hàng) | **384px** — đúng như ảnh chụp |
| `frontend/components/layout/ShortcutsModal.tsx:66` | `max-w-xl max-h-[80vh] overflow-y-auto` | 576px | **384px** |
| `frontend/components/domain/EncounterAmendDialog.tsx:52` | `max-w-xl` (Select + 2 Textarea) | 576px | **384px** |
| `frontend/app/(dashboard)/reports/schedules/_components/ScheduleFormDialog.tsx:184` | `max-w-lg` (form nhiều field, scroll `max-h-[70vh]`) | 512px | **384px** |
| `frontend/app/(dashboard)/admin/notifications-config/page.tsx:135` | `max-w-md` | 448px | **384px** |
| `frontend/components/domain/SignPrescriptionWizard.tsx:109` | `max-w-md` (wizard ký đơn nhiều bước) | 448px | **384px** |
| `frontend/components/domain/QrPaymentModal.tsx:83` | `max-w-sm text-center` | 384px (trùng default, không lỗi nhưng dư thừa) | 384px |
| `frontend/components/domain/CashierShiftOpenDialog.tsx:39` | `max-w-sm` | 384px (trùng default) | 384px |
| `frontend/components/domain/CashierShiftCloseDialog.tsx:52` | `max-w-sm` | 384px (trùng default) | 384px |

**Các Dialog KHÔNG bị lỗi này** (đã dùng đúng tiền tố `sm:`, dùng làm mẫu chuẩn để fix các file trên): `EmrSignDialog.tsx:36` (`sm:max-w-md`), `ExportReportDialog.tsx:76` (`sm:max-w-md`), `bhyt/BhytExportForm.tsx:76`, `bhyt/BhytSignDialog.tsx:34`, `bhyt/BhytReconcileTable.tsx:80`, `app/(dashboard)/bhyt/page.tsx:163`.

**Sheet không bị lỗi này** — toàn bộ 13 usage `SheetContent` grep được đều dùng đúng `w-full sm:max-w-xl/2xl/lg/md` (có tiền tố `sm:`), khớp base `SheetContent` cũng dùng `data-[side=right]:sm:max-w-sm`. Không cần sửa Sheet.

### Đề xuất fix (2 lớp, làm cả 2 để tránh tái phát)
1. **Fix tận gốc ở component nền** `frontend/components/ui/dialog.tsx:56`: đổi default từ `sm:max-w-sm` → `sm:max-w-lg` (512px là baseline hợp lý hơn cho form 3-4 field, thay vì 384px quá chật) — giảm rủi ro cho các Dialog tương lai quên override.
2. **Fix từng usage sai tiền tố** (bảng trên): đổi `max-w-xxx` → `sm:max-w-xxx` để khớp đúng modifier với class base cần override. Ví dụ `ClsRoundCreateDialog.tsx:96`:
   ```
   <DialogContent className="sm:max-w-4xl">
   ```
3. Bổ sung **lint/quy ước cho Nam**: mọi override `max-w-*` trên `DialogContent`/`SheetContent` bắt buộc đi kèm tiền tố `sm:` (ghi thêm 1 dòng vào `design-system-standards.md` mục 3 làm chú thích kỹ thuật, tránh tái phát — Linh sẽ bổ sung sau khi Nam xác nhận fix).

---

## Finding #2 (P1) — Nội dung 2 cột bên trong ClsRoundCreateDialog vẫn chật ngay cả khi dialog đúng kích thước

`frontend/components/domain/cls/ClsRoundCreateDialog.tsx:104-197`: layout `grid gap-4 md:grid-cols-2` nhồi cả khối "Tìm dịch vụ" (danh sách kết quả `max-h-72 overflow-y-auto`) và khối "Dịch vụ đã chọn" (`max-h-56`) side-by-side. Sau khi fix Finding #1 (896px), 2 cột sẽ có ~430px/cột — đủ dùng, nhưng danh sách dịch vụ CLS catalog có thể có tên dài (vd "Siêu âm ổ bụng tổng quát có Doppler màu") dễ bị truncate (`truncate` đang dùng ở dòng 140, 163). Đây là hệ quả phụ, chấp nhận được sau khi width đúng 896px — không cần sửa thêm, chỉ cần QC lại sau fix #1.

**Đề xuất:** không đổi cấu trúc, chỉ theo dõi sau khi fix #1; nếu vẫn truncate nhiều trên thực tế, cân nhắc đổi `md:grid-cols-2` → `md:grid-cols-[1.2fr_1fr]` để ưu tiên cột tìm kiếm rộng hơn cột giỏ hàng.

---

## Finding #3 (P2) — Cỡ chữ dưới ngưỡng thang chuẩn (`text-[9px]/[10px]/[11px]`)

Standards mục 2 quy định chỉ dùng thang `--text-*` (nhỏ nhất là `text-xs` = 12px), **cấm** `text-[13px]` tuỳ tiện. Grep thấy 13 vị trí dùng `text-[9px]`, `text-[10px]`, `text-[11px]` cho badge/label phụ — nhỏ hơn cả `text-xs`, khó đọc trong môi trường ánh sáng mạnh/đeo găng theo đúng persona CLAUDE.md.

| File:dòng | Nội dung |
|---|---|
| `frontend/components/domain/PaymentDialog.tsx:212` | `text-[9px]` — tên phương thức thanh toán, dễ nhìn nhầm |
| `frontend/components/domain/DrugAutocomplete.tsx:92,95,98` | `text-[10px]` — badge "Hướng thần"/"Gây nghiện"/"OTC" — **cảnh báo an toàn thuốc, cần đọc rõ**, không nên nhỏ nhất hệ thống |
| `frontend/components/domain/DdiWarningPanel.tsx:82,87` | `text-[10px]` — cảnh báo tương tác thuốc (DDI) — cùng lý do, mức an toàn cao |
| `frontend/components/domain/ExpiryAlertCard.tsx:35` | `text-[10px]` — cảnh báo hết hạn thuốc |
| `frontend/components/domain/LowStockAlertCard.tsx:27` | `text-[10px]` — cảnh báo tồn kho thấp |
| `frontend/components/domain/NotificationDropdown.tsx:46,105` | `text-[10px]` |
| `frontend/components/domain/StockTable.tsx:74,79` | `text-[10px]` |
| `frontend/components/domain/prescriptions/CdssAlertBanner.tsx:132,136` | `text-[10px]` — cảnh báo CDSS kê đơn |
| `frontend/components/domain/diabetes/AiSuggestionPanel.tsx:72` | `text-[11px]` |
| `frontend/app/(dashboard)/patients/_components/PatientEditorLayout.tsx:277` | `text-[10px]` badge số đếm |
| `frontend/components/forms/TestLoginPanel.tsx:53` | `text-[10px]` (chỉ dev-only test panel, ưu tiên thấp) |

**Đề xuất:** đổi toàn bộ `text-[9/10/11px]` → `text-xs` (12px, đã có sẵn trong thang). Riêng nhóm cảnh báo an toàn thuốc (DrugAutocomplete, DdiWarningPanel, CdssAlertBanner) nên ưu tiên P1 thay vì P2 vì ảnh hưởng trực tiếp đến việc đọc đúng cảnh báo lâm sàng.

---

## Finding #4 (P2) — Badge dùng Tailwind palette cứng thay vì `HisStatusBadge`/token

Standards mục 1.2 và mục 4 cấm hardcode `bg-green-100`/`text-red-800`… và bắt buộc dùng `HisStatusBadge` cho mọi trạng thái nghiệp vụ. Ví dụ cụ thể phát hiện trong lần audit này (nằm trong phạm vi các file vừa đọc, không phải audit toàn bộ 42 file trùng pattern):

| File:dòng | Vi phạm |
|---|---|
| `frontend/components/domain/ExpiryAlertCard.tsx:35` | `bg-red-100 text-red-800 border-red-300` / `bg-yellow-100 text-yellow-800 border-yellow-300` hardcode thay vì `--status-critical`/`--status-warning` |
| `frontend/components/domain/LowStockAlertCard.tsx:27` | `bg-orange-100 text-orange-800 border-orange-300` hardcode thay vì `--status-warning` |

Grep rộng hơn cho thấy pattern `bg-{red,green,yellow,orange,blue,amber}-NNN` xuất hiện ở **42 file** trong `frontend/components/` — đây là khoản nợ token lớn, vượt phạm vi 1 lần audit layout. **Đề xuất:** tách thành task audit token riêng (`docs/design/color-token-audit-*.md`) do đây là vấn đề hệ thống, không phải lỗi cục bộ của popup CLS; ở đây chỉ liệt kê 2 ví dụ liên quan trực tiếp tới cảnh báo nghiệp vụ (thuốc hết hạn/tồn thấp) vì mức ảnh hưởng cao nhất tới an toàn thao tác.

---

## Finding #5 (P3) — Ghi nhận các Dialog ĐÃ tuân thủ đúng (để không sửa nhầm)

Không cần thay đổi, dùng làm baseline mẫu:
- `frontend/components/domain/EmrSignDialog.tsx:36`, `ExportReportDialog.tsx:76`, `bhyt/BhytExportForm.tsx:76`, `bhyt/BhytSignDialog.tsx:34`, `bhyt/BhytReconcileTable.tsx:80`, `app/(dashboard)/bhyt/page.tsx:163` — đều dùng đúng `sm:max-w-*`.
- Toàn bộ 13 `SheetContent` usage grep được đều đúng chuẩn `w-full sm:max-w-xl/2xl/lg/md overflow-y-auto px-6 pb-6` theo mục 3 design-system-standards.md — không có Sheet nào bị lỗi cascade tương tự Dialog.
- `frontend/components/domain/reports-engine/ReportKpiRow.tsx:21-25` — hex `#F0FDFA` v.v. là **key ánh xạ tint từ Report Engine BE** đúng theo bảng mục 6.4 design-system-standards.md, KHÔNG phải hardcode màu vi phạm — không flag nhầm khi audit sau này.

---

## Giới hạn chưa kiểm chứng được (do không có browser tool trong phiên này)
- Chưa resize thực tế các viewport (390/768/1024/1440px) để xác nhận vỡ layout responsive khác ngoài bug Dialog.
- Chưa so sánh trực quan light/dark mode thực tế (chỉ đối chiếu token định nghĩa trong CSS).
- Chưa đo số click/thao tác thực tế cho luồng "tạo bệnh nhân"/"kê đơn"/"nhập KQ XN" bằng cách bấm thử trên UI — phần này cần Nam hoặc Phượng (tester) đo lại bằng tay hoặc Chi chạy Playwright MCP.

**Đề nghị:** sau khi Nam fix Finding #1, nhờ Chi (qc) hoặc Phượng (tester) dùng Playwright MCP chụp lại đúng popup "Tạo đợt chỉ định cận lâm sàng" ở viewport 1280×800 (light + dark) để xác nhận đã lên đúng 896px trước khi đóng finding.

---

## Bảng tổng hợp

| Mức | Số finding | Nội dung |
|---|---|---|
| P0 | 1 (ảnh hưởng 9 file Dialog) | Bug cascade `sm:max-w-sm` đè override thiếu tiền tố `sm:` — nguyên nhân gốc popup CLS bị nhỏ |
| P1 | 1 | Layout 2 cột bên trong ClsRoundCreateDialog cần QC lại sau fix #1; đề xuất nâng nhóm cảnh báo an toàn thuốc trong Finding #3 lên ưu tiên xử lý sớm |
| P2 | 2 nhóm | `text-[9-11px]` dưới ngưỡng thang chuẩn (13 vị trí) · badge hardcode Tailwind palette (2 ví dụ cụ thể, 42 file cần audit riêng) |
| P3 | 1 | Ghi nhận baseline đúng chuẩn, tránh sửa nhầm |

**Thứ tự xử lý đề xuất:**
1. Nam sửa Finding #1 (đổi default `dialog.tsx` + 9 file override thiếu `sm:`) — ưu tiên tuyệt đối, đây là bug đang gây khó chịu trực tiếp cho người dùng.
2. Nam đổi `text-[9-11px]` → `text-xs` cho nhóm cảnh báo an toàn thuốc (DrugAutocomplete, DdiWarningPanel, CdssAlertBanner) trước, các badge còn lại làm sau.
3. Lên lịch audit token màu riêng (Finding #4) do phạm vi 42 file quá lớn cho lần audit này.
4. Chi (qc) xác nhận lại bằng Playwright MCP sau khi Nam fix xong Finding #1.

## Hand-off
- **Nam (frontend)**: sửa theo bảng Finding #1 + #3 (nhóm an toàn thuốc). File cần sửa: `frontend/components/ui/dialog.tsx`, `frontend/components/domain/cls/ClsRoundCreateDialog.tsx`, `frontend/components/layout/ShortcutsModal.tsx`, `frontend/components/domain/EncounterAmendDialog.tsx`, `frontend/app/(dashboard)/reports/schedules/_components/ScheduleFormDialog.tsx`, `frontend/app/(dashboard)/admin/notifications-config/page.tsx`, `frontend/components/domain/SignPrescriptionWizard.tsx`, `frontend/components/domain/DrugAutocomplete.tsx`, `frontend/components/domain/DdiWarningPanel.tsx`, `frontend/components/domain/prescriptions/CdssAlertBanner.tsx`.
- **Chi (qc)**: gác cổng xác nhận Finding #1 đã fix đúng (896px thực tế cho ClsRoundCreateDialog) bằng Playwright MCP, cả light + dark.
- **Đăng (po-analyst)**: không cần thay đổi PRD, đây là lỗi kỹ thuật CSS thuần tuý.
