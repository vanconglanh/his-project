# 単体テスト仕様書 (UTC) — Màn hình **Danh mục thuốc** (Drug)

> Quy ước & catalog: xem [utc-00-quy-uoc-chuan-nhat.md](utc-00-quy-uoc-chuan-nhat.md). Bám code thật: FE `DrugForm.tsx` · BE `DrugHandlers.cs`/`DrugDtos.cs` · DB `9005`/`9010`/`9029`.

| Mục | Nội dung |
|---|---|
| 機能ID | `DRUG-CRUD-001` |
| Màn hình | Dược → Danh mục thuốc |
| Route FE | `/drugs` |
| API base | `/api/v1/drugs` |
| Bảng DB | `diab_his_pha_drugs` (PK `id` CHAR(36)) · UNIQUE `(tenant_id, code)` |
| Permission | Xem `drug.read` · Ghi `drug.write` · Import `drug.import` |
| Đặc thù | Create check trùng code → 409 `DRUG_CODE_EXISTS`; Update **không** sửa được code; có Import Excel |

## 1. Field matrix (3 tầng)

| Field | Nhãn | Control | FE required/rule | BE DTO | DB: type/null/default | GAP |
|---|---|---|---|---|---|---|
| code | Mã thuốc | Input text | ✅ min1, **ko maxLen** | `string` non-null | `code` VARCHAR(50) NOT NULL, UNIQUE | FE ko chặn 50 |
| name_vi | Tên thuốc (VN) | Input text | ✅ min1, ko maxLen | `string` non-null | `name_vi` VARCHAR(255) NULL | DB null nhưng FE/DTO bắt buộc |
| name (legacy) | — | (ko trên form) | — | — | `name` VARCHAR(255) NULL (sau 9029) | 9029 cho null để INSERT ko set `name` |
| name_en | Tên (EN) | Input text | optional | `string?` | `name_en` VARCHAR(255) NULL | |
| generic_name | Hoạt chất | Input text | optional | `string?` | VARCHAR(255) NULL | |
| atc_code | Mã ATC | Input text | optional | `string?` | VARCHAR(20) NULL | FE ko chặn 20 |
| strength | Hàm lượng | Input text | optional | `string?` | VARCHAR(100) NULL | |
| unit | Đơn vị | Input text | ✅ min1, ko maxLen | `string` non-null | `unit` VARCHAR(20) NOT NULL | FE ko chặn 20 |
| form | Dạng bào chế | Select 11 giá trị, default TABLET | ✅ enum | `string` non-null | `form` VARCHAR(100) NULL | DB null; enum chỉ FE |
| manufacturer | NSX | Input text | optional | `string?` | VARCHAR(255) NULL | |
| country | Nước SX | Input text | optional | `string?` | VARCHAR(100) NULL | |
| price | Giá bán | Input number, min0 | optional (coerce number) | `decimal?` | `price` DECIMAL(18,2) NULL | min0 chỉ HTML |
| requires_prescription | Kê đơn | Switch, default **true** | optional | `bool` | TINYINT(1) NOT NULL **DEFAULT 0** | ⚠️ default FE(true) ≠ DB(0) |
| is_psychotropic | Hướng thần | Switch, default false | optional | `bool` | TINYINT(1) NOT NULL DEF 0 | |
| is_narcotic | Gây nghiện | Switch, default false | optional | `bool` | TINYINT(1) NOT NULL DEF 0 | |
| dtqg_drug_code | Mã ĐTQG | Input text | optional | `string?` | VARCHAR(50) NULL | |
| category_id | Nhóm thuốc | (ko render) | — | `int?` | `category_id` **CHAR(36)** NULL | ⚠️ DTO int? vs DB CHAR(36) |
| status | Trạng thái | (ko control; default ACTIVE) | enum optional | `string` non-null | VARCHAR(20) NOT NULL DEF 'ACTIVE' | ko control trên form |

**Bắt buộc (FE zod):** `code`, `name_vi`, `unit`, `form`. **DB NOT NULL:** `code`, `unit`.

## 2. Test cases

### A — Load ban đầu
| No | 観点 | 操作 | 期待結果 | 判定 |
|---|---|---|---|---|
| A01 | List render | Mở `/drugs` | Bảng + nút "Tạo thuốc" + ô tìm + nút Import | |
| A02 | Form default | Mở form Tạo | form=TABLET; 3 switch: requires_prescription **bật**, 2 cái tắt; status ẩn | |
| A03 | Dropdown form | Mở select Dạng bào chế | Đủ 11 option nhãn tiếng Việt | |

### B — Nhập/hiển thị
| No | 観点 | データ | 期待結果 | 判定 |
|---|---|---|---|---|
| B01 | name_vi tiếng Việt | `Paracetamol 500mg (Hộp 10 vỉ)` | Hiển thị đủ dấu | |
| B02 | price số | `1200` | Nhận số, ko cho chữ | |
| B03 | Switch kê đơn | toggle | Đổi trạng thái đúng | |

