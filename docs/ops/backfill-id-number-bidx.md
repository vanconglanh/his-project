# Backfill blind index PII (id_number_bidx / card_no_bidx / phone_bidx)

> Script **DATA chạy-một-lần** — KHÔNG phải migration schema. Chạy thủ công sau khi deploy trên
> MỌI môi trường có dữ liệu cũ (local / staging / prod). Idempotent: chạy lại nhiều lần an toàn.

## 1. Vấn đề (lỗi P0)

Tìm kiếm bệnh nhân chuyển sang dùng **blind index HMAC** (`id_number_bidx`) thay cho LIKE trên
plaintext. Dữ liệu bệnh nhân cũ có `id_number_enc` (đã mã hoá AES-GCM) đầy đủ nhưng
`id_number_bidx = NULL` — nên lễ tân gõ **đúng CCCD** của bệnh nhân cũ vẫn ra **"không tìm thấy"**.
Tương tự cho số thẻ BHYT (`diab_his_pat_insurances.card_no_bidx`) và số điện thoại (`phone_bidx`).

Không thể tính blind index bằng SQL thuần (AES-GCM nonce ngẫu nhiên) → phải chạy bằng code C#:
giải mã `*_enc` → `BlindIndex(plain)` → `UPDATE *_bidx`. Logic nằm trong
`PiiBackfillService` (tái dùng), gọi qua console command chạy-một-lần.

## 2. Cách chạy

```bash
# Toàn bộ tenant có dữ liệu (quét DISTINCT tenant_id từ diab_his_pat_patients):
dotnet run --project backend/src/ProDiabHis.Api -- backfill-bidx

# Chỉ 1 tenant cụ thể:
dotnet run --project backend/src/ProDiabHis.Api -- backfill-bidx 1
```

App khởi tạo DI, chạy `PiiBackfillService.RunAsync` cho từng tenant (batch mặc định 500),
log tiến độ ra console, rồi **thoát — KHÔNG chạy web server**. Alias `--backfill-pii` tương đương.

### Điều kiện môi trường bắt buộc

- `ASPNETCORE_ENVIRONMENT` trỏ đúng môi trường (vd `Development`) để nạp đúng connection string + khoá.
- **`Encryption:BlindIndexKey`** (base64 ≥ 32 byte) PHẢI được cấu hình và **GIỐNG HỆT khoá mà app
  runtime dùng khi tìm kiếm**. Nếu backfill dùng khoá khác lúc query → bidx sinh ra không khớp →
  tìm vẫn không ra. Nếu thiếu khoá này, blind index bị tắt (`BlindIndex` trả null) → backfill
  không điền được gì. `Encryption:MasterKey` cũng phải là khoá đã dùng để mã hoá `*_enc` (để giải mã được).
- Truyền khoá qua env var khi chạy nếu chưa để trong config:
  ```bash
  export ASPNETCORE_ENVIRONMENT=Development
  export Encryption__BlindIndexKey="<BASE64_32BYTE_KEY>"   # KHỚP với khoá app runtime
  dotnet run --project backend/src/ProDiabHis.Api -- backfill-bidx
  ```

## 3. Query verify

Chạy trước/sau backfill, kỳ vọng `bidx_ok == enc_total`:

```bash
docker exec prodiab-mysql mysql --default-character-set=utf8mb4 -uprodiab -p<PASS> prodiab_his -e \
"SELECT SUM(id_number_enc IS NOT NULL AND id_number_bidx IS NOT NULL AND id_number_bidx<>'') AS bidx_ok, \
        SUM(id_number_enc IS NOT NULL AND id_number_enc<>'') AS enc_total \
 FROM diab_his_pat_patients;"
```

Kết quả thực tế trên DB local (2026-08-30): `bidx_ok = 20`, `enc_total = 20` — ĐẠT.

Kiểm tra thẻ BHYT tương tự trên `diab_his_pat_insurances` (`card_no_enc` / `card_no_bidx`).

## 4. Lưu ý vận hành

- **KHÔNG** nhét backfill vào migration schema hay tự chạy lúc app khởi động: dataset lớn có thể
  giải mã/ghi hàng chục nghìn dòng, làm chậm/khoá startup. Đây là bước thủ công, có kiểm soát.
- **Idempotent**: chỉ xử lý dòng `*_enc IS NOT NULL AND *_bidx IS NULL`; chạy lại an toàn.
- Mỗi lần chạy ghi audit `PII_BACKFILL` (severity WARN) vào `diab_his_sec_audit_logs` — đây là
  thao tác giải mã hàng loạt dữ liệu nhạy cảm, cần dấu vết.
- Sau khi backfill, kiểm tra lại chức năng tìm bệnh nhân theo CCCD/SDT/số thẻ BHYT trên UI.
- **Giải mã đúng (`*_enc` không có tiền tố `enc:v1:`)**: `id_number_enc` và `card_no_enc` được lưu
  bằng `IEncryptionService.Encrypt` (RAW, không marker), khác `phone_enc` lưu qua `IPiiProtector.Protect`
  (có marker). `PiiBackfillService.DecryptEnc()` xử lý marker-aware (`IsProtected ? Unprotect : Decrypt`).
  Trước fix này backfill dùng `_pii.Unprotect` cho cả 2 → trả nguyên ciphertext → bidx = hash(ciphertext)
  ≠ hash(CCCD thật) → tìm vẫn trượt dù cột bidx đã có giá trị (lỗi im lặng).

## 5. ⚠️ CẢNH BÁO BẢO TRÌ KHOÁ (bắt buộc trước go-live prod)

- `Encryption:BlindIndexKey` phải được set **CỐ ĐỊNH** cho mỗi môi trường và **BACKUP an toàn**
  (cùng chỗ với `Encryption:MasterKey`). **Mất khoá này = mất khả năng tìm kiếm theo CCCD/SĐT/số thẻ
  cho TOÀN BỘ dữ liệu cũ vĩnh viễn** — vì blind index là HMAC một chiều, không thể tái tạo nếu không
  còn khoá gốc. Nếu buộc phải đổi khoá, phải chạy lại backfill với khoá mới (reset `*_bidx = NULL`
  trước rồi chạy lại — vì backfill idempotent bỏ qua dòng đã có bidx).
- Dev/local: đặt trong `appsettings.Development.json` (gitignored) cạnh `MasterKey`.
  Staging/prod: đặt qua biến môi trường `ENCRYPTION_BLIND_INDEX_KEY` (xem `ops/.env.example`), KHÔNG commit giá trị thật.
