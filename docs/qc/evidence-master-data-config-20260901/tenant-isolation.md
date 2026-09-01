# Evidence — Tenant override/isolation danh mục mã (Việc 1)

Chạy trực tiếp trên DB `prodiab_his` (container prodiab-mysql), dùng CHÍNH query của
`CodeResolver.GetAsync` (sau fix hide). Nhóm test: `ENCOUNTER_TYPE`.

## Dữ liệu test (tenant A = 101)
- Thêm mã riêng `SCREENING` = "Khám tầm soát ĐTĐ"
- Override `FIRST_VISIT` → "Khám mới (PK A)"
- Ẩn `EMERGENCY` (row tenant is_hidden=1)

## Kết quả resolve tenant A (101)
```
FIRST_VISIT   Khám mới (PK A)  (tenant 101 override thắng global)
FOLLOW_UP     Tái khám         (global)
CONSULTATION  Hội chẩn         (global)
SCREENING     Khám tầm soát ĐTĐ (tenant 101, mã riêng)
-- EMERGENCY KHÔNG xuất hiện (đã ẩn)
```

## Kết quả resolve tenant B (202) — KHÔNG bị ảnh hưởng
```
FIRST_VISIT   Khám mới    (global — không thấy tên PK A)
FOLLOW_UP     Tái khám
EMERGENCY     Cấp cứu     (vẫn còn — A ẩn không ảnh hưởng B)
CONSULTATION  Hội chẩn
```

Kết luận: tenant tự thêm/sửa/ẩn mã riêng, KHÔNG ảnh hưởng tenant khác. Fallback về
mã mặc định hệ thống (tenant_id IS NULL) khi tenant chưa tuỳ biến. Data test đã dọn sạch sau verify.

## Schema sau migration 9193 (verify thật)
- code_detail thêm: tenant_id, tenant_scope (generated), is_hidden, is_system
- code_master thêm: tenant_id, tenant_scope, is_system
- UNIQUE key mới: (tenant_scope, code_master_id, code)
- 127 detail rows seed sẵn đánh is_system=1
- Permissions seed: code.read, code.manage, setting.manage (grant admin)
- Idempotent: apply lần 2 không lỗi, không nhân bản dòng.

## setting_meta (migration 9194)
5 key có metadata nhãn tiếng Việt; chỉ `stock_transfer_approval_threshold` is_public=1
(FE đọc qua /api/v1/settings/public).
</content>
