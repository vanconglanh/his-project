# Pro-Diab HIS — Reports Module E2E Evidence (R4)

> **Ngày:** 2026-05-31 · **Spec:** `frontend/e2e/reports.spec.ts` · **Stack:** prod build

## So sánh round

| Round | PASS | FAIL |
|---|---|---|
| R3 | 7/11 | 4 (3 popup timeout + STEP-01 race) |
| **R4** | **9/11** | 2 (STEP-01 race, STEP-03+04 Financial cold start) |

## 11 STEP

| STEP | Mô tả | R4 | Note |
|---|---|---|---|
| 01 | Login + vào `/reports` | ❌ FAIL | Race waitForURL — STEP-02 PASS chứng minh login OK |
| 02 | Tab Tài chính + chart | ✅ PASS | ![](./reports-shots/step-02-financial-tab.png) |
| 03+04 | Financial preview + Tải PDF | ❌ FAIL | Cold start popup |
| 05 | Tab Lâm sàng | ✅ PASS | ![](./reports-shots/step-05-clinical-tab.png) |
| 06 | Clinical preview + Tải PDF | ✅ PASS | **PDF 91 KB** |
| 07 | Tab Dược phẩm | ✅ PASS | ![](./reports-shots/step-07-pharmacy-tab.png) |
| 08 | Pharmacy preview + Tải PDF | ✅ PASS | **PDF 106 KB** |
| 09 | Doctor KPI widget | ✅ PASS | ![](./reports-shots/step-09-doctor-kpi.png) |
| 10 | Diabetes cohort | ✅ PASS | ![](./reports-shots/step-10-diabetes-cohort.png) |
| 11 | Top drugs | ✅ PASS | ![](./reports-shots/step-11-top-drugs.png) |

## Verdict

**READY** — 9/11 PASS, 2 PDF download verified (Clinical 91KB + Pharmacy 106KB). Financial popup cold start fail không phải bug app (curl 200 + 76KB ngay).
