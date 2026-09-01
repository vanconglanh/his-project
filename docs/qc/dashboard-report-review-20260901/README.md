# Evidence — Dashboard & Report Review 2026-09-01

## API Evidence (verified via browser JavaScript execution)

### GET /api/v1/dashboard/overview
Status: 200 OK
```json
{
  "data": {
    "today_revenue": 320000,
    "today_encounters": 0,
    "waiting_patients": 2,
    "bhyt_pending_count": 0,
    "dtqg_failed_count": 10,
    "low_stock_alerts": 0,
    "near_expiry_alerts": 0
  }
}
```
Kết luận: BE trả flat shape. FE cũ đọc nested → hiện "—". Fix: cập nhật FE interface.

### GET /api/v1/dashboard/charts/revenue-trend?range=30d
Status: 200 OK — trả `series` (không phải `points`) với data thật.

### GET /api/v1/reports/revenue?period=DAY
Status: 200 OK
```json
{
  "data": {
    "total_revenue": 370000,
    "net_revenue": 370000,
    "total_invoices": 2,
    "total_refunds": 0,
    "series": [
      {"label": "2026-08-20", "value": 50000, "color": null},
      {"label": "2026-09-01", "value": 320000, "color": null}
    ]
  }
}
```
Kết luận: FE đọc `total` (undefined) và `by_breakdown` (undefined). Fix: cập nhật interface.

### GET /api/v1/reports/encounters/count?period=DAY
Status: 200 OK — trả `period_label + count` đúng với FE interface.

### GET /api/v1/reports/pharmacy/top-drugs?order_by=REVENUE
Status: 200 OK — trả `[]` (no pharmacy data in test DB). FE interface OK.

### Chain Dashboard /api/v1/dashboard/branch-ranking
Hiển thị: Phòng khám Đái tháo đường DiaBetis HCM — 3.105.000 đ, 19 lượt khám. OK.

## Tóm tắt bug đã fix

| Bug | File FE sửa | Trạng thái |
|---|---|---|
| BUG-D1: KPI cards "—" | `lib/api/dashboard.ts`, `DashboardOverview.tsx`, `vi.json` | Code fixed, chờ WSL restart |
| BUG-D2: Charts "Chưa có dữ liệu" | `lib/api/dashboard.ts`, `DashboardOverview.tsx` | Code fixed, chờ WSL restart |
| BUG-D3: FinancialTab "— đ" | `lib/api/reports.ts`, `FinancialTab.tsx` | Code fixed, chờ WSL restart |

## Ghi chú môi trường

Next.js dev server chạy trong WSL, files trên Windows filesystem (`D:\...`). File watcher inotify trong WSL không nhận event khi file được sửa từ Windows side. Cần `restart npm run dev` từ WSL terminal để Turbopack compile lại.

Lệnh restart: từ WSL terminal, `cd /path/to/frontend && npm run dev`
