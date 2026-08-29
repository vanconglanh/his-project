# Đối chiếu ma trận phân quyền Pro-Diab HIS với chuẩn/quy định hiện hành

- Ngày: 2026-08-29
- Người thực hiện: Đăng (PO/BA)
- Phạm vi: 5 role non-admin sau migration `db/migrations/9139_reconcile_role_permissions.sql`
- Trạng thái: **Báo cáo phân tích + đề xuất**, chưa áp dụng thay đổi nào vào DB/code

---

## 0. Trả lời thẳng câu hỏi của user

**Migration 9139 KHÔNG được xây theo một chuẩn/thông tư nào.** Nó là kết quả của một bài toán kỹ thuật:
đối chiếu 167 mã `[RequirePermission(...)]` thực tế trong controller với mapping role→permission trong DB,
rồi cấp lại đúng bộ mã mà từng role cần để màn hình của họ không bị 403. Đây là **sửa lỗi vận hành**,
không phải **thiết kế tuân thủ**.

Sau khi rà soát các quy định hiện hành, kết luận là: ma trận 9139 **đúng về mặt chức năng nhưng chưa đủ chuẩn
về mặt kiểm soát** — thiếu 1 yêu cầu pháp lý bắt buộc (nhật ký truy cập) và vi phạm nguyên tắc phân tách
nhiệm vụ ở 3 điểm.

---

## 1. Khung chuẩn áp dụng

Không có văn bản nào của Bộ Y tế quy định sẵn "lễ tân được quyền gì, bác sĩ được quyền gì" ở mức mã quyền.
Cái tồn tại là **các nguyên tắc bắt buộc** mà ma trận phân quyền phải thỏa mãn:

| # | Nguồn | Nội dung ràng buộc phân quyền |
|---|-------|-------------------------------|
| C1 | **Thông tư 13/2025/TT-BYT** (ban hành 06/6/2025, hiệu lực 21/7/2025) — hướng dẫn triển khai HSBA điện tử, thay thế TT 46/2018 | Điều 1 dẫn chiếu: HSBA điện tử phải tuân thủ pháp luật về **dữ liệu, CNTT, giao dịch điện tử, an toàn thông tin mạng và bảo vệ dữ liệu cá nhân**. Điều 2 yêu cầu hạ tầng có **giải pháp bảo mật**. Điều 3 cho phép xác thực nội dung bằng **chữ ký số / sinh trắc học / phương thức xác nhận điện tử khác**. Cơ sở KCB phải **tự ban hành quy chế** về tạo lập, cập nhật, quản lý, lưu trữ, sử dụng và bảo mật thông tin. → **Ma trận phân quyền là tài liệu bắt buộc phải có của cơ sở, không phải tùy chọn.** Hạn chót: bệnh viện 30/9/2025, cơ sở KCB khác (phòng khám) **31/12/2026**. |
| C2 | **Luật Khám bệnh, chữa bệnh 2023** (Điều 69 — khai thác hồ sơ bệnh án) | Chỉ các đối tượng luật định được khai thác HSBA: người hành nghề trực tiếp điều trị, người làm công tác chuyên môn/nghiên cứu/thống kê được người đứng đầu cơ sở cho phép, cơ quan có thẩm quyền, người bệnh/người đại diện. → **Nguyên tắc need-to-know có cơ sở pháp lý**, không chỉ là best practice. |
| C3 | **Luật Bảo vệ dữ liệu cá nhân 2025** + **Nghị định 356/2025/NĐ-CP** (hiệu lực **01/01/2026** — đã có hiệu lực tại thời điểm rà soát), thay thế NĐ 13/2023 | **Tình trạng sức khỏe là dữ liệu cá nhân nhạy cảm** → nghĩa vụ bảo vệ tăng cường: tối thiểu hóa dữ liệu, kiểm soát truy cập chặt, **ghi nhận/lưu vết hoạt động xử lý**, thông báo cho chủ thể dữ liệu, quyền yêu cầu xóa dữ liệu. |
| C4 | **QĐ 4750/QĐ-BYT** (XML 4210) | Dữ liệu xuất giám định gắn định danh CSKCB + người ký → cần kiểm soát ai được `bhyt.sign`, `bhyt.submit`. |
| C5 | **TT 27/2021/TT-BYT** (Đơn thuốc Quốc gia) | Đơn thuốc phải do **người kê đơn có chứng chỉ hành nghề** ký; mã đơn liên thông gắn định danh bác sĩ. → `prescription.sign` chỉ bác sĩ. |
| C6 | Thông lệ kỹ thuật quốc tế (tinh thần HIPAA Security Rule §164.312, ISO 27799, NIST RBAC) | 4 nguyên tắc: **least privilege**, **need-to-know**, **segregation of duties (SoD)**, **accountability qua audit trail bất biến**. VN không bắt buộc HIPAA nhưng đây là chuẩn kỹ thuật phổ biến và không mâu thuẫn với C1–C3. |

