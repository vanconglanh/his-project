# 単体テスト仕様書 (UTC) — Màn hình **Vai trò & Quyền hạn** (Role / RBAC)

> Quy ước & catalog: xem [utc-00-quy-uoc-chuan-nhat.md](utc-00-quy-uoc-chuan-nhat.md). Bám code thật: FE `roles/page.tsx` + `RoleForm.tsx` + `PermissionMatrix.tsx` · BE `CreateRoleCommand.cs`/`UpdateRoleCommand.cs`/`DeleteRoleCommand.cs` · DB `9001_create_sec_all.sql`.

| Mục | Nội dung |
|---|---|
| 機能ID | `ROLE-CRUD-001` |
| Màn hình | Quản trị → Vai trò & Quyền hạn |
| Route FE | `/admin/roles` |
| API base | `/api/v1/roles` (định danh theo **`{code}`**, không phải id) |
| Bảng DB | `diab_his_sec_roles` (PK `id` CHAR(36), UNIQUE `code`) + nối `diab_his_sec_role_permissions` (role_id, permission_id) |
| Permission (API) | Xem `role.read` · Ghi `role.write` |
| ⚠️ Permission (nút FE) | Nút "Tạo vai trò mới" gate `admin.role_manage` — **khác** permission API |
| Đặc thù | Role hệ thống (`role_type=SYSTEM`) **không sửa/xóa** (403 `ROLE_SYSTEM_PROTECTED`); role tạo mới luôn `CUSTOM` |

## 1. Field matrix (3 tầng)

| Field (FE id) | Nhãn | Control | FE rule | BE validator (Create) | DB | GAP |
|---|---|---|---|---|---|---|
| role-code | Mã vai trò | Input text (chỉ khi Tạo) | regex `^[A-Z][A-Z0-9_]{2,30}$` | `NotEmpty().Matches(^[A-Z][A-Z0-9_]{2,30}$)` | `code` VARCHAR(50) NOT NULL UNIQUE | text FE ghi "3-30" nhưng regex cho 3-31 |
| role-name | Tên vai trò | Input text | min2 | `NotEmpty()` (Update **ko** validator) | `name` VARCHAR(100) NOT NULL | Update chỉ FE chặn |
| role-desc | Mô tả | Input text | optional | — | `description` VARCHAR(500) NULL | |
| perm-{code} | Ma trận quyền | Checkbox nhóm theo resource | `array.min(1)` | `PermissionCodes NotEmpty()`; mỗi code phải tồn tại | bảng nối `role_permissions` | Không maxLength field nào |

**Bắt buộc (Create):** `role-code`, `role-name`, ≥1 permission. `role-desc` optional. **Update:** mọi field optional (chỉ set field gửi lên).

## 2. Test cases

### A — Load ban đầu
| No | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| A01 | List | Mở `/admin/roles` | Bảng vai trò; cột Mã/Tên/Loại(SYSTEM/CUSTOM)/số quyền | | |
| A02 | Nút Tạo | Quyền | User có `admin.role_manage` | Nút "Tạo vai trò mới" hiển thị | | ⚠️ lệch API perm |
| A03 | Row SYSTEM | Bảo vệ | Xem 1 role SYSTEM | Chỉ nút "Sửa quyền" (xem ma trận); ẩn Sửa/Xóa | | |
| A04 | Form Tạo | Control | Bấm Tạo | code/name/desc rỗng; ma trận quyền nhóm theo resource, tất cả bỏ chọn | | |

### B — Nhập/hiển thị
| No | 観点 | データ | 期待結果 | 判定 |
|---|---|---|---|---|
| B01 | Tick quyền | chọn vài permission | State `permission_codes` cập nhật; nhóm theo resource | |
| B02 | Sửa role CUSTOM | Load data | mở Sửa | name/desc/quyền đã lưu load đúng; **code ẩn** (ko sửa) | |

