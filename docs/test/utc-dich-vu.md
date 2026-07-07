# 単体テスト仕様書 (UTC) — Màn hình **Dịch vụ** (Service Catalog)

> Quy ước & catalog: xem [utc-00-quy-uoc-chuan-nhat.md](utc-00-quy-uoc-chuan-nhat.md). Bám code thật: FE `ServiceForm.tsx` · BE `ServiceCatalogHandlers.cs` + `ServiceUpsertRequestValidator` · EF `BillingConfiguration.cs` · DB `0040_service_catalog.sql`.

| Mục | Nội dung |
|---|---|
| 機能ID | `SVC-CRUD-001` |
| Màn hình | Danh mục → Dịch vụ |
| Route FE | `/services` |
| API base | `/api/v1/services` |
| Bảng DB | `diab_his_bil_services` (PK `id` CHAR(36)) · UNIQUE `(tenant_id, code)` |
| Permission | Xem `service.read` · Ghi `service.write` (⚠️ `/categories` chỉ `[Authorize]`, ko permission) |
| Đặc thù | Create trùng mã → 409 `SERVICE_CODE_EXISTS`; **có validator BE nhưng KHÔNG chạy** (thiếu ValidationBehavior) |

## 1. Field matrix (3 tầng)

| Field (FE id) | Nhãn | Control | FE required/rule | BE validator | DB: type/null/default | GAP |
|---|---|---|---|---|---|---|
| svc_code | Mã DV | Input text | ✅ min1, **ko maxLen** | `NotEmpty().MaximumLength(50)` *(dead)* | VARCHAR(50) NOT NULL, UNIQUE | FE ko chặn 50; validator ko chạy |
| svc_name | Tên DV | Input text | ✅ min1, ko maxLen | `NotEmpty().MaximumLength(255)` *(dead)* | VARCHAR(255) NOT NULL | |
| category | Nhóm | Select 6 (CONSULTATION/PROCEDURE/LAB/RAD/PHARMACY/OTHER), default CONSULTATION | ✅ enum | `Must in [6]` *(dead)* | VARCHAR(20) NOT NULL | DB ko CHECK |
| svc_price | Giá | Input number, min0 step1000 | ✅ ≥0, default 0 | `≥0` *(dead)* | DECIMAL(15,2) NOT NULL DEF 0 | khớp 3 tầng |
| vat_rate | VAT % | Select [0,5,8,10], default 0 | ✅ ∈{0,5,8,10} | `Must in [0,5,8,10]` *(dead)* | TINYINT NOT NULL DEF 0 | |
| bhyt_code | Mã BHYT | Input text | optional, ko maxLen | — | VARCHAR(50) NULL | |
| bhyt_max_amount | Mức BHYT tối đa | Input number min0 | optional | — (ko chặn âm) | DECIMAL(15,2) NULL | ⚠️ zod ko `.min(0)` |
| is_active | Kích hoạt | Switch, default true | boolean | — | TINYINT(1) NOT NULL DEF 1 | |

**Bắt buộc:** `svc_code`, `svc_name`, `category`. Còn lại có default/optional.

## 2. Test cases

### A — Load ban đầu
| No | 観点 | 操作 | 期待結果 | 判定 |
|---|---|---|---|---|
| A01 | List render | Mở `/services` | Bảng + nút "Tạo dịch vụ" + ô tìm | |
| A02 | Form default | Mở form | category=CONSULTATION, price=0, vat=0, is_active=bật | |
| A03 | Dropdown | Mở category & vat | category 6 option, vat [0,5,8,10] — nhãn tiếng Việt | |

### B — Nhập/hiển thị
| No | 観点 | データ | 期待結果 | 判定 |
|---|---|---|---|---|
| B01 | svc_name tiếng Việt | `Khám nội tổng quát` | Đủ dấu | |
| B02 | price | `150000` | Nhận số, step 1000 | |
| B03 | Chọn VAT | 10 | Set đúng | |