**Kết luận khung chuẩn:** ma trận phân quyền của Pro-Diab phải chứng minh được 4 điều —
(a) mỗi role chỉ có quyền tối thiểu cho công việc của họ; (b) không ai vừa thực hiện vừa tự duyệt
một hành vi có rủi ro; (c) mọi truy cập dữ liệu bệnh nhân đều có vết; (d) dữ liệu nhạy cảm
(CCCD, BHYT, chẩn đoán) chỉ hiển thị cho người cần biết.

---

## 2. Hiện trạng đã kiểm chứng trong code

### 2.1 Điểm ĐẠT (giữ nguyên, không cần sửa)

| Hạng mục | Bằng chứng |
|---|---|
| Mã hóa PII tại rest | `PatientConfiguration.cs` — `IdNumberEnc`, `street_enc`… dùng `HasConversion(PiiConverter)`; có `PiiBackfillService`, có `encryption.rotate` (chỉ admin). |
| Masking PII ở tầng API | `PatientMappingHelper.MaskIdNumber/MaskCardNo/MaskPhone`; `PatientEntityMapper` trả `IdNumberMasked` cho **mọi role**, kể cả bác sĩ. **Không có mã quyền `patient.pii.reveal`** → không role nào xem được CCCD/BHYT đầy đủ qua API bệnh nhân. Đạt trên mức tối thiểu của C3. |
| Tìm kiếm không lộ PII | Dùng blind index (`IdNumberBidx`) thay vì giải mã rồi so sánh. |
| Audit ghi/sửa | `AuditService` + `IAuditService` được inject ở **42 handler**: bệnh nhân, EMR, đơn thuốc, kết quả XN/CĐHA, kho, thanh toán, gói, user/role, BHYT, telehealth. Có `CrossTenantAttempt`, `Severity`, `IpAddress`, `RequestId`, `AuditAnomalyDetectionJob`. |
| Đính chính bệnh án có vết | `EncounterAddendumHandlers` bắt buộc `Reason`, dẫn chiếu TT 32/2023, kiểm tra quyền 2 lớp (controller + handler). Đúng tinh thần C1/C2. |
| `audit.review` / `audit.export` | Không cấp cho role nào trong 9139 → chỉ admin xem log. Đúng SoD. |
| `prescription.sign` | Chỉ `bac_si`. Đúng C5. |
| `bhyt.sign/submit/export` | Chỉ `ke_toan`. Bác sĩ chỉ có `bhyt.read`. Đúng C4. |
| `role.write` / `user.assign_role` / `system.config` / `tenant.write` | Không cấp cho role nào. Đúng least privilege. |
| Multi-tenant | Global query filter + branch filter trên cả `AuditLog`. |

### 2.2 Điểm LỆCH CHUẨN

---

#### P0-01 — Không có nhật ký TRUY CẬP (đọc) hồ sơ bệnh án

