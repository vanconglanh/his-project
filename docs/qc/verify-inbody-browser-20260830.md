# Verify E2E qua browser that — InBody OCR (PDF) — 2026-08-30

- Commit verify: `9064452` (FE) + `d76e090` (BE), nhanh `develop`.
- Moi truong: stack local `ops/docker-compose.yml` + `ops/docker-compose.local-app.yml`
  (backend :5000, frontend :3000, MySQL/MinIO/Redis that).
- Browser that: Chromium (Playwright, session `qc-lib.newSession`), dang nhap qua panel
  "Dang nhap nhanh theo vai tro" tren `/login` (role **Bac si** — co `patient.clinical.write`).
- Evidence: `docs/qc/evidence-inbody-ocr-20260830/br-*.png`, log `browser-log-qc-20260830.txt`,
  script `qc-script-inbody-b2.js`, `qc-script-inbody-b4.js`.

## Tong ket

| # | Buoc | Ket qua |
|---|---|---|
| 1 | Dang nhap role co `patient.clinical.write` (Bac si) | PASS |
| 2 | Tab "Nhap tu may InBody (PDF)" canh form nhap tay trong sheet ghi sinh hieu | PASS |
| 3 | File PDF mau co san (`sample-inbody-full.pdf`, `sample-inbody-partial.pdf`) | PASS |
| 4 | Upload full PDF -> bang 9 chi so, checkbox "Dung" tich san | PASS |
| 5 | Sua tay 1 gia tri -> "Xac nhan & Luu" | PASS |
| 6 | Upload partial PDF -> tag "Chua doc duoc" (vang), khong chan submit | PASS |
| 7 | Tab "Lich su InBody" o `/patients/[id]` | PASS (kem BUG-02 link file goc) |
| 8 | Can nang vao vital signs / lich su sinh hieu | PASS (kem BUG-03 pre-exist) |
| 9 | Upload file rac doi duoi `.pdf` -> loi tieng Viet, khong crash | PASS (kem BUG-01) |

**Verdict: PASS** — 9/9 buoc dat, 0 bug critical. 4 diem can xu ly (2 minor moi, 1 major cau hinh
ha tang dung chung, 1 minor pre-exist ngoai pham vi InBody).

## Chi tiet

### Buoc 1 — Dang nhap
Dang nhap role **Bac si** (`bacsi.test@prodiab.test` / `Test@123`) qua UI that, vao duoc dashboard.
Evidence `br-01-after-login.png`.

### Buoc 2 — Tab InBody trong sheet ghi sinh hieu — PASS
`/encounters/764e58fe-d048-4765-bb0e-8b3e0ac3b75b` -> nut "Ghi sinh hieu" -> sheet co dung 2 tab
"Nhap tay" | "Nhap tu may InBody (PDF)". Evidence `br-30-vital-sheet-tabs.png`,
`br-31-inbody-tab-in-sheet.png`.

### Buoc 4 — Upload `sample-inbody-full.pdf` — PASS
Bang xac nhan render du **9 chi so** dung PRD muc 5 (Can nang, BMI, SMM, Khoi luong mo, PBF,
Mo noi tang, TBW, BMR, Diem InBody), 9/9 tag "Doc duoc", **9/9 checkbox "Dung" tich san**, 0 field
"Chua doc duoc". Evidence `br-10-full-result.png`, `br-32-extract-in-encounter.png`.

### Buoc 5 — Sua tay roi luu — PASS
Sua Can nang 68.4 -> **77.7** tren UI -> "Xac nhan & Luu" -> toast xanh
"Da luu ket qua InBody vao ho so", khong co page error. DB that:
`diab_his_enc_vital_signs` co ban ghi moi `weight_kg = 77.70`,
`note = 'Nhap tu ket qua may InBody (da xac nhan)'`, gan dung `encounter_id` — tuc gia tri SUA TAY
duoc luu, khong phai gia tri extract goc. Evidence `br-33-edit-weight-77.7.png`,
`br-34-confirm-success-toast.png`.

