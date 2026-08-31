# PO Review — Chuỗi tính năng OCR (5 luồng)

**Ngày review:** 2026-08-31
**Reviewer:** PO Analyst (đọc code thực tế, không suy đoán)
**Phạm vi:** InBody OCR (J), Lab Result OCR (O), Rad Result OCR (Q), Legacy Import (A), Smart-upload classifier (P)
**Nguồn:** Code trong `backend/src/ProDiabHis.Application/{InBody,LabResults/Ocr,RadResults/Ocr,LegacyImport,Documents}/`, controller tương ứng, và `docs/TASKLIST-20260829.md` mục J/O/P/Q + A.

---

## Tổng quan

5 tính năng OCR được xây dựng nhất quán về kiến trúc (stateless extract → confirm), tái dùng hạ tầng đúng cách, có audit log, có permission guard. Phần lớn nhu cầu nghiệp vụ hàng ngày đã được đáp ứng. Báo cáo này **chỉ liệt kê gap thật** tìm được khi đọc code, kèm đánh giá mức độ. Mục nào đã đủ tốt sẽ được xác nhận rõ ràng.

---

## 1. Vòng đời tài liệu — Xem lại lịch sử và hủy tài liệu nhập nhầm

### Đã đủ

- **InBody:** có `GET /patients/{id}/inbody-reports` trả toàn bộ lịch sử theo bệnh nhân, có `confirmed_by` / `confirmed_at`, audit log `CREATE` và `CONFIRM`. `extracted_fields_json` lưu bản đọc OCR gốc; `diab_his_cli_indicator_reading` lưu `source='inbody_ocr'` + `source_ref_id=reportId` — truy ngược được từ chỉ số về báo cáo InBody gốc.
- **Legacy Import:** có lifecycle đầy đủ (pending → pending_review → confirmed/rejected), có `ConfirmedBy` / `ConfirmedAt`, `MatchMethod`, audit log MATCH/CONFIRM/REJECT, danh sách batch + item phân trang.

### Gap thực tế

#### GAP-1 (P1) — InBody: không có endpoint hủy/xóa mềm báo cáo nhập nhầm

**Bằng chứng code:** Query đều có `deleted_at IS NULL` — soft-delete đã được thiết kế trong schema — nhưng không có handler nào ghi `deleted_at`. Không có endpoint `DELETE /inbody-reports/{id}`.

**Tác động nghiệp vụ:** Nếu y tá upload nhầm file InBody của bệnh nhân khác, hoặc file bị lỗi/corrupt, không có cách xóa bỏ bản ghi. Bản ghi treo mãi ở trạng thái `pending` trong lịch sử bệnh nhân, gây nhầm lẫn.

**Yêu cầu nghiệp vụ y tế:** Phải là soft-delete (đặt `deleted_at`, ghi `deleted_by`), không phải hard-delete. Cần thêm audit log `DELETE` + lý do hủy.

#### GAP-2 (P1) — Lab OCR và Rad OCR: không lưu diff "bản OCR gốc vs bản xác nhận"

**Bằng chứng code:** `LabOcrHandlers.cs` stateless — không ghi gì vào DB ở bước extract. Confirm gọi `CreateLabResultCommand` với giá trị người dùng đã sửa tay; method được set là `"Đọc từ file kết quả (OCR)"` nhưng không có cột nào lưu `ocr_raw_value` bên cạnh `value` đã xác nhận. Tương tự `RadOcrHandlers.cs` không lưu text OCR gốc.

**Tác động nghiệp vụ:** Không thể biết sau khi confirm: OCR đọc được bao nhiêu, người dùng đã sửa tay những gì. Nếu có tranh chấp về kết quả (bệnh nhân khiếu nại giá trị sai), không có bằng chứng để đối chiếu. Đây là điểm yếu về audit y tế.

**So sánh:** InBody đã làm đúng — lưu `extracted_fields_json` (bản gốc OCR) riêng, `ConfirmInBodyReportCommand` nhận `Fields` đã xác nhận — diff rõ ràng.

**Mức độ:** P1 (không phải P0 vì Lab/Rad OCR là tính năng mới, chưa có precedent về audit; nhưng nên sửa trước khi go-live với nhiều bệnh nhân).

---

## 2. Xử lý lỗi OCR — Cơ chế cảnh báo giá trị bất thường trước khi lưu

### Đã đủ

