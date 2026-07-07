# 単体テスト仕様書 (UTC) — Màn hình **Bệnh nhân** (Patient)

> Quy ước & catalog test viewpoint: xem [utc-00-quy-uoc-chuan-nhat.md](utc-00-quy-uoc-chuan-nhat.md). File này bám code thật (FE `patient-schema.ts`/`PatientGeneralTab.tsx` · BE `PatientCommands.cs`/`PatientCommandHandler.cs` · DB `9002_create_patient.sql` + EF `PatientConfiguration.cs`).

| Mục | Nội dung |
|---|---|
| 機能ID | `PAT-CRUD-001` |
| Màn hình | Bệnh nhân → Tạo/Sửa hồ sơ (tab **Thông tin chung**) |
| Route FE | `/patients` (list), form tạo/sửa |
| API base | `/api/v1/patients` |
| Bảng DB | `diab_his_pat_patients` — `9002_create_patient.sql:17` (⚠️ KHÔNG dùng file dump `db/diab_his_pat_patients.sql` cột IN HOA — khác schema thật) |
| PK / Unique | `id` CHAR(36) · UNIQUE `(tenant_id, code)` |
| Permission | Xem `patient.read` · Ghi `patient.write` · Xóa `patient.delete` |
| Mã hóa | **AES-256-GCM** cho `id_number` (CMND/CCCD) & `card_no` (BHYT) → cột `*_enc` + `*_masked` |
| Đặc thù | `code` server tự sinh `BNT{tenant:D2}{seq:D6}`; POST trùng mã → 409 `PATIENT_CODE_EXISTS` |

## 1. Field matrix (bám 3 tầng)

| Field (FE id) | Nhãn | Control | FE required/rule | BE (DTO / validator) | DB: type/null/default | Ghi chú GAP |
|---|---|---|---|---|---|---|
| full_name | Họ tên | Input text | **✅ required** min2, max200 | `string FullName` non-null; **KHÔNG validator** | `full_name` VARCHAR(255) NOT NULL | FE max200 < DB255; BE không chặn rỗng |
| gender | Giới tính | Select MALE/FEMALE/OTHER | optional | `string?`; ko enum-check | `gender` VARCHAR(10) NULL | Enum chỉ ở FE |
| date_of_birth | Ngày sinh | Input date, max=today | optional, < hôm nay | `DateOnly?` | `date_of_birth` DATE NULL | |
| id_number | CMND/CCCD | Input text | optional, regex `^\d{9}$\|^\d{12}$` | `string?`; ko validator | **`id_number_enc` VARCHAR(500)** + `id_number_masked` VARCHAR(20) | **MÃ HÓA**; ko unique |
| phone | Điện thoại | Input text | optional, regex `^(\+84\|0)\d{9,10}$` | `string?` | `phone` VARCHAR(30) NULL | regex chỉ FE |
| email | Email | Input email | optional, `.email()` | `string?` | `email` VARCHAR(100) NULL | |
| occupation | Nghề nghiệp | Input text | optional, **ko maxLen** | `string?` | `occupation` VARCHAR(100) NULL | FE thiếu maxLen |
| ethnicity | Dân tộc | Input text | optional, **ko maxLen** | `string?` | `ethnicity` VARCHAR(50) NULL | FE thiếu maxLen |
| blood_type | Nhóm máu | Select A_POS…O_NEG/UNKNOWN | optional enum | `string?` | `blood_type` VARCHAR(**5**) NULL | ⚠️ `AB_POS`(6)/`UNKNOWN`(7) > 5 → truncate/lỗi |
| id_card_issued_place | Nơi cấp | Input text | optional, **max255** | `string?` | VARCHAR(**100**) NULL | ⚠️ FE max255 > DB100 |
| id_card_issued_date | Ngày cấp | Input date | optional, < hôm nay | `DateOnly?` | DATE NULL | |
| nationality | Quốc tịch | Select, default VN | optional | `string="VN"` | VARCHAR(5) NOT NULL DEF 'VN' | |
| marital_status | Hôn nhân | Select | optional | `string?` | VARCHAR(20) NULL | |
| patient_type | Đối tượng | Select SERVICE/BHYT/FREE/CONTRACT | optional, default SERVICE | `string="SERVICE"` | VARCHAR(20) NOT NULL DEF 'SERVICE' | |
| visit_type | Loại khám | Select | optional, default FIRST_VISIT | `string?="FIRST_VISIT"` | VARCHAR(20) NULL DEF 'FIRST_VISIT' | |
| address.* | Địa chỉ | 4 Input (province/district/ward/street code) | optional | `AddressDto?` | 4 cột VARCHAR | |

