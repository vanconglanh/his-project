# 単体テスト仕様書 (UTC) — Màn hình **API Partner** (Đối tác API)

> Quy ước & catalog: xem [utc-00-quy-uoc-chuan-nhat.md](utc-00-quy-uoc-chuan-nhat.md). Bám code thật: FE `ApiPartnerForm.tsx` · BE `ApiPartnerHandlers.cs`/`PublicApiDtos.cs` · DB `9027_recreate_api_partners.sql`.

| Mục | Nội dung |
|---|---|
| 機能ID | `APIP-CRUD-001` |
| Màn hình | Quản trị → API Partner |
| Route FE | `/admin/api-partners` |
| API base | `/api/v1/api-partners` |
| Bảng DB | `diab_his_api_partners` (PK `id` **BINARY(16)**) · không UNIQUE ngoài PK |
| Permission | Xem `api_partner.read` · Ghi `api_partner.write` · Regenerate/Test `api_partner.admin` |
| Đặc thù | Server sinh API key `pdh_live_...`, lưu **SHA256 hash** (ko lưu plain); prefix `pdh_live_****XXXX`; Create ép `status=ACTIVE` |

## 1. Field matrix (3 tầng)

| Field (FE id) | Nhãn | Control | FE rule | BE DTO/default | DB: type/null/default | GAP |
|---|---|---|---|---|---|---|
| name | Tên đối tác | Input text | ✅ min2, **ko maxLen** | `string` non-null | `name` VARCHAR(255) NOT NULL | FE ko chặn 255 |
| contact_email | Email liên hệ | Input email | optional, `.email().or("")` | `string?` | `contact_email` VARCHAR(255) NULL | BE ko validate email |
| scopes | Phạm vi | Checkbox `scope-{scope}` (6 giá trị) | ✅ array.min1 | `List<string>` | `scopes` JSON NULL | BE ko validate scope hợp lệ |
| rate_limit_per_min | Giới hạn/phút | Input number `valueAsNumber` | ✅ int 1..10000 | `int=60` | INT NOT NULL DEF 60 | max chỉ FE |
| daily_quota | Hạn mức/ngày | Input number | ✅ int 1..10_000_000 | `int=10000` | INT NOT NULL DEF 10000 | max chỉ FE |
| expires_at | Hết hạn | Input date | optional | `DateTime?` | DATETIME NULL | ko ràng buộc tương lai |
| (ip_whitelist) | — | (ko trên form) | — | `List<string>?` | `ip_whitelist` JSON NULL | ⚠️ chỉ set qua API |

Server sinh: `api_key_hash`, `api_key_prefix` VARCHAR(30), `status` DEF ACTIVE, tenant_id, audit.
**Bắt buộc (FE):** `name`, `scopes`(≥1), `rate_limit_per_min`, `daily_quota`. **DB NOT NULL người dùng ảnh hưởng:** `name`.

## 2. Test cases

### A — Load ban đầu
| No | 観点 | 操作 | 期待結果 | 判定 |
|---|---|---|---|---|
| A01 | List | Mở `/admin/api-partners` | Bảng partner; cột Tên/Prefix key/Status/rate | |
| A02 | Form default | Mở form Tạo | rate=60, quota=10000, scopes rỗng | |
| A03 | Scope checkbox | Hiển thị 6 scope | public.patient.read/write, appointment.read/write, catalog.read, visit.lookup | |

### B — Nhập/hiển thị
| No | 観点 | データ | 期待結果 | 判定 |
|---|---|---|---|---|
| B01 | name | `Website phòng khám A` | Nhận đủ dấu | |
| B02 | rate/quota số | 60 / 10000 | Kiểu number | |
| B03 | Tick scope | chọn 2 scope | State cập nhật | |

