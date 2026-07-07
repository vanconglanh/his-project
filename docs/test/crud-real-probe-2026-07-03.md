# Kiểm chứng CRUD THẬT (API-level, có assert) — 03/07/2026

> Chạy trực tiếp vào API deploy `https://his.diab.com.vn`, **assert đúng HTTP 2xx + có `id`**, lưu response làm evidence.
> Khác hẳn `e2e/crud-actions.spec.ts` (ghi PASS ngay khi bấm nút, KHÔNG kiểm tra API → giấu lỗi 500).
> Evidence chi tiết: `crud-evidence.txt` (trên VM `/opt/prodiab-his/`).

## Kết quả: **PASS 20 / FAIL 17**

### ✅ Module CHẠY THẬT (đã assert)
| Module | Create | Read | Update | Delete |
|---|---|---|---|---|
| Supplier | ✅ 201 | ✅ | ✅ | ✅ 204 |
| Role | ✅ 201 | ✅ | ✅ | ✅ 204 |
| Patient | ✅ 201 | ✅ | ✅ | ✅ 204 |
| Encounter | ✅ 201 | ✅ | — | — |
| Prescription | ✅ 201 | — | — | ✅ 204 |
| (READ list mọi module) | | ✅ 200 | | |

> Patient chạy được **sau khi fix 2 bug hôm nay**: thiếu `Encryption__MasterKey` + bộ sinh mã tái dùng mã đã xoá (IgnoreQueryFilters).

### ❌ Module HỎNG (500 — do thiếu bảng/cột trên DB deploy)
| Module | Op | HTTP | Root cause (từ log backend) |
|---|---|---|---|
| **Room** | C/R/U/D | 500 | `Table 'prodiab_his.his_rooms' doesn't exist` |
| **Warehouse** | Create | 500 | `Table 'prodiab_his.pha_warehouses' doesn't exist` |
| **Drug** | Create | 500 | `Unknown column 'd.DeletedAt'` (query sai tên cột, phải `deleted_at`) |
| **ApiPartner** | Create/Read | 500 | `Unknown column 'api_key_prefix' in 'field list'` |
| **Service** | Create | 500 | (schema — điều tra tiếp) |
| **Prescription** | Add item | 500 | (điều tra tiếp) |
| **User** | Invite | 422 | validation/business (role code / email) — không phải crash |

## Kết luận
- **KHÔNG phải "CRUD chạy toàn bộ".** Khoảng **một nửa lệnh Create bị 500** do **DB deploy thiếu bảng/cột** (`his_rooms`, `pha_warehouses`, cột `api_key_prefix`, `DeletedAt`…) — dump+migration chưa dựng đủ, hoặc code trỏ tên bảng/cột legacy không còn.
- Con số E2E "13/13 CRUD PASS" trước đây là **ảo** (spec không assert) — đã che toàn bộ các lỗi trên.

## Cần fix (theo nhóm)
1. **Thiếu bảng**: `his_rooms`, `pha_warehouses` → migration tạo bảng (hoặc sửa handler trỏ đúng bảng mới `diab_his_sys_rooms`…).
2. **Thiếu/sai cột**: `api_key_prefix` (ApiPartner), `d.DeletedAt` (Drug query) → migration thêm cột / sửa query.
3. **Service create, Prescription add-item**: điều tra exception cụ thể.

---

# ✅ SAU KHI FIX (03/07/2026) — **PASS 37 / FAIL 0**

Re-probe sau khi vá: **toàn bộ CRUD xanh**. Evidence: `crud-evidence-2026-07-03.txt` (response body thật từng op).

| Module | C | R | U | D | Ghi chú fix |
|---|---|---|---|---|---|
| Supplier | ✅ | ✅ | ✅ | ✅ | (đã chạy sẵn) |
| Role | ✅ | ✅ | ✅ | ✅ | (đã chạy sẵn) |
| Patient | ✅ | ✅ | ✅ | ✅ | Encryption key + code-gen (IgnoreQueryFilters) |
| **Room** | ✅ | ✅ | ✅ | ✅ | Code: `his_rooms`→`diab_his_sys_rooms`, `code`/`capacity` |
| **Warehouse** | ✅ | ✅ | — | — | Migration `9026` tạo `pha_warehouses` + handler đọc thật |
| **ApiPartner** | ✅ | ✅ | — | ✅ | Migration `9027` tạo lại bảng (id BINARY(16) + cột thiếu) |
| **Service** | ✅ | ✅ | ✅ | ✅ | EF `HasColumnName` (BillingConfiguration) + `Convert.ToBoolean` |
| **Drug** | ✅ | ✅ | ✅ | ✅ | `Convert.ToBoolean` (bool==int) + migration `9029` (`name` nullable) |
| **Encounter** | ✅ | ✅ | — | — | (đã chạy sẵn) |
| **Prescription** | ✅ | +item ✅ | — | ✅ | Migration `9028` (`route` ENUM→VARCHAR) |
| **User** | invite ✅ | ✅ | — | ✅ | (endpoint đúng; probe cũ dùng sai role code) |

**Fix áp dụng:** code handlers (Room, Warehouse, Service EF, Drug/Service bool); migrations `9026`–`9029`; verify local build 0 error + backend rebuild + re-probe **37/37**.
