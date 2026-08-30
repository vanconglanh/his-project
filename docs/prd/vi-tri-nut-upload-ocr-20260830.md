# Khuyến nghị vị trí nút Upload OCR thông minh

**Ngày:** 2026-08-30
**Tác giả:** PO Analyst (đọc code thực tế, không giả định)
**Phạm vi:** Feature "Tải tài liệu lên thông minh" — 3 loại: InBody / Kết quả CLS / Hồ sơ cũ

---

## 1. Phân tích luồng thực tế theo vai trò

| Vai trò | Màn hình đang đứng khi cần upload | Loại tài liệu | Entry point hiện có trong code |
|---|---|---|---|
| Điều dưỡng | `/nurse` — Sheet "Nhập sinh hiệu" (tab "Nhập từ máy InBody (PDF)") | InBody PDF | `NursePageClient.tsx` → Sheet → Tab "inbody" → `InBodyImportPanel` |
| Bác sĩ | `/encounters/[id]` — Sheet "Sinh hiệu" (tab "Nhập từ máy InBody (PDF)") | InBody PDF | `EncounterDetailClient.tsx` → Sheet → Tab "inbody" → `InBodyImportPanel` |
| KTV/Điều dưỡng | `/labrad` — Tab "Kết quả xét nghiệm" | Kết quả CLS (PDF/ảnh từ lab ngoài) | `LabResultsTab.tsx` → `LabResultOcrPanel` |
| Admin | `/admin/legacy-import` | Hồ sơ cũ (ZIP ảnh scan) | `LegacyImportPageClient.tsx` |

**Quan sát quan trọng từ code:**

1. **InBody đã có 2 entry point song song:** điều dưỡng dùng `/nurse`, bác sĩ dùng `/encounters/[id]`. Cả hai đều nhúng `InBodyImportPanel` trong cùng 1 Sheet sinh hiệu — đúng ngữ cảnh thao tác.

2. **CLS OCR đã có entry point tại `/labrad`:** `LabResultOcrPanel` nằm trong `LabResultsTab`. KTV không cần rời màn hình để upload.

3. **Legacy import đã có màn riêng** `/admin/legacy-import` — thao tác 1 lần/không thường xuyên, admin tự tìm.

4. **Trang `/patients/[id]`** hiện có các tab: "Kết quả CLS", "Lịch sử InBody", "Tài liệu cũ đã số hoá" — đây là tab **xem lịch sử**, không phải nơi người dùng upload trong quy trình làm việc thật. Điều dưỡng/KTV không ở trang này khi họ cần upload.

---

## 2. Kết luận: Không nên đặt nút chung tại `/patients/[id]`

**Lý do:**

- `/patients/[id]` là màn hình của **lễ tân** (xem/sửa hồ sơ) hoặc bác sĩ (xem lịch sử nhanh trước khi khám). Trong thực tế vận hành, điều dưỡng ít khi vào trang này để upload — họ đang ở `/nurse`, bác sĩ đang ở `/encounters/[id]`.
- Đặt nút upload chính ở đây tạo ra hành trình dài hơn: điều dưỡng phải thoát `/nurse` → tìm bệnh nhân → vào `/patients/[id]` → upload → quay lại `/nurse`. Ngược với luồng thật.
- "Trung tâm dữ liệu bệnh nhân về mặt kỹ thuật" ≠ "nơi nhân viên đang đứng khi cần upload".

---

## 3. Khuyến nghị cuối cùng

### Giữ nguyên 3 entry point đúng ngữ cảnh (đã có sẵn)

| Loại tài liệu | Entry point chính | Lý do |
|---|---|---|
| InBody PDF | `/nurse` (Sheet sinh hiệu → tab "Nhập từ máy InBody") VÀ `/encounters/[id]` (Sheet sinh hiệu → tab tương tự) | Điều dưỡng đang ở `/nurse`, bác sĩ đang ở `/encounters/[id]` — đúng ngữ cảnh |
| Kết quả CLS | `/labrad` → LabResultsTab → LabResultOcrPanel | KTV đang ở màn CLS khi nhận kết quả từ lab ngoài |
| Hồ sơ cũ | `/admin/legacy-import` | Thao tác admin, 1 lần, không thường xuyên |

### Bổ sung 1 lối tắt phụ tại `/patients/[id]` (không phải điểm vào chính)

Chỉ thêm nút "Tải tài liệu lên" tại tab tương ứng trong `/patients/[id]` như một **shortcut bổ sung** cho trường hợp:
- Người dùng đang xem hồ sơ bệnh nhân và muốn upload nhanh mà không rõ loại.
- Hệ thống phân loại tự động (OCR smart classify) rồi route đúng luồng.

**Quan trọng:** Shortcut này không thay thế 3 entry point trên. Agent đang code (`a40dac309e31e2334`) cần giữ các entry point gốc, và CHỈ thêm shortcut tại `/patients/[id]` nếu muốn. Không cần thiết phải có nút chung ở đây nếu làm phức tạp hơn.

---

## 4. Hành động cụ thể cho agent đang code

1. **Không di chuyển** `InBodyImportPanel` khỏi `/nurse` và `/encounters/[id]`.
2. **Không di chuyển** `LabResultOcrPanel` khỏi `/labrad`.
3. Nếu muốn thêm "nút upload thông minh" ở `/patients/[id]`:
   - Đặt tại tab "Kết quả CLS" hoặc "Lịch sử InBody" như một action phụ (secondary button, không phải primary CTA).
   - Hoặc thêm 1 floating action button "Upload tài liệu" nhỏ ở góc phải dưới trang `/patients/[id]` — rõ ràng là lối tắt, không phải điểm vào chính.
4. Smart classify (OCR phân loại tự động) phát huy tác dụng nhất tại lối tắt phụ này, vì ở các entry point gốc người dùng đã biết mình upload loại gì.
