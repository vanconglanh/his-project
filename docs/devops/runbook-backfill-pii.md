# Runbook: Bật mã hóa PII bệnh nhân (Migration 9100 + Backfill)

> Đối tượng thực hiện: **DevOps (Chương)**, phối hợp Backend on-call.
> Phạm vi: bảng `diab_his_pat_patients` (phone, street, reception_note, id_number) và
> `diab_his_pat_insurances` (card_no).
> Bắt buộc đọc kỹ mục 5 (Rollback) TRƯỚC khi chạy bước backfill — hành động này **không thể hoàn tác bằng code**.

---

## 0. Tóm tắt cơ chế (để hiểu rủi ro, không phải lý thuyết)

- Migration `db/migrations/9100_pii_encryption_blind_index.sql`: chỉ thêm cột mới
  (`phone_enc`, `phone_masked`, `phone_bidx`, `id_number_bidx`, `street_enc`,
  `reception_note_enc`, `card_no_bidx`) + index. **Idempotent đã verify — apply được 2 lần
  liên tiếp trên MySQL thật không lỗi.** Cột cũ (`phone`, `street`, `reception_note`) được
  giữ lại, chỉ nới NOT NULL → NULL nếu đang NOT NULL.
- Backfill là **job C#** gọi qua API:
  `POST /api/v1/admin/encryption/pii-backfill` (permission `encryption.rotate`),
  implement tại `backend/src/ProDiabHis.Infrastructure/Security/PiiBackfillService.cs`.
  Chạy **theo từng tenant một** (tham số tenant lấy từ token người gọi — hiện **chưa có
  job all-tenant**, phải lặp lại API này cho từng tenant).
- **ĐIỂM MẤU CHỐT (đã đọc code, dòng 80-89 `PiiBackfillService.cs`):** trong cùng một câu
  `UPDATE`, sau khi ghi `phone_enc/street_enc/reception_note_enc`, code **chủ động**
  `SET phone = NULL, street = NULL, reception_note = NULL` — plaintext bị xóa ngay lập tức,
  **không có bản sao dự phòng nào khác trong DB**.
- Việc đọc lại dữ liệu phụ thuộc hoàn toàn vào `Encryption:MasterKey` (đọc qua
  `IConfiguration["Encryption:MasterKey"]`, throw exception nếu thiếu —
  `AesGcmEncryptor.cs:17-18`, `EncryptionKeyStoreImpl.cs:34-35`). **Mất khóa này sau khi
  backfill = mất vĩnh viễn SĐT/địa chỉ/ghi chú tiếp đón của mọi bệnh nhân đã backfill.**
  Đường lùi DUY NHẤT là restore backup DB chụp **trước** khi backfill.
- Backfill idempotent theo điều kiện `WHERE ... phone_enc IS NULL` (và tương tự cho
  street/note/bidx) — chạy lại lần 2 khớp 0 dòng, **không mã hóa chồng** (double-encrypt).
- Tra cứu theo `*_bidx` dùng HMAC-SHA256 **exact-match** trên giá trị đã chuẩn hóa
  (`PiiNormalizer`) → **tìm kiếm một phần số điện thoại/CMND sẽ không còn ra kết quả**,
  người dùng phải nhập ĐỦ số.
- Nếu thiếu `Encryption:BlindIndexKey`: `PiiProtector.BlindIndexEnabled = false`,
  `BlindIndex()` trả `null` — hệ thống vẫn chạy nhưng **tra cứu theo SĐT/CMND/số thẻ BHYT
  ngừng hoạt động hoàn toàn** cho tới khi cấu hình khóa và chạy lại backfill phần bidx.

---

## 1. Điều kiện tiên quyết & checklist trước khi chạy

- [ ] Đã có APPROVE từ QC cho tính năng mã hóa PII (theo nguyên tắc CLAUDE.md — không
      deploy production khi chưa approve).
- [ ] Xác định môi trường chạy: **staging trước, production sau**, cách nhau tối thiểu
      1 chu kỳ smoke-test ổn định.
- [ ] Backend đã deploy version có migration 9100 + `PiiBackfillService` +
      `EncryptionAdminController` (build local → transfer image theo quy trình deploy chuẩn
      của dự án, KHÔNG build trên server).
- [ ] Đã sinh và cấu hình 2 khóa (xem mục 2) **trước khi** gọi API backfill:
      `Encryption:MasterKey`, `Encryption:BlindIndexKey`.