- **Lab OCR (sau khi lưu):** `LabResultFlagCalculator` tính flag NORMAL/H/L/HH/LL/CRITICAL dựa theo khoảng tham chiếu của từng xét nghiệm — hoạt động sau `CreateLabResultCommand`. Lab kết quả được đánh dấu CRITICAL nếu ngoài khoảng 100% → UI có thể hiển thị cảnh báo từ flag này.
- **InBody FE:** Component `InBodyImportPanel.tsx` tô cảnh báo amber cho field `extracted=false` (không đọc được), cảnh báo đỏ khi toàn bộ field thất bại — UX đã có nhắc nhở cơ bản.

### Gap thực tế

#### GAP-3 (P1) — InBody: không có validation range y khoa cho giá trị trước khi confirm

**Bằng chứng code:** `ConfirmInBodyReportCommandHandler` chỉ kiểm tra `if (!f.Value.HasValue) continue` — không validate range. Không có bảng hay constant nào định nghĩa giá trị hợp lý tối thiểu/tối đa cho từng `InBodyIndicatorTypes`.

**Ví dụ tác động:** OCR đọc nhầm "PBF: 80.0%" (thay vì 8.0%) — hệ thống vẫn lưu thành công. Giá trị này vô lý về mặt y khoa (% mỡ cơ thể 80% là không thể sống) nhưng không bị chặn hay cảnh báo. Người dùng xác nhận nhanh mà không đọc kỹ sẽ ghi sai vào hồ sơ.

**Đề xuất:** Thêm soft-range validation (không chặn, chỉ cảnh báo nổi bật "Giá trị nằm ngoài khoảng thông thường — vui lòng kiểm tra lại") ở cả backend (trong response confirm) và FE (highlight ô input màu đỏ trước khi bấm xác nhận).

#### GAP-4 (P2) — Lab OCR: flag CRITICAL/HH chỉ hiển thị SAU KHI confirm, không cảnh báo TẠI bước xác nhận OCR

**Bằng chứng code:** `LabOcrHandlers.cs` không gọi `ILabResultFlagCalculator` ở bước extract hay tại response `LabOcrExtractResponse`. Flag chỉ được tính trong `CreateLabResultCommand` sau khi lưu vào DB.

**Tác động:** Người dùng nhìn vào bảng xác nhận OCR (giá trị + đơn vị) mà không thấy flag ngay — phải vào xem kết quả đã lưu mới thấy CRITICAL. Với quy trình nhanh (nhiều XN cùng lúc), dễ bỏ qua.

**Đề xuất P2:** Tính preview flag (dùng `LabResultFlagCalculator.Calculate` với reference range từ catalog) tại response `LabOcrExtractResponse` để FE hiển thị cột "Mức độ" cạnh giá trị OCR trong bảng xác nhận.

---

## 3. Trùng lặp dữ liệu

### Gap thực tế

#### GAP-5 (P2) — Không có cơ chế phát hiện upload trùng file

**Bằng chứng code:** Không tìm thấy bất kỳ đoạn code nào tính hash/checksum của file (SHA-256/MD5) và so sánh với bản ghi đã tồn tại trong toàn bộ 5 luồng (đã grep toàn bộ `backend/src` với pattern `sha256|md5|checksum|hash` — kết quả không liên quan OCR).

**Tác động thực tế:** Nếu upload lại đúng file InBody đã xử lý (nhầm hoặc do hệ thống mạng gửi lại), sẽ tạo 2 bản ghi `diab_his_cli_inbody_report` y hệt nhau, cả 2 đều ở trạng thái `pending`. Với Legacy Import, ZIP trùng sẽ tạo batch mới và enqueue job OCR lại.

**Đánh giá mức độ:** P2 — xảy ra ít (người dùng thường không upload lại file đã xử lý), và hậu quả không nghiêm trọng (có thể reject/delete bản trùng). Nên làm nhưng không chặn go-live.

**Đề xuất:** Lưu `sha256` của file khi upload InBody/Legacy, check trùng trước khi insert, trả cảnh báo `DUPLICATE_FILE` kèm link bản đã xử lý. Lab/Rad OCR stateless nên không cần.

---

## 4. Phân quyền theo vai trò

### Đã đủ