- **Quy định liên quan:** C1 (TT 13/2025 dẫn chiếu Luật ATTT mạng + Luật BVDLCN), C2 (Điều 69 — phải xác định được ai đã khai thác HSBA), C3 (nghĩa vụ ghi nhận hoạt động xử lý dữ liệu nhạy cảm).
- **Hiện trạng:** `AuditAction` (file `backend/src/ProDiabHis.Domain/Entities/AuditLog.cs`) chỉ định nghĩa `CREATE, UPDATE, DELETE, LOGIN, LOGOUT, EXPORT, SIGN, ENCRYPTION_ROTATE, FAILED_LOGIN, CROSS_TENANT_ATTEMPT`. Grep toàn `backend/src` cho `"VIEW"`, `"READ"`, `"ACCESS"` → **không có kết quả nào**. Nghĩa là: một tài khoản bác sĩ có thể mở hồ sơ của **toàn bộ bệnh nhân trong tenant** và hệ thống **không lưu bất kỳ vết nào**.
- **Rủi ro:** Đây là lỗ hổng **tuân thủ**, không phải lỗ hổng kỹ thuật. Khi bị khiếu nại "ai đã xem bệnh án của tôi" hoặc khi thanh tra Sở Y tế/A05 kiểm tra theo Luật BVDLCN, cơ sở **không có khả năng chứng minh**. Đồng thời triệt tiêu giá trị của `AuditAnomalyDetectionJob` (không phát hiện được hành vi lướt hàng loạt hồ sơ để lấy dữ liệu).
- **Đề xuất:** Đây là **thay đổi code, không phải SQL**. Bổ sung `AuditAction.View = "VIEW"` và ghi audit ở các query handler nhạy cảm: `GET /patients/{id}` (chi tiết), `emr.read`, `encounter` detail, `lab_result`/`rad_result` detail, `prescription` detail. Ghi ở mức **chi tiết một bệnh nhân**, KHÔNG ghi cho endpoint list/search (tránh phình bảng). Kèm: bổ sung mã quyền `patient.pii.reveal` nếu sau này cần chức năng xem CCCD đầy đủ.

---

#### P1-02 — Bác sĩ vừa chỉ định CLS vừa chốt thanh toán và miễn phí CLS

- **Nguyên tắc liên quan:** C6 — segregation of duties. Người tạo nghĩa vụ tài chính không được là người xác nhận nghĩa vụ đó đã hoàn thành hoặc được xóa bỏ.
- **Hiện trạng:** Trong 9139, `bac_si` được cấp `cls_round.create`, `cls_round.submit`, **`cls_round.pay`**, **`cls_round.waive`**. Kiểm chứng `ClsRoundHandlers.cs` dòng 306–400: `PayClsRoundCommandHandler` thực hiện `UPDATE ... SET payment_status='PAID'`, `WaiveClsRound` thực hiện `SET payment_status='WAIVED'`. Đây là **thao tác tài chính thật sự**, không phải cờ hiển thị. Ngược lại, `ke_toan` **không có bất kỳ mã `cls_round.*` nào** — kể cả `cls_round.read`.
- **Rủi ro:**
  - Gian lận: một bác sĩ có thể chỉ định CLS, thu tiền mặt trực tiếp của bệnh nhân, tự bấm PAID, tiền không vào sổ quỹ của kế toán. Hoặc bấm WAIVE cho người quen — không ai đối soát được vì kế toán không nhìn thấy vòng CLS.
  - Chức năng: kế toán **không thể** thu tiền CLS đúng quy trình vì không có quyền đọc/chốt vòng CLS. Nghĩa là mô hình hiện tại **ép** phải để bác sĩ thu tiền.
- **Đề xuất:**
  - Cấp cho `ke_toan`: `cls_round.read`, `cls_round.pay`, `cls_round.waive`.
  - Thu hồi của `bac_si`: `cls_round.pay`, `cls_round.waive`. Giữ `cls_round.create`, `cls_round.submit`, `cls_round.read`.
  - Nếu nghiệp vụ thực tế của phòng khám nhỏ **bắt buộc** bác sĩ thu tại chỗ (không có quầy thu ngân ngoài giờ): giữ `cls_round.pay` cho bác sĩ nhưng **bắt buộc thu hồi `cls_round.waive`** (miễn phí phải là quyết định của cấp quản lý), và bổ sung audit `Severity=WARN` cho mọi lần bác sĩ tự pay.

---

#### P1-03 — Kỹ thuật viên tự nhập rồi tự duyệt kết quả xét nghiệm của chính mình

