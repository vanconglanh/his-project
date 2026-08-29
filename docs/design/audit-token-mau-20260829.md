# Audit token màu hardcode — Pro-Diab HIS (2026-08-29)

> D-3 trong TASKLIST. Đây là **audit độc lập** (không fix chung đợt này theo đúng ghi chú TASKLIST: "P2, quy mô lớn — tách thành audit token màu riêng"). Mục tiêu: liệt kê phạm vi, xếp ưu tiên, khuyến nghị hướng chuyển sang design token / component chuẩn (`HisStatusBadge`, token semantic).

## 1. Phạm vi thực tế

Grep toàn `frontend/components` + `frontend/app` (`.tsx`) cho pattern Tailwind palette cứng
`(bg|text|border|ring|from|to|via)-(red|green|blue|…)-[0-9]{2,3}`:

- **77 file** có ít nhất 1 lần dùng palette cứng (rộng hơn con số 42 ước lượng ban đầu — 42 là khi chỉ tính `bg-*`/`text-*` nhóm trạng thái).
- Tập trung nhiều nhất ở: badge trạng thái, banner cảnh báo lâm sàng, màn in ấn (print/PDF).

### Top file (số lần xuất hiện)
| # lần | File | Nhóm |
|---|---|---|
| 38 | `components/domain/prescriptions/CdssAlertBanner.tsx` | Cảnh báo lâm sàng |
| 33 | `app/(dashboard)/encounters/[id]/cls-print/_components/ClsOrderPrintClient.tsx` | **In ấn (giữ nguyên)** |
| 32 | `components/ui/RoleBadge.tsx` | Badge |
| 28 | `app/(dashboard)/admin/audit/page.tsx` | Badge/severity |
| 27 | `components/domain/DdiWarningPanel.tsx` | Cảnh báo thuốc |
| 25 | `app/(dashboard)/recall/page.tsx` | Trạng thái |
| 21 | `components/domain/FlagBadge.tsx` | Badge |
| 20 | `app/(dashboard)/diabetes/risk-list/page.tsx` | Trạng thái |
| 18 | `components/ui/EmptyState.tsx`, `DeteriorationBanner.tsx`, `AlertBanner.tsx`, `ServicesPageClient.tsx`, `PrescriptionsPageClient.tsx`, `PrescriptionDetailClient.tsx`, `CashierPageClient.tsx` | Badge/banner |
| 16-17 | `TicketCard.tsx`, `DebtsTab.tsx`, `BhytExportStatusBadge.tsx`, `EncounterPrintClient.tsx` | Badge/in ấn |

(Danh sách đầy đủ 77 file: chạy lại lệnh grep ở mục 4.)

## 2. Phân loại & ưu tiên

| Nhóm | Ưu tiên chuyển token | Ghi chú |
|---|---|---|
| **Badge trạng thái** (`RoleBadge`, `FlagBadge`, `BillingStatusBadge`, `BhytExportStatusBadge`, `EncounterStatusBadge`…) | **P2 — cao nhất trong nhóm này** | Nên gom về 1 component chuẩn `HisStatusBadge` với `variant`/`tone` map sang token semantic (`--status-done`, `--status-warning`, `--status-critical`, `--status-info`). Hiện mỗi badge tự map palette cứng → không đồng nhất giữa màn. |
| **Banner cảnh báo lâm sàng** (`CdssAlertBanner`, `DdiWarningPanel`, `AlertBanner`, `DeteriorationBanner`) | **P1** (an toàn thuốc) | Màu cảnh báo cần nhất quán + đủ tương phản. Dùng token `--tint-critical/warning`. Đã đồng bộ cỡ chữ ở D-2; màu nên đi cùng đợt. |
| **Màn in ấn / PDF** (`ClsOrderPrintClient`, `EncounterPrintClient`, `cls-print`, `print`) | **KHÔNG chuyển** (cố ý) | Print CSS cần màu tuyệt đối (đen/trắng/xám cụ thể) để in đúng, không phụ thuộc theme sáng/tối. Giữ palette cứng ở đây là hợp lý. |
| **Trang danh sách/tab nghiệp vụ** (`recall`, `risk-list`, `Cashier`, `Services`, `Prescriptions`) | P2 | Chủ yếu badge inline + màu nhấn; chuyển dần khi refactor từng màn. |

## 3. Khuyến nghị kỹ thuật

1. Định nghĩa bộ **token semantic trạng thái** trong `globals.css` / theme (đã có `ReportTintTokens` phía BE — nên có bản FE tương ứng): `--status-{done,warning,critical,info,neutral,insurance}` cho cả nền + chữ, có biến thể sáng/tối.
2. Tạo/hoàn thiện `HisStatusBadge` nhận `tone` semantic thay vì màu cứng; refactor các badge (RoleBadge/FlagBadge/BillingStatusBadge…) dùng chung.
3. Cảnh báo lâm sàng (P1) chuyển trước vì ảnh hưởng đọc đúng thông tin an toàn.
4. Thêm quy ước lint (eslint rule/regex CI) chặn palette cứng mới trong `components/domain` + `app` (loại trừ thư mục `*print*`).
5. Màn in ấn được **whitelist** — không áp lint token màu.

## 4. Lệnh tái tạo inventory

```bash
# Đếm file
grep -rlE "(bg|text|border|ring|from|to|via)-(red|green|blue|yellow|amber|orange|emerald|teal|cyan|sky|indigo|violet|purple|pink|rose|lime|slate|gray|zinc|neutral|stone)-[0-9]{2,3}" \
  frontend/components frontend/app --include=*.tsx | wc -l

# Xếp theo số lần xuất hiện
for f in $(grep -rlE "..." frontend/components frontend/app --include=*.tsx); do
  echo "$(grep -oE "..." "$f" | wc -l) $f"; done | sort -rn
```

## 5. Kết luận

D-3 là nợ **quy mô lớn nhưng không khẩn cấp** (không phải bug). Đề xuất tách 1 đợt refactor riêng: (1) dựng token semantic + `HisStatusBadge`, (2) chuyển nhóm cảnh báo lâm sàng (P1) trước, (3) badge trạng thái (P2), (4) whitelist màn in ấn. **Không** gộp vào đợt fix bug/đa chi nhánh hiện tại để tránh diff khổng lồ khó review.