- [ ] Có tài khoản admin còn permission `encryption.rotate` để gọi API, và biết
      `tenant_id` của từng tenant cần backfill (KHÔNG có job all-tenant, phải lặp thủ công).
- [ ] **Backup DB (bắt buộc, xem mục 1.1)** đã chụp xong VÀ đã verify restore-được, thực
      hiện NGAY TRƯỚC khi gọi bước backfill (không dùng backup cũ hơn vài giờ vì dữ liệu
      bệnh nhân có thể phát sinh mới).
- [ ] Đã thông báo trước cho lễ tân/bác sĩ về thay đổi cách tìm kiếm (xem mục 7), tránh
      backfill giờ cao điểm tiếp đón.
- [ ] Có kênh rollback sẵn sàng: dung lượng đĩa đủ chứa 1 bản dump đầy đủ + thời gian
      restore ước tính (ghi vào checklist thực tế trước khi chạy).

### 1.1. Backup DB — lệnh bắt buộc + cách verify

```bash
# Trên server (hoặc máy có quyền truy cập MySQL), dump TOÀN BỘ database liên quan
# (không chỉ 2 bảng PII — để restore trọn vẹn nếu cần)
BACKUP_FILE="prodiab_his_pre_pii_backfill_$(date +%Y%m%d_%H%M%S).sql.gz"

mysqldump --single-transaction --routines --triggers \
  -h <DB_HOST> -u <DB_USER> -p prodiab_his \
  | gzip > "$BACKUP_FILE"

# Ghi checksum để phát hiện file hỏng/truncate khi transfer
sha256sum "$BACKUP_FILE" > "$BACKUP_FILE.sha256"
```

**Verify bản dump restore được (BẮT BUỘC — dump hỏng mà không biết = mất dữ liệu thật):**

```bash
# 1) Kiểm tra checksum khớp sau khi copy sang nơi lưu trữ
sha256sum -c "$BACKUP_FILE.sha256"

# 2) Kiểm tra gzip không hỏng
gzip -t "$BACKUP_FILE"

# 3) Restore thử vào DB tạm (KHÔNG phải DB đang chạy) để chắc chắn file dump chạy được
mysql -h <DB_HOST> -u <DB_USER> -p -e "CREATE DATABASE prodiab_his_restore_test;"
gunzip -c "$BACKUP_FILE" | mysql -h <DB_HOST> -u <DB_USER> -p prodiab_his_restore_test

# 4) Đối chiếu số dòng bảng PII giữa DB gốc và DB restore-test để chắc dữ liệu đầy đủ
mysql -h <DB_HOST> -u <DB_USER> -p -N -e \
  "SELECT COUNT(*) FROM prodiab_his.diab_his_pat_patients;"
mysql -h <DB_HOST> -u <DB_USER> -p -N -e \
  "SELECT COUNT(*) FROM prodiab_his_restore_test.diab_his_pat_patients;"
# Hai số phải bằng nhau -> dump hợp lệ

# 5) Dọn DB tạm sau khi verify xong
mysql -h <DB_HOST> -u <DB_USER> -p -e "DROP DATABASE prodiab_his_restore_test;"
```

- Lưu `$BACKUP_FILE` + `$BACKUP_FILE.sha256` ra **nơi khác server DB** (MinIO bucket
  `backup/`, hoặc máy DevOps) — tránh trường hợp mất luôn server gốc.
- Đặt tên rõ ràng có timestamp, giữ tối thiểu tới khi xác nhận backfill ổn định (khuyến
  nghị giữ vĩnh viễn bản backup pre-PII-migration, không tính vào chu kỳ xoay vòng 30 ngày
  thông thường).

---

## 2. Quản lý `Encryption:MasterKey` và `Encryption:BlindIndexKey`

### 2.1. Sinh khóa

```bash
# MasterKey: 32 bytes base64, dùng cho AesGcmEncryptor + EncryptionKeyStoreImpl
openssl rand -base64 32

# BlindIndexKey: 32 bytes base64, KHÁC MasterKey (domain riêng, xem PiiProtector.cs:12)
openssl rand -base64 32
```

- Mỗi môi trường (dev/staging/prod) có cặp khóa **riêng biệt**, không tái sử dụng khóa
  dev cho staging/prod.
