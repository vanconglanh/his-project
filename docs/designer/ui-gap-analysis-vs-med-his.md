# Gap Analysis UI/UX — Pro-Diab HIS vs "MedArmor" (HIS tham chiếu)

> Tác giả: Linh (designer) · Ngày: 2026-08-18
> Nguồn ảnh: `D:\_Project\diaB\his\med\` (21 ảnh chụp `his.uat.medarmor.vn`, "MedArmor Việt Nam — AI-powered HIS by DXP")
> Đối chiếu: `frontend/app/(dashboard)/**`, `frontend/components/domain/**`, `docs/design/design-system-standards.md`
> Phạm vi: chỉ soi UI/UX layout, pattern tương tác — KHÔNG đánh giá nghiệp vụ/API/tích hợp.

---

## 1. Inventory 21 màn hình tham chiếu

| # | File ảnh | Màn hình | Điểm mạnh UI đáng ghi nhận |
|---|----------|----------|---------------------------|
| 1 | 13-33-37 | Camera AI nhận diện lễ tân | Banner vàng "đang xem với tư cách X (role badge)" — impersonate rõ ràng; nút "Quay lại Admin" luôn nổi góc phải |
| 2 | 13-34-24 | Tiếp nhận — lịch hẹn trong ngày | Toolbar date-picker + toggle "Danh sách/Lịch"; empty state 1 dòng gọn |
| 3 | 13-46-01 | Khám bệnh — màn hình khám 1 màn (Cận lâm sàng tab, order XN) | **1 màn duy nhất**: trái = hồ sơ+sinh hiệu+lịch sử, phải = tabs nghiệp vụ (Bệnh án/Tiền sử/Cận lâm sàng/Kết quả CLS/Chẩn đoán/Đơn thuốc/Tái khám/Tập tin/Ghi âm); autocomplete chỉ định XN ngay trong tab, không cần rời trang |
| 4 | 13-46-21 | Khám bệnh — 2 đợt chỉ định CLS xếp chồng | Badge trạng thái "Chưa thanh toán" theo từng đợt chỉ định, thao tác "Xem/In" theo dòng |
| 5 | 13-47-10 | Nhập kết quả CLS (danh sách phiếu) | 2 cột: trái = danh sách phiếu theo ngày (filter Xét nghiệm/CĐHA/Nội soi), phải = chi tiết dịch vụ + trạng thái "Chưa hoàn thành" + nút "Nhập kết quả" theo dòng — thao tác hàng loạt nhanh |
| 6 | 13-47-41 | Viện phí — danh sách hoá đơn | List trái (mã hoá đơn + badge "Chưa thu/Đã thu/Đã huỷ" theo màu) + panel phải "Chọn hoá đơn để xem chi tiết" (master-detail, giảm click) |
| 7 | 13-50-31 | Xem file đính kèm kết quả XN (PDF viewer inline) | Modal xem PDF preview ngay trong luồng khám, không phải tải về mới xem |
| 8 | 13-52-32 | Đơn thuốc in (PDF) | Letterhead gọn: logo + tên PK + SĐT + email + web, **có barcode + mã đơn**, bảng liều dùng dạng biểu tượng Sáng/Trưa/Chiều/Tối |
| 9 | 13-56-12 | Lịch nhắc hẹn (calendar tháng) | Calendar tháng dạng lưới, click ngày → panel phải hiện chi tiết nhắc hẹn ngày đó, filter loại nhắc (Tái khám/Lấy mẫu tại nhà) |
| 10 | 13-57-04 | Khám bệnh — tab "Ghi âm" | Tính năng ghi âm cuộc khám (AI), banner "Ca khám đang được ghi âm" luôn hiện trên toolbar khám |
| 11 | 13-59-21 | Chi tiết phiếu xét nghiệm | Bảng kết quả có cột "Khoảng tham chiếu" + cờ **Flag "BT"** (bất thường) ngay cạnh giá trị, nút "Scan phiếu XN/Import CSV" |
| 12 | 14-00-22 | Nhập kết quả CLS — đã hoàn thành, có link Medlink/file đính kèm | Trạng thái "Hoàn thành" đổi màu xanh; đính kèm hiện icon nguồn (link ngoài / PDF) ngay trong dòng |
| 13 | 14-00-33 | Khám bệnh — Queue (danh sách chờ khám hôm nay) | 3 KPI ngang đầu trang (Chờ khám/Đang khám/Hoàn thành) bằng số lớn — tự "cập nhật mỗi 30 giây" (hiện text ở footer) |
| 14 | 14-01-22 | Khám bệnh — hoàn thành, khoá sửa + tab Ghi âm (player) | Banner cam "Ca khám đã hoàn thành — chỉ xem, không thể chỉnh sửa"; audio player mini có trạng thái lỗi ghi âm (đỏ) rõ ràng |
| 15 | 14-10-10 | Điều phối khám (đổi bác sĩ/phòng) | Bảng cho phép **đổi bác sĩ/phòng khám bằng dropdown ngay trong ô** (inline edit) — không cần mở form riêng |
| 16 | 14-10-20 | Khám bệnh — queue, nút "Khám" | Nút hành động theo trạng thái đổi từ "Xem" → "Khám" tuỳ trạng thái dòng |
| 17 | 14-10-29 | Khám bệnh — dropdown chuyển phòng khám nhanh | Dropdown "Chuyển phòng" ngay trên toolbar khám (không rời màn) |
| 18 | 14-10-59 | Khám bệnh — queue lễ tân xem (đổi bác sĩ hiển thị) | Queue đồng bộ 2 role (lễ tân/bác sĩ) cùng 1 layout |
| 19 | 14-16-47 | Kê đơn — tab Đơn thuốc trong màn khám | Bảng thuốc: cột Sáng/Trưa/Chiều/Thời gian(trước/sau ăn)/SL/ĐV trên 1 dòng, input trực tiếp trong bảng, "+ Thêm dòng mới" cuối bảng, toggle "Thuốc có trong kho / Thuốc ngoài (bán, tham chiếu)" |
| 20 | 14-18-31 | Dialog "Có thay đổi chưa được lưu" | Dialog xác nhận 3 lựa chọn rõ ràng: Lưu & tiếp tục / Bỏ qua & rời trang / Ở lại trang — tránh mất dữ liệu khi rời tab |
| 21 | 14-30-45 | Khám bệnh — chỉ định CLS với cột "In"/"Ẩn giá" | Checkbox "In" và "Ẩn giá" theo từng dòng dịch vụ chỉ định — kiểm soát in phiếu linh hoạt |

**Pattern tổng thể quan trọng nhất**: toàn bộ luồng khám bệnh (từ sinh hiệu → tiền sử → cận lâm sàng → kết quả CLS → chẩn đoán → đơn thuốc → tái khám → tập tin → ghi âm) diễn ra trên **1 trang duy nhất dạng tab ngang**, không điều hướng route riêng cho từng bước. Sidebar trái của trang khám luôn giữ cố định: ảnh đại diện + tên BN + tuổi/giới + thông tin hành chính + lịch sử khám (timeline) + tiền sử bệnh + ghi chú nội bộ.

---

## 2. Bảng GAP — họ có, mình chưa có

| # | Gap | Mức | Mô tả | Đề xuất |
|---|-----|-----|-------|---------|
| G1 | Màn khám bệnh dạng 1-trang-nhiều-tab | **P0** | Pro-Diab hiện `encounters/[id]/page.tsx` — cần xác nhận layout thực tế đã tab hoá đủ (Bệnh án/Tiền sử/CLS/Kết quả CLS/Chẩn đoán/Đơn thuốc/Tái khám/Tập tin) hay còn rải rác route riêng (`labrad/results`, `prescriptions/new` là route tách). Nếu bác sĩ phải rời trang khám để kê đơn/chỉ định CLS → tăng click, mất context sinh hiệu đang xem. | Audit lại `EncounterDetailClient`: đưa toàn bộ thao tác chỉ định CLS + kê đơn + xem kết quả vào **trong cùng 1 trang** dạng Tabs, sidebar trái sticky hồ sơ+sinh hiệu+lịch sử không đổi khi chuyển tab |
| G2 | Banner trạng thái ca khám (đang ghi/đã khoá) | P1 | MedArmor có banner nổi bật trên cùng toolbar khám: "Ca khám đang được ghi âm" / "Ca khám đã hoàn thành — chỉ xem, không thể chỉnh sửa". Pro-Diab cần xác nhận có banner khoá sửa rõ ràng khi encounter đã DONE chưa. | Thêm `EncounterAlertBanner` (đã có component tương tự trong `components/domain/`) hiển thị trạng thái khoá/mở form rõ ràng ở đầu trang khám |
| G3 | Master-detail cho danh sách hoá đơn/kết quả CLS | P1 | Viện phí và Nhập kết quả CLS ở MedArmor dùng layout 2 cột cố định (list trái, chi tiết phải) thay vì điều hướng route/mở Dialog riêng — giảm 1 click, giữ ngữ cảnh danh sách khi xem nhiều bản ghi liên tiếp. | Với `billings/page.tsx`, `labrad/results/page.tsx`: cân nhắc pattern 2 cột (list 30% – detail panel 70%) thay cho điều hướng sang `[id]/page.tsx` (đặc biệt khi thao tác lặp nhiều bản ghi/ca trực) |
| G4 | Inline-edit dropdown trong bảng (điều phối khám) | P2 | Đổi bác sĩ/phòng khám ngay trong ô bảng bằng `<Select>` thay vì mở Dialog riêng. | Nếu có màn "Điều phối khám" tương tự, áp dụng Select inline trong ô thay vì Dialog để giảm thao tác cho lễ tân |
| G5 | Dialog xác nhận rời trang khi có thay đổi chưa lưu (3 lựa chọn) | **P0** | Đây là pattern an toàn dữ liệu quan trọng cho form dài (khám bệnh, kê đơn). MedArmor cho 3 lựa chọn rõ: Lưu & tiếp tục / Bỏ qua & rời trang / Ở lại trang. | Cần kiểm tra Pro-Diab đã có `beforeunload`/route-guard cho encounter form dài chưa. Nếu chưa: thêm `ConfirmDialog` biến thể 3-action dùng `ConfirmDialog.tsx` sẵn có, gắn vào form khám/kê đơn dài (đã có "Auto-save draft" trong chuẩn design nhưng cần dialog xác nhận khi điều hướng bằng router) |
| G6 | Flag bất thường (BT) cạnh giá trị XN | P1 | Bảng kết quả XN của MedArmor gắn flag "BT" (bất thường) ngay cạnh giá trị đo, không chỉ dựa vào khoảng tham chiếu. Pro-Diab đã có `FlagBadge.tsx` — cần xác nhận có hiển thị đúng vị trí/mức nổi bật này trong `LabResultTable.tsx`. | Rà `LabResultTable.tsx`: đảm bảo cột "Khoảng tham chiếu" luôn hiện cạnh giá trị + FlagBadge dùng token `--status-warning`/`--status-critical` (không hex cứng) |
| G7 | Toggle "Thuốc có trong kho / Thuốc ngoài (bán, tham chiếu)" trong bảng kê đơn | P1 | Cho phép bác sĩ kê cả thuốc ngoài kho (không trừ tồn, chỉ tham chiếu) ngay trong cùng bảng đơn thuốc, tránh phải tách 2 form. | Kiểm tra `PrescriptionItemTable.tsx`/`PrescriptionItemForm.tsx` đã hỗ trợ phân loại "trong kho / ngoài kho" trong 1 bảng chưa; nếu chưa, đây là gợi ý UX bổ sung (làm việc với po-analyst xác nhận nghiệp vụ trước khi thêm) |
| G8 | Cột "In" / "Ẩn giá" theo dòng dịch vụ chỉ định CLS | P2 | Kiểm soát in ấn/hiện giá theo từng dòng dịch vụ ngay trong bảng chỉ định, hữu ích khi in phiếu cho BN không cần thấy giá dịch vụ gói khám. | Gợi ý bổ sung 2 checkbox cột cuối bảng `LabOrderForm`/`RadOrderForm` nếu nghiệp vụ có yêu cầu "ẩn giá gói khám" |
| G9 | KPI-strip 3 số lớn đầu trang Queue khám bệnh + auto-refresh 30s có ghi chú | P2 | Queue khám của MedArmor hiện rõ "Chờ khám / Đang khám / Hoàn thành" bằng số lớn + text nhỏ "Tự động cập nhật mỗi 30 giây" ở góc — tăng niềm tin dữ liệu real-time. | Với queue tương tự ở Pro-Diab (reception, nurse, encounters), thêm dòng chữ nhỏ ghi rõ tần suất auto-refresh (đã có polling ở nhiều nơi — chỉ thiếu label minh bạch) |
| G10 | Impersonate banner rõ ràng khi admin xem-như-role-khác | P3 | Banner vàng cố định "Đang xem với tư cách X [role badge]" + nút "Quay lại Admin" luôn nổi — UX debug/support tốt. | Nếu Pro-Diab có tính năng admin impersonate user, tham khảo pattern banner sticky này (không phải P0, chỉ ghi nhận nếu tính năng tồn tại) |

---

## 3. Cái Pro-Diab đang làm TỐT HƠN — giữ nguyên, đừng bắt chước ngược

1. **Design token hoá triệt để** — Pro-Diab có `design-system-standards.md` là nguồn chân lý duy nhất, 1 hệ token cho cả light/dark, cấm hardcode hex. MedArmor dùng inline theme cam/be khá "marketing" (topbar cam nhạt, không rõ có dark mode) — không phù hợp môi trường ánh sáng mạnh/ca đêm mà CLAUDE.md yêu cầu.
2. **HisStatusBadge chuẩn hoá 6 variant + icon + aria-label** — MedArmor chỉ dùng text màu (`Chưa thu`, `Đã thu`) không thấy icon kèm theo → có rủi ro vi phạm WCAG 1.4.1 (truyền tin chỉ bằng màu). Pro-Diab đã chủ động khắc phục lỗi này ngay từ chuẩn thiết kế — giữ nguyên.
3. **Letterhead bắt buộc đủ 6 trường gồm mã CSKCB** — đơn thuốc in của MedArmor (ảnh 13-52-32) KHÔNG thấy mã CSKCB trên letterhead, chỉ có logo/tên/SĐT/email/web. Đây là rủi ro tuân thủ TT 27/2021/TT-BYT khi giám định BHYT. Pro-Diab đã bắt buộc mục 6.1 trong design-system-standards — không hạ chuẩn theo MedArmor.
4. **Phím tắt chuẩn hoá toàn hệ thống (F2/F3/F4/F8/F9)** — MedArmor không thấy gợi ý phím tắt trên UI (không có `<kbd>` hiển thị). Pro-Diab đã có `ShortcutsModal` + hiện kbd ngay trên nút (vd trang reception F2) — tốt hơn, giữ nguyên và mở rộng sang các trang khác.
5. **Sticky action bar cho form dài + auto-save draft** — là chuẩn thiết kế đã có sẵn trong `input-form-layout-spec.md`, MedArmor không thấy rõ auto-save (chỉ có dialog cảnh báo rời trang bị động).
6. **Density quy tắc rõ (`py-2` dense/`py-3` comfortable)** — MedArmor có vẻ dùng padding tuỳ màn không nhất quán (queue rows khá thoáng trong khi bảng XN lại dày) — Pro-Diab có luật rõ ràng hơn cho lễ tân xử lý bảng lớn.

---

## 4. Đề xuất cải thiện cụ thể

### Layout / 1-màn-khám-bệnh cho bác sĩ (ưu tiên P0)
- Xác nhận `frontend/app/(dashboard)/encounters/[id]/_components/EncounterDetailClient.tsx` đã gom đủ luồng (sinh hiệu, tiền sử, chỉ định CLS, kết quả CLS, chẩn đoán ICD-10, đơn thuốc, tái khám, tập tin đính kèm) vào Tabs trong 1 trang, tránh bác sĩ phải nhảy sang `/labrad/results`, `/prescriptions/new` làm mất context sinh hiệu bệnh nhân đang xem.
- Sidebar trái trong trang khám (ảnh đại diện, tuổi/giới, timeline lịch sử khám, tiền sử, ghi chú nội bộ) nên **sticky khi cuộn** và **không đổi** khi chuyển tab phải.

### Density
- Bảng danh sách chỉ định CLS/đơn thuốc trong màn khám nên cho input trực tiếp trong ô (số lượng, sáng/trưa/chiều) — đúng chuẩn `py-2` dense đã có, chỉ cần đảm bảo `PrescriptionItemTable` hỗ trợ edit inline thay vì mở Dialog cho từng dòng thuốc.

### Phím tắt / workflow bác sĩ
- Trang khám nên có phím tắt: `Ctrl+S`/`F8` lưu hồ sơ, `F9` in đơn, và cho phép Enter di chuyển nhanh giữa các trường sinh hiệu (đã có trong chuẩn 7 của design-system-standards — chỉ cần audit đã áp dụng đủ ở encounter form chưa).

### Trạng thái / badge
- Đảm bảo `LabResultTable.tsx` hiển thị **Flag bất thường** (BT/cao/thấp) ngay cạnh giá trị kết quả bằng `FlagBadge` dùng token `--status-warning`/`--status-critical`, không chỉ dựa vào cột khoảng tham chiếu rời rạc.
- Banner khoá sửa khi encounter đã DONE (dùng `EncounterAlertBanner` sẵn có) đặt ngay dưới toolbar khám, màu `--status-warning` nền nhạt + icon khoá.

### Bảng danh sách & form kê đơn
- Với danh sách viện phí/kết quả CLS dùng nhiều trong ca trực (lễ tân/bác sĩ mở đi mở lại nhiều bản ghi liên tiếp), cân nhắc pattern **list-detail 2 cột trong cùng 1 trang** thay vì điều hướng `[id]/page.tsx` riêng — giảm round-trip điều hướng, giữ vị trí cuộn danh sách.
- Form kê đơn nên rõ ràng phân biệt "thuốc trong kho (trừ tồn)" và "thuốc ngoài kho (tham chiếu, không trừ tồn)" ngay trong 1 bảng bằng toggle/badge nhỏ đầu dòng — làm rõ nghiệp vụ trước khi triển khai (cần po-analyst xác nhận có tồn tại ca dùng này không).

### Dialog xác nhận rời trang chưa lưu (P0, an toàn dữ liệu)
- Bổ sung route-guard/`beforeunload` cho form khám bệnh + kê đơn dài, dùng lại `ConfirmDialog.tsx` với 3 action: "Lưu & tiếp tục" (primary) / "Bỏ qua & rời trang" (outline) / "Ở lại trang" (ghost) — đúng token/Button variant trong chuẩn.

### Design token / component shadcn cần bổ sung
- Không cần thêm token màu mới — MedArmor không có gì vượt khỏi 6-variant status hiện có của Pro-Diab.
- Cân nhắc bổ sung **component chuẩn** (không phải token):
  - `EncounterLockBanner` (biến thể của `EncounterAlertBanner`) — banner trạng thái khoá form khi encounter DONE, tái dùng cho mọi form gắn theo encounter.
  - `ListDetailLayout` (helper layout 2 cột 30/70, sticky trái, scroll riêng phải) — dùng chung cho billings, labrad/results, prescriptions nếu quyết định áp dụng G3.
  - `UnsavedChangesDialog` — wrapper của `ConfirmDialog` với 3-action cố định cho mọi form Fullpage dài, tránh mỗi trang tự viết logic riêng.

---

## 5. Không áp dụng / không khuyến nghị theo MedArmor

- Không nên bắt chước theme cam/be sáng làm mặc định — trái với yêu cầu dark-mode-default cho ca trực của Pro-Diab HIS.
- Không nên bỏ mã CSKCB khỏi letterhead in — đây là gap tiêu cực bên MedArmor, Pro-Diab cần giữ chuẩn nghiêm hơn.
- Không cần thêm tính năng "Camera AI nhận diện" hay "Ghi âm cuộc khám AI" vào phạm vi UI audit này — đây là tính năng nghiệp vụ/AI ngoài phạm vi thiết kế thuần UI, cần PRD riêng từ po-analyst nếu muốn xem xét.