**Field bắt buộc thật:** chỉ `full_name`. Các field có default (`code`,`status`,`nationality`,`patient_type`) server tự set.

## 2. Test cases

### A — 初期表示 (Load ban đầu)
| No | 中項目 | 観点 | 操作 | 期待結果 | 判定 |
|---|---|---|---|---|---|
| A01 | List | Render | Mở `/patients` | Bảng danh sách + nút "Thêm bệnh nhân" + ô tìm | |
| A02 | Form tạo | Control đủ | Bấm Thêm | Tab "Thông tin chung" mở; field rỗng; nationality=VN, patient_type=SERVICE, visit_type=FIRST_VISIT; focus Họ tên | |
| A03 | Dropdown load | Đủ option | Mở từng select | gender(3), blood_type(9), nationality, marital_status, patient_type(4), visit_type(4) hiển thị **nhãn tiếng Việt** | |
| A04 | Quyền | Ẩn nút | User ko `patient.write` | Nút Thêm/Sửa ẩn | |

### B — 項目表示・設定 (Nhập/hiển thị field)
| No | 中項目 | 観点 | データ | 期待結果 | 判定 |
|---|---|---|---|---|---|
| B01 | full_name tiếng Việt | Encoding | `Nguyễn Thị Bích Hằng` | Hiển thị đủ dấu | |
| B02 | date_of_birth | Control date | 01/01/1990 | Nhận đúng, ko cho tương lai | |
| B03 | Chọn gender/blood_type | Select | MALE / O_POS | Giá trị set đúng | |
| B04 | Load lại khi Sửa | Load đủ data | mở BN đã lưu | Mọi field đúng; **CMND hiển thị dạng che** (masked, ko plaintext) | |

### C — 入力チェック (Validate từng field)
| No | 中項目 | 観点 | データ | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|
| C01 | full_name bắt buộc | Mandatory | rỗng | Chặn, message bắt buộc | | zod min2 |
| C02 | full_name 1 ký tự | Min | `A` | Chặn (min 2) | | |
| C03 | full_name 200 / 201 | 境界値 | 200 / 201 ký tự | 200 OK; 201 FE chặn (max200) | | |
| C04 | id_number sai định dạng | Regex | `1234` (4 số) | FE báo "CMND/CCCD không hợp lệ" | | |
| C05 | id_number 9 số / 12 số | Hợp lệ | `012345678` / `012345678901` | Chấp nhận | | |
| C06 | phone sai | Regex | `12345` | FE báo lỗi ĐT | | |
| C07 | email sai | Format | `abc@` | FE báo lỗi email | | |
| C08 | date_of_birth tương lai | Ngày | ngày > hôm nay | Chặn | | |
| C09 | blood_type AB_POS | 境界値 DB(5) | chọn AB_POS | **Kỳ vọng:** lưu đúng | | ⚠️ **Defect#3** DB VARCHAR(5) truncate `AB_POS`(6) |
| C10 | id_card_issued_place 101-255 | 境界値 | 150 ký tự | **Kỳ vọng:** chặn | | ⚠️ **Defect#2** FE max255 > DB100 → truncate/lỗi |
| C11 | occupation > 100 | maxLen | 120 ký tự | Kỳ vọng chặn | | ⚠️ **Defect#4** FE ko maxLen, lỗi DB |
| C12 | **API trực tiếp bỏ FE** | Ko validator BE | POST `{full_name:""}` | **Kỳ vọng 422**; thực tế **lưu được** | | ⚠️ **Defect#1** ko validator BE |

### D/E — Business + DB登録
| No | 中項目 | 観点 | 操作 | 期待結果 | 判定 |
|---|---|---|---|---|---|
| E01 | Insert đúng bảng | Ghi DB | Tạo BN hợp lệ | 1 row `diab_his_pat_patients`; HTTP 201 | |
| E02 | code tự sinh | Auto code | Sau E01 | `code` = `BNT` + tenant(2) + seq(6); duy nhất | |
| E03 | **Mã hóa CMND** | Security | Nhập id_number `012345678901` → xem DB | `id_number_enc` là ciphertext (KHÔNG phải plaintext); `id_number_masked` che giữa; **ko có cột plaintext** | |
| E04 | Mã hóa BHYT | Security | Nhập card_no (tab BHYT) | `card_no_enc` mã hóa; masked giữ 5 đầu | |
| E05 | tenant_id | Multi-tenant | Sau E01 | = tenant JWT, ko theo client | |
| E06 | audit | created_* | Sau E01 | created_at/by set | |
| D01 | Tạo đua trùng code | Unique | 2 request đồng thời | 1 thành công, 1 nhận 409 `PATIENT_CODE_EXISTS` | |