Truong hop mo panel tu tab "Lich su InBody" cua ho so benh nhan (khong co encounter): UI hien canh
bao do "Chua chon luot kham (encounter) — khong the luu can nang vao sinh hieu", bam Luu van bi BE
chan dung voi toast `Can chon luot kham de ghi can nang vao sinh hieu` (HTTP 422
`INBODY_ENCOUNTER_REQUIRED`). Evidence `br-12-after-confirm.png`.

### Buoc 6 — Upload `sample-inbody-partial.pdf` — PASS
4 field doc duoc (Can nang/BMI/SMM/PBF) tag xanh "Doc duoc"; **5 field thieu** (Khoi luong mo,
Mo noi tang, TBW, BMR, Diem InBody) tag vang **"Chua doc duoc"**, o gia tri rong va checkbox "Dung"
tu bo tich. Nut "Xac nhan & Luu" van enabled -> khong chan cac field con lai. HTTP 200, khong 500.
Evidence `br-13-partial-result.png`.

### Buoc 7 — Tab "Lich su InBody" — PASS
`/patients/f0000000-0000-0000-0000-000000000008` -> tab "Lich su InBody" hien thi danh sach cac lan
do (badge trang thai "Cho xac nhan"/"Da xac nhan (du)"/"Da xac nhan (thieu)" + tom tat chi so + link
"Xem file goc"). So ban ghi tang dung 5 -> 7 sau 2 lan upload trong phien test.
Evidence `br-02-history-tab.png`, `br-15-history-after.png`. Xem BUG-02 ve link file goc.

### Buoc 8 — Phan anh vao sinh hieu — PASS
Drawer "Xem tat ca" cua Sinh hieu hien thi ban ghi can nang **77.7** sinh ra tu luong InBody, nam
cung dong thoi gian voi ban ghi nhap tay. Evidence `br-42-vital-history-drawer.png`. Xem BUG-03 ve
card tom tat ben sidebar.

### Buoc 9 — File khong hop le — PASS
File text doi duoi `.pdf` -> BE tra 422 `INBODY_EXTRACT_FAILED`, UI hien toast tieng Viet ro rang
**"Khong doc duoc file PDF, vui long kiem tra lai file hoac nhap tay"**, dialog van song, trang
khong crash, chon lai file khac duoc. File sai dinh dang (`.txt`) -> 422 `INBODY_INVALID_FORMAT`
"Chi chap nhan file PDF". Evidence `br-20-invalid-toast.png`. Xem BUG-01.

## Kiem tra bo sung o tang API (curl that, khong mock)

- RBAC: role **Ke toan** (khong co `patient.clinical.write`) POST upload -> **403 PERMISSION_DENIED**;
  role **KTV** upload/confirm -> OK. GET lich su: Ke toan van 200 (dung PRD AC-7 — GET chi can quyen
  doc benh nhan).
- Multi-tenant / not-found: `GET /patients/00000000-.../inbody-reports` -> **404 PATIENT_NOT_FOUND**.
- Audit log: `diab_his_sec_audit_logs` co du `CREATE` (luc upload) va `CONFIRM` (luc xac nhan) voi
  `resource_type = 'InBodyReport'`. Upload bi tu choi (file rac) KHONG sinh log CREATE — dung.
- `diab_his_cli_indicator_reading`: moi field include=true tao 1 dong, `source = 'inbody_ocr'`,
  `indicator_type`/`unit` khop bang mapping. Gui `BMI include=true` -> BE bo qua im lang, khong tao
  dong, khong loi (dung thiet ke PRD "BMI khong persist rieng"). Xem BUG-04 (cosmetic).

## Bug phat hien

### BUG-01 — Unhandled promise rejection khi upload loi — **minor**
- Vi tri: `frontend/components/domain/InBodyImportPanel.tsx:83` (`await uploadMutation.mutateAsync(...)`
  khong co `try/catch`); tuong tu `:109` cho `confirmMutation.mutateAsync`.