- **Legacy Import:** `legacy_import.write` — chỉ admin, phù hợp vì đây là tính năng migration 1 lần.
- **Lab OCR:** `lab_result.write` — đúng, khớp với quyền nhập KQ XN thủ công.
- **Rad OCR:** `rad_result.write` — đúng, khớp với quyền nhập KQ CĐHA.
- **Smart Upload:** `patient.clinical.write` — hợp lý vì đây là lớp điều phối, downstream tự kiểm tra quyền.

### Gap thực tế

#### GAP-6 (P1) — InBody: dùng `patient.clinical.write` cho cả upload lẫn confirm, không phân biệt vai trò được phép xác nhận

**Bằng chứng code:** Cả 2 endpoint `POST /inbody-reports` (upload) và `POST /inbody-reports/{id}/confirm` (xác nhận ghi vào VitalSigns) đều dùng `[RequirePermission("patient.clinical.write")]`.

**Vấn đề nghiệp vụ:** Confirm InBody ghi `weight_kg` vào `diab_his_enc_vital_signs` — đây là thao tác ghi sinh hiệu, theo quy trình phòng khám thường chỉ y tá/điều dưỡng/kỹ thuật viên thực hiện (không phải lễ tân). Hiện tại `patient.clinical.write` được cấp cho `bac_si` và `ky_thuat_vien` (theo migration RBAC đã áp), nhưng không có gì ngăn lễ tân có `patient.clinical.write` cũng confirm InBody.

**Đề xuất xem xét:** Cân nhắc tách permission `inbody_report.confirm` (cấp cho `ky_thuat_vien` + `bac_si`) khác với `inbody_report.upload` (có thể cấp thêm cho lễ tân nếu lễ tân được phép scan và upload). Hoặc giữ nguyên nếu BO xác nhận `patient.clinical.write` đủ bảo vệ.

**Quyết định triển khai (đợt sửa gap OCR — backend):** GIỮ NGUYÊN `patient.clinical.write`, KHÔNG tách permission. Căn cứ kiểm tra thực tế migration RBAC: `patient.clinical.write` (tạo ở `9153_rbac_p2_clinical_write_permission.sql`) chỉ được gán cho `bac_si` và `ky_thuat_vien` — lễ tân (`le_tan`) KHÔNG được cấp quyền này ở bất kỳ migration nào (đã grep toàn bộ `db/migrations/*.sql`). Vì vậy lễ tân đã tự nhiên KHÔNG thể confirm InBody (ghi sinh hiệu), mục tiêu bảo mật của việc tách quyền đã đạt được. Tách thêm `inbody_report.confirm` chỉ làm phình RBAC mà không thêm giá trị an toàn. Endpoint `DELETE /inbody-reports/{id}` (GAP-1, soft-delete) dùng CÙNG quyền `patient.clinical.write` để nhất quán với confirm. Nếu sau này lễ tân được cấp `patient.clinical.write` cho việc khác, cần xem lại quyết định này.

---

## 5. Hiệu năng và UX khi OCR chạy lâu

### Đã đủ

- **Legacy Import:** Dùng Hangfire background job (`_jobs.EnqueueLegacyOcrBatch`), UI có thể poll trạng thái batch — đúng thiết kế cho batch lớn.
- **Smart Upload batch (P-7):** Giới hạn 20 file/lần, trả ngay (đồng bộ), hướng dẫn dùng Legacy Import cho batch lớn — đúng.

### Gap thực tế

#### GAP-7 (P1) — InBody/Lab/Rad OCR synchronous: file lớn nhiều trang có thể gây timeout hoặc UX treo

**Bằng chứng code:** `UploadInBodyReportCommandHandler.Handle()`, `ExtractLabResultOcrCommandHandler.Handle()`, `ExtractRadResultOcrCommandHandler.Handle()` đều await trực tiếp OCR trong request pipeline. Không có timeout riêng cho OCR (ngoài `RegexMatchTimeoutException` 250ms per label trong `LabResultOcrParser`).

**Giới hạn file:** InBody 15MB, Lab/Rad 20MB. Một file PDF scan 20MB nhiều trang qua Tesseract (ảnh) có thể mất 30-60 giây. ASP.NET default request timeout thường là 100 giây — đủ để không bị kill bởi server, nhưng browser/client có thể timeout trước.

**Tác động UX:** Người dùng không biết hệ thống đang xử lý hay bị treo (nếu FE không có loading indicator rõ ràng). Nếu request timeout, file đã được upload lên MinIO nhưng record có thể chưa insert → data inconsistency nhỏ (chỉ với InBody vì Lab/Rad stateless).