- Không sinh khóa từ nguồn ngẫu nhiên yếu (không dùng `RANDOM.ORG`, không tự gõ tay).

### 2.2. Lưu ở đâu / ai giữ

- Lưu trong `ops/.env` trên server production (file **chỉ tồn tại trên server**, theo
  quy ước hiện có của repo, không sync vào git).
- Người giữ bản sao ngoài server: **DevOps (Chương)** lưu trong password manager /
  vault nội bộ có kiểm soát truy cập (không lưu file .txt trần trên máy cá nhân, không
  gửi qua chat không mã hóa).
- Khuyến nghị dài hạn: chuyển sang secret manager tập trung (Vault/SOPS) khi hệ thống
  scale nhiều tenant — hiện tại chấp nhận `.env` + backup vault cá nhân do quy mô nhỏ.

### 2.3. Sao lưu khóa ra ngoài server (BẮT BUỘC)

- Ngay khi sinh khóa, lưu ngay 1 bản backup **ngoài server** (vault) TRƯỚC khi dùng để
  chạy backfill. **Mất khóa sau khi backfill = mất dữ liệu PII vĩnh viễn** (không có cách
  nào khác ngoài restore DB backup pre-backfill).
- Ghi rõ ngày sinh khóa, môi trường áp dụng, người thực hiện vào note vault (không ghi
  giá trị khóa vào note văn bản thường kèm ticket/Jira công khai).

### 2.4. Quy tắc TUYỆT ĐỐI

- **KHÔNG** commit giá trị khóa vào bất kỳ file nào trong repo (kể cả file mẫu có giá trị
  thật, kể cả branch tạm). File mẫu chỉ chứa placeholder
  `<BASE64_32BYTE_KEY_GENERATE_NEW_PER_ENV>` như `appsettings.Development.template.json`
  hiện có.
- **KHÔNG** ghi giá trị khóa vào log (Serilog/Sentry) dưới bất kỳ hình thức nào — kể cả
  khi debug, không `Console.WriteLine` khóa, không đưa vào exception message.
- **KHÔNG** dán khóa vào issue tracker, chat nhóm không mã hóa đầu-cuối, hay comment code.
- **KHÔNG** để Backend log lỗi kèm theo giá trị `Encryption:MasterKey`/`BlindIndexKey` khi
  troubleshoot — chỉ log rằng khóa "chưa được cấu hình" / "sai độ dài", đúng như hành vi
  hiện có (`AesGcmEncryptor.cs`, `EncryptionKeyStoreImpl.cs` chỉ throw message mô tả, không
  in giá trị khóa).

---

## 3. Trình tự chạy

### Bước 1 — Apply migration 9100

```bash
# Đảm bảo 0000_helpers.sql đã apply trước (chứa add_col_if_missing/add_index_if_missing)
mysql -h <DB_HOST> -u <DB_USER> -p prodiab_his < db/migrations/0000_helpers.sql
mysql -h <DB_HOST> -u <DB_USER> -p prodiab_his < db/migrations/9100_pii_encryption_blind_index.sql
```

### Bước 2 — Verify migration đã tạo đủ cột/index

```sql
SELECT COLUMN_NAME, IS_NULLABLE, COLUMN_TYPE
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = 'prodiab_his'
  AND TABLE_NAME = 'diab_his_pat_patients'
  AND COLUMN_NAME IN ('phone_enc','phone_masked','phone_bidx','id_number_bidx',
                       'street_enc','reception_note_enc');

SELECT COLUMN_NAME FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = 'prodiab_his'
  AND TABLE_NAME = 'diab_his_pat_insurances'
  AND COLUMN_NAME = 'card_no_bidx';

SHOW INDEX FROM diab_his_pat_patients WHERE Key_name IN
  ('idx_patients_phone_bidx','idx_patients_idnum_bidx');
SHOW INDEX FROM diab_his_pat_insurances WHERE Key_name = 'idx_insurances_card_bidx';
```

Kỳ vọng: đủ 6 cột trên `diab_his_pat_patients`, 1 cột trên `diab_his_pat_insurances`,
đủ 3 index. Chạy lại migration lần 2 (đã verify) phải không lỗi và không tạo trùng.

### Bước 3 — Deploy backend có `PiiBackfillService` + cấu hình khóa

- Deploy theo quy trình chuẩn của dự án (build local → GHCR/transfer → server pull →
  `up -d --no-deps <service>`).
