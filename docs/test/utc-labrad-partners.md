# 単体テスト仕様書 (UTC) — Màn hình **Đối tác CLS / Lab Partners**

> Quy ước & catalog: xem [utc-00-quy-uoc-chuan-nhat.md](utc-00-quy-uoc-chuan-nhat.md). Bám code thật: FE `LabPartnerForm.tsx` + `LabPartnersTab.tsx` · BE `LabPartnerHandlers.cs`/`LabPartnerDtos.cs` · DB `9006b_create_ext_tables.sql:494` (+`9012`) · EF `LabRadConfiguration.cs`.

| Mục | Nội dung |
|---|---|
| 機能ID | `LABP-CRUD-001` |
| Màn hình | CLS → Đối tác (labrad/partners) |
| Route FE | `/labrad/partners` |
| API base | `/api/v1/lab-partners` |
| Bảng DB | `diab_his_int_lab_partners` (PK `id` CHAR(36)) · UNIQUE `(tenant_id, code)` |
| Permission | Xem `lab_partner.read` · Ghi `lab_partner.write` · Admin (xóa/credential) `lab_partner.admin` |
| Đặc thù | Là **kết nối tích hợp** (endpoint+auth+transport); api_key/bearer **mã hóa AES**; Create ép `status=INACTIVE`; credential sửa qua endpoint riêng; **KHÔNG có** field loại LAB/RAD & giá/hợp đồng |

## 1. Field matrix (3 tầng)

| Field | Nhãn | Control | FE rule | BE validator | DB: type/null/default | GAP |
|---|---|---|---|---|---|---|
| code | Mã đối tác | Input (chỉ khi Tạo) | ✅ min1, **max20** | KHÔNG validator; ko sửa khi Update | VARCHAR(**50**) NOT NULL, UNIQUE | ⚠️ FE max20 < DB50 |
| name | Tên | Input | ✅ min1, ko maxLen | KHÔNG | VARCHAR(255) NOT NULL | FE ko chặn 255 |
| endpoint_url | URL endpoint | Input | ✅ `.url()`, ko maxLen | KHÔNG | VARCHAR(500) **NOT NULL** | BE ko chặn null → lỗi DB |
| transport | Giao thức | Select REST/HL7_MLLP | ✅ enum | KHÔNG enum-check | VARCHAR(20) NOT NULL DEF REST | enum chỉ FE |
| auth_type | Kiểu xác thực | Select NONE/API_KEY/BEARER | ✅ enum | KHÔNG | VARCHAR(30) NOT NULL DEF API_KEY | |
| api_key | API Key | Input password (khi API_KEY) | optional | mã hóa AES → `api_key_encrypted` + masked | BLOB NULL + `api_key_masked` VARCHAR(100) | |
| bearer_token | Bearer Token | Input password (khi BEARER) | optional | mã hóa AES | `bearer_token_encrypted` BLOB NULL | |
| contact_email | Email LH | Input email | optional `.email()` | KHÔNG | VARCHAR(255) NULL | |
| contact_phone | ĐT LH | Input | optional | KHÔNG | VARCHAR(20) NULL | ko chặn maxLen |
| supported_tests | DS xét nghiệm | (ko trên form) | — | `List<string>?` → JSON | `supported_tests` JSON NULL | ⚠️ chỉ set qua API |
| status | Trạng thái | (ko trên form) | — | Create ép INACTIVE | VARCHAR(20) NOT NULL DEF INACTIVE | sửa qua Update |

**Bắt buộc (FE):** `code`, `name`, `endpoint_url`(URL hợp lệ), `transport`, `auth_type`.

## 2. Test cases

### A/B — Load & nhập
| No | 観点 | 操作 | 期待結果 | 判定 |
|---|---|---|---|---|
| A01 | List | Mở `/labrad/partners` | Bảng đối tác; cột Mã/Tên/Transport/Auth/Status | |
| A02 | Form default | Mở Tạo | transport=REST, auth_type=API_KEY; ô api_key hiện | |
| A03 | Ẩn/hiện credential | auth=BEARER | Ẩn api_key, hiện bearer_token; auth=NONE ẩn cả 2 | |
| B01 | name tiếng Việt | `Phòng XN Medlatec` | Đủ dấu | |