**Đề xuất P1:** (a) FE: loading state với progress text "Đang đọc nội dung tài liệu..." ngay khi submit, không cho submit lại trong lúc chờ. (b) Backend: đặt timeout rõ ràng cho `CancellationToken` trong OCR call (ví dụ 90 giây), trả lỗi có nghĩa `OCR_TIMEOUT` thay vì generic 500. Không cần chuyển sang async/background nếu 95% file phòng khám thực tế < 5MB.

---

## 6. Liên kết ngược — Xem file gốc từ kết quả đã lưu

### Đã đủ

- **InBody:** `diab_his_cli_inbody_report` lưu `file_id` + `file_url` (object key MinIO). `ListInBodyReportsQuery` trả `signed_url` — người dùng xem lại được PDF gốc từ lịch sử InBody. `diab_his_cli_indicator_reading` có `source='inbody_ocr'` + `source_ref_id` trỏ về report → truy ngược được.
- **Legacy Import:** File lưu vào `fil_files` + `diab_his_fil_cls_uploads`, `image_object_key` sinh `signed_url` trong danh sách item — xem lại được.

### Gap thực tế

#### GAP-8 (P0) — Lab OCR và Rad OCR: kết quả đã confirm KHÔNG lưu file gốc, không thể đối chiếu về sau

**Bằng chứng code:**
- `ExtractLabResultOcrCommandHandler`: stateless, không upload file lên storage, không trả `file_id` hay object key.
- `ConfirmLabResultOcrCommandHandler`: gọi `CreateLabResultCommand(req)` — `req` chứa `LabOrderItemId`, `Value`, `Unit`, `Method`, `PerformedAt`, `Note`. Không có `SourceFileId`.
- `LabResult` entity/DTO: không có cột `source_file_id` hay `source_file_url`.
- Rad OCR: tương tự — `ConfirmRadResultOcrCommand` gọi `CreateRadResultCommand` không có file ref.

**Tác động y tế nghiêm trọng:** Khi có tranh chấp ("kết quả XN HbA1c 8.1% hay 81%?"), bệnh nhân/bác sĩ không thể xem lại phiếu PDF gốc từ đối tác lab để đối chiếu. Đây là yêu cầu cơ bản của hệ thống lưu trữ hồ sơ y tế. Theo TT 13/2023/TT-BYT về hồ sơ bệnh án điện tử, tài liệu nguồn phải được lưu trữ và truy xuất được.

**Mức độ P0:** Ảnh hưởng trực tiếp đến an toàn dữ liệu y tế và khả năng thanh tra/kiểm tra.

**Đề xuất:** Tại bước `ExtractLabResultOcr` (và `ExtractRadResultOcr`), upload file lên MinIO vào bucket `lab-ocr-sources` / `rad-ocr-sources`, lưu object key vào một bảng trung gian hoặc trả `source_file_key` cho FE giữ tạm. Tại bước confirm, truyền `source_file_key` vào `CreateLabResultCommand` → lưu vào cột mới `source_file_id` trong `diab_his_lab_results` / `diab_his_rad_results`. Audit log giữ nguyên.

---

## 7. Tính năng OCR nên làm thêm — Soát phạm vi nghiệp vụ

### Đã phủ (5/5 loại tài liệu chính theo phạm vi phòng khám đa khoa nhỏ)

| Luồng | Điểm trong hành trình | Trạng thái |
|---|---|---|
| InBody OCR | Khám — ghi sinh hiệu/chỉ số theo dõi | Xong |
| Lab Result OCR | CLS — nhập kết quả XN từ đối tác | Xong |
| Rad Result OCR | CLS — nhập kết quả CĐHA | Xong |
| Legacy Import | Hồ sơ bệnh nhân — đính kèm hồ sơ giấy cũ | Xong |
| Smart Upload classifier | Điều phối chung 4 luồng trên | Xong |

### Còn thiếu — xác nhận bằng đọc code

#### GAP-9 (P1) — Đơn thuốc ngoài (giấy) và Giấy chuyển viện: đã ghi nhận trong task nhưng chưa làm

**Xác nhận:** Đọc toàn bộ TASKLIST-20260829.md không thấy mục nào đánh dấu Done cho OCR đơn thuốc ngoài hay giấy chuyển viện. Phần "chưa làm" được ghi nhận trong bối cảnh đặt câu hỏi ("đã loại trừ: đơn thuốc ngoài/giấy chuyển viện đã note trước đó").