### C — Validate
| No | 中項目 | 観点 | データ | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|---|
| C01 | code bắt buộc | Mandatory | rỗng | Chặn, "Bắt buộc" | | |
| C02 | name_vi bắt buộc | Mandatory | rỗng | Chặn | | |
| C03 | unit bắt buộc | Mandatory | rỗng | Chặn | | |
| C04 | form bắt buộc | Enum | ko chọn | Chặn | | |
| C05 | code 50/51 | 境界値 | 50/51 ký tự | 50 OK; 51 **kỳ vọng chặn** | | ⚠️ Defect#2 (lỗi DB) |
| C06 | unit 20/21 | 境界値 | 20/21 ký tự | 21 kỳ vọng chặn | | ⚠️ Defect#2 |
| C07 | atc_code 21 | 境界値 | 21 ký tự | Kỳ vọng chặn | | ⚠️ Defect#2 |
| C08 | price âm | Kiểu số | `-5` | Kỳ vọng chặn (≥0) | | ⚠️ zod ko `.min(0)`, chỉ HTML min |
| C09 | price chữ | Kiểu số | `abc` | Ko nhập được / báo lỗi | | |
| C10 | **API bỏ FE** | Ko validator BE | POST `{code:"",name_vi:"",unit:""}` | **Kỳ vọng 422**; thực tế lưu/DB chặn NULL thôi | | ⚠️ **Defect#1** |

### D/E — Business + DB
| No | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| D01 | Mã trùng | Unique | Tạo trùng code | 409 `DRUG_CODE_EXISTS`, message VN | | |
| D02 | Mã trùng khác tenant | Cách ly | Cùng code, tenant khác | Thành công | | |
| E01 | Insert | Ghi DB | Tạo hợp lệ | 1 row `diab_his_pha_drugs`; HTTP 201 | | |
| E02 | requires_prescription default | Ghi đúng default | Tạo qua **API ko gửi field** | ⚠️ DB lưu **0** dù UI mặc định true | | **Defect#5** |
| E03 | category_id | Kiểu | Gửi category_id qua API | ⚠️ mismatch int? vs CHAR(36) | | **Defect#3** |
| E04 | tenant_id/audit | Multi-tenant | Sau E01 | tenant_id từ JWT; created_* set | | |
| E05 | name legacy | Ko lỗi INSERT | Tạo thuốc | `name`(legacy)=NULL, ko 500 (nhờ 9029) | | |

### F — Load sau insert
| No | 観点 | 操作 | 期待結果 | 判定 |
|---|---|---|---|---|
| F01 | List | reload | Thuốc mới xuất hiện, đủ cột (giá format VND) | |
| F02 | Round-trip | mở Sửa | Field đúng; **code ko sửa được** (disabled/ko gửi) | |
| F03 | Search | `/drugs/search` | Tìm theo mã/tên/hoạt chất | |

### G/H — Sửa/Xóa
| No | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| G01 | Update field | Sửa giá/tên → PUT | DB cập nhật | | |
| G02 | Update ko sửa code | Business | Đổi code khi Update | Code KHÔNG đổi (handler ko update code) | | **Defect#8** ko nhất quán Create |
| H01 | Xóa mềm | Soft delete | DELETE | `deleted_at` set; HTTP 204 | | |
| H02 | Ẩn sau xóa | Filter | reload | Ko hiển thị | | |

### I/J — Quyền, bảo mật
| No | 観点 | 操作 | 期待結果 | 判定 |
|---|---|---|---|---|
| I01 | Thiếu quyền | Authz | POST ko `drug.write` | 403 | |
| I02 | Cách ly tenant | Multi-tenant | GET thuốc tenant khác | 404 | |
| J01 | Encoding | tiếng Việt | name_vi có dấu | Lưu/hiển thị đúng | |
| K01 | Import Excel | Bulk | Import file thuốc | Số dòng OK/lỗi báo rõ; perm `drug.import` | |

## 3. Defect candidates
| ID | Mức | Mô tả | Vị trí |
|---|---|---|---|
| #1 | **Cao** | Không có validator BE → API thẳng bỏ mọi ràng buộc; chỉ DB NOT NULL chặn | `DrugHandlers.cs` |
| #2 | **Cao** | FE/BE ko chặn maxLength (code50, unit20, atc20…) → lỗi/truncate DB | FE `DrugForm.tsx` |
| #3 | **Cao** | `category_id` DTO `int?` vs DB `CHAR(36)` — lệch kiểu | `DrugDtos.cs` vs 9010:35 |
| #4 | TB | `name_vi` FE/DTO bắt buộc nhưng DB cho NULL | 9010:29 |
| #5 | TB | `requires_prescription` default lệch FE(true)/DB(0) | 9010:36 |
| #6 | Thấp | `status` không có control trên form | DrugForm |
| #7 | Thấp | Nhiều cột DB cũ ko dùng (drug_form, sell_price, requires_rx…) dễ nhầm khi assert DB | 9005 |

## 4. 実施サマリ
| Nhóm | Case | OK | NG | 保留 |
|---|---|---|---|---|
| A/B | 6 | | | |
| C Validate | 10 | | | |
| D/E | 8 | | | |
| F | 3 | | | |
| G/H | 4 | | | |
| I/J/K | 4 | | | |
| **TỔNG** | **35** | | | |