- Set `Encryption:MasterKey` và `Encryption:BlindIndexKey` trong `ops/.env` **trước khi**
  container backend khởi động lần đầu với version này (nếu thiếu, `AesGcmEncryptor`/
  `EncryptionKeyStoreImpl` throw exception ngay lúc DI resolve — backend sẽ không start).
- Restart backend, xác nhận health check pass và log KHÔNG báo lỗi thiếu khóa.

### Bước 4 — Dry-run backfill từng tenant (bắt buộc trước khi chạy thật)

```bash
curl -X POST https://<host>/api/v1/admin/encryption/pii-backfill \
  -H "Authorization: Bearer <ADMIN_TOKEN_TENANT_X>" \
  -H "Content-Type: application/json" \
  -d '{"batchSize": 500, "dryRun": true}'
```

- Response trả `patients_scanned`, `patients_encrypted` (số sẽ được xử lý), `errors`.
- `dryRun=true` **không ghi DB** (xem code: `if (dryRun) { encrypted++; continue; }` rồi
  `break` khỏi vòng lặp — chỉ quét 1 batch để ước lượng, không đại diện tổng số tuyệt đối
  nếu tổng bản ghi > `batchSize`). Dùng câu SQL đếm ở Bước 5 để biết chính xác tổng số còn
  lại nếu cần con số đầy đủ trước khi chạy thật.
- Nếu `errors` không rỗng → dừng lại, điều tra nguyên nhân (ví dụ dữ liệu phone có ký tự
  lạ khiến `Protect()`/`BlindIndex()` lỗi) trước khi chạy thật.

### Bước 5 — Câu SQL đếm số dòng chưa mã hóa (chạy trước và sau mỗi lần backfill)

```sql
-- Bệnh nhân còn plaintext chưa mã hóa (điều kiện y hệt WHERE trong PiiBackfillService)
SELECT COUNT(*) AS con_can_backfill
FROM diab_his_pat_patients
WHERE tenant_id = <TENANT_ID>
  AND (
        (phone IS NOT NULL AND phone <> '' AND phone_enc IS NULL)
     OR (street IS NOT NULL AND street <> '' AND street_enc IS NULL)
     OR (reception_note IS NOT NULL AND reception_note <> '' AND reception_note_enc IS NULL)
     OR (phone IS NOT NULL AND phone <> '' AND phone_bidx IS NULL)
      );

-- CMND đã có ciphertext nhưng chưa có blind index
SELECT COUNT(*) AS con_thieu_id_number_bidx
FROM diab_his_pat_patients
WHERE tenant_id = <TENANT_ID>
  AND id_number_enc IS NOT NULL AND id_number_bidx IS NULL;

-- Số thẻ BHYT đã có ciphertext nhưng chưa có blind index
SELECT COUNT(*) AS con_thieu_card_no_bidx
FROM diab_his_pat_insurances
WHERE tenant_id = <TENANT_ID>
  AND card_no_enc IS NOT NULL AND card_no_bidx IS NULL;
```

### Bước 6 — Chạy backfill thật, từng tenant, có kiểm tra sau mỗi tenant

```bash
curl -X POST https://<host>/api/v1/admin/encryption/pii-backfill \
  -H "Authorization: Bearer <ADMIN_TOKEN_TENANT_X>" \
  -H "Content-Type: application/json" \
  -d '{"batchSize": 500, "dryRun": false}'
```

- Job tự lặp batch tới khi `rows.Count == 0` (xem `PiiBackfillService.cs:44-102`) — một
  lần gọi API xử lý HẾT tenant đó, không cần gọi lại trừ khi có lỗi giữa chừng hoặc dữ
  liệu mới phát sinh sau đó.
- Sau khi response trả về, chạy lại câu SQL Bước 5 cho đúng `TENANT_ID` — kỳ vọng
  `con_can_backfill = 0`, `con_thieu_id_number_bidx = 0`, `con_thieu_card_no_bidx = 0`.
- Nếu còn > 0: đọc `errors` trong response lần chạy trước, xử lý bản ghi lỗi thủ công, gọi
  lại API (an toàn vì idempotent — response lần 2 sẽ khớp 0 dòng cho phần đã xử lý xong).
- Lặp lại Bước 4-6 cho từng `tenant_id` còn lại. Không có job all-tenant — theo dõi tiến
  độ bằng bảng tenant (ví dụ ghi vào checklist vận hành riêng ngoài file này).