- **Nguyên tắc liên quan:** C6 (SoD), C2 (kết quả CLS là căn cứ chẩn đoán — cần người thứ hai xác nhận).
- **Hiện trạng:** `ky_thuat_vien` có đồng thời `lab_result.write` + `lab_result.verify` + `lab_result.import`, và `rad_result.write` + `rad_result.verify`. Kiểm chứng `LabResultHandlers.cs`: `CreateLabResult` set `PerformedBy = _user.UserId`; `VerifyLabResultCommandHandler` (dòng 270–295) set `VerifiedBy = _user.UserId` **và không hề so sánh với `PerformedBy`** → tự duyệt kết quả của chính mình được chấp nhận. Nghiêm trọng hơn: `ImportLabResultsCommand` có cờ **`AutoVerify`** (dòng 404, 424–425) — khi bật, kết quả import hàng loạt được đánh `Verified` ngay, **bỏ qua hoàn toàn bước duyệt**.
- **Rủi ro:** Kết quả sai (nhập nhầm đơn vị, nhầm bệnh nhân, file máy XN lỗi) đi thẳng vào bệnh án và làm căn cứ kê đơn mà không có mắt thứ hai. Với phòng khám nội tiết (HbA1c, glucose), sai số này dẫn tới sai liều insulin — rủi ro an toàn người bệnh, không chỉ rủi ro tuân thủ.
- **Đề xuất (2 phương án, cần leader chọn):**
  - **PA-A (chuẩn, khuyến nghị):** Tách vai — KTV giữ `*.write`/`*.import`, chuyển `lab_result.verify` + `rad_result.verify` sang `bac_si`. Bác sĩ đọc kết quả trước khi nó hiển thị chính thức.
  - **PA-B (thực dụng cho phòng khám 2–5 bác sĩ, ít KTV):** Giữ `verify` cho KTV **nhưng sửa code** chặn `VerifiedBy == PerformedBy` (trả `LAB_RESULT_SELF_VERIFY_FORBIDDEN`), và cấp thêm `verify` cho `bac_si` để luôn có người thứ hai. Đồng thời **bỏ cờ `AutoVerify`** hoặc giới hạn nó chỉ dùng cho kết nối máy XN đã ký (`lab_integration`), không cho import file thủ công.
  - Cả 2 phương án đều cần: cấp `lab_result.verify`/`rad_result.verify` cho `bac_si` (hiện bác sĩ chỉ có `lab_result.read`).

---

#### P1-04 — Report engine giải mã PII và trả về cho mọi role có quyền báo cáo

- **Quy định liên quan:** C3 (tối thiểu hóa dữ liệu nhạy cảm), C2 (need-to-know).
- **Hiện trạng:** `GenericReportDataService.cs` dòng 64–67:
  ```csharp
  // Hang muc 6: cot PII duoc SELECT o dang *_enc -> giai ma theo tien to marker.
  dict[kv.Key] = kv.Value is string sv ? PiiCrypto.Unprotect(sv) : kv.Value;
  ```
  Mọi cột string trong kết quả báo cáo đều được **giải mã tự động**. `ReportRegistry.cs` dòng 851 đã có descriptor SELECT `pt.street_enc AS address` → **địa chỉ nhà bệnh nhân hiện ra dạng plaintext trong báo cáo**, trong khi API bệnh nhân thì mask kỹ. Hiện `report.read` được cấp cho **cả 5 role**, `report.build` cấp cho `bac_si`, `duoc_si`, `ke_toan`.
  Ngoài ra `ReportRegistry` có các báo cáo theo **chẩn đoán ICD-10** — `ke_toan` và `duoc_si` đều đọc được.
- **Rủi ro:**
  - Kênh báo cáo trở thành **đường vòng qua toàn bộ lớp masking PII** đã đầu tư. Ai thêm một descriptor mới có `id_number_enc` là lộ CCCD toàn bộ bệnh nhân, không cần quyền gì đặc biệt.
  - Kế toán/dược sĩ đọc được chẩn đoán gắn tên bệnh nhân → vượt need-to-know (C2). Với bệnh lý nhạy cảm (HIV, tâm thần, sản) đây là rủi ro pháp lý thật.
