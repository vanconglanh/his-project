# Báo cáo Audit Design System — Pro-Diab HIS (2026-07)

> Rà soát nhất quán toàn hệ thống, đối chiếu với `docs/design/design-system-standards.md`.
> Người thực hiện: Linh (designer, chế độ Auditor) · Ngày: 2026-07-02
> Phạm vi: (a) token & layout, (b) report/in ấn, (c) màn hình nhập liệu, (d) accessibility & responsive.
> **Lưu ý:** báo cáo này CHỈ phát hiện + đề xuất, KHÔNG sửa code. Bàn giao Nam (frontend) / Thảo (backend), Chi (qc) gác cổng.

---

## Tổng hợp mức ưu tiên

| Mức | Số finding | Ý nghĩa |
|-----|-----------|---------|
| **P0** | 1 | Hỏng chức năng / không dùng được (phải sửa ngay) |
| **P1** | 4 | Lệch rõ, ảnh hưởng nhất quán hoặc nghiệp vụ |
| **P2** | 3 | Lệch nhẹ / trùng lặp / nợ kỹ thuật |
| **P3** | 1 | Quy trình / tài liệu |

**Thứ tự xử lý đề xuất:** F1 → F2 → F4 → F3 → F5 → F6 → F7 → F8 → F9.

## Trạng thái xử lý (cập nhật 2026-07-02)

| Finding | Trạng thái | Ghi chú |
|---------|-----------|---------|
| F1 — Token light-mode chết | ✅ Đã sửa | `globals.css`: khối light → `:root` (đặt trước), khối dark → `.dark`; hết `data-theme` |
| F2 — Trùng token chart | ✅ Đã sửa | Xóa `--chart-1..5` OKLCH trùng ở 2 khối shadcn; còn đúng 1 bộ HIS (light+dark) |
| F3 — Letterhead thiếu Mã CSKCB | ✅ Đã sửa | `cskcb_code` vào SELECT + `LetterheadDto.CskcbCode` + `ClinicLetterhead` prop `cskcbCode` + `QuestPdfReportExporter` |
| F4 — Sai tên bảng PrintHandlers | ✅ Đã sửa | `diab_his_ten_tenants`→`sys_tenants`, `usr_users`→`sec_users` (đối chiếu migrations) |
| F5 — Chart hardcode hex | ✅ Đã sửa | 6 file chart chuyển sang `var(--chart-n)` / `var(--status-*)`; thêm token `--scale-hba1c-1..10` |
| F6 — StatusBadge trùng tên | ✅ Đã sửa | `StatusBadge.tsx` → `entity-status-badge.tsx`, tô màu bằng token HIS, import cập nhật đủ |
| F7 — Report endpoint stub | ⏳ Chưa xử lý | P2 — cần hoàn thiện query backend (đợt sau) |
| F8 — Hex ngoài chart (avatar, letterhead) | ⏳ Chưa xử lý | P2 — đợt sau |
| F9 — WORKFLOW.md thiếu designer | ✅ Đã sửa | Đã bổ sung từ đợt trước |

> Nghiệm thu (2026-07-02): fix F1–F6 xác minh bằng grep/đọc code trực tiếp. Build backend `dotnet build`: **PASS** (0 error, 4 warning có sẵn). Build frontend `npm run build` (Next.js 16): **PASS** (đủ route manifest, gồm cả các trang in `/reports/print/[type]`, `/encounters/[id]/print`, `/encounters/[id]/cls-print`).

---

## Chi tiết finding

### P0

**F1 — Token light-mode "chết" (cả hệ HIS token vô hiệu ở light mode)**
| Khía cạnh | Token & màu · Light/Dark |
|-----------|--------------------------|
| File | `frontend/app/globals.css:146` · `frontend/app/layout.tsx:56` |
| Điểm lệch | HIS light tokens gắn selector `:root[data-theme="light"]`, nhưng `next-themes` cấu hình `attribute="class"` (chỉ toggle `.dark`). Đã grep toàn frontend: **`data-theme` không hề được set ở đâu** → ở light mode các biến `--bg-base`, `--bg-surface`, `--status-*`, `--text-*`, `--accent-primary`, `--chart-1..6` **không kích hoạt (undefined)**. Component dùng `var(--status-x)`, `var(--bg-surface)`… sẽ mất màu hoặc rơi về giá trị kế thừa từ `.dark`. |
| Chuẩn đúng | Mục 1: token phải định nghĩa trên `:root` (light) và `.dark` (dark), **không** dùng `[data-theme]`. |
| Đề xuất sửa | Đổi selector khối light (globals.css:146) từ `:root[data-theme="light"]` thành `:root`; khối dark giữ `.dark` (bỏ `:root[data-theme="dark"]` ở dòng 107). |

### P1

