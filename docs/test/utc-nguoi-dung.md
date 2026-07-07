# 単体テスト仕様書 (UTC) — Màn hình **Người dùng** (User management)

> Quy ước & catalog: xem [utc-00-quy-uoc-chuan-nhat.md](utc-00-quy-uoc-chuan-nhat.md). Bám code thật: FE `admin/users/page.tsx` + `InviteUserForm.tsx` · BE `InviteUserCommand.cs`/`AcceptInviteCommand.cs`/`AssignRolesCommand.cs` · DB `sec_users` + `0020_extend_sec_users.sql` + `sec_user_roles`.

| Mục | Nội dung |
|---|---|
| 機能ID | `USER-CRUD-001` |
| Màn hình | Quản trị → Người dùng |
| Route FE | `/admin/users` |
| API base | `/api/v1/users` |
| Bảng DB | `sec_users` (PK `ID`) + nối `sec_user_roles` (UNIQUE `USER_ID,ROLE_ID`) |
| Permission | Xem `user.read` · Mời `user.invite` · Sửa `user.write` · Xóa `user.delete` · Gán role `user.assign_role` (⚠️ KHÔNG có `admin.user_manage`) |
| Đặc thù | **Chỉ luồng Mời (invite)** — KHÔNG tạo trực tiếp/mật khẩu tạm. User mới = PENDING → accept-invite đặt mật khẩu (≥12 ký tự, đủ loại) → ACTIVE |

## 1. Field matrix — form Mời (Invite)

| Field (FE id) | Nhãn | Control | FE rule | BE validator | DB | GAP |
|---|---|---|---|---|---|---|
| inv-email | Email | Input email | `.email()` ✅ | `NotEmpty().EmailAddress()` | `EMAIL` VARCHAR(255) NOT NULL, UNIQUE `UK_EMAIL` (toàn cục) | ⚠️ unique scope lệch |
| inv-full-name | Họ tên | Input text | min2 ✅ | `NotEmpty()` (ko min2) | `FULL_NAME` generated STORED | ⚠️ BE nhận 1 ký tự; cột generated |
| inv-phone | Điện thoại | Input text | optional | — | `PHONE` VARCHAR(50) NULL | ko chặn maxLen/format |
| role-{code} | Vai trò | Checkbox 7 role | array.min1 ✅ | `RoleCodes NotEmpty()` + phải tồn tại | qua `sec_user_roles` | |

Role options (FE): ADMIN, BACSI, DIEUDUONG, LETAN, DUOCSI, KETOAN, KYTHUATVIEN.
**Bắt buộc:** `email`, `full_name`, `role_codes`(≥1). `phone` optional.

## 2. Test cases

### A — Load ban đầu
| No | 観点 | 操作 | 期待結果 | 判定 |
|---|---|---|---|---|
| A01 | List | Mở `/admin/users` | Bảng user; cột Email/Tên/Vai trò/Trạng thái; filter role + status (PENDING/ACTIVE/LOCKED/DISABLED) | |
| A02 | Form Mời | Mở "Mời người dùng" | email/full_name/phone rỗng; danh sách 7 role checkbox bỏ chọn | |
| A03 | Quyền | User ko `user.invite` | Nút Mời ẩn / API 403 | |

### B — Nhập/hiển thị
| No | 観点 | データ | 期待結果 | 判定 |
|---|---|---|---|---|
| B01 | full_name tiếng Việt | `Trần Thị Hồng` | Đủ dấu | |
| B02 | Chọn role | tick BACSI, DUOCSI | State `role_codes` cập nhật | |

### C — Validate
| No | 観点 | データ | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| C01 | email bắt buộc | rỗng | Chặn | | |
| C02 | email sai | `abc@` | Chặn (FE+BE) | | |
| C03 | full_name min2 | `A` | FE chặn | | ⚠️ **Defect#4** BE chỉ NotEmpty → API nhận 1 ký tự |
| C04 | ko chọn role | 0 role | Chặn (min1) | | |
| C05 | role ko tồn tại | gửi code lạ (API) | `ROLE_NOT_FOUND` | | |
| C06 | phone > 50 | 境界値 DB(50) | **Kỳ vọng chặn** | | ⚠️ **Defect#5** ko chặn FE/BE → lỗi DB |