---

## 4. Verify sau backfill

1. **Đọc lại được dữ liệu qua API nghiệp vụ** (không chỉ raw SQL): mở hồ sơ vài bệnh nhân
   đại diện mỗi tenant qua UI/API `GET /patients/{id}` → xác nhận SĐT, địa chỉ, ghi chú
   tiếp đón hiển thị đúng như trước khi backfill (đối chiếu với dữ liệu đã ghi lại thủ công
   trước đó hoặc từ DB restore-test ở mục 1.1 nếu cần đối chiếu).
2. **Tìm kiếm theo số đầy đủ hoạt động**: thử API/UI tìm bệnh nhân theo đúng số điện thoại
   đầy đủ (khớp `phone_bidx`) và số CMND/CCCD đầy đủ, số thẻ BHYT đầy đủ — phải ra đúng kết
   quả.
3. **Xác nhận tìm kiếm một phần KHÔNG còn hoạt động** (hành vi mong đợi, không phải bug) —
   để tránh báo cáo nhầm là lỗi trong tuần đầu sau go-live.
4. Kiểm tra bảng audit `diab_his_sec_audit_logs` có bản ghi `PII_BACKFILL` (severity WARN)
   cho từng tenant vừa chạy — đối chiếu số liệu `scanned/encrypted/indexed` khớp kỳ vọng.
5. Theo dõi Sentry/log 24-48h sau backfill: không phát sinh exception liên quan
   `Encryption:MasterKey`/`Decrypt` khi tải hồ sơ bệnh nhân.

---

## 5. Kịch bản Rollback — GIỚI HẠN QUAN TRỌNG

> **Một khi plaintext ở `phone/street/reception_note` đã bị `SET NULL` (xảy ra ngay trong
> câu UPDATE của backfill, `PiiBackfillService.cs:80-89`), KHÔNG có cách rollback bằng
> code/migration.** Không có bảng lưu bản sao plaintext, không có soft-delete cho giá trị
> cũ. Đường lùi DUY NHẤT là **restore toàn bộ DB từ bản backup chụp trước khi backfill**
> (mục 1.1).

### 5.1. Rollback migration 9100 (an toàn, KHÔNG mất dữ liệu — chỉ áp dụng nếu CHƯA backfill)

Nếu mới apply migration nhưng chưa gọi API backfill lần nào (chưa có `*_enc` được ghi):
có thể tự tin bỏ qua/giữ nguyên cột mới (không cần rollback bắt buộc, cột mới không ảnh
hưởng luồng cũ vì code cũ không đọc `*_enc`). Nếu cần dọn cột đã thêm:

```sql
ALTER TABLE diab_his_pat_patients
  DROP COLUMN phone_enc, DROP COLUMN phone_masked, DROP COLUMN phone_bidx,
  DROP COLUMN id_number_bidx, DROP COLUMN street_enc, DROP COLUMN reception_note_enc;
ALTER TABLE diab_his_pat_insurances DROP COLUMN card_no_bidx;
-- (kèm DROP INDEX tương ứng nếu cần dọn sạch)
```

**Chỉ chạy bước này khi chắc chắn `*_enc`/`*_bidx` toàn bộ đều NULL (verify bằng SQL Bước
5 = tổng số dòng bảng, tức chưa backfill dòng nào).** Nếu đã có dữ liệu trong `*_enc`, DROP
sẽ xóa luôn ciphertext → mất dữ liệu tương đương chạy backfill rồi mất khóa.

### 5.2. Rollback sau khi ĐÃ backfill (plaintext đã bị xóa)

1. **Dừng ngay** việc backfill thêm tenant khác, dừng traffic ghi mới vào bảng liên quan
   nếu có thể (maintenance window ngắn).
2. Xác định chính xác thời điểm backup pre-backfill đã chụp (mục 1.1) — đây là **RPO thực
   tế**: mọi thay đổi dữ liệu (bệnh nhân mới, cập nhật hồ sơ) giữa thời điểm backup và thời
   điểm restore sẽ **mất**.
3. Thông báo ngay cho PO/QC/leader về việc sẽ có mất dữ liệu phát sinh trong khoảng RPO —
   đây là quyết định cần approve, không tự ý restore khi giờ hành chính nếu không phải
   hotfix khẩn.