- Repro: mo panel InBody -> chon file khong doc duoc -> "Tai len & doc".
- Thuc te: toast loi tieng Viet van hien dung va trang khong crash, **nhung** browser ghi nhan
  `pageerror: Request failed with status code 422` (unhandled rejection) — log trong
  `browser-log-qc-20260830.txt`. Tren build dev se bat error overlay cua Next, gay hieu nham la crash;
  ngoai ra lam nhieu Sentry.
- De xuat owner: **frontend** — boc `try { } catch { /* toast da xu ly o hook onError */ }`.

### BUG-02 — Link "Xem file goc" tro host noi bo `minio:9000`, browser khong mo duoc — **major (cau hinh ha tang, khong rieng InBody)**
- Vi tri: `backend/src/ProDiabHis.Infrastructure/Storage/MinioFileStorage.cs` (presigned URL sinh theo
  `Minio__Endpoint`); `ops/docker-compose.local-app.yml:25`, `docker-compose.deploy.yml:64`,
  `docker-compose.prod.yml:158` deu set `Minio__Endpoint: "minio:9000"`.
- Repro: tab "Lich su InBody" -> click "Xem file goc". `href` =
  `http://minio:9000/inbody-reports/...` -> hostname chi resolve duoc ben trong docker network,
  browser nguoi dung khong mo duoc file.
- Anh huong: moi module co file (CLS, don thuoc PDF...), khong phai loi do code InBody moi. Can
  them cau hinh `Minio__PublicEndpoint` (hien chua ton tai trong codebase) hoac proxy Nginx
  `/files/*`.
- De xuat owner: **devops + backend**.

### BUG-03 — Card "Sinh hieu" o sidebar kham benh khong tu refresh sau khi luu — **minor, PRE-EXIST (khong do InBody)**
- Vi tri: card lay du lieu tu `encounter.vital_signs_latest`
  (`components/domain/EncounterPatientSidebar.tsx:35`, `EncounterDetailClient.tsx:173`) — tuc thuoc
  query chi tiet encounter, trong khi ca `useConfirmInBodyReport`
  (`lib/hooks/use-inbody-reports.ts:52-58`) lan `useCreateVitalSigns`
  (`lib/hooks/use-vital-signs.ts:47-50`) chi invalidate `vitalKeys.*`.
- Repro: luu sinh hieu (nhap tay HOAC qua InBody) -> card sidebar van hien gia tri cu; F5 moi dung.
- Da kiem chung luong **nhap tay cu bi y het** (`br-40-manual-vital-save.png` / `br-41-after-reload.png`)
  -> khong phai hoi quy do tinh nang InBody, nhung nen fix chung.
- De xuat owner: **frontend**.

### BUG-04 — Checkbox "Dung" cua BMI tich san nhung luu khong co tac dung — **minor (cosmetic)**
- Vi tri: `frontend/components/domain/InBodyImportPanel.tsx:56` (`include: f?.extracted`) — BMI
  extract duoc nen mac dinh tich, nhung BE co tinh khong persist BMI (PRD muc 5).
- He qua: nguoi dung tuong BMI duoc luu vao ho so trong khi thuc te bi bo qua im lang.
- De xuat owner: **frontend** — disable checkbox BMI + ghi chu "BMI tinh lai tu can nang/chieu cao".

## Cap nhat fix — 2026-08-30 (Thao, backend)

Ca 4 bug da duoc fix va verify lai. Chi tiet tung bug:

### BUG-02 — DA FIX
- Them client MinIO rieng (`AddKeyedSingleton<IMinioClient>("public", ...)`) trong
  `backend/src/ProDiabHis.Infrastructure/DependencyInjection.cs`, dung config moi `Minio:PublicEndpoint`
  (+ `Minio:PublicUseSsl`) — fallback ve `Minio:Endpoint` neu khong set.