### C — Validate
| No | 観点 | データ | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| C01 | name min2 | `A` | Chặn | | |
| C02 | scopes rỗng | 0 scope | Chặn (min1) | | |
| C03 | rate biên | 0 / 1 / 10000 / 10001 | 0→NG, 1→OK, 10000→OK, 10001→NG | | FE zod |
| C04 | quota biên | 0 / 1 / 10_000_000 / 10_000_001 | tương tự | | |
| C05 | email sai | `abc@` | FE chặn | | |
| C06 | rate/quota chữ | `abc` | Ko nhập/chặn | | |
| C07 | **API bỏ FE** | POST rate=0, quota=99_000_000, scope lạ, email sai | **Kỳ vọng 400**; thực tế **được chấp nhận** | | ⚠️ **Defect#1** ko validator BE |

### D/E — Business + DB + key
| No | 観点 | 操作 | 期待結果 | 判定 |
|---|---|---|---|---|
| E01 | Insert | Ghi DB | Tạo hợp lệ | 1 row `diab_his_api_partners`; HTTP 201 | |
| E02 | **API key sinh 1 lần** | Security | Sau E01 | Response trả `api_key_plain` **1 lần duy nhất**; DB chỉ lưu `api_key_hash` (SHA256) — **ko** lưu plain | |
| E03 | Prefix che | Masked | Sau E01 | `api_key_prefix` = `pdh_live_****` + 4 ký tự cuối | |
| E04 | status ép ACTIVE | Business | POST kèm status=DISABLED | DB vẫn `ACTIVE` (client bị bỏ qua) | |
| E05 | tenant_id/audit | Multi-tenant | Sau E01 | tenant từ JWT; created_by set | |
| E06 | id BINARY(16) | PK | Sau E01 | id lưu BINARY(16), API trả GUID chuỗi | |

### F — Load sau insert
| No | 観点 | 操作 | 期待結果 | 判定 |
|---|---|---|---|---|
| F01 | List | reload | Partner mới; hiển thị prefix che, ko lộ key | |
| F02 | Round-trip | mở Sửa | name/scope/rate/quota đúng | |

### G/H/I — Update, key, quyền
| No | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| G01 | Update partial | PUT đổi rate | Chỉ field gửi thay đổi | | |
| G02 | Regenerate key | Security | POST `/{id}/regenerate-key` | Key mới (prefix đổi); hash cập nhật; giữ scope/limit; **perm `api_partner.admin`** | | |
| H01 | Xóa mềm | Soft delete | DELETE | `deleted_at` set + `status='DISABLED'`; 204 | | |
| I01 | Thiếu quyền write | Authz | POST ko `api_partner.write` | 403 | | |
| I02 | Regenerate thiếu admin | Authz | POST regenerate với chỉ `write` | 403 | | |
| I03 | Cách ly tenant | Multi-tenant | GET partner tenant khác | 404 | | |

## 3. Defect candidates
| ID | Mức | Mô tả | Vị trí |
|---|---|---|---|
| #1 | **Cao** | Không có validator BE → API thẳng bỏ min/max rate&quota, scope hợp lệ, email format | `ApiPartnerHandlers.cs` |
| #2 | TB | rate/quota max chỉ ở FE; DB INT nhận tới ~2.1 tỷ | zod vs DB |
| #3 | TB | scope không validate server → nhét scope lạ/rỗng vào JSON | — |
| #4 | TB | rate/quota min≥1 chỉ FE; API nhét 0/âm được | — |
| #5 | Thấp | `ip_whitelist` có ở DTO/DB nhưng ko có control FE → chỉ tạo qua API | — |
| #6 | Thấp | `status` Update là string tự do, ko enum-check BE | — |

## 4. 実施サマリ
| Nhóm | Case | OK | NG | 保留 |
|---|---|---|---|---|
| A/B | 6 | | | |
| C Validate | 7 | | | |
| E DB+key | 6 | | | |
| F | 2 | | | |
| G/H/I | 6 | | | |
| **TỔNG** | **27** | | | |
