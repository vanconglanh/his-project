# Evidence — Retest vòng 2 sau khi fix 4 Blocker (31/08/2026)

**Nhánh:** `develop` (`b65d566`) · **Người chạy:** QC · **Kiểu chạy:** API + DB (3 lớp: HTTP → JSON → dump SQL)

Tài liệu liên quan: [`../utc-full-flow-20260831.md`](../utc-full-flow-20260831.md) ·
[`../ute-full-flow-20260831.md`](../ute-full-flow-20260831.md) ·
[`../go-live-readiness-nghiepvu-20260831.md`](../go-live-readiness-nghiepvu-20260831.md)

---

## 1. Môi trường

| Hạng mục | Giá trị |
|---|---|
| Rebuild | `docker compose -f ops/docker-compose.yml -f ops/docker-compose.local-app.yml up -d --build backend frontend` |
| Kết quả build | Backend **cache hit hoàn toàn** → image `prodiab-dev-backend` giữ nguyên digest `2026-08-31T08:24:32Z` (UTC) = **15:24 giờ VN**. Mã nguồn backend **không đổi** kể từ lần build mà dev dùng để verify 4 fix (commit lúc 15:33–15:34 là commit của chính code đã build). Frontend rebuild mới `08:41:36Z`. |
| Backend | `prodiab-backend` :5000 · Frontend `prodiab-frontend` :3000 |
| DB | MySQL 8 `prodiab_his`, utf8mb4, truy vấn qua `docker exec prodiab-mysql mysql --default-character-set=utf8mb4` |
| `dotnet test` | **987 PASS / 0 FAIL** (963 unit + 17 integration + 7 architecture) |

> **Lưu ý env parity:** vẫn là môi trường DEV local, timezone container UTC, dữ liệu ít.
> Mọi kết luận về hiệu năng/khối lượng **không** suy ra được cho prod.

## 2. Kết quả tổng hợp — 100 lượt kiểm (98 case UTC + 2 kiểm tra bổ sung)

| Nhóm | Tổng | PASS | FAIL | SKIP |
|---|---:|---:|---:|---:|
| AUTH | 8 | 8 | 0 | 0 |
| REC | 13 | 13 | 0 | 0 |
| ENC/EMR | 10 | 8 | 2 | 0 |
| VIT/INB | 10 | 9 | 0 | 1 |
| CLS | 17 | 16 | 1 | 0 |
| DOC | 7 | 4 | 1 | 2 |
| RX | 7 | 5 | 2 | 0 |
| BIL | 11 | 9 | 0 | 2 |
| DIS | 5 | 5 | 0 | 0 |
| APM | 3 | 2 | 0 | 1 |
| BRN | 3 | 3 | 0 | 0 |
| SEC | 6 | 6 | 0 | 0 |
| **TỔNG** | **100** | **88** | **6** | **6** |

> Vòng 1 gộp nhóm cho ra con số 93; đếm chi tiết theo ID case là 98. Cộng 2 kiểm tra bổ sung
> (`UTC-BIL-08b`, `UTC-DIS-02a`) do QC thêm để khoá chặt BUG-01 và BUG-04 → 100 lượt kiểm.

**6 FAIL = đúng 6 lỗi High/Med đã biết từ vòng 1, chưa nằm trong phạm vi giao sửa. KHÔNG có lỗi mới, KHÔNG có regression.**

## 3. Bốn Blocker — bằng chứng đã fix

| Bug | Case | Bằng chứng thực tế vòng 2 |
|---|---|---|
| **BUG-01** thất thoát tồn kho | `UTC-DIS-02a` | Phát đơn có Gliclazide thiếu tồn → **422**; tồn Metformin **2601→2601**, Gliclazide **60→60**, `stock_movements` **6→6**, phiếu phát **8→8**. Không một dòng nào phát sinh. |
| **BUG-02** phòng chỉ nhận 1 BN/ngày | `UTC-REC-13` | Phòng `PK02` `capacity=1`: BN A → **201**, BN B (khác người, cùng phòng, cùng ngày) → **201**. |
| **BUG-03** ô chọn thuốc sai/rỗng tên | `UTC-RX-01` | `GET /drugs/search?q=Metformin` → `["Metformin 500mg"]`; 30/30 thuốc **không có tên rỗng**. |
| **BUG-04** thu tiền 0/âm/vượt | `UTC-BIL-06/07/08` | `amount=0` → **400** `VALIDATION_ERROR` "Số tiền thanh toán phải lớn hơn 0"; `-50.000` → **400**; `999.999.999` → **400**, `balance` giữ `155000.00` (**không âm**). |