- `backend/src/ProDiabHis.Infrastructure/Storage/MinioFileStorage.cs`: `GetSignedUrlAsync` dung
  `_publicClient` (endpoint public) de sinh presigned URL tra ve FE; cac thao tac server-to-server
  (Upload/Download/Delete/EnsureBucket) van dung `_client` voi `Minio:Endpoint` noi bo — KHONG doi.
- Cau hinh: `ops/docker-compose.local-app.yml` set `Minio__PublicEndpoint: ${MINIO_PUBLIC_ENDPOINT:-localhost:9000}`
  (MinIO da publish port 9000 ra host o local dev); `ops/docker-compose.deploy.yml` va
  `ops/docker-compose.prod.yml` doc tu `${MINIO_PUBLIC_ENDPOINT}` (BAT BUOC set trong `.env` khi deploy,
  KHONG co gia tri mac dinh — xem ghi chu trong `ops/.env.example`); `appsettings.json` them default
  `localhost:9000` cho local (khong container).
- Verify that (khong dung mock): dang nhap qua API that (`bacsi.test@prodiab.test`), goi
  `GET /api/v1/patients/{id}/inbody-reports`, xac nhan `file_url` tra ve co host `localhost:9000`
  (KHONG con `minio:9000`), sau do `curl` truc tiep URL nay tu ben ngoai docker network ->
  **HTTP 200, Content-Type: application/pdf**, tai duoc file PDF that (chung minh chu ky presigned URL
  van hop le voi host moi). Evidence: `docs/qc/evidence-inbody-ocr-20260830/bug02-fix-verify-20260830.txt`,
  `bug02-inbody-pdf-downloaded.pdf`.
- Ghi chu: cong cu cua Thao (backend agent) khong co Playwright/browser pane truc tiep nhu QC — da verify
  bang HTTP request that (khong mock) thay vi click UI; ket qua tuong duong ve mat ky thuat (URL truy cap
  duoc tu ngoai docker network, dung URL scheme browser se dung khi click "Xem file goc"). De nghi QC verify
  lai buoc click UI that ("Xem file goc" mo tab moi hien PDF) trong lan retest ke tiep.
- Anh huong dien rong da ra soat: moi noi dung `IFileStorage.GetSignedUrlAsync` (InBody, CLS, don thuoc PDF,
  Patients, Files) deu di qua ham nay -> fix 1 lan la du, khong can sua rieng tung module.

### BUG-01 — DA FIX
- `frontend/components/domain/InBodyImportPanel.tsx`: boc `try { await ...mutateAsync(...) } catch {}`
  quanh ca `uploadMutation.mutateAsync` (dong ~83-91) va `confirmMutation.mutateAsync` (dong ~109-122).
  Toast loi van hien dung qua `onError` cua hook, chi khac la khong con unhandled rejection len console/Sentry.
- Verify: `npx tsc --noEmit` sach; da rebuild + redeploy container frontend.

### BUG-03 — DA FIX
- Them invalidate `encounterKeys.detail(encounterId)` (tu `frontend/lib/hooks/use-encounters.ts`) vao
  `onSuccess` cua CA 3 noi: `useCreateVitalSigns`, `useUpdateVitalSign`
  (`frontend/lib/hooks/use-vital-signs.ts`) va `useConfirmInBodyReport`
  (`frontend/lib/hooks/use-inbody-reports.ts`) — ca luong nhap tay lan InBody deu refresh dung card sidebar
  ma khong can F5.
- Verify: `npx tsc --noEmit` sach; logic invalidate them, khong doi hanh vi cache khac.

### BUG-04 — DA FIX
- Xac nhan trong `backend/src/ProDiabHis.Application/InBody/InBodyHandlers.cs` (dong ~224,
  `InBodyIndicatorTypes.IndicatorTableTypes`) — BMI thuc su KHONG nam trong danh sach duoc persist vao
  `diab_his_cli_indicator_reading` (chi tinh lai tu can nang + chieu cao o noi khac theo PRD).