### D/E — Business + DB + invite
| No | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| D01 | Email trùng | Unique | Mời email đã tồn tại (cùng tenant) | `USER_EMAIL_EXISTS` "Email đã được đăng ký" | | |
| D02 | Email trùng khác tenant | Unique scope | Mời cùng email ở tenant khác | ⚠️ BE cho phép (check per-tenant) nhưng DB `UK_EMAIL` **toàn cục** → lỗi DB | | **Defect#3** |
| E01 | Invite tạo PENDING | Business | Mời hợp lệ | User `Status=PENDING`, `PasswordHash` rỗng, `InviteToken` hex, hết hạn +7 ngày; HTTP 2xx trả `user_id`,`invite_expires_at` | | |
| E02 | Gán role | Ghi nối | Sau E01 | Row trong `sec_user_roles` đúng role | | |
| E03 | Gửi email mời | Side-effect | Sau E01 | Link `accept-invite?token=` gửi tới email | | |
| E04 | tenant_id | Multi-tenant | Sau E01 | Gán từ JWT | | ⚠ **Defect#1** entity int vs DB CHAR(36) |

### F — Accept invite (đặt mật khẩu) & load sau
| No | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| F01 | Accept hợp lệ | Public flow | POST `/accept-invite` token + mật khẩu mạnh | Status→ACTIVE, IsActive=true, xóa token, cấp access+refresh token | | |
| F02 | Mật khẩu yếu | Password policy | mật khẩu `12345678` | `PASSWORD_TOO_WEAK` (cần ≥12, hoa+thường+số+ký tự đặc biệt) | | |
| F03 | Token hết hạn | Expiry | token quá 7 ngày | `USER_INVITE_EXPIRED` | | |
| F04 | Token sai | Security | token bịa | `USER_INVITE_EXPIRED`/lỗi | | |
| F05 | List sau accept | Load sau | reload list | User chuyển ACTIVE | | |

### G/H/I — Vòng đời, role, quyền
| No | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| G01 | Khóa (disable) | Business | POST `/{id}/disable` | Status đổi (khóa); perm `user.write` | | |
| G02 | Mở khóa (enable) | Business | POST `/{id}/enable` | Status ACTIVE | | |
| G03 | Gán thêm role | Assign | POST `/{id}/roles` | Thêm role mới, **ko xóa role cũ**; ghi audit; perm `user.assign_role` | | |
| G04 | Thu hồi role | Revoke | DELETE `/{id}/roles/{code}` | Gỡ role | | |
| H01 | Xóa mềm | Soft delete | DELETE `/{id}` | `deleted_at` set; perm `user.delete` | | |
| I01 | Thiếu quyền mời | Authz | POST invite ko `user.invite` | 403 | | |
| I02 | Cách ly tenant | Multi-tenant | GET user tenant khác | 404/ko thấy | | |
| J01 | Encoding | tiếng Việt | full_name có dấu | Lưu/hiển thị đúng | | |

## 3. Defect candidates
| ID | Mức | Mô tả | Vị trí |
|---|---|---|---|
| #1 | **Cao** | `tenant_id` entity `int` vs DB migration `CHAR(36)`; PK entity Guid vs `sec_users.ID int AUTO_INCREMENT` — lệch kiểu | `User.cs` vs migration/dump |
| #2 | **Cao** | `FULL_NAME` là cột **generated STORED** (từ FIRST/LAST_NAME) trong dump nhưng BE set trực tiếp `FullName` → schema legacy khác model | dump:42 vs command:82 |
| #3 | **Cao** | Email unique: BE check per-tenant nhưng DB `UK_EMAIL` **toàn cục** → 2 tenant ko thể trùng email dù BE cho phép → lỗi DB khó hiểu | command:59 vs sql:84 |
| #4 | TB | `full_name`: FE min2 vs BE chỉ NotEmpty → API nhận 1 ký tự | InviteUserCommand:26 |
| #5 | TB | `phone` ko maxLen/format FE/BE, DB VARCHAR(50) → vượt lỗi DB | — |
| #6 | Thấp | Cột trạng thái trùng lặp (`STATUS int`, `IS_ACTIVE`, `IS_LOCKED` legacy + `user_status` mới) dễ nhầm khi assert DB | migration+dump |
| #7 | Thấp | AssignRolesCommand ko có validator class (chỉ check trong handler) | — |

## 4. 実施サマリ
| Nhóm | Case | OK | NG | 保留 |
|---|---|---|---|---|
| A/B | 5 | | | |
| C Validate | 6 | | | |
| D/E Invite+DB | 6 | | | |
| F Accept | 5 | | | |
| G/H/I/J | 8 | | | |
| **TỔNG** | **30** | | | |
