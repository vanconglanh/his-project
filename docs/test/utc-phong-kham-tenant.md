# 単体テスト仕様書 (UTC) — Màn hình **Phòng khám (Tenant)**

> Quy ước & catalog: xem [utc-00-quy-uoc-chuan-nhat.md](utc-00-quy-uoc-chuan-nhat.md). Bám code thật: FE `TenantForm.tsx` · BE `CreateTenantCommand.cs` (có validator) · DB `0001_create_tenants.sql`.

| Mục | Nội dung |
|---|---|
| 機能ID | `TENANT-CRUD-001` |
| Màn hình | Quản trị → Phòng khám |
| Route FE | `/admin/tenants` |
| API base | `/api/v1/tenants` |
| Bảng DB | `diab_his_sys_tenants` (PK `id` INT AUTO_INCREMENT ⚠️) · UNIQUE `code`, `subdomain` |
| Permission | **`RequireSuperAdmin`** cho toàn bộ (khác `tenant.read/write`) |
| Đặc thù | Tạo tenant **kèm tạo admin user** (Status=Pending + invite token 7 ngày); subdomain unique + format; vòng đời ACTIVE↔SUSPENDED→TERMINATED |

## 1. Field matrix (3 tầng)

| Field | Nhãn | Control | FE rule | BE validator | DB | GAP |
|---|---|---|---|---|---|---|
| code | Mã PK | Input text | `^[A-Z0-9]{3,20}$` | `NotEmpty()` + regex | VARCHAR(20) NOT NULL UNIQUE | khớp |
| subdomain | Subdomain | Input text | `^[a-z0-9-]{3,63}$` | `NotEmpty()` + regex + check trùng | VARCHAR(63) **NULL** UNIQUE | DB null nhưng FE/BE bắt buộc |
| name | Tên PK | Input text | min3, max200 | `NotEmpty().MaximumLength(200)` | VARCHAR(255) NOT NULL | ⚠️ FE min3 vs BE chỉ NotEmpty; BE max200 < DB255 |
| email | Email PK | Input email | `.email()` ✅ | `NotEmpty().EmailAddress()` | VARCHAR(100) **NULL** | DB null nhưng FE/BE bắt buộc |
| phone | Điện thoại | Input text | optional | — | VARCHAR(20) NULL | ko chặn maxLen |
| cskcb_code | Mã CSKCB | Input text | optional | — | VARCHAR(20) NULL | ko validate |
| tax_code | MST | Input text | optional | — | VARCHAR(20) NULL | ko chặn maxLen |
| address | Địa chỉ | Input text | optional | — | TEXT NULL | |
| storage_quota_gb | Hạn mức GB | Input number, default 20 | 1..1000 optional | `InclusiveBetween(1,1000)` | INT NOT NULL DEF 20 | khớp |
| admin_email | Email admin | Input email | `.email()` ✅ | `NotEmpty().EmailAddress()` | → `Users.Email` | tạo admin user |
| admin_full_name | Tên admin | Input text | min2 ✅ | `NotEmpty()` | → `Users.FullName` | |
| status | Trạng thái | (mặc định) | — | set ACTIVE | ENUM(ACTIVE/SUSPENDED/TERMINATED) DEF ACTIVE | |

**Bắt buộc (FE+BE):** `code`, `subdomain`, `name`, `email`, `admin_email`, `admin_full_name`, `storage_quota_gb`.

## 2. Test cases

### A — Load ban đầu
| No | 観点 | 操作 | 期待結果 | 判定 |
|---|---|---|---|---|
| A01 | List | SuperAdmin mở `/admin/tenants` | Bảng phòng khám; cột Mã/Tên/Subdomain/Status/quota | |
| A02 | Form default | Mở Tạo | storage_quota_gb=20; status ẩn (mặc định ACTIVE) | |
| A03 | Quyền | User thường mở | 403 (RequireSuperAdmin) | |

### B — Nhập/hiển thị
| No | 観点 | データ | 期待結果 | 判定 |
|---|---|---|---|---|
| B01 | name tiếng Việt | `Phòng khám Đa khoa An Bình` | Đủ dấu | |
| B02 | subdomain | `pkabc` | Hiển thị preview `pkabc.prodiab.vn` | |