### F — 登録後再表示
| No | 観点 | 操作 | 期待結果 | 判定 |
|---|---|---|---|---|
| F01 | List sau tạo | Reload list | BN mới xuất hiện, đủ cột | |
| F02 | Round-trip | Sửa lại | Field đúng như nhập; **CMND hiển thị masked** (giải mã đúng khi cần) | |
| F03 | Search | `/patients/search?q=` | Tìm theo tên/mã/ĐT ra BN mới | |

### G/H — Sửa / Xóa
| No | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| G01 | Update field | Sửa họ tên → PUT | DB cập nhật; updated_* đổi | | |
| G02 | Update CMND → mã hóa lại | Security | Đổi id_number | `id_number_enc`/masked cập nhật | | |
| G03 | Không xóa được field default qua Update | Pattern set-if-not-empty | gửi nationality="" | **Bị bỏ qua** (giữ giá trị cũ), ko báo lỗi | | ⚠️ **Defect#8** |
| H01 | Xóa mềm | Soft delete | DELETE | `deleted_at` set; ko xóa cứng; HTTP 204 | | perm `patient.delete` |
| H02 | Ẩn sau xóa | Filter | reload list | BN đã xóa ko hiển thị | | |

### I/J — Quyền, tenant, bảo mật
| No | 観点 | 操作 | 期待結果 | 判定 |
|---|---|---|---|---|
| I01 | Thiếu quyền | Authorization | POST khi ko `patient.write` | 403 | |
| I02 | Cách ly tenant | Multi-tenant | GET BN tenant khác | 404 | |
| J01 | XSS | Chống script | full_name=`<script>` | Hiển thị text, ko chạy | |
| J02 | SQLi | Param hóa | full_name=`x'); DROP--` | Lưu chuỗi, DB an toàn | |
| J03 | CMND ko lộ | Security | GET detail (API) | Trả **masked**, tuyệt đối ko plaintext id_number/card_no | |

## 3. 境界値一覧
| Field | min-1 | max | max+1 | Ghi chú |
|---|---|---|---|---|
| full_name | `""`/`A`(1) → NG | 200 | 201 → FE chặn | DB 255 |
| id_number | 8 số → NG | 9 hoặc 12 số | 10/11/13 số → NG | regex FE |
| blood_type | — | `A_POS`(5) | `AB_POS`(6) → ⚠️ DB truncate | Defect#3 |
| id_card_issued_place | — | 100 (DB) | 101 → ⚠️ lỗi | FE nhầm cho tới 255 |
| occupation | — | 100 (DB) | 101 → ⚠️ | FE ko chặn |

## 4. Defect candidates
| ID | Mức | Mô tả | Vị trí |
|---|---|---|---|
| #1 | **Cao** | KHÔNG có FluentValidation cho Patient (Create/Update) → gọi API thẳng bỏ mọi regex/mandatory (full_name rỗng, CMND/phone/email sai vẫn lưu) | `Patients/` ko có `AbstractValidator` |
| #2 | **Cao** | `id_card_issued_place`: FE max255 > DB VARCHAR(100) → 101-255 ký tự lỗi/truncate | schema:52 vs 9002:40 |
| #3 | **Cao** | `blood_type` DB VARCHAR(5) < giá trị enum `AB_POS`(6)/`UNKNOWN`(7) → truncate/sai dữ liệu | 9002:34 |
| #4 | TB | `occupation`/`ethnicity` FE ko maxLen, vượt DB 100/50 → lỗi DB | schema:36-37 |
| #5 | TB | Không enforce enum ở BE/DB (gender, blood_type, patient_type…) — chỉ FE dropdown | — |
| #6 | TB | Update "set-if-not-empty" → không thể clear nationality/patient_type/status qua Update | Handler:134-140 |
| #7 | Thấp | id_number/phone không unique → cho phép trùng CMND/ĐT giữa các BN | 9002 |

## 5. 実施サマリ
| Nhóm | Case | OK | NG | 保留 |
|---|---|---|---|---|
| A Load | 4 | | | |
| B Nhập/hiển thị | 4 | | | |
| C Validate | 12 | | | |
| D/E DB+business | 7 | | | |
| F Load sau ghi | 3 | | | |
| G/H Sửa/Xóa | 5 | | | |
| I/J Quyền/bảo mật | 5 | | | |
| **TỔNG** | **40** | | | |