## 4. Kiểm regression (điểm nhấn của vòng này)

| Rủi ro regression | Case | Kết quả |
|---|---|---|
| Validator thu tiền mới chặn oan giao dịch hợp lệ | `UTC-BIL-03` | Thu một phần 62.000/155.000 → **201**, còn lại `93000.00` — **đúng**, không bị chặn oan |
| " | `UTC-BIL-05` | Thu nốt 93.000 → **201**, `balance=0.00`, `status=PAID` |
| " | `UTC-BIL-08b` | Thu thêm khi đã PAID → **400**, `balance` giữ `0.00` (không âm) |
| Transaction cấp phát mới làm hỏng phát thuốc bình thường | `UTC-DIS-02` | Phát đơn đủ tồn → **201**, tồn **2601→2591** (trừ đúng 10), phiếu phát **8→9**, movements **6→7** |
| Logic sức chứa mới làm hỏng tiếp đón bình thường | `UTC-REC-09/10/11/12` | Check-in → 201 ticket `WAITING`; hàng đợi hiển thị; `admit` → encounter `created=true`; check-in trùng → **409** `RECEPTION_DUPLICATE_CHECKIN` (vẫn chặn đúng) |
| Pipeline validation MediatR dùng chung ảnh hưởng luồng khác | `UTC-VIT-03`, `UTC-APM-02`, `UTC-CLS-01→17` | Đều trả đúng mã lỗi/hành vi cũ; `dotnet test` 987/987 |

## 5. Sáu FAIL còn lại (đều là lỗi cũ, không chặn go-live)

| Case | Mức | Thực tế vòng 2 |
|---|---|---|
| `UTC-ENC-02` | High | `doctor_id` của lượt khám = `letan.test@prodiab.test` (LT. Test Demo) — vẫn gán người tiếp đón làm bác sĩ |
| `UTC-RX-05` | High | `GET /prescriptions/{id}/dtqg/status` → **500**. **Đã tìm ra nguyên nhân gốc** (xem mục 6) |
| `UTC-CLS-15` | High | Phiếu XN có dòng `Glucose (đường huyết) 7.2` → `GLU_F` `extracted=false`, chỉ đọc được `HBA1C` |
| `UTC-EMR-08` | Med | 2 mẫu bệnh án hệ thống đều `structured_json = NULL` → kéo theo `schema_snapshot_json` khi lưu bệnh án cũng `NULL` |
| `UTC-DOC-04` | Med | Phiếu KQ XN → phân loại `Unknown` 0.55 (candidates: LabResult 0.55 / Legacy 0.5) |
| `UTC-RX-07` | Med | `total_amount` của đơn thuốc = **0** dù đơn có 2 thuốc |

## 6. Nguyên nhân gốc BUG-06 (`UTC-RX-05`) — QC tìm được trong vòng này

`docs/qc/evidence-retest-vong2-20260831/log-bug06-dtqg-500-stacktrace.json`:

```
System.FormatException: The input string 'c171cb1f-8905-4bf5-b673-ad3bd82cc689' was not in a correct format.
   at System.String.System.IConvertible.ToInt32(IFormatProvider provider)
   at Dapper.SqlMapper.ExecuteScalarImplAsync[T](...)
   at ProDiabHis.Application.Pharmacy.Dtqg.GetDtqgStatusHandler.Handle(...)
      in /src/src/ProDiabHis.Application/Pharmacy/Dtqg/DtqgHandlers.cs:line 141
```

`DtqgHandlers.cs:141` gọi `ExecuteScalarAsync<int>` trên một truy vấn trả về **GUID** của đơn thuốc
→ Dapper cố `Convert.ToInt32` chuỗi GUID → `FormatException` → 500. **QC không sửa code, chỉ báo cáo.**