- **Đề xuất:**
  - **Code (ưu tiên hơn SQL):** đổi `PiiCrypto.Unprotect` mặc định thành **mask** trong report engine; chỉ giải mã đầy đủ khi descriptor khai báo tường minh `AllowPiiPlaintext = true` **và** người dùng có mã quyền mới `report.pii_plaintext` (chỉ admin).
  - **SQL:** thu hồi `report.build` khỏi `duoc_si` (dược sĩ chỉ cần báo cáo kho có sẵn, không cần report builder truy cập dataset chéo module). Cân nhắc thu hồi khỏi `bac_si`.
  - Phân tách `report.read` theo nhóm báo cáo (`report.clinical.read` / `report.finance.read` / `report.pharmacy.read`) — đây là **thay đổi lớn**, đề nghị đưa vào backlog sprint sau, không làm trong đợt này.

---

#### P2-05 — Dược sĩ nắm trọn vòng đời kho: nhập, sửa danh mục, điều chỉnh tồn, quản lý NCC

- **Nguyên tắc liên quan:** C6 (SoD trong kiểm soát tài sản/tồn kho).
- **Hiện trạng:** `duoc_si` có `drug.write`, `drug.import`, `drug.sync`, `stock.adjust`, `supplier.write`. Một người vừa tạo nhà cung cấp, vừa nhập hàng, vừa **tự điều chỉnh tồn kho** (`stock.adjust` — dùng cho kiểm kê/hao hụt).
- **Rủi ro:** Thất thoát thuốc được che bằng bút toán điều chỉnh tồn do chính người đó thực hiện. Với thuốc gây nghiện/hướng thần (nếu có) thì đây là vi phạm quy chế quản lý dược.
- **Đề xuất:** Thu hồi `stock.adjust` khỏi `duoc_si`, chuyển cho `admin` (hoặc role `quan_ly_kho` mới). Nếu phòng khám chỉ có 1 dược sĩ và không thể tách người: giữ quyền nhưng bắt buộc `reason` + audit `Severity=WARN` cho mọi lần adjust, và bổ sung báo cáo "Nhật ký điều chỉnh tồn" cho admin xem định kỳ.

---

#### P2-06 — `patient.write` là một mã quyền quá thô, lễ tân sửa được cả dữ liệu lâm sàng

- **Nguyên tắc liên quan:** C2 (need-to-know), C6 (least privilege).
- **Hiện trạng:** `PatientsController.cs` dùng **duy nhất** `patient.write` cho 14 endpoint khác nhau — bao gồm cả nhóm hành chính (nhân khẩu, BHYT, người giám hộ, liên hệ khẩn) **và** nhóm lâm sàng (dòng 220, 234, 245 — dị ứng/tiền sử). `le_tan` có `patient.write` → **lễ tân sửa được danh sách dị ứng thuốc của bệnh nhân**.
- **Rủi ro:** Dị ứng là dữ liệu an toàn người bệnh, là đầu vào của cảnh báo DDI khi kê đơn. Người không có chuyên môn sửa được → rủi ro lâm sàng trực tiếp. Về pháp lý, đây là nội dung HSBA mà theo C2 chỉ người hành nghề được cập nhật.
- **Đề xuất:** Tách `patient.clinical.write` (dị ứng, tiền sử) khỏi `patient.write` (hành chính) trong controller, cấp `patient.clinical.write` cho `bac_si` + `ky_thuat_vien`, **không** cấp cho `le_tan`. Đây là **thay đổi code + SQL đi kèm**, không làm được bằng SQL đơn thuần.

---

#### P2-07 — `user.read` cấp cho lễ tân và bác sĩ để phục vụ một nhu cầu rất hẹp