**Nghiệp vụ:** Khi bệnh nhân đến tái khám mang theo đơn thuốc từ nơi khác, nhân viên phải gõ tay tên thuốc/liều lượng vào lịch sử dùng thuốc. OCR đơn thuốc ngoài sẽ tăng tốc đáng kể và giảm sai sót nhập liệu — đặc biệt với bệnh nhân đái tháo đường/nội tiết thường dùng nhiều thuốc.

**Đánh giá:** P1 — có giá trị nghiệp vụ thực tế, nên đưa vào backlog sprint tiếp theo.

#### GAP-10 (P2) — Thẻ BHYT giấy: chưa có OCR, chỉ có quét QR CCCD

**Xác nhận:** Module I (QR CCCD) đã done, nhưng thẻ BHYT giấy (loại cũ, không có QR) vẫn phải nhập tay số thẻ. Nhiều bệnh nhân cao tuổi còn dùng thẻ BHYT giấy.

**Đánh giá:** P2 — ít phổ biến hơn, không chặn go-live. Tuy nhiên nếu phòng khám phục vụ nhiều bệnh nhân BHYT cao tuổi thì nên ưu tiên sớm hơn.

---

## Tóm tắt Gap theo mức độ ưu tiên

| ID | Gap | Mức | Hành động đề xuất |
|---|---|---|---|
| GAP-8 | Lab/Rad OCR không lưu file gốc → không đối chiếu được về sau | **P0** | Sửa trước go-live: upload file vào storage tại bước extract, lưu `source_file_id` vào LabResult/RadResult khi confirm |
| GAP-1 | InBody không có endpoint soft-delete (hủy bản ghi nhập nhầm) | **P1** | Thêm `DELETE /inbody-reports/{id}` với soft-delete + audit log + lý do hủy |
| GAP-3 | InBody không validate range y khoa trước khi confirm | **P1** | Thêm soft-range check tại `ConfirmInBodyReportCommand` + cảnh báo FE |
| GAP-6 | InBody confirm dùng quyền rộng `patient.clinical.write`, chưa phân biệt vai trò được confirm | **P1** | Xem xét tách permission hoặc xác nhận BO chấp nhận quyền hiện tại |
| GAP-7 | InBody/Lab/Rad OCR synchronous không có timeout rõ ràng → UX treo với file lớn | **P1** | Thêm loading state FE + timeout `CancellationToken` có nghĩa ở backend |
| GAP-9 | Đơn thuốc ngoài và giấy chuyển viện chưa có OCR | **P1** | Đưa vào backlog sprint tiếp theo |
| GAP-2 | Lab/Rad OCR không lưu diff "OCR gốc vs xác nhận" | **P1** | Lưu `ocr_raw_value` cạnh `value` đã confirm; nếu lưu file gốc (GAP-8) thì một phần đã giải quyết |
| GAP-4 | Flag CRITICAL/HH không hiện tại bước xác nhận OCR Lab | **P2** | Tính preview flag trong `LabOcrExtractResponse` |
| GAP-5 | Không detect file upload trùng | **P2** | Lưu SHA-256, cảnh báo trùng |
| GAP-10 | Thẻ BHYT giấy chưa có OCR | **P2** | Backlog dài hạn |

---

## Kết luận đánh giá tổng thể

5 luồng OCR được triển khai **tốt về kiến trúc** (tái dùng engine, stateless extract, tách parser ra khỏi infrastructure để unit test được, audit log nhất quán). Phần lớn quy trình thực tế của phòng khám đã được phủ.

**GAP-8 là gap nghiêm trọng nhất (P0)** và cần sửa trước khi go-live: Lab OCR và Rad OCR là 2 trong 5 luồng không lưu file nguồn, trực tiếp ảnh hưởng đến khả năng audit y tế khi có tranh chấp hoặc kiểm tra.

**GAP-1, GAP-2, GAP-3** liên quan với nhau (vòng đời + độ tin cậy dữ liệu InBody) — có thể gộp vào 1 sprint sửa.

**GAP-7** (UX timeout) nên làm song song với FE vì không cần migration DB.

---

*Review được thực hiện trên nhánh develop, commit 8425357 (2026-08-29) đến trạng thái hiện tại. Không có tài liệu nào được tạo dựa trên suy đoán — mọi gap đều có dòng code tham chiếu cụ thể.*
