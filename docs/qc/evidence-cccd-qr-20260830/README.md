# Evidence — Frontend quét QR CCCD (2026-08-30)

Nguồn: `docs/prd/quet-qr-cccd-20260830.md`, mục I-2 trong `docs/TASKLIST-20260829.md`.

## Môi trường verify

- Rebuild `prodiab-backend` từ source hiện tại (image cũ đang chạy trên docker compose local
  KHÔNG có commit `8a8c306`/`d5f1f5a` — 404 khi gọi `check-cccd-duplicate` trước khi rebuild).
  Sau `docker compose -f ops/docker-compose.yml -f ops/docker-compose.local-app.yml build backend`
  + `up -d --no-deps backend`, endpoint hoạt động đúng.
- Frontend chạy `next dev --turbo -p 3100` (Node 22.22.2 qua `nvm use 22.22.2` — container cần
  Node >= 20.9, máy dev mặc định Node 18) trỏ `NEXT_PUBLIC_API_BASE_URL=http://localhost:5000`.
- Không có công cụ trình duyệt tương tác (computer-use) trong phiên này — verify bằng cách:
  1. Gọi trực tiếp API thật qua `curl` để xác nhận đúng shape JSON (snake_case toàn cục).
  2. Request page HTML qua `curl` kèm cookie `his-access-token` hợp lệ để ép Next.js compile +
     render server-side, kiểm tra không có lỗi runtime/compile và UI text đúng như code.

## 1. Backend contract — xác nhận đúng 3 case (curl thật)

```
GET /patients/check-cccd-duplicate?id_number=001099012399
→ {"data":{"case":"NONE","patient_id":null,...,"field_diffs":[]}}

POST /patients { full_name, gender, date_of_birth, id_number, address.street } → tạo BNT01000030

GET check-cccd-duplicate (đúng full_name/dob/gender/address như lúc tạo)
→ {"case":"EXACT_MATCH","patient_id":"82515905-...","field_diffs":[]}

GET check-cccd-duplicate (đổi address)
→ {"case":"FIELD_MISMATCH","field_diffs":[{"field":"address","old_value":"...","new_value":"..."}]}

PUT /patients/{id}/apply-cccd-fields {"fields":[{"field":"address","new_value":"..."}]}
→ 200, patient.address.street đã cập nhật đúng field đã tích.
```

Kết luận: response key là `case` (PascalCase C# `Case` → snake_case `case`), `patient_id`,
`field_diffs[].field/old_value/new_value` — đúng như đã khai báo trong
`frontend/lib/api/types.ts` (`CccdDuplicateCheckResult`, `CccdFieldDiff`).

## 2. Nhánh nghi trùng khi tạo BN (FR-101, ảnh hưởng type `createPatient()`)

```
POST /patients (full_name+dob+phone trùng bản ghi cũ)
→ {"data":{"possible_duplicate":true,"duplicate_candidates":[{...,"match_reason":"SDT_HOTEN_NGAYSINH_TRUNG"}]}}

POST /patients (kèm confirm_create_despite_duplicate: true)
→ tạo thành công bản ghi mới bình thường.
```

Xác nhận: JsonNamingPolicy.SnakeCaseLower toàn cục convert `possibleDuplicate`/`duplicateCandidates`
(khai báo camelCase trong anonymous object ở `PatientsController.Create`) thành
`possible_duplicate`/`duplicate_candidates` — đúng như đã sửa trong
`CreatePatientPossibleDuplicateResponse` (types.ts) sau khi phát hiện qua test thật (ban đầu đoán
nhầm giữ nguyên camelCase).

## 3. Frontend render — không lỗi compile/runtime

- `GET /patients/new` (kèm cookie access token thật) → HTTP 200, dev server log không có lỗi,
  HTML chứa text "Quét CCCD" và "Đưa con trỏ vào đây rồi quét thẻ CCCD..." (từ `CccdQrScanner`).
  File: `patients-new-rendered.html`.
- `GET /reception` → HTTP 200, HTML chứa "Quét CCCD" (nút quét gắn trong `ReceptionCheckInForm`).
  File: `reception-rendered.html`.
- `npx tsc --noEmit` sạch (exit 0).
- `npx eslint` trên toàn bộ file mới/sửa: 0 error, chỉ còn 3 warning CÓ SẴN từ trước (không liên
  quan tới thay đổi của task này) trong `PatientEditorLayout.tsx`.

## Giới hạn của phiên verify này

Không mô phỏng được chính xác hành vi bàn phím tốc độ cao của máy quét thật qua giao diện (không
có công cụ trình duyệt tương tác trong phiên). Logic parse (`frontend/lib/utils/cccd-qr.ts`) được
viết mirror 1:1 với `backend/src/ProDiabHis.Application/Patients/CccdQrParser.cs` (đã có 22/22 unit
test backend pass ở phía server cho cùng logic ngày, giờ, giới tính, field count). Khuyến nghị QC
làm thêm 1 vòng test tay bằng máy quét thật hoặc trình duyệt thật trước khi go-live.