- **Nguyên tắc liên quan:** C6 (least privilege).
- **Hiện trạng:** 9139 cấp `user.read` cho `le_tan` và `bac_si` với ghi chú "xem danh bạ nhân sự để chọn bác sĩ khi đặt lịch". Nhưng `UsersController` với `user.read` trả về **toàn bộ hồ sơ nhân sự**: email, số điện thoại, trạng thái tài khoản, vai trò, chi nhánh của mọi user trong tenant.
- **Rủi ro:** Mức thấp (dữ liệu nội bộ), nhưng đây là mở rộng quyền không cần thiết và là bước đệm cho social engineering / dò tài khoản.
- **Đề xuất:** Bổ sung endpoint hẹp `GET /api/v1/doctors/lookup` (chỉ trả id + họ tên + chuyên khoa + phòng) bảo vệ bằng `appointment.read`, rồi **thu hồi `user.read`** khỏi `le_tan` và `bac_si`. Cho tới khi có endpoint đó, giữ nguyên (chấp nhận rủi ro).

---

#### P2-08 — Kế toán có `branch.cross_view` + `patient.read`

- **Hiện trạng:** `ke_toan` được cấp `branch.cross_view` → xem dữ liệu **xuyên chi nhánh**, kèm `patient.read`.
- **Rủi ro:** Hợp lý cho báo cáo tài chính hợp nhất, nhưng kết hợp với `patient.read` thì kế toán đọc được hồ sơ bệnh nhân của **mọi chi nhánh**. Vượt need-to-know.
- **Đề xuất:** Giữ `branch.cross_view` (nghiệp vụ tài chính cần) nhưng ràng buộc ở code: `IgnoreBranchFilter` chỉ có hiệu lực cho dataset tài chính, không áp cho `Patients`/`Encounters`. Không cần đổi SQL.

---

#### P3-09 — Ghi chú nhỏ, không cần hành động ngay

- `ky_thuat_vien` có `file.delete` → nên chuyển sang soft-delete + audit thay vì thu hồi quyền (KTV cần xóa file upload nhầm).
- `le_tan` có `billing.create` + `package_subscription.collect` nhưng không có `cashier.shift_open/close` → thu tiền gói mà không nằm trong ca quỹ nào. Nếu nghiệp vụ cho phép lễ tân thu tiền, nên cấp thêm cụm `cashier.shift_*` để tiền có sổ ca; nếu không, thu hồi `package_subscription.collect`.
- `duoc_si` có `dtqg.submit` — đúng (dược sĩ đẩy lại đơn khi liên thông lỗi), giữ nguyên.

---

## 3. Bảng tổng hợp mức độ

| Mã | Vấn đề | Mức | Sửa bằng |
|----|--------|-----|----------|
| P0-01 | Không có nhật ký truy cập (đọc) HSBA | **P0** | Code |
| P1-02 | Bác sĩ tự chốt PAY/WAIVE vòng CLS; kế toán không có quyền CLS | **P1** | SQL |
| P1-03 | KTV tự nhập + tự duyệt kết quả; `AutoVerify` bỏ qua duyệt | **P1** | SQL + Code |
| P1-04 | Report engine giải mã PII cho mọi role; kế toán/dược sĩ đọc chẩn đoán | **P1** | Code (+ SQL phụ) |
| P2-05 | Dược sĩ nắm trọn vòng đời kho (`stock.adjust`) | P2 | SQL |
| P2-06 | `patient.write` quá thô, lễ tân sửa được dị ứng | P2 | Code + SQL |
| P2-07 | `user.read` cho lễ tân/bác sĩ quá rộng | P2 | Code + SQL |
| P2-08 | Kế toán `branch.cross_view` + `patient.read` | P2 | Code |
| P3-09 | Ghi chú nhỏ (file.delete, ca quỹ lễ tân) | P3 | Tùy chọn |

**Tổng: 1 điểm P0, 3 điểm P1, 4 điểm P2, 1 nhóm P3.**

---

## 4. Kết luận và khuyến nghị

**Ma trận 9139 chưa "đủ chuẩn", nhưng cũng không phải thảm họa.** Nền tảng tốt hơn mức trung bình của HIS
phòng khám VN: PII đã mã hóa và mask triệt để, audit ghi/sửa phủ 42 handler, multi-tenant có filter,
đính chính bệnh án có lý do bắt buộc, quyền quản trị được giữ chặt. Vấn đề nằm ở **3 khoảng trống kiểm soát**:
không lưu vết đọc, không tách nhiệm vụ ở 3 chỗ có rủi ro tài chính/lâm sàng, và một đường vòng qua lớp
masking PII ở module báo cáo.

