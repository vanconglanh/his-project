# Evidence P0-2 — Backfill id_number_bidx + search CCCD (2026-08-30)

## 1. DB truoc fix (bidx toan NULL)
```
SELECT COUNT(*), SUM(id_number_enc<>""), SUM(id_number_bidx IS NULL) FROM diab_his_pat_patients;
-> total=43, enc_nonempty=20, bidx_null=43 (toan bo NULL)
```

## 2. Bug phat hien khi verify: id_number_enc luu RAW (khong marker enc:v1:), backfill cu dung _pii.Unprotect -> tra nguyen ciphertext -> bidx = hash(ciphertext) != hash(CCCD that)
```
id_number_enc mau: x095vQLKE+UwSjj4Buiw3Z1hnMEJe8eZhoCT98HzBhaYGT3HDQl7ww==  (khong co tien to enc:v1:)
Fix: PiiBackfillService.DecryptEnc() marker-aware: _pii.IsProtected(enc) ? _pii.Unprotect : _enc.Decrypt
```

## 3. Blind index tu tinh KHOP bidx da luu (key canonical trong appsettings.Development.json)
```
CCCD 048172044001 -> HMAC-SHA256(BlindIndexKey,"IdNumber:048172044001") = b7f0bdd2c83a20d8ed02a42d6fc4704d091046a484389b8f773a7feaffb54cc9
DB id_number_bidx (masked 04********01)                                  = b7f0bdd2c83a20d8ed02a42d6fc4704d091046a484389b8f773a7feaffb54cc9  [KHOP]
```

## 4. Backfill chay that (marker-aware + phone-from-enc)
```
PiiBackfill hoan tat tenant=1 scanned=0 encrypted=0 bidx=59 insBidx=1 errors=0
(59 = 20 CCCD + 39 SDT; 1 the BHYT)
```
## 5. Test API THAT: GET /patients/search?q=048172044001 (login letan.test)
```json
{
  "total": 1,
  "found": [
    {
      "code": "BNT01000020",
      "full_name": "Le Thi Huong",
      "id_number": "04********01"
    }
  ]
}
```
=> Tim thay dung benh nhan cu BNT01000020 (Le Thi Huong). LOI DA HET.