4. Restore:
   ```bash
   mysql -h <DB_HOST> -u <DB_USER> -p -e "CREATE DATABASE prodiab_his_rollback;"
   gunzip -c "$BACKUP_FILE" | mysql -h <DB_HOST> -u <DB_USER> -p prodiab_his_rollback
   # Verify số dòng, đối chiếu vài bản ghi mẫu trước khi swap
   # Swap: đổi tên DB hiện tại thành _old, đổi DB rollback thành tên chính thức
   -- Lưu ý: MySQL không có RENAME DATABASE trực tiếp, dùng cách:
   --   dump lại từ prodiab_his_rollback rồi restore đè lên prodiab_his sau khi
   --   đã backup prodiab_his hiện tại (chứa dữ liệu mới hơn) ra file riêng để đối chiếu/vá tay sau.
   ```
5. Sau khi restore, dữ liệu quay về trạng thái **trước migration 9100** (không có cột
   `*_enc`) hoặc trạng thái **sau migration nhưng trước backfill** tùy bản backup được
   chọn — kiểm tra lại bằng SQL Bước 2/5 để biết đang ở trạng thái nào trước khi quyết định
   có chạy lại backfill hay không.
6. Nếu có dữ liệu phát sinh trong khoảng RPO cần cứu: đối chiếu thủ công giữa DB backup cũ
   (`prodiab_his_rollback`) và bản dump "hiện tại trước khi restore" đã lưu ở bước 4, vá tay
   từng bản ghi bị thiếu (việc này tốn thời gian và rủi ro, nên ưu tiên tránh bằng cách
   backup sát thời điểm chạy backfill và backfill ngoài giờ cao điểm).

### 5.3. Rollback vì mất khóa (không phải do backfill sai, mà do thất lạc `MasterKey`)

- Hệ quả giống hệt mục 5.2: không đọc/giải mã được `*_enc`, không rollback bằng code.
- Nếu chỉ mất `BlindIndexKey` (không mất `MasterKey`): dữ liệu vẫn đọc được bình thường
  qua `Unprotect()`, chỉ mất tính năng tìm kiếm. Khắc phục: sinh `BlindIndexKey` mới, chạy
  lại backfill — code sẽ tự **giải mã bằng `MasterKey`** (`_pii.Unprotect(enc)`) rồi tính
  lại `bidx` mới (xem `PiiBackfillService.cs:112-131`, không cần plaintext gốc). Đây là
  điểm khác biệt quan trọng: **mất `BlindIndexKey` sau backfill là rollback được, mất
  `MasterKey` thì KHÔNG.**

---

## 6. Rủi ro & giảm thiểu

| Rủi ro | Ảnh hưởng | Giảm thiểu |
|---|---|---|
| Mất `Encryption:MasterKey` sau backfill | Mất vĩnh viễn SĐT/địa chỉ/ghi chú tiếp đón toàn bộ bệnh nhân đã backfill | Backup khóa ra ngoài server ngay khi sinh (mục 2.3); backup DB pre-backfill bắt buộc và verify restore-được (mục 1.1) |
| Mất `Encryption:BlindIndexKey` | Mất tính năng tìm kiếm theo SĐT/CMND/số thẻ (không mất dữ liệu) | Rollback được: sinh khóa mới + chạy lại backfill phần bidx (mục 5.3) |
| Backfill giữa chừng bị lỗi (network/timeout) một số bản ghi | Một phần bệnh nhân còn plaintext, một phần đã mã hóa — trạng thái hỗn hợp tạm thời | Idempotent — gọi lại API an toàn; theo dõi `errors` trong response và SQL đếm Bước 5 |
| Downtime/ảnh hưởng người dùng trong lúc backfill | Bệnh nhân đang được backfill: UPDATE khóa row ngắn hạn, có thể chậm thao tác tiếp đón/tra cứu đồng thời trên cùng bệnh nhân đó | Chạy `batchSize` vừa phải (mặc định 500, tối đa 5000 — xem `PiiBackfillService.cs:36`), chạy ngoài giờ cao điểm tiếp đón, theo từng tenant tuần tự không chạy song song nhiều tenant lớn cùng lúc trên 1 DB |
| **Bệnh nhân cũ mất hiển thị SĐT/địa chỉ/ghi chú tạm thời** giữa lúc migration đã apply nhưng backfill của tenant đó CHƯA chạy | Nếu code đọc mới ưu tiên đọc `*_enc` trước `phone` cũ mà `*_enc` đang NULL → có thể hiển thị trống cho tới khi backfill tenant đó xong | Backfill ngay sau khi deploy version mới cho từng tenant, không để khoảng trống dài; xác nhận với Backend cách đọc dữ liệu fallback về cột cũ khi `*_enc` NULL trước khi go-live diện rộng |
| Tra cứu một phần số điện thoại/CMND không còn hoạt động | Lễ tân quen thao tác cũ có thể báo "không tìm thấy bệnh nhân" dù bệnh nhân tồn tại | Thông báo trước cho lễ tân/bác sĩ (mục 7), cập nhật hướng dẫn sử dụng nội bộ |
| Backup DB bị hỏng mà không phát hiện trước khi cần restore | Không có đường lùi thực sự dù tưởng đã có backup | Bắt buộc verify restore-được theo mục 1.1 bước 3-4 (restore thử + đối chiếu số dòng), không chỉ tin vào việc lệnh dump chạy "exit code 0" |
| Backfill chạy nhầm 2 lần đồng thời cho cùng 1 tenant (race condition) | Có thể đọc trùng batch, ghi đè `phone_masked`/`phone_bidx` không nhất quán tạm thời (COALESCE tránh ghi đè `phone_masked` đã có, nhưng vẫn nên tránh) | Không gọi song song API backfill cho cùng `tenant_id`; theo dõi tiến độ tuần tự theo checklist |