### C — Validate
| No | 観点 | データ | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| C01 | code regex | `ab` / `AB` / `ABC` / `PKX_1` | thường→NG, 2 ký tự→NG, ABC→OK, có `_`→NG (chỉ A-Z0-9) | | |
| C02 | code biên | 20 / 21 ký tự | 20 OK, 21 NG (regex max20) | | |
| C03 | subdomain regex | `PK` (hoa)/`pk` (2)/`pk-abc` | hoa→NG, 2→NG, `pk-abc`→OK | | |
| C04 | subdomain biên | 63 / 64 ký tự | 63 OK, 64 NG | | |
| C05 | name min3 | `AB` | FE chặn (min3) | | ⚠️ **Defect#2** BE chỉ NotEmpty → API nhận "AB" |
| C06 | email PK sai | `abc@` | Chặn (FE+BE) | | |
| C07 | admin_email sai | `x@` | Chặn | | |
| C08 | quota biên | 0 / 1 / 1000 / 1001 | 0→NG, 1→OK, 1000→OK, 1001→NG | | FE+BE InclusiveBetween |
| C09 | cskcb/tax/phone 21 ký tự | 境界値 DB(20) | **Kỳ vọng chặn** | | ⚠️ **Defect#7** FE/BE ko chặn → lỗi DB |

### D/E — Business + DB + tạo admin
| No | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| D01 | code trùng | Unique | Tạo trùng code | `TENANT_CODE_TAKEN` | | |
| D02 | subdomain trùng | Unique | Tạo trùng subdomain | `TENANT_SUBDOMAIN_TAKEN` | | |
| E01 | Insert tenant | Ghi DB | Tạo hợp lệ | 1 row `diab_his_sys_tenants`, status ACTIVE | | |
| E02 | **Tạo admin kèm** | Business | Sau E01 | 1 `User` Status=**Pending**, PasswordHash rỗng, InviteToken (hết hạn +7 ngày); gán role ADMIN | | |
| E03 | ⚠️ admin TenantId=0 | Defect | Xem UserRole | `TenantId=0` (chưa gán tenant thật) | | **Defect#8 (bug tiềm ẩn)** |
| E04 | Gửi email mời | Side-effect | Sau E01 | Gửi link `/accept-invite?token=`; **ko rollback nếu email lỗi** | | **Defect rủi ro** |
| E05 | id kiểu | PK mismatch | Sau E01 | ⚠️ DB `id INT AUTO_INCREMENT` vs entity gán `Guid.NewGuid()` + route `{id:guid}` | | **Defect#1 (nghiêm trọng)** cần kiểm mapping thực tế |

### F — Load sau insert
| No | 観点 | 操作 | 期待結果 | 判定 |
|---|---|---|---|---|
| F01 | List | reload | Tenant mới xuất hiện, subdomain đúng | |
| F02 | Round-trip | Sửa | Field đúng; `expires_at` chỉ có ở form Sửa | |

### G/H/I — Vòng đời & quyền
| No | 観点 | 操作 | 期待結果 | 判定 |
|---|---|---|---|---|
| G01 | Suspend | POST `/{id}/suspend` | status→SUSPENDED | |
| G02 | Activate | POST `/{id}/activate` | status→ACTIVE | |
| H01 | Terminate (soft) | DELETE | `deleted_at` set, status TERMINATED | |
| I01 | Không SuperAdmin | Authz | Mọi API với user thường | 403 | |

## 3. Defect candidates
| ID | Mức | Mô tả | Vị trí |
|---|---|---|---|
| #1 | **Cao** | PK mismatch: DB `id INT AUTO_INCREMENT` nhưng entity `Guid.NewGuid()` + route `{id:guid}` — nguy cơ ID không nhất quán | `0001:12` vs `CreateTenantCommand.cs:82` |
| #2 | TB | `name`: FE min3 vs BE chỉ `NotEmpty` → API nhận 1-2 ký tự | validator |
| #3 | TB | `name` max: FE/BE=200, DB=255 (lệch, BE chặt hơn — ko lỗi nhưng thiếu đồng bộ) | |
| #4 | TB | `email`/`subdomain` DB cho NULL nhưng FE+BE bắt buộc (ko tạo được qua API dù DB cho phép) | |
| #7 | TB | `cskcb_code`/`tax_code`/`phone` ko chặn maxLen FE/BE, DB VARCHAR(20) → lỗi DB khi >20 | |
| #8 | **Cao** | Admin user tạo kèm có `UserRole.TenantId=0` (chưa gán tenant thật) — bug tiềm ẩn phân quyền | `CreateTenantCommand.cs:122` |
| #9 | TB | Gửi email mời sau SaveChanges, **không rollback** nếu lỗi → tenant tạo nhưng admin ko nhận được mời | `:129-131` |

## 4. 実施サマリ
| Nhóm | Case | OK | NG | 保留 |
|---|---|---|---|---|
| A/B | 5 | | | |
| C Validate | 9 | | | |
| D/E | 7 | | | |
| F | 2 | | | |
| G/H/I | 4 | | | |
| **TỔNG** | **27** | | | |