### C — Validate
| No | 観点 | データ | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| C01 | code bắt buộc | rỗng | Chặn | | |
| C02 | code sai regex | `abc` (thường) | Chặn (phải HOA đầu) | | |
| C03 | code có ký tự lạ | `AB-CD` | Chặn (chỉ A-Z0-9_) | | |
| C04 | code biên | `AB`(2)/`ABC`(3)/31 ký tự/32 ký tự | 2→NG, 3→OK, 31→OK, 32→NG | | |
| C05 | name min2 | `A` | Chặn | | |
| C06 | không chọn quyền | 0 permission | Chặn (min1) | | |
| C07 | **API Update bỏ FE** | PUT `{name:"A"}` | **Kỳ vọng chặn**; thực tế lưu (Update ko validator) | | ⚠️ **Defect#2** |
| C08 | permission ko tồn tại | gửi code lạ | 4xx `PERMISSION_NOT_FOUND`, cả thao tác fail | | |

### D/E — Business + DB
| No | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| D01 | Mã trùng | Unique | Tạo trùng code | `ROLE_CODE_TAKEN` | | |
| D02 | Sửa role SYSTEM | Bảo vệ | PUT role SYSTEM | **403** `ROLE_SYSTEM_PROTECTED` | | |
| D03 | Xóa role SYSTEM | Bảo vệ | DELETE role SYSTEM | **403** | | |
| D04 | Role mới = CUSTOM | Business | Tạo role | DB `role_type='CUSTOM'`, `tenant_id`=JWT, is_active=1 | | ko tạo SYSTEM qua API |
| E01 | Insert role + quyền | Ghi DB | Tạo hợp lệ 3 quyền | 1 row `sec_roles` + 3 row `role_permissions`; HTTP 201 | | |
| E02 | Update = replace quyền | Ghi | PUT đổi bộ quyền | Xóa hết quyền cũ, thêm mới đúng | | |

### F — Load sau insert
| No | 観点 | 操作 | 期待結果 | 判定 |
|---|---|---|---|---|
| F01 | List | reload | Role mới xuất hiện, đúng số quyền | |
| F02 | Round-trip | mở Sửa quyền | Bộ quyền tick đúng như đã lưu | |

### G/H/I
| No | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| H01 | Xóa mềm CUSTOM | Soft delete | DELETE role CUSTOM | `deleted_at`+`deleted_by` set, `is_active=0`; 204 | | |
| H02 | Ẩn sau xóa | reload | Ko hiển thị | | |
| I01 | Thiếu quyền API | Authz | POST ko `role.write` | 403 | | ⚠ user thấy nút (admin.role_manage) vẫn có thể 403 API — **Defect#1** |
| I02 | Cách ly tenant | Multi-tenant | Role CUSTOM tenant khác | Ko thấy/ko sửa | | role SYSTEM tenant_id NULL dùng chung |

## 3. Defect candidates
| ID | Mức | Mô tả | Vị trí |
|---|---|---|---|
| #1 | **Cao** | Lệch permission FE↔API: nút gate `admin.role_manage` nhưng API cần `role.write`/`role.read`. User thấy nút vẫn bị 403; hoặc ngược lại. Các nút Sửa/Xóa/ma trận **không** gate FE | `page.tsx:122` vs `RolesController.cs` |
| #2 | TB | Update không có validator BE → name 1 ký tự/rỗng qua API được (chỉ FE zod chặn) | `UpdateRoleCommand.cs` |
| #3 | TB | Không giới hạn maxLength FE/BE cho name(100)/description(500) → vượt chỉ lỗi ở DB | — |
| #4 | Thấp | Text lỗi FE ghi "3-30 ký tự" nhưng regex thực cho 3-31 (lệch 1) | `RoleForm.tsx:16` |

## 4. 実施サマリ
| Nhóm | Case | OK | NG | 保留 |
|---|---|---|---|---|
| A/B | 6 | | | |
| C Validate | 8 | | | |
| D/E | 6 | | | |
| F | 2 | | | |
| H/I | 4 | | | |
| **TỔNG** | **26** | | | |