---

## 7. Thông báo cho lễ tân / bác sĩ

Gửi trước khi backfill tối thiểu 1 ngày làm việc (trừ hotfix khẩn), nội dung tối thiểu:

> **Thông báo: Thay đổi cách tìm kiếm bệnh nhân từ ngày <DD/MM/YYYY>**
>
> Hệ thống nâng cấp bảo mật, mã hóa số điện thoại, địa chỉ và ghi chú tiếp đón của bệnh
> nhân. Từ thời điểm áp dụng:
> - Tìm kiếm theo **số điện thoại phải nhập ĐỦ số**, không tìm được bằng cách nhập vài số
>   cuối/đầu như trước.
> - Tìm kiếm theo **số CMND/CCCD, số thẻ BHYT** cũng phải nhập ĐỦ số.
> - Trong lúc hệ thống đang cập nhật (một khoảng thời gian ngắn, sẽ thông báo cụ thể theo
>   ca trực), một số hồ sơ bệnh nhân cũ có thể **tạm thời không hiển thị số điện thoại/địa
>   chỉ/ghi chú tiếp đón** — không phải lỗi mất dữ liệu, dữ liệu sẽ trở lại bình thường sau
>   khi cập nhật xong. Nếu thấy sai khác kéo dài quá <X giờ theo kế hoạch thực tế>, báo ngay
>   cho DevOps/Backend on-call.
> - Vui lòng KHÔNG tự sửa/nhập lại thông tin bệnh nhân dựa trên nghi ngờ "mất dữ liệu"
>   trong khoảng thời gian này để tránh tạo dữ liệu trùng/sai.

---

## 8. Tham chiếu code (để đối chiếu khi có thay đổi trong tương lai)

- `db/migrations/9100_pii_encryption_blind_index.sql` — migration cấu trúc, idempotent.
- `backend/src/ProDiabHis.Infrastructure/Security/PiiBackfillService.cs` — logic backfill,
  UPDATE xóa plaintext ở dòng 80-89.
- `backend/src/ProDiabHis.Infrastructure/Security/PiiProtector.cs` — Protect/Unprotect/
  BlindIndex, đọc `Encryption:MasterKey` + `Encryption:BlindIndexKey`.
- `backend/src/ProDiabHis.Infrastructure/Security/AesGcmEncryptor.cs`,
  `EncryptionKeyStoreImpl.cs` — nơi `Encryption:MasterKey` được đọc và validate độ dài.
- `backend/src/ProDiabHis.Api/Controllers/EncryptionAdminController.cs` — endpoint
  `POST /api/v1/admin/encryption/pii-backfill` (permission `encryption.rotate`).
- `backend/src/ProDiabHis.Api/appsettings.Development.template.json` — mẫu khai báo 2 khóa
  (chỉ chứa placeholder, không chứa giá trị thật).
