# Kế hoạch xử lý ưu tiên cao (P1) — Design System module Report

> Nguồn: Audit của Linh (designer) đối chiếu `docs/design/design-system-standards.md`, 2026-07-07.
> Phạm vi bản kế hoạch này: **chỉ P1 (blocker)**. P2/P3 theo dõi riêng ở cuối file.
> Người thực hiện: Nam (frontend). Reviewer DS: Linh.

## Bối cảnh

Module Report chạy được nhưng tồn tại **2 hệ màu song song đều lệch token** và nhiều chỗ **không hỗ trợ dark mode**. 3 lỗi P1 nằm ở thành phần lõi (bảng dùng chung cho toàn bộ 30+ báo cáo Engine, KPI đầu báo cáo, trang lỗi PDF) nên ưu tiên xử lý trước.

Token cần dùng đã có sẵn trong `frontend/app/globals.css` (light + dark), không phải tạo mới:
- `--accent-primary` = `#01645A` (light) / `#1A9E8C` (dark) — có utility `bg-accent-primary`
- `--accent-primary-hover` = `#014A42` (light) / `#01645A` (dark)
- `--status-done / --status-warning / --status-critical / --status-insurance` (light + dark)
- `--text-kpi` (2rem) + `--text-kpi--line-height`
- `--chart-1..6` (color-blind-safe, light + dark)

---

## R1 — ReportGrid: header/dòng tổng/zebra dùng hex cứng, không dark mode  **[P1]**

**File:** `frontend/components/domain/reports-engine/ReportGrid.tsx` (dòng ~63, 125, 132, 190, 197)
**Ảnh hưởng:** bảng dùng chung cho TẤT CẢ báo cáo Report Engine → sửa 1 chỗ, đúng cho toàn bộ.

**Điểm lệch → sửa:**
- `bg-[#01645A] text-white` (header) → `bg-accent-primary text-primary-foreground` (hoặc `text-white` giữ nguyên nếu contrast đủ ở cả 2 theme — kiểm tra dark).
- `bg-[#014A42]` (dòng TỔNG CỘNG) → `bg-[color:var(--accent-primary-hover)]`.
- `bg-[#F3F8F7]` (zebra) → `bg-muted/40` (theo chuẩn zebra shadcn) — bỏ nền teal nhạt cứng.

**DoD:** không còn hex trong file; bảng đổi màu đúng ở light + dark; header sticky vẫn giữ nền đặc (không lộ nội dung cuộn qua).

---

## R2 — ReportKpiRow: nhận tint hex qua inline style, dùng text-2xl  **[P1]**

**File:** `frontend/components/domain/reports-engine/ReportKpiRow.tsx` (dòng ~17, 22)
**Ảnh hưởng:** KPI đầu mỗi báo cáo Engine — bỏ qua tầng token/theme, không đảm bảo contrast khi BE đổi tint.

**Điểm lệch → sửa:**
- `style={{ backgroundColor: kpi.tint }}` (hex thẳng từ BE) → BE trả **`tint_token`** dạng enum status (`done|warning|critical|insurance|progress|waiting`), FE map sang `var(--status-x)`. Cần phối backend (Thảo) đổi payload catalog.
  - *Bước trung gian nếu chưa kịp đổi BE:* map hex → token ở FE bằng bảng lookup, KHÔNG truyền hex thẳng vào `style`.
- `text-2xl font-bold` → `text-[length:var(--text-kpi)] leading-[var(--text-kpi--line-height)] font-bold` (đồng bộ với `kpi-card.tsx` chuẩn).

**Phụ thuộc:** cần thống nhất với backend về field `tint_token`. Nếu chậm → làm bước trung gian FE-only trước, ghi nợ đổi BE.
**DoD:** KPI Engine dùng thang `--text-kpi`; màu nền qua token status; đúng cả dark mode.

---

## R3 — ErrorCard PDF: card + nút tự chế, không dark mode  **[P1]**

**File:** `frontend/app/(dashboard)/reports/print/[type]/_components/ErrorCard.tsx` (toàn file)

**Điểm lệch → sửa:**
- Thay khung tự chế bằng shadcn `Card`/`CardContent`.
- Nút `<button>` raw (`bg-teal-700 hover:bg-teal-800 focus:ring-teal-500`) → shadcn `Button variant="default"`.
- Icon cảnh báo: `bg-amber-100 text-amber-600` → nền `bg-[color:var(--status-warning)]/10`, icon `text-[color:var(--status-warning)]`.
- Nền trang / card: `bg-gray-100 / bg-white / border-gray-200 / text-gray-900 / text-gray-600` → `bg-muted / bg-background/bg-card / border-border / text-foreground / text-muted-foreground`.

**DoD:** không còn class gray/amber/teal cứng; hiển thị đúng light + dark; dùng component shadcn.

---

## Thứ tự & bàn giao

1. **R1** (nhanh nhất, ảnh hưởng rộng nhất — token đã có, chỉ đổi class).
2. **R3** (độc lập, không phụ thuộc BE).
3. **R2** (cần phối backend về `tint_token`; làm bước FE-only trước nếu BE chậm).

Gợi ý: gộp R1 + R3 vào 1 PR (`fix(report): chuan hoa token DS cho ReportGrid + ErrorCard`), R2 tách PR riêng vì đụng payload backend.

**Kiểm thử bắt buộc trước merge:** mở 1 báo cáo Engine bất kỳ + trang lỗi PDF ở **cả light và dark**, xác nhận không còn nền teal/gray cứng và contrast đạt.

---

## Theo dõi P2/P3 (không thuộc đợt này)

- **P2 (gộp 1 PR sau):** R4 PrintToolbar, R5 ReportPrintClient (`bg-gray-100`→`bg-muted`), R6 ClinicalTab (emerald/red→status), R7 PharmacyTab (amber→status-warning), R8 PharmacyTab chart (`#8b5cf6`→`--chart-4`), R9 ReportSidebar (star amber→status-warning). R10 tự hết sau khi P1+P2 xong.
- **P3:** R11 dọn prop `hideHeader` ở `ReportsPageClient` (cần Nam + Lành xác nhận không còn phụ thuộc route cũ); R12 ~~cập nhật `design-system-standards.md` làm rõ "Card title trong Card = `text-sm font-semibold`"~~ **ĐÃ XONG 2026-07-07** — xem mục 2 (Typography) và mục 6.4 (Bảng màu tint KPI báo cáo → token) trong `design-system-standards.md` v1.1.

## Checklist "đủ chuẩn" cho báo cáo Report mới thêm

- [ ] Không hex/rgb hardcode (kể cả inline style) — mọi màu qua `var(--status-*)` / `var(--chart-*)` / class shadcn.
- [ ] Badge trạng thái dùng `HisStatusBadge`, không tự vẽ.
- [ ] KPI dùng `KpiCard` hoặc `text-[length:var(--text-kpi)]`, KHÔNG `text-2xl/3xl`.
- [ ] Bảng > 20 dòng density `py-2`; header dùng token thống nhất (dùng `ReportGrid`, không copy hex).
- [ ] Tiền/đếm dùng `formatNumberVi`/`formatReportCell`; ngày `formatDateVi` (`dd/MM/yyyy`).
- [ ] Đủ 3 trạng thái Empty/Loading/Error (mẫu `ReportRunner.tsx`).
- [ ] Nút icon-only giữ `min-h-[44px]`/`min-w-[44px]` + `aria-label`.
- [ ] Export PDF: letterhead đủ 6 trường + Mã CSKCB.
- [ ] Chart dùng `var(--chart-1..6)` theo thứ tự.
- [ ] Kiểm tra cả light + dark trước merge.