**F2 — Xung đột / trùng định nghĩa token chart (2 hệ token)**
| Khía cạnh | Token & màu |
|-----------|-------------|
| File | `frontend/app/globals.css:137-142` (HIS hex) vs `:205-209` (`:root` OKLCH) và `:240-244` (`.dark` OKLCH) |
| Điểm lệch | `--chart-1..5` được định nghĩa **2 lần với giá trị khác nhau**. Ở dark mode, khối shadcn `.dark` (dòng 240) đứng sau khối HIS (dòng 137) → **shadcn OKLCH ghi đè HIS hex**. Palette color-blind-safe của HIS bị vô hiệu; chart hiển thị màu không như spec. |
| Chuẩn đúng | Mục 1.2: một nguồn chân lý cho `--chart-*` (bảng HIS), `--primary` shadcn phải khớp `--accent-primary`. |
| Đề xuất sửa | Bỏ định nghĩa `--chart-1..5` trùng ở khối shadcn, giữ 1 bộ theo bảng chuẩn; hoặc map `--chart-n` shadcn = `var(--chart-n)` HIS. |

**F3 — Letterhead report thiếu Mã CSKCB (cả 3 tầng)**
| Khía cạnh | Report / In ấn |
|-----------|----------------|
| File | `backend/.../Tenants/GetLetterheadQuery.cs:29` · `frontend/components/print/ClinicLetterhead.tsx:3-11` |
| Điểm lệch | Query SELECT letterhead **không lấy `cskcb_code`**; `LetterheadDto` không có trường này; `ClinicLetterhead` không render. Report/phiếu in **thiếu Mã CSKCB** — trường bắt buộc với hồ sơ y tế / đối soát BHYT. |
| Chuẩn đúng | Mục 6.1: letterhead bắt buộc có **Mã CSKCB** (`cskcb_code`). |
| Đề xuất sửa | Thêm `cskcb_code` vào SELECT + `LetterheadDto` + prop `cskcbCode` của `ClinicLetterhead`, in dưới tên phòng khám. Đồng bộ cả `QuestPdfReportExporter.cs`. |

**F4 — Sai tên bảng khi in biên lai/hóa đơn (rủi ro lỗi runtime)**
| Khía cạnh | Report / Backend |
|-----------|------------------|
| File | `backend/.../Billing/PrintHandlers.cs:253` (`diab_his_ten_tenants`), `:266` (`diab_his_usr_users`) |
| Điểm lệch | Hai bảng này **không khớp** tên thật trong migrations: tenant là `diab_his_sys_tenants` (xác nhận qua `GetLetterheadQuery.cs:30`), user là `diab_his_sec_users`. Nếu bảng `ten_tenants`/`usr_users` không tồn tại → **lỗi runtime khi in biên lai/hóa đơn**. |
| Chuẩn đúng | Tên bảng theo migrations `00xx` (`sys_tenants`, `sec_users`). |
| Đề xuất sửa | Đổi `diab_his_ten_tenants` → `diab_his_sys_tenants`, `diab_his_usr_users` → `diab_his_sec_users` trong `PrintHandlers.cs`. **Kiểm chứng bằng test in biên lai thực tế.** |

**F5 — Chart hardcode màu hex, không dùng token**
| Khía cạnh | Token & màu / Chart |
|-----------|---------------------|
| File | `frontend/components/domain/charts/Hba1cDistributionChart.tsx:15-18` (+ 7 file chart khác, 31 hex tổng cộng: `LabResultTrendChart.tsx`, `DiabetesTrendChart.tsx`, `BhytAmountChart.tsx`, `ComplicationsRateChart.tsx`, `LabIntegrationDashboard.tsx`) |
| Điểm lệch | Mảng `COLORS = ["#22c55e", …]` hardcode trực tiếp trong component → không đổi theo light/dark, không đảm bảo color-blind-safe, lệch palette `--chart-*`. |
| Chuẩn đúng | Mục 1.2: chart đọc màu qua `var(--chart-n)`, không hardcode. |
| Đề xuất sửa | Thay mảng hex bằng đọc `--chart-1..6` (getComputedStyle/CSS var). Trường hợp thang sequential theo mức HbA1c cần palette riêng → định nghĩa token `--scale-hba1c-*` trong globals.css thay vì hardcode. |

### P2

**F6 — Component StatusBadge trùng tên & không nhất quán cách tô màu**
| Khía cạnh | Component |
|-----------|-----------|
| File | `frontend/components/ui/StatusBadge.tsx` vs `frontend/components/ui/status-badge.tsx` |
| Điểm lệch | Hai file **khác nhau chỉ ở hoa/thường** (nguy cơ đụng file trên filesystem không phân biệt hoa-thường như Windows/macOS). `status-badge.tsx` (`HisStatusBadge`) đúng chuẩn — dùng token `var(--status-*)` + icon + `aria-label`. `StatusBadge.tsx` dùng **Tailwind palette cứng** (`bg-green-100 text-green-800`…) cho status tenant/user → không theo token. |
| Chuẩn đúng | Mục 4: chỉ một component badge trạng thái, dùng token, có icon + aria-label. |
| Đề xuất sửa | Đổi tên file để hết đụng hoa-thường; chuyển `StatusBadge` (tenant/user) sang dùng token HIS hoặc map về `HisStatusBadge`. Gộp về một API thống nhất. |