### C — Validate
| No | 観点 | データ | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| C01 | svc_code bắt buộc | rỗng | Chặn | | |
| C02 | svc_name bắt buộc | rỗng | Chặn | | |
| C03 | price âm | `-1000` | Chặn (≥0) | | zod refine |
| C04 | vat lạ | 7 (ép API) | Kỳ vọng chặn | | ⚠️ Defect#1 (validator dead) |
| C05 | code 50/51 | 50/51 ký tự | 51 kỳ vọng chặn | | ⚠️ **Defect#2** FE ko maxLen + validator dead → lỗi DB |
| C06 | name 255/256 | 255/256 | 256 kỳ vọng chặn | | ⚠️ Defect#2 |
| C07 | bhyt_max_amount âm | `-100` | **Kỳ vọng chặn** | | ⚠️ **Defect#3** ko chặn → lưu số âm |
| C08 | **API bỏ FE** | POST code 60 ký tự / category lạ | **Kỳ vọng 400 từ validator**; thực tế **KHÔNG bị chặn** (validator dead) | | ⚠️ **Defect#1** |

### D/E — Business + DB
| No | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| D01 | Mã trùng | Create | trùng code | 409 `SERVICE_CODE_EXISTS` | | |
| D02 | Update đổi code trùng | Business | PUT code trùng DV khác | ⚠️ Lỗi DB thô (ko 409 sạch) | | **Defect#5** |
| E01 | Insert | Ghi DB | Tạo hợp lệ | 1 row `diab_his_bil_services`; HTTP 201 | | |
| E02 | Map cột (EF HasColumnName) | Ánh xạ | Sau E01 | code/name/category/price/vat_rate/is_active đúng cột | | |
| E03 | tenant_id/audit | Multi-tenant | Sau E01 | tenant từ JWT; created_* set; `deleted_by` ignore EF | | |

### F — Load sau insert
| No | 観点 | 操作 | 期待結果 | 判定 |
|---|---|---|---|---|
| F01 | List | reload | DV mới xuất hiện, giá format VND, badge nhóm | |
| F02 | Round-trip | Sửa | Field đúng | |
| F03 | Search | `/services/search` | Tìm ra DV mới | |

### G/H/I/J
| No | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| G01 | Update | Sửa giá → PUT | DB cập nhật | | |
| H01 | Xóa mềm | DELETE | `deleted_at` set; 204 | | |
| H02 | Ẩn sau xóa | reload | Ko hiển thị | | |
| I01 | Thiếu quyền | POST ko `service.write` | 403 | | |
| I02 | `/categories` ko cần perm | Authz | GET categories user thường | Trả 200 (⚠️ khác các read khác) | | **Defect#6** |
| I03 | Cách ly tenant | GET DV tenant khác | 404 | | |
| J01 | Encoding | tên có dấu | Lưu/hiển thị đúng | | |

## 3. Defect candidates
| ID | Mức | Mô tả | Vị trí |
|---|---|---|---|
| #1 | **Cao** | Validator BE (`ServiceUpsertRequestValidator`) **là dead-code**: không có `ValidationBehavior`/`IPipelineBehavior` nào → MediatR ko chạy FluentValidation. Mọi rule (max, category, vat, ≥0) vô hiệu; chỉ DB chặn | Thiếu behavior; `DependencyInjection.cs:16` chỉ `AddValidatorsFromAssembly` |
| #2 | **Cao** | FE thiếu maxLength code(50)/name(255); do #1 → vượt độ dài chỉ vỡ ở DB | `ServiceForm.tsx` |
| #3 | TB | `bhyt_max_amount` cho phép số âm (zod ko `.min(0)`, BE ko rule, DB ko CHECK) | schema:36 |
| #4 | TB | category/vat ko có CHECK ở DB → API thẳng nhét giá trị lạ (do #1) | 0040 |
| #5 | TB | Update ko kiểm trùng code → lỗi DB thô thay vì 409 | `UpdateServiceHandler` |
| #6 | Thấp | `GET /categories` thiếu `service.read` (chỉ `[Authorize]`) | `ServicesController.cs:60` |

## 4. 実施サマリ
| Nhóm | Case | OK | NG | 保留 |
|---|---|---|---|---|
| A/B | 6 | | | |
| C Validate | 8 | | | |
| D/E | 5 | | | |
| F | 3 | | | |
| G/H/I/J | 8 | | | |
| **TỔNG** | **30** | | | |