## 7. Quan sát bổ sung (không phải bug)

1. **`GET /lab-results/pending-items` không có tham số `encounter_id`.** Endpoint chỉ nhận `q` và `limit`
   (`LabResultsController.cs:47`) — truyền `encounter_id` bị bỏ qua âm thầm. Đây là **danh sách công việc
   toàn phòng xét nghiệm** cho KTV, không phải rò rỉ dữ liệu. Ghi nhận ở mức **Low/UX**: người gọi API có
   thể tưởng đã lọc mà thực tế chưa lọc.
2. **Chống brute-force 2FA hoạt động thật.** Gọi `/auth/2fa/verify` sai quá 5 lần/5 phút →
   `AUTH_MFA_TOO_MANY_ATTEMPTS` (rate-limit Redis key `rl:mfa-verify:{userId}`). QC bị khoá thật khi
   chạy lặp và phải xoá key Redis để chạy tiếp — bằng chứng cơ chế sống.
3. **`UTC-BRN-03` nâng từ SKIP lên PASS.** Nay kiểm được thật: user thuộc CN1 gọi API với
   `X-Branch-Id: 2` → **403** `BRANCH_ACCESS_DENIED`.
4. **Ngữ nghĩa sức chứa mới:** vé `WAITING` **không** tính vào sức chứa, chỉ `CALLED`/`IN_PROGRESS` mới tính.
   Đúng nghiệp vụ (hàng đợi không giới hạn, phòng thì có). Hệ quả vận hành: **vé bị bỏ quên ở trạng thái
   `IN_PROGRESS` sẽ chiếm phòng vĩnh viễn** — cần có thao tác "kết thúc khám"/dọn vé treo cuối ngày.

## 8. Ghi chú trung thực về điều kiện chạy

1. **Đã can thiệp dữ liệu test 2 lần, KHÔNG sửa code sản phẩm:**
   - Đóng các vé còn kẹt `CALLED`/`IN_PROGRESS` ở PK01/PK02 (tồn từ các vòng chạy trước) để phòng có chỗ trống.
   - Xoá key Redis `rl:mfa-verify:*` sau khi QC tự làm khoá tài khoản admin bằng vòng lặp thử 2FA.
2. **`UTC-DOC-04` từng cho PASS giả.** Khi upload theo lô cùng 2 tệp khác, bệnh nhân đó đang có **rất nhiều**
   XN chờ nên điểm phân loại bị đẩy lên 0.75 → `LabResult`. Chạy lại đúng tiền điều kiện của case
   (bệnh nhân mới, **chỉ 2 XN đang chờ**) thì kết quả là `Unknown` 0.55. **QC ghi FAIL**, không lấy kết quả thuận lợi.
3. **6 case SKIP đều ghi rõ lý do**, không có case nào bị bỏ im lặng.
4. Không tái dùng ảnh/log của vòng trước. Toàn bộ log trong thư mục này sinh từ lần chạy hôm nay.

## 9. Tệp trong thư mục

| Tệp | Nội dung |
|---|---|
| `ket-qua-100-case.json` | Kết quả từng case: `id` / `status` / `note` (số liệu thật đo được) |
| `log-auth-rec-enc-emr-vit-inb.txt` | Log chạy nhóm AUTH · REC · ENC/EMR · VIT/INB |
| `log-cls-doc-rx.txt` | Log chạy nhóm CLS · DOC · RX |
| `log-bil-dis-apm-brn-sec.txt` | Log chạy nhóm BIL · DIS · APM · BRN · SEC (có số tồn kho trước/sau) |
| `log-bug06-dtqg-500-stacktrace.json` | Stack trace Serilog của lỗi 500 DTQG |
| `retest_util.py`, `retest2.py`, `retest2b.py`, `retest2c.py` | Mã chạy lại được (reproduce) |

Chạy lại:

```bash
cp .qc-tmp/... .qc-tmp/          # cac script nay chay tu .qc-tmp (can .qc-tmp/api.py + admin_totp.txt)
python .qc-tmp/retest2.py        # phan 1
python .qc-tmp/retest2b.py       # phan 2
python .qc-tmp/retest2c.py       # phan 3
```