### C — Validate
| No | 観点 | データ | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| C01 | code bắt buộc | rỗng | Chặn | | |
| C02 | code biên FE | 20 / 21 ký tự | 20 OK, 21 FE chặn (max20) | | ⚠️ DB cho tới 50 → Defect#1 |
| C03 | name bắt buộc | rỗng | Chặn | | |
| C04 | endpoint_url sai | `abc` (ko phải URL) | Chặn (`.url()`) | | |
| C05 | endpoint_url hợp lệ | `https://lab.vn/api` | Chấp nhận | | |
| C06 | contact_email sai | `x@` | Chặn | | |
| C07 | transport/auth lạ | ép API | **Kỳ vọng chặn** | | ⚠️ **Defect#2** ko validator BE → lọt |
| C08 | **API bỏ FE** | POST code rỗng / url null | **Kỳ vọng 400**; thực tế: url null → **lỗi DB 500** | | ⚠️ **Defect#2** |
| C09 | name > 255 | 256 ký tự | Kỳ vọng chặn | | ⚠️ FE ko maxLen → 500 DB |

### D/E — Business + DB + security
| No | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| D01 | Mã trùng | Unique | Tạo trùng code | ⚠️ **Kỳ vọng 409**; thực tế `DbUpdateException` **500** (BE ko check-then-insert) | | **Defect#4** |
| E01 | Insert | Ghi DB | Tạo hợp lệ | 1 row `diab_his_int_lab_partners`; HTTP 201 | | |
| E02 | status ép INACTIVE | Business | POST kèm status=ACTIVE | DB vẫn **INACTIVE** | | Defect thiết kế |
| E03 | **Mã hóa credential** | Security | Nhập api_key → xem DB | `api_key_encrypted` là BLOB mã hóa; `api_key_masked` che; **ko lưu plaintext** | | |
| E04 | tenant_id/audit | Multi-tenant | Sau E01 | tenant từ JWT; audit set | | |

### F/G/H/I — Load sau, sửa, credential, quyền
| No | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| F01 | List sau tạo | reload | Đối tác mới, status INACTIVE, ko lộ key | | |
| F02 | Round-trip | Sửa | name/endpoint/transport đúng; **code ko sửa** | | |
| G01 | Update kích hoạt | Business | PUT status=ACTIVE | status→ACTIVE | | |
| G02 | Sửa credential riêng | Security | PUT `/{id}/credentials` | Cập nhật key (perm `lab_partner.admin`); form Update thường **ko** đổi credential | | |
| G03 | Rotate key | Security | POST `/credentials/rotate` | Key mới; perm admin | | |
| G04 | Test connection | Integration | POST `/{id}/test-connection` | Trả kết quả kết nối; perm `write` | | |
| H01 | Xóa mềm | Soft delete | DELETE (perm `lab_partner.admin`) | `deleted_at` set; 204 | | |
| I01 | Thiếu quyền write | Authz | POST ko `lab_partner.write` | 403 | | |
| I02 | Xóa thiếu admin | Authz | DELETE với chỉ `write` | 403 | | |
| I03 | Cách ly tenant | Multi-tenant | GET đối tác tenant khác | 404 | | |

## 3. Defect candidates
| ID | Mức | Mô tả | Vị trí |
|---|---|---|---|
| #1 | TB | `code` FE max20 < DB VARCHAR(50) — lệch giới hạn | Form:19 vs 9006b |
| #2 | **Cao** | Không có validator BE → API bỏ mọi ràng buộc; `endpoint_url` null → lỗi DB thay vì 400 | `LabPartnerHandlers.cs` |
| #3 | **Cao** | `name`/`contact_phone` FE ko maxLen → vượt DB 255/20 → 500 | Form |
| #4 | **Cao** | Trùng `(tenant_id,code)` → `DbUpdateException` 500 (ko check-then-insert, ko message VN) | Handlers |
| #5 | TB | ⚠️ **Schema 3 phiên bản** (0007 PK INT / 0033 `cli_lab_partners` / 9006b bản EF). Môi trường test apply nhầm 0007 → EF vỡ runtime. Xác nhận bảng thật = `diab_his_int_lab_partners` (9006b+9012) | migrations |
| #6 | Thấp | `supported_tests` chỉ set qua API (ko có control FE) | |
| #7 | Thấp | Không enum-check transport/auth/status ở BE/DB (VARCHAR tự do) | |

## 4. 実施サマリ
| Nhóm | Case | OK | NG | 保留 |
|---|---|---|---|---|
| A/B | 4 | | | |
| C Validate | 9 | | | |
| D/E | 5 | | | |
| F/G/H/I | 11 | | | |
| **TỔNG** | **29** | | | |