- `frontend/components/domain/InBodyImportPanel.tsx`: checkbox "Dung" cua BMI mac dinh KHONG tich
  (`toEditable`), disable hoan toan + boc `Tooltip` giai thich "BMI duoc tinh tu dong tu can nang va
  chieu cao, khong can xac nhan rieng" (dung `TooltipProvider` da co san o `app/layout.tsx`).
- Verify: `npx tsc --noEmit` sach.

### Kiem tra chung sau fix
- `dotnet build` (Infrastructure + Api): 0 error, cac warning con lai la pre-existing (khong lien quan fix).
- `dotnet test tests/ProDiabHis.UnitTests`: **858/858 PASS**.
- `npx tsc --noEmit` (frontend): sach, 0 loi.
- Da rebuild + `docker compose up -d --build backend frontend` tren stack local
  (`ops/docker-compose.yml` + `ops/docker-compose.local-app.yml`) de chay dung code moi truoc khi verify.

## Ghi chu moi truong

Lan chay dau tien tab "Lich su InBody" **khong hien thi** — nguyen nhan la image
`prodiab-dev-frontend` dang chay duoc build luc 00:32 UTC, TRUOC commit FE `9064452` (02:12 UTC).
Sau khi `docker compose ... up -d --build frontend` thi hien thi dung. Day la van de moi truong,
khong phai bug san pham — nhung luu y: **image local phai rebuild sau moi commit FE** truoc khi test.

---

## Verify BUG-02 qua UI click-through

**Ket qua: PASS**

Lan verify truoc chi dung `curl` doc `file_url` tu API, chua bam that tren trinh duyet. Lan nay chay
click-through that bang Playwright (Chromium, locale vi-VN) tren stack local dang chay
(`prodiab-frontend`, `prodiab-backend`, `prodiab-minio` deu Up):
dang nhap `Bac si` qua panel dang nhap nhanh `/login` -> vao `/patients/f0000000-0000-0000-0000-000000000008`
-> mo tab **"Lich su InBody"** (hien thi 8 ban ghi, moi ban ghi deu co link "Xem file goc")
-> **bam that** vao link "Xem file goc" cua ban ghi dau tien.

Ket qua tung diem:

- **Href tren the `<a>`**: `http://localhost:9000/inbody-reports/inbody/1/.../7637516b-...pdf?X-Amz-Algorithm=AWS4-HMAC-SHA256&...`
  — host = `localhost:9000`, **KHONG** phai `minio:9000` noi bo.
- **Click that**: trinh duyet mo tab moi (`target="_blank"`) va tai file ve thanh cong
  (`7637516b-48b9-4db5-a022-c98297c399a6.pdf`). Headless Chromium khong co PDF viewer nen ket qua la
  download thay vi render inline — dung nhu ky vong, khong co loi ket noi / `ERR_NAME_NOT_RESOLVED`.
- **File tai ve hop le**: `PDF document, version 1.7, 1 page(s)`, 17.019 bytes, magic bytes `%PDF-`.
- **Network tab (bat toan bo request cua browser context)**: chi co dung 1 request toi MinIO va la
  `GET http://localhost:9000/inbody-reports/...`; kiem tra `//minio:9000` trong toan bo danh sach
  request = `false`.
- **Goi lai URL bang network stack cua browser**: `HTTP 200`, `content-type: application/pdf`, 17.019 bytes.
- **Page error / console error**: khong co (`PAGEERROR = []`).

=> BUG-02 (presigned URL MinIO sinh bang host noi bo `minio:9000` khien browser khong resolve duoc)
da duoc fix trong commit `2b4c6dc` va **xac nhan dong qua thao tac click that tren UI**.

**Evidence** (`docs/qc/evidence-inbody-ocr-20260830/`):
- `qc-script-bug02-clickthrough.js` — script tai hien
- `bug02-clickthrough-log.txt` — log tung buoc B1..B6
- `bug02-01-patient-page.png`, `bug02-02-inbody-history-tab.png`, `bug02-03-link-xem-file-goc.png`
- `bug02-clickthrough-downloaded.pdf` — file PDF tai ve tu cu click that
