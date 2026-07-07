# 単体テスト仕様書 (UTC) — Màn hình **Phòng khám / Buồng khám (Rooms)**

> Quy ước & catalog: xem [utc-00-quy-uoc-chuan-nhat.md](utc-00-quy-uoc-chuan-nhat.md). Bám code thật: BE `RoomHandlers.cs`/`RoomDtos.cs` · DB `9006_create_clinic.sql:79`.

| Mục | Nội dung |
|---|---|
| 機能ID | `ROOM-CRUD-001` |
| Route FE | (⚠️ **KHÔNG có form FE** — chỉ dùng làm dropdown ở Tiếp đón) |
| API base | `/api/v1/rooms` |
| Bảng DB | `diab_his_sys_rooms` (PK `id` CHAR(36)) · UNIQUE `(tenant_id, code)` |
| Permission | Xem `room.read` · Ghi `room.write` · Xóa `room.delete` |
| ⚠️ Tình trạng | Backend CRUD đầy đủ nhưng **không có màn hình FE** quản lý phòng. UTC test ở **tầng API**. |

## 1. Field matrix (3 tầng)

| Field (DTO) | Nhãn | Control FE | BE validator | DB: type/null/default | GAP |
|---|---|---|---|---|---|
| RoomCode | Mã phòng | ❌ ko có | inline `IsNullOrWhiteSpace`→`VALIDATION_ERROR`; check trùng→`ROOM_CODE_DUPLICATE` | `code` VARCHAR(20) NOT NULL, UNIQUE | ko maxLen guard |
| Name | Tên phòng | ❌ | inline null-check | `name` VARCHAR(100) NOT NULL | |
| MaxPerDay | Sức chứa | ❌ | ko validate range; default 40 (DTO) | `capacity` INT NOT NULL DEF 1 | ⚠️ tên lệch: MaxPerDay↔capacity |
| IsActive | Kích hoạt | ❌ | ko validate; default true | `is_active` TINYINT(1) NOT NULL DEF 1 | |
| (room_type) | Loại phòng | ❌ | **hardcode 'EXAM'** | `room_type` VARCHAR(30) NOT NULL DEF EXAM | ⚠️ ko tạo LAB/RAD qua API |
| (floor) | Tầng | ❌ | ko trong DTO | `floor` VARCHAR(10) NULL | ko quản lý được |
| (branch_id) | Chi nhánh | ❌ | ko trong DTO | `branch_id` INT NULL | |

**Bắt buộc:** `RoomCode`, `Name` (+ unique code trong tenant).

## 2. Test cases (tầng API)

### A/C — Tạo & Validate
| No | 観点 | データ (API) | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| A01 | (Gap FE) | Tìm màn quản lý phòng | ⚠️ **Không có** (chỉ dropdown ở Tiếp đón) | | **Defect#1** |
| C01 | code rỗng | POST `{RoomCode:"   ",Name:"P1"}` | 400 `VALIDATION_ERROR` | | inline check |
| C02 | name rỗng | `{RoomCode:"P01",Name:""}` | 400 | | |
| C03 | code > 20 | 21 ký tự | Kỳ vọng chặn; thực tế lỗi DB (ko maxLen guard) | | **Defect#5** |
| C04 | MaxPerDay âm/0 | `MaxPerDay:-1` | Kỳ vọng chặn; thực tế ko validate range | | |

### D/E/F/G/H/I
| No | 観点 | 操作 | 期待結果 | 判定 | 備考 |
|---|---|---|---|---|---|
| D01 | Mã trùng | POST trùng code | 400 `ROOM_CODE_DUPLICATE` | | |
| E01 | Insert | POST hợp lệ | 1 row `diab_his_sys_rooms`; id CHAR(36); tenant JWT | | |
| E02 | room_type ép EXAM | Business | POST kèm room_type=LAB | DB `room_type='EXAM'` (client bị bỏ) | | **Defect#2** |
| E03 | capacity default | Ghi | POST ko gửi MaxPerDay | DB `capacity=40` (DTO default) — ⚠ khác DB default 1 | | |
| F01 | List/Read | GET `/rooms` | Phòng mới xuất hiện | | |
| G01 | Update | PUT `/{id}` | Cập nhật name/capacity/is_active; **room_type ko đổi** | | |
| H01 | Xóa mềm | DELETE `/{id}` (perm `room.delete`) | `deleted_at` set | | |
| I01 | Thiếu quyền | POST ko `room.write` | 403 | | |
| I02 | Cách ly tenant | GET phòng tenant khác | 404/ko thấy (phòng global tenant_id null dùng chung) | | |

## 3. Defect candidates
| ID | Mức | Mô tả | Vị trí |
|---|---|---|---|
| #1 | **Cao** | Không có màn hình FE quản lý phòng — chỉ có API + dropdown Tiếp đón | `frontend/(dashboard)` |
| #2 | TB | `room_type` hardcode 'EXAM'; Update ko đổi → không tạo LAB/RADIOLOGY/CASHIER qua API dù DB hỗ trợ | `RoomHandlers.cs:149` |
| #3 | TB | `floor`/`branch_id` không có trong DTO → luôn NULL, không quản lý được | Dtos |
| #4 | Thấp | Tên lệch ngữ nghĩa: DTO `MaxPerDay` (per-day) map cột `capacity` (concurrent) | Handlers:150 |
| #5 | Thấp | Validator inline null-check, không giới hạn maxLen code(20)/name(100) → lỗi tận DB | |

## 4. 実施サマリ
| Nhóm | Case | OK | NG | 保留 |
|---|---|---|---|---|
| A/C | 5 | | | |
| D/E/F/G/H/I | 9 | | | |
| **TỔNG** | **14** | | | |
