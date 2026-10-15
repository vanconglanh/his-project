# Kiểm tra chất lượng dữ liệu production pro-his-db — 2026-10-16

> DB: `prodiab_his` (container `pro-his-db`, server VNG 192.168.1.4), tenant_id = 1.
> Phạm vi ghi: CHỈ các bảng nghiệp vụ HIS mới `diab_his_*`. Không đụng SUNS, `diab_his` nguồn, staging.
> Backup trước khi sửa: `/home/stackops/backups/pro-his/prodiab_his_20261016_013223.sql.gz`.
> Script sửa (idempotent): `docs/migration/fix_prod_data_20261016.sql`.

## 1. Tổng quan số lượng (khớp báo cáo migration trước)

| Bảng | Số bản ghi |
|---|---|
| Bệnh nhân (`diab_his_pat_patients`) | 358 |
| Lượt khám (`diab_his_enc_encounters`) | 649 |
| Chẩn đoán | 524 |
| EMR | 649 |
| Sinh hiệu | 212 |
| Lab order | 506 |
| Lab result | 2.609 |
| Đơn thuốc | 147 |
| Dòng đơn thuốc | 517 |
| Thuốc | 248 |
| fil_files (CLS upload) | 181 |
| User | 31 | Phòng 9 | Dịch vụ 47 |

## 2. Những điểm ĐẠT (không cần sửa)

- **tenant_id**: 100% = 1, không null/sai ở mọi bảng nghiệp vụ.
- **Toàn vẹn FK**: 0 orphan ở encounter→patient, diagnoses/emr/vital/lab_order/lab_result→encounter, prescription→encounter/patient.
- **PII**: `phone`/`id_number` đã mã hóa (`_enc`) + blind index (`_bidx`) nhất quán 100%; không còn plaintext phone. (Blind index đã backfill.)
- **Encoding tiếng Việt**: KHÔNG có mojibake. (Nghi ngờ ban đầu 35 tên BN + 12 dịch vụ là **false positive** — hex xác nhận UTF-8 đúng, vd "CHÂU THỊ NGUYỆT", "Điện tâm đồ".)
- **Trùng mã**: 0 trùng `code` ở BN / thuốc / dịch vụ.
- **Ngày tháng**: `started_at` không có giá trị tương lai/trước 2015; DateTime serialize có hậu tố `Z` (UTC) qua API — đúng chuẩn.
- **Giá âm**: 0.
- **Kiểm qua ứng dụng thật** (API production `127.0.0.1:19090`, 4 vai trò non-admin bác sĩ/lễ tân/dược sĩ/kế toán): danh sách/chi tiết BN, lượt khám, EMR, sinh hiệu, lab order, lab result, đơn thuốc, danh mục thuốc/dịch vụ đều trả **HTTP 200**, hiển thị đúng dữ liệu tiếng Việt. **Không có lỗi 500** trong `docker logs pro-his-backend`.

## 3. Vấn đề phát hiện & ĐÃ SỬA

Đối chiếu enum với backend (`Domain/Entities/*`): nhiều giá trị status migrate để **ngoài tập hợp lệ** nên ứng dụng không diễn giải đúng (lọc/state-machine bỏ sót).

| # | Vấn đề | Mức độ | Số bản ghi | Xử lý |
|---|---|---|---|---|
| FIX-1 | `cli_lab_orders.status = 'COMPLETED'` (hoa) — app dùng chữ thường `done` | Cao | 506 | → `done` |
| FIX-2 | `cli_lab_orders.priority = 'ROUTINE'` — app chỉ nhận `NORMAL/URGENT/STAT` | Cao | 506 | → `NORMAL` |
| FIX-3 | `lab_results.status = 'PRELIMINARY'` — app chỉ nhận `DRAFT/VERIFIED/AMENDED` (PRELIMINARY chỉ là giá trị FHIR output) | Cao | 2.609 | → `VERIFIED` (kết quả lịch sử đã có giá trị) |
| FIX-4 | `lab_results.source = 'LEGACY_IMPORT'` — app chỉ nhận `MANUAL/IMPORT/PARTNER` | TB | 2.609 | → `IMPORT` |
| FIX-5 | `enc_encounters.status = 'IN_PROGRESS'` cho **toàn bộ** lượt khám lịch sử (LEG-) — kẹt hàng chờ làm việc của bác sĩ | Cao | 649 | → `DONE`. Đồng thời xóa `finished_at` rác (discharge < admission) về NULL (224) |
| FIX-6 | 4 thuốc gán **sai giá** khi import SUNS | Cao | 4 | → giá 0 + ghi chú "GIA CAN NHAP TAY" |
| FIX-7 | 8 dòng đơn thuốc trỏ `drug_id` không tồn tại (FK gãy) | TB | 8 | Relink theo `TRIM(drug_name)` khớp duy nhất → 0 orphan, 0 mislink |

