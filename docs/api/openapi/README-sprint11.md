# Sprint 11 - EPIC 9: Reports + BI Dashboard

## Files
- `reports-financial.yaml` - revenue, cashier, debts, BHYT, export
- `reports-clinical.yaml` - encounters, diagnoses, diabetes cohort, lab
- `reports-pharmacy.yaml` - top drugs, inventory, near-expiry, DTQG
- `dashboard.yaml` - overview, charts, alerts

## Story mapping

| Story    | Endpoints |
|----------|-----------|
| US-RP01  | GET /reports/revenue, /reports/revenue/by-doctor, /reports/revenue/by-service, /reports/revenue/by-payment-method |
| US-RP02  | GET /reports/cashier/daily-summary, /reports/debts/aging, /reports/bhyt/summary |
| US-RP03  | GET /reports/encounters/count, /reports/diagnoses/top, /reports/encounters/over-12h-rate |
| US-RP04  | GET /reports/diabetes/cohort, /reports/diabetes/hba1c-distribution, /reports/diabetes/complications-rate |
| US-RP05  | GET /reports/lab/abnormal-rate |
| US-RP06  | GET /reports/pharmacy/top-drugs, /reports/pharmacy/inventory-value, /reports/pharmacy/near-expiry-summary, /reports/pharmacy/stock-movements-summary |
| US-RP07  | GET /reports/prescriptions/by-doctor, /reports/dtqg/submission-rate |
| US-RP08  | GET /dashboard/overview, /dashboard/charts/*, /dashboard/alerts, POST /reports/export |

## Permissions (seed via 0054)
- `report.read` - view all report endpoints
- `report.export` - POST /reports/export
- `dashboard.read` - dashboard endpoints

## Materialized View Note (MySQL 8)

MySQL 8 does NOT support native MATERIALIZED VIEW (unlike PostgreSQL).
Strategy: physical cache tables, refresh nightly via Hangfire job.

### Migrations
- `0053_create_materialized_views.sql`:
  - `diab_his_rep_daily_revenue_cache` (tenant_id, clinic_id, business_date, total_revenue, encounter_count, invoice_count, by_payment_method JSON, refreshed_at)
  - `diab_his_rep_top_drugs_cache` (tenant_id, period_from, period_to, drug_id, quantity_sold, revenue, rank_no)
  - `diab_his_rep_doctor_kpi_cache` (tenant_id, period_from, period_to, doctor_id, encounter_count, revenue, rvu)
  - `diab_his_rep_diabetes_cohort_cache` (tenant_id, as_of_date, total_patients, hba1c_lt_7, hba1c_7_8, hba1c_8_9, hba1c_gt_9, retinopathy_count, neuropathy_count, nephropathy_count, cad_count, pad_count)
  - `diab_his_rep_inventory_value_cache` (tenant_id, warehouse_id, as_of_date, total_value, by_category JSON)
  - All have `tenant_id`, `refreshed_at`, composite UNIQUE on natural key, INDEX on (tenant_id, period/date)
- `0054_seed_permissions_sprint11.sql`: insert 3 permissions above into `diab_his_permission`.

### Caching strategy in IReportingService
- period < 30 days: query live tables with index hints
- period >= 30 days OR dashboard charts: read from cache tables
- If cache `refreshed_at` is stale (> 26h), fall back to live + log warning.

## Services (note for Thao)

| Service | Tech |
|---------|------|
| IReportingService | Dapper raw SQL, parameterized, tenant_id filter mandatory, USE INDEX hints |
| IExcelExporter | ClosedXML - xlsx output, multi-sheet for compound reports |
| IPdfReportExporter | QuestPDF - landscape A4, header logo per tenant, footer page numbers |
| ReportCacheRefreshJob | Hangfire RecurringJob, cron `0 2 * * *` (02:00 daily), refresh all `diab_his_rep_*_cache` tables per tenant; emit Serilog metrics on duration |
| IDashboardAlertCollector | Aggregates from pharmacy (low stock, near expiry), encounter (>12h), BHYT (pending), DTQG (failed) - Redis cached 60s |

## Export flow
POST /reports/export is synchronous. Constraints:
- Max 100k rows for CSV/EXCEL
- Max 1k pages for PDF
- File stored in MinIO bucket `reports/{tenant_id}/{yyyy-mm}/`, presigned URL TTL 1h
- Returns `file_url`, `file_name`, `expires_at`

## Multi-tenant
All queries MUST include `WHERE tenant_id = @TenantId`. RLS equivalent enforced at app layer (MySQL InnoDB no native RLS) via repository base class + integration test.

## Non-functional
- p95 overview <= 300ms (cache hit)
- p95 report query <= 2s for 30-day range
- Export EXCEL 10k rows <= 5s

## FHIR mapping (read-only reports - no resource creation)
- Encounter counts -> aggregated from `Encounter` resource pool
- Diagnoses top -> `Condition.code` (ICD-10)
- HbA1c distribution -> `Observation` with LOINC 4548-4
- MedicationRequest counts for prescriptions/by-doctor

## Open items / ADR candidates
- ADR-011: MySQL physical cache table vs nightly ETL to ClickHouse (deferred to Sprint 14 if data volume requires)
- ADR-012: QuestPDF (MIT for <=$1M revenue) license clearance