**Có nên áp dụng ngay đề xuất SQL không? — Khuyến nghị: KHÔNG áp dụng toàn bộ ngay.**

Lý do: file `9139` vừa được deploy để **fix lỗi 403 hàng loạt** từ QC ngày 29/8. Áp thêm một migration thu hồi
quyền ngay sau đó có nguy cơ tái phát 403 trên chính các màn hình vừa fix xong. Đề nghị tách làm 3 đợt:

| Đợt | Nội dung | Điều kiện |
|-----|----------|-----------|
| **Đợt 1 — làm ngay** | Phần A của file đề xuất: cấp `cls_round.read/pay/waive` cho `ke_toan`, cấp `lab_result.verify`+`rad_result.verify` cho `bac_si`. **Chỉ THÊM quyền, không thu hồi gì** → rủi ro regression = 0, mà đã đóng được nửa của P1-02 và P1-03. | Áp được ngay sau khi leader duyệt. |
| **Đợt 2 — sau khi QC xác nhận** | Phần B: thu hồi `cls_round.pay/waive` khỏi `bac_si`, thu hồi `stock.adjust` + `report.build` khỏi `duoc_si`. | **Bắt buộc** hỏi user/chủ phòng khám trước: thực tế bác sĩ có thu tiền CLS tại chỗ không? Dược sĩ có phải người kiểm kê kho không? Nếu có thì giữ quyền + bù bằng audit. |
| **Đợt 3 — backlog sprint sau** | P0-01 (audit VIEW), P1-04 (report PII), P2-06 (tách `patient.clinical.write`), P2-07 (endpoint doctors lookup). Toàn bộ là thay đổi code, cần architect thiết kế. | Ưu tiên P0-01 lên đầu backlog — đây là **yêu cầu tuân thủ có hạn chót 31/12/2026** theo TT 13/2025 cho cơ sở KCB ngoài bệnh viện. |

File SQL đề xuất: `db/migrations/9141_rbac_standard_alignment_proposal.sql.review`
(đuôi `.review` — migrator sẽ không tự chạy; đổi tên thành `.sql` khi quyết định áp dụng).

---

## 5. Nguồn tham chiếu

- [Thông tư 13/2025/TT-BYT hướng dẫn triển khai hồ sơ bệnh án điện tử](https://caselaw.vn/van-ban-phap-luat/587211-thong-tu-so-13-2025-tt-byt-ngay-06-06-2025-cua-bo-truong-bo-y-te-huong-dan-trien-khai-ho-so-benh-an-dien-tu)
- [Thông tư 13/2025/TT-BYT — mốc thời gian bắt buộc triển khai](https://caselaw.vn/bai-viet/ho-so-benh-an-dien-tu-bat-buoc-trien-khai-cham-nhat-tu-30-9-2025-theo-thong-tu-13-2025-tt-byt)
- [Tình trạng sức khỏe là dữ liệu cá nhân nhạy cảm từ 2026](https://thuvienphapluat.vn/hoi-dap-phap-luat/tinh-trang-suc-khoe-la-du-lieu-ca-nhan-nhay-cam-tu-nam-2026-dung-khong-138076906.html)
- [Dữ liệu cá nhân nhạy cảm từ 01/01/2026 — Luật BVDLCN 2025 & NĐ 356/2025 thay thế NĐ 13/2023](https://thuvienphapluat.vn/phap-luat/ho-tro-phap-luat/du-lieu-ca-nhan-nhay-cam-tu-01012026-khac-gi-so-voi-quy-dinh-cu-khi-xu-ly-du-lieu-ca-nhan-can-tuan--251479.html)
- [Thông tin về sức khỏe là dữ liệu cá nhân nhạy cảm](https://luatvietnam.vn/tin-van-ban-moi/thong-tin-ve-suc-khoe-la-du-lieu-ca-nhan-nhay-cam-186-106394-article.html)