Chi tiết 4 thuốc FIX-6 (cần nhập lại giá đúng):
- `LEG-8AA1BB33` **Augmentin** — bị gán 31.000đ của "AUGMENTIN 1G".
- `LEG-42FAEACA` **Augmentin 500mg** — bị gán 31.000đ của "AUGMENTIN 1G".
- `LEG-2366A0E3` **XIGDUO XR 10mg/500mg** — bị gán 33.000đ của "Xigduo XR 10mg/1000mg".
- `LEG-BDAFD05E` **Xigduo XR 5mg/500mg** — bị gán 33.000đ của "Xigduo XR 10mg/1000mg".

**Verify sau sửa** (DB + API): orphan=0, IN_PROGRESS=0, lab_order≠done=0, lab_result≠VERIFIED=0, finished<started=0, tenant sai=0. API trả encounter `DONE`, lab-order `done/NORMAL`, lab-result `VERIFIED/IMPORT`, không lỗi 500.

## 4. Vấn đề CHƯA sửa — cần user/clinic quyết định

| Vấn đề | Mức độ | Số | Vì sao chưa tự sửa |
|---|---|---|---|
| **doctor_id NULL toàn bộ** lượt khám + đơn thuốc (`doctor_id=''`) | Cao | 649 / 147 | Nguồn legacy `cli_visits.ATTENDING_PROVIDER_ID` **tất cả = 1**, tên provider là **"A B" (dummy)**. Không có dữ liệu bác sĩ-theo-lượt-khám thực ở bất kỳ đâu (mới lẫn cũ). Bịa bác sĩ = sai lệch hồ sơ lâm sàng. **Cần clinic gán bác sĩ** (hoặc chấp nhận để trống cho dữ liệu lịch sử). App xử lý doctor_id null an toàn (không crash). |
| **Trùng bệnh nhân** | TB | 6 nhóm | Gồm data test ("TEST", "MINH CHI TEST", "TEST MINH CHI") và 2 ca thật cùng `phone_bidx` (HOÀNG THỊ HUỆ, VŨ ĐÌNH BẢO). Merge BN có lượt khám đi kèm là thao tác rủi ro, cần clinic xác nhận ca nào gộp/xóa. |
| **Đơn thuốc không có giá** (`unit_price=0`, `line_total=0`) | TB | 517 (tất cả) | Migrate không mang giá từ HIS cũ. Có thể backfill theo `drug.sell_price × quantity` nhưng nhiều thuốc giá đang 0 → chỉ backfill được một phần. Chờ quyết định. |
| **85 thuốc giá bán = 0** (gồm 4 thuốc FIX-6) | TB | 85 | Phần lớn legacy không có giá. Cần nhập tay giá bán. |

## 5. Vấn đề do CODE (không phải dữ liệu) — chỉ báo, KHÔNG deploy

> Theo yêu cầu: lỗi do code chỉ ghi lại, không tự sửa/deploy.

1. **Tìm kiếm bệnh nhân không lọc**: `GET /api/v1/patients?search=...` trả về **toàn bộ 358 BN** với mọi giá trị (kể cả chuỗi vô nghĩa `ZZZZZ...`). Tham số `search` bị bỏ qua ở endpoint list. Dữ liệu `phone_bidx` đã đúng & nhất quán — đây là vấn đề code/param, không phải data. Cần dev kiểm lại handler list patients (regression BM-04 vẫn xanh vì BN mục tiêu nằm trong tập trả về).
2. **`patient_name`/`doctor_name`/`room_name` = null** ở API chi tiết encounter & prescription — có thể DTO chi tiết không join tên. Cần dev xác nhận.
3. **`finished_at` null serialize thành `'0001-01-01T00:00:00Z'`** (DateTime.MinValue) thay vì null ở DTO encounter — nên dùng `DateTime?`.
4. **`lab_results.result_flag` varchar(5)** bị cắt cụt ("NORMA", "ABNOR"...) khi import — nhưng API dùng `flag`/`is_abnormal` nên không lộ ra ngoài (ảnh hưởng thấp).
5. Quan sát: entity EF `Prescription.DoctorId` khai báo `Guid` non-nullable trong khi cột cho phép rỗng — chỉ an toàn vì mọi đường đọc đi qua Dapper.

## 6. Cơ sở dữ liệu đã backup

- Trước khi sửa: `/home/stackops/backups/pro-his/prodiab_his_20261016_013223.sql.gz` (mysqldump đầy đủ, single-transaction).
- Mọi thay đổi ở mục 3 có thể hoàn tác từ backup này nếu cần.
