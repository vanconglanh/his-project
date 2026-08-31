# Việc 4 — Integration Test cho Portal (cổng bệnh nhân) + B2B (ApiKey)

Ngày: 2026-08-31 · Worktree: `agent-a38a4d95bce0de1fb` (nhánh worktree tách từ `develop`).

## Kết quả chạy thật (MySQL 8 Testcontainers + WebApplicationFactory<Program>)

```
Passed! - Failed: 0, Passed: 17, Skipped: 0, Total: 17
```

Log đầy đủ: `run-output.txt`.

## File mới tạo

- `backend/tests/ProDiabHis.IntegrationTests/Portal/PortalTestTokens.cs` — helper sinh JWT portal.
- `backend/tests/ProDiabHis.IntegrationTests/Portal/PatientPortalApiIntegrationTests.cs` — 10 test.
- `backend/tests/ProDiabHis.IntegrationTests/B2B/ApiKeyTestSeed.cs` — helper hash + vá cột đọc.
- `backend/tests/ProDiabHis.IntegrationTests/B2B/PublicApiKeyIntegrationTests.cs` — 7 test.

KHÔNG sửa file hạ tầng dùng chung (TestTokens.cs / ApiTestFixture.cs / TestSchemaSupplement.cs).

## Cách sinh Portal token (để tái dùng)

Token `PortalBearer` bám sát `JwtService.GeneratePortalToken`:
- issuer = `ProDiabHis`, audience = **`patient-portal`** (khác token nội bộ aud=`ProDiabHis`),
- ký bằng `TestTokens.Secret` (= JWT__SECRET test host nạp),
- claims: `jti`, `patient_id` (GUID), `patient_code`, `tenant_id`.

```csharp
var token = PortalTestTokens.ForPatient(patientId, tenantId, "BN000001");
var client = _fx.ClientWithToken(token);
// biến thể: PortalTestTokens.Expired(...), PortalTestTokens.WithWrongAudience(...)
```

## Cách seed API key B2B (để tái dùng)

- ApiKeyAuthFilter: đọc header `X-Api-Key` -> SHA-256 hex thường -> `IApiKeyStore.FindByHashAsync`.
- Lưu `ApiKeyTestSeed.Sha256Hex(rawKey)` vào cột `api_key_hash` của `diab_his_api_partners`; khi gọi thì gửi rawKey.
- `ApiKeyTestSeed.EnsureReadColumnsAsync(conn)` thêm cột đọc `ip_whitelist` (idempotent) để câu SELECT của `ApiKeyStoreImpl` parse được — cột `scopes` đã do TestSchemaSupplement thêm sẵn.

## Những gì đã bỏ / giảm phạm vi + lý do

1. **Happy-path B2B "key đúng scope → 200" và "sai scope → 403"**: BỎ.
   - Lý do: `ApiKeyStoreImpl.FindByHashAsync` dùng `SELECT BIN_TO_UUID(id) ... ip_whitelist`
     nhưng schema test (EF EnsureCreated) tạo cột `id` kiểu `char(36)` và cột `ip_whitelist_json`
     (không phải `ip_whitelist` / `binary(16)`). `BIN_TO_UUID` trên chuỗi 36 ký tự sẽ lỗi ->
     không thể materialize 1 partner hợp lệ để đi tới nhánh pass/403. Đây đúng là hạn chế schema
     test ghi ở `itc-full-coverage` mục 5 (chuỗi migration chưa dựng được DB sạch từ số 0).
   - Không sửa kiểu cột `id` tại runtime vì bảng `diab_his_api_partners` dùng chung với
     `Admin/ApiPartnersApiIntegrationTests` trong cùng collection "Api" (EF write path cần char(36)).
   - Đã phủ chắc chắn: thiếu key -> 401, key sai -> 401 (nhánh bảo mật quan trọng nhất).

2. **Portal happy-path `/me/encounters` khẳng định 200**: giảm còn "đã qua xác thực (không 401)".
   - Lý do: endpoint đọc bảng/cột (vd `diab_his_enc_diagnoses`) chỉ tạo đủ bởi migrations -> 500
     do schema test thiếu. Theo đúng tiền lệ `ApiPartners "DungQuyen"`, chỉ assert phần chắc chắn.
   - `/me` (hồ sơ) và cách ly A/B thì seed đủ và assert 200 + nội dung thật.

3. **POST đặt lịch happy-path**: chỉ test guard bảo mật (401 khi thiếu token / token nội bộ sai
   audience). Không test tạo lịch thành công vì bảng `diab_his_sch_appointments` không do EF tạo
   (chỉ có ở migrations) -> ngoài phạm vi schema test.

## Danh sách test

Portal (10): an danh 401; token hết hạn 401; **token nội bộ sai audience 401** (x2: /me và đặt lịch);
token tự-tạo sai audience 401; đặt lịch an danh 401; xem hồ sơ chính mình 200 + đúng dữ liệu;
**cách ly A không thấy dữ liệu B**; danh sách lần khám đã qua xác thực.

B2B (7): thiếu X-Api-Key -> 401 (đăng ký BN / đặt lịch / xem lịch / danh mục / key rỗng);
key sai -> 401 (đăng ký BN / xem lịch).