**F7 — Nhiều report endpoint là stub trả mảng rỗng**
| Khía cạnh | Report |
|-----------|--------|
| File | `backend/.../Api/Controllers/ReportsController.cs` |
| Điểm lệch | Các endpoint `by-service`, `by-payment-method`, `cashier/daily-summary`, `debts/aging`, `bhyt/summary`, `clinical/visits`, `clinical/icd10`, `pharmacy/inventory-value` trả `[]` → màn báo cáo tương ứng rỗng dù UI đã dựng. |
| Chuẩn đúng | Report phải có dữ liệu thật hoặc empty state rõ ràng (mục 5), không "giả rỗng". |
| Đề xuất sửa | Hoàn thiện query từng endpoint; trong lúc chờ, hiển thị empty state "Đang phát triển" thay vì bảng trống gây hiểu nhầm. |

**F8 — Hex hardcode rải rác ngoài chart (`SimpleAvatar`, letterhead)**
| Khía cạnh | Token & màu |
|-----------|-------------|
| File | `frontend/components/domain/SimpleAvatar.tsx` (10 hex — palette avatar) · `frontend/components/print/ClinicLetterhead.tsx:34` (`#0F766E` inline style) |
| Điểm lệch | Màu hardcode thay vì token. Letterhead teal `#0F766E` nên là token in (`--print-header`) để đổi 1 chỗ. |
| Chuẩn đúng | Mục 1.2: không hardcode màu. |
| Đề xuất sửa | Đưa palette avatar + màu header in vào token trong globals.css; component tham chiếu token. |

### P3

**F9 — WORKFLOW.md chưa liệt kê role `designer`**
| Khía cạnh | Quy trình / tài liệu |
|-----------|----------------------|
| File | `WORKFLOW.md` (bảng Agent roles) |
| Điểm lệch | Bảng agent chính không có `designer` (Linh) dù vai trò đã định nghĩa trong `.claude/agents/designer.md` và đứng giữa po-analyst ↔ frontend. |
| Chuẩn đúng | Sơ đồ workflow phản ánh đủ các role đang hoạt động. |
| Đề xuất sửa | Bổ sung dòng `designer (Linh)` vào bảng + chèn bước design/audit vào luồng feature. *(Đã xử lý trong đợt này.)* |

---

## Đợt 2 — Audit layout sâu (2026-07-02, cùng ngày)

Sau khi F1–F6 hoàn tất, đã chạy vòng audit sâu phần **layout** (khía cạnh a + c). Kết quả tách thành 2 báo cáo chi tiết:
- `layout-audit-shell-2026-07.md` — khung/panel/dialog/typography: **14 finding** (L-01→L-14), đã sửa 13, còn L-06 (refactor).
- `layout-audit-forms-2026-07.md` — form nhập liệu: **9 finding** (F-01→F-09), đã sửa 6, còn F-01/F-03 (refactor) + F-09 (nợ kỹ thuật).

Batch sửa: 65 file frontend, build Next.js PASS. Điểm nổi bật đã chuẩn hóa: topbar 56px, sidebar collapsed 64px, 100% Sheet có `px-6 pb-6`, Dialog về đúng thang md/xl/4xl, 36 page title về `text-xl font-bold`, Card/Dialog về `rounded-lg`, KPI dùng token `--text-kpi`, bảng dài về density `py-2`, Ctrl+S/Esc hoạt động thật ở 2 form Fullpage, DrugForm/TenantForm chuyển Dialog→Sheet.

**Tồn đọng chuyển đợt sau (cần Đăng/Lành duyệt vì đổi luồng nghiệp vụ):** L-06/F-01 (gộp luồng tạo bệnh nhân ở Tiếp đón về `/patients/new`, xóa `PatientForm.tsx`), F-03 (PO/GRN/Adjustment → Fullpage route), F-09 (phổ cập `StickyActionBar`/`FieldGroup`, dựng `FullPageFormShell`), F7/F8 đợt 1 (report endpoint stub, hex avatar/letterhead), PageHeader component chống lệch title về sau.

---

## Ghi chú phương pháp
- Grep hex: `#[0-9A-Fa-f]{6}` trong `frontend/components/**/*.tsx` → 31 occurrence / 8 file.
- Xác nhận theme: `next-themes attribute="class"` (`layout.tsx:56`); `data-theme` không set ở đâu (grep toàn frontend, 0 kết quả).
- Đối chiếu tên bảng: `PrintHandlers.cs` vs `GetLetterheadQuery.cs` vs migrations `db/migrations/00xx`.
- **Chưa chạy**: đo contrast từng màn light/dark bằng công cụ; kiểm tab-order thủ công từng form; đối chiếu từng form với `input-form-layout-spec.md`. → Đề xuất vòng audit sâu tiếp theo cho khía cạnh (c) form và (d) a11y.
