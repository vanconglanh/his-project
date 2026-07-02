﻿# CRUD Evidence R5 — 2026-06-01

## Tom tat verdict
**FAIL** — 2/5 suite khong dat target.

| Suite                    | Ket qua            | Target  | Status |
|--------------------------|--------------------|---------|--------|
| BE xUnit                 | 464 pass / 0 fail / 2 skip | 100%  | PASS   |
| FE typecheck (tsc)       | 0 error            | 0 error | PASS   |
| FE build (next build)    | OK                 | OK      | PASS   |
| E2E CRUD (15 spec)       | 37 PASS / 19 SKIP / 0 FAIL (66%) | >=75% | **FAIL** |
| E2E reports-cohort-fix   | 1 pass / 1 fail / 2 not-run | 4/4 | **FAIL** |

## Moi truong test
- BE: dotnet 10.0.300 chay tai http://localhost:5000 voi `ASPNETCORE_ENVIRONMENT=Development`
- FE: Next 16.2.6 dev server tai http://localhost:3000 (PID 21144, instance san co)
- DB: MySQL 8 (container `prodiab-mysql`), schema `prodiab_his`
- Seed B3 da co san trong DB:
  - `diab_his_enc_encounters` status=IN_PROGRESS: 1 record
  - `diab_his_bil_billing` status=FINALIZED: 2 records, PARTIAL_PAID: 1 record
- Login admin OK: `admin@prodiab.local` / `Admin@123`
  - 64 permission, gom day du `billing.print`, `cashier.print_receipt`, `dtqg.submit`

## Chi tiet B4

### B4.1 BE xUnit
```
ProDiabHis.ArchitectureTests : 6 passed
ProDiabHis.UnitTests          : 458 passed
ProDiabHis.IntegrationTests   : 0 passed, 2 skipped
Total: 464 passed / 0 failed / 2 skipped (2s)
```

### B4.2 FE typecheck
`npx tsc --noEmit` → 0 loi, 0 warning.

### B4.3 FE build
`npm run build` PASS — sinh day du route gom `/billing`, `/reports`, `/portal/*`.

### B4.4 E2E CRUD (e2e/crud-actions.spec.ts)
- 15/15 spec passed (3.7 phut)
- **Action level: 37 PASS / 19 SKIP / 0 FAIL → 66.07%**
- SKIP chu yeu vi action "Update / SuspendActivate / EditPermissions" khong tim thay button trong UI (vd: AdminRoles CREATE+EditPermissions, AdminTenants SuspendActivate, Supplier Update)
- Khong dat target 75% → **FAIL**

### B4.5 E2E reports-cohort-fix (e2e/reports-cohort-fix.spec.ts)
- TC01 - Financial tab khong crash: **PASS** (consoleErrors=16, page error=0)
- TC02 - Clinical tab cohort card render: **FAIL** — `Expect at least one cohort mock value (348 or 87) visible`. Stub mock (`stubAuth`) khong inject duoc cohort value, card render rong.
- TC03, TC04: **DID NOT RUN** (Playwright config `fullyParallel:false, workers:1` -> sau fail thi 2 TC tiep theo skip)
- 1/4 → **FAIL** so voi target 4/4

## Blocker giai quyet trong R5
- `dotnet run` mac dinh khong load `appsettings.Development.json` -> phai set `ASPNETCORE_ENVIRONMENT=Development` truoc khi chay. Da fix trong qua trinh test.
- Playwright khi chay `npx playwright test e2e/...` tu thu muc `frontend/` ma khong --config thi `baseURL` bi reset null -> "Cannot navigate to invalid URL". Workaround: `--config=e2e/playwright.config.ts` + `BASE_URL=http://localhost:3000`.

## Bug found

### BUG-R5-01 (major) — TC02 Clinical Cohort card khong render mock value
- Severity: major
- Suite: e2e/reports-cohort-fix.spec.ts
- Repro: chay TC02 voi `stubAuth` -> tab Clinical /reports -> kiem tra card cohort -> khong thay value "348" hoac "87"
- Suggested owner: frontend (FE chua respect mock API hoac chua bind data tu `/api/v1/reports/cohort?dm_type=ALL` vao Card)

### BUG-R5-02 (minor) — 19 CRUD action SKIP do thieu nut/UI
- Action SKIP duoc log: 
  - AdminRoles: CREATE, EditPermissions
  - AdminTenants: SuspendActivate
  - Supplier: Update
  - + cac action UI gap khac trong patient/encounter/prescription/pharmacy
- Suggested owner: frontend (cong thanh UI gap backlog)
- Khong block, chi anh huong coverage %

## Khuyen nghi
1. **Frontend fix TC02**: bind cohort API vao Clinical card hoac dieu chinh stub trong test
2. **Frontend bo sung action UI**: dat coverage CRUD len >=75% bang viec them nut Update/Suspend/EditPermissions vao Admin/Supplier
3. **R6 retest**: khi 2 viec tren xong moi chuyen sang verdict PASS
